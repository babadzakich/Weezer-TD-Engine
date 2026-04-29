import asyncio, json, argparse, socket, logging, sys

logging.basicConfig(level=logging.INFO, format='%(asctime)s [%(levelname)s] %(message)s', stream=sys.stdout)
logger = logging.getLogger(__name__)

SIO_UDP_CONNRESET = getattr(socket, 'SIO_UDP_CONNRESET', -1744830452)

class MockPeerProtocol(asyncio.DatagramProtocol):
    def __init__(self, pid):
        self.pid = pid

    def connection_made(self, transport):
        logger.info(f"PEER {self.pid} BIND SUCCESS")

    def error_received(self, exc): pass

    def datagram_received(self, data, addr):
        logger.info(f"PEER {self.pid} RECV from {addr}")
        try:
            msg = json.loads(data.decode('utf-8'))
            logger.info(f"PEER {self.pid} MESSAGE: {msg['header']['from']} -> {msg['header']['to']}")
        except: pass

async def send_loop(transport, p_addr, sid, tid):
    logger.info(f"PEER {sid} STARTING SEND LOOP")
    await asyncio.sleep(2.0)
    while True:
        pkt = {"header": {"from": sid, "to": tid, "type": "ACTION", "session_id": "test", "is_verified": False}, "payload": {}}
        transport.sendto(json.dumps(pkt).encode('utf-8'), p_addr)
        logger.info(f"PEER {sid} SENT TO PROXY")
        await asyncio.sleep(2.0)

def make_socket(port):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    if sys.platform == 'win32':
        try: sock.ioctl(SIO_UDP_CONNRESET, 0)
        except: pass
    sock.bind(('0.0.0.0', port))
    return sock

async def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--id', type=int, required=True)
    parser.add_argument('--port', type=int, required=True)
    parser.add_argument('--proxy-addr', required=True)
    parser.add_argument('--target-id', type=int, required=True)
    args = parser.parse_args()

    loop = asyncio.get_running_loop()
    sock = make_socket(args.port)
    transport, _ = await loop.create_datagram_endpoint(lambda: MockPeerProtocol(args.id), sock=sock)
    
    ip, port = args.proxy_addr.split(':')
    await send_loop(transport, (ip, int(port)), args.id, args.target_id)

if __name__ == '__main__':
    asyncio.run(main())
