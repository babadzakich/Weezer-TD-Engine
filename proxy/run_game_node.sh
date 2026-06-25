#!/bin/bash
# Запуск ОДНОГО экземпляра игры через прокси (Linux/macOS).
#
# Usage:  ./run_game_node.sh <node_id> <proxy_ip>
# Пример: ./run_game_node.sh 1 192.168.1.93
#
# node_id  — номер этого игрока (1, 2, 3...), должен совпадать с порядком в --nodes у прокси.
# proxy_ip — IP машины, где запущен прокси.
# Порты выводятся из node_id: send = 5000+id, bind = 6000+id.
set -e

if [ $# -ne 2 ]; then
    echo "Usage:  $0 <node_id> <proxy_ip>"
    echo "Example: $0 1 192.168.1.93"
    exit 1
fi

NODE_ID="$1"
PROXY_IP="$2"
SEND_PORT=$((5000 + NODE_ID))
BIND_PORT=$((6000 + NODE_ID))

cd "$(dirname "$0")/.."

echo "Node $NODE_ID -> proxy $PROXY_IP:$SEND_PORT, bind $BIND_PORT"
echo "Открой входящий UDP $BIND_PORT + 47777 (лобби) в файерволе этой машины."

exec env WTD_PROXY=1 WTD_PROXY_ID="$NODE_ID" \
         WTD_PROXY_SEND="$PROXY_IP:$SEND_PORT" \
         WTD_PROXY_BIND="$BIND_PORT" \
         dotnet run --project Weezer-Tower-Defence/Weezer-Tower-Defence-Game.csproj
