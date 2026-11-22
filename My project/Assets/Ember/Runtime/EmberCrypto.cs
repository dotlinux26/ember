using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Ember
{
    // Portable AES-GCM for Unity's Mono runtime. Uses the platform AES block
    // cipher and standard GHASH; no native plugin or NuGet dependency.
    public static class EmberCrypto
    {
        public static byte[] Random(int length){var b=new byte[length];using(var r=RandomNumberGenerator.Create())r.GetBytes(b);return b;}
        public static byte[] Hkdf(byte[] input,byte[] salt,string context,int length=32)
        {
            byte[] prk;using(var mac=new HMACSHA256(salt))prk=mac.ComputeHash(input);
            var info=Encoding.UTF8.GetBytes(context);byte[] previous=Array.Empty<byte>(),result=new byte[length];int position=0;
            for(byte i=1;position<length;i++){using(var mac=new HMACSHA256(prk))previous=mac.ComputeHash(previous.Concat(info).Concat(new[]{i}).ToArray());int n=Math.Min(previous.Length,length-position);Buffer.BlockCopy(previous,0,result,position,n);position+=n;}
            Array.Clear(prk,0,prk.Length);return result;
        }
        public static byte[] Seal(byte[] key,byte[] nonce,byte[] plain,byte[] aad)
        {
            if(nonce.Length!=12)throw new ArgumentException("GCM uses 96-bit nonces");
            byte[] output=new byte[plain.Length+16];
            using(var gcm=new GcmCipher(key))
                gcm.SealPacket(nonce,0,plain,0,plain.Length,aad,0,aad.Length,output,0);
            return output;
        }
        public static byte[] Open(byte[] key,byte[] nonce,byte[] sealedData,byte[] aad)
        {
            if(nonce.Length!=12||sealedData.Length<16)throw new CryptographicException("Invalid GCM packet");
            byte[] output=new byte[sealedData.Length-16];
            using(var gcm=new GcmCipher(key))
                gcm.OpenPacket(nonce,0,sealedData,0,sealedData.Length,aad,0,aad.Length,output,0);
            return output;
        }
        public sealed class GcmCipher:IDisposable
        {
            static readonly ushort[] Last4=BuildLast4();
            readonly Aes aes;
            readonly ICryptoTransform cipher;
            readonly ulong[] tHi=new ulong[16],tLo=new ulong[16];
            readonly byte[] j0=new byte[16],ctr=new byte[16],stream=new byte[16],ghash=new byte[16];
            bool disposed;
            static ushort[] BuildLast4()
            {
                var table=new ushort[16];
                for(int i=0;i<16;i++)
                {
                    int v=i,r=0;
                    for(int b=0;b<4;b++)
                    {
                        int lsb=v&1;v>>=1;
                        if(lsb!=0)r^=(0xE100>>(3-b));
                    }
                    table[i]=(ushort)r;
                }
                return table;
            }
            public GcmCipher(byte[] key)
            {
                aes=Aes.Create();
                aes.Key=key;aes.Mode=CipherMode.ECB;aes.Padding=PaddingMode.None;
                cipher=aes.CreateEncryptor();
                var zero=new byte[16];var h=new byte[16];
                cipher.TransformBlock(zero,0,16,h,0);
                ulong hHi=Read64(h,0),hLo=Read64(h,8);
                tHi[8]=hHi;tLo[8]=hLo;
                for(int i=4;i>=1;i>>=1)
                {
                    ulong hi=tHi[i*2],lo=tLo[i*2];
                    ulong lsb=lo&1;
                    lo=(lo>>1)|(hi<<63);
                    hi>>=1;
                    if(lsb!=0)hi^=0xE100000000000000UL;
                    tHi[i]=hi;tLo[i]=lo;
                }
                for(int i=2;i<=8;i<<=1)
                    for(int j=1;j<i;j++){tHi[i+j]=tHi[i]^tHi[j];tLo[i+j]=tLo[i]^tLo[j];}
            }
            public int SealPacket(byte[] nonce,int nonceOffset,byte[] plain,int plainOffset,int plainLength,byte[] aad,int aadOffset,int aadLength,byte[] output,int outputOffset)
            {
                Buffer.BlockCopy(nonce,nonceOffset,j0,0,12);
                j0[12]=0;j0[13]=0;j0[14]=0;j0[15]=1;
                Buffer.BlockCopy(j0,0,ctr,0,16);
                for(int offset=0;offset<plainLength;offset+=16)
                {
                    for(int i=15;i>=12;i--)if(++ctr[i]!=0)break;
                    cipher.TransformBlock(ctr,0,16,stream,0);
                    int n=Math.Min(16,plainLength-offset);
                    for(int i=0;i<n;i++)output[outputOffset+offset+i]=(byte)(plain[plainOffset+offset+i]^stream[i]);
                }
                ComputeHash(aad,aadOffset,aadLength,output,outputOffset,plainLength);
                cipher.TransformBlock(j0,0,16,stream,0);
                int tagOff=outputOffset+plainLength;
                for(int i=0;i<16;i++)output[tagOff+i]=(byte)(ghash[i]^stream[i]);
                return plainLength+16;
            }
            public int OpenPacket(byte[] nonce,int nonceOffset,byte[] sealedData,int sealedOffset,int sealedLength,byte[] aad,int aadOffset,int aadLength,byte[] output,int outputOffset)
            {
                if(sealedLength<16)throw new CryptographicException("Invalid GCM packet");
                int cipherLen=sealedLength-16;
                Buffer.BlockCopy(nonce,nonceOffset,j0,0,12);
                j0[12]=0;j0[13]=0;j0[14]=0;j0[15]=1;
                ComputeHash(aad,aadOffset,aadLength,sealedData,sealedOffset,cipherLen);
                cipher.TransformBlock(j0,0,16,stream,0);
                int tagOff=sealedOffset+cipherLen,diff=0;
                for(int i=0;i<16;i++)diff|=(ghash[i]^stream[i])^sealedData[tagOff+i];
                if(diff!=0)throw new CryptographicException("GCM authentication failed");
                Buffer.BlockCopy(j0,0,ctr,0,16);
                for(int offset=0;offset<cipherLen;offset+=16)
                {
                    for(int i=15;i>=12;i--)if(++ctr[i]!=0)break;
                    cipher.TransformBlock(ctr,0,16,stream,0);
                    int n=Math.Min(16,cipherLen-offset);
                    for(int i=0;i<n;i++)output[outputOffset+offset+i]=(byte)(sealedData[sealedOffset+offset+i]^stream[i]);
                }
                return cipherLen;
            }
            void ComputeHash(byte[] aad,int aadOffset,int aadLength,byte[] cipherText,int cipherOffset,int cipherLength)
            {
                Array.Clear(ghash,0,16);
                AbsorbBlocks(aad,aadOffset,aadLength);
                AbsorbBlocks(cipherText,cipherOffset,cipherLength);
                ulong aBits=(ulong)aadLength*8,cBits=(ulong)cipherLength*8;
                for(int i=0;i<8;i++){ghash[i]^=(byte)(aBits>>(56-i*8));ghash[8+i]^=(byte)(cBits>>(56-i*8));}
                MultiplyInPlace();
            }
            void AbsorbBlocks(byte[] data,int offset,int length)
            {
                for(int pos=0;pos<length;pos+=16)
                {
                    int n=Math.Min(16,length-pos);
                    for(int i=0;i<n;i++)ghash[i]^=data[offset+pos+i];
                    MultiplyInPlace();
                }
            }
            void MultiplyInPlace()
            {
                ulong zHi=0,zLo=0;
                for(int b=15;b>=0;b--)
                {
                    byte x=ghash[b];
                    int lo=x&0xF;
                    int rem=(int)(zLo&0xF);
                    zLo=(zLo>>4)|(zHi<<60);
                    zHi=(zHi>>4)^((ulong)Last4[rem]<<48);
                    zHi^=tHi[lo];zLo^=tLo[lo];
                    int hi=x>>4;
                    rem=(int)(zLo&0xF);
                    zLo=(zLo>>4)|(zHi<<60);
                    zHi=(zHi>>4)^((ulong)Last4[rem]<<48);
                    zHi^=tHi[hi];zLo^=tLo[hi];
                }
                Write64(ghash,0,zHi);Write64(ghash,8,zLo);
            }
            public void Dispose()
            {
                if(disposed)return;disposed=true;
                cipher?.Dispose();aes?.Dispose();
                Array.Clear(tHi,0,tHi.Length);Array.Clear(tLo,0,tLo.Length);
                Array.Clear(j0,0,16);Array.Clear(ctr,0,16);Array.Clear(stream,0,16);Array.Clear(ghash,0,16);
            }
        }
        public static void Write64(byte[] b,int offset,ulong value){for(int i=7;i>=0;i--){b[offset+i]=(byte)value;value>>=8;}}
        public static ulong Read64(byte[] b,int offset){ulong n=0;for(int i=0;i<8;i++)n=(n<<8)|b[offset+i];return n;}
    }
}
