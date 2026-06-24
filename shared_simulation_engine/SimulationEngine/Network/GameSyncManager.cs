#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using SimulationEngine.EnemyRelated;
using SimulationEngine.BulletRelated;
using SimulationEngine.BulletRelated.Behaviors;
using SimulationEngine.TowerRelated;
using SimulationEngine.TowerRelated.Behaviors;

namespace SimulationEngine.Network;

/// <summary>
/// Manages P2P game state synchronization between master (host) and clients.
///
/// Host: runs full simulation, broadcasts FrameDelta every tick, receives and validates client action requests.
/// Client: receives FrameDelta from master, applies state; sends tower action requests to master.
///
/// Ports:
///   47779 — host broadcasts game state (broadcast + loopback), all clients listen here.
///   47778 — host listens for client action requests.
///   47780 — Raft consensus (leader election + peer-ping), used by RaftGameNode.
///
/// Disconnection handling:
///   If no FrameDelta arrives for HeartbeatTimeout (3 s) the RaftGameNode:
///     1. Pings 2 random peers on port 47780.
///     2. Peers respond   → master is dead → Raft election → new host takes over.
///     3. No peer responds → own network broken → OnNetworkLost fires → 10-s disconnect countdown.
/// </summary>
public sealed class GameSyncManager : IDisposable
{
    private const int StateBroadcastPort = 47779;
    private const int RequestPort        = 47778;
    private const long HelloSeq   = long.MinValue;
    private const long RequestSeq = -1L;

    private readonly GameManager _gm;
    private readonly CancellationTokenSource _cts = new();

    // _isHostMode can flip at runtime when a client wins a Raft election.
    private volatile bool _isHostMode;

    // Host-only
    private UdpClient? _broadcaster;
    private UdpClient? _requestReceiver;
    private readonly List<GameEvent> _pendingEvents = new();
    private readonly ConcurrentQueue<FrameDelta> _pendingClientRequests = new();
    private long _seq;
    private readonly ConcurrentDictionary<string, int> _playerBalances = new(StringComparer.OrdinalIgnoreCase);
    private int _startingMoney = 100;

    // Client-only
    private UdpClient? _stateReceiver;
    private UdpClient? _requester;
    private IPEndPoint? _hostEndpoint;
    private readonly ConcurrentQueue<FrameDelta> _incomingDeltas = new();

    private RaftGameNode? _raftNode;
    private bool _raftPromotion; // guard: PromoteToHost runs only once
    private readonly ConcurrentDictionary<string, string> _peerIps = new(StringComparer.OrdinalIgnoreCase);

    private bool _disposed;

    // -----------------------------------------------------------------------
    // Public events (consumed by GameManager / GameRunner)
    // -----------------------------------------------------------------------

    /// <summary>Fired when our own network appears broken (couldn't reach any peer). Client should show disconnect screen.</summary>
    public event Action? OnNetworkLost;

    /// <summary>Fired when the active game master changes (Raft elected a new leader). Arg = new host IP.</summary>
    public event Action<string>? OnHostSwitched;

    // -----------------------------------------------------------------------

    public GameSyncManager(bool isHost, GameManager gm)
    {
        _isHostMode = isHost;
        _gm = gm;
        _startingMoney = gm.UIManager.Money;
        RegisterPlayerId(gm.UIManager.LocalPlayerInstanceId);
    }

    private readonly ConcurrentDictionary<string, byte> _seenPlayerIds = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterPlayerId(string? id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            _seenPlayerIds.TryAdd(id, 0);
            // Ensure balance is initialized
            GetPlayerBalance(id);
        }
    }

    public int GetPlayerBalance(string playerId)
    {
        if (string.IsNullOrEmpty(playerId) || playerId.Equals(_gm.UIManager.LocalPlayerInstanceId, StringComparison.OrdinalIgnoreCase))
        {
            return _gm.UIManager.Money;
        }

        return _playerBalances.GetOrAdd(playerId, _startingMoney);
    }

    public void SetPlayerBalance(string playerId, int amount)
    {
        if (string.IsNullOrEmpty(playerId) || playerId.Equals(_gm.UIManager.LocalPlayerInstanceId, StringComparison.OrdinalIgnoreCase))
        {
            _gm.UIManager.Money = amount;
            return;
        }
        _playerBalances[playerId] = amount;
    }

    // -----------------------------------------------------------------------
    // Setup
    // -----------------------------------------------------------------------

    /// <summary>
    /// Provides the IP addresses of all other players (instanceId → ip).
    /// Call AFTER construction but BEFORE StartAsHost / StartAsClient so the
    /// Raft node has peer info from the start.
    /// </summary>
    public void SetPeers(IReadOnlyDictionary<string, string> instanceToIp, string myInstanceId)
    {
        _peerIps.Clear();
        foreach (var kv in instanceToIp)
            _peerIps[kv.Key] = kv.Value;

        _raftNode = new RaftGameNode(myInstanceId);
        _raftNode.SetPeers(instanceToIp);
        _raftNode.OnBecameLeader    += HandleRaftBecameLeader;
        _raftNode.OnNewLeaderKnown  += HandleRaftNewLeaderKnown;
        _raftNode.OnNetworkLost     += HandleRaftNetworkLost;
    }

    public void StartAsHost()
    {
        _broadcaster = new UdpClient { EnableBroadcast = true };

        _requestReceiver = new UdpClient();
        _requestReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _requestReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, RequestPort));

        SubscribeToGameEvents();
        _ = Task.Run(() => ReceiveRequestsLoopAsync(_cts.Token), _cts.Token);

        if (_raftNode != null)
        {
            _raftNode.Start();
            // Announce leadership so clients immediately reset their election timers
            _ = _raftNode.SetAsLeaderAsync();
        }
    }

    public void StartAsClient(string hostIp)
    {
        _hostEndpoint = new IPEndPoint(IPAddress.Parse(hostIp), RequestPort);

        _stateReceiver = new UdpClient();
        _stateReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _stateReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, StateBroadcastPort));
        _stateReceiver.EnableBroadcast = true;

        _requester = new UdpClient();

        _ = Task.Run(() => ReceiveStateLoopAsync(_cts.Token), _cts.Token);
        SendHello();

        _raftNode?.Start();
    }

    private void SendHello()
    {
        var hello = new FrameDelta { Seq = HelloSeq };
        byte[] data = FrameDeltaSerializer.Serialize(hello);
        try { _requester?.Send(data, _hostEndpoint); } catch { }
    }

    // -----------------------------------------------------------------------
    // Host: broadcast tick
    // -----------------------------------------------------------------------

    public void BroadcastTick(GameTime gameTime)
    {
        if (!_isHostMode || _broadcaster == null) return;

        ProcessPendingClientRequests();

        var enemyTicks = CollectEnemyTicks();
        List<GameEvent> events;
        lock (_pendingEvents)
        {
            events = new List<GameEvent>(_pendingEvents);
            _pendingEvents.Clear();
        }

        var playerMoney = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(_gm.UIManager.LocalPlayerInstanceId))
        {
            playerMoney[_gm.UIManager.LocalPlayerInstanceId] = _gm.UIManager.Money;
        }
        foreach (var kvp in _playerBalances)
        {
            playerMoney[kvp.Key] = kvp.Value;
        }

        var delta = new FrameDelta
        {
            Seq    = Interlocked.Increment(ref _seq),
            Ts     = gameTime.TotalGameTime.TotalSeconds,
            Global = new GlobalState
            {
                Money      = _gm.UIManager.Money,
                PlayerMoney = playerMoney,
                Lives      = _gm.UIManager.Lives,
                WaveIdx    = _gm.WaveController?.CurrentWaveIndex ?? 0,
                WaveActive = _gm.WaveController?.IsWaveActive ?? false,
            },
            Enemies = enemyTicks,
            Events  = events,
        };

        byte[] payload = FrameDeltaSerializer.Serialize(delta);
        BroadcastState(payload);
    }

    private void BroadcastState(byte[] payload)
    {
        if (_broadcaster == null) return;

        // 1. Broadcast on all active network interfaces to handle multi-NIC/virtual adapter environments
        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;

                var props = ni.GetIPProperties();
                foreach (var unicast in props.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var ipBytes = unicast.Address.GetAddressBytes();
                        var maskBytes = unicast.IPv4Mask?.GetAddressBytes();
                        if (maskBytes == null || maskBytes.Length != 4) continue;

                        var broadcastBytes = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                        }
                        var subnetBroadcast = new IPAddress(broadcastBytes);
                        _broadcaster.Send(payload, new IPEndPoint(subnetBroadcast, StateBroadcastPort));
                    }
                }
            }
        }
        catch
        {
            // Fallback to standard broad broadcast
            var broadcastEp = new IPEndPoint(IPAddress.Broadcast, StateBroadcastPort);
            try { _broadcaster.Send(payload, broadcastEp); } catch { }
        }

        // 2. Local loopback broadcast (same-machine)
        var loopbackEp = new IPEndPoint(IPAddress.Loopback, StateBroadcastPort);
        try { _broadcaster.Send(payload, loopbackEp); } catch { }

        // 3. Unicast directly to all known peer players (fallback direct channel)
        foreach (var ipStr in _peerIps.Values)
        {
            if (!string.IsNullOrEmpty(ipStr) && IPAddress.TryParse(ipStr, out var ip))
            {
                try { _broadcaster.Send(payload, new IPEndPoint(ip, StateBroadcastPort)); } catch { }
            }
        }
    }

    private void ProcessPendingClientRequests()
    {
        while (_pendingClientRequests.TryDequeue(out var delta))
        {
            foreach (var evt in delta.Events)
            {
                try { ProcessClientRequest(evt); }
                catch (Exception ex) { Console.WriteLine($"[GameSync] Client request error: {ex.Message}"); }
            }
        }
    }

    private List<EnemyTick> CollectEnemyTicks()
    {
        var ec = _gm.EnemyController;
        if (ec == null) return [];
        return ec.Enemies
            .Where(e => e.isAlive && e.NetworkId >= 0)
            .Select(e => new EnemyTick { Id = e.NetworkId, X = e.Position.X, Y = e.Position.Y, Hp = e.Health })
            .ToList();
    }

    // -----------------------------------------------------------------------
    // Host: subscribe to game events
    // -----------------------------------------------------------------------

    private void SubscribeToGameEvents()
    {
        var ec = _gm.EnemyController;
        if (ec != null)
        {
            ec.OnEnemySpawned     += OnHostEnemySpawned;
            ec.OnEnemyKilled      += OnHostEnemyKilled;
            ec.OnEnemyReachedGoal += OnHostEnemyReachedGoal;
        }

        var wc = _gm.WaveController;
        if (wc != null)
        {
            wc.OnWaveStarted += idx => EnqueueEvent(new WaveStartedEvent { WaveIdx = idx });
            wc.OnWaveEnded   += idx => EnqueueEvent(new WaveEndedEvent   { WaveIdx = idx });
        }

        var dc = _gm.DamageDealerController;
        if (dc != null)
        {
            dc.OnBulletAdded   += OnHostBulletAdded;
            dc.OnBulletRemoved += OnHostBulletRemoved;
        }
    }

    private void OnHostEnemySpawned(Enemy e)
    {
        var sp = _gm.Map.SpawnPoints.FirstOrDefault(s => s.Id == e.SpawnPointId)
              ?? _gm.Map.SpawnPoints.FirstOrDefault(s => Vector2.Distance(s.Position, e.Position) < 5f);

        EnqueueEvent(new EnemySpawnedEvent
        {
            EnemyId      = e.NetworkId,
            TypeId       = e.TypeId,
            SpawnPointId = sp?.Id ?? "",
            PathId       = e.PathId,
            MaxHp        = e.MaxHealth,
            Speed        = e.Speed,
        });
    }

    private void OnHostEnemyKilled(Enemy e)
    {
        int totalReward = (int)Math.Max(10, e.MaxHealth / 10);
        var activePlayers = _seenPlayerIds.Keys.ToList();
        int numPlayers = activePlayers.Count;

        if (numPlayers > 0)
        {
            float totalDamageDealt = 0f;
            foreach (var kvp in e.DamageContributors)
            {
                totalDamageDealt += kvp.Value;
            }

            if (totalDamageDealt <= 0)
            {
                // Fallback: split equally
                int share = totalReward / numPlayers;
                int leftover = totalReward - (share * numPlayers);
                for (int i = 0; i < numPlayers; i++)
                {
                    int amt = share + (i == 0 ? leftover : 0);
                    SetPlayerBalance(activePlayers[i], GetPlayerBalance(activePlayers[i]) + amt);
                }
            }
            else
            {
                // Option 4: 50% split equally, 50% split by damage contribution
                int baseShareTotal = totalReward / 2;
                int baseSharePerPlayer = baseShareTotal / numPlayers;
                int leftoverBase = baseShareTotal - (baseSharePerPlayer * numPlayers);

                int damageShareTotal = totalReward - baseShareTotal;

                // 1. Distribute base share
                for (int i = 0; i < numPlayers; i++)
                {
                    int amt = baseSharePerPlayer + (i == 0 ? leftoverBase : 0);
                    SetPlayerBalance(activePlayers[i], GetPlayerBalance(activePlayers[i]) + amt);
                }

                // 2. Distribute damage share
                int distributedDamageShare = 0;
                var contributorList = e.DamageContributors.ToList();
                for (int i = 0; i < contributorList.Count; i++)
                {
                    var contributor = contributorList[i];
                    string pId = contributor.Key;
                    float dmg = contributor.Value;

                    if (string.IsNullOrEmpty(pId))
                    {
                        pId = _gm.UIManager.LocalPlayerInstanceId ?? string.Empty;
                    }

                    // Register this player ID if not seen before
                    RegisterPlayerId(pId);

                    int share = (int)Math.Round((dmg / totalDamageDealt) * damageShareTotal);
                    if (i == contributorList.Count - 1)
                    {
                        share = damageShareTotal - distributedDamageShare;
                    }
                    else
                    {
                        distributedDamageShare += share;
                    }

                    SetPlayerBalance(pId, GetPlayerBalance(pId) + share);
                }
            }
        }

        EnqueueEvent(new EnemyKilledEvent { EnemyId = e.NetworkId, Reward = totalReward });
    }

    private void OnHostEnemyReachedGoal(Enemy e)
        => EnqueueEvent(new EnemyReachedGoalEvent
        {
            EnemyId       = e.NetworkId,
            Damage        = e.Damage,
            BaseHpAfter   = _gm.UIManager.Lives,
            MobsRemaining = _gm.EnemyController?.Enemies.Count(en => en.isAlive) ?? 0,
        });

    private void OnHostBulletAdded(DamageDealer d)
    {
        float maxDist = d.Behavior is StandardBulletBehavior sbh ? sbh.MaxDistance : 500f;
        Console.WriteLine($"[Bullet] HOST spawned: id={d.NetworkId} hitRadius={d.HitRadius} behaviorHitRadius={d.Behavior.HitRadius} speed={d.Behavior.Speed} maxDist={maxDist} dmg={d.Behavior.Damage}");
        EnqueueEvent(new BulletSpawnedEvent
        {
            BulletId  = d.NetworkId,
            BehaviorId = d.Behavior.GetType().Name,
            Behavior  = BulletBehavior.Linear,
            X         = d.Position.X,
            Y         = d.Position.Y,
            Dx        = d.Direction.X,
            Dy        = d.Direction.Y,
            Speed     = d.Behavior.Speed,
            MaxDist   = maxDist,
            Dmg       = d.Behavior.Damage,
            HitRadius = d.HitRadius,
        });
    }

    private void OnHostBulletRemoved(DamageDealer d)
    {
        EnqueueEvent(new BulletImpactEvent
        {
            BulletId   = d.NetworkId,
            X          = d.Position.X,
            Y          = d.Position.Y,
            Damage     = d.Behavior.Damage,
            ImpactType = BulletImpactType.Mob,
        });
    }

    private void EnqueueEvent(GameEvent evt)
    {
        lock (_pendingEvents)
            _pendingEvents.Add(evt);
    }

    // Called by GameInputHandler when host places/sells/upgrades
    public void RecordTowerPlaced(int towerId, string zoneId, string behaviorId, string owner, int cost)
        => EnqueueEvent(new TowerPlacedEvent { TowerId = towerId, ZoneId = zoneId, BehaviorId = behaviorId, Owner = owner, Cost = cost });

    public void RecordTowerRemoved(int towerId, string zoneId, int refund)
        => EnqueueEvent(new TowerRemovedEvent { TowerId = towerId, ZoneId = zoneId, Refund = refund });

    public void RecordTowerUpgraded(int towerId, string behaviorId, int prevLevel, int level, int cost)
        => EnqueueEvent(new TowerUpgradedEvent { TowerId = towerId, BehaviorId = behaviorId, PrevLevel = prevLevel, Level = level, Cost = cost });

    // -----------------------------------------------------------------------
    // Host: receive and process client action requests
    // -----------------------------------------------------------------------

    private async Task ReceiveRequestsLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _requestReceiver!.ReceiveAsync(ct);
                var delta  = FrameDeltaSerializer.Deserialize(result.Buffer);
                if (delta == null || delta.Seq == HelloSeq || delta.Seq != RequestSeq) continue;

                _pendingClientRequests.Enqueue(delta);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException)    { break; }
            catch { }
        }
    }

    private void ProcessClientRequest(GameEvent evt)
    {
        switch (evt)
        {
            case TowerPlacedEvent   place:   ApplyClientTowerPlace(place);   break;
            case TowerRemovedEvent  remove:  ApplyClientTowerRemove(remove); break;
            case TowerUpgradedEvent upg:     ApplyClientTowerUpgrade(upg);   break;
        }
    }

    private void ApplyClientTowerPlace(TowerPlacedEvent place)
    {
        RegisterPlayerId(place.Owner);
        Console.WriteLine($"[Owner] HOST ApplyClientTowerPlace: zone='{place.ZoneId}' owner='{place.Owner}' myId='{_gm.UIManager.LocalPlayerInstanceId}'");
        if (string.IsNullOrEmpty(place.Owner))
        {
            Console.WriteLine($"[Owner] Client TowerPlace REJECTED: missing owner InstanceId");
            EnqueueEvent(new TowerPlaceRejectedEvent { ZoneId = place.ZoneId, RequesterId = place.Owner });
            return;
        }
        var zone = _gm.Map.BuildZones.FirstOrDefault(z => z.Id == place.ZoneId);
        if (zone == null)
        {
            Console.WriteLine($"[Owner] Client TowerPlace REJECTED: zone '{place.ZoneId}' not found");
            return;
        }
        if (zone.IsOccupied)
        {
            // If the zone is occupied by the same requester, this is a duplicate UDP packet
            // (same-machine testing often delivers one packet twice via broadcast + loopback).
            // Silently discard — the requester already got the TowerPlacedEvent confirmation.
            if (zone.OccupyingTower?.OwnerInstanceId == place.Owner)
            {
                Console.WriteLine($"[Owner] Client TowerPlace: zone '{place.ZoneId}' duplicate request from '{place.Owner}' (ignored)");
                return;
            }
            Console.WriteLine($"[Owner] Client TowerPlace REJECTED: zone '{place.ZoneId}' already occupied by another player (requester='{place.Owner}')");
            EnqueueEvent(new TowerPlaceRejectedEvent { ZoneId = place.ZoneId, RequesterId = place.Owner });
            return;
        }
        int balance = GetPlayerBalance(place.Owner);
        if (balance < place.Cost)
        {
            Console.WriteLine($"[Owner] Client TowerPlace REJECTED: not enough money ({balance} < {place.Cost})");
            EnqueueEvent(new TowerPlaceRejectedEvent { ZoneId = place.ZoneId, RequesterId = place.Owner });
            return;
        }

        ITowerBehavior behavior;

        if (_gm.TowerDefinitions.TryGetValue(place.BehaviorId, out var def))
        {
            behavior = TowerBehaviorFactory.CreateTowerBehavior(def);
        }
        else
        {
            behavior = TowerBehaviorFactory.CreateFromRegisteredName(place.BehaviorId);
            if (behavior == null) return;
            def = behavior.Definition;
        }

        var tower = new Tower(behavior, zone.Position, def)
        {
            OwnerInstanceId = place.Owner,
        };
        OwnershipDebug.Log($"Host ApplyClientTowerPlace: received request from owner='{place.Owner}'. Created tower with OwnerInstanceId='{tower.OwnerInstanceId}'");
        _gm.TowerController.AddTower(tower);
        SetPlayerBalance(place.Owner, balance - place.Cost);
        zone.Occupy(tower);

        EnqueueEvent(new TowerPlacedEvent
        {
            TowerId    = tower.NetworkId,
            ZoneId     = place.ZoneId,
            BehaviorId = place.BehaviorId,
            Owner      = place.Owner,
            Cost       = place.Cost,
        });
    }

    private void ApplyClientTowerRemove(TowerRemovedEvent remove)
    {
        RegisterPlayerId(remove.Owner);
        var tower = _gm.TowerController.GetByNetworkId(remove.TowerId);
        if (tower == null) return;

        if (!string.IsNullOrEmpty(tower.OwnerInstanceId) && !tower.IsOwnedBy(remove.Owner))
        {
            Console.WriteLine($"[GameSync] Remove rejected: tower owner={tower.OwnerInstanceId}, requester={remove.Owner}");
            return;
        }

        int refund = (int)((tower.Definition?.Cost ?? 0) * 0.5f);
        int balance = GetPlayerBalance(remove.Owner);
        SetPlayerBalance(remove.Owner, balance + refund);

        var zone = _gm.Map.BuildZones.FirstOrDefault(z => Vector2.Distance(z.Position, tower.Position) < 10f);
        zone?.Free();
        _gm.TowerController.RemoveTower(tower);

        EnqueueEvent(new TowerRemovedEvent { TowerId = remove.TowerId, ZoneId = remove.ZoneId, Refund = refund });
    }

    private void ApplyClientTowerUpgrade(TowerUpgradedEvent upg)
    {
        RegisterPlayerId(upg.Owner);
        var tower = _gm.TowerController.GetByNetworkId(upg.TowerId);
        if (tower == null) return;

        if (!string.IsNullOrEmpty(tower.OwnerInstanceId) && !tower.IsOwnedBy(upg.Owner))
        {
            Console.WriteLine($"[GameSync] Upgrade rejected: tower owner={tower.OwnerInstanceId}, requester={upg.Owner}");
            return;
        }

        if (!_gm.TowerDefinitions.TryGetValue(upg.BehaviorId, out var def)) return;
        int balance = GetPlayerBalance(upg.Owner);
        if (balance < upg.Cost) return;

        var behavior = TowerBehaviorFactory.CreateTowerBehavior(def);
        var upgraded = new Tower(behavior, tower.Position, def)
        {
            NetworkId       = upg.TowerId,
            OwnerInstanceId = tower.OwnerInstanceId,
            UpgradeLevel    = upg.Level,
        };
        upgraded.ApplyLevelStats();
        SetPlayerBalance(upg.Owner, balance - upg.Cost);

        _gm.TowerController.ReplaceTower(tower, upgraded);

        EnqueueEvent(upg);
    }

    // -----------------------------------------------------------------------
    // Client: receive state from master
    // -----------------------------------------------------------------------

    private async Task ReceiveStateLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _stateReceiver!.ReceiveAsync(ct);
                var delta  = FrameDeltaSerializer.Deserialize(result.Buffer);
                if (delta != null && delta.Seq > 0)
                {
                    _incomingDeltas.Enqueue(delta);
                    // Treat FrameDelta receipt as a Raft heartbeat from the leader
                    _raftNode?.NotifyMasterAlive();
                }
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException)    { break; }
            catch { }
        }
    }

    /// <summary>Client calls this each frame (from GameManager.Update) to apply buffered state.</summary>
    public void ApplyIncomingDeltas()
    {
        while (_incomingDeltas.TryDequeue(out var delta))
            ApplyDelta(delta);
    }

    private void ApplyDelta(FrameDelta delta)
    {
        _gm.UIManager.Lives = delta.Global.Lives;

        string localId = _gm.UIManager.LocalPlayerInstanceId;
        if (delta.Global.PlayerMoney != null && !string.IsNullOrEmpty(localId) &&
            delta.Global.PlayerMoney.TryGetValue(localId, out int myMoney))
        {
            _gm.UIManager.Money = myMoney;
        }
        else
        {
            _gm.UIManager.Money = delta.Global.Money;
        }

        var ec = _gm.EnemyController;
        if (ec != null)
        {
            foreach (var tick in delta.Enemies)
            {
                var enemy = ec.GetByNetworkId(tick.Id);
                if (enemy == null || !enemy.isAlive) continue;
                enemy.Position = new Vector2(tick.X, tick.Y);
                enemy.Health = tick.Hp;
            }
        }

        foreach (var evt in delta.Events)
        {
            try { ApplyEvent(evt); }
            catch (Exception ex) { Console.WriteLine($"[GameSync] ApplyEvent {evt?.GetType().Name} error: {ex.Message}"); }
        }
    }

    private void ApplyEvent(GameEvent evt)
    {
        switch (evt)
        {
            case EnemySpawnedEvent   spawn:  ClientSpawnEnemy(spawn);                               break;
            case EnemyKilledEvent    kill:   ClientRemoveEnemy(kill.EnemyId,  killed: true);        break;
            case EnemyReachedGoalEvent goal: ClientRemoveEnemy(goal.EnemyId,  killed: false);       break;
            case BulletSpawnedEvent  bSpawn: ClientSpawnBullet(bSpawn);                             break;
            case BulletImpactEvent   impact: ClientRemoveBullet(impact.BulletId);                   break;
            case TowerPlacedEvent         place:    ClientPlaceTower(place);                            break;
            case TowerPlaceRejectedEvent  rejected: ClientHandlePlaceRejected(rejected);               break;
            case TowerRemovedEvent        remove:   ClientRemoveTower(remove.TowerId);                 break;
            case TowerUpgradedEvent       upg:      ClientUpgradeTower(upg);                           break;
            case GameOverEvent            over:
                if (over.Reason == GameOverReason.DefenseFailed) _gm.TriggerDefeat();
                else                                              _gm.TriggerWin();
                break;
        }
    }

    private void ClientSpawnEnemy(EnemySpawnedEvent spawn)
    {
        var ec = _gm.EnemyController;
        if (ec == null || ec.GetByNetworkId(spawn.EnemyId) != null) return;

        var spawnPt = _gm.Map.SpawnPoints.FirstOrDefault(s => s.Id == spawn.SpawnPointId)
                   ?? _gm.Map.SpawnPoints.FirstOrDefault();
        var path    = _gm.Map.GetPathById(spawn.PathId)
                   ?? _gm.Map.Paths.FirstOrDefault();
        if (spawnPt == null || path == null) return;

        var enemyType = EnemyRegistry.create(spawn.TypeId);
        if (enemyType == null) return;

        var enemy = new Enemy(enemyType, spawnPt.Position, path)
        {
            NetworkId    = spawn.EnemyId,
            TypeId       = spawn.TypeId,
            SpawnPointId = spawn.SpawnPointId,
        };
        ec.AddEnemy(enemy);
    }

    private void ClientSpawnBullet(BulletSpawnedEvent spawn)
    {
        var dc = _gm.DamageDealerController;
        if (dc == null || dc.GetByNetworkId(spawn.BulletId) != null) return;

        Console.WriteLine($"[Bullet] CLIENT spawn: id={spawn.BulletId} hitRadius={spawn.HitRadius} speed={spawn.Speed} maxDist={spawn.MaxDist} dmg={spawn.Dmg} visualSize={spawn.HitRadius * 4f}px");
        var behavior = new StandardBulletBehavior(spawn.Dmg, spawn.Speed, spawn.MaxDist, spawn.HitRadius);
        var direction = new Microsoft.Xna.Framework.Vector2(spawn.Dx, spawn.Dy);
        var dealer = new DamageDealer(behavior, new Microsoft.Xna.Framework.Vector2(spawn.X, spawn.Y), direction, spawn.HitRadius)
        {
            NetworkId = spawn.BulletId,
        };
        dc.AddVisualBullet(dealer);
    }

    private void ClientRemoveBullet(int bulletId)
    {
        var dc = _gm.DamageDealerController;
        if (dc == null) return;
        var dealer = dc.GetByNetworkId(bulletId);
        if (dealer != null) dealer.IsActive = false;
    }

    private void ClientRemoveEnemy(int networkId, bool killed)
    {
        var ec    = _gm.EnemyController;
        var enemy = ec?.GetByNetworkId(networkId);
        if (enemy == null || ec == null) return;
        enemy.isAlive  = false;
        enemy.isKilled = killed;
        ec.RemoveEnemy(enemy);
    }

    private void ClientPlaceTower(TowerPlacedEvent place)
    {
        var tc = _gm.TowerController;
        if (tc.GetByNetworkId(place.TowerId) != null) return;

        var zone = _gm.Map.BuildZones.FirstOrDefault(z => z.Id == place.ZoneId);
        if (zone == null) { Console.WriteLine($"[GameSync] ClientPlaceTower: zone '{place.ZoneId}' not found"); return; }

        ITowerBehavior behavior;

        if (_gm.TowerDefinitions.TryGetValue(place.BehaviorId, out var def))
        {
            behavior = TowerBehaviorFactory.CreateTowerBehavior(def);
        }
        else
        {
            behavior = TowerBehaviorFactory.CreateFromRegisteredName(place.BehaviorId);
            if (behavior == null) { Console.WriteLine($"[GameSync] ClientPlaceTower: behavior '{place.BehaviorId}' not found"); return; }
            def = behavior.Definition;
        }

        var tower = new Tower(behavior, zone.Position, def)
        {
            NetworkId       = place.TowerId,
            OwnerInstanceId = place.Owner,
        };
        OwnershipDebug.Log($"Client ClientPlaceTower: received broadcast. NetworkId={place.TowerId} owner='{place.Owner}' LocalPlayerInstanceId='{_gm.UIManager.LocalPlayerInstanceId}'");
        tc.AddTower(tower);
        zone.Occupy(tower);
    }

    private void ClientHandlePlaceRejected(TowerPlaceRejectedEvent rejected)
    {
        Console.WriteLine($"[Owner] CLIENT received rejection: requesterId='{rejected.RequesterId}' myId='{_gm.UIManager.LocalPlayerInstanceId}' isForMe={rejected.RequesterId == _gm.UIManager.LocalPlayerInstanceId}");
        if (rejected.RequesterId != _gm.UIManager.LocalPlayerInstanceId) return;
        _gm.UIManager.HideTowerSelection();
        _gm.UIManager.ShowNotification("Зона уже занята другим игроком!", 3f);
    }

    private void ClientRemoveTower(int networkId)
    {
        var tc    = _gm.TowerController;
        var tower = tc.GetByNetworkId(networkId);
        if (tower == null) return;

        var zone = _gm.Map.BuildZones.FirstOrDefault(z => Vector2.Distance(z.Position, tower.Position) < 10f);
        zone?.Free();
        tc.RemoveTower(tower);
    }

    private void ClientUpgradeTower(TowerUpgradedEvent upg)
    {
        var tc    = _gm.TowerController;
        var tower = tc.GetByNetworkId(upg.TowerId);
        if (tower == null) return;
        if (!_gm.TowerDefinitions.TryGetValue(upg.BehaviorId, out var def)) return;

        var behavior = TowerBehaviorFactory.CreateTowerBehavior(def);
        var upgraded = new Tower(behavior, tower.Position, def)
        {
            NetworkId       = upg.TowerId,
            OwnerInstanceId = tower.OwnerInstanceId,
            UpgradeLevel    = upg.Level,
        };
        upgraded.ApplyLevelStats();

        tc.ReplaceTower(tower, upgraded);
    }

    // -----------------------------------------------------------------------
    // Client: send action requests to master
    // -----------------------------------------------------------------------

    public void RequestTowerPlace(string zoneId, string behaviorId, string owner, int cost, int tempId)
    {
        OwnershipDebug.Log($"Client RequestTowerPlace: sending request for zone='{zoneId}' owner='{owner}'");
        Console.WriteLine($"[Owner] CLIENT RequestTowerPlace: zone='{zoneId}' owner='{owner}' myId='{_gm.UIManager.LocalPlayerInstanceId}'");
        SendRequest(new TowerPlacedEvent { TowerId = tempId, ZoneId = zoneId, BehaviorId = behaviorId, Owner = owner, Cost = cost });
    }

    public void RequestTowerRemove(int towerId, string zoneId)
        => SendRequest(new TowerRemovedEvent { TowerId = towerId, ZoneId = zoneId, Owner = _gm.UIManager.LocalPlayerInstanceId });

    public void RequestTowerUpgrade(int towerId, string behaviorId, int prevLevel, int level, int cost)
        => SendRequest(new TowerUpgradedEvent { TowerId = towerId, BehaviorId = behaviorId, PrevLevel = prevLevel, Level = level, Cost = cost, Owner = _gm.UIManager.LocalPlayerInstanceId });

    private void SendRequest(GameEvent evt)
    {
        if (_requester == null || _hostEndpoint == null) return;
        var delta = new FrameDelta { Seq = RequestSeq, Events = [evt] };
        byte[] data = FrameDeltaSerializer.Serialize(delta);
        Console.WriteLine($"[Owner] CLIENT SendRequest JSON: {System.Text.Encoding.UTF8.GetString(data)}");
        try { _requester.Send(data, _hostEndpoint); } catch { }
    }

    // -----------------------------------------------------------------------
    // Raft event handlers
    // -----------------------------------------------------------------------

    private void HandleRaftBecameLeader()
    {
        Console.WriteLine("[GameSync] Raft: this node won election — promoting to host.");
        PromoteToHost();
    }

    private void HandleRaftNewLeaderKnown(string instanceId, string ip)
    {
        if (string.IsNullOrEmpty(ip)) return;

        Console.WriteLine($"[GameSync] Raft: new master is {instanceId} at {ip}.");

        // Update the endpoint we send client requests to
        _hostEndpoint = new IPEndPoint(IPAddress.Parse(ip), RequestPort);
        OnHostSwitched?.Invoke(ip);
    }

    private void HandleRaftNetworkLost()
    {
        Console.WriteLine("[GameSync] Raft: network lost — cannot reach any peer.");
        OnNetworkLost?.Invoke();
    }

    // -----------------------------------------------------------------------
    // PromoteToHost — client transitions to become the new game master
    // -----------------------------------------------------------------------

    private void PromoteToHost()
    {
        if (_raftPromotion) return;
        _raftPromotion = true;

        // Close client-only sockets
        _stateReceiver?.Dispose();
        _stateReceiver = null;
        // Keep _requester alive so existing requests are not dropped mid-flight

        // Open host sockets
        try
        {
            _broadcaster = new UdpClient { EnableBroadcast = true };

            _requestReceiver = new UdpClient();
            _requestReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _requestReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, RequestPort));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameSync] PromoteToHost: failed to open host sockets: {ex.Message}");
            return;
        }

        SubscribeToGameEvents();
        _ = Task.Run(() => ReceiveRequestsLoopAsync(_cts.Token), _cts.Token);

        // Flip the mode flag last, so BroadcastTick starts sending once sockets are ready
        _isHostMode = true;

        // Tell GameManager to stop applying deltas and start running the simulation
        _gm.PromoteToHost();

        Console.WriteLine("[GameSync] Promoted to game master.");
    }

    // -----------------------------------------------------------------------
    // Disposal
    // -----------------------------------------------------------------------

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
        _raftNode?.Dispose();
        _broadcaster?.Dispose();
        _requestReceiver?.Dispose();
        _stateReceiver?.Dispose();
        _requester?.Dispose();
    }
}
