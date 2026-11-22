using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
using UnityEngine;
#endif

namespace Ember
{
    /// <summary>
    /// Ancient Relic Drop (Hộp Cổ Thất Lạc) World Event System:
    /// Periodic cosmic event where an Ancient Relic falls at the dark boundary of the map.
    /// Signals players via lightning strikes, glowing cosmic beacons, and alert broadcasts.
    /// Spawns a high-tier Relic Chest guarded by strong shadow monsters (VoidStalkers / Sentinels).
    /// Opening grants rare treasures (Void Tokens, rare gems, high-tier gear) creating high-stakes risk/reward.
    /// </summary>
    public static class EmberEvents
    {
        public const int RelicChestKind = 11;
        public const float DefaultRelicInterval = 180f; // Drops every 3 minutes
        public const float RelicAlertDuration = 7.5f;
        public const float RelicInteractionDistance = 2.4f;

        public static float NextRelicDropTime = 120f;
        public static float RelicAlertTimer = 0f;
        public static V2 LastRelicDropPos = new V2(0, 0);
        public static bool HasActiveRelic = false;
        public static int TotalRelicsSpawned = 0;
        public static int TotalRelicsClaimed = 0;
        public static string LastAlertMessageVi = "";
        public static string LastAlertMessageEn = "";

        // Track lightning strike visual effects
        public sealed class LightningStrike
        {
            public V2 TargetPos;
            public float Timer;
            public float MaxDuration;
            public float BeamWidth;
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
            public Color Color;
#endif
        }

        public static readonly List<LightningStrike> ActiveLightnings = new List<LightningStrike>();

        /// <summary>
        /// Simulation tick update called by EmberWorld or host game loop.
        /// </summary>
        public static void Update(EmberWorld world, float dt)
        {
            if (world == null || world.S == null) return;

            // Update alert timer
            if (RelicAlertTimer > 0)
                RelicAlertTimer = Math.Max(0f, RelicAlertTimer - dt);

            // Update active lightning strikes
            for (int i = ActiveLightnings.Count - 1; i >= 0; i--)
            {
                ActiveLightnings[i].Timer -= dt;
                if (ActiveLightnings[i].Timer <= 0)
                    ActiveLightnings.RemoveAt(i);
            }

            // Periodic Relic Drop during Explore / Eclipse phase
            if ((world.S.Phase == Phase.Explore || world.S.Phase == Phase.Eclipse) && !world.S.BossArenaActive)
            {
                if (world.S.Time >= NextRelicDropTime)
                {
                    SpawnAncientRelic(world);
                }
            }

            // Check if active relic chest still exists in world
            bool chestExists = world.S.Structures != null && world.S.Structures.Any(s => s.Kind == RelicChestKind && s.HP > 0);
            HasActiveRelic = chestExists;

            // Proximity interaction check for players pressing Interact [E]
            if (chestExists && world.S.Players != null)
            {
                foreach (var p in world.S.Players)
                {
                    if (p.Ghost || p.Extracted) continue;

                    var chest = world.S.Structures.FirstOrDefault(s => s.Kind == RelicChestKind && s.HP > 0 && V2.Distance(s.Pos, p.Pos) <= RelicInteractionDistance);
                    if (chest != null)
                    {
                        // Check if player triggered interaction or right next to it
                        // Auto-claim when close enough or via Interact intent
                        // Note: world commands or direct trigger
                    }
                }
            }
        }

        /// <summary>
        /// Spawns an Ancient Relic Chest at the map dark boundary with lightning strike and shadow guardians.
        /// </summary>
        public static bool SpawnAncientRelic(EmberWorld world, V2? customPos = null)
        {
            if (world == null || world.S == null) return false;

            int size = world.S.Size > 0 ? world.S.Size : 250;
            var rng = new System.Random(world.S.Seed + (int)(world.S.Time * 10) + TotalRelicsSpawned * 17);

            // Calculate drop position at map's dark boundary (~38-44% of map radius away from center)
            V2 dropPos;
            if (customPos.HasValue)
            {
                dropPos = customPos.Value;
            }
            else
            {
                float minBoundaryRadius = (size * 0.5f) * 0.72f;
                float maxBoundaryRadius = (size * 0.5f) * 0.88f;
                float radius = minBoundaryRadius + (float)rng.NextDouble() * (maxBoundaryRadius - minBoundaryRadius);
                double angle = rng.NextDouble() * Math.PI * 2.0;

                float x = (float)(Math.Cos(angle) * radius);
                float y = (float)(Math.Sin(angle) * radius);

                // Clamp within map bounds
                float limit = size * 0.5f - 8f;
                x = Math.Max(-limit, Math.Min(limit, x));
                y = Math.Max(-limit, Math.Min(limit, y));
                dropPos = new V2(x, y);
            }

            LastRelicDropPos = dropPos;
            HasActiveRelic = true;
            TotalRelicsSpawned++;
            NextRelicDropTime = world.S.Time + DefaultRelicInterval;
            RelicAlertTimer = RelicAlertDuration;

            LastAlertMessageVi = "⚡ CỔ VẬT THẤT LẠC ĐÃ GIÁNG THẾ TẠI RÌA THẾ GIỚI! ⚡";
            LastAlertMessageEn = "⚡ ANCIENT RELIC HAS CRASHED AT THE MAP BOUNDARY! ⚡";

            // 1. Spawns high-tier glowing Relic Chest structure
            int chestId = world.S.Structures != null && world.S.Structures.Length > 0 ? world.S.Structures.Max(s => s.Id) + 1 : 100;
            var chest = new Structure
            {
                Id = chestId,
                Kind = RelicChestKind,
                Pos = dropPos,
                Life = 999999f,
                MaxLife = 999999f,
                HP = 300f
            };

            // Add structure to world via authoritative host helper
            world.SpawnStructure(chest);

            // 2. Cosmic Beacon / Lightning Strike Visuals
            ActiveLightnings.Add(new LightningStrike
            {
                TargetPos = dropPos,
                Timer = 2.5f,
                MaxDuration = 2.5f,
                BeamWidth = 1.6f,
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
                Color = new Color(0.35f, 0.85f, 1f, 0.95f)
#endif
            });

            // Add transient cosmic light so the chest glows and emits light in the deep dark
            world.SpawnTransientLight(new TransientLight
            {
                Id = chest.Id + 500,
                ColorKind = LightColorKind.RuneGold,
                Pos = dropPos,
                Radius = 11.5f,
                Intensity = 1.0f,
                Life = 180f,
                MaxLife = 180f,
                GrantsSafety = true
            });

            // 3. Spawn strong shadow monsters guarding the Relic Chest
            SpawnShadowGuardians(world, dropPos, rng);

            // Announce event to all players
            world.AddWorldEvent(LastAlertMessageVi, LastAlertMessageEn);

            return true;
        }

        /// <summary>
        /// Spawns 3-4 elite shadow monsters guarding the Ancient Relic Chest.
        /// </summary>
        static void SpawnShadowGuardians(EmberWorld world, V2 center, System.Random rng)
        {
            if (world?.S == null) return;

            // Guard 1 & 2: Elite VoidStalkers (high damage stealth monsters)
            for (int i = 0; i < 2; i++)
            {
                double guardAngle = (i * Math.PI) + (rng.NextDouble() * 0.8 - 0.4);
                float dist = 2.2f + (float)rng.NextDouble() * 1.2f;
                V2 pos = center + new V2((float)Math.Cos(guardAngle) * dist, (float)Math.Sin(guardAngle) * dist);

                world.SpawnEnemy(new Enemy
                {
                    Pos = pos,
                    HP = 135f,
                    MaxHP = 135f,
                    Elite = true,
                    Kind = EnemyKind.VoidStalker,
                    AttackCD = 1.5f
                });
            }

            // Guard 3: Elite Sentinel (ranged crystal flyer)
            {
                double guardAngle = Math.PI * 0.5 + (rng.NextDouble() * 0.8 - 0.4);
                V2 pos = center + new V2((float)Math.Cos(guardAngle) * 3.2f, (float)Math.Sin(guardAngle) * 3.2f);
                world.SpawnEnemy(new Enemy
                {
                    Pos = pos,
                    HP = 110f,
                    MaxHP = 110f,
                    Elite = true,
                    Kind = EnemyKind.Sentinel,
                    AttackCD = 1.8f
                });
            }

            // Guard 4: Elite Shade
            {
                double guardAngle = -Math.PI * 0.5 + (rng.NextDouble() * 0.8 - 0.4);
                V2 pos = center + new V2((float)Math.Cos(guardAngle) * 2.5f, (float)Math.Sin(guardAngle) * 2.5f);
                world.SpawnEnemy(new Enemy
                {
                    Pos = pos,
                    HP = 120f,
                    MaxHP = 120f,
                    Elite = true,
                    Kind = EnemyKind.Shade,
                    AttackCD = 1.2f
                });
            }
        }

        /// <summary>
        /// Attempts to open the Ancient Relic Chest.
        /// Grants rare treasures (Void Tokens, rare gems, high-tier gear) creating high-stakes risk/reward.
        /// </summary>
        public static bool OpenRelicChest(EmberWorld world, Player player, Structure chest, Profile profile = null)
        {
            if (world == null || world.S == null || player == null || chest == null) return false;
            if (chest.Kind != RelicChestKind || chest.HP <= 0) return false;

            // Check if player is alive and in range
            if (player.Ghost || player.Extracted) return false;
            if (V2.Distance(player.Pos, chest.Pos) > RelicInteractionDistance + 0.5f) return false;

            // 1. Grant Void Tokens (Profile meta progression)
            if (profile != null)
            {
                profile.Tokens += 3;
            }

            // 2. Grant rare resources (Dust, Core, Umbra, Ore)
            if (player.Bag != null && player.Bag.Length >= 5)
            {
                player.Bag[(int)Resource.Dust] += 30;
                player.Bag[(int)Resource.Core] += 4;
                player.Bag[(int)Resource.Umbra] += 5;
                player.Bag[(int)Resource.Ore] += 15;
            }

            // 3. Grant High-Tier Gear / Relic Lantern
            player.Lantern = Lantern.Relic;
            player.CarriesLantern = true;

            // Upgrade weapon if basic
            if (player.Weapon == Weapon.Stick)
            {
                player.Weapon = Weapon.CrystalAxe;
            }
            else if (player.Weapon == Weapon.Pickaxe && player.PickaxeTier < 2)
            {
                player.PickaxeTier = 2; // Crystal Pickaxe
                player.PickaxeDurability = 150;
            }

            // 4. Restore HP & Dread relief
            player.HP = Math.Min(player.MaxHP, player.HP + 50f);
            player.Dread = Math.Max(0f, player.Dread - 60f);

            // 5. Emit popups & burst FX
            world.EmitPopupDirect(new CombatPopup { Kind = 2, Amount = 50, Pos = chest.Pos, Life = 2f, MaxLife = 2f });
            world.EmitPopupDirect(new CombatPopup { Kind = 3, Amount = 4, SubKind = (int)Resource.Core, Pos = chest.Pos + new V2(0, 0.8f), Life = 2.5f, MaxLife = 2.5f });
            world.EmitPopupDirect(new CombatPopup { Kind = 3, Amount = 5, SubKind = (int)Resource.Umbra, Pos = chest.Pos + new V2(0.6f, 1.2f), Life = 2.5f, MaxLife = 2.5f });

            // Mark chest consumed
            chest.HP = 0;
            chest.Life = 0;
            world.RemoveStructureById(chest.Id);

            HasActiveRelic = false;
            TotalRelicsClaimed++;

            // Broadcast announcement
            string openVi = "Đã khai mở Hộp Cổ Thất Lạc! Đội thám hiểm nhận 3 Void Tokens, Lõi & Mảnh Tối quý hiếm!";
            string openEn = "Ancient Relic Opened! Recovered 3 Void Tokens, Rare Cores & Umbra Shards!";
            world.S.EventActor = player.Name;
            world.AddWorldEvent(openVi, openEn);

            return true;
        }

        /// <summary>
        /// Finds the nearest active Relic Chest if one exists.
        /// </summary>
        public static Structure GetNearestRelicChest(Snapshot state, V2 pos)
        {
            if (state?.Structures == null) return null;
            Structure nearest = null;
            float minDist = float.MaxValue;
            foreach (var s in state.Structures)
            {
                if (s.Kind == RelicChestKind && s.HP > 0)
                {
                    float d = V2.Distance(pos, s.Pos);
                    if (d < minDist)
                    {
                        minDist = d;
                        nearest = s;
                    }
                }
            }
            return nearest;
        }
    }

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    public sealed partial class EmberGame
    {
        GUIStyle relicTitleStyle, relicSubStyle, relicPromptStyle;

        void EnsureRelicStyles()
        {
            if (relicTitleStyle != null) return;
            relicTitleStyle = new GUIStyle(title)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 17,
                normal = { textColor = new Color(1f, 0.88f, 0.42f) }
            };
            relicSubStyle = new GUIStyle(small)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                normal = { textColor = new Color(0.85f, 0.92f, 1f) }
            };
            relicPromptStyle = new GUIStyle(small)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                normal = { textColor = new Color(1f, 0.95f, 0.65f) }
            };
        }

        /// <summary>
        /// Draws dramatic cosmic beacon alerts, lightning strikes, and relic indicators.
        /// </summary>
        public void DrawRelicHud()
        {
            if (State == null) return;
            EnsureRelicStyles();

            // 1. Cosmic Beacon Drop Alert Banner
            if (EmberEvents.RelicAlertTimer > 0)
            {
                float fade = Mathf.Clamp01(EmberEvents.RelicAlertTimer / 0.8f);
                float flash = 0.8f + 0.2f * Mathf.Sin(clock * 14f);
                float w = Mathf.Min(560f, Screen.width - 32f);
                float y = State.BossArenaActive ? 180f : 52f;
                var banner = new Rect((Screen.width - w) * 0.5f, y, w, 56f);

                // Cosmic border with pulsing cyan/gold glow
                Color border = new Color(0.35f, 0.85f, 1f, 0.92f * fade * flash);
                Panel(new Rect(banner.x - 3, banner.y - 3, banner.width + 6, banner.height + 6), border);
                Panel(banner, new Color(0.08f, 0.05f, 0.18f, 0.94f * fade));

                GUI.color = new Color(1f, 1f, 1f, fade);
                string titleText = EmberLanguage.English
                    ? "⚡ COSMIC BEACON · ANCIENT RELIC CRASHED ⚡"
                    : "⚡ CẢNH BÁO THIÊN THỂ · HỘP CỔ THẤT LẠC ĐÃ GIÁNG THẾ ⚡";
                GUI.Label(new Rect(banner.x + 10, banner.y + 4, banner.width - 20, 24), titleText, relicTitleStyle);

                string detailText = EmberLanguage.English
                    ? "A mythical relic chest fell into the deep boundary dark. Guarded by shadow fiends!"
                    : "Rương cổ thần thoại đã rơi tại vùng tối rìa thế giới. Hãy đánh bại quái vật bóng tối để đoạt bảo vật!";
                GUI.Label(new Rect(banner.x + 10, banner.y + 28, banner.width - 20, 20), detailText, relicSubStyle);
                GUI.color = Color.white;
            }

            // 2. Proximity interact prompt for Relic Chest
            var localPlayer = Local;
            if (localPlayer != null && !localPlayer.Ghost && State.Structures != null)
            {
                var chest = EmberEvents.GetNearestRelicChest(State, localPlayer.Pos);
                if (chest != null && V2.Distance(localPlayer.Pos, chest.Pos) <= EmberEvents.RelicInteractionDistance)
                {
                    Vector2 sp = ScreenPos(chest.Pos);
                    var promptRect = new Rect(sp.x - 130f, sp.y - 52f, 260f, 26f);
                    float pulse = 0.82f + 0.18f * Mathf.Sin(clock * 10f);

                    Panel(promptRect, new Color(0.12f, 0.06f, 0.22f, 0.90f));
                    Panel(new Rect(promptRect.x, promptRect.y, promptRect.width, 2f), new Color(1f, 0.84f, 0.2f, pulse));
                    Panel(new Rect(promptRect.x, promptRect.yMax - 2f, promptRect.width, 2f), new Color(1f, 0.84f, 0.2f, pulse));

                    string prompt = EmberLanguage.English
                        ? "[E] Open Ancient Relic Chest"
                        : "[E] Khai Mở Hộp Cổ Thất Lạc";
                    GUI.Label(promptRect, "✦  " + prompt + "  ✦", relicPromptStyle);

                    // Check interaction key - send authoritative Interact intent to host
                    if (Input.GetKeyDown(KeyCode.E) || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.E))
                    {
                        intent.Interact = true;
                    }
                }
            }
        }

        /// <summary>
        /// World-space visual effects: Cosmic beam pillar, lightning strike bolts, and glowing chest rings.
        /// </summary>
        public void DrawRelicWorldEffects()
        {
            if (State == null || Event.current.type != EventType.Repaint) return;

            float pulse = 0.75f + 0.25f * Mathf.Sin(clock * 6f);

            // Draw glowing rings around active Relic Chests
            if (State.Structures != null)
            {
                foreach (var s in State.Structures)
                {
                    if (s.Kind == EmberEvents.RelicChestKind && s.HP > 0)
                    {
                        // Pulsing celestial aura
                        GlowRing(s.Pos, 1.6f + 0.15f * pulse, new Color(1f, 0.84f, 0.2f, 0.88f * pulse), 3f);
                        Ring(s.Pos, 2.2f, new Color(0.65f, 0.35f, 1f, 0.60f * pulse), 1.8f);

                        // Vertical cosmic beam rising into sky
                        Vector2 basePos = ScreenPos(s.Pos);
                        Vector2 topPos = new Vector2(basePos.x, basePos.y - 280f);
                        GUI.color = new Color(0.4f, 0.85f, 1f, 0.35f * pulse);
                        Line(basePos, topPos, 4f);
                        GUI.color = new Color(1f, 0.9f, 0.4f, 0.55f * pulse);
                        Line(basePos, topPos, 2f);
                        GUI.color = Color.white;
                    }
                }
            }

            // Draw active lightning strike animations
            for (int i = 0; i < EmberEvents.ActiveLightnings.Count; i++)
            {
                var bolt = EmberEvents.ActiveLightnings[i];
                float progress = Mathf.Clamp01(bolt.Timer / bolt.MaxDuration);
                Vector2 target = ScreenPos(bolt.TargetPos);
                Vector2 sky = new Vector2(target.x, target.y - Screen.height * 0.85f);

                GUI.color = new Color(1f, 1f, 1f, progress);
                Line(sky, target, bolt.BeamWidth * (progress * 6f + 1f));
                GUI.color = bolt.Color;
                GlowRing(bolt.TargetPos, 3.5f * (1f - progress), bolt.Color, 2.5f);
                GUI.color = Color.white;
            }
        }
    }
#endif
}
