import subprocess, time, sys, os

os.environ["PYTHONUNBUFFERED"] = "1"

if sys.platform == "win32":
    python_exe = os.path.join(".venv", "Scripts", "python.exe")
else:
    python_exe = os.path.join(".venv", "bin", "python")

def run():
    procs = []
    try:
        print("--- Weezer Net-Shaper TEST START ---")
        
        # 1. Proxy
        procs.append(subprocess.Popen([python_exe, "proxy.py", "--nodes", "1:127.0.0.1:6001:5001 2:127.0.0.1:6002:5002"]))
        time.sleep(2)
        
        # 2. Peer 2 (Binding first)
        procs.append(subprocess.Popen([python_exe, "mock_peer.py", "--id", "2", "--port", "6002", "--proxy-addr", "127.0.0.1:5002", "--target-id", "1"]))
        
        # 3. Peer 1 (Binding second)
        procs.append(subprocess.Popen([python_exe, "mock_peer.py", "--id", "1", "--port", "6001", "--proxy-addr", "127.0.0.1:5001", "--target-id", "2"]))

        print("Waiting for peers to stabilize...")
        while True: time.sleep(1)
    except KeyboardInterrupt:
        print("\nStopping...")
    finally:
        for p in procs: p.terminate()

if __name__ == "__main__":
    run()
