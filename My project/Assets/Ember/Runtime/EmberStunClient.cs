using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Ember
{
    // Endpoint discovery only. Keep this socket open for subsequent punching;
    // querying with a throwaway socket cannot discover the game's mapped port.
    public static class EmberStunClient
    {
        const uint Cookie=0x2112A442;
        public static byte[] Request(out byte[] transaction)
        {
            transaction=new byte[12];using(var random=RandomNumberGenerator.Create())random.GetBytes(transaction);
            var data=new byte[20];data[0]=0;data[1]=1;data[4]=0x21;data[5]=0x12;data[6]=0xa4;data[7]=0x42;Buffer.BlockCopy(transaction,0,data,8,12);return data;
        }
        static int U16(byte[] b,int i)=>(b[i]<<8)|b[i+1];
        public static IPEndPoint Parse(byte[] data,byte[] transaction)
        {
            if(data==null||data.Length<20||transaction==null||transaction.Length!=12||U16(data,0)!=0x101||U16(data,2)%4!=0||U16(data,2)+20>data.Length||data[4]!=0x21||data[5]!=0x12||data[6]!=0xa4||data[7]!=0x42)throw new FormatException("Invalid STUN response");
            for(int i=0;i<12;i++)if(data[8+i]!=transaction[i])throw new FormatException("STUN transaction mismatch");
            int end=20+U16(data,2);
            for(int offset=20;offset+4<=end;)
            {
                int type=U16(data,offset),length=U16(data,offset+2),start=offset+4;if(start+length>end)throw new FormatException("Truncated STUN attribute");
                if(type==0x20)
                {
                    if(length<8)throw new FormatException("Truncated mapped endpoint");int family=data[start+1],bytes=family==1?4:family==2?16:0;
                    if(bytes==0||length!=bytes+4)throw new FormatException("Unknown STUN address family");
                    int port=U16(data,start+2)^(int)(Cookie>>16);byte[] address=new byte[bytes],mask=new byte[16]{0x21,0x12,0xa4,0x42,0,0,0,0,0,0,0,0,0,0,0,0};Buffer.BlockCopy(transaction,0,mask,4,12);
                    for(int i=0;i<bytes;i++)address[i]=(byte)(data[start+4+i]^mask[i]);return new IPEndPoint(new IPAddress(address),port);
                }
                offset=start+((length+3)&~3);
            }
            throw new FormatException("No XOR-MAPPED-ADDRESS");
        }
        public static async Task<IPEndPoint> Discover(UdpClient socket,IPEndPoint server,int timeoutMs=1500)
        {
            byte[] request=Request(out var transaction);await socket.SendAsync(request,request.Length,server);
            // Caller owns the socket and must not run another receive loop during discovery.
            var receive=socket.ReceiveAsync();if(await Task.WhenAny(receive,Task.Delay(timeoutMs))!=receive){socket.Close();throw new TimeoutException("STUN timed out; socket closed to cancel pending receive");}
            var packet=await receive;if(!packet.RemoteEndPoint.Equals(server))throw new FormatException("Unexpected STUN sender");return Parse(packet.Buffer,transaction);
        }
    }
}
