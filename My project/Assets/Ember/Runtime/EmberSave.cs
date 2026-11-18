using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Ember
{
    [Serializable] public sealed class Profile
    {
        public int Version=1,Ash,Memory,Tokens,PerkSlots=1,Runs,Extractions;
        public int[] Vessels={0},Perks=Array.Empty<int>(),Hats=Array.Empty<int>(),Pets=Array.Empty<int>(),EquippedPerks=Array.Empty<int>();
        public int AwakeningStones;
        public bool LittleMooneAwake;
        public bool WandererAwake;
        public bool IsAwake(int vessel)=>vessel==(int)Vessel.Wanderer?WandererAwake:vessel==(int)Vessel.LittleMoone?LittleMooneAwake:false;
        public bool CanPlay(int vessel)=>Vessels.Contains(vessel)&&(vessel!=(int)Vessel.LittleMoone||LittleMooneAwake);
        public bool AwakenLittleMoone()
        {
            if(!Vessels.Contains((int)Vessel.LittleMoone)||LittleMooneAwake||AwakeningStones<1)return false;
            AwakeningStones--;
            LittleMooneAwake=true;
            return true;
        }
        public bool AwakenWanderer()
        {
            if(WandererAwake||AwakeningStones<1)return false;
            AwakeningStones--;
            WandererAwake=true;
            return true;
        }
        public int SelectedVessel,SelectedHat=-1,SelectedPet=-1;
        public int[] PerkRanks=new int[7],Recipes={0,1,2,3,4,5,6};
        public int AchievementFlags,TotalGathered,TotalCrafted;
        public int Rank(int index)=>Perks.Contains(index)?Math.Max(1,Math.Min(3,PerkRanks!=null&&index<PerkRanks.Length?PerkRanks[index]:1)):0;
        public bool UpgradePerk(int index)
        {
            int rank=Rank(index),cost=rank*60;if(rank<1||rank>=3||Ash<cost)return false;
            if(PerkRanks==null||PerkRanks.Length!=7)PerkRanks=new int[7];Ash-=cost;PerkRanks[index]=rank+1;return true;
        }
        public bool UnlockRecipe(int index)
        {
            if(index<0||index>=Catalog.Recipes.Length||Recipes.Contains(index)||Memory<3)return false;
            Memory-=3;Recipes=Recipes.Concat(new[]{index}).ToArray();return true;
        }
        public bool ExchangeAsh(){if(Ash<100)return false;Ash-=100;Tokens++;return true;}
        public void ValidateAndNormalize()
        {
            if(Version!=1||Ash<0||Memory<0||Tokens<0||Ash>1000000000||Memory>1000000000||Tokens>1000000000||PerkSlots<1||PerkSlots>3||Runs<0||Extractions<0||Vessels==null||Perks==null||Hats==null||Pets==null||EquippedPerks==null||EquippedPerks.Length>PerkSlots)throw new InvalidDataException("Profile không hợp lệ.");
            if(!CanPlay(SelectedVessel)||AwakeningStones<0||(LittleMooneAwake&&!Vessels.Contains((int)Vessel.LittleMoone))||(WandererAwake&&!Vessels.Contains((int)Vessel.Wanderer))||Vessels.Any(v=>v<0||v>=Catalog.VesselNames.Length)||Perks.Any(v=>v<0||v>=Catalog.PerkNames.Length)||Hats.Any(v=>v<0||v>3)||Pets.Any(v=>v<0||v>3)||EquippedPerks.Any(v=>!Perks.Contains(v))||(SelectedHat!=-1&&!Hats.Contains(SelectedHat))||(SelectedPet!=-1&&!Pets.Contains(SelectedPet)))throw new InvalidDataException("Profile chứa lựa chọn không hợp lệ.");
            if(PerkRanks==null)PerkRanks=new int[7];if(Recipes==null)Recipes=Enumerable.Range(0,14).ToArray(); // Existing pre-recipe saves keep their unlocked content.
            if(PerkRanks.Length!=7||PerkRanks.Any(v=>v<0||v>3)||Recipes.Length>14||Recipes.Any(v=>v<0||v>=14)||TotalCrafted<0||TotalGathered<0)throw new InvalidDataException("Profile progression không hợp lệ.");
            Vessels=Vessels.Distinct().ToArray();Perks=Perks.Distinct().ToArray();Hats=Hats.Distinct().ToArray();Pets=Pets.Distinct().ToArray();EquippedPerks=EquippedPerks.Distinct().ToArray();Recipes=Recipes.Distinct().ToArray();
        }
        public bool UnlockPerk(int index)
        {
            if(index<0||index>=Catalog.PerkNames.Length||Perks.Contains(index)||Ash<30)return false;
            Ash-=30;Perks=Perks.Concat(new[]{index}).ToArray();return true;
        }
        public bool BuySlot(){if(PerkSlots>=3||Memory<5)return false;Memory-=5;PerkSlots++;return true;}
        public int Roll(int pool,Random random)
        {
            if(pool<0||pool>2||Tokens<3)return -1;Tokens-=3;int result=random.Next(pool==0?5:4);
            if(pool==0){int draw=random.Next(Catalog.VesselDrawWeights.Sum());for(int i=0;i<Catalog.VesselDrawWeights.Length;i++){draw-=Catalog.VesselDrawWeights[i];if(draw<0){result=i;break;}}}
            int[] owned=pool==0?Vessels:pool==1?Hats:Pets;
            if(owned.Contains(result)){Ash+=10;return result;}
            owned=owned.Concat(new[]{result}).ToArray();if(pool==0)Vessels=owned;else if(pool==1)Hats=owned;else Pets=owned;return result;
        }
        public void Reward(Snapshot s,Player p,Random random=null)
        {
            var rng=random??new Random();
            if(p.Extracted&&s.Charge>=Math.Max(100,s.ChargeTarget)&&rng.Next(100)<30)AwakeningStones++;
            for(int b=0;b<s.BossesDefeated;b++)if(rng.Next(100)<25)AwakeningStones++;
            Runs++;bool escaped=s.TeamExtracted>0||s.Players.Any(q=>q.Extracted);float share=p.Extracted?1:p.Ghost&&escaped?.5f:0;Memory+=(int)(s.BossesDefeated*2*(p.Ghost&&escaped?.5f:1));
            Ash+=(int)((s.Time*.1f+Math.Max(p.Gathered,p.Bag.Sum())*2+20)*(share>0?share:.25f));if(share>0){Memory+=(int)(4*share);Tokens+=p.Extracted?2:1;}if(p.Extracted)Extractions++;
            TotalGathered+=p.Gathered;TotalCrafted+=p.Crafted;
            int earned=(s.Time>=120?1:0)|(p.Extracted?2:0)|(s.BossesDefeated>0?4:0)|(TotalCrafted>=5?8:0);
            for(int bit=0;bit<4;bit++)if((earned&(1<<bit))!=0&&(AchievementFlags&(1<<bit))==0)Tokens+=bit==1?3:2;
            AchievementFlags|=earned;
        }
    }
    // Versioned encrypt-then-MAC envelope. Header, salt, IV and ciphertext are
    // all authenticated before decrypt. Local files are tamper evident, not anti-cheat.
    public static class EmberSave
    {
        static readonly byte[] Magic=Encoding.ASCII.GetBytes("EMBER001");
        const int SaltLength=16,IVLength=16,TagLength=32,Iterations=100000;
        public static byte[] Protect(string json,string password)
        {
            byte[] salt=RandomBytes(SaltLength),iv=RandomBytes(IVLength),key=Derive(password,salt),cipher;
            using(var aes=Aes.Create()){aes.Key=key.Take(32).ToArray();aes.IV=iv;aes.Mode=CipherMode.CBC;aes.Padding=PaddingMode.PKCS7;using(var encrypt=aes.CreateEncryptor()){var plain=Encoding.UTF8.GetBytes(json);cipher=encrypt.TransformFinalBlock(plain,0,plain.Length);}}
            var body=Magic.Concat(salt).Concat(iv).Concat(cipher).ToArray();byte[] tag;using(var hmac=new HMACSHA256(key.Skip(32).ToArray()))tag=hmac.ComputeHash(body);
            Array.Clear(key,0,key.Length);return body.Concat(tag).ToArray();
        }
        public static string Unprotect(byte[] file,string password)
        {
            if(file==null||file.Length<88||file.Length>2*1024*1024||!file.Take(8).SequenceEqual(Magic))throw new InvalidDataException("Invalid .ember file");
            var key=Derive(password,file.Skip(8).Take(16).ToArray());int bodyLength=file.Length-TagLength;byte[] tag;
            using(var hmac=new HMACSHA256(key.Skip(32).ToArray()))tag=hmac.ComputeHash(file,0,bodyLength);
            int diff=0;for(int i=0;i<TagLength;i++)diff|=tag[i]^file[bodyLength+i];if(diff!=0)throw new CryptographicException("Sai mật khẩu hoặc file đã bị thay đổi.");
            try{using(var aes=Aes.Create()){aes.Key=key.Take(32).ToArray();aes.IV=file.Skip(24).Take(16).ToArray();using(var decrypt=aes.CreateDecryptor())return Encoding.UTF8.GetString(decrypt.TransformFinalBlock(file,40,bodyLength-40));}}
            finally{Array.Clear(key,0,key.Length);}
        }
        static byte[] Derive(string password,byte[] salt){using(var kdf=new Rfc2898DeriveBytes(password,salt,Iterations,HashAlgorithmName.SHA256))return kdf.GetBytes(64);}
        static byte[] RandomBytes(int length){var data=new byte[length];using(var r=RandomNumberGenerator.Create())r.GetBytes(data);return data;}
        public static void WriteAtomic(string path,byte[] data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));string tmp=path+".tmp";File.WriteAllBytes(tmp,data);
            if(File.Exists(path))File.Replace(tmp,path,path+".previous");else File.Move(tmp,path);
        }
        public static string Export(string directory,string destination,string json,string password)
        {
            var bytes=Protect(json,password);string vault=Path.Combine(directory,"Backups");Directory.CreateDirectory(vault);
            string backup=Path.Combine(vault,"backup_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".ember");WriteAtomic(backup,bytes);
            foreach(var old in new DirectoryInfo(vault).GetFiles("backup_*.ember").OrderByDescending(f=>f.LastWriteTimeUtc).Skip(5))old.Delete();
            if(!string.IsNullOrWhiteSpace(destination))WriteAtomic(destination,bytes);return backup;
        }
    }
}
