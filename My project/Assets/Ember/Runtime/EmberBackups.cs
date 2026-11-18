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
            Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.08f,.04f,.15f,.78f));
            float width=Mathf.Min(560,Screen.width-24);
            float x=(Screen.width-width)/2,y=(Screen.height-298)/2;
            PixelPanel(new Rect(x,y,width,298),FieldPaper);
            UiLabel(new Rect(x+16,y+11,width-32,44),L("Phục hồi ký ức","Restore memories"),title);
            UiLabel(new Rect(x+16,y+55,width-32,28),restoreSource,small);

            float cell=(width-32)/3;
            int[] amounts={pendingRestore.Ash,pendingRestore.Memory,pendingRestore.Tokens};
            string[] titles={L("Tro Tàn","Ash"),L("Ký Ức","Memory"),L("Định Mệnh","Tokens")};
            for(int i=0;i<amounts.Length;i++)
            {
                var card=new Rect(x+16+i*cell,y+89,cell-6,67);
                PixelPanel(card,new Color(.21f,.15f,.32f));
                PixelArt(new Rect(card.x+7,card.y+7,24,24),RewardIcons[i]);
                UiLabel(new Rect(card.x+34,card.y+8,card.width-38,25),amounts[i].ToString(),small);
                UiLabel(new Rect(card.x+7,card.y+37,card.width-12,24),titles[i],small);
            }
            UiLabel(new Rect(x+16,y+162,width-32,26),
                L("Trận ","Runs ")+pendingRestore.Runs+L("  ·  Rút lui ","  ·  Extractions ")+pendingRestore.Extractions+
                L("  ·  Bản Ngã ","  ·  Vessels ")+pendingRestore.Vessels.Length,small);
            UiLabel(new Rect(x+16,y+193,width-32,42),
                L("Thay tiến trình hiện tại bằng bản sao này?","Replace current progress with this backup?"),label);

            float buttonWidth=(width-40)/2;
            if(UiButton(new Rect(x+16,y+245,buttonWidth,38),L("Phục hồi","Restore")))
            {
                try
                {
                    // Preserve current progress before replacement, even when it differs.
                    if(!saveBlocked)EmberSave.Export(ProfileDirectory,null,JsonUtility.ToJson(Profile),savePassword);
                    Profile=pendingRestore;selected=Profile.SelectedVessel;pendingRestore=null;saveBlocked=false;SaveProfile();notice="Đã phục hồi. Tiến trình cũ được giữ trong Backups.";
                }catch(Exception ex){notice=ex.Message;}
            }
            if(UiButton(new Rect(x+24+buttonWidth,y+245,buttonWidth,38),L("Hủy","Cancel")))pendingRestore=null;
        }
    }
}
