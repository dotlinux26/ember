param([string]$Broker='127.0.0.1',[int]$Seconds=15,[int]$Cycles=2,[int]$Loss=20)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $root
New-Item -ItemType Directory -Path Builds -Force | Out-Null
foreach($mode in @('auto','direct','relay')) {
    $log=Join-Path $root "Builds/network-$mode.jsonl"
    & (Join-Path $root 'tools/dotnet10/dotnet.exe') run --project services/Ember.NetProbe -- $Broker "--mode=$mode" "--seconds=$Seconds" "--cycles=$Cycles" "--loss=$Loss" | Out-File -LiteralPath $log -Encoding utf8
    if($LASTEXITCODE -ne 0){Get-Content -LiteralPath $log -Tail 10;throw "Network probe failed: $mode"}
    Get-Content -LiteralPath $log | Select-String 'probe.pass|probe.complete'
}
