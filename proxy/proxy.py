import asyncio, json, argparse, logging, random, socket, os, sys
import aioconsole
from typing import Dict, Tuple, List, Optional, Set

logging.basicConfig(level=logging.INFO, format='%(asctime)s [%(levelname)s] %(message)s', stream=sys.stdout)
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
        self.load()

    def load(self):
        if not os.path.exists(self.path): return
        try:
            with open(self.path, 'r') as f:
                c = json.load(f)
                self.defaults.update(c.get('defaults', {}))
                self.nodes = c.get('nodes', {})
        except: pass

    def get(self, nid, key):
        return self.nodes.get(str(nid), {}).get(key, self.defaults.get(key, 0))

class ProxyProtocol(asyncio.DatagramProtocol):
    def __init__(self, node, rt, rules):
        self.node = node
        self.rt = rt
        self.rules = rules
        self.transport = None

    def connection_made(self, transport):
        self.transport = transport
        logger.info(f"PROXY listening on port {self.node.proxy_port} (Node {self.node.id})")

    def error_received(self, exc): pass

    def datagram_received(self, data, addr):
        try:
            msg = json.loads(data.decode('utf-8'))
            hdr = msg.get('header', {})
            sid, tid = hdr.get('from'), hdr.get('to')
            logger.info(f"PROXY RECV [{sid} -> {tid}] from {addr}")
            
            if tid == 0:
                for target in self.rt.values():
                    if target.id != sid: self.process(data, sid, target)
            elif tid in self.rt:
                self.process(data, sid, self.rt[tid])
        except Exception as e:
            logger.error(f"Proxy recv error: {e}")

    def process(self, data, sid, target):
        if any(sid in p and target.id not in p for p in self.rules.partitions): return
        if random.random() < self.rules.get(sid, "drop_rate"): return
        
        lat = self.rules.get(sid, "latency")
        jit = self.rules.get(sid, "jitter")
        delay = (lat + random.uniform(-jit, jit)) / 1000.0 if jit > 0 else lat / 1000.0
        
        if delay > 0:
            asyncio.get_event_loop().call_later(delay, self.send, data, target)
        else:
            self.send(data, target)

    def send(self, data, target):
        try:
            self.transport.sendto(data, target.real_addr)
        except: pass

def make_socket(port):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    if sys.platform == 'win32':
        try: sock.ioctl(SIO_UDP_CONNRESET, 0)
        except Exception as e: logger.debug(f"SIO_UDP_CONNRESET failed: {e}")
    sock.bind(('0.0.0.0', port))
    return sock

async def cli_handler(rules):
    while True:
        try:
            line = await aioconsole.ainput("proxy > ")
            parts = line.split()
            if not parts: continue
            cmd = parts[0]
            if cmd == "status":
                print(f"Rules: {rules.defaults}")
            elif cmd == "exit":
                os._exit(0)
            elif cmd == "help":
                print("Commands: status, exit, help")
        except: pass

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
    loop = asyncio.get_running_loop()
    
    for n in rt.values():
        sock = make_socket(n.proxy_port)
        await loop.create_datagram_endpoint(lambda node=n: ProxyProtocol(node, rt, rules), sock=sock)

    logger.info("Proxy server is READY.")
    await cli_handler(rules)

if __name__ == '__main__':
    asyncio.run(main())
