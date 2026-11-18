using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ember
{
    public sealed partial class EmberGame
    {
        AudioClip jesterLaugh;float nextLaugh;float soundVolume=1;
        static readonly int[] CameraZoomPixels={64,72,80,88,96,112,128};
        const int DefaultZoom=4;
        int zoomLevel=DefaultZoom;

        void SetCameraZoom(int level)
        {
            level=Mathf.Clamp(level,0,CameraZoomPixels.Length-1);
            if(level==zoomLevel)return;
            zoomLevel=level;
            PlayerPrefs.SetInt("Ember.CameraZoom",zoomLevel);
        }

        void ReadCameraZoom()
        {
            var mouse=Mouse.current;
            if(mouse==null||menu||craftOpen||debugOpen||State==null||Local==null||
                State.Phase==Phase.Sanctuary||State.Phase==Phase.Won||State.Phase==Phase.Lost)return;
            float wheel=mouse.scroll.ReadValue().y;
            if(wheel!=0)SetCameraZoom(zoomLevel+(wheel>0?1:-1));
        }

        bool showFps=true;
        float fpsSampleTime;
        int fpsSampleFrames,displayFps;

        void SampleFrameRate()
        {
            if(!showFps)return;
            fpsSampleTime+=Time.unscaledDeltaTime;
            fpsSampleFrames++;
            if(fpsSampleTime<.5f)return;
            displayFps=Mathf.RoundToInt(fpsSampleFrames/fpsSampleTime);
            fpsSampleFrames=0;
            fpsSampleTime=0;
        }

        void DrawFrameRate()
        {
            if(!showFps||displayFps<=0||menu||inventoryOpen||State==null||
                State.Phase==Phase.Sanctuary||State.Phase==Phase.Won||State.Phase==Phase.Lost||
                Event.current.type!=EventType.Repaint)return;
            var badge=new Rect(10,Screen.height-34,80,24);

            UiLabel(new Rect(badge.x+6,badge.y+2,badge.width-12,21),displayFps+" FPS",small);
        }

        void InitializeSettings()
        {
            showFps=PlayerPrefs.GetInt("Ember.ShowFps",1)!=0;
            zoomLevel=Mathf.Clamp(PlayerPrefs.GetInt("Ember.CameraZoom",DefaultZoom),0,CameraZoomPixels.Length-1);
            soundVolume=PlayerPrefs.GetFloat("Ember.Volume",.7f);AudioListener.volume=soundVolume;moba=PlayerPrefs.GetInt("Ember.Moba",1)==1;
            int count=22050;var samples=new float[count];
            for(int i=0;i<count;i++){float t=i/22050f,pulse=Mathf.Max(0,Mathf.Sin(t*22));samples[i]=Mathf.Sin(t*(210+Mathf.Sin(t*17)*35)*Mathf.PI*2)*pulse*Mathf.Sin(t*Mathf.PI)*.18f;}
            jesterLaugh=AudioClip.Create("Jester laugh",count,1,22050,false);jesterLaugh.SetData(samples,0);
        }
        void UpdateAmbientAudio()
        {
            if(State==null||Local==null||clock<nextLaugh)return;
            var jester=State.Players.FirstOrDefault(p=>p.Hat==(int)Hat.Jester&&p.Dread>=90&&!p.Ghost&&!p.Extracted&&V2.Distance(p.Pos,Local.Pos)<14);
            if(jester!=null){audioSource.panStereo=Mathf.Clamp((jester.Pos.X-Local.Pos.X)/12,-.8f,.8f);audioSource.PlayOneShot(jesterLaugh,.7f);nextLaugh=clock+7;Trace("audio.jester","player="+jester.Id);}
        }
        bool pauseSettings,pauseConfirm;
        Vector2 pauseScroll;

        void DrawPauseMenu()
        {
            float width=Mathf.Min(438,Screen.width-30);
            bool canSaveRun=CanSaveCurrentSoloRun();
            float height=pauseSettings?Mathf.Min(520,Screen.height-56):(canSaveRun?344:300);
            var box=new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height);
            Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.035f,.025f,.075f,.70f));
            PixelPanel(box,FieldPaper);
            GUILayout.BeginArea(new Rect(box.x+17,box.y+12,box.width-34,box.height-24));
            UiText(pauseSettings?L("Thiết lập","Settings"):pauseConfirm?L("Bỏ trận đang chơi?","Discard current run?"):
                network?L("Menu trận","Match menu"):L("Tạm dừng","Paused"),title);
            GUILayout.Space(6);

            if(pauseSettings)
            {
                pauseScroll=GUILayout.BeginScrollView(pauseScroll);
                DrawSettings();
                GUILayout.EndScrollView();
                if(UiButton(L("Quay lại","Back"),GUILayout.Height(36)))pauseSettings=false;
            }
            else if(pauseConfirm)
            {
                UiText(network
                    ?L("Anh sẽ về sảnh và rời trận co-op hiện tại.","Return to Sanctuary and leave the current co-op run.")
                    :L("Bản lưu trận dở sẽ bị xóa và không cộng thưởng.","The saved solo run will be discarded with no rewards."),label);
                GUILayout.Space(12);
                if(UiButton(network?L("Rời trận","Leave run"):L("Xóa trận & Về sảnh","Discard & Sanctuary"),GUILayout.Height(40)))
                {
                    pauseConfirm=false;
                    if(!network)ClearSavedRun();
                    LeaveRoom();
                }
                if(UiButton(L("Quay lại","Back"),GUILayout.Height(40)))pauseConfirm=false;
            }
            else
            {
                if(network)UiText(L("Trận co-op vẫn tiếp tục.","The co-op match keeps running."),small);
                else
                {
                    var banner=GUILayoutUtility.GetRect(0,54,GUILayout.ExpandWidth(true));
                    PixelArt(banner,"sanctuary",ScaleMode.ScaleAndCrop);
                }
                GUILayout.Space(10);
                if(UiButton(L("Tiếp tục","Resume"),GUILayout.Height(40)))menu=false;
                if(UiButton(L("Thiết lập","Settings"),GUILayout.Height(40))){pauseSettings=true;pauseScroll=Vector2.zero;}
                if(canSaveRun)
                {
                    if(UiButton(L("Lưu & Về sảnh","Save & Sanctuary"),GUILayout.Height(40))){SaveActiveRun(true);LeaveRoom();}
                    if(UiButton(L("Bỏ trận (xóa save)","Discard run"),GUILayout.Height(36)))pauseConfirm=true;
                }
                else if(UiButton(L("Về sảnh","Back to Sanctuary"),GUILayout.Height(40)))pauseConfirm=true;
            }
            GUILayout.EndArea();
        }

        void DrawSettings()
        {
            UiText(L("Âm thanh","Audio"),label);
            UiText(L("Âm lượng ","Volume ")+Mathf.RoundToInt(soundVolume*100)+"%",small);
            float volume=GUILayout.HorizontalSlider(soundVolume,0,1,GUILayout.Height(20));
            if(Math.Abs(volume-soundVolume)>.001f)
            {
                soundVolume=volume;
                AudioListener.volume=volume;
                PlayerPrefs.SetFloat("Ember.Volume",volume);
            }
            GUILayout.Space(14);
            UiText(L("Tầm nhìn","Camera zoom"),label);
            UiText(L("Cuộn chuột trong trận để đổi mức.","Scroll in game to change zoom."),small);
            bool fps=GUILayout.Toggle(showFps,L("Hiện FPS","Show FPS"),GUILayout.Height(30));
            if(fps!=showFps){showFps=fps;PlayerPrefs.SetInt("Ember.ShowFps",fps?1:0);}
            SetCameraZoom(UiSelection(zoomLevel,new[]{L("Xa","Far"),"2","3","4",L("Mặc định","Default"),"6",L("Gần","Near")},4));
            GUILayout.Space(14);
            UiText(L("Điều khiển","Controls"),label);
            bool next=GUILayout.Toggle(moba,moba?L("Tìm đường chuột phải · Bật","Right-click pathfinding · On"):
                L("Tìm đường chuột phải · Tắt","Right-click pathfinding · Off"),GUILayout.Height(32));
            if(next!=moba){moba=next;PlayerPrefs.SetInt("Ember.Moba",moba?1:0);}
            UiText(L("K đánh thường · Space lướt · J kỹ năng · L đẩy · I kho đồ · G trao đèn · M đổi kiểu chuột","K attack · Space dash · J skill · L shove · I inventory · G pass lantern · M switches mouse mode"),small);
            GUILayout.Space(14);
            UiText(L("Hướng dẫn trận","Run guide"),label);
            DrawGuideCard("core",L("Thắp sáng hải đăng","Light the beacon"),
                L("Mang 20 lõi về trụ, bấm B để nạp.","Bring 20 cores to the beacon and press B to deposit."));
            DrawGuideCard("umbra",L("Tử thủ","Defend the beacon"),
                L("Giữ hải đăng trong 90 giây Nhật Thực.","Protect the beacon through the 90-second Eclipse."));
            DrawGuideCard("awakening",L("Rút lui","Extract"),
                L("Khi cổng mở, về hải đăng và bấm E.","When the gate opens, return to the beacon and press E."));
            DrawGuideCard("revival",L("Hồi sinh đồng đội","Revive an ally"),
                L("Đến gần hải đăng, chọn đá [4] rồi dùng [U].","Near the beacon, select the stone [4] and use it [U]."));
            DrawGuideCard("dust",L("Hoảng Loạn","Dread"),
                L("Ánh sáng giảm Hoảng Loạn. Từ 90% trong bóng tối có thể đánh nhầm đồng đội.",
                  "Light lowers Dread. At 90% in darkness, you can hit teammates."));
            if(Application.isEditor||Debug.isDebugBuild)
            {
                GUILayout.Space(14);
                if(UiButton(L("Công cụ kiểm thử [F8]","Test tools [F8]")))debugOpen=!debugOpen;
            }
        }
        void DrawGuideCard(string icon,string heading,string detail)
        {
            Rect card=GUILayoutUtility.GetRect(152,74,GUILayout.ExpandWidth(true));
            PixelPanel(card,menu&&State!=null&&State.Phase!=Phase.Sanctuary?FieldPaper:Paper);
            PixelArt(new Rect(card.x+9,card.y+12,30,30),icon);
            UiLabel(new Rect(card.x+48,card.y+6,card.width-57,24),heading,label);
            UiLabel(new Rect(card.x+48,card.y+30,card.width-57,42),detail,small);
            GUILayout.Space(6);
        }

        void DrawProgressionExtras()
        {
            GUILayout.Space(8);GUILayout.Label(L("Kinh tế và thành tựu","Economy and achievements"));
            GUI.enabled=Profile.Ash>=100;
            if(GUILayout.Button(L("Đổi 100 tro → 1 Đá Định Mệnh","Exchange 100 ash → 1 Void Token"))){Profile.ExchangeAsh();SaveProfile();}
            GUI.enabled=true;
            GUILayout.Space(4);
            string[] achievements=EmberLanguage.English?new[]{"Survive 120 seconds","First extraction","Defeat a boss","Craft 5 items"}:new[]{"Sống sót 120 giây","Rút lui lần đầu","Hạ một boss","Chế tạo 5 vật phẩm"};
            for(int bit=0;bit<achievements.Length;bit++)
            {
                bool earned=(Profile.AchievementFlags&(1<<bit))!=0;
                Rect raw=GUILayoutUtility.GetRect(160,56,GUILayout.ExpandWidth(true));
                Rect card=new Rect(raw.x+4,raw.y,Mathf.Max(140,raw.width-8),raw.height);
                PixelPanel(card,earned?Lilac:Paper);
                PixelArt(new Rect(card.x+10,card.y+12,32,32),"perk_"+new[]{3,5,0,4}[bit]);
                GUI.Label(new Rect(card.x+52,card.y+5,card.width-60,24),achievements[bit],label);
                GUI.Label(new Rect(card.x+52,card.y+30,card.width-60,21),
                    (earned?L("Đã nhận","Claimed"):L("Chưa đạt","Incomplete"))+" · "+(bit==1?3:2)+L(" Đá Định Mệnh"," Void Tokens"),small);
                GUILayout.Space(6);
            }
            GUILayout.Label(L("Mở công thức bằng ký ức","Unlock recipes with memory"));
            for(int i=7;i<Catalog.Recipes.Length;i++)
            {
                bool owned=Profile.Recipes.Contains(i);GUI.enabled=!owned&&Profile.Memory>=3;
                if(GUILayout.Button((owned?"✓ ":"")+EmberLanguage.Text(Catalog.Recipes[i].Name)+(owned?"":" · 3 "+L("ký ức","memory")))){Profile.UnlockRecipe(i);SaveProfile();}
                GUI.enabled=true;
            }
        }
        string PerkRankDescription(int index)
        {
            int rank=Profile.Rank(index);float scale=1+.2f*Math.Max(0,rank-1);
            switch((Perk)index)
            {
                case Perk.SteelNerves:return L("Tăng hoảng loạn giảm ","Dread gain reduced by ")+(25*scale).ToString("0")+"%";
                case Perk.StrongBack:return L("Tải trọng +","Carry capacity +")+(30*scale).ToString("0")+"%";
                case Perk.GhostStep:return L("Thể lực lướt giảm ","Dash stamina reduced by ")+(20*scale).ToString("0")+"%";
                case Perk.Prepared:return (rank+1)+L(" pháo sáng + "," flares + ")+(rank==3?2:1)+L(" thuốc an thần"," sedatives");
                case Perk.Miner:return L("Cuốc sắt; khai thác tối đa ","Iron pickaxe; gather up to ")+(rank+2)+L(" mỗi lượt"," per action");
                case Perk.LastLight:return L("Đèn bùng sáng thêm ","Final light lasts ")+(5*scale).ToString("0")+L(" giây"," seconds");
                default:return L("Giảm sát thương gây nhầm ","Outgoing friendly fire reduced by ")+(50*scale).ToString("0")+"%";
            }
        }
    }
}
