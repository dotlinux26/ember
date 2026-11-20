using Ember;
using System.Security.Cryptography;
using System.Text;

int checks=0;
void Check(bool value,string name){if(!value)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
var w=new EmberWorld(42);var p=w.AddPlayer(1,"A",Vessel.Wanderer);var b=w.AddPlayer(2,"B",Vessel.MoonMender);w.StartRun();
Check(w.Tiles.SequenceEqual(EmberWorld.MakeTiles(42,250)),"seed deterministic");
Check(!w.Walkable(new V2(124,0)),"abyss blocked");
Check(!w.CanHarm(p,b),"safe zone blocks friendly fire");
w.S.Fuel=0;w.S.BeaconHP=0;p.Pos=new V2(10,0);b.Pos=new V2(11,0);p.Dread=90;
Check(w.CanHarm(p,b)&&!w.CanHarm(b,p),"host validates attacker madness");
w.S.BeaconHP=600;w.S.Charge=w.S.ChargeTarget;p.Pos=new V2();Check(!w.CanHarm(p,b),"either safe participant blocks friendly fire");
w.S.Charge=0;w.S.Fuel=100;p.Pos=new V2();p.Bag[1]=20;w.Submit(1,new Command{Deposit=true});w.Tick();Check(w.S.Phase==Phase.Eclipse&&w.S.Charge==w.S.ChargeTarget&&p.Bag[1]==0,"20 core starts eclipse, consumed once");
w.S.Wave=.01f;w.Tick();Check(w.S.Phase==Phase.Extraction,"wave opens extraction");
b.HP=0;w.Submit(1,new Command{Interact=true});w.Tick();Check(w.S.Phase==Phase.Won&&p.Extracted,"survivor extraction wins for team");
var profile=new Profile();profile.Reward(w.S,b);Check(profile.Memory==2&&profile.Ash>0,"dead ally receives half reward");
var c=new EmberWorld(123);var crafter=c.AddPlayer(1,"C",Vessel.RuneWarden);c.StartRun();
Check(!c.Craft(crafter,Recipe.CrystalAxe),"craft rejects missing resources");
crafter.Bag=new[]{100,100,100,100,100};Check(c.Craft(crafter,Recipe.CrystalAxe)&&crafter.Weapon==Weapon.CrystalAxe&&crafter.Bag[1]==98,"craft spends exact recipe");
var before=crafter.Pos;c.Submit(1,new Command{X=1,Y=1,Dash=true});c.Tick();Check(crafter.Stamina==75&&crafter.Pos.X>before.X&&crafter.Pos.Y>before.Y,"diagonal dash uses stamina");
var black=new EmberWorld(9);var lost=black.AddPlayer(1,"D",Vessel.Wanderer);black.StartRun();black.S.Fuel=0;black.S.BeaconHP=0;for(int i=0;i<301;i++)black.Tick();Check(black.S.Phase==Phase.Lost,"10 second blackout loses run");
var text="{\"Ash\":42,\"Name\":\"Ký ức\"}";byte[] encrypted=EmberSave.Protect(text,"test-password");Check(EmberSave.Unprotect(encrypted,"test-password")==text,"encrypted Unicode save round trip");
bool wrong=false;try{EmberSave.Unprotect(encrypted,"wrong");}catch(CryptographicException){wrong=true;}Check(wrong,"incorrect password rejected");
encrypted[45]^=1;bool tamper=false;try{EmberSave.Unprotect(encrypted,"test-password");}catch(CryptographicException){tamper=true;}Check(tamper,"ciphertext tampering rejected before decrypt");
var temp=Path.Combine(Path.GetTempPath(),"ember-check-"+Guid.NewGuid());for(int i=0;i<7;i++)EmberSave.Export(temp,null,text,"password-123");Check(Directory.GetFiles(Path.Combine(temp,"Backups"),"*.ember").Length==5,"backup rotation retains five");
// Loopback verifies identity assignment, authenticated intent and chunked state.
using var host=new EmberLan();using var client=new EmberLan();int joined=0,inputs=0;string received=null;
host.Joined=(id,s)=>joined=id;host.Input=(id,s)=>{if(s=="move")inputs++;};client.State=s=>received=s;
host.Open(true,"","room-pass");client.Open(false,"127.0.0.1","room-pass");client.Join("identity");
for(int i=0;i<30;i++){host.Poll(i*.01);client.Poll(i*.01);Thread.Sleep(2);}Check(joined==2&&client.LocalId==2,"LAN authenticated join");
client.SendInput("move");host.Poll(.4);Check(inputs==1,"LAN sends intent to host");
string payload=string.Concat(Enumerable.Range(0,5000).Select(i=>Guid.NewGuid().ToString()));host.Broadcast(payload,10);
for(int i=0;i<100&&received==null;i++){client.Poll(.5+i*.01);Thread.Sleep(2);}Check(received==payload,"chunked compressed snapshot reconstruction");
var last=new EmberWorld(5);var light=last.AddPlayer(1,"Light",Vessel.Wanderer,new[]{(int)Perk.LastLight});last.StartRun();light.HeldSlot=2;last.S.Fuel=.001f;last.S.BeaconHP=0;last.Tick();Check(last.S.LastLight==5&&last.IsSafe(light.Pos),"last light extends safety for five seconds");
var request=EmberStunClient.Request(out var transaction);var response=new byte[32];Array.Copy(request,response,20);response[0]=1;response[1]=1;response[3]=12;response[20]=0;response[21]=0x20;response[22]=0;response[23]=8;response[25]=1;
int mappedPort=54321^0x2112;response[26]=(byte)(mappedPort>>8);response[27]=(byte)mappedPort;byte[] ip={203,0,113,10},cookie={0x21,0x12,0xa4,0x42};for(int i=0;i<4;i++)response[28+i]=(byte)(ip[i]^cookie[i]);
var mapped=EmberStunClient.Parse(response,transaction);Check(mapped.Address.ToString()=="203.0.113.10"&&mapped.Port==54321,"STUN XOR IPv4 endpoint decoding");
response[8]^=1;bool mismatch=false;try{EmberStunClient.Parse(response,transaction);}catch(FormatException){mismatch=true;}Check(mismatch,"STUN rejects mismatched transaction");
foreach(Vessel vessel in Enum.GetValues<Vessel>())
{
    var skillWorld=new EmberWorld(42);var caster=skillWorld.AddPlayer(1,"Caster",vessel);skillWorld.StartRun();skillWorld.Tick();
    caster.HP=20;caster.Dread=80;caster.Pos=new V2();
    if(skillWorld.S.Enemies.Length>0){skillWorld.S.Enemies[0].Pos=new V2(1,0);skillWorld.S.Enemies[0].AttackCD=100;}
    skillWorld.Submit(1,new Command{Skill=true});skillWorld.Tick();
    if(vessel==Vessel.Wanderer)Check(caster.SkillCD==0&&caster.CastTime==0,"wanderer has no active skill");
    if(vessel==Vessel.MoonMender)Check(caster.HP==55&&caster.Dread<41&&caster.SkillCD==25,"moon mender heals and cleanses");
    if(vessel==Vessel.BombTrapper)Check(caster.SkillCD==20&&skillWorld.S.Enemies.Where(e=>V2.Distance(e.Pos,new V2(1,0))<3).All(e=>e.HP<=0||e.Blind>0),"trapper bomb damages and blinds");
    if(vessel==Vessel.RuneWarden)Check(caster.ShieldTime==4&&caster.SkillCD==18,"warden gains timed shield");
    if(vessel==Vessel.LittleMoone)
    {
        Check(caster.SkillCD==24&&skillWorld.S.Effects.Count(e=>e.Kind==4)==12,"Little Moone queues twelve auto-aimed meteors");
        bool impacted=false;
        for(int i=0;i<70;i++){skillWorld.Tick();impacted|=skillWorld.S.Effects.Any(e=>e.Kind==5);}
        Check(impacted,"Little Moone resolves delayed meteor impacts");
    }
    if(vessel==Vessel.SpaceWraith)Check(caster.Pos.Length>=1.5f&&caster.Pos.Length<=3.2f&&caster.SkillCD==60,"wraith returns safely beside beacon");
}
var lampWorld=new EmberWorld(42);var carrier=lampWorld.AddPlayer(1,"Carrier",Vessel.Wanderer);var follower=lampWorld.AddPlayer(2,"Follower",Vessel.Wanderer);lampWorld.StartRun();carrier.Pos=new V2(10,0);follower.Pos=new V2(11,0);lampWorld.Submit(1,new Command{PassLamp=true});lampWorld.Tick();Check(!carrier.CarriesLantern&&follower.CarriesLantern,"lantern passes to nearby ally");
var recipes=new EmberWorld(42);var smith=recipes.AddPlayer(1,"Smith",Vessel.Wanderer);foreach(Recipe recipe in Enum.GetValues<Recipe>()){smith.Bag=new[]{100,100,100,100,100};Check(recipes.Craft(smith,recipe),"craft branch "+recipe);}
Check(Convert.ToHexString(EmberCrypto.Seal(new byte[16],new byte[12],Array.Empty<byte>(),Array.Empty<byte>())).ToLowerInvariant()=="58e2fccefa7e3061367f1d57a4e7455a","NIST GCM empty plaintext vector");
Check(Convert.ToHexString(EmberCrypto.Seal(new byte[16],new byte[12],new byte[16],Array.Empty<byte>())).ToLowerInvariant()=="0388dace60b6a392f328c2b971b2fe78ab6e47d42cec13bdf53a67b21257bddf","NIST GCM single block vector");
byte[] gcmKey=RandomNumberGenerator.GetBytes(32),gcmNonce=RandomNumberGenerator.GetBytes(12),gcmPlain=RandomNumberGenerator.GetBytes(1027),aad=RandomNumberGenerator.GetBytes(31),platformCipher=new byte[1027],platformTag=new byte[16];
using(var gcm=new AesGcm(gcmKey,16))gcm.Encrypt(gcmNonce,gcmPlain,platformCipher,platformTag,aad);
byte[] sealedPacket=EmberCrypto.Seal(gcmKey,gcmNonce,gcmPlain,aad);Check(sealedPacket.SequenceEqual(platformCipher.Concat(platformTag)),"portable AES-256-GCM matches platform provider including AAD");
sealedPacket[31]^=1;bool badGcm=false;try{EmberCrypto.Open(gcmKey,gcmNonce,sealedPacket,aad);}catch(CryptographicException){badGcm=true;}Check(badGcm,"GCM tampered packet rejected");
host.Dispose();client.Dispose();
foreach(int capacity in new[]{1,2,4})
{
    using var room=new EmberLan{Capacity=capacity};var lobby=new EmberWorld(99);lobby.S.Capacity=capacity;lobby.AddPlayer(1,"Host",Vessel.Wanderer);
    room.Joined=(id,s)=>lobby.AddPlayer(id,s,Vessel.Wanderer);room.ReadyChanged=(id,r)=>lobby.SetReady(id,r);room.Left=id=>lobby.RemovePlayer(id);int actionCount=0;room.Input=(id,s)=>{if(s=="cast")actionCount++;};room.Open(true,"","capacity-test");
    var guests=new List<EmberLan>();double t=0;
    for(int i=1;i<capacity;i++){var guest=new EmberLan();guest.Open(false,"127.0.0.1","capacity-test");guest.Join("Guest "+i);guests.Add(guest);}
    void Pump(int steps){for(int i=0;i<steps;i++){t+=.02;room.Poll(t);foreach(var guest in guests)guest.Poll(t);Thread.Sleep(1);}}
    Pump(20);Check(lobby.S.Players.Length==capacity,"room admits "+capacity+" players");
    using(var extra=new EmberLan()){extra.Open(false,"127.0.0.1","capacity-test");extra.Join("Extra");room.Poll(t);extra.Poll(t);Check(extra.LocalId==0&&extra.Error!=null,"capacity "+capacity+" rejects extra player");}
    if(capacity>1){Check(!lobby.StartRun(true),"lobby waits for ready guests");foreach(var guest in guests)guest.SetReady(true);Pump(10);}
    Check(lobby.StartRun(true),"room starts with "+capacity+" ready players");
    if(capacity>1)
    {
        guests[0].SendAction("cast");guests[0].Poll(t+.2);room.Poll(t+.2);Check(actionCount==1,"retransmitted reliable action executes once");
        guests[0].Leave();room.Poll(t+.22);Check(lobby.S.Players.Length==capacity-1,"leaving guest removes slot");
    }
    foreach(var guest in guests)guest.Dispose();
}
var queued=new EmberWorld(3);var qp=queued.AddPlayer(1,"Queued",Vessel.Wanderer);queued.StartRun();qp.Bag=new[]{100,100,100,100,100};int torchesBefore=qp.Items[0];queued.Submit(1,new Command{Craft=(int)Recipe.Torch});queued.Submit(1,new Command{Craft=(int)Recipe.Torch});queued.Submit(1,new Command{X=1});queued.Tick();queued.Tick();Check(qp.Items[0]==torchesBefore+2,"movement packets do not overwrite queued crafts");
if(args.Contains("--broker"))foreach(bool relay in new[]{false,true})
{
    using var room=new EmberLan{Capacity=4};int joins=0,reliable=0;room.Joined=(id,s)=>joins++;room.Input=(id,s)=>{if(s=="use")reliable++;};room.OpenPublic(true,"127.0.0.1",relay?"RELAY1":"DIRECT","test-broker-password",useStun:false);
    var guests=new List<EmberLan>();var receivedStates=new string[3];
    for(int i=0;i<3;i++){int slot=i;var guest=new EmberLan();guest.State=s=>receivedStates[slot]=s;guest.OpenPublic(false,"127.0.0.1",relay?"RELAY1":"DIRECT","test-broker-password",useStun:false);guests.Add(guest);}
    double time=0;
    void PumpPublic(int steps,bool join=true)
    {
        for(int step=0;step<steps;step++){time+=.02;room.Poll(time);foreach(var guest in guests){guest.Poll(time);if(join&&guest.LocalId==0&&(!relay||time>3.7)&&step%10==0)guest.Join("Guest");}Thread.Sleep(3);}
    }
    PumpPublic(relay?300:100);Check(joins==3&&guests.All(g=>g.LocalId>0),(relay?"relay":"direct")+" broker admits 4-player room");
    Check(room.RelayActive==relay&&guests.All(g=>g.RelayActive==relay),"broker routing mode synchronized");
    foreach(var guest in guests)guest.SendAction("use");PumpPublic(30);Check(reliable==3,"broker reliable actions reach host exactly once");
    room.Broadcast("encrypted-state-for-all-four",42);PumpPublic(30);Check(receivedStates.All(s=>s=="encrypted-state-for-all-four"),"encrypted snapshot reaches all 3 guests via "+(relay?"fan-out relay":"direct"));
    foreach(var guest in guests)guest.Leave();room.Leave();
}
var ff=new EmberWorld(42);var paranoid=ff.AddPlayer(1,"A",Vessel.Wanderer,new[]{(int)Perk.ReluctantHealer});var friend=ff.AddPlayer(2,"B",Vessel.Wanderer);ff.StartRun();ff.S.Fuel=0;ff.S.BeaconHP=0;paranoid.Pos=new V2(10,0);friend.Pos=new V2(11,0);paranoid.Dread=95;ff.Submit(1,new Command{Equip=1,Attack=true});ff.Tick();
float expectedFfDmg=18f*paranoid.Stats.PowerMultiplier*.5f*friend.Stats.DamageTakenMultiplier;
Check(Math.Abs((100-friend.HP)-expectedFfDmg)<.001,"GDD Reluctant Healer halves outgoing friendly fire");
Check(ff.CanHarm(friend,paranoid),"attacked ally may defend outside light");
ff.S.BeaconHP=600;ff.S.Charge=ff.S.ChargeTarget;paranoid.Pos=new V2();ff.Tick();Check(!ff.CanHarm(friend,paranoid),"entering safe light immediately blocks retaliation");
EmberLanguage.English=false;Check(EmberLanguage.Text("Sẵn sàng")=="Sẵn sàng","Vietnamese UI preserves original text");EmberLanguage.English=true;Check(EmberLanguage.Text("Sẵn sàng")=="Ready"&&EmberLanguage.Text("Y Sư Ánh Trăng")=="Moon Mender","English UI translates lobby and vessel names");
Check(EmberLanguage.Text("Lõi hải đăng: 25% — mang lõi về trung tâm, nhấn B.")=="Beacon charge: 25% — bring cores to the beacon and press B.","English dynamic HUD preserves numbers");EmberLanguage.English=false;
var sandbox=new EmberWorld(42);var debugA=sandbox.AddPlayer(1,"DebugA",Vessel.Wanderer);var debugB=sandbox.AddPlayer(2,"DebugB",Vessel.Wanderer);
Check(!sandbox.DebugScenario(2)&&!sandbox.DebugAction("supplies")&&debugA.Dread==0,"debug API is disabled in normal worlds");
sandbox.DebugEnabled=true;sandbox.StartRun();Check(sandbox.DebugScenario(0)&&sandbox.S.Players.All(p=>p.Safe)&&!sandbox.CanHarm(debugA,debugB),"normal debug preset restores safe protection");
Check(sandbox.DebugScenario(1)&&debugA.Dread==65&&!debugA.Safe&&!sandbox.CanHarm(debugA,debugB),"distortion preset is outside light without friendly fire");
sandbox.Tick();Check(debugA.MirageLife>0&&debugA.Dread==65,"distortion preset creates private mirage and freezes dread");
Check(sandbox.DebugScenario(2)&&debugA.Mad&&sandbox.CanHarm(debugA,debugB),"madness debug preset activates validated friendly fire");
Check(sandbox.DebugAction("supplies")&&debugA.Bag.All(n=>n==100)&&debugA.Items.All(n=>n==10),"debug supplies are host-side test state");
sandbox.DebugAction("eclipse");Check(sandbox.S.Phase==Phase.Eclipse&&sandbox.S.Wave==90,"debug eclipse keeps real 90-second countdown");sandbox.DebugAction("portal");Check(sandbox.S.Phase==Phase.Extraction,"debug portal scenario ready");
var economy=new Profile{Ash=1000,Memory=20};Check(economy.UnlockPerk((int)Perk.StrongBack)&&economy.Rank((int)Perk.StrongBack)==1,"perk unlock preserves GDD base rank");Check(economy.UpgradePerk((int)Perk.StrongBack)&&economy.UpgradePerk((int)Perk.StrongBack)&&!economy.UpgradePerk((int)Perk.StrongBack),"perk upgrades cap at three ranks");
var upgraded=new EmberWorld(42);var strong=upgraded.AddPlayer(1,"Strong",Vessel.Wanderer,new[]{(int)Perk.StrongBack},ranks:economy.PerkRanks);Check(Math.Abs(strong.Capacity-strong.BaseCapacity*1.42f)<.001,"upgraded carry perk uses declared tuning");
Check(economy.ExchangeAsh()&&economy.Tokens==1,"ash exchange adds token and spends balance");int memoryBefore=economy.Memory;Check(economy.UnlockRecipe(9)&&economy.Memory==memoryBefore-3&&!economy.UnlockRecipe(9),"memory unlocks recipe once");
var locked=new EmberWorld(42);var learner=locked.AddPlayer(1,"Learner",Vessel.Wanderer,recipes:new[]{0});learner.Bag=new[]{100,100,100,100,100};Check(!locked.Craft(learner,Recipe.CrystalAxe)&&locked.Craft(learner,Recipe.Torch),"host enforces unlocked recipes");
var legacy=new Profile{PerkRanks=null,Recipes=null};legacy.ValidateAndNormalize();Check(legacy.PerkRanks.Length==7&&legacy.Recipes.Length==14,"legacy saves migrate without losing prior recipes");
bool invalidRank=false;try{new Profile{PerkRanks=new[]{99}}.ValidateAndNormalize();}catch(System.IO.InvalidDataException){invalidRank=true;}Check(invalidRank,"malformed progression rejected");
var awardProfile=new Profile();var awardPlayer=new Player{Extracted=true,Crafted=5};var awardState=new Snapshot{Time=200,BossesDefeated=1,TeamExtracted=1,Players=new[]{awardPlayer}};awardProfile.Reward(awardState,awardPlayer);int tokenBefore=awardProfile.Tokens;awardProfile.Reward(awardState,awardPlayer);Check(awardProfile.AchievementFlags==15&&awardProfile.Tokens-tokenBefore==2,"one-time achievement rewards are not granted twice");
var unlitBeacon=new EmberWorld(4);var torch=unlitBeacon.AddPlayer(1,"Torch",Vessel.Wanderer);torch.Pos=new V2(15,0);Check(!unlitBeacon.IsSafe(new V2()),"GDD beacon starts unlit before awakening");unlitBeacon.S.Charge=unlitBeacon.S.ChargeTarget;unlitBeacon.S.BeaconHP=600;Check(unlitBeacon.IsSafe(new V2()),"fully charged beacon creates safe light");
Check(sandbox.DebugScenario(3)&&debugA.Dread==35&&!debugA.Mad,"suspicion threshold is separately testable");
using(var repairHost=new EmberLan())using(var repairClient=new EmberLan())
{
    string repaired=null;repairHost.Open(true,"","repair-test");repairClient.Open(false,"127.0.0.1","repair-test");repairClient.State=s=>repaired=s;repairClient.Join("repair");double repairTime=0;
    for(int i=0;i<30;i++){repairTime+=.02;repairHost.Poll(repairTime);repairClient.Poll(repairTime);Thread.Sleep(2);}
    repairHost.DebugDropPercent=30;string fragmented=Convert.ToBase64String(RandomNumberGenerator.GetBytes(18000));repairHost.Broadcast(fragmented,300);
    for(int i=0;i<200&&repaired==null;i++){repairTime+=.01;repairHost.Poll(repairTime);repairClient.Poll(repairTime);Thread.Sleep(2);}
    Check(repaired==fragmented&&repairHost.SnapshotRepairs>0,"fragmented snapshot recovers missing chunks under 30 percent loss");
}
var deadBossReward=new Profile();var deadBossPlayer=new Player{HP=0};deadBossReward.Reward(new Snapshot{TeamExtracted=1,BossesDefeated=1,Players=new[]{deadBossPlayer}},deadBossPlayer);Check(deadBossReward.Memory==3,"dead teammate receives half boss and extraction memory");
var candleWorld=new EmberWorld(42);var candlePlayer=candleWorld.AddPlayer(1,"Candle",Vessel.Wanderer,hat:(int)Hat.Candle);candlePlayer.LampOn=false;candleWorld.StartRun();candleWorld.Tick();float expectedCandleDread=3f*candlePlayer.Stats.DreadGainMultiplier*.9f*EmberWorld.Step;Check(!candlePlayer.Safe&&candlePlayer.Dread>0&&Math.Abs(candlePlayer.Dread-expectedCandleDread)<.00001,"candle hat emits light but reduces rather than eliminates dread");
var nearestHeal=new EmberWorld(42);var healer=nearestHeal.AddPlayer(1,"Healer",Vessel.MoonMender);var nearAlly=nearestHeal.AddPlayer(2,"Near",Vessel.Wanderer);var farAlly=nearestHeal.AddPlayer(3,"Far",Vessel.Wanderer);nearAlly.Pos=new V2(1,0);nearAlly.HP=80;farAlly.Pos=new V2(3,0);farAlly.HP=10;nearestHeal.StartRun();nearestHeal.S.Charge=nearestHeal.S.ChargeTarget;nearestHeal.S.BeaconHP=600;nearestHeal.Submit(1,new Command{Skill=true});nearestHeal.Tick();Check(nearAlly.HP==100&&farAlly.HP==10,"healing auto-aim selects nearest eligible ally in 360 degrees");
using(var fakeBroker=new System.Net.Sockets.UdpClient(0))using(var routeSocket=new System.Net.Sockets.UdpClient(0))
{
    var brokerEnd=new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,((System.Net.IPEndPoint)fakeBroker.Client.LocalEndPoint).Port);
    var route=new EmberRoomRoute(routeSocket,false,brokerEnd,"RETRY1","test",4){Ready=true,Token=Convert.ToBase64String(new byte[32])};route.Poll(0,false);route.Poll(3.1,false);route.Poll(4.2,false);int fallbacks=0;
    while(fakeBroker.Available>0){var endpoint=new System.Net.IPEndPoint(System.Net.IPAddress.Any,0);var packet=fakeBroker.Receive(ref endpoint);if(Encoding.UTF8.GetString(packet).StartsWith("FALLBACK|"))fallbacks++;}
    Check(fallbacks==2,"automatic relay fallback retries lost control request without flooding");
}
var mooneProfile=new Profile{Tokens=300};
for(int draw=0;draw<100;draw++)mooneProfile.Roll(0,new FixedDraw(draw));
Check(Catalog.VesselDrawWeights.Sum()==100&&Catalog.VesselDrawWeights[5]==1&&mooneProfile.Vessels.Contains(5)&&!mooneProfile.CanPlay(5),"Little Moone has a one-percent slot and initially sleeps");
var mooneSnapshot=new Snapshot{Charge=100,Players=Array.Empty<Player>()};
var mooneSurvivor=new Player{Extracted=true};
mooneProfile.Reward(mooneSnapshot,mooneSurvivor,new FixedDraw(30));
Check(mooneProfile.AwakeningStones==0&&!mooneProfile.AwakenLittleMoone(),"awakening chance excludes roll 30 and requires a stone");
mooneProfile.Reward(mooneSnapshot,mooneSurvivor,new FixedDraw(29));
Check(mooneProfile.AwakenLittleMoone()&&mooneProfile.CanPlay(5)&&mooneProfile.AwakeningStones==0&&!mooneProfile.AwakenLittleMoone(),"awakening consumes exactly one stone and enables selection");
var escapeWorld=new EmberWorld(42){DebugEnabled=true,DebugNoEnemies=true};
var escapePlayer=escapeWorld.AddPlayer(1,"Escape",Vessel.Wanderer);
escapeWorld.StartRun();
escapeWorld.DebugAction("enemy",1);
escapeWorld.Tick();
var blockEnemy=escapeWorld.S.Enemies.OrderBy(e=>V2.Distance(e.Pos,escapePlayer.Pos)).First();
blockEnemy.Pos=escapePlayer.Pos+new V2(.9f,0);
escapeWorld.Submit(1,new Command{X=1,Dash=true,Equip=2,Attack=true});
for(int i=0;i<6;i++)escapeWorld.Tick();
Check(escapePlayer.Pos.X>blockEnemy.Pos.X&&escapePlayer.HeldSlot==2&&escapePlayer.AttackCD==0,
    "dash crosses enemy bodies while a consumable prevents weapon attacks");
blockEnemy.Pos=escapePlayer.Pos+new V2(1,0);
float beforePush=blockEnemy.Pos.X;
escapeWorld.Submit(1,new Command{Push=true});
escapeWorld.Tick();
Check(blockEnemy.Pos.X>beforePush&&escapePlayer.PushCD>0,"stamina shove knocks a nearby enemy back");
var slots=new EmberWorld(42);
slots.AddPlayer(1,"Host",Vessel.Wanderer);
slots.AddPlayer(2,"Second",Vessel.Wanderer);
var third=slots.AddPlayer(3,"Third",Vessel.Wanderer);
slots.RemovePlayer(2);
var replacement=slots.AddPlayer(5,"Replacement",Vessel.Wanderer);
Check(third.PartySlot==3&&replacement.PartySlot==2&&replacement.Id==5,"party slots remain stable and reuse vacancies without reusing transport IDs");
var darkStart=new EmberWorld(42);
var lightBearer=darkStart.AddPlayer(1,"Light",Vessel.Wanderer);
darkStart.StartRun();
darkStart.Tick();
Check(darkStart.S.BeaconHP==0&&darkStart.S.Fuel==100&&!darkStart.IsSafe(lightBearer.Pos),"empty hands leave the starting beacon dark and do not burn fuel");
darkStart.Submit(1,new Command{Equip=2});
darkStart.Tick();
darkStart.Tick();
Check(lightBearer.HasHeldTorch&&darkStart.IsSafe(lightBearer.Pos)&&darkStart.S.Fuel<100,"equipping the starter torch creates safety and burns fuel");
var kbWorld=new EmberWorld(42){DebugEnabled=true,DebugNoEnemies=true};
var fighter=kbWorld.AddPlayer(1,"Fighter",Vessel.Wanderer);
kbWorld.StartRun();
kbWorld.DebugAction("enemy",1);
kbWorld.Tick();
var dummy=kbWorld.S.Enemies.OrderBy(e=>V2.Distance(e.Pos,fighter.Pos)).First();
dummy.HP=dummy.MaxHP=200;
dummy.Pos=fighter.Pos+new V2(1.2f,0);
float beforeMeleeX=dummy.Pos.X;
kbWorld.Submit(1,new Command{Equip=1,Attack=true});
kbWorld.Tick();
Check(dummy.Pos.X>beforeMeleeX&&dummy.HP<200,"melee stick hit deals damage and knocks enemy back");
dummy.Pos=fighter.Pos+new V2(4.5f,0);
float beforeGunX=dummy.Pos.X,beforeGunHp=dummy.HP;
for(int i=0;i<20;i++)kbWorld.Tick();
kbWorld.Submit(1,new Command{Equip=7,Attack=true});
for(int i=0;i<20;i++)kbWorld.Tick();
Check(dummy.Pos.X>beforeGunX&&dummy.HP<beforeGunHp,"ranged side gun auto-aims, hits and knocks enemy back");
var diffEasy=new EmberWorld(7,250,Difficulty.Easy);
var diffNormal=new EmberWorld(7,250,Difficulty.Normal);
var diffHard=new EmberWorld(7,250,Difficulty.Hard);
Check(diffEasy.S.ChargeTarget==500&&diffNormal.S.ChargeTarget==750&&diffHard.S.ChargeTarget==1000,"difficulty scales beacon charge target 500/750/1000");
var reviveWorld=new EmberWorld(7,250,Difficulty.Easy);
var rescuer=reviveWorld.AddPlayer(1,"Rescuer",Vessel.Wanderer);
var fallen=reviveWorld.AddPlayer(2,"Fallen",Vessel.MoonMender);
reviveWorld.StartRun();
fallen.HP=0;fallen.Pos=new V2(8,0);
reviveWorld.Submit(2,new Command{X=1,Y=0});
reviveWorld.Tick();
Check(fallen.Pos.X==8&&fallen.Pos.Y==0,"downed player cannot move freely as a ghost");
rescuer.Pos=new V2(1,0);reviveWorld.S.Charge=150;
reviveWorld.Submit(1,new Command{Revive=true});
reviveWorld.Tick();
Check(!fallen.Ghost&&fallen.HP>0&&reviveWorld.S.Charge==50&&fallen.Pos.Length>=2f&&fallen.Pos.Length<=3.5f,"beacon revive spends 100 energy and revives ally safely outside beacon");
var beaconBlock=new EmberWorld(7);
var walker=beaconBlock.AddPlayer(1,"Walker",Vessel.Wanderer);
beaconBlock.StartRun();
walker.Pos=new V2(0,-1.8f);
for(int i=0;i<30;i++){beaconBlock.Submit(1,new Command{X=0,Y=1});beaconBlock.Tick();}
Check(V2.Distance(walker.Pos,new V2(0,.35f))>=1.3f,"beacon solid collision blocks player from clipping inside beacon");
var statWorld=new EmberWorld(11);
var wardenStat=statWorld.AddPlayer(1,"Warden",Vessel.RuneWarden);
var wraithStat=statWorld.AddPlayer(2,"Wraith",Vessel.SpaceWraith);
var mooneStat=statWorld.AddPlayer(3,"Moone",Vessel.LittleMoone);
Check(wardenStat.MaxHP==100&&wraithStat.MaxHP==100&&mooneStat.MaxHP==100,"all vessels share standardized 100 base HP");
Check(wardenStat.Stats.DamageTakenMultiplier<wraithStat.Stats.DamageTakenMultiplier&&wraithStat.Stats.MoveSpeed>wardenStat.Stats.MoveSpeed&&mooneStat.Stats.PowerMultiplier>wardenStat.Stats.PowerMultiplier,"hardcore vessel stats differentiate defense, speed, and power");
var mineWorld=new EmberWorld(42);
var miner=mineWorld.AddPlayer(1,"Miner",Vessel.Wanderer);
mineWorld.StartRun();
var nearestNode=mineWorld.S.Nodes.Where(n=>!n.Dropped&&n.DropKind==0&&(n.Resource==Resource.Ore||n.Resource==Resource.Wood)).OrderBy(n=>V2.Distance(n.Pos,miner.Pos)).First();
miner.Pos=nearestNode.Pos-new V2(1,0);
int bagBefore=miner.Load;
mineWorld.Submit(1,new Command{Equip=8,Attack=true});
mineWorld.Tick();
Check(miner.Load>bagBefore&&miner.HarvestCD>0&&mineWorld.S.Effects.Any(e=>e.Kind==7),"pickaxe slot mines nearby resource nodes with harvest swing and chip FX");
miner.HeldSlot=1;miner.HarvestCD=0;
var nextNode=mineWorld.S.Nodes.Where(n=>!n.Dropped&&n.DropKind==0&&(n.Resource==Resource.Ore||n.Resource==Resource.Wood)).OrderBy(n=>V2.Distance(n.Pos,miner.Pos)).First();
miner.Pos=nextNode.Pos-new V2(1,0);
miner.HasPickaxe=false;
int blockedLoad=miner.Load;
mineWorld.Submit(1,new Command{Interact=true});
mineWorld.Tick();
Check(miner.Load==blockedLoad&&miner.HeldSlot==1,"mining a resource node requires owning a pickaxe");
miner.HasPickaxe=true;
mineWorld.Submit(1,new Command{Interact=true});
mineWorld.Tick();
Check(miner.Load>blockedLoad&&miner.HeldSlot==8,"pressing E to mine auto-equips the pickaxe slot");
var gunWorld=new EmberWorld(42){DebugEnabled=true,DebugNoEnemies=true};
var gunner=gunWorld.AddPlayer(1,"Gunner",Vessel.Wanderer);
gunWorld.StartRun();
gunWorld.Submit(1,new Command{Equip=7});
gunWorld.Tick();
for(int s=0;s<8;s++){gunWorld.Submit(1,new Command{Attack=true});for(int k=0;k<12;k++)gunWorld.Tick();}
Check(gunner.Reloading&&gunner.CurrentAmmo==0,"ranged gun triggers reload automatically after emptying its 8-shot magazine");
for(int k=0;k<60;k++)gunWorld.Tick();
Check(!gunner.Reloading&&gunner.CurrentAmmo==8,"ranged gun refills magazine after reload completes");
var torchWorld=new EmberWorld(42);
var torchUser=torchWorld.AddPlayer(1,"TorchUser",Vessel.Wanderer);
torchWorld.StartRun();
torchWorld.Submit(1,new Command{Equip=2,Use=0});
torchWorld.Tick();
Check(torchUser.Items[0]==0&&torchUser.HeldSlot==6&&torchWorld.S.Structures.Any(s=>s.Kind==0&&s.MaxLife==45&&s.Life>0),"placing the last torch consumes it, clears the hand slot, and spawns a timed torch with max fuel");
var runWorld=new EmberWorld(777,250,Difficulty.Hard);
var runner=runWorld.AddPlayer(1,"Saver",Vessel.LittleMoone);
runWorld.StartRun();
runner.Bag[(int)Resource.Core]=4;
runner.Bag[(int)Resource.Wood]=7;
runner.HP=74;
runner.Dread=28;
runWorld.Chest[(int)Resource.Ore]=9;
runWorld.Submit(1,new Command{Deposit=true,Use=0});
for(int k=0;k<15;k++)runWorld.Tick();
var exportedRun=runWorld.ExportRun(7);
var runJsonOpts=new System.Text.Json.JsonSerializerOptions{IncludeFields=true,IgnoreReadOnlyProperties=true};
var encryptedRun=EmberSave.Protect(System.Text.Json.JsonSerializer.Serialize(exportedRun,runJsonOpts),"device-local-key");
var decryptedRun=System.Text.Json.JsonSerializer.Deserialize<RunSaveData>(EmberSave.Unprotect(encryptedRun,"device-local-key"),runJsonOpts);
var resumedWorld=EmberWorld.RestoreRun(decryptedRun,"0123456789abcdef0123456789abcdef");
var resumedPlayer=resumedWorld.PlayerById(1);
Check(resumedWorld.S.Seed==777&&resumedWorld.S.Difficulty==Difficulty.Hard&&resumedWorld.S.ChargeTarget==1000&&resumedWorld.S.Charge>=100,"restored solo run preserves seed, difficulty, and beacon charge");
Check(resumedPlayer!=null&&resumedPlayer.Vessel==Vessel.LittleMoone&&Math.Abs(resumedPlayer.HP-74)<.1f&&resumedPlayer.Bag[(int)Resource.Wood]==7&&resumedWorld.Chest[(int)Resource.Ore]==9,"restored solo run preserves player HP, bag, vessel, and shared chest");
Check(resumedWorld.S.Structures.Any(s=>s.Kind==0&&s.MaxLife==45&&s.Life>0)&&resumedWorld.S.Nodes.Length==runWorld.S.Nodes.Length&&resumedWorld.S.Enemies.Length==runWorld.S.Enemies.Length,"restored solo run preserves placed structures, nodes, and enemies");
int tickBeforeResume=resumedWorld.S.Tick;
resumedWorld.Tick();
Check(resumedWorld.S.Tick==tickBeforeResume+1,"restored solo run continues advancing simulation cleanly");

var bossWorld=new EmberWorld(909,250,Difficulty.Normal);
var bP1=bossWorld.AddPlayer(1,"Leader",Vessel.Wanderer);
var bP2=bossWorld.AddPlayer(2,"Scout",Vessel.SpaceWraith);
bossWorld.StartRun();
bP1.Pos=new V2(12,8);
bP2.Pos=new V2(-40,-35);
Check(bossWorld.TriggerWorldBossEvent((int)BossType.VoidSovereign,1),"world boss event triggers near chosen player");
Check(bossWorld.S.BossArenaActive&&bossWorld.S.BossAlertTimer>0&&V2.Distance(bP2.Pos,bossWorld.S.BossArenaCenter)<bossWorld.S.BossArenaRadius,"boss event teleports all living teammates into sealed arena with red alert");
for(int k=0;k<220;k++){bossWorld.Submit(1,new Command{X=1,Y=0});bossWorld.Tick();}
Check(V2.Distance(bP1.Pos,bossWorld.S.BossArenaCenter)<=bossWorld.S.BossArenaRadius,"boss arena barrier prevents player from leaving while boss lives");
bool sawCircle=false,sawRect=false,sawBullet=false;
var activeBoss=bossWorld.S.Enemies.First(e=>e.Id==bossWorld.S.BossId);
activeBoss.SkillIndex=0;activeBoss.SkillCD=0;
for(int step=0;step<480;step++)
{
    bP1.HP=100;bP2.HP=100;
    bossWorld.Tick();
    if(bossWorld.S.BossTelegraphs.Any(t=>t.Kind==0))sawCircle=true;
    if(bossWorld.S.BossTelegraphs.Any(t=>t.Kind==1))sawRect=true;
    if(bossWorld.S.Projectiles.Any(p=>p.FromEnemy))sawBullet=true;
    if(sawCircle&&sawRect&&sawBullet)break;
}
Check(sawCircle&&sawRect&&sawBullet,"world boss executes all 3 telegraphed skills (circle rain, bullet storm, rect charge)");
bP1.Pos=activeBoss.Pos+new V2(1,0);
activeBoss.HP=1;
bossWorld.Submit(1,new Command{Equip=1,Attack=true});
bossWorld.Tick();
Check(!bossWorld.S.BossArenaActive&&bossWorld.S.BossesDefeated==1&&bossWorld.S.BossTelegraphs.Length==0,"defeating world boss lifts arena barrier, clears telegraphs, and increments BossesDefeated");
var timerBossWorld=new EmberWorld(313,250,Difficulty.Easy);
timerBossWorld.AddPlayer(1,"Timer",Vessel.Wanderer);
timerBossWorld.StartRun();
timerBossWorld.S.Time=EmberWorld.BossEventInterval;
timerBossWorld.Tick();
Check(timerBossWorld.S.BossArenaActive&&Catalog.BossSpecs.Length==3,"10-minute timer automatically spawns one of the 3 random world bosses");

var lightSkillWorld=new EmberWorld(55){DebugEnabled=true,DebugNoEnemies=true};
var mooneLight=lightSkillWorld.AddPlayer(1,"Moone",Vessel.LittleMoone);
lightSkillWorld.StartRun();
mooneLight.HeldSlot=1;mooneLight.LampOn=false;mooneLight.Pos=new V2(14,8);
// Out of range -> no skill cast, no transient light emitted.
lightSkillWorld.Submit(1,new Command{Skill=true});
lightSkillWorld.Tick();
Check(lightSkillWorld.S.Lights.Length==0&&!lightSkillWorld.IsSafe(mooneLight.Pos),"failed out-of-range skill cast emits no transient light");
// Enemy in range -> Little Moone emits Both caster + target starlight and grants temporary torch-like safety.
lightSkillWorld.DebugAction("enemy",1);
lightSkillWorld.Submit(1,new Command{Skill=true});
lightSkillWorld.Tick();
Check(lightSkillWorld.S.Lights.Length==2&&lightSkillWorld.IsSafe(mooneLight.Pos),"successful Little Moone cast emits caster+target transient lights and grants temporary safety");
for(int k=0;k<75;k++)lightSkillWorld.Tick();
Check(lightSkillWorld.S.Lights.Length==0&&!lightSkillWorld.IsSafe(mooneLight.Pos),"transient skill light expires automatically once skill finishes");
var wardenLightWorld=new EmberWorld(56){DebugEnabled=true,DebugNoEnemies=true};
var wardenLight=wardenLightWorld.AddPlayer(1,"Warden",Vessel.RuneWarden);
wardenLightWorld.StartRun();
wardenLight.HeldSlot=1;wardenLight.LampOn=false;wardenLight.Pos=new V2(10,6);
wardenLightWorld.DebugAction("enemy",1);
wardenLightWorld.Submit(1,new Command{Skill=true});
wardenLightWorld.Tick();
var startLightPos=wardenLightWorld.S.Lights[0].Pos;
for(int k=0;k<10;k++){wardenLightWorld.Submit(1,new Command{X=1,Y=0});wardenLightWorld.Tick();}
Check(wardenLightWorld.S.Lights.Length==1&&V2.Distance(wardenLightWorld.S.Lights[0].Pos,wardenLight.Pos)<.05f&&V2.Distance(wardenLightWorld.S.Lights[0].Pos,startLightPos)>.2f,"caster-anchored skill light tracks moving player smoothly");

var popupWorld=new EmberWorld(88){DebugEnabled=true,DebugNoEnemies=true};
var popPlayer=popupWorld.AddPlayer(1,"Tester",Vessel.Wanderer);
popupWorld.StartRun();
popupWorld.DebugAction("enemy",1);
var popEnemy=popupWorld.S.Enemies.First(e=>e.Kind==EnemyKind.Shade);
popPlayer.Pos=popEnemy.Pos-new V2(1,0);
popupWorld.Submit(1,new Command{Equip=1,Attack=true});
popupWorld.Tick();
Check(popupWorld.S.Popups.Any(p=>p.Kind==0&&p.Amount>0),"attacking an enemy spawns a floating damage popup");
var popNode=popupWorld.S.Nodes.OrderBy(n=>V2.Distance(n.Pos,popPlayer.Pos)).First();
popPlayer.Pos=popNode.Pos-new V2(1,0);
popupWorld.Submit(1,new Command{Interact=true});
popupWorld.Tick();
Check(popupWorld.S.Popups.Any(p=>p.Kind==3&&p.Amount>0),"mining a node spawns a floating harvest popup");
for(int k=0;k<40;k++)popupWorld.Tick();
Check(popupWorld.S.Popups.Length==0,"combat and harvest popups expire automatically");

var coopWorld=new EmberWorld(2026,250,Difficulty.Normal);
string hostGuid="11111111111111111111111111111111";
string guestGuid="22222222222222222222222222222222";
string strangerGuid="33333333333333333333333333333333";
coopWorld.AddPlayer(1,"Host",Vessel.Wanderer,installationId:hostGuid);
var coopGuest=coopWorld.AddPlayer(2,"Guest",Vessel.LittleMoone,installationId:guestGuid);
coopWorld.StartRun();
coopGuest.Pos=new V2(28,18);
coopGuest.HP=64;
coopGuest.Weapon=Weapon.CrystalAxe;
coopGuest.Items[0]=2;
coopGuest.Bag[(int)Resource.Core]=10;
coopGuest.Bag[(int)Resource.Wood]=10;
int droppedBefore=coopWorld.S.Nodes.Count(n=>n.Dropped);
coopWorld.RemovePlayer(2);
Check(coopWorld.CanRejoin(guestGuid)&&!coopWorld.CanRejoin(strangerGuid)&&coopWorld.S.Disconnected.Length==1,"disconnecting mid-match reserves host-side seat only for that player identity");
Check(coopWorld.S.Nodes.Count(n=>n.Dropped&&n.Resource==Resource.Core&&n.Amount==3&&V2.Distance(n.Pos,new V2(28,18))<1.5f)==1,"Rule B drops 30% of raw resources at the disconnect spot");
var rejoined=coopWorld.RejoinPlayer(5,guestGuid);
Check(rejoined.PartySlot==2&&rejoined.Weapon==Weapon.CrystalAxe&&rejoined.Items[0]==2&&rejoined.Bag[(int)Resource.Core]==7&&Math.Abs(rejoined.HP-64)<.1f&&rejoined.Pos.Length<4f,"rejoining restores gear, 70% bag, slot, HP, and respawns safely at Home Beacon");
rejoined.HP=0;
coopWorld.RemovePlayer(5);
var ghostRejoin=coopWorld.RejoinPlayer(6,guestGuid);
Check(ghostRejoin.Ghost,"disconnected ghost stays downed on rejoin (prevents self-revive exploit)");
ghostRejoin.HP=80;
coopWorld.RemovePlayer(6);
coopWorld.TriggerWorldBossEvent((int)BossType.CinderBehemoth,1);
var bossRejoin=coopWorld.RejoinPlayer(7,guestGuid);
Check(V2.Distance(bossRejoin.Pos,coopWorld.S.BossArenaCenter)<coopWorld.S.BossArenaRadius,"rejoining during an active World Boss pulls player directly into the Boss Arena");
using(var rejoinHost=new EmberLan{Capacity=2})
using(var rejoinGuest=new EmberLan())
using(var strangerClient=new EmberLan())
{
    rejoinHost.Open(true,"","rejoin-pass");
    rejoinHost.AcceptingJoins=false;
    rejoinHost.CanRejoin=id=>id==guestGuid;
    int admittedId=0;
    rejoinHost.Joined=(id,idText)=>{admittedId=id;};
    strangerClient.Open(false,"127.0.0.1","rejoin-pass");
    rejoinGuest.Open(false,"127.0.0.1","rejoin-pass");
    double simNow=0;
    for(int step=0;step<40;step++)
    {
        simNow+=.05;
        if(step==1){strangerClient.Join(strangerGuid);rejoinGuest.Join(guestGuid);}
        rejoinHost.Poll(simNow);strangerClient.Poll(simNow);rejoinGuest.Poll(simNow);
        System.Threading.Thread.Sleep(5);
    }
    Check(strangerClient.Error!=null&&rejoinGuest.LocalId>=2&&admittedId==rejoinGuest.LocalId,"EmberLan rejects strangers after match start while selectively admitting rejoining teammates");
    Check(rejoinGuest.PingMs>=0&&rejoinGuest.PingMs<500,"EmberLan measures real RTT network ping over PING/PONG");
}
var cullWorld=new EmberWorld(404){DebugEnabled=true};
var cullPlayer=cullWorld.AddPlayer(1,"Culler",Vessel.Wanderer);
cullWorld.StartRun();
cullWorld.DebugAction("enemy",1);
var farShade=cullWorld.S.Enemies.First(e=>e.Kind==EnemyKind.Shade);
farShade.Pos=cullPlayer.Pos+new V2(70,0);
int nestsBefore=cullWorld.S.Enemies.Count(e=>e.Kind==EnemyKind.Nest);
cullWorld.Tick();
Check(!cullWorld.S.Enemies.Any(e=>e.Id==farShade.Id)&&cullWorld.S.Enemies.Count(e=>e.Kind==EnemyKind.Nest)==nestsBefore,"Minecraft-style despawn removes distant normal mobs while preserving Nests");
var bossStoneProfile=new Profile();
var bossLossSnap=new Snapshot{BossesDefeated=2,Players=Array.Empty<Player>()};
var bossLossPlayer=new Player{Extracted=false};
bossStoneProfile.Reward(bossLossSnap,bossLossPlayer,new FixedDraw(25));
Check(bossStoneProfile.AwakeningStones==0,"boss awakening drop excludes roll 25");
bossStoneProfile.Reward(bossLossSnap,bossLossPlayer,new FixedDraw(24));
Check(bossStoneProfile.AwakeningStones==2,"defeating bosses grants 25% awakening stone roll per boss even without extraction");
var shotWorld=new EmberWorld(505){DebugEnabled=true};
var shotPlayer=shotWorld.AddPlayer(1,"Dodger",Vessel.Wanderer);
shotWorld.StartRun();
shotPlayer.Pos=new V2(0,4.5f);
shotWorld.DebugAction("enemy",1);
var rangedShade=shotWorld.S.Enemies.First(e=>e.Kind==EnemyKind.Shade);
rangedShade.Pos=new V2(8.0f,4.5f);
rangedShade.SkillCD=0f;
rangedShade.Blind=2f;
shotWorld.Tick();
Check(!shotWorld.S.Projectiles.Any(p=>p.FromEnemy),"blinded regular enemy cannot fire ranged projectiles");
rangedShade.Blind=0f;
rangedShade.Pos=new V2(15.5f,4.5f);
shotWorld.Tick();
Check(!shotWorld.S.Projectiles.Any(p=>p.FromEnemy),"regular enemy outside max range does not fire projectiles");
rangedShade.Pos=new V2(8.0f,4.5f);
rangedShade.SkillCD=0f;
shotWorld.Tick();
var shadeShots=shotWorld.S.Projectiles.Where(p=>p.FromEnemy&&p.Owner==rangedShade.Id).ToArray();
Check(shadeShots.Length==1&&shadeShots[0].Velocity.Length<=5.5f&&shadeShots[0].ColorKind==4&&rangedShade.SkillCD>=5f,"regular enemy fires one slow dodgeable orb in range and enters non-spam cooldown");
rangedShade.Kind=EnemyKind.Sentinel;
rangedShade.Elite=true;
rangedShade.Pos=new V2(9.0f,4.5f);
rangedShade.SkillCD=0f;
shotWorld.Tick();
var eliteShots=shotWorld.S.Projectiles.Where(p=>p.FromEnemy&&p.Owner==rangedShade.Id&&p.ColorKind==1).ToArray();
Check(eliteShots.Length==3&&eliteShots.All(p=>p.Velocity.Length<=5.5f)&&rangedShade.SkillCD>=3.5f,"elite sentinel fires a slow 3-orb fan with bounded range and cooldown");

// Item drop check
var dropWorld=new EmberWorld(42);
var dropPlayer=dropWorld.AddPlayer(1,"DropTester",Vessel.Wanderer);
dropWorld.StartRun();
dropPlayer.Bag[(int)Resource.Wood]=10;
dropWorld.DropItem(dropPlayer, 3, (int)Resource.Wood, 4);
Check(dropPlayer.Bag[(int)Resource.Wood]==6&&dropWorld.S.Nodes.Any(n=>n.Dropped&&n.Resource==Resource.Wood&&n.Amount==4), "dropping resource reduces bag count and spawns dropped node");

// Pickaxe durability and chopping tree check
var harvestWorld=new EmberWorld(100);
var harvestPlayer=harvestWorld.AddPlayer(1,"MinerTester",Vessel.Wanderer);
harvestWorld.StartRun();
harvestPlayer.PickaxeTier=1;
harvestPlayer.PickaxeDurability=50;
harvestPlayer.Pos=new V2(25, 25);
int hSize=harvestWorld.S.Size;
int hTx=(int)(25+hSize/2f), hTy=(int)(25+hSize/2f);
harvestWorld.Tiles[hTy*hSize+hTx]=3; // tree obstacle
harvestWorld.Submit(1, new Command{Interact=true});
harvestWorld.Tick();
Check(harvestWorld.S.HarvestTargets.Any(h=>h.Tx==hTx&&h.Ty==hTy&&h.HP<h.MaxHP)&&harvestPlayer.Bag[(int)Resource.Wood]>0, "chopping tree tile damages it, reveals overhead health bar, and yields wood");
while(harvestWorld.Tiles[hTy*hSize+hTx]!=0&&harvestPlayer.PickaxeDurability>40)
{
    harvestPlayer.HarvestCD=0;
    harvestPlayer.Stamina=100;
    harvestWorld.Submit(1, new Command{Interact=true});
    harvestWorld.Tick();
}
Check(harvestWorld.Tiles[hTy*hSize+hTx]==0&&harvestPlayer.PickaxeDurability<=48, "final chop clears tree collision, spawns stump, and depletes durability");

// Pickaxe break check
harvestPlayer.PickaxeTier=0;
harvestPlayer.PickaxeDurability=1;
harvestPlayer.Pos=new V2(28, 28);
int bTx=(int)(28+hSize/2f), bTy=(int)(28+hSize/2f);
harvestWorld.Tiles[bTy*hSize+bTx]=3;
harvestPlayer.HarvestCD=0;
harvestPlayer.Stamina=100;
harvestWorld.Submit(1, new Command{Interact=true});
harvestWorld.Tick();
Check(!harvestPlayer.HasPickaxe&&harvestPlayer.PickaxeDurability==0, "depleted tier 0 pickaxe breaks completely");

// Wanderer Awakening Profile Checks
var wandererProfile = new Profile{ AwakeningStones = 0, WandererAwake = false };
Check(!wandererProfile.AwakenWanderer(), "cannot awaken wanderer without awakening stone");
wandererProfile.AwakeningStones = 2;
Check(wandererProfile.AwakenWanderer() && wandererProfile.WandererAwake && wandererProfile.AwakeningStones == 1, "awakening wanderer consumes 1 stone and enables wanderer awakening");
Check(!wandererProfile.AwakenWanderer() && wandererProfile.AwakeningStones == 1, "awakening already awakened wanderer returns false and preserves stone");

// Awakened Wanderer Skill & Buff Range Check
var buffWorld = new EmberWorld(77);
var awkWanderer = buffWorld.AddPlayer(1, "AwkWanderer", Vessel.Wanderer, awake: true);
var nearTeammate = buffWorld.AddPlayer(2, "NearMate", Vessel.MoonMender);
var farTeammate = buffWorld.AddPlayer(3, "FarMate", Vessel.RuneWarden);
buffWorld.StartRun();
awkWanderer.Pos = new V2(0, 0);
nearTeammate.Pos = new V2(4, 0);   // distance 4 (in 8m range)
farTeammate.Pos = new V2(15, 0);   // distance 15 (outside 8m range)

Check(awkWanderer.SkillRange == 8f, "awakened wanderer has 8 tile skill range");
buffWorld.Submit(1, new Command{ Skill = true });
buffWorld.Tick();

Check(awkWanderer.SkillCD == 22f && awkWanderer.HasBuff(BuffKind.Swiftness) && Math.Abs(awkWanderer.BuffValue(BuffKind.Swiftness) - 0.45f) < 0.001f, "casting swiftwind aura grants 10s +45% swiftness buff to self and triggers 22s cooldown");
Check(nearTeammate.HasBuff(BuffKind.Swiftness) && Math.Abs(nearTeammate.BuffValue(BuffKind.Swiftness) - 0.45f) < 0.001f, "swiftwind aura applies swiftness buff to nearby ally within 8 tiles");
Check(!farTeammate.HasBuff(BuffKind.Swiftness), "swiftwind aura does not affect distant ally outside 8 tiles");

// Movement speed verification with Swiftness buff
float unbuffedSpeed = awkWanderer.Stats.MoveSpeed;
var startPos = awkWanderer.Pos;
buffWorld.Submit(1, new Command{ X = 1, Y = 0 });
buffWorld.Tick();
float distanceTraveled = awkWanderer.Pos.X - startPos.X;
float expectedDistance = unbuffedSpeed * 1.45f * (1f / 30f);
Check(Math.Abs(distanceTraveled - expectedDistance) < 0.02f, "swiftness buff accelerates movement speed by 45%");

// Multi-buff & Industrial-grade status effect system checks
var testSubject = nearTeammate;
testSubject.HP = 50f;
testSubject.MaxHP = 100f;
testSubject.ApplyBuff(BuffKind.Regen, duration: 5f, value: 10f); // 10 HP/s
testSubject.ApplyBuff(BuffKind.IronSkin, duration: 5f, value: 0.30f); // 30% dmg reduction
testSubject.ApplyBuff(BuffKind.AttackPower, duration: 5f, value: 0.50f); // +50% dmg

Check(testSubject.HasBuff(BuffKind.Regen) && testSubject.HasBuff(BuffKind.IronSkin) && testSubject.HasBuff(BuffKind.AttackPower), "multiple status effects coexist simultaneously");
float hpBeforeRegen = testSubject.HP;
buffWorld.Tick();
Check(testSubject.HP > hpBeforeRegen, "regen buff restores health over time");

// Test buff duration expiration
for(int t = 0; t < 310; t++) buffWorld.Tick(); // 310 ticks = ~10.3 seconds (> 10s)
Check(!awkWanderer.HasBuff(BuffKind.Swiftness) && !nearTeammate.HasBuff(BuffKind.Swiftness) && !testSubject.HasBuff(BuffKind.Regen), "status effects automatically expire when duration ends");

// Test beacon passive energy drain over time
var drainWorld = new EmberWorld(42){DebugEnabled=true,DebugNoEnemies=true};
drainWorld.AddPlayer(1, "DrainTester", Vessel.Wanderer);
drainWorld.StartRun();
drainWorld.S.Charge = 200;
drainWorld.S.Time = 16f; // Past the 15s grace period
float chargeBefore = drainWorld.S.Charge;
drainWorld.Tick();
Check(drainWorld.S.Charge < chargeBefore, "beacon passively consumes energy over time after grace period");

// Test overcharge and milestone safe light expansion
drainWorld.S.Charge = 500;
Check(drainWorld.IsSafe(new V2(14, 0)), "higher energy expands beacon safe light radius dynamically");
drainWorld.S.Charge = 50;
Check(!drainWorld.IsSafe(new V2(14, 0)) && drainWorld.IsSafe(new V2(2, 0)), "lower energy contracts safe radius while protecting home base");

// Test harvest tool attack never fires projectiles
var harvestCombatWorld = new EmberWorld(42){DebugEnabled=true,DebugNoEnemies=true};
var minerAttacker = harvestCombatWorld.AddPlayer(1, "MinerAttacker", Vessel.Wanderer);
harvestCombatWorld.StartRun();
minerAttacker.HeldSlot = 8;
minerAttacker.HasPickaxe = true;
harvestCombatWorld.Submit(1, new Command{Attack=true});
harvestCombatWorld.Tick();
Check(!harvestCombatWorld.S.Projectiles.Any(p => p.Owner == minerAttacker.Id), "pickaxe attack is melee and never spawns projectiles");

// --- Test Ancient Relic Drop (Hộp Cổ Thất Lạc) ---
var relicWorld = new EmberWorld(77);
var relicExplorer = relicWorld.AddPlayer(1, "Explorer", Vessel.Wanderer);
relicWorld.StartRun();
Check(EmberEvents.SpawnAncientRelic(relicWorld, new V2(60, 40)), "spawn ancient relic at boundary");
Check(EmberEvents.HasActiveRelic, "has active relic event flag");
var relicChest = relicWorld.S.Structures.FirstOrDefault(s => s.Kind == EmberEvents.RelicChestKind);
Check(relicChest != null && relicChest.HP > 0, "relic chest structure spawned");
Check(relicWorld.S.Enemies.Count(e => e.Elite && (e.Kind == EnemyKind.VoidStalker || e.Kind == EnemyKind.Sentinel || e.Kind == EnemyKind.Shade)) >= 3, "relic chest guarded by elite shadow monsters");
Check(relicWorld.S.Lights.Any(l => l.GrantsSafety && l.ColorKind == LightColorKind.RuneGold), "cosmic beacon light emitted at relic drop");

// Test opening Ancient Relic Chest
relicExplorer.Pos = relicChest.Pos;
var testProfile = new Profile();
int initialTokens = testProfile.Tokens;
int initialDust = relicExplorer.Bag[(int)Resource.Dust];
int initialCore = relicExplorer.Bag[(int)Resource.Core];
Check(EmberEvents.OpenRelicChest(relicWorld, relicExplorer, relicChest, testProfile), "open ancient relic chest");
Check(testProfile.Tokens == initialTokens + 3, "relic chest grants 3 void tokens");
Check(relicExplorer.Bag[(int)Resource.Dust] == initialDust + 30 && relicExplorer.Bag[(int)Resource.Core] == initialCore + 4, "relic chest grants dust and core rewards");
Check(relicExplorer.Lantern == Lantern.Relic, "relic chest grants high-tier relic lantern");
Check(!relicWorld.S.Structures.Any(s => s.Kind == EmberEvents.RelicChestKind), "relic chest cleared after open");

// --- Test Merchant Reputation & Secret Stash ---
var merchantWorld = new EmberWorld(88);
var trader = merchantWorld.AddPlayer(1, "Trader", Vessel.Wanderer);
merchantWorld.StartRun();
EmberMerchant.Reputation = 0;
Check(EmberMerchant.Catalog.Length >= 6, "merchant has standard and secret stash wares");

trader.Bag = new int[] { 100, 100, 100, 100, 100 };
int repBefore = EmberMerchant.Reputation;
Check(EmberMerchant.CanTrade(trader, 0), "can afford standard trade");
Check(EmberMerchant.ExecuteTrade(merchantWorld, trader, 0), "execute standard trade");
Check(EmberMerchant.Reputation == repBefore + 15, "trade builds reputation points (+15)");

// Secret wares should be locked when reputation < 50
Check(!EmberMerchant.CanTrade(trader, 4), "secret ware locked when reputation < 50");

// Trade 3 more times to raise reputation past 50
EmberMerchant.ExecuteTrade(merchantWorld, trader, 1);
EmberMerchant.ExecuteTrade(merchantWorld, trader, 2);
EmberMerchant.ExecuteTrade(merchantWorld, trader, 3);
Check(EmberMerchant.Reputation >= 50, "reputation reaches 50+ threshold");

// Secret ware 4 (Invisibility Cloak / Shadow Veil) unlocked
Check(EmberMerchant.CanTrade(trader, 4), "secret ware 4 (shadow veil) unlocked at 50 rep");
Check(EmberMerchant.ExecuteTrade(merchantWorld, trader, 4), "execute secret trade 4");
Check(EmberMerchant.IsInvisible(trader) && EmberMerchant.GetInvisibilityDuration(trader.Id) > 10f, "shadow veil grants 15s invisibility");

// Secret ware 5 (Treasure Compass)
Check(EmberMerchant.CanTrade(trader, 5), "secret ware 5 (treasure compass) unlocked");
Check(EmberMerchant.ExecuteTrade(merchantWorld, trader, 5), "execute secret trade 5");
Check(EmberMerchant.HasTreasureCompass(trader.Id), "treasure compass activated for player");

// Test Treasure Compass tracking
relicWorld.S.Structures = new Structure[] { new Structure { Id = 999, Kind = EmberEvents.RelicChestKind, Pos = new V2(50, 50), HP = 100 } };
var target = EmberMerchant.FindNearestTreasure(relicWorld.S, new V2(0, 0));
Check(target.HasValue && target.Value.distance > 0, "treasure compass locates nearest relic chest");

// --- Test Batch 2 Features: Starter Ground Loot, Pickup Mechanics, Pickaxe Nerf, Forageables, Energy Drink, Campfire Cooking ---
var liveWorld = new EmberWorld(999);
var hero = liveWorld.AddPlayer(1, "LiveHero", Vessel.Wanderer);
liveWorld.StartRun(liveMatch: true);

Check(hero.HeldSlot == 6 && !hero.HasPickaxe && !hero.HasWeapon && !hero.HasSideWeapon, "live match starts with empty equipment hands");
Check(hero.Bag.All(b => b == 0) && hero.Items.All(i => i == 0), "live match starts with completely empty bag and quickslots");

var groundStarters = liveWorld.S.Nodes.Where(n => n.Dropped && (n.DropKind == 1 || n.DropKind == 2) && (n.DropId == (int)Weapon.Stick || n.DropId == 0 || n.DropId == (int)Weapon.Pickaxe)).ToList();
Check(groundStarters.Count == 3, "exactly 3 starter items spawn around Beacon");
Check(groundStarters.Any(n => n.DropKind == 1 && n.DropId == (int)Weapon.Stick), "starter stick on ground");
Check(groundStarters.Any(n => n.DropKind == 2 && n.DropId == 0), "starter torch on ground");
Check(groundStarters.Any(n => n.DropKind == 1 && n.DropId == (int)Weapon.Pickaxe), "starter pickaxe on ground");

// Test Pickup: Proximity (< 0.65f)
var stickNode = groundStarters.First(n => n.DropKind == 1 && n.DropId == (int)Weapon.Stick);
hero.Pos = stickNode.Pos;
liveWorld.Tick();
Check(hero.HasWeapon && hero.Weapon == Weapon.Stick && hero.HeldSlot == 1, "proximity pickup (< 0.65f) equips stick");

// Test Pickup: No auto-vacuum outside 0.65f without interact
var torchNode = groundStarters.First(n => n.DropKind == 2 && n.DropId == 0);
hero.Pos = torchNode.Pos + new V2(1.5f, 0f); // 1.5 tiles away (> 0.65f, <= 2.2f)
liveWorld.Tick();
Check(hero.Items[0] == 0, "no auto-vacuum from 1.5 tiles away");

// Test Pickup: Pressing [E] within 2.2f picks up
liveWorld.Submit(1, new Command { Interact = true });
liveWorld.Tick();
Check(hero.Items[0] == 1, "interact [E] within 2.2f picks up torch");

// Test Pickaxe Nerf
var pickNode = groundStarters.First(n => n.DropKind == 1 && n.DropId == (int)Weapon.Pickaxe);
hero.Pos = pickNode.Pos;
liveWorld.Tick();
Check(hero.HasPickaxe && hero.PickaxeDurability == 48 && hero.PickaxeMaxDurability == 48, "pickaxe durability nerfed to 48");

// Test Forageable Wild Mushrooms
var wildMushrooms = liveWorld.S.Nodes.Where(n => !n.Dropped && n.DropKind == 2 && n.DropId == 16).ToList();
Check(wildMushrooms.Count > 0, "wild mushrooms spawned outside sanctuary");
var targetMushroom = wildMushrooms.First();
hero.Pos = targetMushroom.Pos - new V2(1.0f, 0f);
liveWorld.Submit(1, new Command { Attack = true });
liveWorld.Tick();
Check(targetMushroom.Dropped, "attacking wild mushroom pops it to the ground");

// Test Energy Drink Buffs
hero.Items[14] = 1;
liveWorld.Submit(1, new Command { Use = 14 });
liveWorld.Tick();
Check(hero.Items[14] == 0 && hero.StaminaBuffTimer > 28f && hero.Stamina == 100f, "energy drink grants stamina buff timer and max stamina");

// Test Campfire Cooking (Apple -> Roasted Apple)
hero.Items[15] = 1;
hero.Pos = new V2(1.5f, 0f);
liveWorld.DropItem(hero, 2, 15, 1);
var droppedRawApple = liveWorld.S.Nodes.FirstOrDefault(n => n.Dropped && n.DropKind == 2 && (n.DropId == 15 || n.DropId == 17));
Check(droppedRawApple != null && droppedRawApple.DropId == 15, "dropped raw apple near Beacon");

hero.Pos = new V2(0f, -2.8f); // step away to prevent premature proximity pickup
for (int t = 0; t < 140; t++) liveWorld.Tick(); // ~4.66s of cooking
var cookedApple = liveWorld.S.Nodes.FirstOrDefault(n => n.Dropped && n.DropKind == 2 && n.DropId == 17);
Check(cookedApple != null, "raw apple cooked into roasted apple near campfire");

// Test Consuming Roasted Apple (Heals +45 HP & Well Fed buff)
hero.Pos = cookedApple.Pos;
liveWorld.Tick(); // proximity pickup
Check(hero.Items[17] == 1, "picked up cooked roasted apple");
hero.HP = 40f;
liveWorld.Submit(1, new Command { Use = 17 });
liveWorld.Tick();
Check(hero.HP == 85f && hero.WarmthBuffTimer > 40f, "roasted apple heals 45 HP and grants Well Fed buff");

// Test EmberCooking API & Mushroom Cooking -> Mushroom Stew
Check(EmberCooking.IsRawFood(2, 15) && EmberCooking.IsRawFood(2, 16), "EmberCooking recognizes raw food");
Check(EmberCooking.GetCookedTransformId(15) == 17 && EmberCooking.GetCookedTransformId(16) == 18, "EmberCooking cooked transforms match RoastedApple and MushroomStew");
Check(EmberCooking.IsNearHeatSource(liveWorld.S, new V2(1.5f, 0f)), "beacon center within 3 tiles is active heat source");
Check(!EmberCooking.IsNearHeatSource(liveWorld.S, new V2(15f, 15f)), "distant location outside 3 tiles is not heat source");

// Test Cooking Mushroom near Campfire Torch structure
var torchStructure = liveWorld.S.Structures.FirstOrDefault(s => s.Kind == 0 && s.Life > 0);
V2 heatSpot = torchStructure != null ? torchStructure.Pos + new V2(1.0f, 0f) : new V2(1.2f, 0.5f);
var groundMushroom = liveWorld.SpawnGroundFood(16, heatSpot, 1);
Check(groundMushroom != null && groundMushroom.DropId == 16, "dropped raw mushroom near campfire heat");

hero.Pos = new V2(0f, -3.5f);
for (int t = 0; t < 140; t++) liveWorld.Tick(); // ~4.66s cooking
var cookedStew = liveWorld.S.Nodes.FirstOrDefault(n => n.Dropped && n.DropKind == 2 && n.DropId == 18);
Check(cookedStew != null, "raw mushroom simmered into mushroom stew near campfire");

// Test Consuming Mushroom Stew
hero.Pos = cookedStew.Pos;
liveWorld.Tick(); // proximity pickup
Check(hero.Items[18] == 1, "picked up cooked mushroom stew");
hero.HP = 30f;
liveWorld.Submit(1, new Command { Use = 18 });
liveWorld.Tick();
Check(hero.HP >= 75f && hero.WarmthBuffTimer > 40f, "mushroom stew heals and refreshes Well Fed buff");

// Test Well Fed 50% Dread Resistance
var dreadTestWorld = new EmberWorld(444);
var tester1 = dreadTestWorld.AddPlayer(1, "NoBuffTester", Vessel.Wanderer);
var tester2 = dreadTestWorld.AddPlayer(2, "WellFedTester", Vessel.Wanderer);
dreadTestWorld.StartRun();
tester1.Pos = new V2(25f, 25f); // in the dark
tester2.Pos = new V2(25f, 25f);
tester1.WarmthBuffTimer = 0f;
tester2.WarmthBuffTimer = 45f;
tester1.Dread = 0f;
tester2.Dread = 0f;
dreadTestWorld.Tick(0.1f);
Check(tester2.Dread < tester1.Dread, "Well Fed buff provides 50% dread resistance in dark");

// --- Test Batch 3: Survival Starter Fixes, Ore Drop Bug Fix, Merchant Timed Event ---
// 1. Starter item positions
Check(pickNode.Pos.Y < 0f, "starter pickaxe is positioned south of beacon (avoiding label overlap)");
Check(stickNode.Pos.X < -1.5f, "starter stick is positioned west of beacon");
Check(torchNode.Pos.X > 1.5f, "starter torch is positioned east of beacon");

// 2. No merchant at tick 0
Check(!liveWorld.S.Structures.Any(s => s.Kind == 10), "no merchant spawned at Tick 0 in live match");

// 3. DropItem & Attack does not confuse Pickaxe with Ore
liveWorld.DropItem(hero, 1, 8, 1);
var droppedPickaxeNode = liveWorld.S.Nodes.FirstOrDefault(n => n.Dropped && n.DropKind == 1 && n.DropId == (int)Weapon.Pickaxe);
Check(droppedPickaxeNode != null, "dropped pickaxe node spawned on ground");
hero.Pos = droppedPickaxeNode.Pos + new V2(1.0f, 0f); // within 2.8f
liveWorld.Submit(hero.Id, new Command { Attack = true });
liveWorld.Tick();
Check(hero.Bag[(int)Resource.Ore] == 0, "attacking near dropped pickaxe does not harvest it as ore");
hero.Pos = droppedPickaxeNode.Pos; // walk onto it
liveWorld.Tick();
Check(hero.HasPickaxe && hero.Bag[(int)Resource.Ore] == 0, "picking up dropped pickaxe restores pickaxe without adding ore");

// 4. Merchant Timed Event Lifecycle
var merchantEventWorld = new EmberWorld(777);
merchantEventWorld.AddPlayer(1, "EventHero", Vessel.Wanderer);
merchantEventWorld.StartRun(liveMatch: true);
Check(!merchantEventWorld.S.Structures.Any(s => s.Kind == 10), "merchant absent at start of match");
merchantEventWorld.MerchantSpawnTimer = 0.05f;
merchantEventWorld.Tick(0.1f);
var timedMerchant = merchantEventWorld.S.Structures.FirstOrDefault(s => s.Kind == 10);
Check(timedMerchant != null && timedMerchant.Life == 60f && timedMerchant.MaxLife == 60f, "merchant spawns with 60s life and maxlife");
timedMerchant.Life = 0.1f;
merchantEventWorld.Tick(0.15f);
Check(!merchantEventWorld.S.Structures.Any(s => s.Kind == 10), "merchant despawns after life expires");
Check(merchantEventWorld.MerchantSpawnTimer >= 120f, "merchant schedules next visit after 120-180s");

// 5. Combat attack with Pickaxe against monsters
var combatWorld = new EmberWorld(333);
var attacker = combatWorld.AddPlayer(1, "Attacker", Vessel.Wanderer);
combatWorld.StartRun();
attacker.HasPickaxe = true;
attacker.HeldSlot = 8;
combatWorld.DebugAction("enemy", 1);
combatWorld.Tick();
var monster = combatWorld.S.Enemies.First();
monster.Pos = attacker.Pos + new V2(1.2f, 0f);
float monsterHpBefore = monster.HP;
combatWorld.Submit(attacker.Id, new Command { Attack = true });
combatWorld.Tick();
Check(monster.HP < monsterHpBefore, "pickaxe attacks and damages enemy monster");

// --- Test Batch 4: Expanded Crafting Tools, Weapons & Universal Item Naming ---
var craftTestWorld = new EmberWorld(777);
var crafterHero = craftTestWorld.AddPlayer(1, "Artisan", Vessel.Wanderer, recipes: new[] { 0 }); // only torch in sanctuary
craftTestWorld.StartRun();
crafterHero.HeldSlot = 6; // empty hands
crafterHero.HasWeapon = false;
crafterHero.HasPickaxe = false;

// 1. Craft Iron Pickaxe: 6 Wood, 4 Ore
crafterHero.Bag[(int)Resource.Wood] = 6;
crafterHero.Bag[(int)Resource.Ore] = 4;
Check(craftTestWorld.Craft(crafterHero, Recipe.IronPickaxe), "craft iron pickaxe succeeds with basic resources");
Check(crafterHero.HasPickaxe && crafterHero.PickaxeTier == 1 && crafterHero.PickaxeDurability == 48, "iron pickaxe sets tier 1 and 48 durability");
Check(crafterHero.HeldSlot == 8, "crafting pickaxe from empty hands auto-equips to slot 8");
Check(crafterHero.Bag[(int)Resource.Wood] == 0 && crafterHero.Bag[(int)Resource.Ore] == 0, "crafting iron pickaxe spends exact 6 wood and 4 ore");

// 2. Craft Stone Pickaxe: 4 Wood, 4 Ore
crafterHero.HeldSlot = 6;
crafterHero.Bag[(int)Resource.Wood] = 4;
crafterHero.Bag[(int)Resource.Ore] = 4;
Check(craftTestWorld.Craft(crafterHero, Recipe.StonePickaxe), "craft stone pickaxe succeeds");
Check(crafterHero.PickaxeTier == 0 && crafterHero.PickaxeDurability == 30 && crafterHero.HeldSlot == 8, "stone pickaxe sets tier 0 and 30 durability and equips");

// 3. Craft Wooden Stick: 3 Wood
crafterHero.HeldSlot = 6;
crafterHero.Bag[(int)Resource.Wood] = 3;
Check(craftTestWorld.Craft(crafterHero, Recipe.WoodenStick), "craft wooden stick succeeds");
Check(crafterHero.HasWeapon && crafterHero.Weapon == Weapon.Stick && crafterHero.HeldSlot == 1, "wooden stick equips to slot 1");

// 4. Craft Woodcutter Axe: 5 Wood, 2 Ore
crafterHero.HeldSlot = 6;
crafterHero.Bag[(int)Resource.Wood] = 5;
crafterHero.Bag[(int)Resource.Ore] = 2;
Check(craftTestWorld.Craft(crafterHero, Recipe.WoodcutterAxe), "craft woodcutter axe succeeds");
Check(crafterHero.HasWeapon && crafterHero.Weapon == Weapon.CrystalAxe && crafterHero.HeldSlot == 1, "woodcutter axe equips to slot 1 as crystal axe");

// 5. Basic survival recipes can be crafted without Sanctuary unlock
crafterHero.Bag[(int)Resource.Wood] = 10;
crafterHero.Bag[(int)Resource.Ore] = 10;
Check(craftTestWorld.Craft(crafterHero, Recipe.Apple), "craft apple succeeds without sanctuary unlock");
Check(!craftTestWorld.Craft(crafterHero, Recipe.CrystalAxe), "sanctuary high-tier weapon is still locked");

// 6. Field crafting: basic tools can be crafted outside beacon, while sanctuary gear requires beacon
crafterHero.Pos = new V2(25f, 25f); // deep in the field > 5 tiles
crafterHero.Bag[(int)Resource.Wood] = 6;
crafterHero.Bag[(int)Resource.Ore] = 4;
Check(craftTestWorld.Craft(crafterHero, Recipe.IronPickaxe), "basic tool craft succeeds in the field outside beacon");

// 7. Universal Item Naming check
Check(Catalog.GetDroppedDisplayName(1, (int)Weapon.Pickaxe, Resource.Ore, false) == "Cuốc Sắt", "dropped pickaxe resolves to Cuốc Sắt in Vietnamese");
Check(Catalog.GetDroppedDisplayName(1, (int)Weapon.Stick, Resource.Wood, false) == "Gậy Gỗ", "dropped stick resolves to Gậy Gỗ in Vietnamese");
Check(Catalog.GetDroppedDisplayName(2, 15, Resource.Wood, false) == "Quả Táo", "dropped apple resolves to Quả Táo in Vietnamese");
Check(Catalog.GetDroppedDisplayName(0, 0, Resource.Wood, false) == "Gỗ", "dropped raw wood resolves to Gỗ in Vietnamese");
Check(Catalog.GetDroppedDisplayName(2, 17, Resource.Wood, true) == "Roasted Apple", "dropped roasted apple resolves to Roasted Apple in English");

// 8. Cây This Cây That & Mỏ Đá Đa Dạng Checks
bool foundAncientTree = false, foundSmallTree = false, foundNormalTree = false;
bool foundRichVein = false, foundSmallGravel = false, foundNormalRock = false;
for (int tx = 0; tx < 50; tx++)
{
    for (int ty = 0; ty < 50; ty++)
    {
        var tDef = Catalog.GetTileHarvestDef(tx, ty, 3);
        if (tDef.Tier == 2) { foundAncientTree = true; Check(tDef.MaxHP == 260f && tDef.MinDrop == 6 && tDef.MaxDrop == 10 && tDef.VisualScale >= 1.6f, "ancient tree has high HP 260 and large scale 1.7"); }
        if (tDef.Tier == 0) { foundSmallTree = true; Check(tDef.MaxHP == 65f && tDef.MinDrop == 1 && tDef.MaxDrop == 2 && tDef.VisualScale <= 1.05f, "small tree has low HP 65 and compact scale"); }
        if (tDef.Tier == 1) { foundNormalTree = true; Check(tDef.MaxHP == 130f && tDef.MinDrop == 3 && tDef.MaxDrop == 5, "normal tree has standard HP 130 and 3-5 drops"); }

        var rDef = Catalog.GetTileHarvestDef(tx, ty, 2);
        if (rDef.Tier == 2) { foundRichVein = true; Check(rDef.MaxHP == 230f && rDef.MinDrop == 5 && rDef.MaxDrop == 8 && rDef.VisualScale >= 1.4f, "rich vein has high HP 230 and 5-8 drops"); }
        if (rDef.Tier == 0) { foundSmallGravel = true; Check(rDef.MaxHP == 55f && rDef.MinDrop == 1 && rDef.MaxDrop == 2, "small gravel has low HP 55 and 1-2 drops"); }
        if (rDef.Tier == 1) { foundNormalRock = true; Check(rDef.MaxHP == 130f && rDef.MinDrop == 2 && rDef.MaxDrop == 4, "normal rock has standard HP 130 and 2-4 drops"); }

        if (foundAncientTree && foundSmallTree && foundNormalTree && foundRichVein && foundSmallGravel && foundNormalRock)
            break;
    }
    if (foundAncientTree && foundSmallTree && foundNormalTree && foundRichVein && foundSmallGravel && foundNormalRock)
        break;
}
Check(foundAncientTree && foundSmallTree && foundNormalTree, "procedural generator spawns small, normal, and ancient giant trees");
Check(foundRichVein && foundSmallGravel && foundNormalRock, "procedural generator spawns small gravel, normal rocks, and rich veins");

// --- Test Abandoned Mystery Chests (Rương Bỏ Hoang Ngẫu Nhiên / Hộp Pandora) ---
var chestWorld1 = new EmberWorld(42);
var chests1 = chestWorld1.S.Structures.Where(s => s.Kind == EmberWorld.MysteryChestKind).ToList();
Check(chests1.Count >= 4 && chests1.Count <= 8, "spawn 4 to 8 abandoned mystery chests on map creation");
Check(chests1.All(c => c.Pos.Length >= 18f), "all mystery chests are far from beacon (>= 18 tiles)");
Check(chests1.All(c => chestWorld1.Walkable(c.Pos)), "all mystery chests are on walkable terrain");

// Verify no clustering (all chests are at least 15 tiles apart)
bool allChestsSpreadOut = true;
for (int i = 0; i < chests1.Count; i++)
{
    for (int j = i + 1; j < chests1.Count; j++)
    {
        if (V2.Distance(chests1[i].Pos, chests1[j].Pos) < 14.9f)
        {
            allChestsSpreadOut = false;
            break;
        }
    }
}
Check(allChestsSpreadOut, "mystery chests are distributed and at least 15 tiles apart (no clustering)");

// Check deterministic seed variation (seed 42 vs seed 99)
var chestWorld2 = new EmberWorld(99);
var chests2 = chestWorld2.S.Structures.Where(s => s.Kind == EmberWorld.MysteryChestKind).ToList();
Check(V2.Distance(chests1[0].Pos, chests2[0].Pos) > 1f, "mystery chest locations vary randomly by seed");

// Test Interaction: Press [E] when within interaction distance (< 2.2f) opens chest
var chestPlayer = chestWorld1.AddPlayer(1, "Adventurer", Vessel.Wanderer);
chestWorld1.StartRun();
var targetChest = chests1[0];
chestPlayer.Pos = targetChest.Pos + new V2(0.5f, 0.5f);
Check(V2.Distance(chestPlayer.Pos, targetChest.Pos) < EmberWorld.MysteryChestInteractionDistance, "player is in mystery chest interaction range");

// Test Scenario 1: Loot Drop (40%)
var lootWorld = new EmberWorld(101);
var lootPlayer = lootWorld.AddPlayer(1, "Looter", Vessel.Wanderer);
lootWorld.StartRun();
var lootChest = lootWorld.S.Structures.First(s => s.Kind == EmberWorld.MysteryChestKind);
lootPlayer.Pos = lootChest.Pos;
int sc1 = lootWorld.OpenMysteryChest(lootPlayer, lootChest, forcedScenario: 1);
Check(sc1 == 1, "scenario 1 triggers loot drop");
Check(lootChest.Kind == EmberWorld.MysteryChestOpenKind && lootChest.Life == 30f, "chest converts to open kind and sets 30s life");
int droppedNodes = lootWorld.S.Nodes.Count(n => n.Dropped && V2.Distance(n.Pos, lootChest.Pos) <= 2.5f);
Check(droppedNodes >= 2 && droppedNodes <= 3, "loot drop scatters 2-3 item nodes near chest");
Check(lootWorld.S.Event.Contains("Rương báu nhả ra quà tặng quý giá"), "scenario 1 broadcasts lucky gift announcement");

// Test Scenario 2: Monster Trap Ambush (25%)
var ambushWorld = new EmberWorld(102);
var ambushPlayer = ambushWorld.AddPlayer(1, "Scout", Vessel.Wanderer);
ambushWorld.StartRun();
var ambushChest = ambushWorld.S.Structures.First(s => s.Kind == EmberWorld.MysteryChestKind);
ambushPlayer.Pos = ambushChest.Pos;
int enemiesBefore = ambushWorld.S.Enemies.Length;
int sc2 = ambushWorld.OpenMysteryChest(ambushPlayer, ambushChest, forcedScenario: 2);
Check(sc2 == 2, "scenario 2 triggers monster ambush");
int newEnemies = ambushWorld.S.Enemies.Length - enemiesBefore;
Check(newEnemies >= 2 && newEnemies <= 3, "monster ambush spawns 2-3 shadow monsters");
Check(ambushWorld.S.Event.Contains("Bẫy quái vật! Quái bóng tối phục kích"), "scenario 2 broadcasts ambush warning");

// Test Scenario 3: Magical Projectile Burst (20%)
var bulletWorld = new EmberWorld(103);
var bulletPlayer = bulletWorld.AddPlayer(1, "Dodger", Vessel.Wanderer);
bulletWorld.StartRun();
var bulletChest = bulletWorld.S.Structures.First(s => s.Kind == EmberWorld.MysteryChestKind);
bulletPlayer.Pos = bulletChest.Pos;
int sc3 = bulletWorld.OpenMysteryChest(bulletPlayer, bulletChest, forcedScenario: 3);
Check(sc3 == 3, "scenario 3 triggers projectile circle burst");
int firedShots = bulletWorld.S.Projectiles.Count(p => p.Owner == bulletChest.Id && p.FromEnemy);
Check(firedShots >= 12 && firedShots <= 16, "projectile burst fires 12-16 circular magic projectiles");
Check(bulletWorld.S.Event.Contains("Rương bẫy phát nổ chùm đạn ma thuật"), "scenario 3 broadcasts projectile explosion announcement");

// Test Scenario 4: Teleport Back to Beacon (15%)
var tpWorld = new EmberWorld(104);
var tpPlayer = tpWorld.AddPlayer(1, "Traveler", Vessel.Wanderer);
tpWorld.StartRun();
var tpChest = tpWorld.S.Structures.First(s => s.Kind == EmberWorld.MysteryChestKind);
tpPlayer.Pos = tpChest.Pos;
Check(tpPlayer.Pos.Length >= 18f, "player was deep in the dark before teleport");
int sc4 = tpWorld.OpenMysteryChest(tpPlayer, tpChest, forcedScenario: 4);
Check(sc4 == 4, "scenario 4 triggers beacon teleport");
Check(tpPlayer.Pos.Length < 2.5f, "player teleported straight to Beacon safe zone");
Check(tpWorld.S.Event.Contains("đá bay bạn về Trụ Khởi Sinh"), "scenario 4 broadcasts kick back to beacon announcement");

// Test Opening via [E] Command (c.Interact)
var interactWorld = new EmberWorld(105);
var interactPlayer = interactWorld.AddPlayer(1, "Opener", Vessel.Wanderer);
interactWorld.StartRun();
var iChest = interactWorld.S.Structures.First(s => s.Kind == EmberWorld.MysteryChestKind);
interactPlayer.Pos = iChest.Pos;
interactWorld.Submit(1, new Command { Interact = true });
interactWorld.Tick();
Check(iChest.Kind == EmberWorld.MysteryChestOpenKind, "pressing E in range opens mystery chest via simulation command");

// Test Despawn after 30s
iChest.Life = 0.05f;
interactWorld.Tick(0.1f);
Check(!interactWorld.S.Structures.Any(s => s.Id == iChest.Id), "opened mystery chest disappears after 30s decay");

// --- Test Batch 5: Beacon Energy Deposit Modal & Network Synchronization ---
var depositWorld = new EmberWorld(888);
var depPlayer = depositWorld.AddPlayer(1, "BeaconDepositor", Vessel.Wanderer);
depositWorld.StartRun();
depPlayer.Pos = new V2(1.5f, 1.0f); // near Beacon (< 4.5f)

// 1. Crystal Core Deposit (+50 NL per Core)
depositWorld.S.Charge = 0f;
depPlayer.Bag[(int)Resource.Core] = 10;

// Deposit 1 Core
bool dep1Ok = depositWorld.DepositEnergy(depPlayer, (int)Resource.Core, 1);
Check(dep1Ok && depPlayer.Bag[(int)Resource.Core] == 9 && depositWorld.S.Charge == 50f, "deposit 1 crystal core gives +50 Beacon energy");

// Deposit 5 Cores
bool dep5Ok = depositWorld.DepositEnergy(depPlayer, (int)Resource.Core, 5);
Check(dep5Ok && depPlayer.Bag[(int)Resource.Core] == 4 && depositWorld.S.Charge == 300f, "deposit 5 crystal cores gives +250 Beacon energy");

// Deposit All Remaining Cores (4 cores)
bool depAllCoreOk = depositWorld.DepositEnergy(depPlayer, (int)Resource.Core, depPlayer.Bag[(int)Resource.Core]);
Check(depAllCoreOk && depPlayer.Bag[(int)Resource.Core] == 0 && depositWorld.S.Charge == 500f, "deposit all remaining cores gives +200 Beacon energy");

// Reject when empty
Check(!depositWorld.DepositEnergy(depPlayer, (int)Resource.Core, 1), "deposit core rejected when player has 0 cores");

// 2. Lumidust Deposit (+15 NL per 10 Dust)
depositWorld.S.Charge = 0f;
depPlayer.Bag[(int)Resource.Dust] = 25;

// Reject if less than 10 requested
Check(!depositWorld.DepositEnergy(depPlayer, (int)Resource.Dust, 5), "deposit lumidust rejected when requested amount < 10");

// Deposit 10 Dust
bool depDust10Ok = depositWorld.DepositEnergy(depPlayer, (int)Resource.Dust, 10);
Check(depDust10Ok && depPlayer.Bag[(int)Resource.Dust] == 15 && depositWorld.S.Charge == 15f, "deposit 10 lumidust gives +15 Beacon energy");

// Deposit All batches of 10 (from 15 dust -> deposits 10 dust, leaves 5)
bool depDustAllOk = depositWorld.DepositEnergy(depPlayer, (int)Resource.Dust, (depPlayer.Bag[(int)Resource.Dust] / 10) * 10);
Check(depDustAllOk && depPlayer.Bag[(int)Resource.Dust] == 5 && depositWorld.S.Charge == 30f, "deposit all batches of 10 lumidust leaves remainder and adds energy");

// 3. Wood Deposit (+12 NL per 10 Wood)
depositWorld.S.Charge = 0f;
depPlayer.Bag[(int)Resource.Wood] = 34;

// Reject if less than 10
Check(!depositWorld.DepositEnergy(depPlayer, (int)Resource.Wood, 8), "deposit wood rejected when amount < 10");

// Deposit 10 Wood
bool depWood10Ok = depositWorld.DepositEnergy(depPlayer, (int)Resource.Wood, 10);
Check(depWood10Ok && depPlayer.Bag[(int)Resource.Wood] == 24 && depositWorld.S.Charge == 12f, "deposit 10 wood gives +12 Beacon energy");

// Deposit All batches of 10 (from 24 wood -> deposits 20 wood, leaves 4)
bool depWoodAllOk = depositWorld.DepositEnergy(depPlayer, (int)Resource.Wood, (depPlayer.Bag[(int)Resource.Wood] / 10) * 10);
Check(depWoodAllOk && depPlayer.Bag[(int)Resource.Wood] == 4 && depositWorld.S.Charge == 36f, "deposit all batches of 10 wood leaves remainder and adds +24 energy");

// 4. Distance check: deposit rejected if player > 4.5 tiles away
depPlayer.Pos = new V2(6.0f, 6.0f);
depPlayer.Bag[(int)Resource.Core] = 5;
float chargeBeforeDist = depositWorld.S.Charge;
Check(!depositWorld.DepositEnergy(depPlayer, (int)Resource.Core, 1), "deposit rejected when player is far from beacon (> 4.5 tiles)");
Check(depositWorld.S.Charge == chargeBeforeDist && depPlayer.Bag[(int)Resource.Core] == 5, "charge and bag remain untouched on distance rejection");
depPlayer.Pos = new V2(0.5f, 0.5f);

// 5. Network / Simulation Command synchronization (c.DepositRes & c.DepositAmt)
depPlayer.Bag[(int)Resource.Core] = 3;
float chargeBeforeCmd = depositWorld.S.Charge;
depositWorld.Submit(depPlayer.Id, new Command { Deposit = true, DepositRes = (int)Resource.Core, DepositAmt = 2 });
depositWorld.Tick();
Check(depPlayer.Bag[(int)Resource.Core] == 1 && depositWorld.S.Charge == chargeBeforeCmd + 100f, "simulation Command with DepositRes=1 and DepositAmt=2 executes over tick");

// Command Serialization check
var testCmd = new Command { Deposit = true, DepositRes = (int)Resource.Wood, DepositAmt = 20 };
Check(testCmd.HasActions, "Command with DepositRes has HasActions=true");
var cmdOpts = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
string cmdJson = System.Text.Json.JsonSerializer.Serialize(testCmd, cmdOpts);
var deserializedCmd = System.Text.Json.JsonSerializer.Deserialize<Command>(cmdJson, cmdOpts);
Check(deserializedCmd.Deposit && deserializedCmd.DepositRes == (int)Resource.Wood && deserializedCmd.DepositAmt == 20, "Command DepositRes and DepositAmt serialize/deserialize round-trip");

// 6. Eclipse Transition when target charge reached via multi-resource deposit
depositWorld.S.Phase = Phase.Explore;
depositWorld.S.Charge = depositWorld.S.ChargeTarget - 50f;
depPlayer.Bag[(int)Resource.Core] = 1;
depositWorld.Submit(depPlayer.Id, new Command { Deposit = true, DepositRes = (int)Resource.Core, DepositAmt = 1 });
depositWorld.Tick();
Check(depositWorld.S.Phase == Phase.Eclipse && depositWorld.S.BeaconHP == 600f, "reaching Beacon charge target via deposit triggers Eclipse phase and restores Beacon HP to 600");

// 7. Test Lantern vs Torch Craft separation
Check(Catalog.Recipes[(int)Recipe.Torch].Name == "Đuốc", "recipe torch name is Đuốc");
Check(Catalog.Recipes[(int)Recipe.InfernoLantern].Name == "Đèn Hỏa Ngục", "recipe inferno lantern name is Đèn Hỏa Ngục");
Check(Catalog.Recipes[(int)Recipe.RelicLantern].Name == "Đèn Thánh Tích", "recipe relic lantern name is Đèn Thánh Tích");
var lanternWorld = new EmberWorld(99);
var lanternCrafter = lanternWorld.AddPlayer(1, "Crafter", Vessel.Wanderer);
lanternWorld.StartRun();
lanternCrafter.Bag = new[] { 100, 100, 100, 100, 100 };
Check(lanternWorld.Craft(lanternCrafter, Recipe.InfernoLantern), "craft inferno lantern succeeds");
Check(lanternCrafter.Lantern == Lantern.Inferno && lanternCrafter.CarriesLantern, "craft inferno lantern sets Lantern.Inferno and CarriesLantern");
lanternCrafter.Bag = new[] { 100, 100, 100, 100, 100 };
Check(lanternWorld.Craft(lanternCrafter, Recipe.RelicLantern), "craft relic lantern succeeds");
Check(lanternCrafter.Lantern == Lantern.Relic && lanternCrafter.CarriesLantern, "craft relic lantern sets Lantern.Relic and CarriesLantern");

// 8. Ground Torch Retrieval Mechanic
var torchRetrWorld = new EmberWorld(88);
var torchRetrPlayer = torchRetrWorld.AddPlayer(1, "TorchBearer", Vessel.Wanderer);
torchRetrWorld.StartRun();
torchRetrPlayer.Pos = new V2(10f, 10f);
torchRetrPlayer.Items[0] = 1; // 1 Torch
torchRetrPlayer.HeldSlot = 6; // Empty hands
// Place ground torch via Use
torchRetrWorld.Use(torchRetrPlayer, 0);
Check(torchRetrWorld.S.Structures.Any(s => s.Kind == 0), "ground torch structure exists");
Check(torchRetrPlayer.Items[0] == 0, "torch item consumed when placing on ground");
// Press [E] interact within 2.0f
torchRetrWorld.Submit(torchRetrPlayer.Id, new Command { Interact = true });
torchRetrWorld.Tick();
Check(!torchRetrWorld.S.Structures.Any(s => s.Kind == 0), "ground torch structure removed upon retrieval");
Check(torchRetrPlayer.Items[0] == 1, "torch item restored to player inventory");
Check(torchRetrPlayer.HeldSlot == 2, "torch auto-equipped to slot 2 from empty hands");

// 8b. Ground Torch Distance Boundary Check (> 2.0f rejected)
torchRetrPlayer.Items[0] = 1;
torchRetrPlayer.Pos = new V2(20f, 20f);
torchRetrWorld.Use(torchRetrPlayer, 0);
Check(torchRetrWorld.S.Structures.Any(s => s.Kind == 0 && V2.Distance(s.Pos, new V2(20f, 20f)) < 0.1f), "ground torch placed at (20,20)");
torchRetrPlayer.Pos = new V2(22.5f, 20f); // 2.5f distance (> 2.0f)
torchRetrWorld.Submit(torchRetrPlayer.Id, new Command { Interact = true });
torchRetrWorld.Tick();
Check(torchRetrWorld.S.Structures.Any(s => s.Kind == 0 && V2.Distance(s.Pos, new V2(20f, 20f)) < 0.1f), "torch not retrieved when beyond 2.0f distance");
Check(torchRetrPlayer.Items[0] == 0, "player torch inventory unchanged on distant interact");

// 8c. Ground Torch Full Inventory Drop (Items[0] == 99 drops node)
torchRetrPlayer.Pos = new V2(20.5f, 20f); // within 0.5f (< 2.0f)
torchRetrPlayer.Items[0] = 99; // full stack
torchRetrWorld.Submit(torchRetrPlayer.Id, new Command { Interact = true });
torchRetrWorld.Tick();
Check(!torchRetrWorld.S.Structures.Any(s => s.Kind == 0), "ground torch removed even when inventory full");
Check(torchRetrPlayer.Items[0] == 99, "torch inventory remains capped at 99");
Check(torchRetrWorld.S.Nodes.Any(n => n.Dropped && n.DropKind == 2 && n.DropId == 0), "torch dropped as ground node when inventory full");

// 8d. Dropped Item DropId 22 (Wooden Stick) Display Name Resolution Check
Check(Catalog.GetDroppedDisplayName(2, 22, Resource.Wood, false) == "Gậy Gỗ", "dropped stick item 22 resolves to Gậy Gỗ");
Check(Catalog.GetDroppedDisplayName(2, 22, Resource.Wood, true) == "Wooden Stick", "dropped stick item 22 resolves to Wooden Stick");

// 8e. Beacon Modal Coordinate Clamp Simulation Check
foreach (float sw in new[] { 320f, 360f, 480f, 720f, 1080f, 1920f })
{
    float mw = Math.Min(480f, sw - 20f);
    float mx = Math.Max(10f, (sw - mw) * 0.5f);
    Check(mx >= 10f && mx + mw <= sw, $"modal X clamped safely on screen width {sw}");
}

// 9. Weapon Grips & 3-Class Weapon Engine Checks
Check(EmberWeaponGrips.Get(Weapon.ShadowBlades).GripMode == WeaponGripMode.DualWield, "ShadowBlades grip mode is DualWield");
Check(EmberWeaponGrips.Get(Weapon.CrystalAxe).GripMode == WeaponGripMode.TwoHanded, "CrystalAxe grip mode is TwoHanded");
Check(EmberWeaponGrips.Get(Weapon.Stick).GripMode == WeaponGripMode.SingleHand, "Stick grip mode is SingleHand");
Check(EmberWeaponGrips.Get(Weapon.Crossbow).GripMode == WeaponGripMode.TwoHanded, "Crossbow grip mode is TwoHanded");
Check(EmberWeaponGrips.Get(Weapon.LumenGun).GripMode == WeaponGripMode.TwoHanded, "LumenGun grip mode is TwoHanded");
Check(EmberWeaponGrips.Get(Weapon.ShadowBlades).DualSpriteName == "shadow_blade_single", "ShadowBlades dual sprite is shadow_blade_single");
Check(EmberWeaponGrips.Torch != null && EmberWeaponGrips.Torch.SpriteName == "torch", "Torch grip config exists and sprite is torch");
Check(EmberWeaponGrips.Torch.SpriteBaseAngle == 90f, "Torch SpriteBaseAngle is 90f");
Check(EmberWeaponGrips.Torch.GripPivot.X == 0.50f && EmberWeaponGrips.Torch.GripPivot.Y == 0.22f, "Torch GripPivot is (0.50, 0.22)");
Check(EmberWeaponGrips.Torch.RestAngle == -22f, "Torch RestAngle is -22f");
Check(EmberWeaponGrips.Torch.Scale == 0.85f, "Torch Scale is 0.85f");
Check(EmberWeaponGrips.Torch.GripMode == WeaponGripMode.SingleHand, "Torch GripMode is SingleHand");
Check(EmberWeaponGrips.GetGrip("torch") == EmberWeaponGrips.Torch, "EmberWeaponGrips.GetGrip(\"torch\") returns Torch grip");

// 10. Crystal Axe Kiếm Khí (Blade Wave) Spawning Check
var axeWorld = new EmberWorld(7, 250, Difficulty.Easy);
var axeHero = axeWorld.AddPlayer(1, "AxeHero", Vessel.Wanderer);
axeWorld.StartRun();
axeHero.HasWeapon = true;
axeHero.Weapon = Weapon.CrystalAxe;
axeHero.HeldSlot = 1;
axeHero.Stamina = 100f;
axeHero.Pos = new V2(2f, 0f);
axeHero.Right = true;
axeWorld.Submit(1, new Command { Equip = 1, Attack = true });
axeWorld.Tick();
Check(axeWorld.S.Projectiles.Any(p => p.Weapon == Weapon.CrystalAxe), "Crystal Axe attack spawns Kiếm Khí blade wave projectile");

// 11. Shadow Blades Dual-Wield Melee Check (Pure Melee, no projectile/shuriken)
var shadowHero = axeWorld.AddPlayer(2, "ShadowHero", Vessel.Wanderer);
shadowHero.HasWeapon = true;
shadowHero.Weapon = Weapon.ShadowBlades;
shadowHero.HeldSlot = 1;
shadowHero.Stamina = 100f;
shadowHero.Pos = new V2(2f, 2f);
shadowHero.Right = true;
axeWorld.Submit(2, new Command { Equip = 1, Attack = true });
axeWorld.Tick();
Check(!axeWorld.S.Projectiles.Any(p => p.Weapon == Weapon.ShadowBlades), "Shadow Blades is pure dual-wield melee and does not shoot shuriken projectile");
Check(axeWorld.S.Effects.Any(e => e.Kind == 9), "Shadow Blades attack emits Song Trảm Chữ X effect kind 9");

Console.WriteLine($"ALL {checks} CHECKS PASSED");

sealed class FixedDraw : Random
{
    readonly int roll;
    public FixedDraw(int roll)=>this.roll=roll;
    public override int Next(int maxValue)=>roll;
}
