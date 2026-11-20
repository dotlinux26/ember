using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
using UnityEngine;
#endif

namespace Ember
{
    public enum WeaponGripMode
    {
        SingleHand,   // 1 hand holds weapon, other hand moves freely / rests
        TwoHanded,    // Both hands grip the single weapon handle (pickaxe, axe, rifle, crossbow)
        DualWield     // Dual-wield: Main hand holds blade 1, Off hand holds blade 2 with independent animations!
    }

    [Serializable]
    public sealed class WeaponGripConfig
    {
        public Weapon WeaponId;
        public string SpriteName = "";
        public float SpriteBaseAngle = 45f;     // Native orientation angle in raw PNG (0=horiz, 45=diag, 90=upright)
        public V2 GripPivot = new V2(0.35f, 0.25f); // Hand contact point [0..1]
        public V2 HandOffset = new V2(0f, 0f);      // Position offset relative to character hand bone
        public float RestAngle = -25f;          // Idle rest angle relative to facing direction
        public float Scale = 0.85f;             // Visual scale multiplier
        public WeaponGripMode GripMode = WeaponGripMode.SingleHand;
        public V2 TwoHandJoinOffset = new V2(-0.08f, -0.06f);

        // Dual Wield Specifications:
        public string DualSpriteName = "";      // Separate sprite for single blade (e.g. "shadow_blade_single")
        public float DualOffHandRestAngle = 38f;// Idle rest angle for off-hand blade
        public V2 DualOffHandPivot = new V2(0.35f, 0.25f);
        public V2 DualOffHandOffset = new V2(0f, 0f);
        public float DualOffHandScale = 0.85f;
    }

    [Serializable]
    public sealed class WeaponGripFile
    {
        public WeaponGripConfig[] Weapons;
    }

    /// <summary>
    /// Industrial-Grade Weapon Transform & Grip Engine.
    /// Standardizes sprite base angles, grip anchors, idle rests, two-hand grip, and dual-wielding.
    /// Eliminates hardcoded magic angles and ad-hoc rotational offsets.
    /// </summary>
    public static class EmberWeaponGrips
    {
        static readonly Dictionary<Weapon, WeaponGripConfig> configs = new Dictionary<Weapon, WeaponGripConfig>();
        static readonly WeaponGripConfig torchConfig = new WeaponGripConfig
        {
            SpriteName = "torch",
            SpriteBaseAngle = 90f,
            GripPivot = new V2(0.50f, 0.22f),
            HandOffset = new V2(0.04f, 0.08f),
            RestAngle = -22f,
            Scale = 0.58f,
            GripMode = WeaponGripMode.SingleHand
        };
        static bool initialized;

        public static WeaponGripConfig Torch => torchConfig;

        public static WeaponGripConfig GetGrip(string name)
        {
            if (string.Equals(name, "torch", StringComparison.OrdinalIgnoreCase))
                return torchConfig;
            EnsureInitialized();
            foreach (var kv in configs)
            {
                if (string.Equals(kv.Value.SpriteName, name, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
            return null;
        }

        static WeaponGripConfig CreateDefault(Weapon w)
        {
            switch (w)
            {
                case Weapon.Stick:
                    return new WeaponGripConfig
                    {
                        WeaponId = w,
                        SpriteName = "stick",
                        SpriteBaseAngle = 65f,
                        GripPivot = new V2(0.32f, 0.18f),
                        HandOffset = new V2(0.04f, 0.08f),
                        RestAngle = -22f,
                        Scale = 0.85f,
                        GripMode = WeaponGripMode.SingleHand
                    };

                case Weapon.CrystalAxe:
                    return new WeaponGripConfig
                    {
                        WeaponId = w,
                        SpriteName = "crystal_axe",
                        SpriteBaseAngle = 45f,
                        GripPivot = new V2(0.28f, 0.22f),
                        HandOffset = new V2(0.06f, 0.10f),
                        RestAngle = -28f,
                        Scale = 0.90f,
                        GripMode = WeaponGripMode.TwoHanded,
                        TwoHandJoinOffset = new V2(-0.08f, -0.07f)
                    };

                case Weapon.ShadowBlades:
                    // DUAL-WIELD MASTER: Two separate daggers held in Main Hand & Off Hand
                    return new WeaponGripConfig
                    {
                        WeaponId = w,
                        SpriteName = "shadow_blades",               // Paired icon for inventory & ground
                        DualSpriteName = "shadow_blade_single",     // Single dagger for each hand
                        SpriteBaseAngle = 45f,
                        GripPivot = new V2(0.36f, 0.32f),
                        HandOffset = new V2(0.05f, 0.09f),
                        RestAngle = -18f,                            // Main blade rests forward-down
                        Scale = 0.85f,
                        GripMode = WeaponGripMode.DualWield,

                        // Off-hand dual dagger configuration
                        DualOffHandRestAngle = 42f,                 // Off-hand blade held in raised counter-guard
                        DualOffHandPivot = new V2(0.36f, 0.32f),
                        DualOffHandOffset = new V2(-0.03f, 0.07f),
                        DualOffHandScale = 0.85f
                    };

                case Weapon.Crossbow:
                    return new WeaponGripConfig
                    {
                        WeaponId = w,
                        SpriteName = "crossbow",
                        SpriteBaseAngle = 0f,                       // Drawn pointing right
                        GripPivot = new V2(0.35f, 0.45f),
                        HandOffset = new V2(0.12f, 0.07f),
                        RestAngle = 0f,
                        Scale = 0.85f,
                        GripMode = WeaponGripMode.TwoHanded,
                        TwoHandJoinOffset = new V2(-0.06f, -0.04f)
                    };

                case Weapon.LumenGun:
                    return new WeaponGripConfig
                    {
                        WeaponId = w,
                        SpriteName = "lumen_gun",
                        SpriteBaseAngle = 0f,                       // Drawn pointing horizontally
                        GripPivot = new V2(0.25f, 0.42f),
                        HandOffset = new V2(0.14f, 0.06f),
                        RestAngle = 0f,
                        Scale = 0.85f,
                        GripMode = WeaponGripMode.TwoHanded,
                        TwoHandJoinOffset = new V2(-0.07f, -0.04f)
                    };

                case Weapon.MirrorShield:
                    return new WeaponGripConfig
                    {
                        WeaponId = w,
                        SpriteName = "mirror_shield",
                        SpriteBaseAngle = 0f,
                        GripPivot = new V2(0.50f, 0.50f),
                        HandOffset = new V2(0.08f, 0.08f),
                        RestAngle = 0f,
                        Scale = 0.95f,
                        GripMode = WeaponGripMode.SingleHand
                    };

                case Weapon.Pickaxe:
                default:
                    return new WeaponGripConfig
                    {
                        WeaponId = w,
                        SpriteName = "pickaxe",
                        SpriteBaseAngle = 45f,
                        GripPivot = new V2(0.26f, 0.22f),
                        HandOffset = new V2(0.06f, 0.10f),
                        RestAngle = -32f,
                        Scale = 0.88f,
                        GripMode = WeaponGripMode.TwoHanded,
                        TwoHandJoinOffset = new V2(-0.08f, -0.07f)
                    };
            }
        }

        public static void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            for (int i = 0; i <= (int)Weapon.Pickaxe; i++)
            {
                var w = (Weapon)i;
                configs[w] = CreateDefault(w);
            }

            // Optional JSON override
            try
            {
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
                var textAsset = Resources.Load<TextAsset>("Ember/weapon-grips");
                if (textAsset != null && !string.IsNullOrWhiteSpace(textAsset.text))
                {
                    var file = JsonUtility.FromJson<WeaponGripFile>(textAsset.text);
                    if (file?.Weapons != null)
                    {
                        foreach (var c in file.Weapons)
                        {
                            if (c != null) configs[c.WeaponId] = c;
                        }
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public static WeaponGripConfig Get(Weapon w)
        {
            EnsureInitialized();
            if (!configs.TryGetValue(w, out var config))
            {
                config = CreateDefault(w);
                configs[w] = config;
            }
            return config;
        }

        public static void Set(Weapon w, WeaponGripConfig config)
        {
            EnsureInitialized();
            configs[w] = config;
        }

        public static WeaponGripConfig[] GetAll()
        {
            EnsureInitialized();
            var list = new List<WeaponGripConfig>();
            for (int i = 0; i <= (int)Weapon.Pickaxe; i++)
                list.Add(Get((Weapon)i));
            return list.ToArray();
        }

        public static string ExportJson()
        {
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
            var file = new WeaponGripFile { Weapons = GetAll() };
            return JsonUtility.ToJson(file, true);
#else
            return "";
#endif
        }
    }
}
