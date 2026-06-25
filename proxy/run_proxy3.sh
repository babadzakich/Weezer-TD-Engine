#!/bin/bash
# Запуск chaos-прокси для ТРЁХ игроков на разных машинах.
#
# Usage:  ./run_proxy3.sh <IP_узла1> <IP_узла2> <IP_узла3>
# Пример: ./run_proxy3.sh 192.168.1.93 192.168.1.54 192.168.1.77
#
# IP — реальные LAN-адреса машин игроков (узнать: `ip -4 addr` / `ipconfig`).
# Туда прокси ПЕРЕСЫЛАЕТ пакеты. Слушает он на этой машине порты 5001/5002/5003.
# Схема портов: узел i → real 600i, proxy 500i.
set -e

if [ $# -ne 3 ]; then
    echo "Usage:  $0 <ip_node1> <ip_node2> <ip_node3>"
    echo "Example: $0 192.168.1.93 192.168.1.54 192.168.1.77"
    exit 1
fi

cd "$(dirname "$0")"

NODES="1:$1:6001:5001 2:$2:6002:5002 3:$3:6003:5003"

# Берём python из repo .venv, если он есть (там стоит aioconsole для CLI).
PY=python3
[ -x "../.venv/bin/python" ] && PY="../.venv/bin/python"

echo "=== Weezer-TD chaos proxy (3 nodes) ==="
echo "nodes: $NODES"
echo "На ЭТОЙ машине открой входящие UDP 5001-5003 в файерволе."
echo "На каждой машине игрока: входящий UDP его bind-порта (6001/6002/6003) + 47777 (лобби)."
echo "======================================="

exec "$PY" proxy.py --nodes "$NODES"
