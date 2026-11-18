param([switch]$BuildGame)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $root
$sdk=Join-Path $root 'tools/dotnet10/dotnet.exe'
if(!(Test-Path -LiteralPath $sdk)){throw '.NET 10 SDK required at tools/dotnet10, or edit $sdk to your supported SDK path.'}
if($BuildGame){
    $unity='D:/UnityHub/Editor/6000.6.5f1/Editor/Unity.exe'
    $buildLog=Join-Path $root 'Builds/unity-release-package.log'
    $buildProcess=Start-Process -FilePath $unity -ArgumentList "-batchmode -nographics -quit -projectPath `"$root/My project`" -executeMethod Ember.Editor.EmberBuild.Windows -logFile `"$buildLog`"" -WindowStyle Hidden -PassThru -Wait
    if($buildProcess.ExitCode -ne 0 -or !(Select-String -LiteralPath $buildLog -SimpleMatch 'EMBER_BUILD_PASS')){throw 'Unity build did not pass.'}
}
foreach($rid in @('linux-x64','linux-arm64')){
    & $sdk publish services/Ember.RoomBroker -c Release -r $rid --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:InvariantGlobalization=true -o "dist/EmberVps/$rid"
    if($LASTEXITCODE -ne 0){throw "Publish failed: $rid"}
}
& $sdk publish services/Ember.NetProbe -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -o dist/EmberVps/probe-windows
if($LASTEXITCODE -ne 0){throw 'Probe publish failed.'}
$docker=New-Item -ItemType Directory -Path dist/EmberVps/docker -Force
foreach($name in @('Dockerfile','Program.cs','Ember.RoomBroker.csproj')){Copy-Item -LiteralPath "services/Ember.RoomBroker/$name" -Destination $docker.FullName -Force}
$win=Join-Path $root 'dist/EmberWindows';New-Item -ItemType Directory -Path $win -Force | Out-Null
foreach($item in Get-ChildItem -LiteralPath Builds/Windows){
    if($item.Name -in @('D3D12','Ember_BackUpThisFolder_ButDontShipItWithYourGame')){continue}
    Copy-Item -LiteralPath $item.FullName -Destination $win -Recurse -Force
}
New-Item -ItemType Directory -Path "$win/Diagnostics" -Force | Out-Null
Copy-Item -LiteralPath dist/EmberVps/probe-windows/Ember.NetProbe.exe -Destination "$win/Diagnostics/Ember.NetProbe.exe" -Force
foreach($name in @('Launch-Game.cmd','Launch-Test.cmd','Test-VPS.cmd','Collect-Logs.ps1')){Copy-Item -LiteralPath "tools/release/$name" -Destination $win -Force}
foreach($folder in @('EmberVps','EmberWindows')){
    New-Item -ItemType Directory -Path "dist/$folder/licenses" -Force | Out-Null
    Copy-Item -LiteralPath tools/dotnet10/LICENSE.txt -Destination "dist/$folder/licenses/dotnet-LICENSE.txt" -Force
    Copy-Item -LiteralPath tools/dotnet10/ThirdPartyNotices.txt -Destination "dist/$folder/licenses/dotnet-ThirdPartyNotices.txt" -Force
}
foreach($folder in @('EmberVps','EmberWindows')){
    Copy-Item -LiteralPath docs/HUONG_DAN_TEST_VI_EN.md -Destination "dist/$folder/HUONG_DAN_TEST_VI_EN.md" -Force
    Copy-Item -LiteralPath docs/BALANCE_TUNING.md -Destination "dist/$folder/BALANCE_TUNING.md" -Force
    if(Test-Path -LiteralPath docs/RELEASE_VERIFICATION.json){Copy-Item -LiteralPath docs/RELEASE_VERIFICATION.json -Destination "dist/$folder/RELEASE_VERIFICATION.json" -Force}
    $packageRoot=(Resolve-Path -LiteralPath "dist/$folder").Path
    $entries=Get-ChildItem -LiteralPath $packageRoot -File -Recurse | Where-Object { $_.Name -ne 'SHA256SUMS' -and $_.FullName -notmatch '[\\/](logs|\.bundle-cache)[\\/]' } | Sort-Object FullName | ForEach-Object {
        $relative=$_.FullName.Substring($packageRoot.Length+1).Replace('\','/')
        (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$relative
    }
    [IO.File]::WriteAllLines((Join-Path $packageRoot 'SHA256SUMS'),[string[]]$entries,[Text.UTF8Encoding]::new($false))
}
# POSIX scripts must have LF endings in the Windows-created zip.
Get-ChildItem -LiteralPath dist/EmberVps -Filter *.sh | ForEach-Object { $body=[IO.File]::ReadAllText($_.FullName).Replace("`r`n","`n");[IO.File]::WriteAllText($_.FullName,$body,[Text.UTF8Encoding]::new($false)) }
foreach($folder in @('EmberVps','EmberWindows')){
    # Rehash after newline normalization.
    $packageRoot=(Resolve-Path -LiteralPath "dist/$folder").Path
    $entries=Get-ChildItem -LiteralPath $packageRoot -File -Recurse | Where-Object { $_.Name -ne 'SHA256SUMS' -and $_.FullName -notmatch '[\\/](logs|\.bundle-cache)[\\/]' } | Sort-Object FullName | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$_.FullName.Substring($packageRoot.Length+1).Replace('\','/') }
    [IO.File]::WriteAllLines((Join-Path $packageRoot 'SHA256SUMS'),[string[]]$entries,[Text.UTF8Encoding]::new($false))
}
Compress-Archive -LiteralPath dist/EmberVps -DestinationPath dist/EmberVps-PM2-v0.3.0-test.zip -CompressionLevel Optimal -Force
Compress-Archive -LiteralPath dist/EmberWindows -DestinationPath dist/Ember-Windows-v0.3.0-test.zip -CompressionLevel Optimal -Force
Get-FileHash -LiteralPath dist/EmberVps-PM2-v0.3.0-test.zip,dist/Ember-Windows-v0.3.0-test.zip -Algorithm SHA256 | Format-Table -AutoSize
