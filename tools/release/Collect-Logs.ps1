$ErrorActionPreference='Stop'
$destination=Join-Path $PSScriptRoot ('Ember-Logs-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.zip')
$paths=@()
$bundleLogs=Join-Path $PSScriptRoot 'Logs'
if(Test-Path -LiteralPath $bundleLogs){$paths+=Get-ChildItem -LiteralPath $bundleLogs -File | Where-Object { $_.Extension -in @('.log','.jsonl') } | Select-Object -ExpandProperty FullName}
$parent=Join-Path $env:USERPROFILE 'AppData/LocalLow/Ember'
if(Test-Path -LiteralPath $parent){$paths+=Get-ChildItem -LiteralPath $parent -Directory | ForEach-Object { $logs=Join-Path $_.FullName 'Logs';if(Test-Path -LiteralPath $logs){Get-ChildItem -LiteralPath $logs -File -Filter 'ember_*.jsonl*' | Select-Object -ExpandProperty FullName} }}
if(!$paths.Count){throw 'No Ember logs found.'}
# Stage by unique filename; never include profile.ember, passwords or backup vaults.
$stage=Join-Path $env:TEMP ('Ember-Logs-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
$i=0;foreach($path in $paths){Copy-Item -LiteralPath $path -Destination (Join-Path $stage (($i++).ToString('000')+'-'+[IO.Path]::GetFileName($path)))}
Compress-Archive -LiteralPath $stage -DestinationPath $destination -CompressionLevel Optimal
Write-Output $destination
