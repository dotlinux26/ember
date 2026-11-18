using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

string address=args.FirstOrDefault()??"127.0.0.1";int count=args.Length>1?Math.Clamp(int.Parse(args[1]),1,3000):2000;
using var client=new UdpClient();client.Connect(IPAddress.Parse(address),27842);var clock=Stopwatch.StartNew();
void Log(string kind,object data)=>Console.WriteLine(JsonSerializer.Serialize(new{utc=DateTime.UtcNow.ToString("O"),kind,data}));
async Task<string[]> Exchange(string command)
{
    await client.SendAsync(Encoding.UTF8.GetBytes(command));using var timeout=new CancellationTokenSource(3000);var response=await client.ReceiveAsync(timeout.Token);return Encoding.UTF8.GetString(response.Buffer).Split('|');
}
try
{
    string prefix=Convert.ToHexString(RandomNumberGenerator.GetBytes(1));string? firstCode=null,firstNonce=null,firstProof=null;
    for(int i=0;i<count;i++)
    {
        string code=prefix+i.ToString("X4"),nonce=Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));byte[] proof=RandomNumberGenerator.GetBytes(32);string key=Convert.ToBase64String(proof);
        var reply=await Exchange($"CREATE|{code}|{key}|4|{nonce}");
        if(reply.Length!=9||reply[0]!="ROOM"||reply[1]!=nonce||reply[2]!=code)throw new Exception("Unexpected broker response while creating room "+i);
        using var hmac=new HMACSHA256(proof);if(!CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(reply[8]),hmac.ComputeHash(Encoding.UTF8.GetBytes(string.Join('|',reply.Take(8))))))throw new Exception("Invalid room signature");
        if(i==0){firstCode=code;firstNonce=nonce;firstProof=key;}
        if(i%100==99)await Task.Delay(40); // Stay below the per-IP limit.
    }
    Log("broker.load.pass",new{rooms=count,createSeconds=clock.Elapsed.TotalSeconds});
    for(int i=0;i<17;i++)
    {
        string nonce=Convert.ToHexString(RandomNumberGenerator.GetBytes(16));var response=await Exchange("PING|Ember|"+nonce);
        if(response.Length!=3||response[0]!="PONG"||response[1]!=nonce)throw new Exception("Liveness ping failed");await Task.Delay(1000);
    }
    var expired=await Exchange($"JOINROOM|{firstCode}|{firstProof}|4|{firstNonce}");
    if(expired.Length!=3||expired[0]!="ROOMERROR")throw new Exception("Expired room remained joinable");
    Log("broker.expiry.pass",new{rooms=count,elapsedSeconds=clock.Elapsed.TotalSeconds});
}
catch(Exception ex){Log("broker.check.fail",new{error=ex.Message});Environment.ExitCode=1;}
