import asyncio, json, argparse, logging, random, socket, os, sys, time
try:
    import aioconsole
except ImportError:
    aioconsole = None
from typing import Dict, Tuple, List, Optional, Set

logging.basicConfig(level=logging.INFO, format='%(asctime)s [%(levelname)s] %(message)s', filename='proxy.log')
logger = logging.getLogger(__name__)

# Fallback for SIO_UDP_CONNRESET
SIO_UDP_CONNRESET = getattr(socket, 'SIO_UDP_CONNRESET', -1744830452)

class Node:
    def __init__(self, id: int, real_ip: str, real_port: int, proxy_port: int):
        self.id = id
        self.real_addr = (real_ip, real_port)
        self.proxy_port = proxy_port
        self.proxy_addr = ('0.0.0.0', proxy_port)

class Rules:
    def __init__(self, path):
        self.path = path
        self.defaults = {"latency": 0, "jitter": 0, "drop_rate": 0.0}
        self.nodes = {}
        self.partitions = []
        self.isolated = set()
        self.load()

    def load(self):
        if not os.path.exists(self.path): return
        try:
            with open(self.path, 'r') as f:
                c = json.load(f)
                self.defaults.update(c.get('defaults', {}))
                self.nodes = c.get('nodes', {})
                self.partitions = [set(map(int, p)) for p in c.get('partitions', [])]
                self.isolated = set(map(int, c.get('isolated', [])))
        except: pass

    def get(self, nid, key):
        return self.nodes.get(str(nid), {}).get(key, self.defaults.get(key, 0))

    def set_node(self, nid, key, value):
        self.nodes.setdefault(str(nid), {})[key] = value

    def get_node_rules(self, nid):
        return self.nodes.get(str(nid), {})

class TrafficStats:
    def __init__(self):
        self.recv = {}
        self.forwarded = {}
        self.dropped = {}
        self.broadcasts = {}
        self.last_seen = {}

    def seen(self, sid, tid):
        self.recv[sid] = self.recv.get(sid, 0) + 1
        self.last_seen[sid] = time.time()
        if tid == 0:
            self.broadcasts[sid] = self.broadcasts.get(sid, 0) + 1

    def sent(self, sid):
        self.forwarded[sid] = self.forwarded.get(sid, 0) + 1

    def drop(self, sid):
        self.dropped[sid] = self.dropped.get(sid, 0) + 1

    def guess_host(self, node_ids):
        active = [nid for nid in node_ids if self.recv.get(nid, 0) > 0]
        if not active:
            return None
        return max(active, key=lambda nid: (
            self.broadcasts.get(nid, 0),
            self.recv.get(nid, 0),
            self.forwarded.get(nid, 0),
            self.last_seen.get(nid, 0),
        ))

def read_header_id(header, key):
    value = header.get(key)
    if value is None:
        raise ValueError(f"missing header.{key}")
    return int(value)

class ProxyProtocol(asyncio.DatagramProtocol):
    def __init__(self, node, rt, rules, stats):
        self.node = node
        self.rt = rt
        self.rules = rules
        self.stats = stats
        self.transport = None

    def connection_made(self, transport):
        self.transport = transport
        logger.info(f"PROXY listening on port {self.node.proxy_port} (Node {self.node.id})")

    def error_received(self, exc): pass

    def datagram_received(self, data, addr):
        try:
            msg = json.loads(data.decode('utf-8'))
            hdr = msg.get('header', {})
            sid = read_header_id(hdr, 'from')
            tid = read_header_id(hdr, 'to')
            logger.info(f"PROXY RECV [{sid} -> {tid}] from {addr}")
            self.stats.seen(sid, tid)
            
            if tid == 0:
                for target in self.rt.values():
                    if target.id != sid: self.process(data, sid, target)
            elif tid in self.rt:
                self.process(data, sid, self.rt[tid])
        except Exception as e:
            logger.error(f"Proxy recv error: {e}")

    def process(self, data, sid, target):
        if self.is_blocked(sid, target.id):
            self.stats.drop(sid)
            return
        if random.random() < self.rules.get(sid, "drop_rate"):
            self.stats.drop(sid)
            return
        
        lat = self.rules.get(sid, "latency")
        jit = self.rules.get(sid, "jitter")
        delay_ms = max(0, lat + random.uniform(-jit, jit)) if jit > 0 else max(0, lat)
        delay = delay_ms / 1000.0
        
        if delay > 0:
            asyncio.get_event_loop().call_later(delay, self.send, data, sid, target)
        else:
            self.send(data, sid, target)

    def is_blocked(self, sid, tid):
        if sid in self.rules.isolated or tid in self.rules.isolated:
            return True

        sid_group = next((p for p in self.rules.partitions if sid in p), None)
        tid_group = next((p for p in self.rules.partitions if tid in p), None)
        if sid_group is None and tid_group is None:
            return False
        return sid_group != tid_group

    def send(self, data, sid, target):
        try:
            self.transport.sendto(data, target.real_addr)
            self.stats.sent(sid)
        except: pass

def make_socket(port):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    if sys.platform == 'win32':
        try: sock.ioctl(SIO_UDP_CONNRESET, 0)
        except Exception as e: logger.debug(f"SIO_UDP_CONNRESET failed: {e}")
    sock.bind(('0.0.0.0', port))
    return sock

def parse_node_id(raw, rt):
    try:
        nid = int(raw)
    except ValueError:
        raise ValueError(f"invalid node id: {raw}")
    if nid not in rt:
        raise ValueError(f"unknown node id: {nid}")
    return nid

def parse_chance(raw):
    try:
        value = float(raw)
    except ValueError:
        raise ValueError(f"invalid chance: {raw}")
    if value < 0.0 or value > 1.0:
        raise ValueError("chance must be between 0.0 and 1.0")
    return value

def parse_ms(raw):
    try:
        value = int(raw)
    except ValueError:
        raise ValueError(f"invalid milliseconds value: {raw}")
    if value < 0:
        raise ValueError("milliseconds value must be >= 0")
    return value

def parse_group(raw, rt):
    if not raw.strip():
        raise ValueError("empty group")
    return {parse_node_id(part.strip(), rt) for part in raw.split(',') if part.strip()}

def format_group(group):
    return ",".join(str(nid) for nid in sorted(group))

def print_help():
    print("Commands:")
    print("  status")
    print("  set_delay <id> <ms>")
    print("  drop <id> <chance>")
    print("  isolate <id>")
    print("  unisolate <id>")
    print("  split_groups 1,2 3,4")
    print("  sabotage_host")
    print("  exit")
    print("  help")

def print_status(rt, rules, stats, sabotage_task):
    print("\nRouting:")
    print("  id | real address       | proxy port | latency | jitter | drop | state")
    print("  ---+--------------------+------------+---------+--------+------+---------")
    for nid, node in sorted(rt.items()):
        state = "isolated" if nid in rules.isolated else "active"
        print(
            f"  {nid:<2} | {node.real_addr[0]}:{node.real_addr[1]:<10} | "
            f"{node.proxy_port:<10} | {rules.get(nid, 'latency'):<7} | "
            f"{rules.get(nid, 'jitter'):<6} | {rules.get(nid, 'drop_rate'):<4} | {state}"
        )

    partitions = "none" if not rules.partitions else " | ".join(format_group(g) for g in rules.partitions)
    host = stats.guess_host(rt.keys())
    host_text = "unknown" if host is None else str(host)
    sabotage = "running" if sabotage_task and not sabotage_task.done() else "idle"
    print(f"\nDefaults: {rules.defaults}")
    print(f"Partitions: {partitions}")
    print(f"Guessed host: {host_text}")
    print(f"Sabotage: {sabotage}")
    print("\nTraffic:")
    print("  id | recv | forwarded | dropped | broadcasts")
    print("  ---+------+-----------+---------+-----------")
    for nid in sorted(rt):
        print(
            f"  {nid:<2} | {stats.recv.get(nid, 0):<4} | "
            f"{stats.forwarded.get(nid, 0):<9} | {stats.dropped.get(nid, 0):<7} | "
            f"{stats.broadcasts.get(nid, 0)}"
        )
    print()

async def sabotage_host(rules, stats, rt):
    host = stats.guess_host(rt.keys())
    if host is None:
        print("Cannot infer host yet: no traffic has passed through the proxy.")
        return

    print(f"Sabotaging node {host}: increasing latency/drop every 5 seconds.")
    while True:
        current_latency = int(rules.get(host, "latency"))
        current_drop = float(rules.get(host, "drop_rate"))
        rules.set_node(host, "latency", min(current_latency + 100, 2000))
        rules.set_node(host, "drop_rate", min(current_drop + 0.05, 0.75))
        print(
            f"Host {host}: latency={rules.get(host, 'latency')}ms, "
            f"drop_rate={rules.get(host, 'drop_rate'):.2f}"
        )
        await asyncio.sleep(5)

async def cli_handler(rt, rules, stats):
    sabotage_task = None
    while True:
        try:
            if aioconsole:
                line = await aioconsole.ainput("proxy > ")
            else:
                line = await asyncio.to_thread(input, "proxy > ")
            parts = line.split()
            if not parts: continue
            cmd = parts[0]
            if cmd == "status":
                print_status(rt, rules, stats, sabotage_task)
            elif cmd == "set_delay":
                if len(parts) != 3:
                    print("Usage: set_delay <id> <ms>")
                    continue
                nid = parse_node_id(parts[1], rt)
                ms = parse_ms(parts[2])
                rules.set_node(nid, "latency", ms)
                print(f"Node {nid} latency set to {ms}ms.")
            elif cmd == "drop":
                if len(parts) != 3:
                    print("Usage: drop <id> <chance>")
                    continue
                nid = parse_node_id(parts[1], rt)
                chance = parse_chance(parts[2])
                rules.set_node(nid, "drop_rate", chance)
                print(f"Node {nid} drop_rate set to {chance:.2f}.")
            elif cmd == "isolate":
                if len(parts) != 2:
                    print("Usage: isolate <id>")
                    continue
                nid = parse_node_id(parts[1], rt)
                rules.isolated.add(nid)
                print(f"Node {nid} is isolated.")
            elif cmd == "unisolate":
                if len(parts) != 2:
                    print("Usage: unisolate <id>")
                    continue
                nid = parse_node_id(parts[1], rt)
                rules.isolated.discard(nid)
                print(f"Node {nid} is unisolated.")
            elif cmd == "split_groups":
                if len(parts) < 2:
                    print("Usage: split_groups 1,2 3,4")
                    continue
                groups = [parse_group(raw, rt) for raw in parts[1:]]
                seen = set()
                duplicates = set()
                for group in groups:
                    duplicates.update(seen.intersection(group))
                    seen.update(group)
                if duplicates:
                    print(f"Nodes cannot appear in multiple groups: {format_group(duplicates)}")
                    continue
                rules.partitions = groups
                print(f"Partitions set: {' | '.join(format_group(g) for g in groups)}")
            elif cmd == "sabotage_host":
                if len(parts) != 1:
                    print("Usage: sabotage_host")
                    continue
                if sabotage_task and not sabotage_task.done():
                    sabotage_task.cancel()
                    print("Previous host sabotage stopped.")
                sabotage_task = asyncio.create_task(sabotage_host(rules, stats, rt))
            elif cmd == "exit":
                os._exit(0)
            elif cmd == "help":
                print_help()
            else:
                print(f"Unknown command: {cmd}. Type help for commands.")
        except ValueError as e:
            print(f"Error: {e}")
        except EOFError:
            logger.info("CLI input closed; proxy will keep running without interactive commands.")
            await asyncio.Future()
        except asyncio.CancelledError:
            raise
        except Exception as e:
            print(f"Command error: {e}")

async def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--nodes', required=True)
    parser.add_argument('--rules', default='rules.json')
    args = parser.parse_args()

    rt = {}
    for c in args.nodes.split():
        nid, rip, rp, pp = c.split(':')
        rt[int(nid)] = Node(int(nid), rip, int(rp), int(pp))

    rules = Rules(args.rules)
    stats = TrafficStats()
    loop = asyncio.get_running_loop()
    
    for n in rt.values():
        sock = make_socket(n.proxy_port)
        await loop.create_datagram_endpoint(lambda node=n: ProxyProtocol(node, rt, rules, stats), sock=sock)

    logger.info("Proxy server is READY.")
    await cli_handler(rt, rules, stats)

if __name__ == '__main__':
    asyncio.run(main())
