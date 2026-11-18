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
    public enum Lantern { Oil, Inferno, Relic }
    public enum Perk { SteelNerves, StrongBack, GhostStep, Prepared, Miner, LastLight, ReluctantHealer }
    public enum Hat { Straw, Witch, Candle, Jester }
    public enum Pet { Firefly, Bat, CrystalCat, LittleGhost }
    public enum Recipe { Torch, Flare, Sedative, RevivalStone, Wall, Trap, Chest, InfernoLantern, RelicLantern, CrystalAxe, ShadowBlades, Crossbow, LumenGun, MirrorShield }

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
        public V2 DashDir; public Weapon Weapon; public Weapon SideWeapon=Weapon.LumenGun; public Weapon AmmoWeapon=Weapon.LumenGun; public int Ammo=-1; public int HeldSlot=6; public Lantern Lantern;
        public V2 MiragePos;public float MirageLife,MirageCD=12;
        public int[] Bag=new int[(int)Resource.Count],Items=new int[4];
        public int[] Perks=Array.Empty<int>(); public int Hat=-1,Pet=-1;
        public int[] PerkRanks=Array.Empty<int>(),Recipes;public int Gathered,Crafted;
        public int Rank(Perk perk)=>Has(perk)?Math.Max(1,Math.Min(3,PerkRanks!=null&&PerkRanks.Length>(int)perk?PerkRanks[(int)perk]:1)):0;
        public float PerkStrength(Perk perk)=>1+.2f*Math.Max(0,Rank(perk)-1);
        public bool Ghost=>HP<=0; public bool Has(Perk perk)=>Array.IndexOf(Perks,(int)perk)>=0;
        public VesselStats Stats=>Catalog.StatsFor(Vessel);
        public float BaseCapacity=>30f+Stats.Gathering*4.5f;
        public int Load=>Bag.Sum(); public float Capacity=>BaseCapacity*(Has(Perk.StrongBack)?1+.3f*PerkStrength(Perk.StrongBack):1);
        public bool HasHeldTorch=>HeldSlot==2&&Items[0]>0&&LampOn&&!Ghost&&!Extracted;
        public bool HoldingWeapon=>HeldSlot==1||HeldSlot==7||HeldSlot==8;
        public Weapon ActiveWeapon=>HeldSlot==7?SideWeapon:HeldSlot==8?Weapon.Pickaxe:Weapon;
        public bool Ranged=>ActiveWeapon==Weapon.Crossbow||ActiveWeapon==Weapon.LumenGun||ActiveWeapon==Weapon.MirrorShield;
        public float Range=>Ranged?7:ActiveWeapon==Weapon.CrystalAxe?2.6f:ActiveWeapon==Weapon.Pickaxe?2.4f:2;
        public int MaxAmmo=>Catalog.WeaponMagSize(ActiveWeapon);
        public int CurrentAmmo=>MaxAmmo<=0?0:(Ammo<0||AmmoWeapon!=ActiveWeapon?MaxAmmo:Math.Min(Ammo,MaxAmmo));
        public bool Reloading=>ReloadTimer>0;
        public float SkillRange=>Catalog.SkillRange(Vessel);
        public bool Mad=>Dread>=90&&!Safe;
    }
    [Serializable] public sealed class Enemy
    {
        public int Id; public V2 Pos; public float HP=42,MaxHP=42,AttackCD,Blind; public bool Elite;public EnemyKind Kind;
    }
    [Serializable] public sealed class Node
    {
        public int Id; public V2 Pos; public Resource Resource; public int Amount; public bool Dropped;
    }
    [Serializable] public sealed class Structure
    {
        public int Id,Kind; public V2 Pos; public float Life,HP=100; // 0 torch 1 flare 2 wall 3 trap 4 chest
    }
    [Serializable] public sealed class Effect
    {
        public V2 From,To; public int Kind; public float Life;
    }
    [Serializable] public sealed class Projectile
    {
        public int Id,Owner;public V2 Pos,Velocity;public float Damage,Life=1;public Weapon Weapon;
    }
    [Serializable] public sealed class Snapshot
    {
        public int Seed,Size,Tick,Capacity=4,BossesDefeated,TeamExtracted; public string RoomName="Ember";public Phase Phase; public Difficulty Difficulty; public float Time,Fuel,BeaconHP,Charge,ChargeTarget=100,Wave,Blackout,LastLight;
        public Player[] Players; public Enemy[] Enemies; public Node[] Nodes; public Structure[] Structures; public Effect[] Effects;public Projectile[] Projectiles=Array.Empty<Projectile>();
        public string Event,EventActor,EventTarget;public int EventTick;
        public bool Sandbox;
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
    public static class Catalog
    {
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
        public static float SkillRange(Vessel vessel)=>vessel==Vessel.LittleMoone?18:vessel==Vessel.BombTrapper?10:vessel==Vessel.RuneWarden?5:vessel==Vessel.SpaceWraith?2:vessel==Vessel.MoonMender?8:0;
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
        static readonly V2 BeaconColliderPos=new V2(0,.35f);
        const float BeaconColliderRadius=1.35f;
        public Snapshot S=new Snapshot();
        public byte[] Tiles; public readonly int[] Chest=new int[5];
        readonly List<Player> players=new List<Player>(); readonly List<Enemy> enemies=new List<Enemy>();
        readonly List<Node> nodes=new List<Node>(); readonly List<Structure> structures=new List<Structure>(); readonly List<Effect> effects=new List<Effect>();
        readonly List<Projectile> projectiles=new List<Projectile>();
        readonly Dictionary<int,Command> commands=new Dictionary<int,Command>();
        readonly Dictionary<int,Queue<Command>> actions=new Dictionary<int,Queue<Command>>();
        readonly Dictionary<int,float> inputAt=new Dictionary<int,float>();
        readonly Dictionary<long,float> retaliation=new Dictionary<long,float>();
        readonly Dictionary<int,FlowField> navigation=new Dictionary<int,FlowField>();
        sealed class FlowField{public int[] Distance;public int Goal=-1;public float Built=-100;}
        Random rng;int nextId=10;float spawnClock,petClock;bool lastLightSpent,bossSpawned;
        float DifficultyEnemyScale=>S.Difficulty==Difficulty.Hard?1.25f:S.Difficulty==Difficulty.Easy?.85f:1f;
        float DifficultyDreadScale=>S.Difficulty==Difficulty.Hard?1.25f:S.Difficulty==Difficulty.Normal?1.05f:1f;
        float DifficultyEnemySpeedScale=>S.Difficulty==Difficulty.Hard?1.12f:S.Difficulty==Difficulty.Easy?.9f:1f;
        float ExploreSpawnInterval=>S.Difficulty==Difficulty.Hard?5.5f:S.Difficulty==Difficulty.Easy?9.5f:7.5f;
        float EclipseSpawnInterval=>S.Difficulty==Difficulty.Hard?1.2f:S.Difficulty==Difficulty.Easy?2.2f:1.7f;
        int MaxActiveEnemies=>S.Phase==Phase.Eclipse
            ?(S.Difficulty==Difficulty.Hard?75:S.Difficulty==Difficulty.Easy?40:55)
            :(S.Difficulty==Difficulty.Hard?30:S.Difficulty==Difficulty.Easy?16:22);
        public EmberWorld(int seed,int size=250,Difficulty difficulty=Difficulty.Easy)
        {
            S.Seed=seed;S.Size=size;S.Phase=Phase.Sanctuary;S.Difficulty=difficulty;S.ChargeTarget=Catalog.BeaconChargeTarget(difficulty);S.Fuel=100;S.BeaconHP=0;S.Wave=90;rng=new Random(seed);
            Tiles=MakeTiles(seed,size);GenerateNodes();Publish();
        }
        public static byte[] MakeTiles(int seed,int size)
        {
            var random=new Random(seed);var tiles=new byte[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=x-size/2,dy=y-size/2,r=(float)Math.Sqrt(dx*dx+dy*dy);
                bool starter=dx>=4&&dx<=17&&dy>=2&&dy<=12;
                tiles[y*size+x]=(byte)(x<3||y<3||x>=size-3||y>=size-3?4:r<7||starter?0:random.NextDouble()<.12?3:r<size*.22?0:r<size*.37?1:2);
            }
            // Two cellular smoothing passes produce forest clusters; central lanes remain open.
            for(int pass=0;pass<2;pass++)
            {
                var copy=(byte[])tiles.Clone();
                for(int y=4;y<size-4;y++)for(int x=4;x<size-4;x++)
                {
                    if(Math.Abs(x-size/2)<3||Math.Abs(y-size/2)<3||(x-size/2>=4&&x-size/2<=17&&y-size/2>=2&&y-size/2<=12))continue;
                    int count=0;for(int j=-1;j<=1;j++)for(int i=-1;i<=1;i++)if(copy[(y+j)*size+x+i]==3)count++;
                    if(count>=4)tiles[y*size+x]=3;
                }
            }
            return tiles;
        }
        void GenerateNodes()
        {
            for(int i=0;i<1400;i++)
            {
                var pos=new V2(rng.Next(6,S.Size-6)-S.Size/2,rng.Next(6,S.Size-6)-S.Size/2);
                if(!Walkable(pos)||pos.Length<5)continue;
                float radius=pos.Length;Resource resource=new[]{Resource.Dust,Resource.Core,Resource.Wood,Resource.Ore}[rng.Next(4)];
                if(radius<15)resource=i%3==0?Resource.Core:Resource.Dust;
                nodes.Add(new Node{Id=nextId++,Pos=pos,Resource=resource,Amount=resource==Resource.Core?(radius>S.Size*.35f?5:2):4});
            }
            // Guaranteed accessible starter resources, regardless of seed.
            for(int i=0;i<12;i++)nodes.Add(new Node{Id=nextId++,Pos=new V2(6+i%4*3,4+i/4*3),Resource=i%3==0?Resource.Core:Resource.Dust,Amount=5});
            for(int i=0;i<10;i++)
            {
                double a=i*Math.PI*.2;V2 pos=new V2((float)Math.Cos(a),(float)Math.Sin(a))*(30+i*3);
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

        public Player AddPlayer(int id,string name,Vessel vessel,int[] perks=null,int hat=-1,int pet=-1,int[] ranks=null,int[] recipes=null,string installationId=null)
        {
            var existing=players.Find(p=>p.Id==id);if(existing!=null)return existing;
            if(players.Count>=S.Capacity)throw new InvalidOperationException("Room is full");
            string identity=Guid.TryParseExact(installationId,"N",out var guid)&&guid!=Guid.Empty?guid.ToString("N"):null;
            if(identity!=null&&players.Any(player=>player.InstallationId==identity))
                throw new InvalidOperationException("Player identity is already in the room");
            var displayName=new string((name??"").Where(c=>!char.IsControl(c)).Take(20).ToArray()).Trim();
            if(displayName.Length==0)displayName="Traveler";
            int partySlot=1;
            while(players.Any(member=>member.PartySlot==partySlot))partySlot++;
            var p=new Player{Id=id,PartySlot=partySlot,InstallationId=identity,Name=displayName,Vessel=vessel,Perks=perks??Array.Empty<int>(),Hat=hat,Pet=pet,PerkRanks=ranks??Array.Empty<int>(),Recipes=recipes,Pos=BeaconSpawnSpot(partySlot-1,2.3f)};
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
            var removed=players.Find(p=>p.Id==id);if(removed!=null&&removed.CarriesLantern){var other=players.Find(p=>p.Id!=id&&!p.Ghost);if(other!=null){other.CarriesLantern=true;other.Lantern=removed.Lantern;}}
            players.RemoveAll(p=>p.Id==id);commands.Remove(id);actions.Remove(id);inputAt.Remove(id);navigation.Remove(id);Publish();
        }
        public void SetReady(int id,bool ready){if(S.Phase==Phase.Sanctuary){var p=PlayerById(id);if(p!=null)p.Ready=ready;}}
        public bool StartRun(bool requireReady=false)
        {
            if(players.Count==0||S.Phase!=Phase.Sanctuary||(requireReady&&players.Any(p=>!p.Ready)))return false;
            spawnClock=18f;
            S.Phase=Phase.Explore;Announce("Thu thập lõi, nạp hải đăng, giữ lửa.");return true;
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
            if(Tiles[y*S.Size+x]==3||Tiles[y*S.Size+x]==4)return false;
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
            if(S.BeaconHP>0&&S.Charge>=S.ChargeTarget&&pos.Length<6)return true;
            foreach(var p in players)
            {
                if(p.Ghost||p.Extracted)continue;
                bool lit=p.HasHeldTorch&&(S.Fuel>0||S.LastLight>0);
                if(lit&&V2.Distance(pos,p.Pos)<(p.Lantern==Lantern.Relic?8:5))return true;
            }
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
                case "enemy":enemies.Add(new Enemy{Id=nextId++,Kind=EnemyKind.Shade,Pos=p.Pos+new V2(3,0),HP=42,MaxHP=42});break;
                case "boss":enemies.Add(new Enemy{Id=nextId++,Kind=EnemyKind.Boss,Elite=true,Pos=p.Pos+new V2(5,0),HP=600,MaxHP=600});break;
                default:return false;
            }
            Announce("DEBUG "+action);Publish();return true;
        }
        public void Tick(float dt=Step)
        {
            if(dt<=0||dt>.2f)throw new ArgumentOutOfRangeException(nameof(dt));
            if(S.Phase==Phase.Sanctuary||S.Phase==Phase.Won||S.Phase==Phase.Lost){Publish();return;}
            S.Tick++;S.Time+=dt;float oldFuel=S.Fuel;if(players.Any(p=>p.HasHeldTorch))S.Fuel=Math.Max(0,S.Fuel-dt*.13f);S.LastLight=Math.Max(0,S.LastLight-dt);
            if(DebugEnabled&&DebugInfiniteFuel)S.Fuel=100;
            if(oldFuel>0&&S.Fuel<=0&&!lastLightSpent&&players.Any(p=>p.Has(Perk.LastLight))){S.LastLight=5*players.Where(p=>p.Has(Perk.LastLight)).Max(p=>p.PerkStrength(Perk.LastLight));lastLightSpent=true;Announce("Tia sáng cuối — "+S.LastLight.ToString("0")+" giây!");}
            foreach(var p in players)UpdatePlayer(p,dt);
            if(!DebugEnabled||!DebugNoEnemies)foreach(var e in enemies.ToArray())UpdateEnemy(e,dt);
            UpdateProjectiles(dt);
            UpdateMeteors(dt);
            enemies.RemoveAll(e=>e.HP<=0);
            foreach(var s in structures)
            {
                if(s.Kind<=1)s.Life-=dt;
                if(s.Kind==3)foreach(var e in enemies)if(V2.Distance(e.Pos,s.Pos)<2){Damage(e,dt*15);e.Blind=Math.Max(e.Blind,.3f);s.Life-=dt;}
            }
            structures.RemoveAll(s=>s.HP<=0||s.Life<=0);
            foreach(var e in effects)e.Life-=dt;effects.RemoveAll(e=>e.Life<=0);
            spawnClock-=dt;if((!DebugEnabled||!DebugNoEnemies)&&spawnClock<=0&&enemies.Count(e=>e.Kind!=EnemyKind.Nest)<MaxActiveEnemies){SpawnEnemy();spawnClock=S.Phase==Phase.Eclipse?EclipseSpawnInterval:ExploreSpawnInterval;}
            if((!DebugEnabled||!DebugNoEnemies)&&S.Phase==Phase.Eclipse&&S.Wave<60&&!bossSpawned)
            {
                bossSpawned=true;var pos=new V2(0,18);float hp=450+players.Count*180;enemies.Add(new Enemy{Id=nextId++,Kind=EnemyKind.Boss,Elite=true,Pos=pos,HP=hp,MaxHP=hp});Announce("Một thực thể lớn đang tiến về hải đăng.");
            }
            petClock+=dt;if(petClock>=2){petClock=0;foreach(var p in players.Where(p=>!p.Ghost&&p.Pet==(int)Pet.Firefly))Harvest(p,4,true);}
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
            p.Stamina=Clamp(p.Stamina+dt*(p.Safe?24:12)*stats.StaminaRegenMultiplier*(p.Hat==(int)Hat.Witch?1.08f:1),0,100);
            if(!commands.TryGetValue(p.Id,out var movement))movement=new Command();
            if(inputAt.TryGetValue(p.Id,out var received)&&S.Time-received>.5f){movement.X=movement.Y=0;}
            Command c=new Command{X=movement.X,Y=movement.Y};
            if(actions.TryGetValue(p.Id,out var queue)&&queue.Count>0){c=queue.Dequeue();c.X=movement.X;c.Y=movement.Y;}
            p.Move=new V2(c.X,c.Y);if(p.Move.Length>1)p.Move=p.Move.Normal;
            if(Math.Abs(p.Move.X)>.01f)p.Right=p.Move.X>0;
            float speed=stats.MoveSpeed;
            if(p.Load>p.Capacity)speed*=.55f;
            if(p.Ghost){p.Move=new V2();return;}
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
            if(c.Refuel&&p.Bag[0]>0){int amount=Math.Min(p.Bag[0],(int)Math.Ceiling((100-S.Fuel)/2));p.Bag[0]-=amount;S.Fuel=Math.Min(100,S.Fuel+amount*2);}
            if(c.Deposit&&p.Pos.Length<4&&S.Phase==Phase.Explore){int amount=Math.Min(p.Bag[1],(int)Math.Ceiling((S.ChargeTarget-S.Charge)/CoreChargeValue));p.Bag[1]-=amount;S.Charge=Math.Min(S.ChargeTarget,S.Charge+amount*CoreChargeValue);if(S.Charge>=S.ChargeTarget){S.BeaconHP=600;S.Phase=Phase.Eclipse;S.Wave=90;Announce("NHẬT THỰC — bảo vệ hải đăng 90 giây!");}}
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
                if(p.HeldSlot==8&&Harvest(p,2.4f))p.AttackCD=p.HarvestCD;
                else Attack(p);
            }
            if(c.Skill&&p.SkillCD<=0&&p.CastTime<=0)Skill(p);
            if(p.HasHeldTorch&&p.Lantern==Lantern.Inferno&&S.Fuel>0)foreach(var e in enemies)if(V2.Distance(e.Pos,p.Pos)<5)Damage(e,dt*8*DamageMultiplier(p));
            if(S.Fuel>0)foreach(var carrier in players.Where(q=>q.HasHeldTorch&&q.Lantern==Lantern.Relic))
            {
                if(V2.Distance(carrier.Pos,p.Pos)>=8)continue;p.HP=Math.Min(p.MaxHP,p.HP+dt*3);p.Dread=Math.Max(0,p.Dread-dt*3);p.Shield=Math.Min(20,p.Shield+dt*2);p.ShieldTime=Math.Max(p.ShieldTime,.2f);
            }
        }
        bool Harvest(Player p,float range,bool dustOnly=false)
        {
            if(!dustOnly&&p.HarvestCD>0)return false;
            var node=nodes.Where(n=>(!dustOnly||n.Resource==Resource.Dust)&&V2.Distance(n.Pos,p.Pos)<range).OrderBy(n=>V2.Distance(n.Pos,p.Pos)).FirstOrDefault();
            if(node==null)return false;
            int yield=(p.Has(Perk.Miner)?2+p.Rank(Perk.Miner):2)+(p.HeldSlot==8&&!dustOnly?1:0);
            int n=Math.Min(node.Amount,yield);p.Bag[(int)node.Resource]+=n;p.Gathered+=n;node.Amount-=n;if(node.Amount<=0)nodes.Remove(node);
            if(!dustOnly)
            {
                if(Math.Abs(node.Pos.X-p.Pos.X)>.05f)p.Right=node.Pos.X>p.Pos.X;
                p.HarvestCD=p.Stats.HarvestCooldown/(p.Hat==(int)Hat.Straw?1.05f:1f);
                FX(p.Pos,node.Pos,7);
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
            else if(index<7)structures.Add(new Structure{Id=nextId++,Kind=index-2,Pos=p.Pos+new V2(p.Right?1.2f:-1.2f,0),Life=index==5?90:999999});
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
            else structures.Add(new Structure{Id=nextId++,Kind=item,Pos=p.Pos,Life=item==0?45:12});
            p.Items[item]--;
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

        void Attack(Player p)
        {
            var weapon=p.ActiveWeapon;
            if(p.Ranged&&p.ReloadTimer>0)return;
            bool heavy=weapon==Weapon.CrystalAxe||weapon==Weapon.Pickaxe||weapon==Weapon.MirrorShield;float heavyCost=8*p.Stats.StaminaCostMultiplier;if(heavy&&p.Stamina<heavyCost)return;if(heavy)p.Stamina-=heavyCost;
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
                FX(p.Pos,p.Pos+new V2(p.Right?1.5f:-1.5f,0),0);return;
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
        void Skill(Player p)
        {
            if(p.Vessel==Vessel.Wanderer)return;
            p.CastTime=4f/9;
            if(p.Vessel==Vessel.LittleMoone){CastMeteors(p);return;}
            if(p.Vessel==Vessel.MoonMender)
            {
                var q=players.Where(q=>!q.Ghost&&!q.Extracted&&V2.Distance(q.Pos,p.Pos)<p.SkillRange&&(q.HP<q.MaxHP||q.Dread>0)).OrderBy(q=>V2.Distance(q.Pos,p.Pos)).FirstOrDefault();
                if(q==null){p.CastTime=0;return;}
                q.HP=Math.Min(q.MaxHP,q.HP+35);q.Dread=Math.Max(0,q.Dread-40);p.SkillCD=25;FX(p.Pos,q.Pos,2);
            }
            if(p.Vessel==Vessel.BombTrapper)
            {
                var e=Target(p,p.SkillRange);var ally=players.Where(q=>CanHarm(p,q)&&V2.Distance(p.Pos,q.Pos)<p.SkillRange).OrderBy(q=>V2.Distance(p.Pos,q.Pos)).FirstOrDefault();
                if(e==null&&ally==null){p.CastTime=0;return;}var center=ally!=null&&(e==null||V2.Distance(ally.Pos,p.Pos)<V2.Distance(e.Pos,p.Pos))?ally.Pos:e.Pos;
                float bombDamage=65*DamageMultiplier(p);
                foreach(var victim in enemies.Where(q=>V2.Distance(q.Pos,center)<3).ToArray()){Damage(victim,bombDamage);victim.Blind=3;}
                foreach(var victim in players.Where(q=>CanHarm(p,q)&&V2.Distance(q.Pos,center)<3)){Hurt(victim,FriendlyDamage(p,bombDamage));retaliation[((long)victim.Id<<32)|(uint)p.Id]=S.Time;}
                p.SkillCD=20;FX(center,center,3);
            }
            if(p.Vessel==Vessel.RuneWarden)
            {
                if(Target(p,p.SkillRange)==null&&!players.Any(q=>CanHarm(p,q)&&V2.Distance(q.Pos,p.Pos)<p.SkillRange)){p.CastTime=0;return;}
                foreach(var e in enemies.Where(e=>e.Kind!=EnemyKind.Nest&&V2.Distance(e.Pos,p.Pos)<p.SkillRange)){e.Pos=Move(e.Pos,(e.Pos-p.Pos).Normal*3);e.Blind=1;}
                foreach(var ally in players.Where(q=>CanHarm(p,q)&&V2.Distance(q.Pos,p.Pos)<p.SkillRange))ally.Pos=Move(ally.Pos,(ally.Pos-p.Pos).Normal*3);
                p.Shield=40;p.ShieldTime=4;p.SkillCD=18;FX(p.Pos,p.Pos,4);
            }
            if(p.Vessel==Vessel.SpaceWraith)
            {
                var q=players.Find(q=>q.Id!=p.Id&&!q.Ghost&&!q.Extracted&&V2.Distance(q.Pos,p.Pos)<p.SkillRange);if(q!=null)q.Pos=BeaconSpawnSpot(q.PartySlot,2.4f);p.Pos=BeaconSpawnSpot(p.PartySlot-1,2.4f);p.SkillCD=60;FX(p.Pos,p.Pos,2);
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
                var start=shot.Pos;shot.Pos+=shot.Velocity*dt;shot.Life-=dt;var owner=PlayerById(shot.Owner);if(owner==null){shot.Life=0;continue;}
                var enemy=enemies.Where(e=>e.HP>0&&SegmentDistance(e.Pos,start,shot.Pos)<(e.Kind==EnemyKind.Boss?1:.45f)).OrderBy(e=>V2.Distance(start,e.Pos)).FirstOrDefault();
                var ally=players.Where(p=>CanHarm(owner,p)&&SegmentDistance(p.Pos,start,shot.Pos)<.45f).OrderBy(p=>V2.Distance(start,p.Pos)).FirstOrDefault();
                if(ally!=null&&(enemy==null||V2.Distance(start,ally.Pos)<V2.Distance(start,enemy.Pos)))
                {Hurt(ally,FriendlyDamage(owner,shot.Damage));retaliation[((long)ally.Id<<32)|(uint)owner.Id]=S.Time;shot.Life=0;FX(start,ally.Pos,1);}
                else if(enemy!=null){Damage(enemy,shot.Damage);Knockback(enemy,shot.Velocity,.48f);shot.Life=0;FX(start,enemy.Pos,1);}
                else if(!Walkable(shot.Pos))shot.Life=0;
            }
            projectiles.RemoveAll(p=>p.Life<=0);
        }
        void Damage(Enemy e,float damage)
        {
            if(e.HP<=0)return;e.HP-=damage;if(e.HP<=0)
            {
                bool umbra=e.Elite||e.Kind==EnemyKind.Nest;nodes.Add(new Node{Id=nextId++,Pos=e.Pos,Resource=umbra?Resource.Umbra:Resource.Dust,Amount=e.Kind==EnemyKind.Nest?8:e.Elite?4:2,Dropped=true});
                if(e.Kind==EnemyKind.Boss){S.BossesDefeated++;Announce("Thực thể đã gục — nhận mảnh ký ức khi kết thúc trận.");}else if(umbra)Announce("Thu được mảnh bóng tối.");
            }
        }
        static float FriendlyDamage(Player p,float damage)=>damage*(p.Has(Perk.ReluctantHealer)?1-.5f*p.PerkStrength(Perk.ReluctantHealer):1);
        static float DamageMultiplier(Player p)=>p.Stats.PowerMultiplier*(p.Hat==(int)Hat.Jester&&p.Dread>=90?1.15f:1);
        void Hurt(Player p,float damage)
        {
            if(DebugEnabled&&DebugInvulnerable)return;
            if(p.Invulnerable>0)return;
            damage*=p.Stats.DamageTakenMultiplier;
            float absorbed=Math.Min(p.Shield,damage);p.Shield-=absorbed;damage-=absorbed;p.HP=Math.Max(0,p.HP-damage);p.Invulnerable=.5f;
            if(p.Ghost){Announce(" đã gục. Chờ hồi sinh tại hải đăng.",p.Name);if(p.Pet==(int)Pet.LittleGhost)structures.Add(new Structure{Id=nextId++,Kind=5,Pos=p.Pos,Life=999999});}
        }
        void SpawnEnemy()
        {
            var target=players.FirstOrDefault(p=>!p.Ghost&&!p.Extracted);if(target==null)return;
            double a=rng.NextDouble()*Math.PI*2;V2 origin=S.Phase==Phase.Eclipse?new V2():target.Pos;V2 pos=origin+new V2((float)Math.Cos(a),(float)Math.Sin(a))*18;
            if(!Walkable(pos))return;bool elite=S.Time>75f&&rng.NextDouble()<.08;float hp=(elite?160:42)*DifficultyEnemyScale;
            var kind=elite?EnemyKind.Sentinel:pos.Length>S.Size*.35f?EnemyKind.VoidStalker:pos.Length>S.Size*.22f?EnemyKind.SwampLeech:EnemyKind.Shade;
            enemies.Add(new Enemy{Id=nextId++,Pos=pos,Elite=elite,Kind=kind,HP=hp,MaxHP=hp});
        }
        void UpdateEnemy(Enemy e,float dt)
        {
            if(e.HP<=0)return;e.AttackCD-=dt;e.Blind=Math.Max(0,e.Blind-dt);
            var target=players.Where(p=>!p.Ghost&&!p.Extracted).OrderBy(p=>V2.Distance(p.Pos,e.Pos)).FirstOrDefault();if(target==null)return;
            if(e.Kind==EnemyKind.Nest)
            {
                if(S.Time>20f&&e.AttackCD<=0&&V2.Distance(target.Pos,e.Pos)<16&&enemies.Count(x=>x.Kind!=EnemyKind.Nest)<MaxActiveEnemies){var pos=e.Pos+new V2(2,0);if(Walkable(pos))enemies.Add(new Enemy{Id=nextId++,Pos=pos,Kind=EnemyKind.VoidStalker,HP=42*DifficultyEnemyScale,MaxHP=42*DifficultyEnemyScale});e.AttackCD=16;}return;
            }
            bool defendingBeacon=S.Phase==Phase.Eclipse&&S.BeaconHP>0;
            if(!defendingBeacon&&V2.Distance(target.Pos,e.Pos)>22f)return;
            V2 goal=defendingBeacon?new V2():target.Pos;
            if(e.Blind<=0)
            {
                var direction=Navigate(e.Pos,goal,defendingBeacon?-1:target.Id);
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
            if(e.AttackCD<=0)
            {
                if(V2.Distance(target.Pos,e.Pos)<1.1f){Hurt(target,(e.Elite?18:9)*DifficultyEnemyScale);e.AttackCD=1;}
                else if(goal.Length<.01f&&e.Pos.Length<2){S.BeaconHP=Math.Max(0,S.BeaconHP-(e.Elite?15:6));e.AttackCD=1;}
                else {var wall=structures.Find(s=>s.Kind==2&&V2.Distance(s.Pos,e.Pos)<1.5f);if(wall!=null){wall.HP-=10;e.AttackCD=1;}}
            }
        }
        void Announce(string text,string actor="",string target=""){S.Event=text;S.EventActor=actor;S.EventTarget=target;S.EventTick=S.Tick;}
        void FX(V2 from,V2 to,int kind)=>effects.Add(new Effect{From=from,To=to,Kind=kind,Life=.3f});
        static float Clamp(float v,float min,float max)=>Math.Max(min,Math.Min(max,v));
        void Publish(){S.Players=players.ToArray();S.Enemies=enemies.ToArray();S.Nodes=nodes.ToArray();S.Structures=structures.ToArray();S.Effects=effects.ToArray();S.Projectiles=projectiles.ToArray();}
    }
}
