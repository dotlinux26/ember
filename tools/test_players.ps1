param([string]$Broker='127.0.0.1')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$exe=Join-Path $root 'Builds/Windows/Ember.exe'
if(!(Test-Path -LiteralPath $exe)){throw 'Build the Windows player first.'}
$failures=@()
foreach($scenario in @('normal','35','65','95')) {
    $log=Join-Path $root "Builds/player-$scenario.log"
    $process=Start-Process -FilePath $exe -ArgumentList "--ember-smoke --ember-scenario=$scenario -screen-width 1280 -screen-height 800 -screen-fullscreen 0 -logFile $log" -WindowStyle Hidden -PassThru
    if(!$process.WaitForExit(25000)){Stop-Process -Id $process.Id;throw "Solo timed out: $scenario"}
    if(!(Select-String -LiteralPath $log -SimpleMatch 'EMBER_SMOKE PASS')){$failures+="solo-$scenario"}
    if(Select-String -LiteralPath $log -Pattern 'NullReferenceException|IndexOutOfRangeException|Shader error|error CS'){$failures+="exception-$scenario"}
    Write-Output "Checked built player: $scenario"
}
foreach($case in @(@{name='lan1';n=1;mode='auto';internet=$false},@{name='lan2';n=2;mode='auto';internet=$false},@{name='lan4';n=4;mode='auto';internet=$false},@{name='p2p4';n=4;mode='direct';internet=$true},@{name='relay4';n=4;mode='relay';internet=$true})) {
    $code='T'+[Guid]::NewGuid().ToString('N').Substring(0,5).ToUpperInvariant()
    $common="--ember-players=$($case.n) --ember-route=$($case.mode) -screen-width 1280 -screen-height 800 -screen-fullscreen 0"
    if($case.internet){$common+=" --ember-broker=$Broker --ember-room=$code"}
    $processes=@();$logs=@()
    try {
        for($i=0;$i -lt $case.n;$i++) {
            $role=if($i -eq 0){'host'}else{'client'}
            $log=Join-Path $root "Builds/player-$($case.name)-$i.log";$logs+=$log
            $processes+=Start-Process -FilePath $exe -ArgumentList "$common --ember-net-$role -logFile $log" -WindowStyle Hidden -PassThru
            if($i -eq 0){Start-Sleep -Milliseconds 700}
        }
        foreach($process in $processes){if(!$process.WaitForExit(55000)){Stop-Process -Id $process.Id;$failures+="timeout-$($case.name)"}}
        foreach($log in $logs){if(!(Select-String -LiteralPath $log -SimpleMatch 'EMBER_NET_SMOKE PASS')){$failures+=$log};if(Select-String -LiteralPath $log -Pattern 'NullReferenceException|IndexOutOfRangeException|Shader error'){$failures+="exception-$log"}}
        Write-Output "Checked built network players: $($case.name)"
    } finally {foreach($process in $processes){if(!$process.HasExited){Stop-Process -Id $process.Id}}}
}
if($failures.Count){throw ('Player checks failed: '+($failures -join ', '))}
Write-Output 'ALL BUILT-PLAYER CHECKS PASSED'
