#!/bin/bash
# Routes two REAL Weezer-TD game instances through the chaos proxy for testing.
#
# Node layout (matches the env vars below):
#   node 1: binds 6001, sends to proxy port 5001
#   node 2: binds 6002, sends to proxy port 5002
#
# Usage:
#   1) Run this script — it starts the proxy and keeps the CLI open.
#   2) In TWO other terminals, launch the game with the matching env vars.
#      The `env VAR=val ... cmd` form works in both bash and fish:
#
#   env WTD_PROXY=1 WTD_PROXY_ID=1 WTD_PROXY_SEND=127.0.0.1:5001 WTD_PROXY_BIND=6001 \
#       dotnet run --project ../Weezer-Tower-Defence/Weezer-Tower-Defence-Game.csproj
#
#   env WTD_PROXY=1 WTD_PROXY_ID=2 WTD_PROXY_SEND=127.0.0.1:5002 WTD_PROXY_BIND=6002 \
#       dotnet run --project ../Weezer-Tower-Defence/Weezer-Tower-Defence-Game.csproj
#
# Both instances run on this machine: they still form a lobby via normal loopback
# discovery (UDP 47777), then ALL in-game state/request/raft traffic flows through the
# proxy. Drive chaos from the `proxy > ` prompt: set_delay, drop, isolate,
# split_groups, sabotage_host, status.

cd "$(dirname "$0")"

cat <<'EOF'
=== Weezer-TD via chaos proxy ===
Launch the two game instances in separate terminals:

  env WTD_PROXY=1 WTD_PROXY_ID=1 WTD_PROXY_SEND=127.0.0.1:5001 WTD_PROXY_BIND=6001 \
      dotnet run --project ../Weezer-Tower-Defence/Weezer-Tower-Defence-Game.csproj

  env WTD_PROXY=1 WTD_PROXY_ID=2 WTD_PROXY_SEND=127.0.0.1:5002 WTD_PROXY_BIND=6002 \
      dotnet run --project ../Weezer-Tower-Defence/Weezer-Tower-Defence-Game.csproj

Then host a lobby on node 1, join from node 2, ready up and start.
=================================
EOF

python3 proxy.py --nodes "1:127.0.0.1:6001:5001 2:127.0.0.1:6002:5002"
