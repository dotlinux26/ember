using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ember
{
    // LAN/VPN transport, host-authoritative input. Network packets are bounded
    // and authenticated; session secret must be shared with invited players.
    // Snapshot chunks can arrive unordered; only complete newer frames publish.
    public sealed class EmberLan:IDisposable
    {
        public const int Port=27841;
        public bool Host;public int LocalId=1;public string Error;public int Capacity=4;public bool AcceptingJoins=true;public double LastStateTime;
        public int PingMs{get;private set;}=-1;
        int pingSeq;double nextPingAt,pingSentAt;
        UdpClient udp;IPEndPoint server;byte[] secret,sessionKey,epoch;ulong packetCounter;string handshake;
        readonly HashSet<ulong> hostPackets=new HashSet<ulong>();ulong hostHighest;
        EmberRoomRoute route;bool broadcasting;string previousRouteError;
        Task<IPAddress[]> stunDns;IPEndPoint stunServer;byte[] stunTransaction,stunRequest;double stunRetry;int stunAttempts,stunService;double openedAt=-1;
        public IPEndPoint PublicEndpoint;
        public bool RelayActive=>route?.Relay??false;public string RoomCode=>route?.Code;
        public bool BrokerReady=>route==null||(route.Ready&&route.Error==null);
        public RouteMode Mode;public Action<string,string> Trace;
        public long SentPackets,ReceivedPackets,SentBytes,ReceivedBytes,RejectedPackets,Retries,DroppedPackets,SnapshotRepairs;
        public int DebugDropPercent;public string RouteName=>route==null?"LAN/VPN":RelayActive?"RELAY":"P2P";
        public int PeerCount=>peers.Count;public int ReliablePending=>outgoing.Count;
        readonly Dictionary<string,Peer> peers=new Dictionary<string,Peer>();
        readonly Dictionary<long,Assembly> pending=new Dictionary<long,Assembly>();
        readonly Dictionary<long,string[]> recentStates=new Dictionary<long,string[]>();
        readonly Dictionary<int,Outbound> outgoing=new Dictionary<int,Outbound>();
        sealed class Outbound{public string Type,Payload;public double Next;public int Tries;}
        int nextPlayer=2,sequence;long accepted=-1;double now;
        public Action<int,string> Joined;public Action<int> Left;public Action<int,string> Input;public Action<int,Command> CommandInput;public Action<string> State;public Action<byte[]> BinaryState;public Action<int,bool> ReadyChanged;
        public Func<string,bool> CanRejoin;
        sealed class Peer{public int Id;public IPEndPoint Address;public string Handshake;public double Seen,RepairAt;public int RepairBudget;public int Sequence=-1,HighestAction=-1;public HashSet<int> Actions=new HashSet<int>();public HashSet<ulong> Packets=new HashSet<ulong>();public ulong HighestPacket;}
        sealed class Assembly{public byte[][] Parts;public double Since,NextRepair;}
        public void Open(bool host,string address,string password)
        {
            Dispose();Host=host;LocalId=host?1:0;Error=null;PingMs=host?0:-1;pingSeq=0;nextPingAt=0;pingSentAt=0;Capacity=Math.Max(1,Math.Min(4,Capacity));AcceptingJoins=true;LastStateTime=0;using(var sha=SHA256.Create())secret=sha.ComputeHash(Encoding.UTF8.GetBytes("Ember LAN v2:"+password));
            udp=new UdpClient(host?Port:0);udp.Client.Blocking=false;udp.Client.ReceiveBufferSize=1024*1024;
            try{udp.Client.IOControl(-1744830452,new byte[]{0,0,0,0},null);}catch{}
            epoch=host?EmberCrypto.Random(16):null;sessionKey=host?EmberCrypto.Hkdf(secret,epoch,"Ember AES-GCM session v2"):null;
            handshake=Convert.ToBase64String(EmberCrypto.Random(16));
            if(!host)server=new IPEndPoint(IPAddress.Parse(address),Port);
            SentPackets=ReceivedPackets=SentBytes=ReceivedBytes=RejectedPackets=Retries=DroppedPackets=SnapshotRepairs=0;
            Trace?.Invoke("transport.open","host="+host+" capacity="+Capacity+" mode="+Mode);
        }
        public void Poll(double time)
        {
            if(udp==null)return;now=time;
            if(openedAt<0)openedAt=now;
            if(route!=null){route.Poll(time,Host||(LocalId>0&&time-LastStateTime<3));if(route.Ready&&!Host)server=route.Server;if(route.Error!=null)Error=route.Error;else if(Error==previousRouteError)Error=null;previousRouteError=route.Error;}
            if(stunDns!=null&&stunDns.IsCompleted)
            {
                if(stunDns.Status==TaskStatus.RanToCompletion){var ip=stunDns.Result.FirstOrDefault(a=>a.AddressFamily==AddressFamily.InterNetwork);if(ip!=null){stunServer=new IPEndPoint(ip,stunService==0?19302:3478);stunRequest=EmberStunClient.Request(out stunTransaction);stunRetry=now;}}
                if(stunRequest==null&&stunService==0){stunService=1;stunDns=Dns.GetHostAddressesAsync("stun.cloudflare.com");Trace?.Invoke("stun.fallback","Google DNS unavailable");}else stunDns=null;
            }
            if(stunRequest!=null&&PublicEndpoint==null&&stunAttempts<3&&now>=stunRetry){udp.Send(stunRequest,stunRequest.Length,stunServer);stunRetry=now+.5*Math.Pow(2,stunAttempts++);}
            if(stunRequest!=null&&PublicEndpoint==null&&stunAttempts>=3&&now>=stunRetry&&stunService==0){stunService=1;stunAttempts=0;stunRequest=null;stunDns=Dns.GetHostAddressesAsync("stun.cloudflare.com");}
            for(int count=0;count<250&&udp.Available>0;count++)
            {
                try
                {
                    IPEndPoint from=new IPEndPoint(IPAddress.Any,0);byte[] packet=udp.Receive(ref from);
                    ReceivedPackets++;ReceivedBytes+=packet.Length;
                    if(stunServer!=null&&from.Equals(stunServer)){try{PublicEndpoint=EmberStunClient.Parse(packet,stunTransaction);route?.SetPublicEndpoint(PublicEndpoint);Trace?.Invoke("stun.mapped","public endpoint discovered");}catch(FormatException){RejectedPackets++;}continue;}
                    if(route!=null&&route.Handle(ref packet,ref from,now))continue;
                    if(packet.Length<33||packet.Length>2048){RejectedPackets++;continue;}bool encrypted=packet[0]==69&&packet[1]==66&&packet[2]==50;byte[] body;
                    if(encrypted){body=Decrypt(packet,from);if(body==null)continue;}
                    else {body=packet.Take(packet.Length-32).ToArray();byte[] tag;using(var hmac=new HMACSHA256(secret))tag=hmac.ComputeHash(body);int diff=0;for(int i=0;i<32;i++)diff|=tag[i]^packet[body.Length+i];if(diff!=0)continue;}
                    if(body.Length>=2&&body[0]==0xEB)
                    {
                        if(Host)ReceiveHostBinary(from,body);
                        else if(from.Equals(server))ReceiveClientBinary(body);
                        continue;
                    }
                    string text=Encoding.UTF8.GetString(body);var split=text.Split(new[]{'|'},3);if(split.Length<3)continue;
                    if(!encrypted&&split[0]!="JOIN"&&split[0]!="ID"&&split[0]!="REJECT")continue;
                    if(Host)ReceiveHost(from,split);else if(from.Equals(server))ReceiveClient(split);
                }
                catch(SocketException ex){if(ex.SocketErrorCode!=SocketError.WouldBlock&&ex.SocketErrorCode!=SocketError.ConnectionReset&&ex.SocketErrorCode!=SocketError.Interrupted)Error=ex.Message;}
                catch(CryptographicException){RejectedPackets++;}
                catch(Exception ex){Error="Rejected packet: "+ex.Message;}
            }
            if(Host)foreach(var key in peers.Where(p=>now-p.Value.Seen>10).Select(p=>p.Key).ToArray()){Trace?.Invoke("peer.timeout","id="+peers[key].Id);Left?.Invoke(peers[key].Id);peers.Remove(key);}
            foreach(var key in pending.Where(p=>now-p.Value.Since>2).Select(p=>p.Key).ToArray())pending.Remove(key);
            if(!Host&&LocalId>0)foreach(var item in pending.OrderBy(p=>p.Value.Since).Take(2).ToArray())
            {
                var assembly=item.Value;if(now<assembly.NextRepair)continue;assembly.NextRepair=now+.15;
                var missing=Enumerable.Range(0,assembly.Parts.Length).Where(i=>assembly.Parts[i]==null).Take(64).ToArray();
                if(missing.Length>0)Send(server,"NACK|"+item.Key+"|"+string.Join(",",missing));
            }
            if(!Host)foreach(var item in outgoing.ToArray())if(now>=item.Value.Next)
            {
                var m=item.Value;if(m.Tries++>30){Error="Không nhận được xác nhận từ host.";outgoing.Remove(item.Key);continue;}
                Retries++;if(m.Tries==3||m.Tries==10)Trace?.Invoke("action.retry","sequence="+item.Key+" tries="+m.Tries);Send(server,m.Type+"|"+item.Key+"|"+m.Payload);m.Next=now+.15;
            }
            if(!Host&&LocalId>0&&now>=nextPingAt)
            {
                pingSeq++;pingSentAt=now;nextPingAt=now+1.0;
                Send(server,"PING|"+pingSeq+"|0");
            }
            if(!Host&&now-openedAt>12&&(LocalId==0||now-LastStateTime>12)&&Error==null)Error="Mất kết nối host. Hãy kiểm tra địa chỉ và mật khẩu phòng.";
        }
        void ReceiveHostBinary(IPEndPoint from,byte[] body)
        {
            string key=from.ToString();
            if(body[1]==0x02&&peers.TryGetValue(key,out var p)&&body.Length>=16)
            {
                int seq=body[2]|(body[3]<<8)|(body[4]<<16)|(body[5]<<24);
                if(seq>p.Sequence)
                {
                    p.Sequence=seq;p.Seen=now;
                    float x=BitConverter.ToSingle(body,6);
                    float y=BitConverter.ToSingle(body,10);
                    ushort act=BitConverter.ToUInt16(body,14);
                    var cmd=new Command{X=x,Y=y};
                    if((act&(1<<0))!=0)cmd.Attack=true;
                    if((act&(1<<1))!=0)cmd.Skill=true;
                    if((act&(1<<2))!=0)cmd.Dash=true;
                    if((act&(1<<3))!=0)cmd.Interact=true;
                    if((act&(1<<4))!=0)cmd.Refuel=true;
                    if((act&(1<<5))!=0)cmd.Deposit=true;
                    if((act&(1<<6))!=0)cmd.ToggleLamp=true;
                    if((act&(1<<7))!=0)cmd.PassLamp=true;
                    if((act&(1<<8))!=0)cmd.Push=true;
                    if((act&(1<<9))!=0)cmd.Revive=true;
                    if((act&(1<<10))!=0)cmd.Reload=true;
                    if(CommandInput!=null)CommandInput(p.Id,cmd);
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
                    else Input?.Invoke(p.Id,UnityEngine.JsonUtility.ToJson(cmd));
#endif
                }
            }
        }
        void ReceiveClientBinary(byte[] body)
        {
            if(body[1]==0x01)
            {
                if(body.Length>=6)
                {
                    int serverTick=BitConverter.ToInt32(body,2);
                    if(serverTick>accepted)accepted=serverTick;
                }
                LastStateTime=now;
                BinaryState?.Invoke(body);
            }
        }
        void ReceiveHost(IPEndPoint from,string[] message)
        {
            string key=from.ToString();
            if(message[0]=="NACK"&&peers.TryGetValue(key,out var repairPeer)&&long.TryParse(message[1],out long frame)&&recentStates.TryGetValue(frame,out var chunks))
            {
                if(now-repairPeer.RepairAt>=1){repairPeer.RepairAt=now;repairPeer.RepairBudget=0;}
                foreach(var index in message[2].Split(',').Take(64))if(int.TryParse(index,out int part)&&part>=0&&part<chunks.Length&&repairPeer.RepairBudget++<256){Send(repairPeer.Address,chunks[part]);SnapshotRepairs++;}
                return;
            }
            if(message[0]=="JOIN")
            {
                int separator=message[2].IndexOf('.');if(separator<1)return;string challenge=message[2].Substring(0,separator),identity=message[2].Substring(separator+1);if(Convert.FromBase64String(challenge).Length!=16)return;
                if(!peers.TryGetValue(key,out var peer))
                {
                    bool rejoining=!AcceptingJoins&&CanRejoin!=null&&CanRejoin(identity);
                    if(!AcceptingJoins&&!rejoining){Send(from,"REJECT|0|STARTED");return;}
                    if(!rejoining&&peers.Count>=Capacity-1){Send(from,"REJECT|0|FULL");return;}
                    peer=new Peer{Id=nextPlayer++,Address=from,Seen=now,Handshake=challenge};
                    try{Joined?.Invoke(peer.Id,identity);}catch{Send(from,"REJECT|0|INVALID");return;}peers[key]=peer;Trace?.Invoke(rejoining?"peer.rejoin":"peer.join","id="+peer.Id);
                }
                if(peer.Handshake!=challenge)return;peer.Seen=now;Send(from,"ID|0|"+peer.Id+","+Convert.ToBase64String(epoch)+","+challenge);return;
            }
            if(message[0]=="INPUT"&&peers.TryGetValue(key,out var p)&&int.TryParse(message[1],out int seq)&&seq>p.Sequence)
            {p.Sequence=seq;p.Seen=now;Input?.Invoke(p.Id,message[2]);}
            if(message[0]=="PING"&&peers.TryGetValue(key,out var pingPeer)){pingPeer.Seen=now;Send(from,"PONG|"+message[1]+"|"+message[2]);return;}
            if(message[0]=="LEAVE"&&peers.TryGetValue(key,out var leaving)){peers.Remove(key);Left?.Invoke(leaving.Id);Trace?.Invoke("peer.leave","id="+leaving.Id);return;}
            if((message[0]=="ACTION"||message[0]=="READY")&&peers.TryGetValue(key,out var actor)&&int.TryParse(message[1],out int action)&&action>=0)
            {
                actor.Seen=now;Send(from,"ACK|"+action+"|ok");
                if(action<=actor.HighestAction-256||!actor.Actions.Add(action))return;
                actor.HighestAction=Math.Max(actor.HighestAction,action);actor.Actions.RemoveWhere(n=>n<actor.HighestAction-256);
                if(message[0]=="ACTION")Input?.Invoke(actor.Id,message[2]);else ReadyChanged?.Invoke(actor.Id,message[2]=="1");
            }
        }
        void ReceiveClient(string[] m)
        {
            if(m[0]=="ID")
            {
                var idParts=m[2].Split(',');if(idParts.Length!=3||idParts[2]!=handshake||!int.TryParse(idParts[0],out int id)||id<2)return;
                var nextEpoch=Convert.FromBase64String(idParts[1]);if(nextEpoch.Length!=16)return;
                if(epoch!=null&&!epoch.SequenceEqual(nextEpoch))return;bool fresh=LocalId==0;epoch=nextEpoch;LocalId=id;sessionKey=EmberCrypto.Hkdf(secret,epoch,"Ember AES-GCM session v2");if(fresh)Trace?.Invoke("peer.assigned","id="+id);return;
            }
            if(m[0]=="REJECT"){Error=m[2]=="FULL"?"Phòng đã đủ người.":m[2]=="STARTED"?"Trận đã bắt đầu; hãy chờ phòng mới.":"Yêu cầu vào phòng không hợp lệ.";return;}
            if(m[0]=="PONG"){if(int.TryParse(m[1],out int seq)&&seq==pingSeq){int rtt=(int)Math.Round(Math.Max(0,(now-pingSentAt)*1000.0));PingMs=PingMs<0?rtt:(int)Math.Round(PingMs*.65+rtt*.35);}return;}
            if(m[0]=="ACK"){if(int.TryParse(m[1],out int ack))outgoing.Remove(ack);return;}
            if(m[0]=="CLOSED"){Error="Host đã đóng phòng.";LocalId=0;return;}
            if(m[0]!="STATE"||!long.TryParse(m[1],out long tick)||tick<=accepted)return;
            var split=m[2].Split(new[]{','},3);if(split.Length<3||!int.TryParse(split[0],out int index)||!int.TryParse(split[1],out int count)||count<1||count>1024||index<0||index>=count)return;
            if(!pending.TryGetValue(tick,out var assembly)){if(pending.Count>=8)return;assembly=new Assembly{Parts=new byte[count][],Since=now,NextRepair=now+.12};pending[tick]=assembly;}
            if(assembly.Parts.Length!=count)return;assembly.Parts[index]=Convert.FromBase64String(split[2]);
            if(assembly.Parts.Any(p=>p==null))return;
            var compressed=assembly.Parts.SelectMany(p=>p).ToArray();using(var input=new MemoryStream(compressed))using(var unzip=new DeflateStream(input,CompressionMode.Decompress))using(var output=new MemoryStream())
            {
                byte[] buffer=new byte[4096];int n;while((n=unzip.Read(buffer,0,buffer.Length))>0){output.Write(buffer,0,n);if(output.Length>4*1024*1024)throw new InvalidDataException("Snapshot too large");}
                State?.Invoke(Encoding.UTF8.GetString(output.ToArray()));accepted=tick;LastStateTime=now;
            }
            foreach(var key in pending.Keys.Where(k=>k<=tick).ToArray())pending.Remove(key);
        }
        public void OpenPublic(bool host,string brokerAddress,string roomCode,string password,int brokerPort=EmberRoomRoute.BrokerPort,bool useStun=true)
        {
            string cleanHost=brokerAddress;
            int port=brokerPort;
            if(!string.IsNullOrEmpty(brokerAddress)&&brokerAddress.Contains(":"))
            {
                var parts=brokerAddress.Split(':');
                cleanHost=parts[0];
                if(int.TryParse(parts[1],out int p))port=p;
            }
            Open(host,cleanHost,roomCode.ToUpperInvariant()+":"+password);
            route=new EmberRoomRoute(udp,host,new IPEndPoint(IPAddress.Parse(cleanHost),port),roomCode,password,Capacity){Mode=Mode,Trace=Trace};
            if(useStun)stunDns=Dns.GetHostAddressesAsync("stun.l.google.com");
        }
        public void Join(string identity){if(!Host&&udp!=null&&(route==null||route.Ready))Send(server,"JOIN|0|"+handshake+"."+identity);}
        public void SendInput(string input){if(!Host&&udp!=null&&LocalId>0)Send(server,"INPUT|"+(++sequence)+"|"+input);}
        public void SendInputBinary(uint seq,Command cmd)
        {
            if(Host||udp==null||LocalId<=0||cmd==null)return;
            byte[] b=new byte[16];
            b[0]=0xEB;b[1]=0x02;
            b[2]=(byte)seq;b[3]=(byte)(seq>>8);b[4]=(byte)(seq>>16);b[5]=(byte)(seq>>24);
            Buffer.BlockCopy(BitConverter.GetBytes(cmd.X),0,b,6,4);
            Buffer.BlockCopy(BitConverter.GetBytes(cmd.Y),0,b,10,4);
            ushort act=0;
            if(cmd.Attack)act|=(1<<0);
            if(cmd.Skill)act|=(1<<1);
            if(cmd.Dash)act|=(1<<2);
            if(cmd.Interact)act|=(1<<3);
            if(cmd.Refuel)act|=(1<<4);
            if(cmd.Deposit)act|=(1<<5);
            if(cmd.ToggleLamp)act|=(1<<6);
            if(cmd.PassLamp)act|=(1<<7);
            if(cmd.Push)act|=(1<<8);
            if(cmd.Revive)act|=(1<<9);
            if(cmd.Reload)act|=(1<<10);
            b[14]=(byte)act;b[15]=(byte)(act>>8);
            SendRaw(server,b);
        }
        public void SendAction(string input)=>Reliable("ACTION",input);
        public void SetReady(bool ready)=>Reliable("READY",ready?"1":"0");
        void Reliable(string type,string payload)
        {
            if(Host||udp==null||LocalId<=0||outgoing.Count>=64)return;int id=++sequence;outgoing[id]=new Outbound{Type=type,Payload=payload,Next=now+.15,Tries=1};Send(server,type+"|"+id+"|"+payload);
        }
        public void Leave()
        {
            if(udp!=null){if(Host)foreach(var p in peers.Values)Send(p.Address,"CLOSED|0|bye");else if(LocalId>0)Send(server,"LEAVE|0|bye");route?.Close();}Dispose();
        }
        public void BroadcastSnapshot(byte[] data)
        {
            if(!Host||udp==null||peers.Count==0||data==null)return;
            var destinations=route?.Relay==true?peers.Values.Take(1):peers.Values;
            broadcasting=route?.Relay==true;
            try
            {
                foreach(var p in destinations)
                {
                    if(data.Length>=10)
                    {
                        uint ack=(uint)Math.Max(0,p.Sequence);
                        data[6]=(byte)ack;
                        data[7]=(byte)(ack>>8);
                        data[8]=(byte)(ack>>16);
                        data[9]=(byte)(ack>>24);
                    }
                    SendRaw(p.Address,data);
                }
            }
            finally{broadcasting=false;}
        }
        public void Broadcast(string json,int tick)
        {
            if(!Host||udp==null||peers.Count==0)return;byte[] data;
            using(var ms=new MemoryStream()){using(var zip=new DeflateStream(ms,System.IO.Compression.CompressionLevel.Fastest,true)){var raw=Encoding.UTF8.GetBytes(json);zip.Write(raw,0,raw.Length);}data=ms.ToArray();}
            const int chunk=850;int count=(data.Length+chunk-1)/chunk;
            if(count>1024)throw new InvalidDataException("Compressed snapshot exceeds transport limit");
            var messages=new string[count];for(int i=0;i<count;i++)messages[i]="STATE|"+tick+"|"+i+","+count+","+Convert.ToBase64String(data,i*chunk,Math.Min(chunk,data.Length-i*chunk));
            recentStates[tick]=messages;foreach(var old in recentStates.Keys.OrderByDescending(k=>k).Skip(20).ToArray())recentStates.Remove(old);
            var destinations=route?.Relay==true?peers.Values.Take(1):peers.Values;
            broadcasting=route?.Relay==true;
            try{foreach(var p in destinations)foreach(string message in messages)Send(p.Address,message);}finally{broadcasting=false;}
        }
        public void SendRaw(IPEndPoint to,byte[] body)
        {
            if(to==null||body==null||sessionKey==null)return;
            var header=new byte[31];header[0]=69;header[1]=66;header[2]=50;Buffer.BlockCopy(epoch,0,header,3,16);
            int id=Host?1:LocalId;header[19]=(byte)(id>>24);header[20]=(byte)(id>>16);header[21]=(byte)(id>>8);header[22]=(byte)id;
            EmberCrypto.Write64(header,23,++packetCounter);
            var nonce=header.Skip(19).Take(12).ToArray();
            byte[] packet=header.Concat(EmberCrypto.Seal(sessionKey,nonce,body,header)).ToArray();
            SentPackets++;SentBytes+=packet.Length;
            if(DebugDropPercent>0&&(SentPackets*37)%100<Math.Min(90,DebugDropPercent)){DroppedPackets++;return;}
            if(route!=null)route.Send(to,packet,broadcasting);else udp.Send(packet,packet.Length,to);
        }
        void Send(IPEndPoint to,string text)
        {
            if(to==null)return;
            byte[] body=Encoding.UTF8.GetBytes(text);
            if(text.StartsWith("JOIN|")||text.StartsWith("ID|")||text.StartsWith("REJECT|"))
            {byte[] tag;using(var hmac=new HMACSHA256(secret))tag=hmac.ComputeHash(body);byte[] packet=body.Concat(tag).ToArray();SentPackets++;SentBytes+=packet.Length;if(DebugDropPercent>0&&(SentPackets*37)%100<Math.Min(90,DebugDropPercent)){DroppedPackets++;return;}if(route!=null)route.Send(to,packet,broadcasting);else udp.Send(packet,packet.Length,to);}
            else SendRaw(to,body);
        }
        byte[] Decrypt(byte[] packet,IPEndPoint from)
        {
            if(packet.Length<47||sessionKey==null)return null;for(int i=0;i<16;i++)if(packet[3+i]!=epoch[i])return null;
            int sender=(packet[19]<<24)|(packet[20]<<16)|(packet[21]<<8)|packet[22];var counter=EmberCrypto.Read64(packet,23);HashSet<ulong> seen;ulong highest;Peer peer=null;
            if(Host){if(!peers.TryGetValue(from.ToString(),out peer)||peer.Id!=sender)return null;seen=peer.Packets;highest=peer.HighestPacket;}
            else {if(!from.Equals(server)||sender!=1)return null;seen=hostPackets;highest=hostHighest;}
            if(seen.Contains(counter)||(highest>1024&&counter<highest-1024))return null;
            var header=packet.Take(31).ToArray();var plain=EmberCrypto.Open(sessionKey,header.Skip(19).Take(12).ToArray(),packet.Skip(31).ToArray(),header);
            highest=Math.Max(highest,counter);seen.Add(counter);seen.RemoveWhere(n=>highest>1024&&n<highest-1024);if(peer!=null)peer.HighestPacket=highest;else hostHighest=highest;return plain;
        }
        public void Dispose(){udp?.Dispose();udp=null;route=null;stunDns=null;stunServer=null;stunRequest=stunTransaction=null;stunAttempts=stunService=0;openedAt=-1;PublicEndpoint=null;PingMs=-1;pingSeq=0;nextPingAt=0;pingSentAt=0;peers.Clear();pending.Clear();recentStates.Clear();outgoing.Clear();hostPackets.Clear();hostHighest=packetCounter=0;sessionKey=null;epoch=null;accepted=-1;sequence=0;nextPlayer=2;now=0;}
    }
}
