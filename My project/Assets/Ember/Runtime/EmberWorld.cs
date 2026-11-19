using System;
using System.Collections.Generic;
using System.Linq;

namespace Ember
{
    public enum Phase { Sanctuary, Explore, Eclipse, Extraction, Won, Lost }
    public enum Difficulty { Easy, Normal, Hard }
    public enum Vessel { Wanderer, MoonMender, BombTrapper, RuneWarden, SpaceWraith, LittleMoone }
    public enum Resource { Dust, Core, Umbra, Wood, Ore, Count }
    public enum Weapon { Stick, CrystalAxe, ShadowBlades, Crossbow, LumenGun, MirrorShield, Pickaxe }
    public enum EnemyKind { Shade, SwampLeech, VoidStalker, Sentinel, Nest, Boss }
    public enum BossType { VoidSovereign, CinderBehemoth, EclipseWeaver }
    public enum BossSkillKind { CircleRain, BulletStorm, RectCharge }
    public enum LightAnchor { None, Caster, Target, Both }
    public enum LightColorKind { Starlight, Moonlight, Flash, RuneGold, VoidPurple, Ember }
    public enum Lantern { Oil, Inferno, Relic }
    public enum Perk { SteelNerves, StrongBack, GhostStep, Prepared, Miner, LastLight, ReluctantHealer }
    public enum Hat { Straw, Witch, Candle, Jester }
    public enum Pet { Firefly, Bat, CrystalCat, LittleGhost }
    public enum Recipe { Torch, Flare, Sedative, RevivalStone, Wall, Trap, Chest, InfernoLantern, RelicLantern, CrystalAxe, ShadowBlades, Crossbow, LumenGun, MirrorShield }
    public enum BuffKind { None, Swiftness, AttackPower, IronSkin, Regen, StaminaSurge, LightAura, DreadWard }

    [Serializable]
    public struct ActiveBuff
    {
        public BuffKind Kind;
        public float Duration;
        public float MaxDuration;
        public float Value;
        public int SourcePlayerId;
        public int Stacks;
    }

    [Serializable] public struct V2
    {
        public float X, Y;
        public V2(float x,float y){X=x;Y=y;}
        public float Length => (float)Math.Sqrt(X*X+Y*Y);
        public V2 Normal => Length>.0001f?this/Length:new V2();
        public static V2 operator +(V2 a,V2 b)=>new V2(a.X+b.X,a.Y+b.Y);
        public static V2 operator -(V2 a,V2 b)=>new V2(a.X-b.X,a.Y-b.Y);
        public static V2 operator *(V2 a,float b)=>new V2(a.X*b,a.Y*b);
        public static V2 operator /(V2 a,float b)=>new V2(a.X/b,a.Y/b);
        public static float Distance(V2 a,V2 b)=>(a-b).Length;
    }
    [Serializable] public struct TreeRespawn { public int Tx, Ty; public float RespawnAt; public byte Tile; }
    [Serializable] public struct HarvestTarget { public int Tx, Ty; public float HP, MaxHP, LastHitTime; public byte Tile; }
    [Serializable] public sealed class Command
    {
        public float X,Y; public bool Attack,Skill,Dash,Interact,Refuel,Deposit,ToggleLamp,PassLamp,Push,Revive,Reload;
        public int Craft=-1,Use=-1,Equip; // consumables: 0 torch, 1 flare, 2 sedative, 3 revive
        public bool HasActions=>Attack||Skill||Dash||Interact||Refuel||Deposit||ToggleLamp||PassLamp||Push||Revive||Reload||Craft>=0||Use>=0||Equip>0;
    }
    [Serializable] public sealed class Player
    {
        public int Id,PartySlot; [NonSerialized] public string InstallationId; public string Name="Traveler"; public Vessel Vessel;
        public V2 Pos,Move; public bool Right=true,Safe,Extracted,CarriesLantern,LampOn=true,Ready;
        public float HP=100,MaxHP=100,Stamina=100,Dread,AttackCD,SkillCD,DashCD,DashTime,CastTime,Invulnerable,ShieldTime,Shield,HarvestCD;
        public float PushCD,ReloadTimer;
        public V2 DashDir; public Weapon Weapon; public Weapon SideWeapon=Weapon.LumenGun; public Weapon AmmoWeapon=Weapon.LumenGun; public int Ammo=-1; public int HeldSlot=6,PrevHeldSlot=2; public bool HasPickaxe=true; public Lantern Lantern;
        public int PickaxeTier=1; public int PickaxeDurability=80;
        public int PickaxeMaxDurability => PickaxeTier==0?40:PickaxeTier==1?80:PickaxeTier==2?150:250;
        public string PickaxeName => PickaxeTier==0?(EmberLanguage.English?"Stone Pickaxe":"Cuốc Đá Thô"):
            PickaxeTier==1?(EmberLanguage.English?"Iron Pickaxe":"Cuốc Sắt"):
            PickaxeTier==2?(EmberLanguage.English?"Crystal Pickaxe":"Cuốc Pha Lê"):
            (EmberLanguage.English?"Shadow Pickaxe":"Cuốc Hắc Ám");
        public V2 MiragePos;public float MirageLife,MirageCD=12;
        public int[] Bag=new int[(int)Resource.Count],Items=new int[4];
        public int[] Perks=Array.Empty<int>(); public int Hat=-1,Pet=-1;
        public int[] PerkRanks=Array.Empty<int>(),Recipes;public int Gathered,Crafted;
        public int Rank(Perk perk)=>Has(perk)?Math.Max(1,Math.Min(3,PerkRanks!=null&&PerkRanks.Length>(int)perk?PerkRanks[(int)perk]:1)):0;
        public float PerkStrength(Perk perk)=>1+.2f*Math.Max(0,Rank(perk)-1);
        public bool Ghost=>HP<=0; public bool Has(Perk perk)=>Array.IndexOf(Perks,(int)perk)>=0;
        public VesselStats Stats=>Catalog.StatsFor(Vessel);
        public float BaseCapacity=>30f+Stats.Gathering*5f;
        public int Load=>Bag.Sum(); public float Capacity=>BaseCapacity*(Has(Perk.StrongBack)?1+.3f*PerkStrength(Perk.StrongBack):1);
        public bool HasHeldTorch=>(HeldSlot==2||(HeldSlot==8&&PrevHeldSlot==2&&HarvestCD>0))&&Items[0]>0&&LampOn&&!Ghost&&!Extracted;
        public bool HoldingWeapon=>HeldSlot==1||HeldSlot==7||HeldSlot==8;
        public Weapon ActiveWeapon=>HeldSlot==7?SideWeapon:HeldSlot==8?Weapon.Pickaxe:Weapon;
        public bool Ranged=>ActiveWeapon==Weapon.Crossbow||ActiveWeapon==Weapon.LumenGun||ActiveWeapon==Weapon.MirrorShield;
        public float Range=>Ranged?7:ActiveWeapon==Weapon.CrystalAxe?2.6f:ActiveWeapon==Weapon.Pickaxe?2.4f:2;
        public int MaxAmmo=>Catalog.WeaponMagSize(ActiveWeapon);
        public int CurrentAmmo=>MaxAmmo<=0?0:(Ammo<0||AmmoWeapon!=ActiveWeapon?MaxAmmo:Math.Min(Ammo,MaxAmmo));
        public bool Reloading=>ReloadTimer>0;
        public float SkillRange=>Catalog.SkillRange(Vessel,Awake);
        public bool Mad=>Dread>=90&&!Safe;
        public bool Awake;
        public List<ActiveBuff> Buffs=new List<ActiveBuff>();
        public bool HasBuff(BuffKind kind)=>Buffs!=null&&Buffs.Any(b=>b.Kind==kind&&b.Duration>0);
        public float BuffValue(BuffKind kind)
        {
            if(Buffs==null)return 0f;
            for(int i=0;i<Buffs.Count;i++)
                if(Buffs[i].Kind==kind&&Buffs[i].Duration>0)return Buffs[i].Value*Math.Max(1,Buffs[i].Stacks);
            return 0f;
        }
        public ActiveBuff GetBuff(BuffKind kind)
        {
            if(Buffs==null)return default;
            for(int i=0;i<Buffs.Count;i++)
                if(Buffs[i].Kind==kind&&Buffs[i].Duration>0)return Buffs[i];
            return default;
        }
        public void ApplyBuff(BuffKind kind,float duration,float value=0f,int sourcePlayerId=-1,int maxStacks=1,bool refresh=true)
        {
            if(Buffs==null)Buffs=new List<ActiveBuff>();
            for(int i=0;i<Buffs.Count;i++)
            {
                if(Buffs[i].Kind==kind)
                {
                    var b=Buffs[i];
                    if(refresh)b.Duration=Math.Max(b.Duration,duration);
                    b.MaxDuration=Math.Max(b.MaxDuration,duration);
                    b.Value=Math.Max(b.Value,value);
                    if(maxStacks>1&&b.Stacks<maxStacks)b.Stacks++;
                    if(sourcePlayerId>=0)b.SourcePlayerId=sourcePlayerId;
                    Buffs[i]=b;
                    return;
                }
            }
            Buffs.Add(new ActiveBuff{Kind=kind,Duration=duration,MaxDuration=duration,Value=value,SourcePlayerId=sourcePlayerId,Stacks=1});
        }
        public void RemoveBuff(BuffKind kind){Buffs?.RemoveAll(b=>b.Kind==kind);}
        public void ClearBuffs(){Buffs?.Clear();}
    }
    [Serializable] public sealed class Enemy
    {
        public int Id; public V2 Pos; public float HP=42,MaxHP=42,AttackCD,Blind; public bool Elite;public EnemyKind Kind;
        public BossType BossType; public bool IsWorldBoss;
        public float SkillCD,ChargeTime,ChargeSpeed,ChargeDamage,WindupTime;
        public V2 ChargeDir;
        public int SkillIndex;
    }
    [Serializable] public sealed class BossTelegraph
    {
        public int Id,OwnerId,Kind,Wave; // Kind: 0 circle AoE, 1 rect charge, 2 bullet storm warning
        public BossType BossType;
        public V2 Pos,Dir;
        public float Radius,Length,Timer,Duration,Damage;
    }
    [Serializable] public sealed class TransientLight
    {
        public int Id,FollowPlayerId;
        public LightColorKind ColorKind;
        public V2 Pos;
        public float Radius,Intensity,Life,MaxLife;
        public bool GrantsSafety;
    }
    [Serializable] public sealed class CombatPopup
    {
        public int Id,Kind,Amount,SubKind; // Kind: 0 enemy dmg, 1 player dmg, 2 heal, 3 harvest
        public V2 Pos;
        public float Life,MaxLife;
    }
    [Serializable] public sealed class DisconnectedSeat
    {
        public string InstallationId="",Name="Traveler";
        public int PartySlot;
        public Vessel Vessel;
        public float HP=100,MaxHP=100,Stamina=100,Dread;
        public Weapon Weapon,SideWeapon=Weapon.LumenGun,AmmoWeapon=Weapon.LumenGun;
        public int Ammo=-1,HeldSlot=1;
        public bool HasPickaxe=true,LampOn=true;
        public int PickaxeTier=1,PickaxeDurability=80;
        public Lantern Lantern;
        public int[] Bag=new int[(int)Resource.Count],Items=new int[4];
        public int[] Perks=Array.Empty<int>(),PerkRanks=Array.Empty<int>(),Recipes;
        public int Hat=-1,Pet=-1,Gathered,Crafted;
        public V2 LastPos;
        public float DisconnectedAt;
        public bool Awake;
    }
    [Serializable] public sealed class Node
    {
        public int Id; public V2 Pos; public Resource Resource; public int Amount; public bool Dropped;
        public int MaxAmount; public float LastHitTime;
    }
    [Serializable] public sealed class Structure
    {
        public int Id,Kind; public V2 Pos; public float Life,MaxLife,HP=100; // 0 torch 1 flare 2 wall 3 trap 4 chest
    }
    [Serializable] public sealed class Effect
    {
        public V2 From,To; public int Kind; public float Life;
    }
    [Serializable] public sealed class Projectile
    {
        public int Id,Owner;public V2 Pos,Velocity;public float Damage,Life=1;public Weapon Weapon;
        public bool FromEnemy; public int ColorKind;
    }
    [Serializable] public sealed class Snapshot
    {
        public int Seed,Size,Tick,Capacity=4,BossesDefeated,TeamExtracted; public string RoomName="Ember";public Phase Phase; public Difficulty Difficulty; public float Time,Fuel,BeaconHP,Charge,ChargeTarget=100,Wave,Blackout,LastLight;
        public Player[] Players; public Enemy[] Enemies; public Node[] Nodes; public Structure[] Structures; public Effect[] Effects;public Projectile[] Projectiles=Array.Empty<Projectile>();
        public string Event,EventActor,EventTarget;public int EventTick;
        public bool Sandbox;
        public bool BossArenaActive;
        public int BossId;
        public BossType BossType;
        public V2 BossArenaCenter;
        public float BossArenaRadius;
        public float BossAlertTimer,BossSkillCalloutTimer,NextBossEventTime=600f;
        public string BossAlertText="",BossSkillCallout="";
        public BossTelegraph[] BossTelegraphs=Array.Empty<BossTelegraph>();
        public TransientLight[] Lights=Array.Empty<TransientLight>();
        public CombatPopup[] Popups=Array.Empty<CombatPopup>();
        public TreeRespawn[] TreeRespawns=Array.Empty<TreeRespawn>();
        public HarvestTarget[] HarvestTargets=Array.Empty<HarvestTarget>();
        public int PeriodicWave=1;
        public DisconnectedSeat[] Disconnected=Array.Empty<DisconnectedSeat>();
    }
    [Serializable] public sealed class PendingMeteor
    {
        public int OwnerId; public V2 Center; public float Delay;
    }
    [Serializable] public sealed class RunSaveData
    {
        public int Version=1;
        public string SavedAt="";
        public int HeldSlot=1;
        public Snapshot Snapshot;
        public int[] Chest=new int[5];
        public int NextId=10,LastBossType=-1;
        public float SpawnClock,PetClock;
        public bool LastLightSpent,BossSpawned;
        public PendingMeteor[] PendingMeteors=Array.Empty<PendingMeteor>();
    }
    public sealed class RecipeData
    {
        public string Name; public int[] Cost; public RecipeData(string name,int dust=0,int core=0,int umbra=0,int wood=0,int ore=0){Name=name;Cost=new[]{dust,core,umbra,wood,ore};}
    }
    public struct VesselStats
    {
        public int Speed,Defense,DreadResist,Power,Endurance,Gathering;
        public VesselStats(int speed,int defense,int dreadResist,int power,int endurance,int gathering)
        {Speed=speed;Defense=defense;DreadResist=dreadResist;Power=power;Endurance=endurance;Gathering=gathering;}
        public float MoveSpeed=>3.2f+Speed*.18f;
        public float DashCooldown=>1.4f-Speed*.04f;
        public float DamageTakenMultiplier=>1f-(Defense-1)*.04f;
        public float DreadGainMultiplier=>1.24f-(DreadResist-1)*.06f;
        public float DreadCalmMultiplier=>.72f+(DreadResist-1)*.07f;
        public float PowerMultiplier=>.8f+(Power-1)*.06f;
        public float StaminaRegenMultiplier=>.76f+(Endurance-1)*.06f;
        public float StaminaCostMultiplier=>1f-(Endurance-5)*.04f;
        public float HarvestCooldown=>.5f-Gathering*.022f;
    }
    public sealed class BossSpec
    {
        public string NameVi,NameEn,SubtitleVi,SubtitleEn;
        public string RainNameVi,RainNameEn,BulletNameVi,BulletNameEn,ChargeNameVi,ChargeNameEn;
        public float BaseHP,MoveSpeed,SkillInterval,Scale;
        public int CircleCount,BulletWaves,BulletCount;
        public float CircleRadius,CircleDelay,CircleDamage;
        public float BulletSpeed,BulletDelay,BulletDamage;
        public bool BulletChaotic;
        public float ChargeLength,ChargeWidth,ChargeDelay,ChargeDamage;
    }
    public readonly struct SkillLightProfile
    {
        public readonly LightAnchor Anchor;
        public readonly LightColorKind ColorKind;
        public readonly float CasterRadius,TargetRadius,Duration,Intensity;
        public readonly bool GrantsSafety;
        public SkillLightProfile(LightAnchor anchor,LightColorKind colorKind,float casterRadius,float targetRadius,float duration,float intensity,bool grantsSafety)
        {Anchor=anchor;ColorKind=colorKind;CasterRadius=casterRadius;TargetRadius=targetRadius;Duration=duration;Intensity=intensity;GrantsSafety=grantsSafety;}
    }
    public readonly struct EnemyShotSpec
    {
        public readonly bool Enabled;
        public readonly float MinRange,MaxRange,Speed,Cooldown,Damage,SpreadDeg;
        public readonly int Count,ColorKind;
        public EnemyShotSpec(bool enabled,float minRange,float maxRange,float speed,float cooldown,float damage,int count,float spreadDeg,int colorKind)
        {Enabled=enabled;MinRange=minRange;MaxRange=maxRange;Speed=speed;Cooldown=cooldown;Damage=damage;Count=count;SpreadDeg=spreadDeg;ColorKind=colorKind;}
    }
    public sealed class BuffDefinition
    {
        public BuffKind Kind;
        public string NameVi,NameEn,Icon;
        public bool IsDebuff;
        public int MaxStacks;
        public string DisplayName => EmberLanguage.English ? NameEn : NameVi;
        public string FormatValue(float value, int stacks = 1)
        {
            switch(Kind)
            {
                case BuffKind.Swiftness:
                    return $"+{Math.Round(value * 100):0}% " + (EmberLanguage.English ? "Speed" : "Tốc chạy");
                case BuffKind.AttackPower:
                    return $"+{Math.Round(value * 100):0}% " + (EmberLanguage.English ? "Damage" : "Sát thương");
                case BuffKind.IronSkin:
                    return $"-{Math.Round(value * 100):0}% " + (EmberLanguage.English ? "Dmg Taken" : "ST nhận");
                case BuffKind.Regen:
                    return $"+{value:0.#} HP/s";
                case BuffKind.StaminaSurge:
                    return $"+{Math.Round(value * 100):0}% " + (EmberLanguage.English ? "Stamina" : "Thể lực");
                case BuffKind.LightAura:
                    return EmberLanguage.English ? "Safe Light" : "Vùng sáng an toàn";
                case BuffKind.DreadWard:
                    return EmberLanguage.English ? "Calm Mind" : "Kháng hoảng sợ";
                default:
                    return "";
            }
        }
    }
    public static class Catalog
    {
        public static readonly Dictionary<BuffKind, BuffDefinition> BuffRegistry = new Dictionary<BuffKind, BuffDefinition>
        {
            [BuffKind.Swiftness] = new BuffDefinition{Kind=BuffKind.Swiftness, NameVi="Gia Tốc", NameEn="Swiftness", Icon="speed", MaxStacks=1},
            [BuffKind.AttackPower] = new BuffDefinition{Kind=BuffKind.AttackPower, NameVi="Công Kích", NameEn="Empowered", Icon="atk", MaxStacks=1},
            [BuffKind.IronSkin] = new BuffDefinition{Kind=BuffKind.IronSkin, NameVi="Giáp Sắt", NameEn="Iron Skin", Icon="shield", MaxStacks=1},
            [BuffKind.Regen] = new BuffDefinition{Kind=BuffKind.Regen, NameVi="Hồi Phục", NameEn="Regeneration", Icon="heal", MaxStacks=1},
            [BuffKind.StaminaSurge] = new BuffDefinition{Kind=BuffKind.StaminaSurge, NameVi="Hưng Phấn", NameEn="Stamina Surge", Icon="stamina", MaxStacks=1},
            [BuffKind.LightAura] = new BuffDefinition{Kind=BuffKind.LightAura, NameVi="Quang Giáp", NameEn="Light Aura", Icon="light", MaxStacks=1},
            [BuffKind.DreadWard] = new BuffDefinition{Kind=BuffKind.DreadWard, NameVi="Tĩnh Tâm", NameEn="Dread Ward", Icon="calm", MaxStacks=1},
        };
        public static BuffDefinition GetBuffDef(BuffKind kind)
        {
            if (BuffRegistry.TryGetValue(kind, out var def)) return def;
            return new BuffDefinition{Kind=kind, NameVi=kind.ToString(), NameEn=kind.ToString(), Icon="buff", MaxStacks=1};
        }
        public static EnemyShotSpec EnemyShotFor(EnemyKind kind,bool elite)
        {
            if(kind==EnemyKind.Nest)return default;
            if(kind==EnemyKind.Boss)return new EnemyShotSpec(true,3.5f,15f,5.4f,4.2f,14f,3,18f,3);
            if(elite||kind==EnemyKind.Sentinel)return new EnemyShotSpec(true,3.6f,14f,5.0f,4.6f,12f,3,16f,1);
            if(kind==EnemyKind.VoidStalker)return new EnemyShotSpec(true,4.0f,13f,5.1f,5.2f,11f,1,0f,4);
            if(kind==EnemyKind.SwampLeech)return new EnemyShotSpec(true,3.8f,11.5f,4.6f,5.8f,9f,1,0f,5);
            return new EnemyShotSpec(true,4.2f,10.5f,4.2f,6.8f,7f,1,0f,4);
        }
        public static readonly SkillLightProfile[] SkillLights={
            new SkillLightProfile(LightAnchor.None,LightColorKind.Ember,0,0,0,0,false),                       // Wanderer
            new SkillLightProfile(LightAnchor.Target,LightColorKind.Moonlight,0,6f,2.5f,1.25f,true),          // MoonMender
            new SkillLightProfile(LightAnchor.Target,LightColorKind.Flash,0,8f,3.0f,1.45f,true),              // BombTrapper
            new SkillLightProfile(LightAnchor.Caster,LightColorKind.RuneGold,6f,0,4.0f,1.25f,true),           // RuneWarden
            new SkillLightProfile(LightAnchor.Target,LightColorKind.VoidPurple,0,5f,1.5f,1.1f,false),         // SpaceWraith
            new SkillLightProfile(LightAnchor.Both,LightColorKind.Starlight,6f,8f,2.2f,1.35f,true),           // LittleMoone
        };
        public static SkillLightProfile SkillLightFor(Vessel vessel,bool awake=false)=>(vessel==Vessel.Wanderer&&awake)?new SkillLightProfile(LightAnchor.Caster,LightColorKind.Ember,7f,0,2.5f,1.2f,true):SkillLights[Math.Max(0,Math.Min(SkillLights.Length-1,(int)vessel))];
        public static readonly BossSpec[] BossSpecs={
            new BossSpec{
                NameVi="Chúa Tể Hư Không",NameEn="Void Sovereign",
                SubtitleVi="Thực Thể Vực Thẳm",SubtitleEn="Abyssal Sovereign",
                RainNameVi="Mưa Hư Không",RainNameEn="Void Rain",
                BulletNameVi="Bão Đạn Tỏa",BulletNameEn="Radial Volley",
                ChargeNameVi="Xung Kích Xé Đêm",ChargeNameEn="Night Cleave",
                BaseHP=950,MoveSpeed=1.75f,SkillInterval=4.6f,Scale=2.05f,
                CircleCount=5,CircleRadius=2.25f,CircleDelay=1.45f,CircleDamage=26f,
                BulletWaves=2,BulletCount=12,BulletSpeed=8.5f,BulletDelay=.85f,BulletDamage=14f,BulletChaotic=false,
                ChargeLength=11f,ChargeWidth=2.5f,ChargeDelay=1.35f,ChargeDamage=28f
            },
            new BossSpec{
                NameVi="Cự Thú Tro Tàn",NameEn="Cinder Behemoth",
                SubtitleVi="Lò Lửa Hủy Diệt",SubtitleEn="Molten Titan",
                RainNameVi="Mưa Thiên Thạch Tro",RainNameEn="Cinder Barrage",
                BulletNameVi="Pháo Hỏa Loạn Xạ",BulletNameEn="Chaotic Embers",
                ChargeNameVi="Húc Lửa Liên Hoàn",ChargeNameEn="Blazing Ram",
                BaseHP=1200,MoveSpeed=1.55f,SkillInterval=4.9f,Scale=2.25f,
                CircleCount=6,CircleRadius=2.6f,CircleDelay=1.6f,CircleDamage=30f,
                BulletWaves=2,BulletCount=14,BulletSpeed=7.8f,BulletDelay=.9f,BulletDamage=15f,BulletChaotic=true,
                ChargeLength=12f,ChargeWidth=3.0f,ChargeDelay=1.3f,ChargeDamage=32f
            },
            new BossSpec{
                NameVi="Kẻ Dệt Nhật Thực",NameEn="Eclipse Weaver",
                SubtitleVi="Bóng Ma Huyết Nguyệt",SubtitleEn="Bloodmoon Weaver",
                RainNameVi="Ấn Nguyệt Giáng Thế",RainNameEn="Lunar Marks",
                BulletNameVi="Loạn Đạn Xoắn Ốc",BulletNameEn="Eclipse Spiral",
                ChargeNameVi="Lưỡi Cắt Nhật Thực",ChargeNameEn="Eclipse Lunge",
                BaseHP=820,MoveSpeed=2.05f,SkillInterval=4.0f,Scale=1.95f,
                CircleCount=4,CircleRadius=2.0f,CircleDelay=1.3f,CircleDamage=24f,
                BulletWaves=3,BulletCount=10,BulletSpeed=9.6f,BulletDelay=.75f,BulletDamage=13f,BulletChaotic=true,
                ChargeLength=13f,ChargeWidth=2.2f,ChargeDelay=1.2f,ChargeDamage=26f
            }
        };
        public static BossSpec BossFor(BossType type)=>BossSpecs[Math.Max(0,Math.Min(BossSpecs.Length-1,(int)type))];
        public static readonly VesselStats[] VesselStatTable={
            new VesselStats(6,6,6,5,6,7),   // Wanderer (36)
            new VesselStats(5,4,9,4,8,5),   // MoonMender (35)
            new VesselStats(5,6,5,8,4,8),   // BombTrapper (36)
            new VesselStats(3,10,7,6,5,5),  // RuneWarden (36)
            new VesselStats(10,2,4,5,9,5),  // SpaceWraith (35)
            new VesselStats(7,3,8,10,5,3),  // LittleMoone (36)
        };
        public static VesselStats StatsFor(Vessel vessel)=>VesselStatTable[Math.Max(0,Math.Min(VesselStatTable.Length-1,(int)vessel))];
        public static int WeaponMagSize(Weapon w)=>w==Weapon.LumenGun?8:w==Weapon.Crossbow?5:w==Weapon.MirrorShield?4:0;
        public static float WeaponReloadTime(Weapon w)=>w==Weapon.LumenGun?1.6f:w==Weapon.Crossbow?1.4f:w==Weapon.MirrorShield?1.8f:0f;
        public static float BeaconChargeTarget(Difficulty difficulty)=>difficulty==Difficulty.Hard?1000:difficulty==Difficulty.Normal?750:500;
        public static readonly string[] DifficultyNamesVi={"Dễ","Trung bình","Khó"};
        public static readonly string[] DifficultyNamesEn={"Easy","Normal","Hard"};
        public static float SkillRange(Vessel vessel,bool awake=false)=>vessel==Vessel.LittleMoone?9:vessel==Vessel.BombTrapper?8:vessel==Vessel.RuneWarden?5:vessel==Vessel.SpaceWraith?2:vessel==Vessel.MoonMender?7:(vessel==Vessel.Wanderer&&awake)?8:0;
        public static readonly string[] VesselNames={"Lữ Hành Vô Danh","Y Sư Ánh Trăng","Kẻ Đặt Bẫy Hỏa Ngục","Hộ Vệ Cổ Ngữ","Bóng Ma Không Gian","Little Moone"};
        public static readonly string[] VesselRarities={"Phổ thông","Hiếm","Hiếm","Sử thi","Sử thi","Đặc biệt"};
        // Economy values not specified by the GDD are explicit test-build tuning.
        public static readonly int[] VesselDrawWeights={49,20,20,5,5,1};
        public static readonly string[] ResourceNames={"Bụi quang","Lõi","Mảnh tối","Gỗ","Quặng"};
        public static readonly string[] PerkNames={"Thần Kinh Thép","Lưng Trâu","Bước Chân Bóng Ma","Kẻ Phòng Xa","Thợ Mỏ Lão Luyện","Tia Sáng Cuối","Lương Y Bất Đắc Dĩ"};
        public static readonly string[] VesselDescriptions={"100 HP • 100 thể lực • Không có kỹ năng chủ động, bù lại chỉ số cân bằng và khai thác ổn định.","100 HP • 100 thể lực • Hồi 35 HP, xóa 40% hoảng loạn • Hồi chiêu 25 giây.","100 HP • 100 thể lực • Bom diện rộng, làm mù 3 giây • Hồi chiêu 20 giây.","100 HP • 100 thể lực • Đẩy lùi và tạo giáp ảo 4 giây • Hồi chiêu 18 giây.","100 HP • 100 thể lực • Đưa bản thân và một đồng đội sát cạnh về hải đăng • Hồi chiêu 60 giây.","100 HP • 100 thể lực • Mưa ánh sao trắng tự ngắm • Hồi chiêu 24 giây."};
        public static readonly string[] PerkDescriptions={"Giảm 25% tốc độ tăng hoảng loạn.","Tăng 30% tải trọng.","Giảm 20% thể lực tiêu hao khi lướt.","Khởi đầu với 2 pháo sáng và 1 thuốc an thần.","Khởi đầu với cuốc sắt.","Đèn cạn nhiên liệu vẫn bùng sáng 5 giây cuối.","Giảm 50% sát thương friendly fire gây ra khi bị ảo giác."};
        public static readonly string[] HatNames={"Mũ Rơm","Mũ Phù Thủy","Vương Miện Nến Chảy","Mặt Nạ Hề Hư Không"};
        public static readonly string[] PetNames={"Đom Đóm Béo","Dơi Bóng Đêm","Mèo Tinh Thể","Hồn Ma Nhỏ"};
        public static readonly string[] HatDescriptions={"+5% tốc độ khai thác.","+8% hồi thể lực.","Sáng bán kính 1 ô, giảm 10% tốc độ tăng hoảng loạn.","Hoảng loạn từ 90%: phát tiếng cười và tăng 15% sát thương."};
        public static readonly string[] PetDescriptions={"Tự nhặt bụi quang ở gần.","Cảnh báo quái vật ở gần.","Mỗi 30 giây chỉ hướng tới mỏ lõi gần nhất.","Giữ đốm sáng tại vị trí chủ nhân chết."};
        public static readonly RecipeData[] Recipes={
            new RecipeData("Đuốc",5,wood:3),new RecipeData("Pháo sáng",8,ore:2),new RecipeData("Thuốc an thần",10),new RecipeData("Đá hồi sinh",15,2),
            new RecipeData("Tường",wood:8,ore:2),new RecipeData("Bẫy điện",4,wood:4,ore:4),new RecipeData("Rương chung",wood:12),
            new RecipeData("Đèn Hỏa Ngục",15,4,ore:5),new RecipeData("Đèn Thánh Tích",20,5,ore:3),new RecipeData("Rìu Quang Tinh",5,2,wood:4,ore:5),
            new RecipeData("Song Đao Bóng Đêm",umbra:5,ore:8),new RecipeData("Nỏ Tinh Thể",core:3,wood:8,ore:3),new RecipeData("Súng Quang Năng",10,4,ore:10),new RecipeData("Khiên Gương",core:3,ore:12)};
    }

    // The host alone advances simulation. Clients send intent, never HP/inventory.
    public sealed class EmberWorld
    {
        public const float Step=1f/30,ReviveChargeCost=100,ReviveBeaconHpCost=150,CoreChargeValue=25;
        public const float BossEventInterval=600f,BossArenaDefaultRadius=12.5f;
        public static float GetBossEventInterval(Difficulty d)=>d==Difficulty.Hard?120f:d==Difficulty.Normal?300f:600f;
        public const float SimActiveRadius=36f,DespawnRadius=56f;
        static readonly V2 BeaconColliderPos=new V2(0,.35f);
        const float BeaconColliderRadius=1.35f;
        public Snapshot S=new Snapshot();
        public byte[] Tiles; public readonly int[] Chest=new int[5];
        readonly List<Player> players=new List<Player>(); readonly List<Enemy> enemies=new List<Enemy>();
        readonly List<Node> nodes=new List<Node>(); readonly List<Structure> structures=new List<Structure>(); readonly List<Effect> effects=new List<Effect>(); readonly List<TreeRespawn> treeRespawns=new List<TreeRespawn>(); readonly List<HarvestTarget> harvestTargets=new List<HarvestTarget>();
        readonly List<Projectile> projectiles=new List<Projectile>();
        readonly List<BossTelegraph> bossTelegraphs=new List<BossTelegraph>();
        readonly List<TransientLight> transientLights=new List<TransientLight>();
        readonly List<CombatPopup> combatPopups=new List<CombatPopup>();
        readonly Dictionary<string,DisconnectedSeat> disconnectedSeats=new Dictionary<string,DisconnectedSeat>();
        readonly Dictionary<int,Command> commands=new Dictionary<int,Command>();
        readonly Dictionary<int,Queue<Command>> actions=new Dictionary<int,Queue<Command>>();
        readonly Dictionary<int,float> inputAt=new Dictionary<int,float>();
        readonly Dictionary<long,float> retaliation=new Dictionary<long,float>();
        readonly Dictionary<int,FlowField> navigation=new Dictionary<int,FlowField>();
        sealed class FlowField{public int[] Distance;public int Goal=-1;public float Built=-100;}
        Random rng;int nextId=10,lastBossType=-1;float spawnClock,petClock;bool lastLightSpent,bossSpawned;
        float DifficultyEnemyScale=>S.Difficulty==Difficulty.Hard?1.25f:S.Difficulty==Difficulty.Easy?.85f:1f;
        float DifficultyDreadScale=>S.Difficulty==Difficulty.Hard?1.25f:S.Difficulty==Difficulty.Normal?1.05f:1f;
        float DifficultyEnemySpeedScale=>S.Difficulty==Difficulty.Hard?1.12f:S.Difficulty==Difficulty.Easy?.9f:1f;
        float ExploreSpawnInterval=>S.Difficulty==Difficulty.Hard?5.5f:S.Difficulty==Difficulty.Easy?9.5f:7.5f;
        float EclipseSpawnInterval=>S.Difficulty==Difficulty.Hard?1.2f:S.Difficulty==Difficulty.Easy?2.2f:1.7f;
        int MaxActiveEnemies=>S.Phase==Phase.Eclipse
            ?(S.Difficulty==Difficulty.Hard?75:S.Difficulty==Difficulty.Easy?40:55)
            :(S.Difficulty==Difficulty.Hard?30:S.Difficulty==Difficulty.Easy?16:22);
        private EmberWorld(){}
        public EmberWorld(int seed,int size=250,Difficulty difficulty=Difficulty.Easy)
        {
            S.Seed=seed;S.Size=size;S.Phase=Phase.Sanctuary;S.Difficulty=difficulty;S.ChargeTarget=Catalog.BeaconChargeTarget(difficulty);S.Fuel=100;S.BeaconHP=0;S.Wave=90;rng=new Random(seed);
            Tiles=MakeTiles(seed,size);GenerateNodes();Publish();
        }
        public RunSaveData ExportRun(int heldSlot=1)
        {
            Publish();
            return new RunSaveData
            {
                Version=1,
                SavedAt=DateTime.UtcNow.ToString("O"),
                HeldSlot=heldSlot,
                Snapshot=S,
                Chest=(int[])Chest.Clone(),
                NextId=nextId,
                LastBossType=lastBossType,
                SpawnClock=spawnClock,
                PetClock=petClock,
                LastLightSpent=lastLightSpent,
                BossSpawned=bossSpawned,
                PendingMeteors=meteors.Select(m=>new PendingMeteor{OwnerId=m.owner?.Id??0,Center=m.center,Delay=m.delay}).ToArray()
            };
        }
        public static void ValidateRunSave(RunSaveData data)
        {
            if(data==null||data.Version!=1||data.Snapshot==null)throw new System.IO.InvalidDataException("Run save không hợp lệ.");
            var s=data.Snapshot;
            if(s.Size<32||s.Size>512)throw new System.IO.InvalidDataException("Kích thước bản đồ không hợp lệ.");
            if(s.Phase!=Phase.Explore&&s.Phase!=Phase.Eclipse&&s.Phase!=Phase.Extraction)throw new System.IO.InvalidDataException("Trận đã kết thúc hoặc chưa bắt đầu.");
            if(s.Players==null||s.Players.Length==0)throw new System.IO.InvalidDataException("Thiếu dữ liệu người chơi.");
            foreach(var p in s.Players)
            {
                if(p==null||p.MaxHP<=0||(int)p.Vessel<0||(int)p.Vessel>=Catalog.VesselNames.Length)throw new System.IO.InvalidDataException("Dữ liệu nhân vật không hợp lệ.");
                if(p.Bag==null||p.Bag.Length!=(int)Resource.Count)p.Bag=new int[(int)Resource.Count];
                if(p.Items==null||p.Items.Length!=4)p.Items=new int[4];
                if(p.Perks==null)p.Perks=Array.Empty<int>();
                if(p.PerkRanks==null)p.PerkRanks=Array.Empty<int>();
            }
            if(s.ChargeTarget<=0)s.ChargeTarget=Catalog.BeaconChargeTarget(s.Difficulty);
            if(s.Enemies==null)s.Enemies=Array.Empty<Enemy>();
            if(s.Nodes==null)s.Nodes=Array.Empty<Node>();
            if(s.Structures==null)s.Structures=Array.Empty<Structure>();
            if(s.Effects==null)s.Effects=Array.Empty<Effect>();
            if(s.Projectiles==null)s.Projectiles=Array.Empty<Projectile>();
            if(s.BossTelegraphs==null)s.BossTelegraphs=Array.Empty<BossTelegraph>();
            if(s.Lights==null)s.Lights=Array.Empty<TransientLight>();
            if(s.Popups==null)s.Popups=Array.Empty<CombatPopup>();
            if(s.Disconnected==null)s.Disconnected=Array.Empty<DisconnectedSeat>();
            if(s.NextBossEventTime<=0)s.NextBossEventTime=s.Time+BossEventInterval;
            if(data.Chest==null||data.Chest.Length!=5)data.Chest=new int[5];
            if(data.PendingMeteors==null)data.PendingMeteors=Array.Empty<PendingMeteor>();
        }
        public static EmberWorld RestoreRun(RunSaveData data,string installationId=null)
        {
            ValidateRunSave(data);
            var w=new EmberWorld();
            w.S=data.Snapshot;
            w.Tiles=MakeTiles(w.S.Seed,w.S.Size);
            Array.Copy(data.Chest,w.Chest,5);
            w.players.AddRange(w.S.Players);
            if(!string.IsNullOrEmpty(installationId)&&w.players.Count>0)w.players[0].InstallationId=installationId;
            w.enemies.AddRange(w.S.Enemies);
            w.nodes.AddRange(w.S.Nodes);
            w.structures.AddRange(w.S.Structures);
            w.effects.AddRange(w.S.Effects);
            w.projectiles.AddRange(w.S.Projectiles);
            w.bossTelegraphs.AddRange(w.S.BossTelegraphs);
            w.transientLights.AddRange(w.S.Lights);
            foreach(var seat in w.S.Disconnected)if(seat!=null&&!string.IsNullOrEmpty(seat.InstallationId))w.disconnectedSeats[seat.InstallationId]=seat;
            int maxId=Math.Max(10,data.NextId);
            foreach(var p in w.players)maxId=Math.Max(maxId,p.Id+1);
            foreach(var e in w.enemies)maxId=Math.Max(maxId,e.Id+1);
            foreach(var n in w.nodes)maxId=Math.Max(maxId,n.Id+1);
            foreach(var st in w.structures)maxId=Math.Max(maxId,st.Id+1);
            foreach(var pr in w.projectiles)maxId=Math.Max(maxId,pr.Id+1);
            foreach(var bt in w.bossTelegraphs)maxId=Math.Max(maxId,bt.Id+1);
            foreach(var tl in w.transientLights)maxId=Math.Max(maxId,tl.Id+1);
            w.nextId=maxId;
            w.lastBossType=data.LastBossType;
            w.spawnClock=Math.Max(0,data.SpawnClock);
            w.petClock=Math.Max(0,data.PetClock);
            w.lastLightSpent=data.LastLightSpent;
            w.bossSpawned=data.BossSpawned;
            w.rng=new Random(unchecked(w.S.Seed^(w.S.Tick*397)^w.nextId));
            foreach(var m in data.PendingMeteors)
            {
                var owner=w.PlayerById(m.OwnerId)??w.players.FirstOrDefault();
                if(owner!=null&&m.Delay>0)w.meteors.Add((owner,m.Center,m.Delay));
            }
            w.Publish();
            return w;
        }
        public static byte[] MakeTiles(int seed,int size)
        {
            var tiles=new byte[size*size];
            int half=size/2;

            float Noise(float x,float y,float scale,int s)
            {
                float gx=x/scale,gy=y/scale;
                int x0=(int)Math.Floor(gx),y0=(int)Math.Floor(gy);
                int x1=x0+1,y1=y0+1;
                float fx=gx-x0,fy=gy-y0;
                fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);
                float H(int hx,int hy)
                {
                    int n=(hx*374761393+hy*668265263+s*1274126177)^0x5bf03635;
                    n=((n^(n>>13))*1274126177);
                    return ((n^(n>>16))&0x7FFFFFFF)/(float)0x7FFFFFFF;
                }
                float v00=H(x0,y0),v10=H(x1,y0),v01=H(x0,y1),v11=H(x1,y1);
                float v0=v00*(1-fx)+v10*fx,v1=v01*(1-fx)+v11*fx;
                return v0*(1-fy)+v1*fy;
            }

            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                if(x<3||y<3||x>=size-3||y>=size-3){tiles[y*size+x]=4;continue;}
                int dx=x-half,dy=y-half;
                float r=(float)Math.Sqrt(dx*dx+dy*dy);
                if(r>=size*0.48f){tiles[y*size+x]=4;continue;}

                float n1=Noise(x,y,36f,seed);
                float n2=Noise(x,y,18f,seed+101)*0.45f;
                float noise=n1+n2;

                bool isSanctuary=r<=7.5f;
                bool starter=dx>=4&&dx<=17&&dy>=2&&dy<=12;

                // Main cardinal bridges across the moat (radius 18 to 39)
                bool isBridge=false;
                if(r>=18&&r<=39)
                {
                    if(Math.Abs(dx)<=1&&dy!=0)isBridge=true;
                    else if(Math.Abs(dy)<=1&&dx!=0)isBridge=true;
                }

                if(isBridge)
                {
                    tiles[y*size+x]=5; // Wooden Bridge (Walkable over water)
                }
                else if(r<=22f+(noise-0.72f)*8f||isSanctuary||starter)
                {
                    tiles[y*size+x]=0; // Central Sanctuary Island (Lush Meadow Grass)
                }
                else if(r<=37f+(noise-0.72f)*10f)
                {
                    tiles[y*size+x]=1; // Surrounding Moat / Lake Water
                }
                else if(r<=size*0.38f)
                {
                    // Outer Archipelago Islands
                    tiles[y*size+x]=(byte)(noise>0.62f?0:1);
                }
                else
                {
                    // Highland / Cliff perimeter
                    tiles[y*size+x]=(byte)(noise>0.58f?2:1);
                }
            }

            // Central sanctuary paths connecting to bridges
            for(int y=4;y<size-4;y++)for(int x=4;x<size-4;x++)
            {
                int dx=x-half,dy=y-half;
                float r=(float)Math.Sqrt(dx*dx+dy*dy);
                if(r<=20&&(Math.Abs(dx)<=1||Math.Abs(dy)<=1)&&tiles[y*size+x]==0)
                {
                    tiles[y*size+x]=6; // Dirt path on central island
                }
            }

            // Place Trees ONLY on land (Grass 0 or Highland 2), NEVER on water (1) or bridges (5)
            var rng=new Random(seed);
            for(int y=4;y<size-4;y++)for(int x=4;x<size-4;x++)
            {
                byte t=tiles[y*size+x];
                if(t!=0&&t!=2)continue;
                int dx=x-half,dy=y-half;
                float r=(float)Math.Sqrt(dx*dx+dy*dy);
                if(r<=7.5f)continue; // sanctuary plaza
                if(dx>=3&&dx<=18&&dy>=1&&dy<=13)continue; // starter path
                if(Math.Abs(dx)<=2&&r<=22)continue; // central lanes
                if(Math.Abs(dy)<=2&&r<=22)continue;

                // Check 3x3 neighbors: no water or bridge near trees
                bool nearWaterOrBridge=false;
                for(int ny=-1;ny<=1;ny++)for(int nx=-1;nx<=1;nx++)
                {
                    byte nb=tiles[(y+ny)*size+(x+nx)];
                    if(nb==1||nb==5||nb==4){nearWaterOrBridge=true;break;}
                }
                if(nearWaterOrBridge)continue;

                // Tree density roll
                if(rng.NextDouble()<0.14)tiles[y*size+x]=3;
            }

            return tiles;
        }
        void GenerateNodes()
        {
            for(int i=0;i<1400;i++)
            {
                var pos=new V2(rng.Next(6,S.Size-6)-S.Size/2,rng.Next(6,S.Size-6)-S.Size/2);
                if(!Walkable(pos)||pos.Length<5)continue;
                int tx=(int)Math.Floor(pos.X+S.Size/2f),ty=(int)Math.Floor(pos.Y+S.Size/2f);
                if(tx<3||ty<3||tx>=S.Size-3||ty>=S.Size-3)continue;
                byte tile=Tiles[ty*S.Size+tx];
                if(tile==5||tile==6)continue;
                bool nearBridge=false;
                for(int ny=-1;ny<=1;ny++)for(int nx=-1;nx<=1;nx++)
                {
                    if(Tiles[(ty+ny)*S.Size+(tx+nx)]==5){nearBridge=true;break;}
                }
                if(nearBridge)continue;
                float radius=pos.Length;Resource resource=new[]{Resource.Dust,Resource.Core,Resource.Wood,Resource.Ore}[rng.Next(4)];
                if(radius<15)resource=i%3==0?Resource.Core:Resource.Dust;
                int amt=resource==Resource.Core?(radius>S.Size*.35f?5:2):resource==Resource.Wood?6:4;
                nodes.Add(new Node{Id=nextId++,Pos=pos,Resource=resource,Amount=amt,MaxAmount=amt});
            }
            // Guaranteed accessible starter resources, regardless of seed.
            for(int i=0;i<12;i++)
            {
                var sp=new V2(6+i%4*3,4+i/4*3);
                int stx=(int)Math.Floor(sp.X+S.Size/2f),sty=(int)Math.Floor(sp.Y+S.Size/2f);
                if(stx>=0&&sty>=0&&stx<S.Size&&sty<S.Size&&(Tiles[sty*S.Size+stx]==5||Tiles[sty*S.Size+stx]==6))continue;
                nodes.Add(new Node{Id=nextId++,Pos=sp,Resource=i%3==0?Resource.Core:Resource.Dust,Amount=5,MaxAmount=5});
            }
            for(int i=0;i<10;i++)
            {
                double a=i*Math.PI*.2;V2 pos=new V2((float)Math.Cos(a),(float)Math.Sin(a))*(52+i*5);
                if(Walkable(pos))enemies.Add(new Enemy{Id=nextId++,Kind=EnemyKind.Nest,Pos=pos,HP=180,MaxHP=180,AttackCD=5});
            }
        }
        V2 BeaconSpawnSpot(int slotIndex,float radius=2.4f)
        {
            float baseAngle=-1.5708f+(slotIndex%8)*(slotIndex%2==0?1:-1)*((slotIndex+1)/2)*.55f;
            for(int attempt=0;attempt<12;attempt++)
            {
                float a=baseAngle+attempt*.52f;
                V2 candidate=new V2((float)Math.Cos(a)*radius,(float)Math.Sin(a)*radius);
                if(Walkable(candidate)&&V2.Distance(candidate,BeaconColliderPos)>BeaconColliderRadius+.35f&&
                   !players.Any(q=>!q.Ghost&&!q.Extracted&&V2.Distance(q.Pos,candidate)<.9f))
                    return candidate;
            }
            return new V2(0,-radius);
        }

        public Player AddPlayer(int id,string name,Vessel vessel,int[] perks=null,int hat=-1,int pet=-1,int[] ranks=null,int[] recipes=null,string installationId=null,bool awake=false)
        {
            var existing=players.Find(p=>p.Id==id);if(existing!=null)return existing;
            if(players.Count>=S.Capacity)throw new InvalidOperationException("Room is full");
            string identity=Guid.TryParseExact(installationId,"N",out var guid)&&guid!=Guid.Empty?guid.ToString("N"):null;
            if(identity!=null&&players.Any(player=>player.InstallationId==identity))
                throw new InvalidOperationException("Player identity is already in the room");
            var displayName=new string((name??"").Where(c=>!char.IsControl(c)).Take(20).ToArray()).Trim();
            if(displayName.Length==0)displayName="Traveler";
            int partySlot=1;
            while(players.Any(member=>member.PartySlot==partySlot)||disconnectedSeats.Values.Any(seat=>seat.PartySlot==partySlot))partySlot++;
            var p=new Player{Id=id,PartySlot=partySlot,InstallationId=identity,Name=displayName,Vessel=vessel,Perks=perks??Array.Empty<int>(),Hat=hat,Pet=pet,PerkRanks=ranks??Array.Empty<int>(),Recipes=recipes,Pos=BeaconSpawnSpot(partySlot-1,2.3f),Awake=awake};
            p.MaxHP=100;p.HP=p.MaxHP;
            p.CarriesLantern=players.Count==0;
            p.Ready=players.Count==0;
            p.Bag[(int)Resource.Dust]=12;
            p.Items[0]=1;
            if(p.Has(Perk.Prepared)){p.Items[1]=1+p.Rank(Perk.Prepared);p.Items[2]=p.Rank(Perk.Prepared)==3?2:1;}
            if(p.Has(Perk.Miner))p.Weapon=Weapon.Pickaxe;
            players.Add(p);Publish();return p;
        }
        public void RemovePlayer(int id)
        {
            var removed=players.Find(p=>p.Id==id);
            if(removed!=null)
            {
                if(removed.CarriesLantern)
                {
                    var other=players.Find(p=>p.Id!=id&&!p.Ghost);
                    if(other!=null){other.CarriesLantern=true;other.Lantern=removed.Lantern;}
                }
                bool activeMatch=S.Phase==Phase.Explore||S.Phase==Phase.Eclipse||S.Phase==Phase.Extraction;
                if(activeMatch&&!removed.Extracted&&!string.IsNullOrEmpty(removed.InstallationId))
                {
                    var keptBag=new int[(int)Resource.Count];
                    for(int r=0;r<(int)Resource.Count;r++)
                    {
                        int total=Math.Max(0,removed.Bag[r]);
                        int drop=(int)Math.Round(total*.30f);
                        if(drop>total)drop=total;
                        keptBag[r]=total-drop;
                        if(drop>0)
                        {
                            double a=r*1.2566;
                            V2 dropPos=removed.Pos+new V2((float)Math.Cos(a)*.65f,(float)Math.Sin(a)*.65f);
                            nodes.Add(new Node{Id=nextId++,Pos=dropPos,Resource=(Resource)r,Amount=drop,Dropped=true});
                        }
                    }
                    disconnectedSeats[removed.InstallationId]=new DisconnectedSeat
                    {
                        InstallationId=removed.InstallationId,
                        Name=removed.Name,
                        PartySlot=removed.PartySlot,
                        Vessel=removed.Vessel,
                        HP=removed.HP,
                        MaxHP=removed.MaxHP,
                        Stamina=removed.Stamina,
                        Dread=removed.Dread,
                        Weapon=removed.Weapon,
                        SideWeapon=removed.SideWeapon,
                        AmmoWeapon=removed.AmmoWeapon,
                        Ammo=removed.Ammo,
                        HeldSlot=removed.HeldSlot,
                        HasPickaxe=removed.HasPickaxe,
                        PickaxeTier=removed.PickaxeTier,
                        PickaxeDurability=removed.PickaxeDurability,
                        LampOn=removed.LampOn,
                        Lantern=removed.Lantern,
                        Bag=keptBag,
                        Items=(int[])removed.Items.Clone(),
                        Perks=(int[])removed.Perks.Clone(),
                        PerkRanks=(int[])removed.PerkRanks.Clone(),
                        Recipes=removed.Recipes!=null?(int[])removed.Recipes.Clone():null,
                        Hat=removed.Hat,
                        Pet=removed.Pet,
                        Gathered=removed.Gathered,
                        Crafted=removed.Crafted,
                        LastPos=removed.Pos,
                        DisconnectedAt=S.Time,
                        Awake=removed.Awake
                    };
                    Announce(" đã mất kết nối — Hành trang được giữ tại Hải Đăng (rơi 30% tài nguyên tại chỗ cũ).",removed.Name);
                }
            }
            players.RemoveAll(p=>p.Id==id);commands.Remove(id);actions.Remove(id);inputAt.Remove(id);navigation.Remove(id);Publish();
        }
        public bool CanRejoin(string installationId)
        {
            if(S.Phase!=Phase.Explore&&S.Phase!=Phase.Eclipse&&S.Phase!=Phase.Extraction)return false;
            if(!Guid.TryParseExact(installationId,"N",out var guid)||guid==Guid.Empty)return false;
            string key=guid.ToString("N");
            return disconnectedSeats.ContainsKey(key)&&!players.Any(p=>p.InstallationId==key);
        }
        public Player RejoinPlayer(int newId,string installationId)
        {
            if(!CanRejoin(installationId))throw new InvalidOperationException("Cannot rejoin this match");
            string key=Guid.ParseExact(installationId,"N").ToString("N");
            var seat=disconnectedSeats[key];
            disconnectedSeats.Remove(key);
            V2 spawnPos=S.BossArenaActive
                ?FindArenaSpot(S.BossArenaCenter,2.4f,Math.Max(3.5f,S.BossArenaRadius-2.5f),seat.PartySlot)
                :BeaconSpawnSpot(seat.PartySlot-1,2.5f);
            bool wasGhost=seat.HP<=0;
            var restored=new Player
            {
                Id=newId,
                PartySlot=seat.PartySlot,
                InstallationId=seat.InstallationId,
                Name=seat.Name,
                Vessel=seat.Vessel,
                Awake=seat.Awake,
                Pos=spawnPos,
                HP=wasGhost?0f:Math.Max(30f,Math.Min(seat.MaxHP,seat.HP)),
                MaxHP=seat.MaxHP,
                Stamina=100,
                Dread=seat.Dread,
                Invulnerable=wasGhost?0f:2.0f,
                Weapon=seat.Weapon,
                SideWeapon=seat.SideWeapon,
                AmmoWeapon=seat.AmmoWeapon,
                Ammo=seat.Ammo,
                HeldSlot=seat.HeldSlot,
                HasPickaxe=seat.HasPickaxe,
                PickaxeTier=seat.PickaxeTier,
                PickaxeDurability=seat.PickaxeDurability,
                LampOn=seat.LampOn,
                Lantern=seat.Lantern,
                Bag=(int[])seat.Bag.Clone(),
                Items=(int[])seat.Items.Clone(),
                Perks=(int[])seat.Perks.Clone(),
                PerkRanks=(int[])seat.PerkRanks.Clone(),
                Recipes=seat.Recipes!=null?(int[])seat.Recipes.Clone():null,
                Hat=seat.Hat,
                Pet=seat.Pet,
                Gathered=seat.Gathered,
                Crafted=seat.Crafted,
                Ready=true
            };
            players.Add(restored);
            FX(restored.Pos,restored.Pos,2);
            Announce(S.BossArenaActive
                ?" đã kết nối lại và được kéo vào đấu trường Boss!"
                :wasGhost?" đã kết nối lại (đang gục, chờ hồi sinh tại Hải Đăng)."
                :" đã kết nối lại và hồi sinh tại Hải Đăng!",restored.Name);
            Publish();
            return restored;
        }
        public void SetReady(int id,bool ready){if(S.Phase==Phase.Sanctuary){var p=PlayerById(id);if(p!=null)p.Ready=ready;}}
        public bool StartRun(bool requireReady=false)
        {
            if(players.Count==0||S.Phase!=Phase.Sanctuary||(requireReady&&players.Any(p=>!p.Ready)))return false;
            spawnClock=18f;
            S.NextBossEventTime=S.Time+GetBossEventInterval(S.Difficulty);
            S.Phase=Phase.Explore;return true;
        }
        public void Submit(int id,Command c)
        {
            if(c==null||PlayerById(id)==null)return;if(float.IsNaN(c.X)||float.IsNaN(c.Y)||float.IsInfinity(c.X)||float.IsInfinity(c.Y))return;
            commands[id]=new Command{X=c.X,Y=c.Y};inputAt[id]=S.Time;
            if(c.HasActions){if(!actions.TryGetValue(id,out var queue)){queue=new Queue<Command>();actions[id]=queue;}if(queue.Count<32)queue.Enqueue(c);}
        }
        public bool Walkable(V2 p)
        {
            int x=(int)Math.Floor(p.X+S.Size/2f),y=(int)Math.Floor(p.Y+S.Size/2f);
            if(x<3||y<3||x>=S.Size-3||y>=S.Size-3)return false;
            byte tile=Tiles[y*S.Size+x];
            if(tile==1||tile==3||tile==4||tile==7)return false;
            return !structures.Any(s=>s.Kind==2&&s.HP>0&&V2.Distance(s.Pos,p)<.8f);
        }
        V2 Move(V2 from,V2 delta,Player body=null,Enemy monster=null,bool ignoreEnemies=false)
        {
            // Substeps prevent dash tunneling through 1-tile obstacles.
            int n=Math.Max(1,(int)Math.Ceiling(delta.Length/.2f));V2 step=delta/n;
            for(int i=0;i<n;i++)
            {
                var x=new V2(from.X+step.X,from.Y);if(CanStep(from,x,body,monster,ignoreEnemies))from=x;
                var y=new V2(from.X,from.Y+step.Y);if(CanStep(from,y,body,monster,ignoreEnemies))from=y;
            }
            return from;
        }
        bool CanStep(V2 from,V2 to,Player body,Enemy monster,bool ignoreEnemies)
        {
            if(!Walkable(to))return false;
            if(body==null&&monster==null)return true;
            const float radius=.24f;
            if(!Walkable(to+new V2(radius,0))||!Walkable(to-new V2(radius,0))||
               !Walkable(to+new V2(0,radius))||!Walkable(to-new V2(0,radius)))return false;
            if(ApproachesBody(from,to,BeaconColliderPos,BeaconColliderRadius))return false;
            if(S.BossArenaActive)
            {
                if(body!=null)
                {
                    float maxR=Math.Max(2f,S.BossArenaRadius-.35f);
                    if(V2.Distance(to,S.BossArenaCenter)>maxR&&V2.Distance(to,S.BossArenaCenter)>V2.Distance(from,S.BossArenaCenter))return false;
                }
                else if(monster!=null&&monster.Id==S.BossId)
                {
                    float maxR=Math.Max(2f,S.BossArenaRadius-.75f);
                    if(V2.Distance(to,S.BossArenaCenter)>maxR&&V2.Distance(to,S.BossArenaCenter)>V2.Distance(from,S.BossArenaCenter))return false;
                }
            }
            foreach(var player in players)
                if(player!=body&&!player.Ghost&&!player.Extracted&&ApproachesBody(from,to,player.Pos,.55f))return false;
            foreach(var enemy in enemies)
                if(!ignoreEnemies&&enemy!=monster&&enemy.HP>0&&ApproachesBody(from,to,enemy.Pos,enemy.Kind==EnemyKind.Boss?.85f:.55f))return false;
            return true;
        }

        static bool ApproachesBody(V2 from,V2 to,V2 center,float distance)
        {
            float next=V2.Distance(to,center);
            return next<distance&&next<V2.Distance(from,center);
        }

        V2 Navigate(V2 from,V2 goal,int key)
        {
            if(V2.Distance(from,goal)<1.4f)return (goal-from).Normal;
            int size=S.Size,gx=(int)Math.Floor(goal.X+size/2f),gy=(int)Math.Floor(goal.Y+size/2f),goalCell=gy*size+gx;
            if(gx<3||gy<3||gx>=size-3||gy>=size-3)return (goal-from).Normal;
            if(!navigation.TryGetValue(key,out var field)){field=new FlowField{Distance=new int[size*size]};navigation[key]=field;}
            if(field.Goal!=goalCell||S.Time-field.Built>.8f)
            {
                field.Goal=goalCell;field.Built=S.Time;for(int i=0;i<field.Distance.Length;i++)field.Distance[i]=int.MaxValue;
                var frontier=new Queue<int>();frontier.Enqueue(goalCell);field.Distance[goalCell]=0;int budget=8000;
                while(frontier.Count>0&&budget-->0)
                {
                    int at=frontier.Dequeue(),x=at%size,y=at/size;if(Math.Abs(x-gx)>40||Math.Abs(y-gy)>40)continue;
                    foreach(int delta in new[]{-1,1,-size,size})
                    {
                        int next=at+delta,nx=next%size,ny=next/size;
                        if(nx<3||ny<3||nx>=size-3||ny>=size-3||Math.Abs(nx-x)+Math.Abs(ny-y)!=1||field.Distance[next]!=int.MaxValue)continue;
                        if(!Walkable(new V2(nx-size/2+.5f,ny-size/2+.5f)))continue;field.Distance[next]=field.Distance[at]+1;frontier.Enqueue(next);
                    }
                }
            }
            int fx=(int)Math.Floor(from.X+size/2f),fy=(int)Math.Floor(from.Y+size/2f),index=fy*size+fx;
            if(index<0||index>=field.Distance.Length)return (goal-from).Normal;
            int best=index,distance=field.Distance[index];
            foreach(int delta in new[]{-1,1,-size,size}){int next=index+delta;if(next<0||next>=field.Distance.Length)continue;if(field.Distance[next]<distance){distance=field.Distance[next];best=next;}}
            if(best==index)return (goal-from).Normal;
            return (new V2(best%size-size/2+.5f,best/size-size/2+.5f)-from).Normal;
        }
        public bool IsSafe(V2 pos)
        {
            float beaconSafeRadius=4.8f+(S.Charge/100f)*2.2f;
            if((S.Charge>0||S.BeaconHP>0)&&pos.Length<beaconSafeRadius)return true;
            foreach(var p in players)
            {
                if(p.Ghost||p.Extracted)continue;
                bool lit=p.HasHeldTorch&&(S.Fuel>0||S.LastLight>0);
                if(lit&&V2.Distance(pos,p.Pos)<(p.Lantern==Lantern.Relic?8:5))return true;
                if(p.HasBuff(BuffKind.LightAura)&&V2.Distance(pos,p.Pos)<4.5f)return true;
            }
            if(transientLights.Any(l=>l.GrantsSafety&&l.Life>0&&V2.Distance(pos,l.Pos)<l.Radius*.85f))return true;
            return structures.Any(s=>(s.Kind==0||s.Kind==1)&&s.Life>0&&V2.Distance(pos,s.Pos)<(s.Kind==1?8:4));
        }
        public bool CanHarm(Player a,Player b)
        {
            if(a.Id==b.Id||a.Ghost||b.Ghost||a.Extracted||b.Extracted||IsSafe(a.Pos)||IsSafe(b.Pos))return false;
            long pair=((long)a.Id<<32)|(uint)b.Id;
            return a.Dread>=90||(b.Dread>=90&&retaliation.ContainsKey(pair));
        }
        public Player PlayerById(int id)=>players.Find(p=>p.Id==id);
        // Test tools are local host API, never accepted through network Command.
        public bool DebugEnabled,DebugInvulnerable,DebugFreezeDread,DebugInfiniteFuel,DebugNoEnemies;
        public bool DebugScenario(int scenario,int playerId=1)
        {
            if(!DebugEnabled||scenario<0||scenario>3)return false;
            if(S.Phase==Phase.Sanctuary)StartRun();if(S.Phase==Phase.Won||S.Phase==Phase.Lost){S.Phase=Phase.Explore;S.TeamExtracted=0;}
            var subject=PlayerById(playerId);if(subject==null)return false;
            int slot=0;foreach(var p in players)
            {
                p.HP=p.MaxHP;p.Extracted=false;p.Stamina=100;p.LampOn=scenario==0;
                if(scenario==0){p.HeldSlot=2;p.Items[0]=Math.Max(1,p.Items[0]);}
                V2 position=scenario==0?BeaconSpawnSpot(slot,2.3f):new V2(12+slot*1.25f,0);
                for(int radius=0;radius<20&&!Walkable(position);radius++)position=new V2(12+slot*1.25f,radius*.5f);
                p.Pos=position;p.Dread=p.Id==playerId?(scenario==1?65:scenario==2?95:scenario==3?35:0):0;p.Safe=IsSafe(p.Pos);p.MirageCD=0;slot++;
            }
            DebugFreezeDread=true;S.Fuel=100;S.BeaconHP=S.Charge>=S.ChargeTarget?600:0;S.Blackout=0;
            Announce("DEBUG scenario "+scenario);Publish();return true;
        }
        public bool DebugAction(string action,int playerId=1)
        {
            if(!DebugEnabled)return false;var p=PlayerById(playerId);if(p==null)return false;
            switch(action)
            {
                case "supplies":for(int i=0;i<5;i++)p.Bag[i]=100;for(int i=0;i<4;i++)p.Items[i]=10;break;
                case "heal":p.HP=p.MaxHP;p.Dread=0;p.Stamina=100;p.SkillCD=p.DashCD=0;break;
                case "ghost":p.HP=0;break;
                case "beacon":p.Pos=BeaconSpawnSpot(0,2.3f);p.LampOn=true;S.BeaconHP=S.Charge>=S.ChargeTarget?600:0;break;
                case "eclipse":S.Charge=S.ChargeTarget;S.BeaconHP=600;S.Wave=90;S.Phase=Phase.Eclipse;break;
                case "portal":S.Charge=S.ChargeTarget;S.BeaconHP=600;S.Wave=0;S.Phase=Phase.Extraction;break;
                case "blackout":DebugInfiniteFuel=false;S.Fuel=0;S.LastLight=0;S.BeaconHP=0;lastLightSpent=true;break;
                case "enemy":enemies.Add(new Enemy{Id=nextId++,Kind=EnemyKind.Shade,Pos=p.Pos+new V2(3,0),HP=42,MaxHP=42,SkillCD=1.6f});break;
                case "boss":enemies.Add(new Enemy{Id=nextId++,Kind=EnemyKind.Boss,Elite=true,Pos=p.Pos+new V2(5,0),HP=600,MaxHP=600,SkillCD=2.5f});break;
                case "boss_event":return TriggerWorldBossEvent(null,playerId);
                case "boss_void":return TriggerWorldBossEvent((int)BossType.VoidSovereign,playerId);
                case "boss_cinder":return TriggerWorldBossEvent((int)BossType.CinderBehemoth,playerId);
                case "boss_eclipse":return TriggerWorldBossEvent((int)BossType.EclipseWeaver,playerId);
                default:return false;
            }
            Announce("DEBUG "+action);Publish();return true;
        }
        public void Tick(float dt=Step)
        {
            if(dt<=0||dt>.2f)throw new ArgumentOutOfRangeException(nameof(dt));
            if(S.Phase==Phase.Sanctuary||S.Phase==Phase.Won||S.Phase==Phase.Lost){Publish();return;}
            S.Tick++;S.Time+=dt;float oldFuel=S.Fuel;if(players.Any(p=>p.HasHeldTorch))S.Fuel=Math.Max(0,S.Fuel-dt*.13f);if(S.Phase==Phase.Explore&&S.Time>=60f)S.Fuel=Math.Max(0,S.Fuel-dt*.35f);S.LastLight=Math.Max(0,S.LastLight-dt);
            if((!DebugEnabled||!DebugNoEnemies)&&S.Phase==Phase.Explore&&S.Time>=S.PeriodicWave*60f)
            {
                S.PeriodicWave++;
                foreach(var lp in players.Where(p=>!p.Ghost&&!p.Extracted))
                {
                    for(int wi=0;wi<2;wi++)
                    {
                        double wa=rng.NextDouble()*Math.PI*2;
                        V2 wpos=lp.Pos+new V2((float)Math.Cos(wa)*10f,(float)Math.Sin(wa)*10f);
                        if(Walkable(wpos))enemies.Add(new Enemy{Id=nextId++,Kind=EnemyKind.VoidStalker,Pos=wpos,HP=85,MaxHP=85,AttackCD=1.5f});
                    }
                }
            }
            S.BossAlertTimer=Math.Max(0,S.BossAlertTimer-dt);
            S.BossSkillCalloutTimer=Math.Max(0,S.BossSkillCalloutTimer-dt);
            if(DebugEnabled&&DebugInfiniteFuel)S.Fuel=100;
            if(oldFuel>0&&S.Fuel<=0&&!lastLightSpent&&players.Any(p=>p.Has(Perk.LastLight))){S.LastLight=5*players.Where(p=>p.Has(Perk.LastLight)).Max(p=>p.PerkStrength(Perk.LastLight));lastLightSpent=true;Announce("Tia sáng cuối — "+S.LastLight.ToString("0")+" giây!");}
            if((!DebugEnabled||!DebugNoEnemies)&&S.Phase==Phase.Explore&&!S.BossArenaActive&&S.Time>=S.NextBossEventTime&&players.Any(p=>!p.Ghost&&!p.Extracted))
                TriggerWorldBossEvent();
            foreach(var p in players)UpdatePlayer(p,dt);
            if(!DebugEnabled||!DebugNoEnemies)foreach(var e in enemies.ToArray())UpdateEnemy(e,dt);
            UpdateProjectiles(dt);
            UpdateMeteors(dt);
            UpdateBossTelegraphs(dt);
            UpdateTransientLights(dt);
            for(int i=combatPopups.Count-1;i>=0;i--){combatPopups[i].Life-=dt;if(combatPopups[i].Life<=0)combatPopups.RemoveAt(i);}
            enemies.RemoveAll(e=>e.HP<=0);
            if(S.BossArenaActive&&!enemies.Any(e=>e.Id==S.BossId&&e.HP>0))EndBossArena();
            foreach(var s in structures)
            {
                if(s.Kind<=1)s.Life-=dt;
                if(s.Kind==3)foreach(var e in enemies)if(V2.Distance(e.Pos,s.Pos)<2){Damage(e,dt*15);e.Blind=Math.Max(e.Blind,.3f);s.Life-=dt;}
            }
            structures.RemoveAll(s=>s.HP<=0||s.Life<=0);
            foreach(var e in effects)e.Life-=dt;effects.RemoveAll(e=>e.Life<=0);
            bool beaconActive=S.Fuel>0||S.BeaconHP>0||S.Charge>0;
            if((S.Phase==Phase.Explore||S.Phase==Phase.Extraction)&&S.Charge>0&&S.Time>=15f)
            {
                float drainRate=S.Charge>S.ChargeTarget?1.0f:0.45f;
                S.Charge=Math.Max(0f,S.Charge-dt*drainRate);
            }
            float chargeTier=S.Charge/100f;
            float spawnRate=((beaconActive&&S.Phase==Phase.Explore)?1.65f:1f)*(1f+chargeTier*0.25f);
            int effectiveMobCap=(int)(MaxActiveEnemies*(1f+chargeTier*0.20f));
            spawnClock-=dt*spawnRate;if((!DebugEnabled||!DebugNoEnemies)&&spawnClock<=0&&enemies.Count(e=>e.Kind!=EnemyKind.Nest)<effectiveMobCap){SpawnEnemy();spawnClock=S.Phase==Phase.Eclipse?EclipseSpawnInterval:ExploreSpawnInterval;}
            if((!DebugEnabled||!DebugNoEnemies)&&S.Phase==Phase.Eclipse&&S.Wave<60&&!bossSpawned)
            {
                bossSpawned=true;var pos=new V2(0,18);float hp=450+players.Count*180;enemies.Add(new Enemy{Id=nextId++,Kind=EnemyKind.Boss,Elite=true,Pos=pos,HP=hp,MaxHP=hp,SkillCD=3.2f});Announce("Một thực thể lớn đang tiến về hải đăng.");
            }
            petClock+=dt;if(petClock>=2){petClock=0;foreach(var p in players.Where(p=>!p.Ghost&&p.Pet==(int)Pet.Firefly))Harvest(p,4,true);}
            for(int tri=treeRespawns.Count-1;tri>=0;tri--)
            {
                var tr=treeRespawns[tri];
                if(S.Time>=tr.RespawnAt)
                {
                    V2 treePos=new V2(tr.Tx-S.Size/2f+.5f,tr.Ty-S.Size/2f+.5f);
                    bool occupied=players.Any(p=>!p.Ghost&&!p.Extracted&&V2.Distance(p.Pos,treePos)<1.1f)||enemies.Any(e=>e.HP>0&&V2.Distance(e.Pos,treePos)<1.1f);
                    if(occupied){tr.RespawnAt=S.Time+5f;treeRespawns[tri]=tr;}
                    else
                    {
                        Tiles[tr.Ty*S.Size+tr.Tx]=tr.Tile==2?(byte)2:(byte)3;
                        treeRespawns.RemoveAt(tri);
                        FX(treePos,treePos,7);
                        Publish();
                    }
                }
            }
            if(S.Phase==Phase.Eclipse){S.Wave=Math.Max(0,S.Wave-dt);if(S.Wave<=0){S.Phase=Phase.Extraction;Announce("Cổng đã mở! Về hải đăng và nhấn E.");}}
            if(S.Fuel<=0&&S.LastLight<=0&&S.BeaconHP<=0){S.Blackout+=dt;if(S.Blackout>=10){S.Phase=Phase.Lost;Announce("Bóng tối nuốt chửng cả nhóm.");}}else S.Blackout=0;
            if(players.Count>0&&players.All(p=>p.Ghost||p.Extracted))S.Phase=S.TeamExtracted>0?Phase.Won:Phase.Lost;
            foreach(var c in commands.Values){c.Attack=c.Skill=c.Dash=c.Interact=c.Refuel=c.Deposit=c.ToggleLamp=c.PassLamp=c.Push=c.Revive=c.Reload=false;c.Craft=c.Use=-1;c.Equip=0;}
            Publish();
        }
        void UpdatePlayer(Player p,float dt)
        {
            p.PushCD=Math.Max(0,p.PushCD-dt);
            if(p.ReloadTimer>0)
            {
                p.ReloadTimer=Math.Max(0,p.ReloadTimer-dt);
                if(p.ReloadTimer<=0&&p.MaxAmmo>0){p.AmmoWeapon=p.ActiveWeapon;p.Ammo=p.MaxAmmo;}
            }
            p.AttackCD=Math.Max(0,p.AttackCD-dt);p.SkillCD=Math.Max(0,p.SkillCD-dt);p.DashCD=Math.Max(0,p.DashCD-dt);p.CastTime=Math.Max(0,p.CastTime-dt);p.Invulnerable=Math.Max(0,p.Invulnerable-dt);p.ShieldTime=Math.Max(0,p.ShieldTime-dt);p.HarvestCD=Math.Max(0,p.HarvestCD-dt);
            if(p.ShieldTime<=0)p.Shield=0;
            if(p.Buffs!=null&&p.Buffs.Count>0)
            {
                for(int i=p.Buffs.Count-1;i>=0;i--)
                {
                    var b=p.Buffs[i];
                    b.Duration-=dt;
                    if(b.Kind==BuffKind.Regen&&b.Value>0)p.HP=Math.Min(p.MaxHP,p.HP+b.Value*dt);
                    if(b.Kind==BuffKind.DreadWard&&b.Value>0)p.Dread=Math.Max(0,p.Dread-b.Value*dt);
                    if(b.Duration<=0)p.Buffs.RemoveAt(i);
                    else p.Buffs[i]=b;
                }
            }
            if(p.Extracted)return;
            p.Safe=IsSafe(p.Pos);
            if(p.Safe)foreach(var key in retaliation.Keys.Where(k=>(int)(k>>32)==p.Id||(int)(uint)k==p.Id).ToArray())retaliation.Remove(key);
            var stats=p.Stats;
            bool alone=!players.Any(q=>q.Id!=p.Id&&!q.Ghost&&V2.Distance(p.Pos,q.Pos)<12);
            float dreadRate=p.Safe?-5*stats.DreadCalmMultiplier:(alone?3:1.5f)*DifficultyDreadScale*stats.DreadGainMultiplier;
            if(p.Weapon==Weapon.ShadowBlades&&!p.Safe)dreadRate+=.5f;
            if(dreadRate>0&&p.Has(Perk.SteelNerves))dreadRate*=1-.25f*p.PerkStrength(Perk.SteelNerves);
            if(dreadRate>0&&p.Hat==(int)Hat.Candle)dreadRate*=.9f;
            if(!DebugEnabled||!DebugFreezeDread)p.Dread=Clamp(p.Dread+dt*dreadRate,0,100);
            float stamMult=stats.StaminaRegenMultiplier*(p.Hat==(int)Hat.Witch?1.08f:1);
            if(p.HasBuff(BuffKind.StaminaSurge))stamMult*=(1f+p.BuffValue(BuffKind.StaminaSurge));
            p.Stamina=Clamp(p.Stamina+dt*(p.Safe?8f:5f)*stamMult,0,100);
            if(p.HeldSlot!=8)p.PrevHeldSlot=p.HeldSlot;
            if(!commands.TryGetValue(p.Id,out var movement))movement=new Command();
            if(inputAt.TryGetValue(p.Id,out var received)&&S.Time-received>.5f){movement.X=movement.Y=0;}
            Command c=new Command{X=movement.X,Y=movement.Y};
            if(actions.TryGetValue(p.Id,out var queue)&&queue.Count>0){c=queue.Dequeue();c.X=movement.X;c.Y=movement.Y;}
            p.Move=new V2(c.X,c.Y);if(p.Move.Length>1)p.Move=p.Move.Normal;
            if(Math.Abs(p.Move.X)>.01f)p.Right=p.Move.X>0;
            float speed=stats.MoveSpeed;
            if(p.HasBuff(BuffKind.Swiftness))speed*=(1f+p.BuffValue(BuffKind.Swiftness));
            if(p.Load>p.Capacity)speed*=.55f;
            if(p.Ghost){p.ClearBuffs();p.Move=new V2();return;}
            if(c.ToggleLamp&&p.HeldSlot==2&&p.Items[0]>0)p.LampOn=!p.LampOn;
            if(c.PassLamp&&p.CarriesLantern)
            {
                var receiver=players.Where(q=>q.Id!=p.Id&&!q.Ghost&&!q.CarriesLantern&&V2.Distance(q.Pos,p.Pos)<3).OrderBy(q=>V2.Distance(q.Pos,p.Pos)).FirstOrDefault();
                if(receiver!=null){receiver.CarriesLantern=true;receiver.Lantern=p.Lantern;receiver.LampOn=p.LampOn;p.CarriesLantern=false;Announce(" trao đèn cho ",p.Name,receiver.Name);}
            }
            p.MirageCD-=dt;p.MirageLife=Math.Max(0,p.MirageLife-dt);
            if(p.Safe)p.MirageLife=0;
            if(p.Dread>=60&&!p.Safe&&p.MirageCD<=0)
            {
                var position=p.Pos+new V2(p.Right?4:-4,2);if(Walkable(position)){p.MiragePos=position;p.MirageLife=14;}p.MirageCD=20;
            }
            float dashCost=25*stats.StaminaCostMultiplier*(p.Has(Perk.GhostStep)?1-.2f*p.PerkStrength(Perk.GhostStep):1);
            if(c.Dash&&p.DashCD<=0&&p.Stamina>=dashCost)
            {p.Stamina-=dashCost;p.DashDir=EightWay(p.Move.Length>.1f?p.Move:new V2(p.Right?1:-1,0));p.DashCD=stats.DashCooldown;p.DashTime=.16f;p.Invulnerable=.16f;}
            if(p.DashTime>0){p.DashTime-=dt;p.Pos=Move(p.Pos,p.DashDir*17*dt,p,ignoreEnemies:true);}else p.Pos=Move(p.Pos,p.Move*speed*dt,p);
            if(c.Refuel)
            {
                if(p.Bag[0]>0&&S.Fuel<100){int amount=Math.Min(p.Bag[0],(int)Math.Ceiling((100-S.Fuel)/15f));p.Bag[0]-=amount;S.Fuel=Math.Min(100,S.Fuel+amount*15f);}
                if(p.Bag[3]>0&&S.Fuel<100){int amount=Math.Min(p.Bag[3],(int)Math.Ceiling((100-S.Fuel)/12f));p.Bag[3]-=amount;S.Fuel=Math.Min(100,S.Fuel+amount*12f);}
                if(p.Bag[1]>0&&S.Fuel<100){int amount=Math.Min(p.Bag[1],(int)Math.Ceiling((100-S.Fuel)/50f));p.Bag[1]-=amount;S.Fuel=Math.Min(100,S.Fuel+amount*50f);}
            }
            if(c.Deposit&&p.Pos.Length<4&&(S.Phase==Phase.Explore||S.Phase==Phase.Eclipse||S.Phase==Phase.Extraction))
            {
                int amount=p.Bag[1];
                if(amount>0)
                {
                    int prevMilestone=(int)(S.Charge/100f);
                    p.Bag[1]-=amount;
                    S.Charge+=amount*CoreChargeValue;
                    int newMilestone=(int)(S.Charge/100f);
                    if(newMilestone>prevMilestone)
                    {
                        Announce(EmberLanguage.English
                            ?$"⚡ Beacon reached {newMilestone*100} Energy! Safe light expanded, attracting more shadow creatures!"
                            :$"⚡ Hải đăng đạt mốc {newMilestone*100} NL! Vùng sáng mở rộng, thu hút thêm quái vật bóng tối!");
                        FX(new V2(),new V2(),2);
                    }
                    if(S.Phase==Phase.Explore&&S.Charge>=S.ChargeTarget)
                    {
                        S.BeaconHP=600;
                        S.Phase=Phase.Eclipse;
                        S.Wave=90;
                        Announce("NHẬT THỰC — bảo vệ hải đăng 90 giây!");
                    }
                    else if(S.Charge>S.ChargeTarget)
                    {
                        Announce((EmberLanguage.English?"Crystal Overcharged: ":"Quá tải tinh thể: ")+S.Charge+" NL! Cổng trở về sẵn sàng!");
                    }
                }
            }
            if(c.Use==10&&p.Pos.Length<4.5f)
            {
                if(p.Bag[0]>=2)
                {
                    p.Bag[0]-=2;
                    p.Stamina=Math.Min(100f,p.Stamina+50f);
                    p.Dread=Math.Max(0f,p.Dread-30f);
                    FX(p.Pos,p.Pos,2);
                    Announce(EmberLanguage.English?"Lumidust Tea: +50 Stamina, -30 Dread!":"Trà Bụi Quang: +50 Thể lực, -30 Hoảng loạn!");
                }
                else Announce(EmberLanguage.English?"Need 2 Lumidust to brew tea!":"Cần 2 Bụi Quang để pha trà!");
            }
            else if(c.Use==11&&p.Pos.Length<4.5f)
            {
                if(p.Bag[2]>=2&&p.HasPickaxe)
                {
                    p.Bag[2]-=2;
                    p.PickaxeDurability=Math.Min(p.PickaxeMaxDurability,p.PickaxeDurability+40);
                    FX(p.Pos,p.Pos,1);
                    Announce(EmberLanguage.English?"Sharpened Pickaxe: +40 Durability!":"Đã mài cuốc: +40 Độ bền!");
                }
                else Announce(EmberLanguage.English?"Need 2 Ore to sharpen pickaxe!":"Cần 2 Quặng để mài cuốc!");
            }
            if(c.Revive)ReviveAtBeacon(p);
            if(c.Interact)
            {
                if(p.MirageLife>0&&V2.Distance(p.Pos,p.MiragePos)<2){p.MirageLife=0;Hurt(p,15);FX(p.Pos,p.Pos,3);Announce("Ảo ảnh vỡ tung.");}
                else if(S.Phase==Phase.Extraction&&p.Pos.Length<3){p.Extracted=true;S.TeamExtracted++;Announce(" đã rút lui.",p.Name);}
                else if(structures.Any(s=>s.Kind==4&&V2.Distance(s.Pos,p.Pos)<2))
                {
                    if(p.Load>0)for(int r=0;r<5;r++){Chest[r]+=p.Bag[r];p.Bag[r]=0;}
                    else for(int r=0;r<5;r++){int count=Math.Min(Chest[r],Math.Max(0,(int)p.Capacity-p.Load));Chest[r]-=count;p.Bag[r]+=count;}
                }
                else Harvest(p,2.4f);
            }
            if(c.Craft>=0&&c.Craft<Catalog.Recipes.Length)Craft(p,(Recipe)c.Craft);
            if(c.Equip>=1&&c.Equip<=8)
            {
                if(p.HeldSlot!=c.Equip){p.HeldSlot=c.Equip;p.ReloadTimer=0;}
            }
            if(c.Reload&&p.HoldingWeapon&&p.MaxAmmo>0&&p.ReloadTimer<=0&&p.CurrentAmmo<p.MaxAmmo)
            {
                p.AmmoWeapon=p.ActiveWeapon;
                p.ReloadTimer=Catalog.WeaponReloadTime(p.ActiveWeapon);
            }
            if(c.Use>=0&&c.Use<4)Use(p,c.Use);
            if(c.Push)PushEnemy(p);
            if(c.Attack&&p.HoldingWeapon&&p.AttackCD<=0&&p.CastTime<=0)
            {
                bool isHarvestTool=p.HeldSlot==8||p.ActiveWeapon==Weapon.Pickaxe||p.ActiveWeapon==Weapon.CrystalAxe;
                if(isHarvestTool)
                {
                    if(Harvest(p,2.8f))p.AttackCD=p.HarvestCD;
                    else if(!IsNearHarvestTarget(p,2.8f))Attack(p);
                }
                else Attack(p);
            }
            if(c.Skill&&p.SkillCD<=0&&p.CastTime<=0)Skill(p);
            if(p.HasHeldTorch&&p.Lantern==Lantern.Inferno&&S.Fuel>0)foreach(var e in enemies)if(V2.Distance(e.Pos,p.Pos)<5)Damage(e,dt*8*DamageMultiplier(p));
            if(S.Fuel>0)foreach(var carrier in players.Where(q=>q.HasHeldTorch&&q.Lantern==Lantern.Relic))
            {
                if(V2.Distance(carrier.Pos,p.Pos)>=8)continue;p.HP=Math.Min(p.MaxHP,p.HP+dt*3);p.Dread=Math.Max(0,p.Dread-dt*3);p.Shield=Math.Min(20,p.Shield+dt*2);p.ShieldTime=Math.Max(p.ShieldTime,.2f);
            }
        }
        public void DropItem(Player p,int kind,int id,int amount)
        {
            if(p==null||p.Ghost||amount<=0)return;
            V2 dropPos=p.Pos+new V2((float)(rng.NextDouble()*1.2-0.6),(float)(rng.NextDouble()*1.2-0.6));
            if(kind==3)
            {
                int r=Math.Max(0,Math.Min((int)Resource.Count-1,id));
                int actual=Math.Min(amount,p.Bag[r]);
                if(actual<=0)return;
                p.Bag[r]-=actual;
                nodes.Add(new Node{Id=nextId++,Pos=dropPos,Resource=(Resource)r,Amount=actual,Dropped=true});
                string resName=EmberLanguage.English?new[]{"Lumen Dust","Core","Umbra Shard","Wood","Ore"}[r]:Catalog.ResourceNames[r];
                Announce((EmberLanguage.English?"Dropped: ":"Đã vứt: ")+actual+" "+resName);
            }
            else if(kind==2)
            {
                int c=Math.Max(0,Math.Min(3,id));
                int actual=Math.Min(amount,p.Items[c]);
                if(actual<=0)return;
                p.Items[c]-=actual;
                nodes.Add(new Node{Id=nextId++,Pos=dropPos,Resource=c==0?Resource.Wood:Resource.Ore,Amount=actual,Dropped=true});
                string itemName=EmberLanguage.English?new[]{"Torch","Flare","Sedative","Revival Stone"}[c]:new[]{"Đuốc","Pháo sáng","Thuốc an thần","Đá hồi sinh"}[c];
                Announce((EmberLanguage.English?"Dropped: ":"Đã vứt: ")+actual+" "+itemName);
            }
            else if(kind==1&&id==8)
            {
                if(p.HasPickaxe)
                {
                    p.HasPickaxe=false;
                    if(p.HeldSlot==8)p.HeldSlot=6;
                    nodes.Add(new Node{Id=nextId++,Pos=dropPos,Resource=Resource.Ore,Amount=2,Dropped=true});
                    Announce(EmberLanguage.English?"Dropped Pickaxe":"Đã vứt Cuốc đào");
                }
            }
            Publish();
        }
        bool Harvest(Player p,float range,bool dustOnly=false)
        {
            if(!dustOnly&&p.HarvestCD>0)return false;
            var node=nodes.Where(n=>(!dustOnly||n.Resource==Resource.Dust)&&V2.Distance(n.Pos,p.Pos)<range).OrderBy(n=>V2.Distance(n.Pos,p.Pos)).FirstOrDefault();
            if(node==null&&!dustOnly)
            {
                // Chopping obstacle tree tiles directly on the map
                int size=S.Size;
                int px=(int)Math.Floor(p.Pos.X+size/2f);
                int py=(int)Math.Floor(p.Pos.Y+size/2f);
                int bestTx=-1,bestTy=-1;float bestDist=float.MaxValue;byte targetTile=0;
                for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++)
                {
                    int tx=px+dx,ty=py+dy;
                    if(tx<0||ty<0||tx>=size||ty>=size)continue;
                    byte t=Tiles[ty*size+tx];
                    if(t==3||t==2||t==7)
                    {
                        V2 blockPos=new V2(tx-size/2f+.5f,ty-size/2f+.5f);
                        float d=V2.Distance(p.Pos,blockPos);
                        if(d<range+.4f&&d<bestDist){bestDist=d;bestTx=tx;bestTy=ty;targetTile=t;}
                    }
                }
                if(bestTx>=0)
                {
                    if(targetTile==7)
                    {
                        if(p.Bag[(int)Resource.Wood]>=4&&p.Bag[(int)Resource.Ore]>=2)
                        {
                            p.Bag[(int)Resource.Wood]-=4;p.Bag[(int)Resource.Ore]-=2;
                            Tiles[bestTy*size+bestTx]=5;
                            FX(p.Pos,new V2(bestTx-size/2f+.5f,bestTy-size/2f+.5f),1);
                            Announce(EmberLanguage.English?"Wooden bridge repaired! Access restored.":"Đã sửa xong cầu vượt biển! Mở thông lối đi.");
                            Publish();return true;
                        }
                        Announce(EmberLanguage.English?"Need 4 Wood, 2 Ore to repair bridge!":"Cần 4 Gỗ, 2 Quặng để sửa cầu!");
                        return false;
                    }
                    bool hasTool=p.HasPickaxe||p.Weapon==Weapon.Pickaxe||p.Weapon==Weapon.CrystalAxe;
                    if(!hasTool)
                    {
                        string reqMsg=targetTile==3
                            ?(EmberLanguage.English?"Requires Axe or Pickaxe to chop trees!":"Không có Rìu hoặc Cuốc để chặt cây!")
                            :(EmberLanguage.English?"Requires Pickaxe to mine rocks!":"Không có Cuốc để khai thác quặng!");
                        Announce(reqMsg);
                        return false;
                    }
                    float staminaCost=35f*p.Stats.StaminaCostMultiplier;
                    if(p.Stamina<staminaCost)
                    {
                        Announce(EmberLanguage.English?"Exhausted! Not enough stamina.":"Kiệt sức! Không đủ thể lực.");
                        return false;
                    }
                    p.Stamina=Math.Max(0,p.Stamina-staminaCost);
                    if(p.HeldSlot!=8)p.PrevHeldSlot=p.HeldSlot;
                    p.HeldSlot=8;
                    V2 blockPos=new V2(bestTx-size/2f+.5f,bestTy-size/2f+.5f);
                    if(Math.Abs(blockPos.X-p.Pos.X)>.05f)p.Right=blockPos.X>p.Pos.X;
                    float tierSpeedBonus=p.PickaxeTier*0.12f;
                    p.HarvestCD=Math.Max(0.55f,(p.Stats.HarvestCooldown/(p.Hat==(int)Hat.Straw?1.05f:1f))/(1f+tierSpeedBonus));
                    FX(p.Pos,blockPos,7);
                    Resource res=targetTile==3?Resource.Wood:Resource.Ore;
                    int yield=(p.Has(Perk.Miner)?2+p.Rank(Perk.Miner):2)+(p.HeldSlot==8?p.PickaxeTier:0);
                    p.Bag[(int)res]+=yield;p.Gathered+=yield;
                    EmitPopup(blockPos,3,yield,(int)res,.95f);
                    if(p.HasPickaxe)
                    {
                        p.PickaxeDurability=Math.Max(0,p.PickaxeDurability-1);
                        if(p.PickaxeDurability<=0)
                        {
                            if(p.PickaxeTier>0)
                            {
                                p.PickaxeTier--;p.PickaxeDurability=p.PickaxeMaxDurability/2;
                                Announce((EmberLanguage.English?"Pickaxe degraded to: ":"Cuốc đào bị mòn xuống: ")+p.PickaxeName);
                            }
                            else
                            {
                                p.HasPickaxe=false;if(p.HeldSlot==8)p.HeldSlot=6;
                                Announce(EmberLanguage.English?"Pickaxe broke! Craft a new one.":"Cuốc đào đã gãy! Hãy chế tạo cuốc mới.");
                            }
                        }
                    }
                    int htIdx=harvestTargets.FindIndex(h=>h.Tx==bestTx&&h.Ty==bestTy);
                    HarvestTarget ht=htIdx>=0?harvestTargets[htIdx]:new HarvestTarget{Tx=bestTx,Ty=bestTy,MaxHP=targetTile==3?120f:160f,HP=targetTile==3?120f:160f,Tile=targetTile,LastHitTime=S.Time};
                    float dmg=30f+p.PickaxeTier*30f+(p.Has(Perk.Miner)?10f*p.Rank(Perk.Miner):0f);
                    if(p.Weapon==Weapon.CrystalAxe||p.SideWeapon==Weapon.CrystalAxe)dmg=Math.Max(dmg,60f);
                    ht.HP=Math.Max(0f,ht.HP-dmg);
                    ht.LastHitTime=S.Time;
                    if(ht.HP<=0)
                    {
                        harvestTargets.RemoveAll(h=>h.Tx==bestTx&&h.Ty==bestTy);
                        Tiles[bestTy*size+bestTx]=0;
                        treeRespawns.Add(new TreeRespawn{Tx=bestTx,Ty=bestTy,RespawnAt=S.Time+45f,Tile=targetTile});
                        FX(blockPos,blockPos,7);
                    }
                    else
                    {
                        if(htIdx>=0)harvestTargets[htIdx]=ht;
                        else harvestTargets.Add(ht);
                    }
                    Publish();
                    return true;
                }
            }
            if(node==null)return false;
            if(!dustOnly&&!node.Dropped)
            {
                bool hasTool=p.HasPickaxe||p.Weapon==Weapon.Pickaxe||p.Weapon==Weapon.CrystalAxe||p.SideWeapon==Weapon.Pickaxe||p.SideWeapon==Weapon.CrystalAxe;
                if(!hasTool)
                {
                    string reqMsg=node.Resource==Resource.Wood
                        ?(EmberLanguage.English?"Requires Axe or Pickaxe to chop trees!":"Không có Rìu hoặc Cuốc để chặt cây!")
                        :(EmberLanguage.English?"Requires Pickaxe to mine!":"Không có Cuốc để khai thác!");
                    Announce(reqMsg);
                    return false;
                }
                float staminaCost=(node.Resource==Resource.Wood?35f:30f)*p.Stats.StaminaCostMultiplier;
                if(p.Stamina<staminaCost)
                {
                    Announce(EmberLanguage.English?"Exhausted! Not enough stamina.":"Kiệt sức! Không đủ thể lực.");
                    return false;
                }
                p.Stamina=Math.Max(0,p.Stamina-staminaCost);
                if(hasTool&&p.HasPickaxe){if(p.HeldSlot!=8)p.PrevHeldSlot=p.HeldSlot;p.HeldSlot=8;}
            }
            if(node.MaxAmount<=0)node.MaxAmount=Math.Max(node.Amount,5);
            node.LastHitTime=S.Time;
            int nodeYield=(p.Has(Perk.Miner)?2+p.Rank(Perk.Miner):2)+(!dustOnly&&!node.Dropped&&p.HeldSlot==8?p.PickaxeTier:0);
            int n=Math.Min(node.Amount,nodeYield);p.Bag[(int)node.Resource]+=n;p.Gathered+=n;node.Amount-=n;if(node.Amount<=0)nodes.Remove(node);
            if(n>0)EmitPopup(node.Pos,3,n,(int)node.Resource,.95f);
            if(!dustOnly)
            {
                if(Math.Abs(node.Pos.X-p.Pos.X)>.05f)p.Right=node.Pos.X>p.Pos.X;
                float tierSpeedBonus=p.PickaxeTier*0.12f;
                p.HarvestCD=Math.Max(0.55f,(p.Stats.HarvestCooldown/(p.Hat==(int)Hat.Straw?1.05f:1f))/(1f+tierSpeedBonus));
                if(!node.Dropped)
                {
                    FX(p.Pos,node.Pos,7);
                    if(p.HasPickaxe)
                    {
                        p.PickaxeDurability=Math.Max(0,p.PickaxeDurability-1);
                        if(p.PickaxeDurability<=0)
                        {
                            if(p.PickaxeTier>0)
                            {
                                p.PickaxeTier--;p.PickaxeDurability=p.PickaxeMaxDurability/2;
                                Announce((EmberLanguage.English?"Pickaxe degraded to: ":"Cuốc đào bị mòn xuống: ")+p.PickaxeName);
                            }
                            else
                            {
                                p.HasPickaxe=false;if(p.HeldSlot==8)p.HeldSlot=6;
                                Announce(EmberLanguage.English?"Pickaxe broke! Craft a new one.":"Cuốc đào đã gãy! Hãy chế tạo cuốc mới.");
                            }
                        }
                    }
                }
            }
            return true;
        }
        public bool Craft(Player p,Recipe recipe)
        {
            int index=(int)recipe;if(index<0||index>=Catalog.Recipes.Length||p.Ghost||(p.Recipes!=null&&!p.Recipes.Contains(index)))return false;
            var cost=Catalog.Recipes[index].Cost;if(p.Pos.Length>5&&index>=7)return false;
            for(int i=0;i<5;i++)if(p.Bag[i]<cost[i])return false;
            for(int i=0;i<5;i++)p.Bag[i]-=cost[i];
            if(index<4)p.Items[index]++;
            else if(index<7){float life=index==5?90f:999999f;float hp=index==4?150f:100f;structures.Add(new Structure{Id=nextId++,Kind=index-2,Pos=p.Pos+new V2(p.Right?1.2f:-1.2f,0),Life=life,MaxLife=life,HP=hp});}
            else if(index<9){p.Lantern=index==7?Lantern.Inferno:Lantern.Relic;p.CarriesLantern=true;}
            else p.Weapon=(Weapon)(index-8);
            p.Crafted++;Announce("Đã chế tạo: "+Catalog.Recipes[index].Name);return true;
        }
        public bool CanReviveAtBeacon(Player p)
        {
            if(p==null||p.Ghost||p.Extracted||p.Pos.Length>5)return false;
            if(!players.Any(q=>q.Ghost))return false;
            if(S.Phase==Phase.Explore)return S.Charge>=ReviveChargeCost;
            return (S.Phase==Phase.Eclipse||S.Phase==Phase.Extraction)&&S.BeaconHP>ReviveBeaconHpCost;
        }
        void ReviveAtBeacon(Player p)
        {
            if(!CanReviveAtBeacon(p))return;
            var dead=players.Find(q=>q.Ghost);if(dead==null)return;
            if(S.Phase==Phase.Explore)
            {
                S.Charge=Math.Max(0,S.Charge-ReviveChargeCost);
                Announce(" được hồi sinh tại hải đăng (-100 năng lượng trụ).",dead.Name);
            }
            else
            {
                S.BeaconHP=Math.Max(1,S.BeaconHP-ReviveBeaconHpCost);
                Announce(" được hồi sinh tại hải đăng (-150 HP trụ).",dead.Name);
            }
            dead.HP=dead.MaxHP*.5f;dead.Dread=0;dead.Pos=BeaconSpawnSpot(dead.PartySlot,2.6f);dead.Invulnerable=3;
            FX(new V2(),dead.Pos,2);
        }
        void Use(Player p,int item)
        {
            if(p.Items[item]<=0)return;
            if(item==3)
            {
                if(p.Pos.Length>5)return;var dead=players.Find(q=>q.Ghost);if(dead==null)return;
                dead.HP=dead.MaxHP*.5f;dead.Dread=0;dead.Pos=p.Pos;dead.Invulnerable=3;
            }
            else if(item==2)p.Dread=Math.Max(0,p.Dread-40);
            else {float life=item==0?45f:12f;structures.Add(new Structure{Id=nextId++,Kind=item,Pos=p.Pos,Life=life,MaxLife=life});}
            p.Items[item]--;
            if(p.Items[item]<=0&&p.HeldSlot==item+2)p.HeldSlot=6;
        }
        public Enemy Target(Player p,float range)
        {
            return enemies.Where(e=>e.HP>0&&V2.Distance(e.Pos,p.Pos)<range).OrderBy(e=>V2.Distance(e.Pos,p.Pos)).FirstOrDefault();
        }
        void PushEnemy(Player p)
        {
            float staminaCost=15*p.Stats.StaminaCostMultiplier;
            if(p.PushCD>0||p.Stamina<staminaCost||p.CastTime>0)return;
            var enemy=enemies.Where(e=>e.HP>0&&e.Kind!=EnemyKind.Nest&&V2.Distance(e.Pos,p.Pos)<1.8f)
                .OrderBy(e=>V2.Distance(e.Pos,p.Pos)).FirstOrDefault();
            if(enemy==null)return;
            V2 direction=(enemy.Pos-p.Pos).Normal;
            if(direction.Length<.01f)direction=new V2(p.Right?1:-1,0);
            V2 dest=Move(enemy.Pos,direction*(enemy.Kind==EnemyKind.Boss?.8f:2.3f),monster:enemy);
            p.Stamina-=staminaCost;
            p.PushCD=.65f;
            enemy.Pos=dest;
            enemy.AttackCD=Math.Max(enemy.AttackCD,.35f);
            FX(p.Pos,dest,6);
        }

        void Knockback(Enemy enemy,V2 direction,float distance)
        {
            if(enemy==null||enemy.HP<=0||enemy.Kind==EnemyKind.Nest)return;
            V2 dir=direction.Normal;
            if(dir.Length<.01f)return;
            float scale=enemy.Kind==EnemyKind.Boss?.28f:enemy.Elite?.6f:1f;
            enemy.Pos=Move(enemy.Pos,dir*(distance*scale),monster:enemy);
            enemy.AttackCD=Math.Max(enemy.AttackCD,.12f);
        }

        void ConsumeAmmoAndShoot(Player p,V2 target,float damage)
        {
            var weapon=p.ActiveWeapon;
            int mag=Catalog.WeaponMagSize(weapon);
            if(mag>0)
            {
                if(p.ReloadTimer>0)return;
                if(p.Ammo<0||p.AmmoWeapon!=weapon){p.AmmoWeapon=weapon;p.Ammo=mag;}
                if(p.Ammo<=0){p.ReloadTimer=Catalog.WeaponReloadTime(weapon);return;}
                p.Ammo--;
                if(p.Ammo<=0)p.ReloadTimer=Catalog.WeaponReloadTime(weapon);
            }
            Shoot(p,target,damage);
        }

        bool IsNearHarvestTarget(Player p,float range)
        {
            if(nodes.Any(n=>V2.Distance(n.Pos,p.Pos)<range))return true;
            int size=S.Size;
            int px=(int)Math.Floor(p.Pos.X+size/2f);
            int py=(int)Math.Floor(p.Pos.Y+size/2f);
            for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++)
            {
                int tx=px+dx,ty=py+dy;
                if(tx<0||ty<0||tx>=size||ty>=size)continue;
                byte t=Tiles[ty*size+tx];
                if(t==3||t==2||t==7)
                {
                    V2 blockPos=new V2(tx-size/2f+.5f,ty-size/2f+.5f);
                    if(V2.Distance(p.Pos,blockPos)<range+.4f)return true;
                }
            }
            return false;
        }

        void Attack(Player p)
        {
            var weapon=p.ActiveWeapon;
            if(p.Ranged&&p.ReloadTimer>0)return;
            bool heavy=weapon==Weapon.CrystalAxe||weapon==Weapon.Pickaxe||weapon==Weapon.MirrorShield;
            float atkStaminaCost=(heavy?22f:12f)*p.Stats.StaminaCostMultiplier;
            if(!p.Ranged)
            {
                if(p.Stamina<atkStaminaCost)return;
                p.Stamina=Math.Max(0,p.Stamina-atkStaminaCost);
            }
            p.AttackCD=weapon==Weapon.ShadowBlades?.2f:weapon==Weapon.LumenGun?.32f:.5f;
            float damage=(weapon==Weapon.Stick?18:weapon==Weapon.CrystalAxe?36:25)*DamageMultiplier(p);
            var target=Target(p,p.Range);
            var ally=players.Where(q=>CanHarm(p,q)&&V2.Distance(p.Pos,q.Pos)<p.Range).OrderBy(q=>V2.Distance(q.Pos,p.Pos)).FirstOrDefault();
            if(ally!=null&&(target==null||V2.Distance(ally.Pos,p.Pos)<V2.Distance(target.Pos,p.Pos)))
            {
                if(p.Ranged){ConsumeAmmoAndShoot(p,ally.Pos,damage);return;}
                Hurt(ally,FriendlyDamage(p,damage));retaliation[((long)ally.Id<<32)|(uint)p.Id]=S.Time;FX(p.Pos,ally.Pos,0);return;
            }
            if(target==null)
            {
                if(p.Ranged){ConsumeAmmoAndShoot(p,p.Pos+new V2(p.Right?p.Range:-p.Range,0),damage);return;}
                bool isHarvest=p.HeldSlot==8||weapon==Weapon.Pickaxe||weapon==Weapon.CrystalAxe;
                FX(p.Pos,p.Pos+new V2(p.Right?1.2f:-1.2f,0),isHarvest?7:0);return;
            }
            if(p.Ranged){ConsumeAmmoAndShoot(p,target.Pos,damage);return;}
            if(weapon==Weapon.CrystalAxe||weapon==Weapon.MirrorShield)
            {
                foreach(var e in enemies.Where(e=>V2.Distance(e.Pos,target.Pos)<1.6f).ToArray())
                {
                    Damage(e,damage);
                    Knockback(e,e.Pos-p.Pos,.85f);
                }
            }
            else
            {
                Damage(target,damage);
                Knockback(target,target.Pos-p.Pos,.55f);
            }
            FX(p.Pos,target.Pos,0);
        }
        public void EmitLight(V2 pos,float radius,float duration,LightColorKind colorKind,float intensity=1.2f,bool grantsSafety=true,int followPlayerId=0)
        {
            if(radius<=0||duration<=0)return;
            transientLights.Add(new TransientLight
            {
                Id=nextId++,
                FollowPlayerId=followPlayerId,
                ColorKind=colorKind,
                Pos=pos,
                Radius=radius,
                Intensity=intensity,
                Life=duration,
                MaxLife=duration,
                GrantsSafety=grantsSafety
            });
        }
        void EmitSkillLight(Player caster,V2 targetPos,int followTargetPlayerId=0,float? durationOverride=null)
        {
            var profile=Catalog.SkillLightFor(caster.Vessel,caster.Awake);
            if(profile.Anchor==LightAnchor.None)return;
            float duration=durationOverride??profile.Duration;
            if(profile.Anchor==LightAnchor.Caster||profile.Anchor==LightAnchor.Both)
                EmitLight(caster.Pos,profile.CasterRadius,duration,profile.ColorKind,profile.Intensity,profile.GrantsSafety,caster.Id);
            if(profile.Anchor==LightAnchor.Target||profile.Anchor==LightAnchor.Both)
                EmitLight(targetPos,profile.TargetRadius,duration,profile.ColorKind,profile.Intensity,profile.GrantsSafety,followTargetPlayerId);
        }
        void UpdateTransientLights(float dt)
        {
            for(int i=transientLights.Count-1;i>=0;i--)
            {
                var l=transientLights[i];
                l.Life-=dt;
                if(l.Life<=0){transientLights.RemoveAt(i);continue;}
                if(l.FollowPlayerId>0)
                {
                    var holder=PlayerById(l.FollowPlayerId);
                    if(holder!=null&&!holder.Ghost&&!holder.Extracted)l.Pos=holder.Pos;
                    else l.FollowPlayerId=0;
                }
            }
        }
        void Skill(Player p)
        {
            if(p.Vessel==Vessel.Wanderer)
            {
                if(!p.Awake)return;
                p.CastTime=4f/9;
                p.SkillCD=22;
                p.ApplyBuff(BuffKind.Swiftness,10f,0.45f,p.Id);
                foreach(var ally in players.Where(q=>q.Id!=p.Id&&!q.Ghost&&!q.Extracted&&V2.Distance(q.Pos,p.Pos)<=p.SkillRange))
                {
                    ally.ApplyBuff(BuffKind.Swiftness,10f,0.45f,p.Id);
                    FX(p.Pos,ally.Pos,1);
                    EmitPopup(ally.Pos,2,45);
                }
                FX(p.Pos,p.Pos,1);
                EmitPopup(p.Pos,2,45);
                EmitSkillLight(p,p.Pos);
                Announce(EmberLanguage.English?"Swiftwind Aura: +45% Speed for 10s!":"Gia Tốc Gió Đêm: +45% Tốc độ chạy trong 10s!");
                return;
            }
            p.CastTime=4f/9;
            if(p.Vessel==Vessel.LittleMoone){CastMeteors(p);return;}
            if(p.Vessel==Vessel.MoonMender)
            {
                var q=players.Where(q=>!q.Ghost&&!q.Extracted&&V2.Distance(q.Pos,p.Pos)<p.SkillRange&&(q.HP<q.MaxHP||q.Dread>0)).OrderBy(q=>V2.Distance(q.Pos,p.Pos)).FirstOrDefault();
                if(q==null){p.CastTime=0;return;}
                q.HP=Math.Min(q.MaxHP,q.HP+35);q.Dread=Math.Max(0,q.Dread-40);p.SkillCD=25;FX(p.Pos,q.Pos,2);
                EmitPopup(q.Pos,2,35);
                EmitSkillLight(p,q.Pos,followTargetPlayerId:q.Id);
            }
            if(p.Vessel==Vessel.BombTrapper)
            {
                var e=Target(p,p.SkillRange);var ally=players.Where(q=>CanHarm(p,q)&&V2.Distance(p.Pos,q.Pos)<p.SkillRange).OrderBy(q=>V2.Distance(p.Pos,q.Pos)).FirstOrDefault();
                if(e==null&&ally==null){p.CastTime=0;return;}var center=ally!=null&&(e==null||V2.Distance(ally.Pos,p.Pos)<V2.Distance(e.Pos,p.Pos))?ally.Pos:e.Pos;
                float bombDamage=65*DamageMultiplier(p);
                foreach(var victim in enemies.Where(q=>V2.Distance(q.Pos,center)<3).ToArray()){Damage(victim,bombDamage);victim.Blind=3;}
                foreach(var victim in players.Where(q=>CanHarm(p,q)&&V2.Distance(q.Pos,center)<3)){Hurt(victim,FriendlyDamage(p,bombDamage));retaliation[((long)victim.Id<<32)|(uint)p.Id]=S.Time;}
                p.SkillCD=20;FX(center,center,3);
                EmitSkillLight(p,center);
            }
            if(p.Vessel==Vessel.RuneWarden)
            {
                if(Target(p,p.SkillRange)==null&&!players.Any(q=>CanHarm(p,q)&&V2.Distance(q.Pos,p.Pos)<p.SkillRange)){p.CastTime=0;return;}
                foreach(var e in enemies.Where(e=>e.Kind!=EnemyKind.Nest&&V2.Distance(e.Pos,p.Pos)<p.SkillRange)){e.Pos=Move(e.Pos,(e.Pos-p.Pos).Normal*3);e.Blind=1;}
                foreach(var ally in players.Where(q=>CanHarm(p,q)&&V2.Distance(q.Pos,p.Pos)<p.SkillRange))ally.Pos=Move(ally.Pos,(ally.Pos-p.Pos).Normal*3);
                p.Shield=40;p.ShieldTime=4;p.SkillCD=18;FX(p.Pos,p.Pos,4);
                EmitSkillLight(p,p.Pos);
            }
            if(p.Vessel==Vessel.SpaceWraith)
            {
                var q=players.Find(q=>q.Id!=p.Id&&!q.Ghost&&!q.Extracted&&V2.Distance(q.Pos,p.Pos)<p.SkillRange);
                if(S.BossArenaActive)
                {
                    if(q!=null)q.Pos=FindArenaSpot(S.BossArenaCenter,2.5f,S.BossArenaRadius-2.5f,q.PartySlot);
                    p.Pos=FindArenaSpot(S.BossArenaCenter,2.5f,S.BossArenaRadius-2.5f,p.PartySlot+3);
                }
                else
                {
                    if(q!=null)q.Pos=BeaconSpawnSpot(q.PartySlot,2.4f);
                    p.Pos=BeaconSpawnSpot(p.PartySlot-1,2.4f);
                }
                p.SkillCD=60;FX(p.Pos,p.Pos,2);
                EmitSkillLight(p,p.Pos);
            }
        }
        public static V2 EightWay(V2 direction)
        {
            if(direction.Length<.001f)return new V2(1,0);double angle=Math.Round(Math.Atan2(direction.Y,direction.X)/(Math.PI/4))*Math.PI/4;return new V2((float)Math.Cos(angle),(float)Math.Sin(angle));
        }
        void Shoot(Player owner,V2 target,float damage)=>projectiles.Add(new Projectile{Id=nextId++,Owner=owner.Id,Pos=owner.Pos,Velocity=(target-owner.Pos).Normal*16,Damage=damage,Life=owner.Range/16+.1f,Weapon=owner.ActiveWeapon});
        static float SegmentDistance(V2 point,V2 start,V2 end)
        {
            var d=end-start;float length=d.X*d.X+d.Y*d.Y;if(length<.00001f)return V2.Distance(point,start);float t=Clamp(((point.X-start.X)*d.X+(point.Y-start.Y)*d.Y)/length,0,1);return V2.Distance(point,start+d*t);
        }
        readonly List<(Player owner,V2 center,float delay)> meteors=new List<(Player,V2,float)>();
        void CastMeteors(Player p)
        {
            var nearbyEnemies=enemies.Where(e=>e.HP>0&&V2.Distance(e.Pos,p.Pos)<p.SkillRange)
                .OrderBy(e=>V2.Distance(e.Pos,p.Pos)).Take(6).ToArray();
            var enemy=nearbyEnemies.FirstOrDefault();
            var ally=players.Where(q=>CanHarm(p,q)&&V2.Distance(q.Pos,p.Pos)<p.SkillRange)
                .OrderBy(q=>V2.Distance(q.Pos,p.Pos)).FirstOrDefault();
            if(enemy==null&&ally==null){p.CastTime=0;return;}
            V2 center=ally!=null&&(enemy==null||V2.Distance(ally.Pos,p.Pos)<V2.Distance(enemy.Pos,p.Pos))?ally.Pos:enemy.Pos;
            p.CastTime=8f/6;
            p.SkillCD=24;
            EmitSkillLight(p,center);
            const int count=12;
            for(int i=0;i<count;i++)
            {
                V2 anchor=i<nearbyEnemies.Length&&i%2==1?nearbyEnemies[i].Pos:center;
                float angle=i*2.39996f;
                float radius=i==0?0:(.8f+(i%4)*.78f);
                V2 impact=anchor+new V2((float)Math.Cos(angle)*radius,(float)Math.Sin(angle)*radius);
                float delay=.45f+i*.11f;
                meteors.Add((p,impact,delay));
                effects.Add(new Effect{From=impact,To=impact,Kind=4,Life=delay});
            }
        }
        void UpdateMeteors(float dt)
        {
            for(int i=meteors.Count-1;i>=0;i--)
            {
                var meteor=meteors[i];
                meteor.delay-=dt;
                if(meteor.delay>0){meteors[i]=meteor;continue;}
                meteors.RemoveAt(i);
                float damage=18*DamageMultiplier(meteor.owner);
                foreach(var enemy in enemies.Where(q=>V2.Distance(q.Pos,meteor.center)<2.2f).ToArray())
                {
                    Damage(enemy,damage);
                    Knockback(enemy,enemy.Pos-meteor.center,.42f);
                }
                foreach(var ally in players.Where(q=>CanHarm(meteor.owner,q)&&V2.Distance(q.Pos,meteor.center)<2.2f))
                {
                    Hurt(ally,FriendlyDamage(meteor.owner,damage));
                    retaliation[((long)ally.Id<<32)|(uint)meteor.owner.Id]=S.Time;
                }
                effects.Add(new Effect{From=meteor.center,To=meteor.center,Kind=5,Life=.48f});
            }
        }
        void UpdateProjectiles(float dt)
        {
            foreach(var shot in projectiles)
            {
                var start=shot.Pos;shot.Pos+=shot.Velocity*dt;shot.Life-=dt;
                if(shot.FromEnemy)
                {
                    var victim=players.Where(p=>!p.Ghost&&!p.Extracted&&SegmentDistance(p.Pos,start,shot.Pos)<.48f).OrderBy(p=>V2.Distance(start,p.Pos)).FirstOrDefault();
                    if(victim!=null){Hurt(victim,shot.Damage);shot.Life=0;FX(start,victim.Pos,3);}
                    else if(!Walkable(shot.Pos)||(S.BossArenaActive&&V2.Distance(shot.Pos,S.BossArenaCenter)>S.BossArenaRadius+.5f))shot.Life=0;
                    continue;
                }
                var owner=PlayerById(shot.Owner);if(owner==null){shot.Life=0;continue;}
                var enemy=enemies.Where(e=>e.HP>0&&SegmentDistance(e.Pos,start,shot.Pos)<(e.Kind==EnemyKind.Boss?1.15f:.45f)).OrderBy(e=>V2.Distance(start,e.Pos)).FirstOrDefault();
                var ally=players.Where(p=>CanHarm(owner,p)&&SegmentDistance(p.Pos,start,shot.Pos)<.45f).OrderBy(p=>V2.Distance(start,p.Pos)).FirstOrDefault();
                if(ally!=null&&(enemy==null||V2.Distance(start,ally.Pos)<V2.Distance(start,enemy.Pos)))
                {Hurt(ally,FriendlyDamage(owner,shot.Damage));retaliation[((long)ally.Id<<32)|(uint)owner.Id]=S.Time;shot.Life=0;FX(start,ally.Pos,1);}
                else if(enemy!=null){Damage(enemy,shot.Damage);Knockback(enemy,shot.Velocity,.48f);shot.Life=0;FX(start,enemy.Pos,1);}
                else if(!Walkable(shot.Pos))shot.Life=0;
            }
            projectiles.RemoveAll(p=>p.Life<=0);
        }
        void EmitPopup(V2 pos,int kind,int amount,int subKind=0,float duration=.88f)
        {
            if(amount<=0)return;
            if(combatPopups.Count>=48)combatPopups.RemoveAt(0);
            float jx=(float)(rng.NextDouble()-.5)*.65f;
            float jy=.55f+(float)rng.NextDouble()*.22f;
            combatPopups.Add(new CombatPopup{Id=nextId++,Kind=kind,Amount=amount,SubKind=subKind,Pos=pos+new V2(jx,jy),Life=duration,MaxLife=duration});
        }
        void Damage(Enemy e,float damage)
        {
            if(e.HP<=0)return;
            if(damage>=1f)EmitPopup(e.Pos,0,Math.Max(1,(int)Math.Round(damage)));
            e.HP-=damage;if(e.HP<=0)
            {
                if(e.IsWorldBoss)
                {
                    var spec=Catalog.BossFor(e.BossType);
                    nodes.Add(new Node{Id=nextId++,Pos=e.Pos+new V2(-1.1f,.4f),Resource=Resource.Core,Amount=6,Dropped=true});
                    nodes.Add(new Node{Id=nextId++,Pos=e.Pos+new V2(1.1f,.4f),Resource=Resource.Umbra,Amount=8,Dropped=true});
                    nodes.Add(new Node{Id=nextId++,Pos=e.Pos+new V2(0,-1.0f),Resource=Resource.Dust,Amount=15,Dropped=true});
                    S.BossesDefeated++;
                    if(S.BossId==e.Id)EndBossArena();
                    Announce("ĐÃ HẠ GỤC BOSS: "+spec.NameVi+"! Viền phong ấn đã tan biến.");
                    return;
                }
                bool umbra=e.Elite||e.Kind==EnemyKind.Nest;nodes.Add(new Node{Id=nextId++,Pos=e.Pos,Resource=umbra?Resource.Umbra:Resource.Dust,Amount=e.Kind==EnemyKind.Nest?8:e.Elite?4:2,Dropped=true});
                if(e.Kind==EnemyKind.Boss){S.BossesDefeated++;Announce("Thực thể đã gục — nhận mảnh ký ức khi kết thúc trận.");}else if(umbra)Announce("Thu được mảnh bóng tối.");
            }
        }
        static float FriendlyDamage(Player p,float damage)=>damage*(p.Has(Perk.ReluctantHealer)?1-.5f*p.PerkStrength(Perk.ReluctantHealer):1);
        static float DamageMultiplier(Player p)=>p.Stats.PowerMultiplier*(p.Hat==(int)Hat.Jester&&p.Dread>=90?1.15f:1)*(p.HasBuff(BuffKind.AttackPower)?1f+p.BuffValue(BuffKind.AttackPower):1f);
        void Hurt(Player p,float damage)
        {
            if(DebugEnabled&&DebugInvulnerable)return;
            if(p.Invulnerable>0)return;
            damage*=p.Stats.DamageTakenMultiplier;
            if(p.HasBuff(BuffKind.IronSkin))damage*=Math.Max(0.1f,1f-p.BuffValue(BuffKind.IronSkin));
            float totalHit=damage;
            float absorbed=Math.Min(p.Shield,damage);p.Shield-=absorbed;damage-=absorbed;p.HP=Math.Max(0,p.HP-damage);p.Invulnerable=.5f;
            if(totalHit>=1f)EmitPopup(p.Pos,1,Math.Max(1,(int)Math.Round(damage>0?damage:totalHit)));
            if(p.Ghost){Announce(" đã gục. Chờ hồi sinh tại hải đăng.",p.Name);if(p.Pet==(int)Pet.LittleGhost)structures.Add(new Structure{Id=nextId++,Kind=5,Pos=p.Pos,Life=999999});}
        }
        void SpawnEnemy()
        {
            var target=players.FirstOrDefault(p=>!p.Ghost&&!p.Extracted);if(target==null)return;
            bool eclipse=S.Phase==Phase.Eclipse;
            V2 origin=eclipse?new V2():target.Pos;
            float radius=eclipse?22f:26f;
            V2 pos=new V2();bool found=false;
            for(int attempt=0;attempt<6;attempt++)
            {
                double a=rng.NextDouble()*Math.PI*2;
                V2 candidate=origin+new V2((float)Math.Cos(a),(float)Math.Sin(a))*radius;
                if(!Walkable(candidate))continue;
                if(!eclipse&&candidate.Length<26f)continue;
                pos=candidate;found=true;break;
            }
            if(!found)return;
            bool elite=S.Time>75f&&rng.NextDouble()<.08;float hp=(elite?160:42)*DifficultyEnemyScale;
            var kind=elite?EnemyKind.Sentinel:pos.Length>S.Size*.35f?EnemyKind.VoidStalker:pos.Length>S.Size*.22f?EnemyKind.SwampLeech:EnemyKind.Shade;
            float initialSkillCD=2.5f+(float)rng.NextDouble()*3.0f;
            enemies.Add(new Enemy{Id=nextId++,Pos=pos,Elite=elite,Kind=kind,HP=hp,MaxHP=hp,SkillCD=initialSkillCD});
        }
        V2 FindArenaSpot(V2 center,float minR,float maxR,int seedOffset=0)
        {
            for(int attempt=0;attempt<24;attempt++)
            {
                double a=(attempt+seedOffset)*2.39996;
                float r=minR+(maxR-minR)*((attempt%5)/4f);
                V2 candidate=center+new V2((float)Math.Cos(a)*r,(float)Math.Sin(a)*r);
                if(Walkable(candidate)&&V2.Distance(candidate,BeaconColliderPos)>BeaconColliderRadius+.5f)
                    return candidate;
            }
            return Walkable(center)?center:BeaconSpawnSpot(seedOffset,3f);
        }
        public bool TriggerWorldBossEvent(int? forcedBossType=null,int? triggerPlayerId=null)
        {
            var living=players.Where(p=>!p.Ghost&&!p.Extracted).ToList();
            if(living.Count==0)return false;
            if(S.BossArenaActive)
            {
                if(forcedBossType==null&&triggerPlayerId==null)return false;
                enemies.RemoveAll(e=>e.Id==S.BossId);
                EndBossArena();
            }
            var trigger=triggerPlayerId.HasValue?living.Find(p=>p.Id==triggerPlayerId.Value)??living[0]:living[rng.Next(living.Count)];
            int typeCount=Catalog.BossSpecs.Length;
            int pickedType=forcedBossType.HasValue?Math.Max(0,Math.Min(typeCount-1,forcedBossType.Value)):rng.Next(typeCount);
            if(!forcedBossType.HasValue&&typeCount>1&&pickedType==lastBossType)pickedType=(pickedType+1+rng.Next(typeCount-1))%typeCount;
            lastBossType=pickedType;
            var bossType=(BossType)pickedType;
            var spec=Catalog.BossFor(bossType);

            float bound=S.Size/2f-BossArenaDefaultRadius-5f;
            V2 arenaCenter=new V2(Clamp(trigger.Pos.X,-bound,bound),Clamp(trigger.Pos.Y,-bound,bound));
            if(!Walkable(arenaCenter))arenaCenter=FindArenaSpot(arenaCenter,0f,4f,0);

            V2 bossPos=FindArenaSpot(arenaCenter,6.2f,8.2f,rng.Next(8));
            V2 awayDir=(arenaCenter-bossPos).Normal;
            if(awayDir.Length<.01f)awayDir=new V2(0,-1);
            float baseAngle=(float)Math.Atan2(awayDir.Y,awayDir.X);

            for(int i=0;i<living.Count;i++)
            {
                var lp=living[i];
                V2 dest=arenaCenter+awayDir*2.8f;
                bool placed=false;
                for(int attempt=0;attempt<16;attempt++)
                {
                    float spread=(i-living.Count*.5f)*.55f+(attempt==0?0:(attempt%2==0?1:-1)*attempt*.28f);
                    float dist=2.2f+(attempt/4)*1.1f;
                    V2 cand=arenaCenter+new V2((float)Math.Cos(baseAngle+spread)*dist,(float)Math.Sin(baseAngle+spread)*dist);
                    if(Walkable(cand)&&V2.Distance(cand,BeaconColliderPos)>BeaconColliderRadius+.5f&&V2.Distance(cand,bossPos)>4.5f)
                    {dest=cand;placed=true;break;}
                }
                if(!placed)dest=FindArenaSpot(arenaCenter,1.5f,4.5f,i+3);
                lp.Pos=dest;
                lp.MirageLife=0;
                lp.Invulnerable=Math.Max(lp.Invulnerable,1.6f);
                FX(lp.Pos,lp.Pos,2);
            }

            float hp=(spec.BaseHP+Math.Max(0,living.Count-1)*280f)*DifficultyEnemyScale;
            var boss=new Enemy
            {
                Id=nextId++,Kind=EnemyKind.Boss,Elite=true,IsWorldBoss=true,BossType=bossType,
                Pos=bossPos,HP=hp,MaxHP=hp,AttackCD=1.2f,SkillCD=2.6f,SkillIndex=rng.Next(3)
            };
            enemies.Add(boss);
            S.BossArenaActive=true;
            S.BossId=boss.Id;
            S.BossType=bossType;
            S.BossArenaCenter=arenaCenter;
            S.BossArenaRadius=BossArenaDefaultRadius;
            S.BossAlertTimer=5.5f;
            S.BossAlertText=spec.NameVi+" — "+spec.SubtitleVi;
            S.BossSkillCallout="";
            S.BossSkillCalloutTimer=0;
            S.NextBossEventTime=S.Time+BossEventInterval;
            Announce("CẢNH BÁO ĐỎ: "+spec.NameVi+" xuất hiện! Toàn đội đã bị kéo vào vùng phong ấn!",trigger.Name);
            Publish();
            return true;
        }
        void EndBossArena()
        {
            int oldBossId=S.BossId;
            S.BossArenaActive=false;
            S.BossId=0;
            S.BossSkillCallout="";
            S.BossSkillCalloutTimer=0;
            S.NextBossEventTime=Math.Max(S.NextBossEventTime,S.Time+BossEventInterval);
            bossTelegraphs.RemoveAll(t=>oldBossId==0||t.OwnerId==oldBossId);
            projectiles.RemoveAll(p=>p.FromEnemy&&(oldBossId==0||p.Owner==oldBossId));
        }
        void CastBossSkill(Enemy e,Player target,BossSpec spec)
        {
            var skill=(BossSkillKind)(e.SkillIndex%3);
            e.SkillIndex++;
            e.SkillCD=spec.SkillInterval/(S.Difficulty==Difficulty.Hard?1.15f:S.Difficulty==Difficulty.Easy?.88f:1f);
            if(skill==BossSkillKind.CircleRain)
            {
                var living=players.Where(p=>!p.Ghost&&!p.Extracted).ToList();
                for(int i=0;i<spec.CircleCount;i++)
                {
                    V2 pos;
                    if(i<living.Count)pos=living[i].Pos;
                    else
                    {
                        double a=rng.NextDouble()*Math.PI*2;
                        float r=(float)(.25+rng.NextDouble()*.70)*Math.Max(3f,S.BossArenaRadius-spec.CircleRadius-.5f);
                        pos=S.BossArenaCenter+new V2((float)Math.Cos(a)*r,(float)Math.Sin(a)*r);
                    }
                    bossTelegraphs.Add(new BossTelegraph
                    {
                        Id=nextId++,OwnerId=e.Id,Kind=0,BossType=e.BossType,
                        Pos=pos,Radius=spec.CircleRadius,Timer=spec.CircleDelay,Duration=spec.CircleDelay,
                        Damage=spec.CircleDamage*DifficultyEnemyScale
                    });
                }
                e.WindupTime=.55f;
                S.BossSkillCallout=spec.RainNameVi+"|"+spec.RainNameEn;
                S.BossSkillCalloutTimer=spec.CircleDelay+.4f;
            }
            else if(skill==BossSkillKind.BulletStorm)
            {
                bossTelegraphs.Add(new BossTelegraph
                {
                    Id=nextId++,OwnerId=e.Id,Kind=2,BossType=e.BossType,
                    Pos=e.Pos,Radius=2.6f,Timer=spec.BulletDelay,Duration=spec.BulletDelay,
                    Damage=spec.BulletDamage*DifficultyEnemyScale,Wave=spec.BulletWaves
                });
                e.WindupTime=spec.BulletDelay+.45f;
                S.BossSkillCallout=spec.BulletNameVi+"|"+spec.BulletNameEn;
                S.BossSkillCalloutTimer=spec.BulletDelay+spec.BulletWaves*.5f;
            }
            else
            {
                V2 dir=(target.Pos-e.Pos).Normal;
                if(dir.Length<.01f)dir=new V2(1,0);
                bossTelegraphs.Add(new BossTelegraph
                {
                    Id=nextId++,OwnerId=e.Id,Kind=1,BossType=e.BossType,
                    Pos=e.Pos,Dir=dir,Radius=spec.ChargeWidth*.5f,Length=spec.ChargeLength,
                    Timer=spec.ChargeDelay,Duration=spec.ChargeDelay,
                    Damage=spec.ChargeDamage*DifficultyEnemyScale
                });
                e.WindupTime=spec.ChargeDelay+.15f;
                S.BossSkillCallout=spec.ChargeNameVi+"|"+spec.ChargeNameEn;
                S.BossSkillCalloutTimer=spec.ChargeDelay+.6f;
            }
        }
        void FireBossBulletWave(Enemy boss,BossSpec spec,float damage,int waveIndex)
        {
            int count=spec.BulletCount;
            double baseAngle=waveIndex*.38+S.Time*.7;
            int colorKind=(int)boss.BossType+1;
            for(int i=0;i<count;i++)
            {
                double angle=spec.BulletChaotic
                    ?baseAngle+i*(Math.PI*2/count)+(rng.NextDouble()-.5)*.45
                    :baseAngle+i*(Math.PI*2/count);
                float speed=spec.BulletSpeed*(spec.BulletChaotic?(float)(.78+rng.NextDouble()*.44):1f);
                V2 dir=new V2((float)Math.Cos(angle),(float)Math.Sin(angle));
                projectiles.Add(new Projectile
                {
                    Id=nextId++,Owner=boss.Id,FromEnemy=true,ColorKind=colorKind,
                    Pos=boss.Pos+dir*.8f,Velocity=dir*speed,Damage=damage,Life=3.2f
                });
            }
            FX(boss.Pos,boss.Pos,3);
        }
        void UpdateBossTelegraphs(float dt)
        {
            for(int i=bossTelegraphs.Count-1;i>=0;i--)
            {
                var bt=bossTelegraphs[i];
                var owner=enemies.Find(e=>e.Id==bt.OwnerId&&e.HP>0);
                if(owner==null){bossTelegraphs.RemoveAt(i);continue;}
                if(bt.Kind==2)bt.Pos=owner.Pos;
                bt.Timer-=dt;
                if(bt.Timer>0)continue;
                bossTelegraphs.RemoveAt(i);
                var spec=Catalog.BossFor(bt.BossType);
                if(bt.Kind==0)
                {
                    foreach(var p in players.Where(p=>!p.Ghost&&!p.Extracted&&V2.Distance(p.Pos,bt.Pos)<=bt.Radius))
                    {
                        Hurt(p,bt.Damage);
                        V2 push=(p.Pos-bt.Pos).Normal;
                        if(push.Length>.01f)p.Pos=Move(p.Pos,push*.9f,body:p);
                    }
                    effects.Add(new Effect{From=bt.Pos,To=bt.Pos,Kind=3,Life=.42f});
                }
                else if(bt.Kind==2)
                {
                    FireBossBulletWave(owner,spec,bt.Damage,spec.BulletWaves-bt.Wave);
                    if(bt.Wave>1)
                    {
                        bossTelegraphs.Add(new BossTelegraph
                        {
                            Id=nextId++,OwnerId=owner.Id,Kind=2,BossType=owner.BossType,
                            Pos=owner.Pos,Radius=1.8f,Timer=.48f,Duration=.48f,
                            Damage=bt.Damage,Wave=bt.Wave-1
                        });
                    }
                }
                else if(bt.Kind==1)
                {
                    owner.ChargeDir=bt.Dir;
                    owner.ChargeTime=.42f;
                    owner.ChargeSpeed=bt.Length/.42f;
                    owner.ChargeDamage=bt.Damage;
                    owner.WindupTime=0;
                    FX(owner.Pos,owner.Pos+bt.Dir*bt.Length,3);
                }
            }
        }
        void UpdateWorldBoss(Enemy e,Player target,float dt)
        {
            var spec=Catalog.BossFor(e.BossType);
            if(e.ChargeTime>0)
            {
                e.ChargeTime-=dt;
                e.Pos=Move(e.Pos,e.ChargeDir*(e.ChargeSpeed*dt),monster:e,ignoreEnemies:true);
                foreach(var p in players.Where(p=>!p.Ghost&&!p.Extracted&&V2.Distance(p.Pos,e.Pos)<1.35f))
                {
                    Hurt(p,e.ChargeDamage);
                    V2 side=new V2(-e.ChargeDir.Y,e.ChargeDir.X);
                    p.Pos=Move(p.Pos,(e.ChargeDir*.6f+side*.8f)*1.1f,body:p);
                }
                if(e.ChargeTime<=0&&e.BossType==BossType.CinderBehemoth)
                {
                    foreach(var p in players.Where(p=>!p.Ghost&&!p.Extracted&&V2.Distance(p.Pos,e.Pos)<2.6f))Hurt(p,e.ChargeDamage*.65f);
                    FX(e.Pos,e.Pos,3);
                }
                return;
            }
            if(e.WindupTime>0){e.WindupTime=Math.Max(0,e.WindupTime-dt);return;}
            e.SkillCD-=dt;
            if(e.SkillCD<=0&&e.Blind<=0){CastBossSkill(e,target,spec);return;}
            if(e.Blind<=0)
            {
                var direction=Navigate(e.Pos,target.Pos,target.Id);
                float speed=spec.MoveSpeed*DifficultyEnemySpeedScale*dt;
                e.Pos=Move(e.Pos,direction*speed,monster:e);
            }
            if(e.AttackCD<=0&&V2.Distance(target.Pos,e.Pos)<1.45f)
            {
                Hurt(target,20f*DifficultyEnemyScale);
                e.AttackCD=1.1f;
            }
        }
        void UpdateEnemy(Enemy e,float dt)
        {
            if(e.HP<=0)return;
            Player target=null;
            float distToPlayer=float.MaxValue;
            for(int i=0;i<players.Count;i++)
            {
                var p=players[i];
                if(p.Ghost||p.Extracted)continue;
                float d=V2.Distance(p.Pos,e.Pos);
                if(d<distToPlayer){distToPlayer=d;target=p;}
            }
            if(target==null)return;
            bool defendingBeacon=S.Phase==Phase.Eclipse&&S.BeaconHP>0;
            bool beaconLit=S.Fuel>0||S.BeaconHP>0;
            bool targetBeacon=defendingBeacon||(beaconLit&&distToPlayer>9f&&e.Pos.Length<45f);
            // Minecraft-style Simulation Distance & Despawn Sphere:
            // Distant normal mobs beyond DespawnRadius despawn cleanly to free CPU & mob cap;
            // Nests/Bosses never despawn, and anything outside SimActiveRadius hibernates at 0 CPU.
            if(!defendingBeacon&&!targetBeacon&&!e.IsWorldBoss&&e.Kind!=EnemyKind.Boss)
            {
                if(e.Kind!=EnemyKind.Nest&&distToPlayer>DespawnRadius){e.HP=0;return;}
                if(distToPlayer>SimActiveRadius)return;
            }
            e.AttackCD-=dt;e.Blind=Math.Max(0,e.Blind-dt);
            if(e.IsWorldBoss){UpdateWorldBoss(e,target,dt);return;}
            if(e.Kind==EnemyKind.Nest)
            {
                if(S.Time>20f&&e.AttackCD<=0&&distToPlayer<16&&enemies.Count(x=>x.Kind!=EnemyKind.Nest)<MaxActiveEnemies){var pos=e.Pos+new V2(2,0);if(Walkable(pos))enemies.Add(new Enemy{Id=nextId++,Pos=pos,Kind=EnemyKind.VoidStalker,HP=42*DifficultyEnemyScale,MaxHP=42*DifficultyEnemyScale,SkillCD=2.8f+(float)rng.NextDouble()*2f});e.AttackCD=16;}return;
            }
            if(!defendingBeacon&&!targetBeacon&&distToPlayer>18f)
            {
                if(e.SkillCD>1.2f)e.SkillCD=Math.Max(1.2f,e.SkillCD-dt);
                // Gentle patrol within simulation distance while staying away from home.
                if(e.Blind<=0)
                {
                    double wanderAngle=S.Time*.55+e.Id*1.9;
                    V2 patrolDir=e.Pos.Length<22f?e.Pos.Normal:new V2((float)Math.Cos(wanderAngle),(float)Math.Sin(wanderAngle));
                    e.Pos=Move(e.Pos,patrolDir*(.55f*DifficultyEnemySpeedScale*dt),monster:e);
                }
                return;
            }
            if(e.SkillCD>0)e.SkillCD=Math.Max(0,e.SkillCD-dt);
            V2 goal=targetBeacon?new V2(0,.35f):target.Pos;
            if(e.Blind<=0)
            {
                var direction=Navigate(e.Pos,goal,targetBeacon?-1:target.Id);
                float baseSpeed=e.Kind==EnemyKind.Boss?1.55f:e.Elite?1.65f:1.35f;
                float speed=baseSpeed*DifficultyEnemySpeedScale*dt;
                var next=Move(e.Pos,direction*speed,monster:e);
                if(V2.Distance(next,e.Pos)<speed*.2f)
                {
                    V2 left=new V2(-direction.Y,direction.X),right=new V2(direction.Y,-direction.X);
                    next=Move(e.Pos,((e.Id%2==0)?left:right)*speed,monster:e);
                }
                e.Pos=next;
            }
            float curDist=V2.Distance(target.Pos,e.Pos);
            if(e.AttackCD<=0)
            {
                if(curDist<1.1f){Hurt(target,(e.Elite?18:9)*DifficultyEnemyScale);e.AttackCD=1;}
                else if(e.Pos.Length<2.2f)
                {
                    float drain=e.Elite?4f:2f;
                    if(S.Fuel>0)
                    {
                        S.Fuel=Math.Max(0,S.Fuel-drain);
                        EmitPopup(new V2(0,.35f),1,(int)drain);
                        FX(e.Pos,new V2(0,.35f),1);
                        if(rng.NextDouble()<0.15)Announce(EmberLanguage.English?"Monsters are attacking the beacon and draining fuel!":"Quái vật đang tấn công trụ và cắn mất nhiên liệu!");
                    }
                    else if(S.Charge>0)
                    {
                        float cDrain=Math.Min(S.Charge,drain*2.5f);
                        S.Charge-=cDrain;
                        EmitPopup(new V2(0,.35f),1,(int)cDrain);
                        FX(e.Pos,new V2(0,.35f),1);
                        if(rng.NextDouble()<0.15)Announce(EmberLanguage.English?"Monsters are siphoning energy from the beacon!":"Quái vật đang cắn mất năng lượng nạp của trụ!");
                    }
                    if(S.BeaconHP>0)S.BeaconHP=Math.Max(0,S.BeaconHP-(e.Elite?15:6));
                    e.AttackCD=1.2f;
                }
                else
                {
                    var wall=structures.Find(s=>s.Kind==2&&s.HP>0&&V2.Distance(s.Pos,e.Pos)<1.5f);
                    if(wall!=null)
                    {
                        float dmg=(e.Elite?18:8)*DifficultyEnemyScale;
                        wall.HP-=dmg;
                        e.AttackCD=1f;
                        EmitPopup(wall.Pos,1,(int)dmg);
                        FX(e.Pos,wall.Pos,1);
                        if(wall.HP<=0)FX(wall.Pos,wall.Pos,7);
                    }
                }
            }
            if(e.SkillCD<=0&&e.Blind<=0&&!S.BossArenaActive)TryFireEnemyShot(e,target,curDist);
        }
        bool ShotBlocked(V2 p)
        {
            int x=(int)Math.Floor(p.X+S.Size/2f),y=(int)Math.Floor(p.Y+S.Size/2f);
            if(x<3||y<3||x>=S.Size-3||y>=S.Size-3)return true;
            byte tile=Tiles[y*S.Size+x];
            if(tile==3||tile==4)return true;
            return structures.Any(s=>s.Kind==2&&s.HP>0&&V2.Distance(s.Pos,p)<.8f);
        }
        bool HasClearShot(V2 from,V2 to)
        {
            float dist=V2.Distance(from,to);
            if(dist<=.75f)return true;
            int steps=Math.Max(2,(int)Math.Ceiling(dist/.6f));
            for(int i=1;i<steps;i++)
            {
                float t=i/(float)steps;
                if(ShotBlocked(new V2(from.X+(to.X-from.X)*t,from.Y+(to.Y-from.Y)*t)))return false;
            }
            return true;
        }
        void TryFireEnemyShot(Enemy e,Player target,float distToPlayer)
        {
            var spec=Catalog.EnemyShotFor(e.Kind,e.Elite);
            if(!spec.Enabled||distToPlayer<spec.MinRange||distToPlayer>spec.MaxRange)return;
            int activeEnemyShots=0;
            for(int i=0;i<projectiles.Count;i++)if(projectiles[i].FromEnemy)activeEnemyShots++;
            if(activeEnemyShots>=32)return;
            if(!HasClearShot(e.Pos,target.Pos))return;
            V2 aim=(target.Pos-e.Pos).Normal;
            if(aim.Length<.001f)return;
            float baseAngle=(float)Math.Atan2(aim.Y,aim.X);
            float spreadRad=spec.SpreadDeg*(float)(Math.PI/180.0);
            float life=(spec.MaxRange+1.5f)/spec.Speed;
            float dmg=spec.Damage*DifficultyEnemyScale;
            int count=Math.Max(1,spec.Count);
            for(int i=0;i<count;i++)
            {
                float offset=count==1?0f:(i-(count-1)*.5f)*spreadRad;
                float a=baseAngle+offset;
                V2 dir=new V2((float)Math.Cos(a),(float)Math.Sin(a));
                projectiles.Add(new Projectile{Id=nextId++,Owner=e.Id,Pos=e.Pos+dir*.45f,Velocity=dir*spec.Speed,Damage=dmg,Life=life,FromEnemy=true,ColorKind=spec.ColorKind});
            }
            FX(e.Pos,e.Pos,6);
            float diffRate=S.Difficulty==Difficulty.Hard?1.12f:S.Difficulty==Difficulty.Easy?.86f:1f;
            float jitter=.92f+(float)rng.NextDouble()*.18f;
            e.SkillCD=(spec.Cooldown/diffRate)*jitter;
        }
        void Announce(string text,string actor="",string target=""){S.Event=text;S.EventActor=actor;S.EventTarget=target;S.EventTick=S.Tick;}
        void FX(V2 from,V2 to,int kind)=>effects.Add(new Effect{From=from,To=to,Kind=kind,Life=.3f});
        static float Clamp(float v,float min,float max)=>Math.Max(min,Math.Min(max,v));
        void Publish(){S.Players=players.ToArray();S.Enemies=enemies.ToArray();S.Nodes=nodes.ToArray();S.Structures=structures.ToArray();S.Effects=effects.ToArray();S.Projectiles=projectiles.ToArray();S.BossTelegraphs=bossTelegraphs.ToArray();S.Lights=transientLights.ToArray();S.Popups=combatPopups.ToArray();S.TreeRespawns=treeRespawns.ToArray();S.HarvestTargets=harvestTargets.ToArray();S.Disconnected=disconnectedSeats.Values.OrderBy(d=>d.PartySlot).ToArray();}
    }
}
