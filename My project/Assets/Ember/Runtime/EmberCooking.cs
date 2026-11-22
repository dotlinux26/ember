using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace Ember
{
    /// <summary>
    /// Campfire Cooking (Nấu nướng Dã ngoại quanh Lửa trại/Trụ):
    /// - Raw food (Apple, Mushroom) dropped within 3 tiles of an active Beacon or Campfire / Torch light.
    /// - After a 4-5s cooking timer (with rising steam/smoke FX), transforms into 'Roasted Apple' or 'Mushroom Stew'.
    /// - Consuming cooked food restores +45 HP and grants 'No ấm' (Well Fed): +50% Dread Resistance for 45s.
    /// </summary>
    public static class EmberCooking
    {
        public const float CookingRadius = 3.0f;
        public const float CookDuration = 4.5f; // 4-5 seconds
        public const int ItemEnergyDrink = 14;
        public const int ItemApple = 15;
        public const int ItemMushroom = 16;
        public const int ItemRoastedApple = 17;
        public const int ItemMushroomStew = 18;

        public const float CookedFoodHeal = 45f;
        public const float RawAppleHeal = 25f;
        public const float WellFedDuration = 45f;
        public const float WellFedDreadResistance = 0.5f; // +50% dread resistance (halves dread gain)

        /// <summary>
        /// Check if a dropped node represents raw cookable food.
        /// </summary>
        public static bool IsRawFood(int dropKind, int dropId)
        {
            if (dropKind == 2)
            {
                return dropId == ItemApple || dropId == ItemMushroom || dropId == 5 || dropId == 6;
            }
            return false;
        }

        /// <summary>
        /// Returns the cooked item ID for a given raw food ID.
        /// </summary>
        public static int GetCookedTransformId(int dropId)
        {
            if (dropId == ItemApple || dropId == 5) return ItemRoastedApple;
            if (dropId == ItemMushroom || dropId == 6) return ItemMushroomStew;
            return dropId;
        }

        /// <summary>
        /// Check if a position is within 3 tiles of an active Beacon, Campfire/Torch, or heat source.
        /// </summary>
        public static bool IsNearHeatSource(Snapshot s, V2 pos, IEnumerable<Player> players = null, IEnumerable<TransientLight> lights = null)
        {
            if (s == null) return false;

            // 1. Active Beacon: center (0, 0) within 3 tiles
            bool beaconActive = (s.Charge > 0 || s.BeaconHP > 0 || s.Fuel > 0 || s.Phase == Phase.Explore || s.Phase == Phase.Sanctuary);
            if (beaconActive && pos.Length <= CookingRadius) return true;

            // 2. Campfire / Torch / Flare structures within 3 tiles
            if (s.Structures != null)
            {
                for (int i = 0; i < s.Structures.Length; i++)
                {
                    var st = s.Structures[i];
                    if (st != null && (st.Kind == 0 || st.Kind == 1) && st.Life > 0 && st.HP > 0)
                    {
                        if (V2.Distance(pos, st.Pos) <= CookingRadius) return true;
                    }
                }
            }

            // 3. Players carrying an active lit torch or lantern
            if (players != null)
            {
                foreach (var p in players)
                {
                    if (p != null && !p.Ghost && !p.Extracted && p.HasHeldTorch && (s.Fuel > 0 || s.LastLight > 0))
                    {
                        if (V2.Distance(pos, p.Pos) <= CookingRadius) return true;
                    }
                }
            }

            // 4. Transient lights granting safety
            if (lights != null)
            {
                foreach (var l in lights)
                {
                    if (l != null && l.GrantsSafety && l.Life > 0 && V2.Distance(pos, l.Pos) <= CookingRadius)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Ticks campfire cooking logic for all dropped items.
        /// </summary>
        public static void Update(EmberWorld world, float dt)
        {
            if (world == null) return;
            world.UpdateCooking(dt);
        }

        /// <summary>
        /// Handles item consumption from inventory or quickslots.
        /// </summary>
        public static bool HandleUseItem(EmberWorld world, Player p, int item)
        {
            if (p == null || p.Ghost || p.Items == null) return false;

            // Map quickslot indices 4..8 to canonical item IDs 14..18 if applicable
            int canonicalId = item;
            if (item == 4) canonicalId = ItemEnergyDrink;
            else if (item == 5) canonicalId = ItemApple;
            else if (item == 6) canonicalId = ItemMushroom;
            else if (item == 7) canonicalId = ItemRoastedApple;
            else if (item == 8) canonicalId = ItemMushroomStew;

            // Ensure player has the item in either slot
            int slotToConsume = -1;
            if (canonicalId < p.Items.Length && p.Items[canonicalId] > 0)
                slotToConsume = canonicalId;
            else if (item < p.Items.Length && p.Items[item] > 0)
                slotToConsume = item;

            if (slotToConsume < 0) return false;

            // 1. Roasted Apple (+45 HP + No ấm / Well Fed)
            if (canonicalId == ItemRoastedApple)
            {
                p.Items[slotToConsume]--;
                p.HP = Math.Min(p.MaxHP, p.HP + CookedFoodHeal);
                p.WarmthBuffTimer = Math.Max(p.WarmthBuffTimer, WellFedDuration);
                p.ApplyBuff(BuffKind.DreadWard, WellFedDuration, 0.5f);
                if (world != null)
                {
                    world.EmitPopup(p.Pos, 2, (int)CookedFoodHeal);
                    world.FX(p.Pos, p.Pos, 1);
                    string name = Catalog.GetItemDisplayName(ItemRoastedApple);
                    string msg = EmberLanguage.English
                        ? $"Ate {name}: +45 HP! 'Well Fed' active (+50% Dread Resistance for 45s)!"
                        : $"Đã ăn {name}: +45 HP! Nhận hiệu ứng 'No ấm' (+50% Kháng sợ hãi trong 45s)!";
                    world.Announce(msg, p.Name);
                }
                return true;
            }

            // 2. Mushroom Stew (+45 HP + No ấm / Well Fed)
            if (canonicalId == ItemMushroomStew)
            {
                p.Items[slotToConsume]--;
                p.HP = Math.Min(p.MaxHP, p.HP + CookedFoodHeal);
                p.WarmthBuffTimer = Math.Max(p.WarmthBuffTimer, WellFedDuration);
                p.ApplyBuff(BuffKind.DreadWard, WellFedDuration, 0.5f);
                if (world != null)
                {
                    world.EmitPopup(p.Pos, 2, (int)CookedFoodHeal);
                    world.FX(p.Pos, p.Pos, 1);
                    string name = Catalog.GetItemDisplayName(ItemMushroomStew);
                    string msg = EmberLanguage.English
                        ? $"Ate {name}: +45 HP! 'Well Fed' active (+50% Dread Resistance for 45s)!"
                        : $"Đã ăn {name}: +45 HP! Nhận hiệu ứng 'No ấm' (+50% Kháng sợ hãi trong 45s)!";
                    world.Announce(msg, p.Name);
                }
                return true;
            }

            // 3. Raw Apple (+25 HP)
            if (canonicalId == ItemApple)
            {
                p.Items[slotToConsume]--;
                p.HP = Math.Min(p.MaxHP, p.HP + RawAppleHeal);
                if (world != null)
                {
                    world.EmitPopup(p.Pos, 2, (int)RawAppleHeal);
                    world.FX(p.Pos, p.Pos, 1);
                    string name = Catalog.GetItemDisplayName(ItemApple);
                    string msg = EmberLanguage.English
                        ? $"Ate fresh {name}: +25 HP!"
                        : $"Đã ăn {name}: +25 HP!";
                    world.Announce(msg, p.Name);
                }
                return true;
            }

            // 4. Energy Drink (+35% move speed, infinite stamina 30s)
            if (canonicalId == ItemEnergyDrink)
            {
                p.Items[slotToConsume]--;
                p.StaminaBuffTimer = Math.Max(p.StaminaBuffTimer, 30f);
                p.ApplyBuff(BuffKind.Swiftness, 30f, 0.35f);
                p.Stamina = 100f;
                if (world != null)
                {
                    world.FX(p.Pos, p.Pos, 2);
                    string name = Catalog.GetItemDisplayName(ItemEnergyDrink);
                    string msg = EmberLanguage.English
                        ? $"Drank {name}: +35% Speed & Infinite Stamina for 30s!"
                        : $"Đã uống {name}: +35% Tốc chạy & Vô hạn Thể lực trong 30s!";
                    world.Announce(msg, p.Name);
                }
                return true;
            }

            return false;
        }
    }

    public sealed partial class EmberWorld
    {
        /// <summary>
        /// Simulation tick for campfire cooking. Runs on host/server.
        /// </summary>
        public void UpdateCooking(float dt)
        {
            if (nodes == null || nodes.Count == 0) return;

            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (n == null || !n.Dropped) continue;
                if (!EmberCooking.IsRawFood(n.DropKind, n.DropId)) continue;

                bool nearHeat = EmberCooking.IsNearHeatSource(S, n.Pos, players, transientLights);
                if (nearHeat)
                {
                    n.CookTimer += dt;

                    // Steam / smoke rising FX during cooking (Kind 8 = steam)
                    if (rng.NextDouble() < 0.28)
                    {
                        FX(n.Pos, n.Pos + new V2(0, 0.45f), 8);
                    }

                    // Cooking finished! Transform into roasted / stew
                    if (n.CookTimer >= EmberCooking.CookDuration)
                    {
                        int cookedId = EmberCooking.GetCookedTransformId(n.DropId);
                        n.DropId = cookedId;
                        n.CookTimer = 0f;

                        // Burst FX & notification
                        FX(n.Pos, n.Pos, 2);
                        string foodName = Catalog.GetItemDisplayName(cookedId);
                        string msg = EmberLanguage.English
                            ? $"Campfire cooking complete: {foodName} ready!"
                            : $"Nấu nướng dã ngoại hoàn tất: Đã chín {foodName}!";
                        Announce(msg);
                        EmitPopup(n.Pos, 2, (int)EmberCooking.CookedFoodHeal);
                    }
                }
                else if (n.CookTimer > 0f)
                {
                    // Slowly cool down if removed from heat
                    n.CookTimer = Math.Max(0f, n.CookTimer - dt * 0.5f);
                }
            }
        }

        /// <summary>
        /// Helper to spawn a ground food node for campfire cooking tests or gameplay.
        /// </summary>
        public Node SpawnGroundFood(int itemId, V2 pos, int amount = 1)
        {
            var node = new Node
            {
                Id = nextId++,
                Pos = pos,
                Resource = Resource.Wood,
                Amount = amount,
                MaxAmount = amount,
                Dropped = true,
                DropKind = 2,
                DropId = itemId,
                CookTimer = 0f
            };
            nodes.Add(node);
            Publish();
            return node;
        }

        /// <summary>
        /// Direct helper to consume cooked food and apply +45 HP & Well Fed buff.
        /// </summary>
        public bool ConsumeCookedFood(Player p, int itemId)
        {
            return EmberCooking.HandleUseItem(this, p, itemId);
        }
    }

#if UNITY_5_3_OR_NEWER
    public sealed partial class EmberGame
    {
        static readonly string[] CookDotStrings = { "", ".", "..", "..." };
        GUIStyle cookBarLabelStyle;

        /// <summary>
        /// Visual overhead progress bar and rising steam puffs for cooking items.
        /// </summary>
        void DrawCookingBars()
        {
            if (Event.current.type != EventType.Repaint || State?.Nodes == null) return;
            V2 focus = CameraTarget()?.Pos ?? new V2();
            if (cookBarLabelStyle == null)
            {
                cookBarLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.9f, 0.7f, 0.95f) }
                };
            }
            if (small?.font != null && cookBarLabelStyle.font != small.font) cookBarLabelStyle.font = small.font;

            for (int i = 0; i < State.Nodes.Length; i++)
            {
                var n = State.Nodes[i];
                if (n == null || !n.Dropped) continue;
                if (!EmberCooking.IsRawFood(n.DropKind, n.DropId)) continue;
                if (n.CookTimer <= 0f || V2.Distance(n.Pos, focus) > 20) continue;

                float progress = Mathf.Clamp01(n.CookTimer / EmberCooking.CookDuration);
                Vector2 anchor = ScreenPos(n.Pos + new V2(0, 0.65f));
                float x = Mathf.Round(anchor.x);
                float y = Mathf.Round(anchor.y);

                // Small cooking progress bar
                Panel(new Rect(x - 18, y - 6, 36, 6), new Color(0.06f, 0.04f, 0.08f, 0.9f));
                Color barFill = Color.Lerp(new Color(1f, 0.55f, 0.2f), new Color(0.35f, 0.95f, 0.4f), progress);
                Panel(new Rect(x - 17, y - 5, 34 * progress, 4), barFill);

                // Cooking label / steam animation
                int steamDot = (int)(Time.time * 3f) % 4;
                string dots = CookDotStrings[steamDot];
                string cookText = L("Đang nấu" + dots, "Cooking" + dots);
                GUI.Label(new Rect(x - 30, y - 20, 60, 14), cookText, cookBarLabelStyle);
            }
        }

        /// <summary>
        /// Render cooking steam rising rings for Effect Kind == 8.
        /// </summary>
        void DrawCookingSteamEffect(Effect e)
        {
            float t = 1f - Mathf.Clamp01(e.Life / 0.3f);
            float radius = 0.2f + t * 0.4f;
            float alpha = (1f - t) * 0.65f;
            Color steamColor = new Color(0.92f, 0.92f, 0.96f, alpha);
            Ring(e.From + new V2(Mathf.Sin(Time.time * 6f + e.From.X) * 0.1f, t * 0.5f), radius, steamColor);
        }
    }
#endif
}
