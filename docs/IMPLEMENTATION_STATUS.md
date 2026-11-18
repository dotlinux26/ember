# Ember GDD coverage and verification

Updated 2026-11-16, release candidate `0.3.0-test`, protocol 3. The original GDD is unchanged. User amendments: solo/1–4 capacity, Vietnamese/English, PM2, an isolated debugging mode and log visibility. Sprite redesign is explicitly deferred by the user.

## Implemented behavior

| GDD area | Implementation | Evidence / practical limit |
| --- | --- | --- |
| Expedition loop | Explore, 20-core awakening, 90s Eclipse, extraction, total wipe and 10s blackout | Simulation transition tests; debug can reproduce every phase |
| Dead teammate rewards | 50% eligible extraction rewards when a teammate escapes; extracted count persists after departing clients | Reward and extraction checks; a hard host crash can lose unreceived final state |
| World | Seeded 250x250 cellular map, three radial biomes, streamed obstacle presentation | Seed/abyss/navigation tests; no claim of measured minimum-spec frame rate |
| Abyss | Host movement collision, BoxCollider2D borders and unlit opaque fog shader with five-tile fade | Boundary checks and shader compilation; final visual playthrough remains a manual acceptance step |
| Inputs | WASD/arrows, eight-direction dash, right-click bounded A*, virtual joystick/buttons through the same intent model | Simulation and built-player movement checks; mobile target builds not tested |
| Character presentation | Left/right flip, separate glove/weapon layers, hemisphere focus and dual indicators, Cast hides equipment | Existing placeholder character atlases; new art is not approved/shipped as final |
| Core stats | HP/wisp, stamina, doubled light regeneration, burden, shared fuel, lamp transfer, dread | Authoritative simulation and rule checks |
| Visibility/FF | Cyan allies with names/HP/dread bars; red enemies; private monster disguise at 90%; both participants must be outside real safety for FF, retaliation, relock in light | FF and safety checks; same-machine multiplayer exercised |
| Paranoia | False footsteps/edge shadows at 30%; private exploding mirage and observer-only lamp flicker at 60%; disguise/targeting at 90% | Debug presets 35/65/95; audio/UI aesthetic review is still manual |
| Resources/crafting | All five resources, all 14 recipes, table proximity for advanced gear, consumables, walls/traps/shared chest, lantern/weapon branches | All recipe branches tested; economics exposed as test tuning |
| Gear | Oil/Inferno/Relic support, virtual light shield, six weapon branches plus pickaxe, host projectile travel and collision | Core combat/crafting and crypto/state tests; balance needs playtesting |
| Vessels | Five stated stat baselines and all four active skills with GDD cooldowns/effects | Per-vessel checks; Warden has damage-absorbing timed armor, not blanket invulnerability |
| Enemies | Biome rosters, elites, nests dropping Umbra, Eclipse boss and Memory reward | Code/build and scenario spawning; final animation/content polish deferred |
| Meta | Ash/Memory/Tokens, extraction/boss rewards, exchange, one-time achievements, perk unlock/upgrades, up to three slots, recipe unlocks | New progression/migration/host recipe-enforcement checks |
| Perks | Seven rank-1 effects retain GDD values; explicit rank-2/3 tuning | Rank/cost/cap and base-rule checks; see balance ledger |
| Hats/pets | Four hats and four pets; Jester laugh, pickup/warning/core direction/death marker behaviors | Functional implementation; authored art still provisional |
| Gacha | Vessel rarity labels, weighted vessel draws, uniform hats/pets, duplicates grant Ash | Costs/odds not specified in GDD are declared tuning |
| VI/EN | Saved language toggle, translated gameplay/catalog/menus, explicit bilingual debug/settings | Translation checks; player/room names remain unchanged |
| Saves | Local AES256-CBC encrypt-then-HMAC, PBKDF2 100k backup password, atomic replace, dual export, five-backup rotation, restore preview and recovery code | Unicode/tamper/wrong-password/rotation/progression validation checks; no absolute client-memory anti-cheat claim |
| Windows picker | Native comdlg32 Open/Save and Explorer reveal | Source/build; actual save-dialog clickthrough remains manual |
| LAN/VPN | Capacity 1–4, readiness, authoritative intent, reliable deduplicated actions, bounded compressed snapshots, departure/timeout cleanup | Actual 1/2/4 built-player loopback tests |
| STUN/P2P | Same gameplay socket, Google→Cloudflare fallback, XOR-MAPPED parsing, discovered and broker-observed candidates, signed room replies and punching | STUN vectors, direct room/player tests; no multi-NAT success percentage measured |
| Relay | Three-second Auto fallback, explicit forced modes, host star fan-out, endpoint-bound expiring tokens, per-packet HMAC/replay protection and gameplay AES-GCM/HKDF | 4-member direct/relay checks, malformed data, wrong password/full room, packet-loss probes |
| Snapshot recovery | Selective NACK/retransmit of missing encrypted fragments, bounded cache/requests/assemblies | Exact reconstruction test under 30% loss; random difficult-to-compress probe data |
| Deployment | Self-contained .NET 10 x64/ARM64, PM2 one fork, restart policy, local UDP healthcheck, JSON logs and signed-file inventory | Cross-publish, executable headers, local service/probe tests. Linux/PM2 execution requires real VPS acceptance |
| Debug | F8 sandbox, no real save writes, host-only mutations, selectable 35/65/95 presets, phase/resource/death/boss/loadout tools, simulated packet loss | Disabled-by-default API checks and built-player scenario launches |

## Evidence

- `Builds/core-final.txt`: **102 checks passed** on the final source, including snapshot repair under 30% loss and fallback-control retries.
- `Builds/player-*.log`: solo preset and actual built-player LAN/P2P/relay smoke results. These are multiple processes on one Windows machine.
- `Builds/network-{auto,direct,relay}.jsonl`: repeated four-member probes with 20% simulated loss.
- `Builds/network-repair-check.jsonl`: fragmented random payload with 30% loss, ACK recovery and exactly-once actions.
- `Builds/network-relay-soak-final.jsonl`: sustained four-member relay probe, recorded separately from initial failed probe.
- `Builds/broker-load.jsonl`: **2,000 broker registrations in ~1.26s**, signature validation, liveness while idle, and room expiration after TTL. This does **not** represent 2,000 active relayed games.
- `Builds/broker-release.log`: protocol-3 .NET10 broker start, room lifecycle, health counters and expiry.
- `Builds/unity-final.log`: final Windows compilation/build report.
- `dist/*/SHA256SUMS`: checksums for the actual packaged files.
- `Builds/vps-health-observation.json`: the target VPS did not return a protocol-3 health reply within five seconds. This does not distinguish missing deployment from unreachable UDP; remote deployment remains unverified.

The initial relay soak's failure is retained at `Builds/network-relay-soak.jsonl`: actions arrived but the probe checked before remaining ACKs drained. The strengthened probe now waits for ACK completion and uses incompressible fragmented payloads; the transport also gained missing-fragment repair. Old test counts and earlier prototype claims are preserved only in `IMPLEMENTATION_STATUS_earlier.md` and are not current status.

## Remaining acceptance work

1. Run the Linux binary under PM2 on the target VPS. This Windows workstation has no installed Linux/WSL or Docker runtime; cross-publishing does not prove Linux startup or host-library compatibility.
2. Execute the guide's external probe and two-to-four-machine checks on distinct networks, including Auto failure→Relay, real NAT/firewall behavior, host departure, disconnect during extraction, and a complete ordinary (non-cheat) run.
3. Inspect the final UI/light/audio/backup dialogs manually, profile low-end performance, and tune gameplay/economy. Built smoke tests do not establish visual quality or full interactive acceptance.
4. Independent Internet security/load review and sustained real relay traffic. The GDD's 80–85% direct-connection estimate and absolute DDoS/anti-cheat language are not verified guarantees.
5. Android/iOS packaging, native document callbacks and device testing belong to the GDD's explicitly second-stage mobile work; they are not delivered Windows features.

The original GDD does not define every craft cost, damage/radius, perk-upgrade tier, gacha probability or reward equation. Those choices are openly listed in `BALANCE_TUNING.md`; they are not silently attributed to the GDD. No claim of gap-free production completion is made while these acceptance items remain.
