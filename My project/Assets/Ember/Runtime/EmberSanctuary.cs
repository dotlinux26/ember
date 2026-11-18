using System;
using System.Linq;
using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        static readonly Color Paper=new Color(1,.984f,.95f);
        static readonly Color Ink=new Color(.17f,.13f,.25f);
        static readonly Color Lilac=new Color(.91f,.86f,.98f);
        static readonly Color Plum=new Color(.34f,.23f,.51f);
        GUISkin sanctuarySkin;
        GUIStyle sanctuaryTitle,sanctuaryLabel,sanctuarySmall,sanctuaryBrandSubtitle;
        int sanctuaryPage,collectionPage,settingsPage,summonPool,summonPrize=-1,summonPrizePool;
        float summonStart=-10;
        int summonAshRefund;
        const float SummonDuration=2.35f;

        void WithSanctuaryTheme(Action draw)
        {
            var previousSkin=GUI.skin;
            var previousTitle=title;
            var previousLabel=label;
            var previousSmall=small;
            if(sanctuarySkin==null)
            {
                sanctuarySkin=Instantiate(previousSkin);
                sanctuarySkin.label.normal.textColor=Ink;
                sanctuaryTitle=new GUIStyle(title){fontSize=38,normal={textColor=Ink}};
                sanctuaryLabel=new GUIStyle(label){normal={textColor=Ink}};
                sanctuarySmall=new GUIStyle(small){normal={textColor=Plum}};
                PaintPixelSkin(sanctuarySkin,true);
            }
            GUI.skin=sanctuarySkin;
            title=sanctuaryTitle;
            label=sanctuaryLabel;
            small=sanctuarySmall;
            try{draw();}
            finally{GUI.skin=previousSkin;title=previousTitle;label=previousLabel;small=previousSmall;}
        }

        static readonly string[] CraftIcons={"torch","torch","core","beacon","wall","trap","wall","torch","torch","crystal_axe","shadow_blades","crossbow","lumen_gun","mirror_shield"};
        bool onlineExpanded;

        const float PlayActionHeight=40;
        bool vesselProfileOpen;
        static readonly string[] LoreVi={
            "Một lữ khách vô danh lần theo ánh đèn qua những con đường bị bóng tối nuốt mất.",
            "Từng chữa thương dưới ánh trăng, người y sư giữ lại chút tinh quang để dẫn đồng đội trở về.",
            "Người canh lò bỏ xưởng khi lửa vụt tắt, mang theo những quả bom tích ánh sáng.",
            "Lời thề khắc trên giáp buộc hộ vệ đứng lại cho đến khi người cuối cùng qua cổng.",
            "Bóng ma lần theo dấu hải đăng qua những khe nứt, kéo người đứng cạnh về nơi an toàn.",
            "Little Moone từng nhặt những mảnh sao trên mái tháp. Khi bầu trời tối hẳn, cô học cách gọi chúng trở về."
        };
        static readonly string[] LoreEn={
            "A nameless traveler follows lanterns down roads swallowed by the dark.",
            "Once a moonlit healer, the mender carries a spark of starlight to guide companions home.",
            "The furnace keeper left the workshop when its flame died, carrying bombs charged with light.",
            "An oath etched into the armor binds the guardian to stay until the last companion crosses the gate.",
            "The wraith follows the beacon through fractures in space and takes a nearby companion along.",
            "Little Moone once collected fallen stars on the tower roof. When the sky went dark, she learned to call them back."
        };

        static readonly string[] SanctuaryTabIcons={"tab_play","tab_summon","tab_collection","tab_settings"};
        GUIStyle inactiveTabText,activeTabText;

        int pendingSanctuaryPage=-1,pendingCollectionPage=-1,pendingSettingsPage=-1;

        void ApplySanctuaryNavigation()
        {
            if(Event.current.type!=EventType.Layout)return;
            if(pendingSanctuaryPage>=0)
            {
                sanctuaryPage=pendingSanctuaryPage;
                pendingSanctuaryPage=-1;
                metaScroll=Vector2.zero;
            }
            if(pendingCollectionPage>=0)
            {
                collectionPage=pendingCollectionPage;
                pendingCollectionPage=-1;
                metaScroll=Vector2.zero;
            }
            if(pendingSettingsPage>=0)
            {
                settingsPage=pendingSettingsPage;
                pendingSettingsPage=-1;
                metaScroll=Vector2.zero;
            }
        }

        void DrawSanctuarySubtabs(int page)
        {
            if(page==2)
            {
                int next=UiToolbar(collectionPage,new[]{L("Trang bị","Loadout"),L("Đặc quyền","Perks"),L("Tiến trình","Progress")});
                if(next!=collectionPage)pendingCollectionPage=next;
            }
            if(page==3)
            {
                int next=UiToolbar(settingsPage,new[]{L("Trò chơi","Game"),L("Sao lưu","Backups")});
                if(next!=settingsPage)pendingSettingsPage=next;
            }
        }

        int DrawSanctuaryTabs()
        {
            string[] names={L("Vào trận","Play"),L("Triệu hồi","Summon"),L("Bộ sưu tập","Collection"),L("Thiết lập","Settings")};
            bool compact=Screen.width<520;
            Rect row=GUILayoutUtility.GetRect(144,compact?66:57,GUILayout.ExpandWidth(true));
            if(inactiveTabText==null)
            {
                inactiveTabText=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,wordWrap=true,clipping=TextClipping.Clip,padding=new RectOffset(3,3,0,0)};
                activeTabText=new GUIStyle(inactiveTabText){normal={textColor=Color.white}};
            }
            int page=sanctuaryPage;
            float step=Mathf.Max(1,(row.width-18)/names.Length);
            for(int i=0;i<names.Length;i++)
            {
                var cell=new Rect(row.x+i*(step+6),row.y,step,row.height);
                bool active=i==sanctuaryPage;
                bool hover=cell.Contains(Event.current.mousePosition);
                PixelPanel(cell,active?Plum:hover?Color.Lerp(Lilac,Plum,.18f):Lilac);
                PixelArt(new Rect(cell.center.x-12,cell.y+4,24,24),SanctuaryTabIcons[i]);
                GUI.Label(new Rect(cell.x+1,cell.y+28,cell.width-2,cell.height-30),names[i],active?activeTabText:inactiveTabText);
                if(GUI.Button(cell,new GUIContent("",names[i]),GUIStyle.none))page=i;
            }
            return page;
        }

        void DrawVesselPicker()
        {
            var names=Catalog.VesselNames;
            Rect row=GUILayoutUtility.GetRect(180,184,GUILayout.ExpandWidth(true));
            if(Event.current.type!=EventType.Layout)
            {
                const float edge=46;
                if(GUI.Button(new Rect(row.x+2,row.center.y-22,36,44),"‹"))selected=(selected+names.Length-1)%names.Length;
                if(GUI.Button(new Rect(row.xMax-38,row.center.y-22,36,44),"›"))selected=(selected+1)%names.Length;
                var card=new Rect(row.x+edge,row.y,row.width-2*edge,row.height);
                PixelPanel(card,Lilac);
                PixelArt(new Rect(card.x+7,card.y+7,card.width-14,112),"sanctuary",ScaleMode.ScaleAndCrop);
                DrawSpritePreview(new Rect(card.center.x-50,card.y+10,100,100),Frame((Vessel)selected,"idle",(int)(Time.unscaledTime*6)));
                UiLabel(new Rect(card.x+10,card.y+120,card.width-20,27),names[selected],label);
                string status=Profile.CanPlay(selected)?Catalog.VesselRarities[selected]:
                    Profile.Vessels.Contains(selected)?"Ngủ đông":L("Chưa sở hữu","Unowned");
                UiLabel(new Rect(card.x+10,card.y+150,card.width-20,27),
                    EmberLanguage.Text(status)+"   ·   "+(selected+1)+"/"+names.Length,small);
            }

            GUILayout.Space(8);
            if(UiButton(vesselProfileOpen?L("Đóng hồ sơ","Close profile"):L("Đọc hồ sơ","Read profile"),GUILayout.Height(PlayActionHeight)))vesselProfileOpen=!vesselProfileOpen;
            if(!vesselProfileOpen)return;
            GUILayout.Space(8);
            UiText(L("Chỉ số và kỹ năng","Stats and skill"),label);
            UiText(Catalog.VesselDescriptions[selected],label);
            if(selected!=(int)Vessel.Wanderer)UiText(SkillRangeText((Vessel)selected),small);
            GUILayout.Space(8);
            UiText(L("Truyện nền","Story"),label);
            UiText((EmberLanguage.English?LoreEn:LoreVi)[selected],label);
            GUILayout.Space(8);
        }

        void DrawMultiplayerSetup()
        {
            usePublicBroker=UiToolbar(usePublicBroker?1:0,new[]{"LAN / VPN",L("Internet · mã phòng","Internet · room code")})==1;
            if(usePublicBroker)
            {
                UiText(L("Máy chủ kết nối","Connection server"),small);
                brokerAddress=GUILayout.TextField(brokerAddress);
                routeMode=(RouteMode)GUILayout.Toolbar((int)routeMode,new[]{"Auto","P2P","Relay"});
            }
            UiText("Mật khẩu phòng",small);
            password=GUILayout.PasswordField(password,'•');
            UiText(L("Tạo phòng","Create room"),label);
            roomName=GUILayout.TextField(roomName,32);
            UiText(L("Số người tối đa","Maximum players"),small);
            roomCapacity=UiSelection(roomCapacity-1,new[]{"1","2","3","4"},4)+1;
            GUI.enabled=Profile.CanPlay(selected);
            if(UiButton(L("Tạo phòng","Create room"),GUILayout.Height(32)))HostGame();
            GUI.enabled=true;
            GUILayout.Space(8);
            UiText(L("Tham gia phòng","Join room"),label);
            if(usePublicBroker)
            {
                UiText("Mã phòng để tham gia",small);
                joinRoomCode=GUILayout.TextField(joinRoomCode,6).ToUpperInvariant();
            }
            else
            {
                UiText("IP máy host",small);
                address=GUILayout.TextField(address);
            }
            GUI.enabled=Profile.CanPlay(selected);
            if(UiButton("Vào phòng",GUILayout.Height(32)))JoinGame();
            GUI.enabled=true;
        }

        void DrawPerkCards()
        {
            UiText(L("Đặc quyền","Perks")+"   "+Profile.EquippedPerks.Length+"/"+Profile.PerkSlots,label);
            if(Profile.PerkSlots<3)
            {
                GUI.enabled=Profile.Memory>=5;
                if(UiButton(L("Mở ô · 5 ký ức","Unlock slot · 5 memory"))){Profile.BuySlot();SaveProfile();}
                GUI.enabled=true;
            }

            for(int i=0;i<Catalog.PerkNames.Length;i++)
            {
                int perk=i;
                bool owned=Profile.Perks.Contains(i);
                bool equipped=Profile.EquippedPerks.Contains(i);
                int rank=Profile.Rank(i);
                Rect card=GUILayoutUtility.GetRect(180,113,GUILayout.ExpandWidth(true));
                PixelPanel(card,equipped?Lilac:Paper);
                PixelArt(new Rect(card.x+12,card.y+12,36,36),"perk_"+i);
                UiLabel(new Rect(card.x+58,card.y+7,card.width-70,24),Catalog.PerkNames[i],label);
                UiLabel(new Rect(card.x+58,card.y+32,card.width-70,40),owned?PerkRankDescription(i):Catalog.PerkDescriptions[i],small);
                string action=!owned?L("Mở · 30 tro","Unlock · 30 ash"):equipped?L("Tháo","Unequip"):L("Trang bị","Equip");
                GUI.enabled=owned?(equipped||Profile.EquippedPerks.Length<Profile.PerkSlots):Profile.Ash>=30;
                if(UiButton(new Rect(card.x+12,card.y+75,112,28),action))
                {
                    if(!owned)Profile.UnlockPerk(i);
                    else if(equipped)Profile.EquippedPerks=Profile.EquippedPerks.Where(value=>value!=perk).ToArray();
                    else Profile.EquippedPerks=Profile.EquippedPerks.Concat(new[]{perk}).ToArray();
                    SaveProfile();
                }
                GUI.enabled=true;
                if(owned)
                {
                    UiLabel(new Rect(card.x+131,card.y+78,64,24),L("Cấp ","Rank ")+rank+"/3",small);
                    if(rank<3&&Screen.width>=680)
                    {
                        GUI.enabled=Profile.Ash>=rank*60;
                        if(UiButton(new Rect(card.xMax-120,card.y+75,108,28),L("Nâng · ","Upgrade · ")+(rank*60)))
                        {
                            Profile.UpgradePerk(i);
                            SaveProfile();
                        }
                        GUI.enabled=true;
                    }
                    else if(rank<3)
                    {
                        GUI.enabled=Profile.Ash>=rank*60;
                        if(UiButton(L("Nâng cấp · ","Upgrade · ")+(rank*60))){Profile.UpgradePerk(i);SaveProfile();}
                        GUI.enabled=true;
                    }
                }
                GUILayout.Space(6);
            }
        }

        void DrawEquipmentCards(int pool)
        {
            bool hats=pool==1;
            var owned=hats?Profile.Hats:Profile.Pets;
            var descriptions=hats?Catalog.HatDescriptions:Catalog.PetDescriptions;
            int equipped=hats?Profile.SelectedHat:Profile.SelectedPet;
            GUILayout.Space(10);
            UiText(hats?L("Mũ","Hats"):L("Thú cưng","Pets"),label);
            if(owned.Length==0)
            {
                UiText(L("Chưa có món nào. Xem phần thưởng ở Triệu hồi.","Your collection is empty. Preview rewards in Summon."),small);
                return;
            }

            foreach(int index in owned)
            {
                bool active=equipped==index;
                Rect card=GUILayoutUtility.GetRect(180,94,GUILayout.ExpandWidth(true));
                PixelPanel(card,active?Lilac:Paper);
                DrawSpritePreview(new Rect(card.x+8,card.y+10,58,58),SummonSprite(pool,index));
                UiLabel(new Rect(card.x+72,card.y+5,card.width-80,23),SummonNames(pool)[index],label);
                UiLabel(new Rect(card.x+72,card.y+28,card.width-80,34),descriptions[index],small);
                if(UiButton(new Rect(card.x+72,card.y+63,Mathf.Min(130,card.width-80),25),
                    active?L("Đang dùng · Tháo","Equipped · Remove"):L("Trang bị","Equip")))
                {
                    int next=active?-1:index;
                    if(hats)Profile.SelectedHat=next;
                    else Profile.SelectedPet=next;
                    SaveProfile();
                }
                GUILayout.Space(8);
            }
        }

        void DrawOwnedPortraits()
        {
            GUILayout.BeginHorizontal();
            foreach(var vessel in Profile.Vessels)
            {
                var card=GUILayoutUtility.GetRect(48,62,GUILayout.Width(48));
                PixelPanel(card,Lilac);
                DrawSpritePreview(new Rect(card.x,card.y,48,48),Frame((Vessel)vessel,"idle",0));
                if(!Profile.CanPlay(vessel))PixelArt(new Rect(card.x+31,card.y+43,15,15),"awakening");
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        static readonly string[] ResourceIcons={"dust","core","umbra","wood","ore"};
        readonly System.Collections.Generic.Dictionary<string,Texture2D> interfaceArt=new System.Collections.Generic.Dictionary<string,Texture2D>();

        void PixelArt(Rect rect,string key,ScaleMode mode=ScaleMode.ScaleToFit)
        {
            if(Event.current.type!=EventType.Repaint)return;
            if(!interfaceArt.TryGetValue(key,out var texture))
            {
                texture=Resources.Load<Texture2D>("Ember/Interface/"+key);
                if(texture!=null)texture.filterMode=FilterMode.Point;
                interfaceArt[key]=texture;
            }
            if(texture==null)return;
            if(mode==ScaleMode.ScaleToFit)
                rect=FitPixelRect(rect,texture.width,texture.height);
            GUI.DrawTexture(rect,texture,mode,true);
        }

        void DrawCharacterCheat()
        {
            if(network||(!Application.isEditor&&!Debug.isDebugBuild))return;
            if(!UiButton(L("Test · Mở khóa tất cả Bản Ngã","Test · Unlock all vessels")))return;
            if(!debugSession)
            {
                Profile=JsonUtility.FromJson<Profile>(JsonUtility.ToJson(Profile));
                debugSession=true;
            }
            Profile.Vessels=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Range(0,Catalog.VesselNames.Length));
            Profile.LittleMooneAwake=true;
            notice=L("Đã mở toàn bộ Bản Ngã trong phiên test.","All vessels unlocked for this test session.");
            Trace("debug.unlock_vessels",notice);
        }

        void DrawSanctuaryBackdrop()
        {
            Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.95f,.92f,.98f));
            PixelArt(new Rect(0,8,Screen.width,Screen.height-8),"sanctuary_paper",ScaleMode.StretchToFill);
            Panel(new Rect(0,0,Screen.width,8),Plum);
            Panel(new Rect(0,101,Screen.width,1),new Color(.81f,.74f,.89f));
        }

        void DrawSanctuaryStage(Rect area)
        {
            PixelArt(area,"sanctuary",ScaleMode.ScaleAndCrop);
            float horizon=area.y+area.height*.65f;

            float cx=area.center.x;
            PixelPanel(new Rect(area.x,area.y,area.width,49),Paper);
            PixelPanel(new Rect(area.x,area.yMax-66,area.width,66),Paper);
            UiLabel(new Rect(area.x+18,area.y+15,area.width-36,30),Catalog.VesselRarities[selected],small);
            float size=Mathf.Min(area.width*.62f,area.height*.56f);
            DrawSpritePreview(new Rect(cx-size/2,horizon-size*.80f+Mathf.Sin(Time.unscaledTime*2)*3,size,size),Frame((Vessel)selected,"idle",(int)(Time.unscaledTime*6)));
            UiLabel(new Rect(area.x+18,area.yMax-65,area.width-36,45),Catalog.VesselNames[selected],label);
        }

        static Rect FitPixelRect(Rect box,float sourceWidth,float sourceHeight)
        {
            if(box.width<=0||box.height<=0)return box;
            float scale=Mathf.Min(box.width/sourceWidth,box.height/sourceHeight);
            if(scale>=1)scale=Mathf.Floor(scale);
            float width=Mathf.Max(1,Mathf.Floor(sourceWidth*scale));
            float height=Mathf.Max(1,Mathf.Floor(sourceHeight*scale));
            return new Rect(Mathf.Round(box.center.x-width/2),Mathf.Round(box.center.y-height/2),width,height);
        }

        void DrawSpritePreview(Rect rect,Sprite sprite)
        {
            if(sprite==null||Event.current.type!=EventType.Repaint)return;
            var uv=sprite.rect;
            var texture=sprite.texture;
            rect=FitPixelRect(rect,uv.width,uv.height);
            GUI.DrawTextureWithTexCoords(rect,texture,new Rect(uv.x/texture.width,uv.y/texture.height,uv.width/texture.width,uv.height/texture.height));
        }

        string[] SummonNames(int pool)=>pool==0?Catalog.VesselNames:pool==1?Catalog.HatNames:Catalog.PetNames;

        Sprite SummonSprite(int pool,int index)
        {
            if(pool==0)return Frame((Vessel)index,"idle",0);
            return Primitive(pool==1
                ?new[]{"hat_straw","hat_witch","hat_candle","hat_jester"}[index]
                :new[]{"pet_firefly","pet_bat","pet_cat","pet_ghost"}[index]);
        }

        void DrawSanctuaryBalances()
        {
            var strip=GUILayoutUtility.GetRect(180,34,GUILayout.ExpandWidth(true));
            if(Event.current.type!=EventType.Repaint)return;
            int[] amounts={Profile.Ash,Profile.Memory,Profile.AwakeningStones};
            string[] icons={"ash","memory","awakening"};
            string[] labels={L("Tro","Ash"),L("Ký ức","Memory"),L("Thức tỉnh","Awakening")};
            float step=strip.width/3;
            for(int i=0;i<amounts.Length;i++)
            {
                var chip=new Rect(strip.x+i*step+2,strip.y,step-4,32);
                PixelPanel(chip,Lilac);
                PixelArt(new Rect(chip.x+5,chip.y+5,22,22),icons[i]);
                UiLabel(new Rect(chip.x+30,chip.y+5,chip.width-32,25),labels[i]+" "+amounts[i],small);
            }
        }

        void DrawSummoning()
        {
            float elapsed=Time.unscaledTime-summonStart;
            bool spinning=summonPrize>=0&&elapsed<SummonDuration;
            DrawSanctuaryBalances();
            var balance=GUILayoutUtility.GetRect(120,36,GUILayout.ExpandWidth(true));
            PixelArt(new Rect(balance.x+4,balance.y+4,28,28),"token");
            UiLabel(new Rect(balance.x+39,balance.y+6,balance.width-44,27),
                Profile.Tokens+L(" Đá Định Mệnh"," Void Tokens"),label);
            GUI.enabled=!spinning;
            summonPool=UiToolbar(summonPool,new[]{L("Bản Ngã","Vessels"),L("Mũ","Hats"),L("Thú cưng","Pets")});
            GUI.enabled=true;

            var hero=GUILayoutUtility.GetRect(0,166,GUILayout.ExpandWidth(true));
            PixelArt(hero,"sanctuary",ScaleMode.ScaleAndCrop);
            PixelPanel(new Rect(hero.x,hero.y+106,hero.width,60),Paper);
            GUILayout.Space(10);
            if(summonPrize>=0)
            {
                int pool=summonPrizePool;
                var poolNames=SummonNames(pool);
                float progress=Mathf.Clamp01(elapsed/SummonDuration);
                int shown=spinning?(int)(Mathf.Sqrt(progress)*24)%poolNames.Length:summonPrize;
                float size=spinning?88+Mathf.Sin(elapsed*16)*7:96;
                DrawSpritePreview(new Rect(hero.center.x-size/2,hero.y+9,size,size),SummonSprite(pool,shown));
                UiLabel(new Rect(hero.x+12,hero.y+110,hero.width-24,28),
                    spinning?L("Đang triệu hồi…","Summoning…"):poolNames[shown],label);
                if(!spinning)
                {
                    string detail=pool==0?EmberLanguage.Text(Catalog.VesselRarities[shown]):pool==1?L("Mũ","Hat"):L("Thú cưng","Pet");
                    if(pool==0&&shown==(int)Vessel.LittleMoone&&!Profile.LittleMooneAwake)detail+=L(" · Ngủ đông"," · Dormant");
                    detail+=summonAshRefund>0?L(" · Trùng +"," · Duplicate +")+summonAshRefund+L(" tro"," ash"):L(" · Mới"," · New");
                    UiLabel(new Rect(hero.x+12,hero.y+138,hero.width-24,23),detail,small);
                }
            }
            else PixelArt(new Rect(hero.center.x-48,hero.y+12,96,96),"token");
            GUI.enabled=!spinning&&Profile.Tokens>=3;
            string summonAction=Profile.Tokens<3
                ?L("Cần thêm ","Need ")+(3-Profile.Tokens)+L(" đá để quay"," more tokens")
                :L("Triệu hồi · 3 đá","Summon · 3 tokens");
            if(UiButton(summonAction,GUILayout.Height(38)))
            {
                int ashBefore=Profile.Ash;
                summonPrize=Profile.Roll(summonPool,new System.Random());
                summonAshRefund=Profile.Ash-ashBefore;
                summonPrizePool=summonPool;
                summonStart=Time.unscaledTime;
                SaveProfile();
            }
            GUI.enabled=true;
            GUILayout.Space(12);
            var names=SummonNames(summonPool);
            for(int first=0;first<names.Length;first+=3)
            {
                GUILayout.BeginHorizontal();
                for(int i=first;i<Math.Min(first+3,names.Length);i++)
                {
                    var card=GUILayoutUtility.GetRect(85,114,GUILayout.ExpandWidth(true));
                    card=new Rect(card.x+4,card.y+3,card.width-8,card.height-6);
                    PixelPanel(card,Lilac);
                    DrawSpritePreview(new Rect(card.center.x-29,card.y+4,58,58),SummonSprite(summonPool,i));
                    UiLabel(new Rect(card.x+4,card.y+62,card.width-8,31),names[i],small);
                    string odds=summonPool==0?EmberLanguage.Text(Catalog.VesselRarities[i])+" · "+Catalog.VesselDrawWeights[i]+"%":"25%";
                    GUI.Label(new Rect(card.x+4,card.y+88,card.width-8,20),odds,small);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
        }
    }
}
