# Project: Ember Game Updates & Visual Polish

## Architecture
- **Runtime C# Engine**: Unity C# runtime scripts located in `My project/Assets/Ember/Runtime/` and verified via `Builds/UiVerificationProject/Assets/Ember/Runtime/`.
  - `EmberWeaponGrips.cs`: Ergonomic grip, pivot, and scale configurations.
  - `EmberInventory.cs`: Held item rendering, torch & coaxial flame animation, crafting recipes, stone wall stack limit.
  - `EmberFieldHud.cs`: HUD notices, sub-announcements, objective bubble clusters, interaction prompts (barehand [E] mushroom harvest).
  - `EmberGame.cs`: Object pooling, renderer lifecycles, decor rendering, boss dash VFX (Effect Kind 3), harvestable proximity glows, stone wall health bar & relocation, Magma Kamehameha Ray beam VFX.
  - `EmberWorld.cs`: Data definitions (`BossType`, `Catalog.BossSpecs`, rock/tree tiers, recipe definitions, decor distribution algorithm, Magma Kamehameha skill logic).
  - `EmberLighting.cs`: Bullet and orb color definitions (`EnemyOrbColor`).
  - `EmberBossEvent.cs`: Boss telegraph lanes, molten arrows, and banner announcements.
- **Pixel Art Assets**:
  - `ArtShowcase/raw/`: Master pixel art repository.
  - `My project/Assets/Resources/Ember/Props/`: Main runtime sprite assets.
  - `Builds/UiVerificationProject/Assets/Resources/Ember/Props/`: Verification build runtime sprite assets.
  - `ArtShowcase/index.html`: Interactive browser showcase for pixel art animations.
- **Verification & QA**:
  - `tests/Ember.CoreChecks`: Standalone C# test harness verifying core gameplay mechanics, recipes, boss skills, and rock/tree tiers.
  - `.agents/skills/pixel-art/scripts/quality_audit.py`: Automated pixel art quality auditor checking 0 anti-aliasing (alpha 0 or 255), 0 orphan pixels, max palette size, and grid alignment.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| F1 | R1 Handheld Torch & Coaxial Flame | Set torch scale to 0.58f in `EmberWeaponGrips.cs`; attach `torch_fire` flame coaxially with torch head in `EmberInventory.cs` (remove `flameDist` offset and angle half-division). | M1 | Survey 1 |
| F2 | R2 Tool Pixel Art Sync | Overwrite outdated tool assets in `Builds/UiVerificationProject/Assets/Resources/Ember/Props/` from `ArtShowcase/raw/` (`axe.png`, `pickaxe.png`, `stone_pickaxe.png`, `crystal_axe.png`, `energy_drink.png`, `ore.png`, `torch.png`, `torch_fire.png`, etc.). | M1 | Survey 3 |
| F3 | R2 Pickaxe Separation | Ensure Iron Pickaxe (`pickaxe.png`) and Stone Pickaxe (`stone_pickaxe.png`, with alias `pickaxe_stone.png`) are separated in art and crafting menu. | M1 | Survey 3 |
| F4 | R2 Shared Team Chest Sprite | Create a new 32x32 warm wooden co-op storage chest for `chest.png` (distinct from Pandora chest `abandoned_chest.png`). | M1 | Survey 3 / Follow-up |
| F5 | R3 Sub-Announcement Repositioning | Lower Y coordinate of sub-announcement banner in `EmberFieldHud.cs` / `EmberGame.cs` (`objectiveY + 92f`) to eliminate overlap with the round HUD bubble cluster. | M1 | Survey 1 |
| F6 | R3 Sub-Announcement Spacing Fix | Fix text string concatenation in `EmberFieldHud.cs` line 628 so `EventActor` is properly separated with spacing/colon (`"{actor}: {evt}"` or `"{actor} {evt}"`). | M1 | Follow-up |
| F7 | R4 MissingReferenceException Resolution | Add safe null checks (`r != null && r.gameObject != null`) in 8 cleanup, retrieval, and sweep loops for `SpriteRenderer` (`decorPool`, `visibleDecor`, `treePool`, `renderers`) in `EmberGame.cs`. | M1 | Survey 1 |
| F8 | R5 Void Sovereign (LOCKED INVARIANT) | Boss 1 `boss.png` is officially finished, approved, and LOCKED. Subagents MUST NOT modify `boss.png`. | M1 | Lock Directive |
| F9 | R5/R8 Boss Bottom Shadow Removal | Remove 100% of bottom trapezoidal shadow (rows 57/58..63 alpha=0) across bosses (`boss_cinder.png`, `boss_eclipse.png`). | M1 | Survey 2 / Follow-up |
| F10 | R6 Cinder Behemoth Soul Knight Redesign | Redesign `boss_cinder.png` (256x64, 4 frames) comprehensively into a massive Basalt Lava Colossus (obsidian carapace, glowing lava veins, core furnace, magma horns, 2 separated heavy volcanic legs, bottom shadow alpha=0). | M1 | Lock Directive |
| F11 | R6 Eclipse Weaver Sprite Sheet | Design `boss_eclipse.png` (256x64, 4 frames: golden-purple eclipse crown halo, Black Sun core, 6 golden eyes, 6 articulated scythe legs, bottom shadow alpha=0). | M1 | Survey 2 |
| F12 | R6 Boss Visual Skill Themes & Orb Colors | Differentiate skill visual themes across 3 bosses; add missing `case 1` (Neon Purple `#a855f7`) for Void Sovereign and fix `case 3` (Eclipse Gold `#eab308`) in `EmberLighting.cs:EnemyOrbColor`. | M1 | Survey 2 |
| F13 | R9 Harvestable Proximity Glow & Prompt Removal | Pulse glowing outline on mushrooms (purple), lumidust (cyan), and ore (gold) when within 2.4 tiles; suppress `[E] Hái Nấm` HUD text prompt; keep landscape decor non-glowing. | M1 | Survey 1 / Follow-up |
| F14 | R10 Polish Night Severing Charge VFX | Polish Boss 1 "Xung Kích Xé Đêm" (Effect Kind 3) in `EmberGame.cs`: multi-layered neon purple void slash with glowing starlight cyan outer borders, shockwaves, and in-motion charge afterimages. | M1 | Survey 2 / Follow-up |
| F15 | R11 Standardize 3 Rock Tiers vs Trees | Standardize 3 tiers of rocks (Small Gravel 55 HP, Boulder 130 HP, Rich Vein 230 HP) with health bars and scaled drops matching trees in `EmberWorld.cs` and `EmberGame.cs`. | M1 | Survey 1 / Follow-up |
| F16 | R12 Fern & Wide Decor Truncation Fix | Separate `isAnimStrip = name.StartsWith("enemy_") || name.StartsWith("boss") || name == "merchant"` in `EmberGame.cs:687` so 32x16 and 40x24 decor retain full width. | M1 | Follow-up |
| F17 | R6 Art Showcase Integration | Update `ArtShowcase/index.html` with boss descriptions, previews, and ensure all 3 boss walk cycles animate smoothly. | M1 | Survey 2 |
| F18 | R7 Preservation Invariants & Build Verification | Keep Beacon, Energy Crystal, Player Character, and Boss 1 (`boss.png`) 100% UNMODIFIED; `dotnet build` 0 warnings/0 errors; `Ember.CoreChecks` 100% pass; `quality_audit.py` 100% pass. | M1 | Survey 1 / Survey 2 / Survey 3 / Lock Directive |
| F19 | Cinder Behemoth Upper-Body Animation | Animate `boss_cinder.png` upper-body with 1px bob, alternating swinging basalt arms, breathing/pulsing magma horns and core across 4 frames. | M1 | Follow-up 13:35 |
| F20 | Mushroom Harvesting Dual Mechanism | Allow barehand [E] to harvest directly into inventory, or attack [K]/click to drop mushroom for ground pickup. | M1 | Follow-up 13:35 |
| F21 | Resource & Decor Distribution Spacing | Maintain min 3 tiles spacing between mushroom, fern, and fallen log clusters; no overlap; never spawn on bridge tiles. | M1 | Follow-up 13:35 |
| F22 | Stone Wall Mechanics | Stone wall shows health bar upon placement, is pickable/relocatable, and enforces StackLimit = 1. | M1 | Follow-up 13:35 |
| F23 | Cinder Behemoth Magma Kamehameha Ray | Implement "Hỏa Chưởng Tro Tàn" (Kind 1 telegraph lane, 14-16 tiles, 2.8w, 1.6s charge with molten progress arrows, 0.8s massive lava beam with knockback and safe flank flanks). | M1 | Follow-up 13:41 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| 1 | Ember Unified Game Updates | Implement F1–F23: C# gameplay & HUD fixes, pixel art upgrades, boss sprites & skill VFX polish, asset sync, showcase update, and full build/QA verification. | none | IN_PROGRESS |

## Interface Contracts
- **EmberWeaponGrips ↔ EmberInventory**:
  - `EmberWeaponGrips.Torch.Scale = 0.58f`
  - `torchHandPos = holdingHand + new V2(sign * tg.HandOffset.X, tg.HandOffset.Y)`
  - `flameR = Put("heldTorchFlame" + player.Id, flameSprite, torchHandPos, Color.white, tg.Scale, heldOrder + 1, !player.Right)`
  - `flameR.transform.rotation = Quaternion.Euler(0, 0, torchAngle)`
- **EmberGame ↔ EmberFieldHud**:
  - `DrawFieldNotices(p, QuestCollapsed ? objectiveY + 92f : objectiveY + 88f)`
  - `message = (string.IsNullOrEmpty(State.EventActor) ? "" : State.EventActor + ": ") + EmberLanguage.Text(State.Event) + ...`
- **EmberLighting ↔ EmberWorld**:
  - `int colorKind = (int)boss.BossType + 1;`
  - `case 1: return new Color(0.66f, 0.33f, 0.97f);` (Neon Purple)
  - `case 2: return new Color(1f, 0.48f, 0.16f);` (Molten Orange)
  - `case 3: return new Color(0.92f, 0.70f, 0.03f);` (Eclipse Gold)

## Code Layout
- `My project/Assets/Ember/Runtime/` and `Builds/UiVerificationProject/Assets/Ember/Runtime/`:
  - `EmberWeaponGrips.cs`: Torch scale and grip definition
  - `EmberInventory.cs`: Torch & coaxial flame rendering, recipe display, stone wall stack limit
  - `EmberFieldHud.cs`: Sub-announcement position and message formatting, barehand harvest prompt
  - `EmberGame.cs`: MissingReference safe null checks, decor strip fix, boss dash VFX, proximity glow, wall HP & relocate, Magma Kamehameha Ray beam
  - `EmberWorld.cs`: Rock tier HP & drops, recipe definitions, distribution algorithm (min 3 tiles, no bridges), Cinder Behemoth Magma Kamehameha skill
  - `EmberLighting.cs`: Enemy orb colors
  - `EmberBossEvent.cs`: Boss telegraph lanes, molten arrows, and banner announcements
- `ArtShowcase/raw/` & `My project/Assets/Resources/Ember/Props/` & `Builds/UiVerificationProject/Assets/Resources/Ember/Props/`:
  - `boss.png`: Void Sovereign sprite sheet (256x64, 4 frames) — **LOCKED INVARIANT**
  - `boss_cinder.png`: Cinder Behemoth sprite sheet (256x64, 4 frames) — Basalt Lava Colossus with animated upper body
  - `boss_eclipse.png`: Eclipse Weaver sprite sheet (256x64, 4 frames)
  - `chest.png`: Team shared co-op chest (32x32)
  - Tools: `axe.png`, `pickaxe.png`, `stone_pickaxe.png`, `crystal_axe.png`, etc.
- `ArtShowcase/index.html`: Showcase viewer
