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
                sanctuaryTitle=new GUIStyle(title){fontSize=38,normal={textColor=Ink},hover={textColor=Ink}};
                sanctuaryLabel=new GUIStyle(label){normal={textColor=Ink},hover={textColor=Ink}};
                sanctuarySmall=new GUIStyle(small){normal={textColor=Plum},hover={textColor=Plum}};
                PaintPixelSkin(sanctuarySkin,true);
            }
            GUI.skin=sanctuarySkin;
            title=sanctuaryTitle;
            label=sanctuaryLabel;
            small=sanctuarySmall;
            try{draw();}
            finally{GUI.skin=previousSkin;title=previousTitle;label=previousLabel;small=previousSmall;}
        }

        static readonly string[] CraftIcons={"torch","torch","core","beacon","wall","trap","chest","torch","torch","crystal_axe","shadow_blades","crossbow","lumen_gun","mirror_shield"};
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

        int DrawSpacedChoices(int active,string[] options,float height=36)
        {
            if(inactiveTabText==null)
            {
                inactiveTabText=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,wordWrap=true,clipping=TextClipping.Clip,padding=new RectOffset(3,3,0,0)};
                activeTabText=new GUIStyle(inactiveTabText){normal={textColor=Color.white}};
            }
            Rect raw=GUILayoutUtility.GetRect(180,height,GUILayout.ExpandWidth(true));
            const float margin=4, gap=8;
            float innerX=raw.x+margin;
            float innerW=Mathf.Max(40,raw.width-2*margin);
            float step=(innerW-gap*(options.Length-1))/options.Length;
            int next=active;
            for(int i=0;i<options.Length;i++)
            {
                var cell=new Rect(innerX+i*(step+gap),raw.y,step,raw.height);
                bool on=i==active;
                bool hover=cell.Contains(Event.current.mousePosition);
                PixelPanel(cell,on?Plum:hover?Color.Lerp(Lilac,Plum,.18f):Lilac);
                GUI.Label(cell,options[i],on?activeTabText:inactiveTabText);
                if(GUI.Button(cell,GUIContent.none,GUIStyle.none))next=i;
            }
            return next;
        }

        int DrawSpacedSubtabs(int active,string[] tabs)
        {
            Rect raw=GUILayoutUtility.GetRect(180,34,GUILayout.ExpandWidth(true));
            const float margin=4, arrowW=34, gap=8;
            var row=new Rect(raw.x+margin,raw.y,Mathf.Max(80,raw.width-2*margin),raw.height);
            int next=active;
            if(GUI.Button(new Rect(row.x,row.y,arrowW,row.height),"‹"))next=(active+tabs.Length-1)%tabs.Length;
            if(GUI.Button(new Rect(row.xMax-arrowW,row.y,arrowW,row.height),"›"))next=(active+1)%tabs.Length;
            float innerX=row.x+arrowW+gap;
            float innerW=Mathf.Max(40,row.width-2*(arrowW+gap));
            float step=(innerW-gap*(tabs.Length-1))/tabs.Length;
            for(int i=0;i<tabs.Length;i++)
            {
                var cell=new Rect(innerX+i*(step+gap),row.y,step,row.height);
                bool on=i==active;
                bool hover=cell.Contains(Event.current.mousePosition);
                PixelPanel(cell,on?Plum:hover?Color.Lerp(Lilac,Plum,.18f):Lilac);
                GUI.Label(cell,tabs[i],on?activeTabText:inactiveTabText);
                if(GUI.Button(cell,GUIContent.none,GUIStyle.none))next=i;
            }
            return next;
        }

        void DrawSanctuarySubtabs(int page)
        {
            if(inactiveTabText==null)
            {
                inactiveTabText=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,wordWrap=true,clipping=TextClipping.Clip,padding=new RectOffset(3,3,0,0)};
                activeTabText=new GUIStyle(inactiveTabText){normal={textColor=Color.white}};
            }
            if(page==2)
            {
                int next=DrawSpacedSubtabs(collectionPage,new[]{L("Bản Ngã","Vessels"),L("Trang bị","Loadout"),L("Đặc quyền","Perks"),L("Tiến trình","Progress")});
                if(next!=collectionPage)pendingCollectionPage=next;
            }
            if(page==3)
            {
                int next=DrawSpacedSubtabs(settingsPage,new[]{L("Trò chơi","Game"),L("Sao lưu","Backups")});
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
            float step=Mathf.Max(1,(row.width-8-24)/names.Length);
            for(int i=0;i<names.Length;i++)
            {
                var cell=new Rect(row.x+4+i*(step+8),row.y,step,row.height);
                bool active=i==sanctuaryPage;
                bool hover=cell.Contains(Event.current.mousePosition);
                PixelPanel(cell,active?Plum:hover?Color.Lerp(Lilac,Plum,.18f):Lilac);
                PixelArt(new Rect(cell.center.x-12,cell.y+4,24,24),SanctuaryTabIcons[i]);
                GUI.Label(new Rect(cell.x+1,cell.y+28,cell.width-2,cell.height-30),names[i],active?activeTabText:inactiveTabText);
                if(GUI.Button(cell,new GUIContent("",names[i]),GUIStyle.none))page=i;
            }
            return page;
        }

        string GetVesselStatusLabel(int v)
        {
            bool owned=Profile.Vessels.Contains(v);
            if(!owned)return L("Chưa sở hữu","Unowned");
            if(v==(int)Vessel.LittleMoone)
            {
                return Profile.LittleMooneAwake
                    ?L("Đặc biệt · ⭐ Đã thức tỉnh","Special · ⭐ Awakened")
                    :L("Đặc biệt · 🌙 Ngủ sâu","Special · 🌙 Deep Sleep");
            }
            if(v==(int)Vessel.Wanderer)
            {
                return Profile.WandererAwake
                    ?L("Phổ thông · ⭐ Đã thức tỉnh","Common · ⭐ Awakened")
                    :L("Phổ thông · Chưa thức tỉnh","Common · Unawakened");
            }
            return EmberLanguage.Text(Catalog.VesselRarities[v]);
        }

        void DrawVesselPicker()
        {
            var names=Catalog.VesselNames;
            Rect row=GUILayoutUtility.GetRect(180,184,GUILayout.ExpandWidth(true));
            if(Event.current.type!=EventType.Layout)
            {
                const float edge=48;
                if(GUI.Button(new Rect(row.x+4,row.center.y-22,36,44),"‹")){selected=(selected+names.Length-1)%names.Length;if(Profile.CanPlay(selected))SaveProfile();}
                if(GUI.Button(new Rect(row.xMax-40,row.center.y-22,36,44),"›")){selected=(selected+1)%names.Length;if(Profile.CanPlay(selected))SaveProfile();}
                bool mooneSleeping=selected==(int)Vessel.LittleMoone&&!Profile.LittleMooneAwake;
                bool isSleeping=mooneSleeping||!Profile.Vessels.Contains(selected);
                var card=new Rect(row.x+edge,row.y,row.width-2*edge,row.height);
                PixelPanel(card,Lilac);
                PixelArt(new Rect(card.x+7,card.y+7,card.width-14,112),"sanctuary",ScaleMode.ScaleAndCrop);
                if(isSleeping)
                    Panel(new Rect(card.x+7,card.y+7,card.width-14,112),new Color(.04f,.06f,.14f,.35f));
                DrawSpritePreview(new Rect(card.center.x-50,card.y+10,100,100),Frame((Vessel)selected,"idle",(int)(Time.unscaledTime*6)),sleeping:isSleeping);
                if(mooneSleeping&&Profile.Vessels.Contains(selected))
                    PixelArt(new Rect(card.xMax-32,card.y+12,20,20),"awakening");
                UiLabel(new Rect(card.x+10,card.y+120,card.width-20,27),names[selected],label);
                UiLabel(new Rect(card.x+10,card.y+150,card.width-20,27),
                    GetVesselStatusLabel(selected)+"   ·   "+(selected+1)+"/"+names.Length,small);
            }

            GUILayout.Space(8);
            if(UiButton(vesselProfileOpen?L("Đóng hồ sơ","Close profile"):L("Đọc hồ sơ","Read profile"),GUILayout.Height(PlayActionHeight)))vesselProfileOpen=!vesselProfileOpen;
            if(!vesselProfileOpen)return;
            GUILayout.Space(8);
            UiText(L("Chỉ số Bản Ngã (cố định · 10 cấp)","Vessel stats (fixed · 10 tiers)"),label);
            DrawVesselStatBars((Vessel)selected);
            GUILayout.Space(6);
            UiText(L("Kỹ năng","Skill"),label);
            if(selected==(int)Vessel.Wanderer)
            {
                if(Profile.WandererAwake)
                {
                    UiText(L("100 HP • 100 thể lực • ⭐ Gia Tốc Gió Đêm: Tăng 45% tốc chạy cho bản thân và đồng đội trong 8 ô trong 10 giây. Hồi chiêu 22 giây.","100 HP • 100 Stamina • ⭐ Swiftwind Aura: Grants +45% movement speed to self and allies within 8 tiles for 10s. Cooldown 22s."),label);
                    UiText(L("Thước 2 · Bán kính tăng tốc đồng đội: 8.0 ô   ·   Hồi chiêu: 22s","Range 2 · Ally speedup radius: 8.0 tiles   ·   Cooldown: 22s"),small);
                }
                else
                {
                    UiText(Catalog.VesselDescriptions[selected],label);
                }
            }
            else if(selected==(int)Vessel.LittleMoone)
            {
                if(Profile.LittleMooneAwake)
                {
                    UiText(L("100 HP • 100 thể lực • ⭐ Mưa Sao Băng: 12 thiên thạch ánh sáng tự ngắm diện rộng. Hồi chiêu 24 giây.","100 HP • 100 Stamina • ⭐ Starlight Meteors: 12 auto-aimed light meteors. Cooldown 24s."),label);
                    UiText(SkillRangeText((Vessel)selected),small);
                }
                else
                {
                    UiText(L("100 HP • 100 thể lực • 🌙 Ngủ sâu: Cần dùng 1 Đá Ký Ức Thức Tỉnh để đánh thức Bản Ngã này xuất trận.","100 HP • 100 Stamina • 🌙 Deep Sleep: Requires 1 Awakening Stone to awaken this Vessel for battle."),label);
                    UiText(L("Bản ngã đặc biệt đang ngủ say, màu sắc chìm vào màn đêm cho đến khi được đánh thức.","Special Vessel is in deep slumber, shrouded in night veil until awakened."),small);
                }
            }
            else
            {
                UiText(Catalog.VesselDescriptions[selected],label);
                if(selected!=(int)Vessel.Wanderer)UiText(SkillRangeText((Vessel)selected),small);
            }
            GUILayout.Space(8);
            UiText(L("Truyện nền","Story"),label);
            UiText((EmberLanguage.English?LoreEn:LoreVi)[selected],label);
            GUILayout.Space(8);
        }

        void DrawVesselStatBars(Vessel vessel)
        {
            var stats=Catalog.StatsFor(vessel);
            string[] names={
                L("Tốc độ","Speed"),
                L("Phòng thủ","Defense"),
                L("Kháng sợ","Dread Res"),
                L("Hỏa lực","Power"),
                L("Thể lực","Endurance"),
                L("Khai thác","Gathering")
            };
            int[] values={stats.Speed,stats.Defense,stats.DreadResist,stats.Power,stats.Endurance,stats.Gathering};
            Color[] colors={
                new Color(.24f,.72f,.86f),
                new Color(.85f,.64f,.20f),
                new Color(.56f,.38f,.84f),
                new Color(.88f,.34f,.38f),
                new Color(.26f,.74f,.52f),
                new Color(.82f,.52f,.28f)
            };
            var raw=GUILayoutUtility.GetRect(220,164,GUILayout.ExpandWidth(true));
            var box=new Rect(raw.x+4,raw.y,Mathf.Max(180,raw.width-8),raw.height);
            PixelPanel(box,Lilac);
            UiLabel(new Rect(box.x+10,box.y+6,box.width-20,20),L("HP 100 · Thể lực 100 · Không thể nâng cấp","100 HP · 100 Stamina · Hardcore fixed"),small);
            for(int i=0;i<names.Length;i++)
            {
                float rowY=box.y+30+i*21;
                UiLabel(new Rect(box.x+10,rowY,86,20),names[i],small);
                float barX=box.x+98;
                float barW=Mathf.Max(80,box.width-148);
                const int tiers=10;
                const float segGap=3;
                float segW=(barW-segGap*(tiers-1))/tiers;
                for(int t=0;t<tiers;t++)
                {
                    var seg=new Rect(barX+t*(segW+segGap),rowY+4,segW,12);
                    Panel(seg,t<values[i]?colors[i]:new Color(.78f,.73f,.88f));
                }
                UiLabel(new Rect(box.xMax-44,rowY,38,20),values[i]+"/10",small);
            }
        }

        void DrawMultiplayerSetup()
        {
            usePublicBroker=DrawSpacedChoices(usePublicBroker?1:0,new[]{"LAN / VPN",L("Internet · mã phòng","Internet · room code")},34)==1;
            if(usePublicBroker)
            {
                UiText(L("Máy chủ kết nối","Connection server"),small);
                brokerAddress=GUILayout.TextField(brokerAddress);
                routeMode=(RouteMode)DrawSpacedChoices((int)routeMode,new[]{"Auto","P2P","Relay"},32);
            }
            UiText("Mật khẩu phòng",small);
            password=GUILayout.PasswordField(password,'•');
            UiText(L("Tạo phòng","Create room"),label);
            roomName=GUILayout.TextField(roomName,32);
            UiText(L("Số người tối đa","Maximum players"),small);
            roomCapacity=DrawSpacedChoices(roomCapacity-1,new[]{"1","2","3","4"},34)+1;
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
                Rect rawCard=GUILayoutUtility.GetRect(180,113,GUILayout.ExpandWidth(true));
                Rect card=new Rect(rawCard.x+4,rawCard.y,Mathf.Max(160,rawCard.width-8),rawCard.height);
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
            UiText(L("Bản Ngã sở hữu","Owned vessels")+"   "+Profile.Vessels.Length+"/"+Catalog.VesselNames.Length,label);
            GUILayout.Space(6);
            int columns=Screen.width<640?2:3;
            const float margin=4, gap=8;
            for(int first=0;first<Catalog.VesselNames.Length;first+=columns)
            {
                Rect row=GUILayoutUtility.GetRect(180,114,GUILayout.ExpandWidth(true));
                float innerW=Mathf.Max(80,row.width-2*margin);
                float cellW=(innerW-gap*(columns-1))/columns;
                for(int col=0;col<columns&&first+col<Catalog.VesselNames.Length;col++)
                {
                    int vessel=first+col;
                    bool owned=Profile.Vessels.Contains(vessel);
                    bool playable=Profile.CanPlay(vessel);
                    bool isSelected=selected==vessel;
                    var card=new Rect(row.x+margin+col*(cellW+gap),row.y,cellW,row.height);
                    PixelPanel(card,isSelected?Plum:owned?Lilac:Paper);
                    if(owned)
                    {
                        bool vesselSleeping=(vessel==(int)Vessel.LittleMoone&&!Profile.LittleMooneAwake);
                        DrawSpritePreview(new Rect(card.center.x-28,card.y+6,56,56),Frame((Vessel)vessel,"idle",0),sleeping:vesselSleeping);
                    }
                    else
                        PixelArt(new Rect(card.center.x-22,card.y+12,44,44),"token");
                    if(owned&&!playable)PixelArt(new Rect(card.xMax-20,card.y+6,15,15),"awakening");
                    var nameStyle=isSelected?activeTabText:inactiveTabText;
                    GUI.Label(new Rect(card.x+4,card.y+64,card.width-8,26),EmberLanguage.Text(Catalog.VesselNames[vessel]),nameStyle);
                    string state=isSelected?L("Đang chọn","Selected"):playable?(Profile.IsAwake(vessel)?L("⭐ Đã thức tỉnh","⭐ Awakened"):EmberLanguage.Text(Catalog.VesselRarities[vessel])):owned?L("Ngủ sâu","Deep Sleep"):L("Chưa mở","Locked");
                    GUI.Label(new Rect(card.x+4,card.y+88,card.width-8,18),state,nameStyle);
                    if(owned&&GUI.Button(card,GUIContent.none,GUIStyle.none)){selected=vessel;SaveProfile();}
                }
                GUILayout.Space(8);
            }
            GUILayout.Space(4);
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
            if(network)return;
            if(debugSession)
            {
                UiText(L("Đang ở Sandbox tạm (F8) · Không ghi vào save thật.","Temporary Sandbox (F8) · Real save writes disabled."),small);
                if(UiButton(L("Thoát Sandbox về save thật","Exit Sandbox to real save")))
                {
                    debugSession=false;
                    LoadProfile();
                    selected=Profile.CanPlay(Profile.SelectedVessel)?Profile.SelectedVessel:0;
                    RefreshSavedRunPreview();
                    notice=L("Đã quay lại save thật (profile.ember).","Returned to real save (profile.ember).");
                }
                GUILayout.Space(6);
                return;
            }
            if(!Application.isEditor&&!Debug.isDebugBuild)return;
            GUILayout.BeginHorizontal();
            if(UiButton(L("Test · Mở hết Bản Ngã (Lưu)","Test · Unlock vessels (Save)")))
            {
                Profile.Vessels=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Range(0,Catalog.VesselNames.Length));
                Profile.LittleMooneAwake=true;
                Profile.WandererAwake=true;
                SaveProfile();
                notice=L("Đã mở toàn bộ Bản Ngã và lưu vào profile.ember.","Unlocked all vessels and saved to profile.ember.");
                Trace("debug.unlock_vessels_saved",notice);
            }
            GUILayout.Space(6);
            if(UiButton(L("Test · +Tài nguyên (Lưu)","Test · +Resources (Save)")))
            {
                Profile.Ash+=1000;
                Profile.Memory+=50;
                Profile.Tokens+=20;
                Profile.AwakeningStones+=5;
                SaveProfile();
                notice=L("Đã cộng +1000 Tro, +50 Ký ức, +20 Định Mệnh, +5 Thức tỉnh (đã lưu).","Added test currencies and saved to profile.ember.");
                Trace("debug.grant_currencies_saved",notice);
            }
            GUILayout.EndHorizontal();
            if(selected==(int)Vessel.LittleMoone&&Profile.Vessels.Contains((int)Vessel.LittleMoone))
            {
                GUILayout.Space(4);
                if(UiButton(Profile.LittleMooneAwake?L("Test · Đặt Moone vào Ngủ Sâu (Lưu)","Test · Put Moone to Deep Sleep (Save)"):L("Test · Thức Tỉnh Moone (Lưu)","Test · Awaken Moone (Save)")))
                {
                    Profile.LittleMooneAwake=!Profile.LittleMooneAwake;
                    SaveProfile();
                }
            }
            GUILayout.Space(4);
        }

        void DrawSanctuaryBackdrop()
        {
            Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.95f,.92f,.98f));
            PixelArt(new Rect(0,8,Screen.width,Screen.height-8),"sanctuary_paper",ScaleMode.StretchToFill);
            Panel(new Rect(0,0,Screen.width,8),Plum);
            Panel(new Rect(0,120,Screen.width,1),new Color(.81f,.74f,.89f));
        }

        void DrawSanctuaryStage(Rect area)
        {
            PixelArt(area,"sanctuary",ScaleMode.ScaleAndCrop);
            bool mooneSleeping=selected==(int)Vessel.LittleMoone&&!Profile.LittleMooneAwake;
            bool isSleeping=mooneSleeping||!Profile.Vessels.Contains(selected);
            if(isSleeping)
                Panel(area,new Color(.04f,.06f,.14f,.30f));
            float horizon=area.y+area.height*.65f;

            float cx=area.center.x;
            PixelPanel(new Rect(area.x,area.y,area.width,49),Paper);
            PixelPanel(new Rect(area.x,area.yMax-66,area.width,66),Paper);
            UiLabel(new Rect(area.x+18,area.y+15,area.width-36,30),GetVesselStatusLabel(selected),small);
            float size=Mathf.Min(area.width*.62f,area.height*.56f);
            DrawSpritePreview(new Rect(cx-size/2,horizon-size*.80f+Mathf.Sin(Time.unscaledTime*2)*3,size,size),
                Frame((Vessel)selected,"idle",(int)(Time.unscaledTime*6)),
                sleeping:isSleeping);
            string stageName=Catalog.VesselNames[selected];
            if(mooneSleeping&&Profile.Vessels.Contains(selected))
                stageName+="   ·   "+L("🌙 Ngủ sâu","🌙 Deep Sleep");
            UiLabel(new Rect(area.x+18,area.yMax-65,area.width-36,45),stageName,label);
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

        void DrawSpritePreview(Rect rect,Sprite sprite,bool sleeping=false)
        {
            if(sprite==null||Event.current.type!=EventType.Repaint)return;
            var uv=sprite.rect;
            var texture=sprite.texture;
            rect=FitPixelRect(rect,uv.width,uv.height);
            Color prevColor=GUI.color;
            if(sleeping)
            {
                // Pass 1: Mất màu sắc, hóa thành tông đen/xám tối (desaturated dark grayscale)
                GUI.color=new Color(.30f,.32f,.40f,.90f);
                GUI.DrawTextureWithTexCoords(rect,texture,new Rect(uv.x/texture.width,uv.y/texture.height,uv.width/texture.width,uv.height/texture.height));
                // Pass 2: Áp thêm lớp filter xanh mờ ánh trăng đêm bao phủ hình thể (translucent night sleep filter overlay)
                GUI.color=new Color(.08f,.16f,.34f,.55f);
                GUI.DrawTextureWithTexCoords(rect,texture,new Rect(uv.x/texture.width,uv.y/texture.height,uv.width/texture.width,uv.height/texture.height));
            }
            else
            {
                GUI.DrawTextureWithTexCoords(rect,texture,new Rect(uv.x/texture.width,uv.y/texture.height,uv.width/texture.width,uv.height/texture.height));
            }
            GUI.color=prevColor;
        }

        string[] SummonNames(int pool)=>pool==0?Catalog.VesselNames:pool==1?Catalog.HatNames:Catalog.PetNames;

        Sprite SummonSprite(int pool,int index)
        {
            if(pool==0)return Frame((Vessel)index,"idle",0);
            return Primitive(pool==1
                ?new[]{"hat_straw","hat_witch","hat_candle","hat_jester"}[index]
                :new[]{"pet_firefly","pet_bat","pet_cat","pet_ghost"}[index]);
        }

        void DrawCurrencyChip(Rect rect,string icon,string name,int amount,string tooltip)
        {
            PixelPanel(rect,Paper);
            PixelArt(new Rect(rect.x+7,rect.y+7,22,22),icon);
            UiLabel(new Rect(rect.x+33,rect.y+3,rect.width-38,16),name,small);
            UiLabel(new Rect(rect.x+33,rect.y+17,rect.width-38,19),amount.ToString(),sanctuaryLabel);
            GUI.Label(rect,new GUIContent("",tooltip),GUIStyle.none);
        }

        void DrawSanctuaryHeaderCurrencies(float leftX,float totalWidth)
        {
            string[] icons={"ash","memory","token","awakening"};
            string[] names={L("Tro","Ash"),L("Ký ức","Memory"),L("Định Mệnh","Tokens"),L("Thức tỉnh","Awaken")};
            int[] amounts={Profile.Ash,Profile.Memory,Profile.Tokens,Profile.AwakeningStones};
            string[] tips={
                L("Tro Tàn · Mở/nâng Đặc quyền hoặc đổi 100 Tro → 1 Đá Định Mệnh","Ash · Unlock/upgrade Perks or exchange 100 Ash → 1 Void Token"),
                L("Mảnh Ký Ức · Mở ô Đặc quyền và công thức chế tạo","Memory · Unlock Perk slots and crafting recipes"),
                L("Đá Định Mệnh · Dùng 3 đá để Triệu hồi Bản Ngã, Mũ, Thú cưng","Void Tokens · Spend 3 to Summon Vessels, Hats, Pets"),
                L("Đá Ký Ức Thức Tỉnh · Đánh thức Little Moone (rơi 30% khi thắng hoặc 25% mỗi Boss hạ gục)","Awakening Stones · Awaken Little Moone (30% on win or 25% per Boss defeated)")
            };
            const float chipW=130, chipH=38, gap=8;
            float y=58;
            for(int i=0;i<2;i++)
                DrawCurrencyChip(new Rect(leftX+i*(chipW+gap),y,chipW,chipH),icons[i],names[i],amounts[i],tips[i]);
            float rightStart=leftX+totalWidth-(chipW*2+gap);
            for(int i=0;i<2;i++)
                DrawCurrencyChip(new Rect(rightStart+i*(chipW+gap),y,chipW,chipH),icons[i+2],names[i+2],amounts[i+2],tips[i+2]);
        }

        void DrawSanctuaryCompactCurrencies()
        {
            var raw=GUILayoutUtility.GetRect(180,40,GUILayout.ExpandWidth(true));
            string[] icons={"ash","memory","token","awakening"};
            string[] names={L("Tro","Ash"),L("Ký ức","Memory"),L("Định Mệnh","Tokens"),L("Thức tỉnh","Awaken")};
            int[] amounts={Profile.Ash,Profile.Memory,Profile.Tokens,Profile.AwakeningStones};
            string[] tips={
                L("Tro Tàn · Mở/nâng Đặc quyền hoặc đổi Đá Định Mệnh","Ash · Unlock/upgrade Perks or exchange for Void Tokens"),
                L("Mảnh Ký Ức · Mở ô Đặc quyền và công thức chế tạo","Memory · Unlock Perk slots and crafting recipes"),
                L("Đá Định Mệnh · Dùng 3 đá để Triệu hồi","Void Tokens · Spend 3 to Summon"),
                L("Đá Ký Ức Thức Tỉnh · Đánh thức Little Moone (rơi 30% khi thắng hoặc 25% mỗi Boss hạ gục)","Awakening Stones · Awaken Little Moone (30% on win or 25% per Boss defeated)")
            };
            const float margin=4, gap=6;
            float innerW=Mathf.Max(120,raw.width-2*margin);
            float step=(innerW-gap*3)/4;
            for(int i=0;i<4;i++)
                DrawCurrencyChip(new Rect(raw.x+margin+i*(step+gap),raw.y,step,38),icons[i],names[i],amounts[i],tips[i]);
            GUILayout.Space(6);
        }

        void DrawSummoning()
        {
            float elapsed=Time.unscaledTime-summonStart;
            bool spinning=summonPrize>=0&&elapsed<SummonDuration;
            GUI.enabled=!spinning;
            summonPool=DrawSpacedChoices(summonPool,new[]{L("Bản Ngã","Vessels"),L("Mũ","Hats"),L("Thú cưng","Pets")},36);
            GUI.enabled=true;
            GUILayout.Space(8);

            var rawHero=GUILayoutUtility.GetRect(0,166,GUILayout.ExpandWidth(true));
            var hero=new Rect(rawHero.x+4,rawHero.y,Mathf.Max(120,rawHero.width-8),rawHero.height);
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
                    spinning?L("Đang triệu hồi…","Summoning…"):poolNames[shown],sanctuaryLabel);
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
            GUILayout.Space(10);
            var names=SummonNames(summonPool);
            const float margin=4, gap=8;
            for(int first=0;first<names.Length;first+=3)
            {
                Rect row=GUILayoutUtility.GetRect(180,110,GUILayout.ExpandWidth(true));
                float innerW=Mathf.Max(90,row.width-2*margin);
                float cellW=(innerW-gap*2)/3;
                for(int col=0;col<3&&first+col<names.Length;col++)
                {
                    int i=first+col;
                    var card=new Rect(row.x+margin+col*(cellW+gap),row.y,cellW,row.height);
                    PixelPanel(card,Lilac);
                    DrawSpritePreview(new Rect(card.center.x-28,card.y+6,56,56),SummonSprite(summonPool,i));
                    UiLabel(new Rect(card.x+6,card.y+62,card.width-12,24),names[i],small);
                    string odds=summonPool==0?EmberLanguage.Text(Catalog.VesselRarities[i])+" · "+Catalog.VesselDrawWeights[i]+"%":"25%";
                    GUI.Label(new Rect(card.x+6,card.y+84,card.width-12,20),odds,small);
                }
                GUILayout.Space(8);
            }
        }
    }
}
