using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Ember
{
    public enum RouteMode { Auto, DirectOnly, RelayOnly }
    // Room broker and relay routing share the gameplay UDP socket. The broker
    // handles endpoints and opaque encrypted packets; it never receives the game key.
    public sealed class EmberRoomRoute
    {
        public const int BrokerPort = 5454;
        public bool Host,Ready,Relay;public string Code,Error,Token;public IPEndPoint Server,Broker;
        public RouteMode Mode;public Action<string,string> Trace;
        public readonly List<IPEndPoint> Candidates=new List<IPEndPoint>();
        readonly UdpClient socket;readonly byte[] proof;readonly string nonce;readonly int capacity;
        readonly HMACSHA256 proofHmac;HMACSHA256 tokenHmac;string cachedToken;bool brokerSupportsBinary;
        readonly byte[] relayBuf=new byte[2048],ip4Scratch=new byte[4];
        double nextControl,nextFallback,joinedAt,lastReply,startedAt=-1;bool fallbackRequested;string registration,publicCandidate="";ulong relaySequence;
        public EmberRoomRoute(UdpClient socket,bool host,IPEndPoint broker,string code,string password,int capacity)
        {
            this.socket=socket;Host=host;Broker=broker;Code=code.ToUpperInvariant();this.capacity=capacity;
            using(var sha=SHA256.Create())proof=sha.ComputeHash(Encoding.UTF8.GetBytes("Ember broker access:"+Code+":"+password));
            proofHmac=new HMACSHA256(proof);
            nonce=Convert.ToBase64String(EmberCrypto.Random(16));registration=(host?"CREATE":"JOINROOM")+"|"+Code+"|"+Convert.ToBase64String(proof)+"|"+capacity+"|"+nonce;
        }
        public void Poll(double now,bool connected)
        {
            if(startedAt<0)startedAt=now;
            if(!Ready&&now-startedAt>10)Error="Không thể kết nối broker. Kiểm tra IP và cổng UDP "+(Broker!=null?Broker.Port:5454)+".";
            if(Ready&&now-lastReply>8){Ready=false;Token=null;nextControl=nextFallback=0;fallbackRequested=false;}
            if(now>=nextControl)
            {
                SendControl(Ready?"KEEP|"+Code+"|"+Token+"|"+nonce+"|"+publicCandidate:registration+(publicCandidate.Length>0?"|"+publicCandidate:""));nextControl=now+(Ready?2:1);
                foreach(var endpoint in Candidates){var punch=Encoding.ASCII.GetBytes("EMBER-PUNCH-v2-keep-binding-open");socket.Send(punch,punch.Length,endpoint);}
            }
            if(Ready&&!Relay&&now>=nextFallback&&(Mode==RouteMode.RelayOnly||(!Host&&Mode==RouteMode.Auto&&!connected&&now-joinedAt>=3)))
            {if(!fallbackRequested)Trace?.Invoke("route.fallback",Mode==RouteMode.RelayOnly?"forced":"direct timeout after 3s");fallbackRequested=true;nextFallback=now+1;SendControl("FALLBACK|"+Code+"|"+Token+"|"+nonce);}
        }
        void SendControl(string message){var bytes=Encoding.UTF8.GetBytes(message);socket.Send(bytes,bytes.Length,Broker);}
        public void SetPublicEndpoint(IPEndPoint endpoint){publicCandidate=endpoint.ToString();nextControl=0;Trace?.Invoke("stun.candidate","discovered endpoint registered with broker");}
        public bool Handle(ref byte[] packet,ref IPEndPoint from,double now)
        {
            if(!from.Equals(Broker))return false;
            if(packet.Length>=8&&packet[0]==0xEB&&packet[1]==0x53)
            {
                Buffer.BlockCopy(packet,2,ip4Scratch,0,4);
                int port=packet[6]|(packet[7]<<8);
                from=new IPEndPoint(new IPAddress(ip4Scratch),port);
                var payload=new byte[packet.Length-8];
                Buffer.BlockCopy(packet,8,payload,0,payload.Length);
                packet=payload;
                return false;
            }
            string text=Encoding.UTF8.GetString(packet);var parts=text.Split('|');
            if(parts.Length>=3&&parts[0]=="FORWARD")
            {
                if(!TryEndpoint(parts[1],out var source))return true;
                try{packet=Convert.FromBase64String(parts[2]);from=source;return false;}catch{return true;}
            }
            if(parts.Length>=3&&parts[0]=="ROOMERROR"&&parts[1]==nonce){Error=parts[2];return true;}
            if(parts.Length!=9||parts[0]!="ROOM"||parts[1]!=nonce||parts[2]!=Code)return true;
            string signed=string.Join("|",parts,0,8);byte[] tag=proofHmac.ComputeHash(Encoding.UTF8.GetBytes(signed));
            byte[] received;try{received=Convert.FromBase64String(parts[8]);}catch{return true;}if(received.Length!=tag.Length)return true;int diff=0;for(int i=0;i<tag.Length;i++)diff|=tag[i]^received[i];if(diff!=0)return true;
            if(!TryEndpoint(parts[4],out var hostEndpoint))return true;
            bool wasReady=Ready,oldRelay=Relay;Server=hostEndpoint;Token=parts[3];
            if(Token!=cachedToken){cachedToken=Token;tokenHmac?.Dispose();tokenHmac=new HMACSHA256(Convert.FromBase64String(Token));}
            Relay=parts[6]=="1";Candidates.Clear();
            foreach(var address in parts[5].Split(','))
            {
                if(address=="bin"){brokerSupportsBinary=true;continue;}
                if(TryEndpoint(address,out var endpoint)&&!Candidates.Contains(endpoint))Candidates.Add(endpoint);
            }
            if(!Ready)joinedAt=now;lastReply=now;Ready=true;Error=Mode==RouteMode.DirectOnly&&Relay?"Room uses relay; direct-only mode cannot join. Use Auto or Relay.":null;
            if(!wasReady||oldRelay!=Relay)Trace?.Invoke("route.ready","route="+(Relay?"relay":"direct")+" mode="+Mode+" peers="+Candidates.Count);
            return true;
        }
        public void Send(IPEndPoint to,byte[] packet,bool broadcast=false)=>Send(to,packet,packet!=null?packet.Length:0,broadcast);
        public void Send(IPEndPoint to,byte[] packet,int length,bool broadcast=false)
        {
            if(!Ready||(Mode==RouteMode.DirectOnly&&Relay)||(Mode==RouteMode.RelayOnly&&!Relay))return;
            if(!Relay){socket.Send(packet,length,to);return;}
            if(brokerSupportsBinary&&tokenHmac!=null&&Code.Length==6&&24+length+32<=relayBuf.Length&&(broadcast||to.AddressFamily==AddressFamily.InterNetwork))
            {
                relayBuf[0]=0xEB;relayBuf[1]=0x52;
                for(int i=0;i<6;i++)relayBuf[2+i]=(byte)Code[i];
                if(broadcast){relayBuf[8]=relayBuf[9]=relayBuf[10]=relayBuf[11]=0;relayBuf[12]=relayBuf[13]=0;}
                else
                {
                    var addr=to.Address.GetAddressBytes();
                    Buffer.BlockCopy(addr,0,relayBuf,8,4);
                    int port=to.Port;
                    relayBuf[12]=(byte)port;relayBuf[13]=(byte)(port>>8);
                }
                ulong seq=++relaySequence;
                for(int i=0;i<8;i++){relayBuf[14+i]=(byte)seq;seq>>=8;}
                relayBuf[22]=(byte)length;relayBuf[23]=(byte)(length>>8);
                Buffer.BlockCopy(packet,0,relayBuf,24,length);
                byte[] hmacTag=tokenHmac.ComputeHash(relayBuf,0,24+length);
                Buffer.BlockCopy(hmacTag,0,relayBuf,24+length,32);
                socket.Send(relayBuf,24+length+32,Broker);
                return;
            }
            string body="DATA|"+Code+"|"+Token+"|"+(broadcast?"*":to.ToString())+"|"+(++relaySequence)+"|"+Convert.ToBase64String(packet,0,length);
            var mac=tokenHmac??new HMACSHA256(Convert.FromBase64String(Token));
            byte[] tag=mac.ComputeHash(Encoding.UTF8.GetBytes(body));
            SendControl(body+"|"+Convert.ToBase64String(tag));
        }
        public void Close(){if(Ready)SendControl("ROOMLEAVE|"+Code+"|"+Token+"|"+nonce);}
        public static bool TryEndpoint(string value,out IPEndPoint endpoint)
        {
            endpoint=null;int colon=value.LastIndexOf(':');if(colon<1||!IPAddress.TryParse(value.Substring(0,colon),out var ip)||!int.TryParse(value.Substring(colon+1),out int port)||port<1||port>65535)return false;endpoint=new IPEndPoint(ip,port);return true;
        }
    }
}
