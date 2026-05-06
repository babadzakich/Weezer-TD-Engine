#!/bin/bash

# Скрипт для запуска прокси сервера (3 узла)
# Формат nodes: id:real_ip:real_port:proxy_port

echo "Запуск Proxy (узлы 1, 2, 3)..."
echo "Node 1: real 8001, proxy 9001"
echo "Node 2: real 8002, proxy 9002"
echo "Node 3: real 8003, proxy 9003"
echo "-----------------------------------"

python3 proxy.py --nodes "1:127.0.0.1:8001:9001 2:127.0.0.1:8002:9002 3:127.0.0.1:8003:9003"
