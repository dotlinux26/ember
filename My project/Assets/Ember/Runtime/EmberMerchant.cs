using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
using UnityEngine;
#endif

namespace Ember
{
    /// <summary>
    /// Merchant Reputation & Secret Stash (Thương nhân Danh Vọng & Cửa hàng Bí mật):
    /// - Trading with the Wandering Merchant builds reputation points (+15 pts per trade).
    /// - High reputation (>= 50) unlocks secret forbidden wares:
    ///   * Invisibility Cloak / Shadow Veil (invisibility to normal monsters for 15s)
    ///   * Treasure Compass (points to nearest hidden resource cache / Ancient Relic Chest)
    ///   * Void Draught (resets dread, restores full stamina, +40% speed)
    /// </summary>
    public static class EmberMerchant
    {
        public const int MerchantKind = 10;
        public const float MerchantInteractionDistance = 2.4f;
        public const int SecretStashReputationThreshold = 50;
        public const int ReputationPerTrade = 15;

        // Persistent reputation points (session / match level)
        public static int Reputation = 0;
        public static int TotalTradesCount = 0;
        public static bool DialogOpen = false;

        // Track active Invisibility / Shadow Veil timers per player
        public static readonly Dictionary<int, float> ShadowVeilTimers = new Dictionary<int, float>();

        // Track active Treasure Compass ownership per player
        public static readonly Dictionary<int, bool> CompassOwned = new Dictionary<int, bool>();

        /// <summary>
        /// Defines an offer in the Wandering Merchant catalog.
        /// </summary>
        public sealed class TradeOffer
        {
            public int Index;
            public string NameVi;
            public string NameEn;
            public string DescriptionVi;
            public string DescriptionEn;
            public int[] Cost; // [Dust, Core, Umbra, Wood, Ore]
            public int RequiredReputation;
            public bool IsSecretWare;
            public string RewardIcon;
            public string RewardLabelVi;
            public string RewardLabelEn;
        }

        public static readonly TradeOffer[] Catalog = new TradeOffer[]
        {
            // Standard Trade 0: 8 Dust -> 1 Torch
            new TradeOffer
            {
                Index = 0,
                NameVi = "Đuốc Sinh Mệnh",
                NameEn = "Lumen Torch",
                DescriptionVi = "Đuốc chiếu sáng xua tan bóng tối và giữ an toàn.",
                DescriptionEn = "A reliable torch to pierce the dark and protect life.",
                Cost = new int[] { 8, 0, 0, 0, 0 },
                RequiredReputation = 0,
                IsSecretWare = false,
                RewardIcon = "torch",
                RewardLabelVi = "+1 Đuốc",
                RewardLabelEn = "+1 Torch"
            },

            // Standard Trade 1: 6 Ore -> 1 Energy Drink
            new TradeOffer
            {
                Index = 1,
                NameVi = "Nước Tăng Lực",
                NameEn = "Energy Drink",
                DescriptionVi = "Bình nước tăng lực: Gia tốc +35% và thể lực vô tận trong 30 giây.",
                DescriptionEn = "Stamina potion: +35% movement speed and infinite stamina for 30s.",
                Cost = new int[] { 0, 0, 0, 0, 6 },
                RequiredReputation = 0,
                IsSecretWare = false,
                RewardIcon = "stamina",
                RewardLabelVi = "+1 Nước Tăng Lực",
                RewardLabelEn = "+1 Energy Drink"
            },

            // Standard Trade 2: 8 Wood -> 2 Apples
            new TradeOffer
            {
                Index = 2,
                NameVi = "Táo Rừng Tươi",
                NameEn = "Forest Apples",
                DescriptionVi = "Hai quả táo rừng ngọt ngào, ăn trực tiếp hồi 25 HP hoặc dùng nướng.",
                DescriptionEn = "Two crisp apples to restore 25 HP or cook over a campfire.",
                Cost = new int[] { 0, 0, 0, 8, 0 },
                RequiredReputation = 0,
                IsSecretWare = false,
                RewardIcon = "apple",
                RewardLabelVi = "+2 Quả Táo",
                RewardLabelEn = "+2 Apples"
            },

            // Standard Trade 3: 1 Core -> 2 Revival Stones
            new TradeOffer
            {
                Index = 3,
                NameVi = "Đá Hồi Sinh",
                NameEn = "Revival Stones",
                DescriptionVi = "Cặp đá thần bí hồi sinh đồng đội đã gục ngã tại trận địa.",
                DescriptionEn = "A pair of mystical stones to revive fallen comrades on site.",
                Cost = new int[] { 0, 1, 0, 0, 0 },
                RequiredReputation = 0,
                IsSecretWare = false,
                RewardIcon = "revive",
                RewardLabelVi = "+2 Đá Hồi Sinh",
                RewardLabelEn = "+2 Revival Stones"
            },

            // Secret Stash 4: Invisibility Cloak / Shadow Veil (2 Umbra + 1 Core)
            new TradeOffer
            {
                Index = 4,
                NameVi = "Áo Choàng Hư Không (Shadow Veil)",
                NameEn = "Shadow Veil (Invisibility)",
                DescriptionVi = "Bí pháp bóng tối: Tàng hình hoàn toàn với quái vật thông thường trong 15 giây. Quái vật không thể phát hiện hay truy đuổi bạn!",
                DescriptionEn = "Forbidden stealth ware: Become completely invisible to normal monsters for 15 seconds! Monsters cannot target or pursue you.",
                Cost = new int[] { 0, 1, 2, 0, 0 },
                RequiredReputation = SecretStashReputationThreshold,
                IsSecretWare = true,
                RewardIcon = "cloak",
                RewardLabelVi = "Tàng Hình 15s",
                RewardLabelEn = "Invisibility 15s"
            },

            // Secret Stash 5: Treasure Compass (5 Ore + 1 Core)
            new TradeOffer
            {
                Index = 5,
                NameVi = "La Bàn Kho Báu (Treasure Compass)",
                NameEn = "Treasure Compass",
                DescriptionVi = "La bàn cổ tự động định vị và chỉ hướng tới Hộp Cổ Thất Lạc hoặc mỏ Lõi / Mảnh Tối quý giá gần nhất!",
                DescriptionEn = "Ancient directional compass pointing to the nearest Ancient Relic Chest or high-value Core / Umbra deposit!",
                Cost = new int[] { 0, 1, 0, 0, 5 },
                RequiredReputation = SecretStashReputationThreshold,
                IsSecretWare = true,
                RewardIcon = "compass",
                RewardLabelVi = "La Bàn Định Vị",
                RewardLabelEn = "Treasure Compass"
            },

            // Secret Stash 6: Void Draught (3 Umbra + 10 Dust)
            new TradeOffer
            {
                Index = 6,
                NameVi = "Thần Dược Vực Thẳm",
                NameEn = "Void Draught",
                DescriptionVi = "Xóa sạch 100% hoảng loạn, hồi đầy thể lực và tăng tốc chạy +40% trong 20 giây.",
                DescriptionEn = "Instantly clears all dread, restores full stamina, and grants +40% speed for 20 seconds.",
                Cost = new int[] { 10, 0, 3, 0, 0 },
                RequiredReputation = SecretStashReputationThreshold,
                IsSecretWare = true,
                RewardIcon = "draught",
                RewardLabelVi = "Thần Dược Vực Thẳm",
                RewardLabelEn = "Void Draught"
            }
        };

        /// <summary>
        /// Checks if a structure represents the Wandering Merchant.
        /// Accepts either MerchantKind (10) or Kind 5 as agreed.
        /// </summary>
        public static bool IsMerchant(Structure s)
        {
            return s != null && (s.Kind == MerchantKind || s.Kind == 5);
        }

        /// <summary>
        /// Checks whether player is invisible to normal monsters via Shadow Veil.
        /// </summary>
        public static bool IsInvisible(Player p)
        {
            if (p == null) return false;
            return ShadowVeilTimers.TryGetValue(p.Id, out float t) && t > 0f;
        }

        /// <summary>
        /// Returns remaining Invisibility / Shadow Veil duration in seconds.
        /// </summary>
        public static float GetInvisibilityDuration(int playerId)
        {
            return ShadowVeilTimers.TryGetValue(playerId, out float t) ? Math.Max(0f, t) : 0f;
        }

        /// <summary>
        /// Activates Shadow Veil (Invisibility) for player.
        /// </summary>
        public static void ApplyShadowVeil(Player p, float duration = 15f)
        {
            if (p == null) return;
            ShadowVeilTimers[p.Id] = Math.Max(GetInvisibilityDuration(p.Id), duration);
            p.ApplyBuff(BuffKind.Swiftness, duration, 0.25f);
        }

        /// <summary>
        /// Checks if player owns the Treasure Compass.
        /// </summary>
        public static bool HasTreasureCompass(int playerId)
        {
            return CompassOwned.TryGetValue(playerId, out bool owned) && owned;
        }

        /// <summary>
        /// Activates the Treasure Compass for player.
        /// </summary>
        public static void ActivateCompass(Player p)
        {
            if (p == null) return;
            CompassOwned[p.Id] = true;
        }

        /// <summary>
        /// Simulation tick update.
        /// </summary>
        public static void Update(EmberWorld world, float dt)
        {
            if (world == null || world.S == null) return;

            // Decrement active stealth timers
            if (ShadowVeilTimers.Count > 0)
            {
                var keys = ShadowVeilTimers.Keys.ToList();
                foreach (var k in keys)
                {
                    float next = ShadowVeilTimers[k] - dt;
                    if (next <= 0)
                        ShadowVeilTimers.Remove(k);
                    else
                        ShadowVeilTimers[k] = next;
                }
            }
        }

        /// <summary>
        /// Spawns a Wandering Merchant NPC structure safely near the Beacon.
        /// </summary>
        public static void SpawnMerchantNearBeacon(EmberWorld world, float life = 60f)
        {
            if (world?.S == null) return;
            int nextId = world.S.Structures != null && world.S.Structures.Length > 0
                ? world.S.Structures.Max(s => s.Id) + 1 : 10;

            // Place merchant ~3.5 tiles southeast of the Beacon
            V2 pos = new V2(3.2f, -2.4f);

            var merchantStruct = new Structure
            {
                Id = nextId,
                Kind = MerchantKind,
                Pos = pos,
                Life = life,
                MaxLife = life,
                HP = 1000f
            };

            var list = world.S.Structures != null ? world.S.Structures.ToList() : new List<Structure>();
            list.Add(merchantStruct);
            world.S.Structures = list.ToArray();
        }

        /// <summary>
        /// Checks if player can afford and execute a specific trade.
        /// </summary>
        public static bool CanTrade(Player p, int tradeIndex)
        {
            if (p == null || tradeIndex < 0 || tradeIndex >= Catalog.Length) return false;
            var trade = Catalog[tradeIndex];

            // Check reputation requirement (Secret Stash)
            if (Reputation < trade.RequiredReputation) return false;

            // Check resource costs
            if (p.Bag == null || p.Bag.Length < 5) return false;
            for (int r = 0; r < 5; r++)
            {
                if (p.Bag[r] < trade.Cost[r]) return false;
            }

            return true;
        }

        /// <summary>
        /// Executes a trade with the Wandering Merchant:
        /// Deducts resources, awards wares, increments reputation points (+15), and broadcasts announcement.
        /// </summary>
        public static bool ExecuteTrade(EmberWorld world, Player p, int tradeIndex, Profile profile = null)
        {
            if (!CanTrade(p, tradeIndex)) return false;
            var trade = Catalog[tradeIndex];

            // 1. Deduct resource cost
            for (int r = 0; r < 5; r++)
            {
                p.Bag[r] -= trade.Cost[r];
            }

            // 2. Deliver reward
            switch (tradeIndex)
            {
                case 0: // Torch
                    if (p.Items != null && p.Items.Length > 0) p.Items[0]++;
                    break;
                case 1: // Energy Drink
                    if (p.Items != null && p.Items.Length > 14) p.Items[14]++;
                    // Apply immediate stamina buff
                    p.ApplyBuff(BuffKind.Swiftness, 30f, 0.35f);
                    p.ApplyBuff(BuffKind.StaminaSurge, 30f, 1.0f);
                    p.Stamina = 100f;
                    break;
                case 2: // Apples
                    if (p.Items != null && p.Items.Length > 15) p.Items[15] += 2;
                    p.HP = Math.Min(p.MaxHP, p.HP + 25f);
                    break;
                case 3: // Revival Stones
                    if (p.Items != null && p.Items.Length > 3) p.Items[3] += 2;
                    break;
                case 4: // Invisibility Cloak / Shadow Veil
                    ApplyShadowVeil(p, 15f);
                    break;
                case 5: // Treasure Compass
                    ActivateCompass(p);
                    break;
                case 6: // Void Draught
                    p.Dread = 0f;
                    p.Stamina = 100f;
                    p.ApplyBuff(BuffKind.Swiftness, 20f, 0.40f);
                    p.HP = Math.Min(p.MaxHP, p.HP + 40f);
                    break;
            }

            // 3. Build Reputation Points
            bool wasLocked = Reputation < SecretStashReputationThreshold;
            Reputation += ReputationPerTrade;
            TotalTradesCount++;

            // 4. Popups & Announcements
            if (world?.S != null)
            {
                string tradeName = EmberLanguage.English ? trade.NameEn : trade.NameVi;
                string rewardLabel = EmberLanguage.English ? trade.RewardLabelEn : trade.RewardLabelVi;

                if (wasLocked && Reputation >= SecretStashReputationThreshold)
                {
                    string unlockVi = "✦ CỬA HÀNG BÍ MẬT CỦA THƯƠNG NHÂN ĐÃ MỞ KHÓA! (Danh vọng " + Reputation + " điểm) ✦";
                    string unlockEn = "✦ MERCHANT SECRET STASH UNLOCKED! (Reputation: " + Reputation + " pts) ✦";
                    world.S.Event = unlockVi + "|" + unlockEn;
                    world.S.EventTick = world.S.Tick;
                }
                else
                {
                    string msgVi = "Giao dịch thành công: " + tradeName + " (" + rewardLabel + ") · +15 Danh vọng!";
                    string msgEn = "Trade complete: " + tradeName + " (" + rewardLabel + ") · +15 Reputation!";
                    world.S.Event = msgVi + "|" + msgEn;
                    world.S.EventTick = world.S.Tick;
                }

                // Emit popups
                if (world.S.Popups != null)
                {
                    var popups = world.S.Popups.ToList();
                    int nextPopupId = popups.Count > 0 ? popups.Max(pop => pop.Id) + 1 : 1;
                    popups.Add(new CombatPopup { Id = nextPopupId++, Kind = 2, Amount = 15, Pos = p.Pos + new V2(0, 0.8f), Life = 2.2f, MaxLife = 2.2f });
                    world.S.Popups = popups.ToArray();
                }
            }

            return true;
        }

        /// <summary>
        /// Finds the nearest treasure (Ancient Relic Chest, Core Vein, or Umbra Deposit) for Treasure Compass.
        /// </summary>
        public static (string name, V2 pos, float distance)? FindNearestTreasure(Snapshot state, V2 playerPos)
        {
            if (state == null) return null;

            float minDistance = float.MaxValue;
            V2 targetPos = new V2(0, 0);
            string targetNameVi = "";
            string targetNameEn = "";

            // 1. Highest Priority: Active Ancient Relic Chest
            if (state.Structures != null)
            {
                foreach (var s in state.Structures)
                {
                    if (s.Kind == EmberEvents.RelicChestKind && s.HP > 0)
                    {
                        float d = V2.Distance(playerPos, s.Pos);
                        if (d < minDistance)
                        {
                            minDistance = d;
                            targetPos = s.Pos;
                            targetNameVi = "Hộp Cổ Thất Lạc (Relic)";
                            targetNameEn = "Ancient Relic Chest";
                        }
                    }
                }
            }

            // 2. Second Priority: Core node deposits
            if (minDistance > 500f && state.Nodes != null)
            {
                foreach (var n in state.Nodes)
                {
                    if (n.Resource == Resource.Core && n.Amount > 0)
                    {
                        float d = V2.Distance(playerPos, n.Pos);
                        if (d < minDistance)
                        {
                            minDistance = d;
                            targetPos = n.Pos;
                            targetNameVi = "Mỏ Lõi Năng Lượng";
                            targetNameEn = "Energy Core Vein";
                        }
                    }
                }
            }

            // 3. Third Priority: Umbra deposits
            if (minDistance > 500f && state.Nodes != null)
            {
                foreach (var n in state.Nodes)
                {
                    if (n.Resource == Resource.Umbra && n.Amount > 0)
                    {
                        float d = V2.Distance(playerPos, n.Pos);
                        if (d < minDistance)
                        {
                            minDistance = d;
                            targetPos = n.Pos;
                            targetNameVi = "Mảnh Tối Huyền Bí";
                            targetNameEn = "Umbra Deposit";
                        }
                    }
                }
            }

            if (minDistance < 5000f)
            {
                string name = EmberLanguage.English ? targetNameEn : targetNameVi;
                return (name, targetPos, minDistance);
            }

            return null;
        }
    }

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    public sealed partial class EmberGame
    {
        GUIStyle merchantTitleStyle, merchantSubStyle, merchantRepStyle, merchantCardStyle, merchantBtnStyle;

        void EnsureMerchantStyles()
        {
            if (merchantTitleStyle != null) return;
            merchantTitleStyle = new GUIStyle(title)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 17,
                normal = { textColor = new Color(1f, 0.90f, 0.45f) }
            };
            merchantSubStyle = new GUIStyle(small)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                normal = { textColor = new Color(0.85f, 0.85f, 0.95f) }
            };
            merchantRepStyle = new GUIStyle(label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                normal = { textColor = new Color(0.95f, 0.80f, 0.35f) }
            };
            merchantCardStyle = new GUIStyle(label)
            {
                fontSize = 11,
                normal = { textColor = Color.white }
            };
            merchantBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
        }

        /// <summary>
        /// Draws the Wandering Merchant overhead prompt, dialogue and trading shop window.
        /// </summary>
        public void DrawMerchantHud(Player p)
        {
            if (State == null || p == null) return;
            EnsureMerchantStyles();

            var merchant = State.Structures?.FirstOrDefault(EmberMerchant.IsMerchant);
            if (merchant == null) return;

            float dist = V2.Distance(p.Pos, merchant.Pos);

            // 1. Overhead Countdown Timer on Merchant head (thời gian còn lại trước khi biến mất)
            if (merchant.Life > 0)
            {
                Vector2 sp = ScreenPos(merchant.Pos + new V2(0f, 1.35f));
                float pulse = 0.85f + 0.15f * Mathf.Sin(clock * 5f);
                string text = EmberLanguage.English ? $"⏳ Depart: {merchant.Life:0}s" : $"⏳ Còn lại: {merchant.Life:0}s";
                float tw = Mathf.Clamp(small.CalcSize(new GUIContent(text)).x + 18, 96, 160);
                var tRect = new Rect(sp.x - tw * 0.5f, sp.y - 12f, tw, 22f);

                Panel(tRect, new Color(0.10f, 0.08f, 0.18f, 0.92f));
                Panel(new Rect(tRect.x, tRect.y, tRect.width, 1.5f), new Color(1f, 0.82f, 0.28f, pulse));
                Panel(new Rect(tRect.x, tRect.yMax - 1.5f, tRect.width, 1.5f), new Color(1f, 0.82f, 0.28f, pulse));

                float maxLife = merchant.MaxLife > 0 ? merchant.MaxLife : 60f;
                float ratio = Mathf.Clamp01(merchant.Life / maxLife);
                Panel(new Rect(tRect.x + 2, tRect.yMax - 3, (tRect.width - 4) * ratio, 2f), new Color(1f, 0.85f, 0.35f, 0.95f));

                var oldCol = GUI.contentColor;
                GUI.contentColor = new Color(1f, 0.90f, 0.40f);
                GUI.Label(new Rect(tRect.x + 2, tRect.y + 1, tRect.width - 4, 18), text, small);
                GUI.contentColor = oldCol;
            }

            // Close dialog if player walks away
            if (dist > EmberMerchant.MerchantInteractionDistance + 1.2f)
            {
                EmberMerchant.DialogOpen = false;
            }

            // 2. Full Trading Dialog Window
            if (EmberMerchant.DialogOpen)
            {
                DrawMerchantTradingWindow(p);
            }

            // 3. Treasure Compass Indicator (if active)
            if (EmberMerchant.HasTreasureCompass(p.Id))
            {
                DrawTreasureCompassHud(p);
            }

            // 4. Shadow Veil Stealth Aura
            float veilTime = EmberMerchant.GetInvisibilityDuration(p.Id);
            if (veilTime > 0)
            {
                DrawShadowVeilHud(p, veilTime);
            }
        }

        void DrawMerchantTradingWindow(Player p)
        {
            float w = Mathf.Min(640f, Screen.width - 24f);
            float h = Mathf.Min(560f, Screen.height - 30f);
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;
            var box = new Rect(x, y, w, h);
            RegisterHudRect(box);

            // Dialog Background
            Panel(new Rect(box.x - 3, box.y - 3, box.width + 6, box.height + 6), new Color(0.85f, 0.70f, 0.25f, 0.90f));
            Panel(box, new Color(0.09f, 0.08f, 0.16f, 0.96f));

            // Header Title
            string titleVi = "THƯƠNG NHÂN LANG THANG · CỬA HÀNG BÍ MẬT";
            string titleEn = "WANDERING MERCHANT · SECRET STASH";
            GUI.Label(new Rect(box.x + 12, box.y + 8, box.width - 24, 24), EmberLanguage.English ? titleEn : titleVi, merchantTitleStyle);

            // Close button [X]
            var closeBtnRect = new Rect(box.xMax - 38f, box.y + 7f, 30f, 26f);
            bool closeHover = closeBtnRect.Contains(Event.current.mousePosition);
            Panel(closeBtnRect, closeHover ? new Color(0.85f, 0.22f, 0.25f, 0.95f) : new Color(0.28f, 0.12f, 0.20f, 0.90f));
            Panel(new Rect(closeBtnRect.x, closeBtnRect.y, closeBtnRect.width, 1.5f), new Color(1f, 0.45f, 0.45f, 0.85f));
            Panel(new Rect(closeBtnRect.x, closeBtnRect.yMax - 1.5f, closeBtnRect.width, 1.5f), new Color(1f, 0.45f, 0.45f, 0.85f));
            var prevContentCol = GUI.contentColor;
            GUI.contentColor = Color.white;
            if (GUI.Button(closeBtnRect, "[X]", merchantBtnStyle))
            {
                EmberMerchant.DialogOpen = false;
                Event.current?.Use();
            }
            GUI.contentColor = prevContentCol;

            // Reputation Meter
            int rep = EmberMerchant.Reputation;
            bool secretUnlocked = rep >= EmberMerchant.SecretStashReputationThreshold;
            string tierName = rep >= 100
                ? (EmberLanguage.English ? "Shadow Confidant" : "Tri Kỷ Hắc Ám")
                : secretUnlocked
                ? (EmberLanguage.English ? "Forbidden Circle Member" : "Hội Viên Cấm Giới")
                : rep >= 30
                ? (EmberLanguage.English ? "Trusted Customer" : "Khách Thân Quen")
                : (EmberLanguage.English ? "Wandering Visitor" : "Khách Vãng Lai");

            var repRect = new Rect(box.x + 16, box.y + 36, box.width - 32, 28);
            Panel(repRect, new Color(0.16f, 0.13f, 0.25f, 0.88f));
            string repText = EmberLanguage.English
                ? $"⭐ Merchant Reputation: {rep} pts ({tierName}) · [Secret Stash: {(secretUnlocked ? "UNLOCKED" : "LOCKED (Requires 50 pts)")}]"
                : $"⭐ Điểm Danh Vọng: {rep} ({tierName}) · [Cửa Hàng Bí Mật: {(secretUnlocked ? "ĐÃ MỞ KHÓA" : "CHƯA MỞ (Cần 50 điểm)")}]";
            GUI.Label(repRect, repText, merchantRepStyle);

            // Catalog Items List
            float curY = box.y + 70f;
            for (int i = 0; i < EmberMerchant.Catalog.Length; i++)
            {
                var trade = EmberMerchant.Catalog[i];
                var card = new Rect(box.x + 16, curY, box.width - 32, 60f);

                bool unlocked = rep >= trade.RequiredReputation;
                bool canAfford = EmberMerchant.CanTrade(p, i);

                Color borderCol = trade.IsSecretWare
                    ? (unlocked ? new Color(0.72f, 0.35f, 1f, 0.95f) : new Color(0.40f, 0.25f, 0.55f, 0.65f))
                    : new Color(0.35f, 0.55f, 0.75f, 0.80f);

                Panel(new Rect(card.x - 1, card.y - 1, card.width + 2, card.height + 2), borderCol);
                Panel(card, trade.IsSecretWare ? new Color(0.14f, 0.08f, 0.22f, 0.92f) : new Color(0.12f, 0.11f, 0.19f, 0.92f));

                if (!unlocked)
                {
                    // Locked Secret Ware Banner
                    GUI.Label(new Rect(card.x + 10, card.y + 18, card.width - 140, 24),
                        L($"🔒 CỬA HÀNG BÍ MẬT: {trade.NameVi} (Cần {trade.RequiredReputation} Danh vọng)",
                          $"🔒 SECRET STASH: {trade.NameEn} (Requires {trade.RequiredReputation} Rep)"),
                        merchantSubStyle);
                }
                else
                {
                    // Item Name & Reward
                    string name = EmberLanguage.English ? trade.NameEn : trade.NameVi;
                    string reward = EmberLanguage.English ? trade.RewardLabelEn : trade.RewardLabelVi;
                    string desc = EmberLanguage.English ? trade.DescriptionEn : trade.DescriptionVi;

                    GUI.Label(new Rect(card.x + 10, card.y + 6, card.width - 150, 20),
                        $"<b>{name}</b>  <color=#f7b538>➔ {reward}</color>", merchantCardStyle);
                    GUI.Label(new Rect(card.x + 10, card.y + 26, card.width - 150, 30), desc, merchantSubStyle);

                    // Resource Cost display
                    string costStr = BuildCostString(trade.Cost);
                    GUI.Label(new Rect(card.x + 10, card.y + 42, card.width - 150, 16),
                        $"<color=#a0d8ef>Chi phí: {costStr}</color>", merchantSubStyle);

                    // Trade Button
                    var btnRect = new Rect(card.xMax - 115, card.y + 14, 105, 32);
                    GUI.enabled = canAfford;
                    string btnLabel = EmberLanguage.English ? "Trade" : "Giao Dịch";
                    if (GUI.Button(btnRect, btnLabel, merchantBtnStyle))
                    {
                        if (World != null)
                        {
                            EmberMerchant.ExecuteTrade(World, p, i, Profile);
                        }
                    }
                    GUI.enabled = true;
                }

                curY += 66f;
            }
        }

        string BuildCostString(int[] cost)
        {
            if (cost == null) return "—";
            var parts = new List<string>();
            string[] namesVi = new string[] { "Bụi", "Lõi", "Mảnh tối", "Gỗ", "Quặng" };
            string[] namesEn = new string[] { "Dust", "Core", "Umbra", "Wood", "Ore" };
            var names = EmberLanguage.English ? namesEn : namesVi;

            for (int r = 0; r < 5; r++)
            {
                if (cost[r] > 0)
                    parts.Add($"{cost[r]} {names[r]}");
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "0";
        }

        void DrawTreasureCompassHud(Player p)
        {
            var treasure = EmberMerchant.FindNearestTreasure(State, p.Pos);
            if (!treasure.HasValue) return;

            var (name, pos, distance) = treasure.Value;
            float angle = Mathf.Atan2(pos.Y - p.Pos.Y, pos.X - p.Pos.X);

            // Floating Compass HUD Widget at top right
            float cw = 210f;
            float ch = 48f;
            float cx = Screen.width - cw - 16f;
            float cy = 110f;
            var cRect = new Rect(cx, cy, cw, ch);

            Panel(new Rect(cRect.x - 2, cRect.y - 2, cRect.width + 4, cRect.height + 4), new Color(0.95f, 0.80f, 0.35f, 0.85f));
            Panel(cRect, new Color(0.10f, 0.08f, 0.18f, 0.94f));

            string arrow = GetCompassArrow(angle);
            string distText = $"{distance:F1}m";
            GUI.Label(new Rect(cRect.x + 8, cRect.y + 4, cRect.width - 16, 20),
                $"🧭 <b>{arrow} {name}</b>", merchantRepStyle);
            GUI.Label(new Rect(cRect.x + 8, cRect.y + 24, cRect.width - 16, 18),
                L($"Khoảng cách: {distText}", $"Distance: {distText}"), merchantSubStyle);
        }

        string GetCompassArrow(float angleRad)
        {
            float deg = angleRad * Mathf.Rad2Deg;
            if (deg < 0) deg += 360f;
            if (deg >= 337.5f || deg < 22.5f) return "➔";
            if (deg >= 22.5f && deg < 67.5f) return "↗";
            if (deg >= 67.5f && deg < 112.5f) return "↑";
            if (deg >= 112.5f && deg < 157.5f) return "↖";
            if (deg >= 157.5f && deg < 202.5f) return "←";
            if (deg >= 202.5f && deg < 247.5f) return "↙";
            if (deg >= 247.5f && deg < 292.5f) return "↓";
            return "↘";
        }

        void DrawShadowVeilHud(Player p, float remainingTime)
        {
            if (remainingTime <= 0) return;

            // Stealth HUD indicator
            float pulse = 0.75f + 0.25f * Mathf.Sin(clock * 12f);
            float bw = 200f;
            float bx = (Screen.width - bw) * 0.5f;
            float by = Screen.height - 110f;
            var bRect = new Rect(bx, by, bw, 26f);

            Panel(bRect, new Color(0.18f, 0.06f, 0.28f, 0.88f * pulse));
            Panel(new Rect(bRect.x, bRect.y, bRect.width, 2f), new Color(0.80f, 0.40f, 1f, pulse));
            Panel(new Rect(bRect.x, bRect.yMax - 2f, bRect.width, 2f), new Color(0.80f, 0.40f, 1f, pulse));

            string stealthText = EmberLanguage.English
                ? $"👻 SHADOW VEIL: {remainingTime:F1}s"
                : $"👻 VÔ ẢNH (TÀNG HÌNH): {remainingTime:F1}s";
            GUI.Label(bRect, stealthText, merchantRepStyle);

            // World-space glowing aura ring around player
            if (Event.current.type == EventType.Repaint)
            {
                GlowRing(p.Pos, 1.25f, new Color(0.70f, 0.30f, 1f, 0.65f * pulse), 2.2f);
            }
        }
    }
#endif
}
