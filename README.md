# Ember — Don't Trust The Torch

Unity 6000.6.5f1 Windows test build, bilingual VI/EN. Sprite work is paused at the user's request. The current release focuses on GDD mechanics, room networking, PM2 packaging and reproducible debugging.

- [Player/VPS/debug guide](docs/HUONG_DAN_TEST_VI_EN.md)
- [GDD implementation and evidence](docs/IMPLEMENTATION_STATUS.md)
- [Explicit balance tuning](docs/BALANCE_TUNING.md)
- [Original GDD](game_design_document_light_shadow_co_op_survival.md)

Release artifacts are generated under `dist`: **Ember-Windows-v0.3.0-test.zip** and **EmberVps-PM2-v0.3.0-test.zip**. The Windows package has `Launch-Game.cmd`, `Launch-Test.cmd`, a VPS probe and log collection. The VPS package contains self-contained .NET 10 x64/ARM64 binaries; run `sh start-pm2.sh`, then `node healthcheck.cjs`. Open UDP 27842 on the VPS/provider network; game LAN hosting uses UDP 27841 on the player's host.

Rooms support solo and capacity 1–4. Internet rooms use six-character codes with Auto P2P→Relay, forced Direct or forced Relay. LAN/VPN connects to host IPv4. Readiness and authoritative gameplay remain on the player host. The broker stores rooms in RAM and relays opaque encrypted packets, not accounts or save data.

F8 opens diagnostics. Enable sandbox to test 35/65/95% dread, hostile hallucinations, supplies, deaths, boss, Eclipse, extraction, blackout, weapons and packet loss. World changes are host-only. Sandbox progress is in RAM; real profile writes are disabled, including for ordinary clients joining a sandbox host. Exit and reopen normal mode to resume real progress.

Controls: WASD/arrows; K/left-click attack; J skill; Space dash; R ranges; M/right-click pathfinding; E interact/extract; B deposit cores; Q refuel; Tab craft; 1–4 select consumable, U use; T toggle lantern; G pass lantern; L shove; I inventory; 0 equip weapon; Esc menu. Collect 20 cores, awaken the beacon, defend 90 seconds, then extract.

Build Windows from Unity using **Ember → Build Windows**, or:

```powershell
& 'D:/UnityHub/Editor/6000.6.5f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath 'D:/Ember/My project' -executeMethod Ember.Editor.EmberBuild.Windows -logFile 'D:/Ember/Builds/unity-release-package.log'
```

Developer checks:

```powershell
dotnet run --project tests/Ember.CoreChecks -- --broker
powershell -File tools/test_players.ps1
powershell -File tools/test_network.ps1 -Seconds 15 -Cycles 2 -Loss 20
powershell -File tools/package_release.ps1 -BuildGame
```

The broker must already run on loopback UDP 27842 for `--broker`, Internet player tests and network probes. Service projects require .NET 10 SDK; the workspace-local SDK is `tools/dotnet10/dotnet.exe`. The pure gameplay checks remain compatible with .NET 8; the Unity game uses its own runtime. Runtime 10 replaces the original infrastructure example's now-unsupported .NET 8 without changing the protocol architecture.

Local automation does not establish production stability, Linux execution, NAT success percentages or independent DDoS/security assurance. Complete the guide's VPS/real-network acceptance steps after deploying the package. Android/iOS remain the GDD's second-stage target; Windows is the current delivered platform.
