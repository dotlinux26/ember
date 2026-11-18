# GDD requirements and explicit test-build tuning

The original GDD is preserved. This file separates its defined behavior from numbers needed to make a runnable test build. Art iteration is deferred at the user's request. User amendments add solo/1–4 capacity, VI/EN, PM2 deployment and opt-in sandbox diagnostics.

## Values fixed by the GDD

- Eclipse lasts 90 seconds; complete darkness after empty lamps and destroyed beacon lasts 10 seconds before defeat.
- HP: Traveler/Trapper 100, Moon Mender 90, Warden 140, Wraith 85; stamina 100. Warden speed −5%, Wraith +15%, Trapper burden +10%.
- Dread: +1.5/s outside light, +3/s alone, −5/s in safe light. Thresholds 30/60/90. Both participants in conditional friendly fire must be outside actual safe light; returning to light relocks it.
- Moon skill heals 35 HP, removes 40 dread, cooldown 25s. Bomb blinds 3s, cooldown 20s. Warden virtual armor 4s, cooldown 18s. Wraith takes self + one adjacent ally to beacon, cooldown 60s.
- Rank-1 perks: dread gain −25%, burden +30%, dash stamina −20%, start with 2 flares + 1 sedative, start with pickaxe, final light 5s, outgoing hallucinated friendly fire damage −50%.
- Hats: gathering speed +5%, stamina regeneration +8%, candle radius 1 and dread gain −10%, Jester damage +15% at dread ≥90.
- Three perk slots maximum. Four hats/pets, five vessel roles. No active skill on Traveler. Rooted body sprite has no attached hands; weapon/gloves hide during Cast.

## Test-build numbers where the GDD gives no exact value

These are implementation tuning, not invented GDD quotations. They remain editable in `EmberWorld.cs`, `EmberSave.cs` and the economy catalog. Playtesting may change them without changing the prescribed mechanics.

| Area | Current test value |
| --- | --- |
| World | 250 x 250, three radial biomes, three-tile abyss border |
| Beacon awakening | 20 cores, 5% each; beacon starts unlit and lights at 100% |
| Beacon HP / shared fuel | 600 / 100; one starting lantern carrier |
| Walking / dash | Base speed 4 tiles/s; dash 25 stamina, 1.2s cooldown, 0.16s dash |
| Lantern radii | Oil/Inferno safe 5, Relic safe 8; visual falloff extends one tile |
| Candle hat safety | Its small visual light does not grant lantern safety; otherwise the specified 10% dread reduction would be replaced by complete immunity |
| Trapper bomb | 65 base AoE damage, radius 3 |
| Warden virtual armor | 40 points for the specified 4 seconds |
| Relic support | Heal 3 HP/s, extra dread cleanse 3/s, regenerate shield up to 20 |
| Mirage chest | 15 damage on opening |
| Craft economy | Fourteen recipe costs in `Catalog.Recipes`; first seven unlocked, advanced recipes 3 Memory each |
| Perks | Unlock 30 Ash, upgrade rank 1→2 costs 60, rank 2→3 costs 120, maximum rank 3 |
| Numeric perk upgrades | Rank 1 retains GDD effect; rank 2/3 multiply that effect by 1.2/1.4 |
| Prepared upgrades | Rank 1: 2 flares/1 sedative; rank 2: 3/1; rank 3: 4/2 |
| Miner upgrades | Pickaxe at every rank; gathering cap 3/4/5 per action |
| Slots | Each additional perk slot costs 5 Memory, capped at 3 |
| Gacha | 3 Tokens/draw. Duplicate grants 10 Ash. Vessel weights 49/20/20/5/5/1 for common/rare/rare/epic/epic/special; hats/pets uniform |
| Exchange | 100 Ash → 1 Token |
| Ash rewards | Time x 0.1 + max(gathered, carried) x 2 + 20; extracted share 100%, dead with successful teammate 50%; otherwise 25% consolation Ash |
| Memory | Extraction base 4; dead with extraction 2; boss base 2, halved for a dead teammate when team extracts |
| Tokens | Successful extraction 2; eligible dead teammate 1; independent one-time achievements below |
| Achievements | Survive 120s: 2 Tokens; first extraction: 3; defeat boss: 2; craft 5 total: 2. Awarded once per profile |
| Network | Simulation 30 Hz; movement up to 30 Hz; snapshots 10 Hz, compressed and chunked; selectively resend missing fragments |
| Transport budgets | Up to 20 cached snapshots, 8 pending assemblies/client, 2s assembly lifetime, up to 256 repair chunks/peer/second |
| Broker | UDP 27842, 4096 room limit, 4 members/room, 15s member TTL, per-IP rate cap 4000 datagrams/s, bounded packet sizes |

The broker is not a game server. Two thousand broker registrations, if reported by tests, do not establish two thousand active relayed games. Same-machine P2P does not establish success across NATs. AES/HMAC are implemented and tested; absolute prevention of client memory tampering or Internet DDoS is not a defensible guarantee.

.NET 8 in the original infrastructure example reached end of support on 2026-11-10. Broker/probe now target supported .NET 10 LTS while retaining the specified C# UDP/in-memory broker/opaque relay architecture. Unity's runtime and the portable gameplay source are separate from that service target.

Sources: [Microsoft .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy), [PM2 application declaration](https://pm2.keymetrics.io/docs/usage/application-declaration/).

Little Moone uses the supplied right-facing clips at 6 fps. Special draws arrive dormant. A living extraction after lighting the beacon rolls a 30% chance for one Awakening Memory Stone per survivor. Awakening costs one stone. Meteor tuning starts at 18 tiles of auto-aim range, 5 impacts, 2.2 tiles of radius per impact, 28 damage per impact and a 24-second cooldown.
