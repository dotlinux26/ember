using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        Profile pendingRestore;string restoreSource="",recoveryCode="";Vector2 metaScroll;
        void PreviewRestore(byte[] bytes,string source)
        {
            var profile=JsonUtility.FromJson<Profile>(EmberSave.Unprotect(bytes,savePassword));ValidateProfile(profile);pendingRestore=profile;restoreSource=source;
        }
        void DrawBackupVault()
        {
            GUILayout.Space(8);UiText("Bản sao lưu nội bộ",label);
            string vault=Path.Combine(ProfileDirectory,"Backups");
            if(Directory.Exists(vault))foreach(var file in new DirectoryInfo(vault).GetFiles("backup_*.ember").OrderByDescending(f=>f.LastWriteTimeUtc).Take(5))
            {
                if(UiButton(file.LastWriteTime.ToString("dd/MM/yyyy HH:mm:ss")+"  •  Xem trước phục hồi"))try{PreviewRestore(File.ReadAllBytes(file.FullName),file.Name);}catch(Exception ex){notice=ex.Message;}
            }
            if(!string.IsNullOrEmpty(saveFile)&&UiButton("Mở thư mục chứa bản xuất gần nhất"))EmberFileDialog.Reveal(saveFile);
            if(!string.IsNullOrEmpty(saveFile)&&UiButton("Sao chép mã khôi phục nhanh"))try{GUIUtility.systemCopyBuffer=Convert.ToBase64String(File.ReadAllBytes(saveFile));notice="Đã sao chép bản sao mã hóa. Vẫn cần mật khẩu để phục hồi.";}catch(Exception ex){notice=ex.Message;}
            UiText("Dán mã khôi phục (bản sao vẫn được mã hóa)",small);recoveryCode=GUILayout.TextField(recoveryCode,200000);
            if(UiButton("Xem trước từ mã khôi phục"))try{PreviewRestore(Convert.FromBase64String(recoveryCode.Trim()),"Mã khôi phục");}catch(Exception ex){notice=ex.Message;}
        }
        void DrawRestoreDialog()
        {
            if(pendingRestore==null)return;
            Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.08f,.04f,.15f,.88f));
            float width=Mathf.Min(680,Screen.width-20);
            float height=Mathf.Min(540,Screen.height-20);
            float x=(Screen.width-width)/2,y=(Screen.height-height)/2;
            PixelPanel(new Rect(x,y,width,height),FieldPaper);
            UiLabel(new Rect(x+20,y+10,width-40,36),L("Xem trước phục hồi ký ức","Memory Restore Preview"),title);
            UiLabel(new Rect(x+20,y+46,width-40,20),restoreSource,small);

            // 1. Currencies (4 cards: Ash, Memory, Tokens, AwakeningStones)
            float cardW=(width-40-18)/4f;
            int[] amounts={pendingRestore.Ash,pendingRestore.Memory,pendingRestore.Tokens,pendingRestore.AwakeningStones};
            string[] titles={L("Tro Tàn","Ash"),L("Ký Ức","Memory"),L("Định Mệnh","Tokens"),L("Đá Thức Tỉnh","Awakening")};
            for(int i=0;i<4;i++)
            {
                var card=new Rect(x+20+i*(cardW+6),y+70,cardW,58);
                PixelPanel(card,new Color(.21f,.15f,.32f));
                PixelArt(new Rect(card.x+7,card.y+6,22,22),RewardIcons[i]);
                UiLabel(new Rect(card.x+33,card.y+6,card.width-36,22),amounts[i].ToString(),label);
                UiLabel(new Rect(card.x+7,card.y+32,card.width-12,20),titles[i],small);
            }

            // 2. Unlocked Vessels
            float curY=y+136;
            UiLabel(new Rect(x+20,curY,width-40,22),L("Bản Ngã đã mở khóa ("+pendingRestore.Vessels.Length+"/"+Catalog.VesselNames.Length+"):","Unlocked Vessels ("+pendingRestore.Vessels.Length+"/"+Catalog.VesselNames.Length+"):"),label);
            curY+=26;
            for(int vi=0;vi<Catalog.VesselNames.Length;vi++)
            {
                bool hasV=pendingRestore.Vessels.Contains(vi);
                bool isAwake=(vi==(int)Vessel.LittleMoone&&pendingRestore.LittleMooneAwake)||(vi==(int)Vessel.Wanderer&&pendingRestore.WandererAwake);
                int row=vi/3,col=vi%3;
                float rW=(width-40-12)/3f;
                var vCard=new Rect(x+20+col*(rW+6),curY+row*40,rW,36);
                PixelPanel(vCard,hasV?new Color(.18f,.24f,.38f):new Color(.15f,.12f,.18f,.6f));
                string vText=Catalog.VesselNames[vi]+(isAwake?" ⭐":!hasV?" (Khóa)":"");
                UiLabel(new Rect(vCard.x+6,vCard.y+6,vCard.width-12,24),vText,small);
            }
            curY+=86;

            // 3. Unlocked Perks & Ranks
            UiLabel(new Rect(x+20,curY,width-40,22),L("Đặc quyền đã khai mở ("+pendingRestore.Perks.Length+"/7):","Unlocked Perks ("+pendingRestore.Perks.Length+"/7):"),label);
            curY+=26;
            string[] perkNamesVi={"Tinh Thần Thép","Lưng Rộng","Bộ Pháp Bóng Ma","Chuẩn Bị Kỹ","Thợ Mỏ Bậc Thầy","Ánh Sáng Cuối","Y Sư Bất Đắc Dĩ"};
            string[] perkNamesEn={"Steel Nerves","Strong Back","Ghost Step","Prepared","Master Miner","Last Light","Reluctant Healer"};
            string[] romanRanks={"-","I","II","III"};
            for(int pi=0;pi<7;pi++)
            {
                bool hasP=pendingRestore.Perks.Contains(pi);
                int rk=hasP?pendingRestore.Rank(pi):0;
                int row=pi/4,col=pi%4;
                float pW=(width-40-18)/4f;
                var pCard=new Rect(x+20+col*(pW+6),curY+row*32,pW,28);
                PixelPanel(pCard,hasP?new Color(.24f,.18f,.34f):new Color(.14f,.12f,.16f,.5f));
                string pName=EmberLanguage.English?perkNamesEn[pi]:perkNamesVi[pi];
                string pText=(hasP?pName+" "+romanRanks[rk]:pName+" (Chưa)");
                UiLabel(new Rect(pCard.x+4,pCard.y+4,pCard.width-8,20),pText,small);
            }
            curY+=70;

            // 4. Progress, Winrate, Gathering & Crafting Stats
            int winRate=pendingRestore.Runs>0?Mathf.RoundToInt((float)pendingRestore.Extractions/pendingRestore.Runs*100f):0;
            string statsText=L("Trận: ","Runs: ")+pendingRestore.Runs+"   |   "+
                             L("Rút lui: ","Extractions: ")+pendingRestore.Extractions+" ("+winRate+"%)   |   "+
                             L("Khai thác: ","Gathered: ")+pendingRestore.TotalGathered+"   |   "+
                             L("Chế tạo: ","Crafted: ")+pendingRestore.TotalCrafted+"   |   "+
                             L("Công thức: ","Recipes: ")+pendingRestore.Recipes.Length+"/7";
            PixelPanel(new Rect(x+20,curY,width-40,30),new Color(.14f,.18f,.28f));
            UiLabel(new Rect(x+28,curY+4,width-56,22),statsText,small);
            curY+=36;

            // 5. Confirmation message
            UiLabel(new Rect(x+20,curY,width-40,24),
                L("Thay tiến trình hiện tại bằng bản sao này?","Replace current progress with this backup?"),label);

            // 6. Action buttons
            float buttonWidth=(width-48)/2;
            if(UiButton(new Rect(x+20,y+height-48,buttonWidth,38),L("Phục hồi dữ liệu này","Restore this backup")))
            {
                try
                {
                    if(!saveBlocked)EmberSave.Export(ProfileDirectory,null,JsonUtility.ToJson(Profile),savePassword);
                    Profile=pendingRestore;selected=Profile.SelectedVessel;pendingRestore=null;saveBlocked=false;SaveProfile();
                    notice=EmberLanguage.English?"Restored! Previous progress saved in Backups.":"Đã phục hồi. Tiến trình cũ được giữ trong Backups.";
                }catch(Exception ex){notice=ex.Message;}
            }
            if(UiButton(new Rect(x+28+buttonWidth,y+height-48,buttonWidth,38),L("Hủy","Cancel")))pendingRestore=null;
        }
    }
}
