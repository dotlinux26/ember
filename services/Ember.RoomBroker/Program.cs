using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

int port=int.TryParse(Environment.GetEnvironmentVariable("EMBER_BROKER_PORT"),out int configured)?configured:5454;
if(args.Contains("--version")){Console.WriteLine(JsonSerializer.Serialize(new{version="0.3.0-test",protocol=3,runtime=RuntimeInformation.FrameworkDescription,architecture=RuntimeInformation.ProcessArchitecture.ToString()}));return;}
if(port<1||port>65535)throw new ArgumentOutOfRangeException("EMBER_BROKER_PORT");
using var socket=new UdpClient(port);var rooms=new Dictionary<string,Room>();var limits=new Dictionary<string,(double since,int count)>();
using var shutdown=new CancellationTokenSource();var clock=Stopwatch.StartNew();
Console.CancelKeyPress+=(_,eventArgs)=>{eventArgs.Cancel=true;shutdown.Cancel();};
using var termination=OperatingSystem.IsWindows()?null:PosixSignalRegistration.Create(PosixSignal.SIGTERM,context=>{context.Cancel=true;shutdown.Cancel();});
using var interrupt=OperatingSystem.IsWindows()?null:PosixSignalRegistration.Create(PosixSignal.SIGINT,context=>{context.Cancel=true;shutdown.Cancel();});
bool verbose=Environment.GetEnvironmentVariable("EMBER_LOG_LEVEL")=="debug";
long rx=0,rejected=0,forwarded=0,sendErrors=0;double nextSweep=0,nextHealth=0;
void Log(string kind,object data)=>Console.WriteLine(JsonSerializer.Serialize(new{utc=DateTime.UtcNow.ToString("O"),kind,data}));
Log("broker.start",new{port,protocol=3,pid=Environment.ProcessId,runtime=RuntimeInformation.FrameworkDescription,architecture=RuntimeInformation.ProcessArchitecture.ToString(),maxRooms=4096,memberTtlSeconds=15,verbose});
Task<UdpReceiveResult>? receive=null;
while(!shutdown.IsCancellationRequested)
{
    UdpReceiveResult received;
    try
    {
        double upkeep=clock.Elapsed.TotalSeconds;
        if(upkeep>=nextSweep)
        {
            nextSweep=upkeep+1;
            foreach(var code in rooms.Where(r=>upkeep-r.Value.Host.Seen>15).Select(r=>r.Key).ToArray()){rooms.Remove(code);Log("room.expired",new{room=code});}
            foreach(var room in rooms.Values)room.Guests.RemoveAll(g=>upkeep-g.Seen>15);
            foreach(var old in limits.Where(x=>upkeep-x.Value.since>20).Select(x=>x.Key).ToArray())limits.Remove(old);
        }
        if(upkeep>=nextHealth){nextHealth=upkeep+15;Log("broker.health",new{uptimeSeconds=(int)upkeep,rooms=rooms.Count,members=rooms.Values.Sum(r=>1+r.Guests.Count),rx,rejected,forwarded,sendErrors,workingSet=Environment.WorkingSet});}
        receive??=socket.ReceiveAsync(shutdown.Token).AsTask();
        if(await Task.WhenAny(receive,Task.Delay(1000,shutdown.Token))!=receive)continue;
        received=await receive;receive=null;rx++;
    }
    catch(OperationCanceledException){break;}
    catch(SocketException ex){receive=null;Log("udp.receive_error",new{error=ex.SocketErrorCode.ToString()});continue;}
    var from=received.RemoteEndPoint;double now=clock.Elapsed.TotalSeconds;
    if(received.Buffer.Length>2400){rejected++;continue;}
    string ip=from.Address.ToString();if(!limits.TryGetValue(ip,out var rate)||now-rate.since>1)rate=(now,0);if(rate.count++>=4000){rejected++;continue;}
    if(!limits.ContainsKey(ip)&&limits.Count>=10000)
    {
        foreach(var old in limits.Where(x=>now-x.Value.since>2).Select(x=>x.Key).ToArray())limits.Remove(old);
        if(limits.Count>=10000){rejected++;continue;}
    }
    limits[ip]=rate;
    try
    {
        if(received.Buffer.Length>=56&&received.Buffer[0]==0xEB&&received.Buffer[1]==0x52)
        {
            var buf=received.Buffer;
            string bCode=Encoding.ASCII.GetString(buf,2,6);
            if(!rooms.TryGetValue(bCode,out var bRoom)||!bRoom.Relay){rejected++;continue;}
            Member? bSource=bRoom.Host.Address.Equals(from)?bRoom.Host:null;
            if(bSource==null){for(int i=0;i<bRoom.Guests.Count;i++)if(bRoom.Guests[i].Address.Equals(from)){bSource=bRoom.Guests[i];break;}}
            if(bSource==null){rejected++;continue;}
            int pLen=buf[22]|(buf[23]<<8);
            if(pLen<33||pLen>1400||buf.Length!=24+pLen+32){rejected++;continue;}
            ulong bSeq=0;for(int i=7;i>=0;i--)bSeq=(bSeq<<8)|buf[14+i];
            if(bSource.Sequences.Contains(bSeq)||(bSource.Highest>1024&&bSeq<bSource.Highest-1024)){rejected++;continue;}
            byte[] expectedMac=bSource.TokenHmac.ComputeHash(buf,0,24+pLen);
            if(!CryptographicOperations.FixedTimeEquals(expectedMac,buf.AsSpan(24+pLen,32))){rejected++;continue;}
            bSource.Seen=now;bSource.SupportsBinary=true;
            bSource.Highest=Math.Max(bSource.Highest,bSeq);bSource.Sequences.Add(bSeq);bSource.Sequences.RemoveWhere(n=>bSource.Highest>1024&&n<bSource.Highest-1024);
            bool bcast=(buf[8]|buf[9]|buf[10]|buf[11]|buf[12]|buf[13])==0;
            uint targetIp=(uint)(buf[8]|(buf[9]<<8)|(buf[10]<<16)|(buf[11]<<24));
            int targetPort=buf[12]|(buf[13]<<8);
            void ForwardTo(Member target)
            {
                if(target.SupportsBinary&&from.AddressFamily==AddressFamily.InterNetwork)
                {
                    var fwd=new byte[8+pLen];
                    fwd[0]=0xEB;fwd[1]=0x53;
                    from.Address.TryWriteBytes(fwd.AsSpan(2,4),out _);
                    fwd[6]=(byte)from.Port;fwd[7]=(byte)(from.Port>>8);
                    Buffer.BlockCopy(buf,24,fwd,8,pLen);
                    SendNonBlocking(fwd,target.Address);
                }
                else
                {
                    string fwdText="FORWARD|"+from+"|"+Convert.ToBase64String(buf,24,pLen);
                    SendNonBlocking(Encoding.UTF8.GetBytes(fwdText),target.Address);
                }
                forwarded++;
            }
            if(bSource==bRoom.Host)
            {
                for(int i=0;i<bRoom.Guests.Count;i++)
                {
                    var g=bRoom.Guests[i];
                    if(bcast||(g.Address.Port==targetPort&&g.Address.Address.Equals(new IPAddress(targetIp))))ForwardTo(g);
                }
            }
            else ForwardTo(bRoom.Host);
            continue;
        }
        var parts=Encoding.UTF8.GetString(received.Buffer).Split('|');if(parts.Length<3)continue;string operation=parts[0],code=parts[1];
        if(operation=="PING"&&code=="Ember"&&parts.Length==3&&parts[2].Length==32&&parts[2].All(Uri.IsHexDigit)){await Send("PONG|"+parts[2]+"|3",from);continue;}
        if(operation=="CREATE"||operation=="JOINROOM")
        {
            if((parts.Length!=5&&parts.Length!=6)||code.Length!=6||code.Any(c=>!char.IsAsciiLetterOrDigit(c))||!int.TryParse(parts[3],out int capacity)||capacity<1||capacity>4)continue;
            byte[] proof=Convert.FromBase64String(parts[2]);if(proof.Length!=32||Convert.FromBase64String(parts[4]).Length!=16)continue;
            if(operation=="CREATE")
            {
                if(!rooms.TryGetValue(code,out var room))
                {
                    if(rooms.Count>=4096){await Error("Máy ghép phòng đang đầy.",parts[4],from);continue;}
                    room=new Room{Proof=proof,Capacity=capacity,Host=new Member(from,parts[4],now)};rooms[code]=room;Log("room.created",new{room=code,capacity});
                }
                else if(!room.Host.Address.Equals(from)||!CryptographicOperations.FixedTimeEquals(room.Proof,proof)){await Error("Mã phòng đã tồn tại.",parts[4],from);continue;}
                room.Host.Seen=now;if(parts.Length==6)room.Host.Candidate=Candidate(parts[5],from);await Publish(code,room);continue;
            }
            else
            {
                if(!rooms.TryGetValue(code,out var room)){await Error("Không tìm thấy phòng.",parts[4],from);continue;}
                if(!CryptographicOperations.FixedTimeEquals(room.Proof,proof)){await Error("Sai mật khẩu phòng.",parts[4],from);continue;}
                var member=room.Guests.Find(g=>g.Address.Equals(from));
                if(member==null){if(room.Guests.Count>=room.Capacity-1){await Error("Phòng đã đủ người.",parts[4],from);continue;}member=new Member(from,parts[4],now);room.Guests.Add(member);Log("room.joined",new{room=code,members=room.Guests.Count+1});}
                member.Seen=now;if(parts.Length==6)member.Candidate=Candidate(parts[5],from);await Publish(code,room);continue;
            }
        }
        if(!rooms.TryGetValue(code,out var active))continue;
        Member? source=(active.Host.Address.Equals(from)&&active.Host.Token==parts[2])?active.Host:null;
        if(source==null){for(int i=0;i<active.Guests.Count;i++)if(active.Guests[i].Address.Equals(from)&&active.Guests[i].Token==parts[2]){source=active.Guests[i];break;}}
        if(source==null)continue;source.Seen=now;
        if(operation=="KEEP"){if(parts.Length==5)source.Candidate=Candidate(parts[4],from);await Publish(code,active);continue;}
        if(operation=="FALLBACK"){if(!active.Relay)Log("room.relay",new{room=code,members=active.Guests.Count+1});active.Relay=true;await Publish(code,active);continue;}
        if(operation=="ROOMLEAVE")
        {
            if(source==active.Host)rooms.Remove(code);else active.Guests.Remove(source);Log("room.left",new{room=code,host=source==active.Host});continue;
        }
        if(operation=="DATA"&&parts.Length==7&&active.Relay)
        {
            if(!ulong.TryParse(parts[4],out var seq)||source.Sequences.Contains(seq)||(source.Highest>1024&&seq<source.Highest-1024)){rejected++;continue;}
            byte[] signature=Convert.FromBase64String(parts[6]),expected=source.TokenHmac.ComputeHash(Encoding.UTF8.GetBytes(string.Join('|',parts,0,6)));
            if(!CryptographicOperations.FixedTimeEquals(signature,expected)){rejected++;continue;}
            var packet=Convert.FromBase64String(parts[5]);if(packet.Length>1400||packet.Length<33){rejected++;continue;}
            source.Highest=Math.Max(source.Highest,seq);source.Sequences.Add(seq);source.Sequences.RemoveWhere(n=>source.Highest>1024&&n<source.Highest-1024);
            byte[]? forwardTextBytes=null;
            void ForwardLegacy(Member target)
            {
                if(target.SupportsBinary&&from.AddressFamily==AddressFamily.InterNetwork)
                {
                    var fwd=new byte[8+packet.Length];
                    fwd[0]=0xEB;fwd[1]=0x53;
                    from.Address.TryWriteBytes(fwd.AsSpan(2,4),out _);
                    fwd[6]=(byte)from.Port;fwd[7]=(byte)(from.Port>>8);
                    Buffer.BlockCopy(packet,0,fwd,8,packet.Length);
                    SendNonBlocking(fwd,target.Address);
                }
                else
                {
                    forwardTextBytes??=Encoding.UTF8.GetBytes("FORWARD|"+from+"|"+parts[5]);
                    SendNonBlocking(forwardTextBytes,target.Address);
                }
                forwarded++;
            }
            if(source==active.Host)
            {
                bool all=parts[3]=="*";
                for(int i=0;i<active.Guests.Count;i++){var g=active.Guests[i];if(all||g.Address.ToString()==parts[3])ForwardLegacy(g);}
            }
            else ForwardLegacy(active.Host);
        }
    }
    catch(FormatException){rejected++;}catch(SocketException){sendErrors++;}catch(ArgumentException){rejected++;}
}
Log("broker.stopped",new{rx,rejected,forwarded});
async Task Publish(string code,Room room)
{
    var members=new[]{room.Host}.Concat(room.Guests).ToArray();
    foreach(var member in members)
    {
        string peerList=string.Join(',',members.Where(m=>m!=member).SelectMany(m=>new[]{m.Address}.Concat(m.Candidate==null?Array.Empty<IPEndPoint>():new[]{m.Candidate!})).Distinct().Select(p=>p.ToString()));
        string peers=peerList.Length>0?peerList+",bin":"bin";
        string body="ROOM|"+member.Nonce+"|"+code+"|"+member.Token+"|"+room.Host.Address+"|"+peers+"|"+(room.Relay?1:0)+"|"+room.Capacity;
        using var hmac=new HMACSHA256(room.Proof);string tag=Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));await Send(body+"|"+tag,member.Address);
    }
}
async Task Error(string message,string nonce,IPEndPoint endpoint){rejected++;if(verbose)Log("request.rejected",new{reason=message});await Send("ROOMERROR|"+nonce+"|"+message,endpoint);}
async Task Send(string text,IPEndPoint endpoint){try{byte[] bytes=Encoding.UTF8.GetBytes(text);await socket.SendAsync(bytes,endpoint);}catch(SocketException ex){sendErrors++;if(verbose)Log("udp.send_error",new{error=ex.SocketErrorCode.ToString()});}}
void SendNonBlocking(byte[] bytes,IPEndPoint endpoint){_ = SendBytesAsync(bytes,endpoint);}
async Task SendBytesAsync(byte[] bytes,IPEndPoint endpoint){try{await socket.SendAsync(bytes,endpoint);}catch(SocketException ex){sendErrors++;if(verbose)Log("udp.send_error",new{error=ex.SocketErrorCode.ToString()});}}
static IPEndPoint? Candidate(string text,IPEndPoint observed)=>IPEndPoint.TryParse(text,out var endpoint)&&endpoint.Port>0&&endpoint.Address.Equals(observed.Address)?endpoint:null;
sealed class Room{public required byte[] Proof;public int Capacity;public bool Relay;public required Member Host;public List<Member> Guests=new();}
sealed class Member
{
    public IPEndPoint Address;public IPEndPoint? Candidate;public string Token,Nonce;public double Seen;public ulong Highest;public HashSet<ulong> Sequences=new();public HMACSHA256 TokenHmac;public bool SupportsBinary;
    public Member(IPEndPoint address,string nonce,double seen){Address=address;Nonce=nonce;Seen=seen;var rawToken=RandomNumberGenerator.GetBytes(32);Token=Convert.ToBase64String(rawToken);TokenHmac=new HMACSHA256(rawToken);}
}
