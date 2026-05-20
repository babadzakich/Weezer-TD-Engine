using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using SimulationEngine.TowerRelated;
using SimulationEngine.TowerRelated.Behaviors;

namespace SimulationEngine.Network;

public interface IGameNetworkSession : IGameRequestSender
{
    void Update(GameTime gameTime);
}

public sealed class ClientNetworkSession : IGameNetworkSession, IAsyncDisposable
{
    private readonly GameManager _gameManager;
    private readonly UdpGameTransport _transport;
    private readonly string _masterPeerId;
    private readonly ConcurrentQueue<GamePacket> _incomingPackets = new();
    private readonly CancellationTokenSource _cts = new();

    public ClientNetworkSession(GameManager gameManager, UdpGameTransport transport, string masterPeerId)
    {
        _gameManager = gameManager ?? throw new ArgumentNullException(nameof(gameManager));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _masterPeerId = masterPeerId ?? throw new ArgumentNullException(nameof(masterPeerId));

        _transport.MessageReceived += OnTransportMessageReceived;
    }

    public Task SendRequestAsync(ClientRequest request, CancellationToken ct)
    {
        return _transport.SendClientRequestAsync(_masterPeerId, request, ct);
    }

    public void Update(GameTime gameTime)
    {
        while (_incomingPackets.TryDequeue(out var packet))
        {
            ProcessPacket(packet);
        }
    }

    private void OnTransportMessageReceived(GamePacket packet)
    {
        if (packet.Kind == GamePacketKind.ClientRequest)
        {
            return;
        }

        _incomingPackets.Enqueue(packet);
    }

    private void ProcessPacket(GamePacket packet)
    {
        if (packet.PeerId != _masterPeerId)
        {
            return;
        }

        switch (packet.Kind)
        {
            case GamePacketKind.FrameDelta:
                FrameDeltaStateSync.ApplyFrameDelta(_gameManager, packet.Delta);
                break;
            case GamePacketKind.StateSnapshot:
                FrameDeltaStateSync.ApplySnapshot(_gameManager, packet.Snapshot);
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _transport.MessageReceived -= OnTransportMessageReceived;
        _cts.Cancel();
        _cts.Dispose();
        await Task.CompletedTask;
    }
}

public sealed class MasterNetworkSession : IGameNetworkSession, IAsyncDisposable
{
    private readonly GameManager _gameManager;
    private readonly UdpGameTransport _transport;
    private readonly ConcurrentQueue<GamePacket> _incomingPackets = new();
    private readonly ConcurrentQueue<GameEvent> _pendingEvents = new();
    private readonly CancellationTokenSource _cts = new();
    private long _nextSeq;

    public MasterNetworkSession(GameManager gameManager, UdpGameTransport transport)
    {
        _gameManager = gameManager ?? throw new ArgumentNullException(nameof(gameManager));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));

        _transport.MessageReceived += OnTransportMessageReceived;
    }

    public Task SendRequestAsync(ClientRequest request, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public void Update(GameTime gameTime)
    {
        ProcessIncomingPackets();

        if (!_transport.HasPeers)
        {
            return;
        }

        var events = DequeuePendingEvents();
        var delta = FrameDeltaStateSync.BuildFrameDelta(_gameManager, events, Interlocked.Increment(ref _nextSeq));
        _ = _transport.BroadcastFrameDeltaAsync(delta, CancellationToken.None);
    }

    public Task SendSnapshotToPeerAsync(string peerId, CancellationToken ct)
    {
        var snapshot = FrameDeltaStateSync.BuildSnapshot(_gameManager, Interlocked.Read(ref _nextSeq));
        return _transport.SendSnapshotAsync(peerId, snapshot, ct);
    }

    private void OnTransportMessageReceived(GamePacket packet)
    {
        if (packet.Kind != GamePacketKind.ClientRequest)
        {
            return;
        }

        _incomingPackets.Enqueue(packet);
    }

    private void ProcessIncomingPackets()
    {
        while (_incomingPackets.TryDequeue(out var packet))
        {
            ProcessClientRequest(packet.Request, packet.PeerId);
        }
    }

    private void ProcessClientRequest(ClientRequest request, string peerId)
    {
        switch (request)
        {
            case BuildTowerRequest build:
                TryBuildTower(build);
                break;
            case SellTowerRequest sell:
                TrySellTower(sell);
                break;
            case UpgradeTowerRequest upgrade:
                TryUpgradeTower(upgrade);
                break;
            case StartWaveRequest start:
                TryStartWave(start);
                break;
        }
    }

    private bool TryBuildTower(BuildTowerRequest request)
    {
        var zone = _gameManager.Map.BuildZones.Find(z => z.Id == request.ZoneId);
        if (zone == null || zone.IsOccupied)
        {
            return false;
        }

        if (!_gameManager.TowerDefinitions.TryGetValue(request.TowerDefinitionId, out var definition))
        {
            return false;
        }

        var behavior = TowerBehaviorFactory.CreateTowerBehavior(definition);
        if (!_gameManager.UIManager.PurchaseTower(behavior))
        {
            return false;
        }

        var tower = new Tower(behavior, zone.Position, definition)
        {
            ZoneId = zone.Id,
            Texture = _gameManager.DefaultTowerTexture
        };

        _gameManager.TowerController.AddTower(tower);
        zone.Occupy(tower);

        EnqueueEvent(new TowerPlacedEvent
        {
            TowerId = tower.Id,
            ZoneId = zone.Id,
            BehaviorId = definition.Id,
            Owner = request.RequesterId ?? "client",
            Cost = definition.Cost
        });

        return true;
    }

    private bool TrySellTower(SellTowerRequest request)
    {
        var tower = _gameManager.TowerController.towers.Find(t => t.Id == request.TowerId);
        if (tower == null)
        {
            return false;
        }

        _gameManager.UIManager.SellTower(tower);
        var zone = _gameManager.Map.BuildZones.Find(z => z.Id == tower.ZoneId);
        zone?.Free();
        _gameManager.TowerController.towers.Remove(tower);

        EnqueueEvent(new TowerRemovedEvent
        {
            TowerId = tower.Id,
            ZoneId = tower.ZoneId,
            Refund = (int)(tower.Behavior.Cost * 0.7f)
        });

        return true;
    }

    private bool TryUpgradeTower(UpgradeTowerRequest request)
    {
        var tower = _gameManager.TowerController.towers.Find(t => t.Id == request.TowerId);
        if (tower == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.TargetTowerId))
        {
            return false;
        }

        if (!_gameManager.TowerDefinitions.TryGetValue(request.TargetTowerId, out var targetDefinition))
        {
            return false;
        }

        var upgradedBehavior = TowerBehaviorFactory.CreateTowerBehavior(targetDefinition);
        if (!_gameManager.UIManager.PurchaseTower(upgradedBehavior))
        {
            return false;
        }

        var upgradedTower = new Tower(upgradedBehavior, tower.Position, targetDefinition)
        {
            Id = tower.Id,
            ZoneId = tower.ZoneId,
            Texture = tower.Texture,
            UpgradeLevel = tower.UpgradeLevel + 1
        };
        upgradedTower.ApplyLevelStats();

        int index = _gameManager.TowerController.towers.IndexOf(tower);
        if (index >= 0)
        {
            _gameManager.TowerController.towers[index] = upgradedTower;
        }
        else
        {
            _gameManager.TowerController.AddTower(upgradedTower);
        }

        EnqueueEvent(new TowerUpgradedEvent
        {
            TowerId = tower.Id,
            BehaviorId = request.TargetTowerId,
            PrevLevel = tower.UpgradeLevel,
            Level = upgradedTower.UpgradeLevel,
            Cost = upgradedBehavior.Cost
        });

        return true;
    }

    private bool TryStartWave(StartWaveRequest request)
    {
        if (_gameManager.WaveController == null || _gameManager.WaveController.IsWaveActive)
        {
            return false;
        }

        if (_gameManager.WaveController.CurrentWaveIndex >= _gameManager.WaveController.TotalWaves)
        {
            return false;
        }

        _gameManager.WaveController.StartNextWave();
        EnqueueEvent(new WaveStartedEvent
        {
            WaveIdx = _gameManager.WaveController.CurrentWaveIndex,
            TotalMobs = 0,
            SpawnIntervalMs = 0
        });

        return true;
    }

    private void EnqueueEvent(GameEvent gameEvent)
    {
        if (gameEvent == null) return;
        _pendingEvents.Enqueue(gameEvent);
    }

    private List<GameEvent> DequeuePendingEvents()
    {
        var events = new List<GameEvent>();
        while (_pendingEvents.TryDequeue(out var gameEvent))
        {
            events.Add(gameEvent);
        }
        return events;
    }

    public async ValueTask DisposeAsync()
    {
        _transport.MessageReceived -= OnTransportMessageReceived;
        _cts.Cancel();
        _cts.Dispose();
        await Task.CompletedTask;
    }
}
