# Запуск ОДНОГО экземпляра игры через прокси (Windows / PowerShell).
#
# Usage:   .\run_game_node.ps1 <node_id> <proxy_ip>
# Пример:  .\run_game_node.ps1 2 192.168.1.93
#
# node_id  — номер этого игрока (1, 2, 3...), как в --nodes у прокси.
# proxy_ip — IP машины, где запущен прокси.
# Порты выводятся из node_id: send = 5000+id, bind = 6000+id.
param(
    [Parameter(Mandatory = $true)][int]$NodeId,
    [Parameter(Mandatory = $true)][string]$ProxyIp
)

$sendPort = 5000 + $NodeId
$bindPort = 6000 + $NodeId

Write-Host "Node $NodeId -> proxy ${ProxyIp}:$sendPort, bind $bindPort"
Write-Host "Открой входящий UDP $bindPort + 47777 (лобби) в брандмауэре Windows."

$env:WTD_PROXY      = "1"
$env:WTD_PROXY_ID   = "$NodeId"
$env:WTD_PROXY_SEND = "${ProxyIp}:$sendPort"
$env:WTD_PROXY_BIND = "$bindPort"

$repo = Split-Path -Parent $PSScriptRoot
dotnet run --project (Join-Path $repo "Weezer-Tower-Defence\Weezer-Tower-Defence-Game.csproj")
