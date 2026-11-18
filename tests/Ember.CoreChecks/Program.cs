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
var nearestNode=mineWorld.S.Nodes.OrderBy(n=>V2.Distance(n.Pos,miner.Pos)).First();
miner.Pos=nearestNode.Pos-new V2(1,0);
int bagBefore=miner.Load;
mineWorld.Submit(1,new Command{Equip=8,Attack=true});
mineWorld.Tick();
Check(miner.Load>bagBefore&&miner.HarvestCD>0&&mineWorld.S.Effects.Any(e=>e.Kind==7),"pickaxe slot mines nearby resource nodes with harvest swing and chip FX");
miner.HeldSlot=1;miner.HarvestCD=0;
var nextNode=mineWorld.S.Nodes.OrderBy(n=>V2.Distance(n.Pos,miner.Pos)).First();
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
Console.WriteLine($"ALL {checks} CHECKS PASSED");

sealed class FixedDraw : Random
{
    readonly int roll;
    public FixedDraw(int roll)=>this.roll=roll;
    public override int Next(int maxValue)=>roll;
}
