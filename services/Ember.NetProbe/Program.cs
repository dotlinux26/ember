using Ember;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Net;
using System.Text.Json;

string address=args.FirstOrDefault(a=>!a.StartsWith("--"))??"127.0.0.1";
string Arg(string key,string fallback)=>args.FirstOrDefault(a=>a.StartsWith("--"+key+"="))?.Split('=',2)[1]??fallback;
int seconds=Math.Clamp(int.Parse(Arg("seconds","3")),1,3600),cycles=Math.Clamp(int.Parse(Arg("cycles","1")),1,1000),loss=Math.Clamp(int.Parse(Arg("loss","0")),0,50),port=int.Parse(Arg("port","27842"));
string mode=Arg("mode",args.Contains("--relay")?"relay":"auto");
if(!new[]{"auto","direct","relay"}.Contains(mode))throw new ArgumentException("--mode=auto|direct|relay");
RouteMode routeMode=mode=="direct"?RouteMode.DirectOnly:mode=="relay"?RouteMode.RelayOnly:RouteMode.Auto;
void Log(string category,string message)=>Console.WriteLine(JsonSerializer.Serialize(new{utc=DateTime.UtcNow.ToString("O"),category,message}));
Log("probe.start",$"broker={address}:{port} mode={mode} cycles={cycles} hold={seconds}s loss={loss}%");
try
{
    for(int cycle=1;cycle<=cycles;cycle++)
    {
        string code=new string(RandomNumberGenerator.GetBytes(6).Select(b=>"ABCDEFGHJKLMNPQRSTUVWXYZ23456789"[b%32]).ToArray());string password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
        using var host=new EmberLan{Capacity=4,Mode=routeMode,DebugDropPercent=loss,Trace=Log};var guests=new List<EmberLan>();int joined=0,actions=0;var snapshots=new int[3];double[] lastSnapshot=new double[3];
        string difficultPayload=Convert.ToBase64String(RandomNumberGenerator.GetBytes(16000));
        var clock=Stopwatch.StartNew();double nextJoin=0,nextState=0,nextAction=0,readyAt=-1;int sequence=0,sentActions=0;
        host.Joined=(id,s)=>joined++;host.Input=(id,s)=>{if(s.StartsWith("probe-action:"))actions++;};
        try
        {
            host.OpenPublic(true,address,code,password,port,useStun:false);
            for(int i=0;i<3;i++){int slot=i;var guest=new EmberLan{Mode=routeMode,DebugDropPercent=loss,Trace=Log};guest.State=s=>{if(s.StartsWith("probe-encrypted-state:")){snapshots[slot]++;lastSnapshot[slot]=clock.Elapsed.TotalSeconds;}};guest.OpenPublic(false,address,code,password,port,useStun:false);guests.Add(guest);}
            while(clock.Elapsed.TotalSeconds<seconds+25)
            {
                double t=clock.Elapsed.TotalSeconds;host.Poll(t);foreach(var guest in guests)guest.Poll(t);
                if(t>=nextJoin){foreach(var guest in guests)if(guest.LocalId==0)guest.Join("Probe");nextJoin=t+.4;}
                if(joined==3&&guests.All(g=>g.LocalId>0))
                {
                    if(readyAt<0){readyAt=t;Log("probe.connected",$"cycle={cycle} players=4 route={host.RouteName}");}
                    if(t>=nextState){host.Broadcast("probe-encrypted-state:"+(++sequence)+":"+difficultPayload,sequence);nextState=t+.1;}
                    if(t>=nextAction&&t-readyAt<seconds){foreach(var guest in guests){guest.SendAction("probe-action:"+sentActions);sentActions++;}nextAction=t+.5;}
                    if(t-readyAt>=seconds&&actions==sentActions&&guests.All(g=>g.ReliablePending==0)&&snapshots.All(n=>n>0)&&lastSnapshot.All(s=>t-s<2))break;
                }
                Thread.Sleep(10);
            }
            bool modeMatches=routeMode==RouteMode.Auto||guests.All(g=>g.RelayActive==(routeMode==RouteMode.RelayOnly))&&host.RelayActive==(routeMode==RouteMode.RelayOnly);
            bool pass=joined==3&&actions==sentActions&&sentActions>0&&snapshots.All(n=>n>0)&&modeMatches&&guests.All(g=>g.ReliablePending==0);
            Log(pass?"probe.pass":"probe.fail",$"cycle={cycle} players={joined+1} actions={actions}/{sentActions} snapshots={string.Join(',',snapshots)} route={host.RouteName} retries={guests.Sum(g=>g.Retries)} repairs={host.SnapshotRepairs} rejected={host.RejectedPackets}");
            if(!pass)throw new Exception("Probe failed. Check UDP port, route mode, broker and client logs.");
            foreach(bool wrong in new[]{true,false})
            {
                using var extra=new EmberLan{Mode=routeMode};extra.OpenPublic(false,address,code,wrong?password+"-wrong":password,port,useStun:false);
                double until=clock.Elapsed.TotalSeconds+3;
                while(clock.Elapsed.TotalSeconds<until&&extra.Error==null){extra.Poll(clock.Elapsed.TotalSeconds);host.Poll(clock.Elapsed.TotalSeconds);foreach(var guest in guests)guest.Poll(clock.Elapsed.TotalSeconds);Thread.Sleep(10);}
                if(extra.Error==null||extra.LocalId!=0)throw new Exception("Broker did not reject "+(wrong?"wrong password":"fifth member"));
                Log("probe.rejection.pass",wrong?"wrong password rejected":"fifth member rejected");
            }
            using(var noise=new UdpClient())for(int n=0;n<20;n++){byte[] data=RandomNumberGenerator.GetBytes(n*131);noise.Send(data,data.Length,new IPEndPoint(IPAddress.Parse(address),port));}
        }
        finally{foreach(var guest in guests){try{guest.Leave();}finally{guest.Dispose();}}host.Leave();}
        Thread.Sleep(100);
    }
    Log("probe.complete",$"PASS cycles={cycles} mode={mode}");
}
catch(Exception ex){Log("probe.failed",ex.Message);Environment.ExitCode=1;}
