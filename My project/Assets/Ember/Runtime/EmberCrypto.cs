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
            using(var aes=Aes.Create())
            {
                aes.Key=key;aes.Mode=CipherMode.ECB;aes.Padding=PaddingMode.None;
                using(var cipher=aes.CreateEncryptor())
                {
                    var h=Block(cipher,new byte[16]);var j=new byte[16];Buffer.BlockCopy(nonce,0,j,0,12);j[15]=1;
                    byte[] encrypted=Ctr(cipher,j,plain),tag=Hash(h,aad,encrypted),mask=Block(cipher,j);
                    for(int i=0;i<16;i++)tag[i]^=mask[i];return encrypted.Concat(tag).ToArray();
                }
            }
        }
        public static byte[] Open(byte[] key,byte[] nonce,byte[] sealedData,byte[] aad)
        {
            if(nonce.Length!=12||sealedData.Length<16)throw new CryptographicException("Invalid GCM packet");
            using(var aes=Aes.Create())
            {
                aes.Key=key;aes.Mode=CipherMode.ECB;aes.Padding=PaddingMode.None;
                using(var cipher=aes.CreateEncryptor())
                {
                    var encrypted=sealedData.Take(sealedData.Length-16).ToArray();var h=Block(cipher,new byte[16]);var j=new byte[16];Buffer.BlockCopy(nonce,0,j,0,12);j[15]=1;
                    var tag=Hash(h,aad,encrypted);var mask=Block(cipher,j);int diff=0;for(int i=0;i<16;i++)diff|=(tag[i]^mask[i])^sealedData[encrypted.Length+i];
                    if(diff!=0)throw new CryptographicException("GCM authentication failed");return Ctr(cipher,j,encrypted);
                }
            }
        }
        static byte[] Block(ICryptoTransform aes,byte[] block){var output=new byte[16];aes.TransformBlock(block,0,16,output,0);return output;}
        static byte[] Ctr(ICryptoTransform aes,byte[] initial,byte[] data)
        {
            byte[] counter=(byte[])initial.Clone(),result=new byte[data.Length];
            for(int offset=0;offset<data.Length;offset+=16){for(int i=15;i>=12;i--)if(++counter[i]!=0)break;var stream=Block(aes,counter);for(int i=0;i<16&&offset+i<data.Length;i++)result[offset+i]=(byte)(data[offset+i]^stream[i]);}return result;
        }
        static byte[] Hash(byte[] h,byte[] aad,byte[] cipher)
        {
            var y=new byte[16];Absorb(y,h,aad);Absorb(y,h,cipher);var lengths=new byte[16];Write64(lengths,0,(ulong)aad.Length*8);Write64(lengths,8,(ulong)cipher.Length*8);for(int i=0;i<16;i++)y[i]^=lengths[i];return Multiply(y,h);
        }
        static void Absorb(byte[] y,byte[] h,byte[] data)
        {
            for(int offset=0;offset<data.Length;offset+=16){for(int i=0;i<16&&offset+i<data.Length;i++)y[i]^=data[offset+i];var next=Multiply(y,h);Buffer.BlockCopy(next,0,y,0,16);}
        }
        static byte[] Multiply(byte[] x,byte[] h)
        {
            var z=new byte[16];var v=(byte[])h.Clone();
            for(int bit=0;bit<128;bit++)
            {
                int selected=-((x[bit/8]>>(7-bit%8))&1);for(int i=0;i<16;i++)z[i]^=(byte)(v[i]&selected);
                int lsb=v[15]&1;for(int i=15;i>=0;i--)v[i]=(byte)((v[i]>>1)|(i>0?(v[i-1]&1)<<7:0));v[0]^=(byte)(0xe1&-lsb);
            }
            return z;
        }
        public static void Write64(byte[] b,int offset,ulong value){for(int i=7;i>=0;i--){b[offset+i]=(byte)value;value>>=8;}}
        public static ulong Read64(byte[] b,int offset){ulong n=0;for(int i=0;i<8;i++)n=(n<<8)|b[offset+i];return n;}
    }
}
