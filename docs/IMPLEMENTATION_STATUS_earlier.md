# Ember implementation status — 2026-11-16

The current art decision is **native 32×32 cells with a compact 18–24px character**, original designs informed by the supplied Soul Knight reference. The earlier 64px tracing experiment is superseded. Art has not been approved by the user.

This is a playable implementation milestone, **not completion of the entire GDD**. This ledger preserves the remaining work rather than declaring the document gap-free.

## Verification achieved

- Current `art/ember32`: strict official PIX2 parsing, zero warnings, mirror equality, serialize/parse pixel equality, and PNG/compositor equality for all 96 frames.
- `tests/Ember.CoreChecks`: **43 checks passed**, including five vessel skill behaviors, all fourteen recipes, lantern transfer, save authentication/rotation, extraction/friendly-fire rules, STUN decoding and LAN packet reconstruction.
- Unity Windows build succeeded (`Builds/unity-build-smoke.log`, `EMBER_BUILD_PASS`).
- Built player startup: `EMBER_SMOKE PASS tick=90 sprites=53` in `Builds/smoke.log`. Automatic screenshot failed when the window was hidden.
- Two actual built Windows processes: host and client both logged `EMBER_NET_SMOKE PASS`, two players, and the same remote X position `6.966665` (`Builds/net-host.log`, `Builds/net-client.log`). This verifies loopback game integration, not distinct-PC LAN or Internet traversal.
- Direct Computer Use inspection encountered a Windows Security firewall prompt for the test executable. It was left untouched. Manual visual gameplay inspection remains incomplete; the menu was only visible behind the modal. No security/network permissions were changed.

## Implemented

| GDD area | Current behavior | Evidence / limits |
|---|---|---|
| Extraction loop | Explore → deposit 20 cores → 90s Eclipse → portal interaction → win; wipe/10s total blackout loss | Core checks cover phase transitions, dead teammate half reward |
| Procedural world | Seeded 250×250 map, three radial biomes, cellular obstacle clusters, abyss collision and visual border | Deterministic seed check; no streaming optimization or biome-specific enemy roster yet |
| Input | WASD/arrows, Space/left click, C/F/R; M toggles right-click pathfinding, E interaction, B deposit, Q refuel | New Input System; A* bounded at 5000 expansions; mobile controls pending |
| Character presentation | Two directions, compact PIX2 sprites, separate floating white gloves, weapon pivot, casting hides hands/weapon | Three visual designs currently cover five gameplay vessels; distinct bomber/wraith art still needed |
| Targeting | Basic attack front hemisphere, immediate host checks, skill targeting by role in 360 degrees; two range rings | Real weapon art/projectile travel and full skill VFX remain prototype quality |
| Stats | HP/wisp/revive, stamina/dash, burden speed penalty, dread rates/thresholds, shared fuel | Team starts with one lantern carrier. T toggles; L passes lamp to a nearby teammate. Crafted lanterns add carriers |
| Paranoia | False footsteps ≥30; private false chest ≥60 explodes on interaction; ally recolor/name hiding and targeting at ≥90 | Recolor is a temporary stand-in for a dedicated monster disguise sprite |
| Friendly fire | Host checks madness, both participants' actual safety, 4s self-defense retaliation window | Unit checks cover protection and initiating attack; full multi-PC scenario testing remains |
| Resources/crafting | All five resources, consumables, revival stone, walls, traps, shared chest deposit/withdraw, lantern and six weapon branches | Structure/enemy/weapon art is procedural placeholder; content balancing not final |
| Vessels | All five stats/active skills, healing/dread cleanse, bomb/blind, knockback/4s shield, return teleport with nearby ally | Teleport implementation tested only through code/build, not all multiplayer edge cases |
| Meta | Three currencies, run rewards, perk unlock/equip/three slots, character/hat/pet draws, duplicate compensation | Reward economy and rarity rates need playtesting; no paid purchases |
| Perks/hats/pets | Seven perks; hats modify harvest/regen/dread/light/damage; firefly dust pickup, bat warning, cat periodic core direction, little-ghost death marker | Hat and pet art is placeholder; miner starts with an axe-equivalent rather than dedicated pickaxe art |
| Save | Versioned AES-256-CBC + HMAC-SHA256 envelope, PBKDF2-SHA256 100k, random salt/IV, authenticated header, atomic writes, five backups, native Windows picker | Tamper/wrong-password/backup tests. Local saves cannot prevent a user who controls the client from cheating |
| LAN/VPN | 2–4 players, UDP host-authoritative intent, authenticated packets, chunked/compressed snapshots, sequence checks, disconnect cleanup | Loopback join/input/multipart snapshot checks. LAN transport is authenticated **not encrypted**, not public-Internet hardened |
| STUN | Standalone endpoint discovery and strict XOR-MAPPED-ADDRESS parser, IPv4/IPv6, transaction/sender checks | Synthetic IPv4 vector and transaction rejection checked. Not yet wired into game matchmaking |

## Remaining GDD gaps

1. Finish the visual design iteration with the user: original appealing sprites at native 32px, then richer animation for approved silhouettes. Dedicated bomber/wraith, enemies, boss, gear, environment, hats and pets need production art. Current art is technically valid but aesthetic approval is outstanding.
2. Replace prototype IMGUI HUD, coarse screen-space darkness and enlarged silhouette outline with production UI, smooth URP 2D lighting and proper outline/disguise shaders. Add accessibility/audio controls, mobile joystick/buttons, tutorials and localization polish.
3. Integrate STUN on the actual transport socket; implement broker room codes, authenticated punch negotiation, timeout/fallback, relay fan-out, session-bound replay protection, HKDF/AES-GCM E2EE and per-peer reliable action delivery. No service has been deployed to the VPS. Credentials and infrastructure access were not available; no attempt was made to change it.
4. Test real 2–4-PC LAN/VPN, hostile/reordered/lost packets, reconnect, extraction/disconnect edge cases, and NAT/relay across distinct networks. Current loopback checks cannot substantiate Internet connectivity percentages or capacity claims.
5. Add map streaming/culling profiling, robust long-distance enemy navigation, full enemy/boss/nest roster, balanced progression/content, persistent settings and complete camera/pixel-perfect handling.
6. Save import/export UX still needs internal-backup selection, restore previews and explicit confirmation before replacing current progress, open-specific-export-folder and quick recovery-code UI. Android/iOS document providers remain pending.
7. Expand regression coverage to each perk, malformed profiles and multiplayer state transitions. All five skill behaviors and all recipe branches now have core checks. Run a complete manual extraction playthrough and a two-instance visual session; startup and loopback checks alone do not establish those behaviors.

## Sources and format decisions

- User GDD: `D:/Ember/game_design_document_light_shadow_co_op_survival.md` is the game specification. Attached prompt prose is reference material, not an authority replacing the user's latest instructions.
- [Noiradix Studio](https://github.com/dotlinux26/noiradix-studio), local reference parser and Unity import schema. PIX2 validation compares compositor pixels after serialize/parse, not source formatting.
- [Unity Input System](https://docs.unity.com/en-us/engine/6000.0/manual/packages-list/packages-all/pack-safe/com-unity-inputsystem) and [runtime initialization documentation](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/runtimeinitializeonloadmethodattribute).
- [RFC 8489](https://www.rfc-editor.org/rfc/rfc8489.html), which supersedes RFC 5389. STUN discovers mappings; it is not a complete NAT traversal solution on its own.
- [.NET cryptographic services](https://learn.microsoft.com/dotnet/standard/security/cryptographic-services). Authentication is verified before decrypting the save payload.

## Reproduce

```powershell
# At D:/Ember
python art/ember32/source/build.py
node art/ember32/source/validate.cjs
python art/ember32/source/check_png.py
dotnet run --project tests/Ember.CoreChecks
& 'D:/UnityHub/Editor/6000.6.5f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath 'D:/Ember/My project' -executeMethod Ember.Editor.EmberBuild.Windows -logFile 'D:/Ember/Builds/unity-build-verified.log'
```

The editor creates a dedicated `Assets/Scenes/Ember.unity` and points Build Settings at it. The original SampleScene is preserved. Runtime only loads `Resources/Ember/Active` for current characters. The archived 64px textures under `Resources/Ember/Characters` are unused and currently still included in builds; moving those out of Resources is a later size cleanup.
