using System.Linq;
using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        static readonly string[] ActionIcons={"torch","flare","sedative","revival"};
        static readonly Color FieldPaper=new Color(.085f,.068f,.14f,.97f);

        string SkillRangeText(Vessel vessel)
        {
            string distance=Catalog.SkillRange(vessel).ToString("0.#")+L(" ô"," tiles");
            return vessel==Vessel.SpaceWraith
                ?L("Thước 2 · Bán kính kéo đồng đội: ","Range 2 · Ally pickup radius: ")+distance
                :L("Thước 2 · Tầm cast: ","Range 2 · Cast radius: ")+distance;
        }

        int selectedItem;
        static readonly string[] ItemNamesVi={"Đuốc","Pháo sáng","An thần","Đá hồi sinh"};
        static readonly string[] ItemNamesEn={"Torch","Flare","Sedative","Revival stone"};

        bool CanUseSelectedItem(Player p)
        {
            return heldSlot>=2&&heldSlot<=5&&p!=null&&!p.Ghost&&!p.Extracted&&p.Items[selectedItem]>0&&
                (selectedItem!=3||(p.Pos.Length<=5&&System.Array.Exists(State.Players,q=>q.Ghost)));
        }

        GUIStyle overheadName,overheadGlow;

        void DrawStructureBars()
        {
            if(Event.current.type!=EventType.Repaint||State?.Structures==null)return;
            V2 focus=CameraTarget()?.Pos??new V2();
            foreach(var s in State.Structures)
            {
                if(s.Kind>2||s.Life<=0||V2.Distance(s.Pos,focus)>23)continue;
                if(s.Kind==2)
                {
                    // Wall / Fence health bar (HP 150)
                    if(s.HP>=150f||s.HP<=0)continue;
                    float ratio=Mathf.Clamp01(s.HP/150f);
                    Vector2 anchor=ScreenPos(s.Pos+new V2(0,.78f));
                    float x=Mathf.Round(anchor.x), y=Mathf.Round(anchor.y);
                    Panel(new Rect(x-20,y-7,40,7),new Color(.05f,.04f,.09f,.92f));
                    Color fill=ratio<.3f?Coral:new Color(.82f,.62f,.38f);
                    Panel(new Rect(x-19,y-6,38*ratio,5),fill);
                }
                else
                {
                    float maxLife=s.MaxLife>0?s.MaxLife:(s.Kind==0?45f:12f);
                    float ratio=Mathf.Clamp01(s.Life/maxLife);
                    Vector2 anchor=ScreenPos(s.Pos+new V2(0,.62f));
                    float x=Mathf.Round(anchor.x), y=Mathf.Round(anchor.y);
                    Panel(new Rect(x-20,y-7,40,7),new Color(.05f,.04f,.09f,.92f));
                    Color fill=ratio<.25f?Coral:s.Kind==1?new Color(1f,.56f,.36f):Gold;
                    Panel(new Rect(x-19,y-6,38*ratio,5),fill);
                }
            }
        }

        void DrawInteractPrompt(Player p)
        {
            V2 targetPos;
            string caption;
            if(State.Phase==Phase.Extraction&&p.Pos.Length<3)
            {
                targetPos=new V2(0,1.2f);
                caption=L("Rút lui","Extract");
            }
            else
            {
                var chest=State.Structures.Where(s=>s.Kind==4&&V2.Distance(s.Pos,p.Pos)<2)
                    .OrderBy(s=>V2.Distance(s.Pos,p.Pos)).FirstOrDefault();
                if(chest!=null)
                {
                    targetPos=chest.Pos+new V2(0,.65f);
                    caption=L("Rương chung","Shared chest");
                }
                else
                {
                    var node=State.Nodes.Where(n=>V2.Distance(n.Pos,p.Pos)<2.4f)
                        .OrderBy(n=>V2.Distance(n.Pos,p.Pos)).FirstOrDefault();
                    if(node!=null)
                    {
                        string resName=EmberLanguage.Text(Catalog.ResourceNames[(int)node.Resource]);
                        bool needsPick=!node.Dropped&&!p.HasPickaxe&&p.Weapon!=Weapon.Pickaxe;
                        string verb=node.Dropped?L("Nhặt","Pick up"):node.Resource==Resource.Wood?L("Chặt","Chop"):L("Đào","Mine");
                        targetPos=node.Pos+new V2(0,node.Dropped?.55f:.75f);
                        caption=needsPick?L("Cần Cuốc để khai thác","Requires Pickaxe"):verb+" · "+resName+" ×"+node.Amount;
                    }
                    else
                    {
                        // Check for natural trees (tile 3) and rock cliffs (tile 2)
                        int size=State.Size;
                        int px=(int)Mathf.Floor(p.Pos.X+size/2f);
                        int py=(int)Mathf.Floor(p.Pos.Y+size/2f);
                        int bestTx=-1,bestTy=-1;float bestDist=float.MaxValue;byte targetTile=0;
                        byte[] tiles=currentTiles??(host?World?.Tiles:null);
                        if(tiles!=null)
                        {
                            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                            {
                                int tx=px+dx,ty=py+dy;
                                if(tx<0||ty<0||tx>=size||ty>=size)continue;
                                int idx=ty*size+tx;
                                if(idx<0||idx>=tiles.Length)continue;
                                byte t=tiles[idx];
                                if(t==3||t==2)
                                {
                                    V2 blockPos=new V2(tx-size/2f+.5f,ty-size/2f+.5f);
                                    float d=V2.Distance(p.Pos,blockPos);
                                    if(d<2.2f&&d<bestDist){bestDist=d;bestTx=tx;bestTy=ty;targetTile=t;}
                                }
                            }
                        }
                        if(bestTx>=0)
                        {
                            bool isTree=targetTile==3;
                            targetPos=new V2(bestTx-size/2f+.5f,bestTy-size/2f+.5f);
                            bool needsPick=!p.HasPickaxe&&p.Weapon!=Weapon.Pickaxe;
                            string verb=isTree?L("Chặt","Chop"):L("Đào","Mine");
                            string resName=isTree?L("Gỗ","Wood"):L("Quặng","Ore");
                            caption=needsPick?L("Cần Cuốc để khai thác","Requires Pickaxe"):verb+" · "+resName+" ×2";
                        }
                        else return;
                    }
                }
            }

            Vector2 screen=ScreenPos(targetPos);
            string full="[E] "+caption;
            float w=Mathf.Clamp(small.CalcSize(new GUIContent(full)).x+26,104,240);
            float h=26;
            var box=new Rect(Mathf.Round(screen.x-w/2),Mathf.Round(screen.y-h-8),w,h);
            PixelPanel(box,new Color(.12f,.09f,.20f,.94f));
            var keyBox=new Rect(box.x+4,box.y+3,22,h-6);
            Panel(keyBox,Gold);
            var oldColor=GUI.contentColor;
            GUI.contentColor=Color.black;
            GUI.Label(new Rect(keyBox.x+6,keyBox.y-1,16,20),"E",small);
            GUI.contentColor=new Color(1f,.96f,.82f);
            GUI.Label(new Rect(keyBox.xMax+6,box.y+3,box.width-34,20),caption,small);
            GUI.contentColor=oldColor;
            if(GUI.Button(box,GUIContent.none,GUIStyle.none))
            {
                if(ShouldAutoEquipPickaxe(p))EquipSlot(8);
                intent.Interact=true;
            }
        }

        void DrawOverheadStatus(Player player)
        {
            if(!renderers.TryGetValue("player"+player.Id,out var body)||!body.enabled||body.sprite==null)return;
            float top=body.bounds.max.y;
            if(renderers.TryGetValue("hat"+player.Id,out var hat)&&hat.enabled&&hat.sprite!=null)
                top=Mathf.Max(top,hat.bounds.max.y);
            V2 pVis=VisualPlayerPos(player);
            Vector2 anchor=ScreenPos(new V2(pVis.X,top));
            float x=Mathf.Round(anchor.x), y=Mathf.Round(anchor.y);
            if(Event.current.type!=EventType.Repaint)return;
            if(overheadName==null)
            {
                overheadName=new GUIStyle
                {
                    font=GUI.skin.font,fontSize=13,fontStyle=FontStyle.Bold,
                    alignment=TextAnchor.MiddleCenter,clipping=TextClipping.Clip,
                    normal={textColor=Color.black}
                };
                overheadGlow=new GUIStyle(overheadName){normal={textColor=Cyan}};
            }
            var text=new GUIContent(player.Name);
            float nameWidth=Mathf.Clamp(overheadName.CalcSize(text).x+14,52,170);
            var name=new Rect(Mathf.Round(x-nameWidth/2),y-43,nameWidth,23);
            var oldColor=GUI.color;
            var oldContentColor=GUI.contentColor;
            bool oldEnabled=GUI.enabled;
            GUI.enabled=true;
            GUI.color=GUI.contentColor=Color.white;
            for(int dx=-1;dx<=1;dx++)
            for(int dy=-1;dy<=1;dy++)
                if(dx!=0||dy!=0)
                    overheadGlow.Draw(new Rect(name.x+dx,name.y+dy,name.width,name.height),text,false,false,false,false);
            overheadName.Draw(name,text,false,false,false,false);
            GUI.enabled=oldEnabled;
            GUI.color=oldColor;
            GUI.contentColor=oldContentColor;
            Panel(new Rect(x-25,y-17,50,13),new Color(.05f,.04f,.09f,.92f));
            Panel(new Rect(x-24,y-16,48*Mathf.Clamp01(player.HP/Mathf.Max(1,player.MaxHP)),5),player.Ghost?Color.gray:Cyan);
            Panel(new Rect(x-24,y-9,48*Mathf.Clamp01(player.Dread/100),4),new Color(.75f,.35f,.7f));
            if(player.Reloading&&player.MaxAmmo>0)
            {
                float reloadMax=Mathf.Max(.1f,Catalog.WeaponReloadTime(player.ActiveWeapon));
                float progress=1f-Mathf.Clamp01(player.ReloadTimer/reloadMax);
                Panel(new Rect(x-25,y-2,50,6),new Color(.05f,.04f,.09f,.92f));
                Panel(new Rect(x-24,y-1,48*progress,4),Gold);
            }
        }

        Texture2D partyBubble;
        GUIStyle partyName;

        bool CompactFieldHud=>Screen.width<760;
        float PartyPortraitY=>CompactFieldHud?163:126;
        float ObjectiveCardY=>network?PartyPortraitY+76:CompactFieldHud?138:99;

        void DrawPartyBubbles()
        {
            if(partyBubble==null)
            {
                const int size=40;
                partyBubble=new Texture2D(size,size,TextureFormat.RGBA32,false)
                    {filterMode=FilterMode.Point,hideFlags=HideFlags.HideAndDontSave};
                var pixels=new Color[size*size];
                for(int y=0;y<size;y++)
                for(int x=0;x<size;x++)
                {
                    float radius=new Vector2(x-19.5f,y-19.5f).magnitude;
                    if(radius<19.5f)pixels[y*size+x]=radius>=17.5f?new Color(.65f,.51f,.85f):FieldPaper;
                }
                partyBubble.SetPixels(pixels);
                partyBubble.Apply(false,true);
                partyName=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,wordWrap=false,clipping=TextClipping.Clip};
            }
            var members=State.Players??System.Array.Empty<Player>();
            var offline=State.Disconnected??System.Array.Empty<DisconnectedSeat>();
            int count=Mathf.Min(members.Length+offline.Length,4);
            float step=Mathf.Min(102,(Screen.width-28f)/Mathf.Max(1,count));
            int i=0;
            bool spectating=Local!=null&&Local.Ghost;
            for(int slot=1;slot<=4;slot++)
            {
                Player teammate=null;
                foreach(var member in members)if(member.PartySlot==slot){teammate=member;break;}
                if(teammate!=null)
                {
                    float x=14+step*(i++ +.5f), y=PartyPortraitY;
                    var center=new Vector2(x,y);
                    var hit=new Rect(x-30,y-30,60,60);
                    RegisterHudRect(new Rect(x-step*.5f,y-32,step,88));
                    if(Event.current.type==EventType.Repaint)
                    {
                        GUI.DrawTexture(hit,partyBubble);
                        DrawSpritePreview(new Rect(x-23,y-28,46,46),teammate.Ghost?Primitive("pet_ghost"):Frame(teammate.Vessel,"idle",0));
                        DrawPartyArc(center,26,teammate.HP/Mathf.Max(1,teammate.MaxHP),Cyan);
                        DrawPartyArc(center,31,teammate.Dread/100,Coral);
                    }
                    string name="P"+teammate.PartySlot+" · "+teammate.Name;
                    string stats=name+"\nHP "+Mathf.CeilToInt(teammate.HP)+"/"+Mathf.CeilToInt(teammate.MaxHP)+
                        " ( "+L("vòng xanh","cyan arc")+" )\n"+L("Hoảng Loạn ","Dread ")+Mathf.CeilToInt(teammate.Dread)+
                        "% ( "+L("vòng hồng","pink arc")+" )";
                    if(spectating&&!teammate.Ghost&&!teammate.Extracted&&teammate.Id!=localId)
                    {
                        if(GUI.Button(hit,new GUIContent("",stats),GUIStyle.none))spectateId=teammate.Id;
                    }
                    else GUI.Label(hit,new GUIContent("",stats),GUIStyle.none);
                    GUI.Label(new Rect(x-step/2,y+34,step,20),new GUIContent(name,stats),partyName);
                    continue;
                }
                DisconnectedSeat seat=null;
                foreach(var d in offline)if(d.PartySlot==slot){seat=d;break;}
                if(seat==null)continue;
                {
                    float x=14+step*(i++ +.5f), y=PartyPortraitY;
                    var hit=new Rect(x-30,y-30,60,60);
                    RegisterHudRect(new Rect(x-step*.5f,y-32,step,88));
                    if(Event.current.type==EventType.Repaint)
                    {
                        GUI.color=new Color(.55f,.55f,.62f,.78f);
                        GUI.DrawTexture(hit,partyBubble);
                        DrawSpritePreview(new Rect(x-23,y-28,46,46),Frame(seat.Vessel,"idle",0));
                        GUI.color=Color.white;
                    }
                    string name="P"+seat.PartySlot+" · "+L("Mất mạng","Offline");
                    string tip="P"+seat.PartySlot+" · "+seat.Name+"\n"+L("Mất kết nối — Hành trang đang được giữ tại Hải Đăng.","Disconnected — Gear preserved at the Beacon.");
                    GUI.Label(hit,new GUIContent("",tip),GUIStyle.none);
                    GUI.Label(new Rect(x-step/2,y+34,step,20),new GUIContent(name,tip),partyName);
                }
            }
        }

        void DrawPartyArc(Vector2 center,float radius,float ratio,Color color)
        {
            const int segments=16;
            float filled=Mathf.Clamp01(ratio)*segments;
            for(int i=0;i<segments;i++)
            {
                float a=(24+132f*i/segments)*Mathf.Deg2Rad;
                float b=(24+132f*(i+1)/segments)*Mathf.Deg2Rad;
                Vector2 from=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
                Vector2 to=center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius;
                GUI.color=new Color(.16f,.12f,.25f);
                Line(from,to,4);
                if(filled>i)
                {
                    GUI.color=color;
                    Line(from,Vector2.Lerp(from,to,Mathf.Clamp01(filled-i)),3);
                }
            }
            GUI.color=Color.white;
        }

        void DrawObjectiveCard(Player p,float y)
        {
            var card=new Rect(16,y,Mathf.Min(370,Screen.width-32),72);
            bool blackout=State.Blackout>0;
            PixelPanel(card,blackout?new Color(.31f,.11f,.21f):FieldPaper);
            string heading,hint;
            float progress;
            string icon;
            if(blackout)
            {
                heading=L("Mất ánh sáng","Blackout")+" · "+Mathf.CeilToInt(Mathf.Max(0,10-State.Blackout))+"s";
                hint=L("Khôi phục nguồn sáng","Restore a light source");
                progress=1-State.Blackout/10;
                icon="torch";
            }
            else if(p.Ghost)
            {
                heading=L("Đã gục · Đang theo dõi","Downed · Spectating");
                hint=L("Chờ đồng đội hồi sinh tại hải đăng (-100 NL trụ)","Wait for an ally to revive you at the beacon (-100 energy)");
                progress=0;
                icon="revival";
            }
            else if(State.Phase==Phase.Eclipse)
            {
                heading=L("Nhật thực","Eclipse")+" · "+Mathf.CeilToInt(State.Wave)+"s";
                hint=L("Bảo vệ hải đăng","Defend the beacon")+" · HP "+Mathf.CeilToInt(State.BeaconHP)+"/600";
                progress=1-State.Wave/90;
                icon="umbra";
            }
            else if(State.Phase==Phase.Extraction)
            {
                heading=L("Cổng đã mở","Exit open");
                hint=L("Về cổng ở hải đăng rồi bấm E","Return to the beacon and press E");
                progress=1;
                icon="awakening";
            }
            else
            {
                string diffName=(EmberLanguage.English?Catalog.DifficultyNamesEn:Catalog.DifficultyNamesVi)[Mathf.Clamp((int)State.Difficulty,0,2)];
                float target=Mathf.Max(1,State.ChargeTarget);
                heading=L("Nạp trụ","Beacon")+" ("+diffName+") · "+Mathf.RoundToInt(State.Charge)+"/"+Mathf.RoundToInt(target);
                hint=L("Mang lõi về trụ · B để nạp (+25 NL/lõi)","Bring cores to beacon · B to deposit (+25/core)");
                progress=State.Charge/target;
                icon="core";
            }

            var iconRect=new Rect(card.x+12,card.y+12,32,32);
            if(icon=="torch")DrawSpritePreview(iconRect,Primitive("torch"));
            else PixelArt(iconRect,icon);
            UiLabel(new Rect(card.x+54,card.y+6,card.width-62,25),heading,label);
            UiLabel(new Rect(card.x+54,card.y+31,card.width-62,28),hint,small);
            var rail=new Rect(card.x+12,card.yMax-10,card.width-24,4);
            Panel(rail,new Color(.31f,.25f,.45f));
            rail.width*=Mathf.Clamp01(progress);
            Panel(rail,blackout?Coral:Gold);
        }

        void DrawFieldNotices(Player player,float y)
        {
            if(Event.current.type!=EventType.Repaint)return;
            if(!string.IsNullOrEmpty(State.Event)&&State.Tick-State.EventTick<240)
            {
                string message=(State.EventActor??"")+EmberLanguage.Text(State.Event)+(State.EventTarget??"");
                y+=DrawFieldNotice(y,"beacon",message);
            }

            if(player.Pet==(int)Pet.Bat&&State.Enemies.Any(e=>e.HP>0&&V2.Distance(e.Pos,player.Pos)<10))
            {
                DrawFieldNotice(y,"pet_bat",L("Dơi phát hiện quái ở gần","Bat spotted a nearby enemy"));
            }
            else if(player.Pet==(int)Pet.CrystalCat&&(int)State.Time%30<5)
            {
                var core=State.Nodes.Where(n=>n.Resource==Resource.Core)
                    .OrderBy(n=>V2.Distance(n.Pos,player.Pos)).FirstOrDefault();
                if(core==null)return;
                var direction=(core.Pos-player.Pos).Normal;
                var at=ScreenPos(player.Pos);
                GUI.color=Cyan;
                Line(at+new Vector2(0,-25),at+new Vector2(direction.X*80,-direction.Y*80-25),3);
                GUI.color=Color.white;
                DrawFieldNotice(y,"pet_cat",L("Lõi gần nhất · ","Nearest core · ")+
                    V2.Distance(core.Pos,player.Pos).ToString("0")+L(" ô"," tiles"));
            }
        }

        float DrawFieldNotice(float y,string icon,string message)
        {
            float width=Mathf.Min(320,Screen.width-32);
            float height=Mathf.Max(35,small.CalcHeight(new GUIContent(message),width-48)+12);
            if(y+height>Screen.height-177)return 0;
            var card=new Rect(16,y,width,height);
            PixelPanel(card,FieldPaper);
            DrawSpritePreview(new Rect(card.x+7,card.y+5,25,25),Primitive(icon));
            GUI.Label(new Rect(card.x+38,card.y+5,width-48,height-10),message,small);
            return height+6;
        }

        GUIStyle meterValue;
        void DrawFieldMeter(float x,float y,float width,string caption,float value,float max,Color tint)
        {
            if(meterValue==null)
                meterValue=new GUIStyle(small){alignment=TextAnchor.MiddleRight,wordWrap=false};
            string number=Mathf.CeilToInt(value)+" / "+Mathf.CeilToInt(max);
            float numberWidth=meterValue.CalcSize(new GUIContent(number)).x+4;
            RegisterHudRect(new Rect(x-4,y-2,width+8,44));
            bool stacked=small.CalcSize(new GUIContent(caption)).x+numberWidth+5>width;
            UiLabel(new Rect(x,y,stacked?width:width-numberWidth,18),caption,small);
            GUI.Label(new Rect(x,stacked?y+15:y,width,18),number,meterValue);
            var rail=new Rect(x,y+33,width,5);
            Panel(rail,new Color(.29f,.22f,.40f));
            rail.width=Mathf.Round(width*Mathf.Clamp01(value/Mathf.Max(1,max)));
            if(rail.width>0)
            {
                Panel(rail,tint);
                Panel(new Rect(rail.x,rail.y,rail.width,1),Color.Lerp(tint,Color.white,.35f));
            }
        }

        GUIStyle matchClockStyle;

        void DrawMatchClock()
        {
            if(State==null||State.Phase==Phase.Sanctuary||State.Phase==Phase.Won||State.Phase==Phase.Lost)return;
            if(matchClockStyle==null)
            {
                matchClockStyle=new GUIStyle(label)
                {
                    alignment=TextAnchor.MiddleCenter,
                    fontStyle=FontStyle.Bold,
                    fontSize=15,
                    wordWrap=false
                };
            }
            int totalSec=Mathf.Max(0,Mathf.FloorToInt(State.Time));
            int hours=totalSec/3600,mins=(totalSec/60)%60,secs=totalSec%60;
            string clockStr=hours>0
                ?hours.ToString("0")+":"+mins.ToString("00")+":"+secs.ToString("00")
                :mins.ToString("00")+":"+secs.ToString("00");
            float textW=matchClockStyle.CalcSize(new GUIContent(clockStr)).x;
            float w=Mathf.Max(108f,Mathf.Ceil(textW+32f)),h=30f;
            var box=new Rect(Mathf.Round((Screen.width-w)*.5f),10f,w,h);
            bool eclipse=State.Phase==Phase.Eclipse;
            bool boss=State.BossArenaActive;
            PixelPanel(box,boss?new Color(.28f,.07f,.14f,.94f):eclipse?new Color(.30f,.14f,.08f,.94f):FieldPaper);
            if(Event.current.type==EventType.Repaint)
            {
                matchClockStyle.normal.textColor=boss?new Color(1f,.76f,.78f):eclipse?Gold:new Color(.96f,.94f,1f);
                GUI.Label(box,clockStr,matchClockStyle);
            }
            int bossRem=Mathf.Max(0,Mathf.CeilToInt(State.NextBossEventTime-State.Time));
            string tip=boss?L("Đang trong Sự kiện Boss","World Boss Event active")
                :State.Phase==Phase.Explore?L("Thời gian trận · Boss tiếp theo sau ","Match time · Next Boss in ")+(bossRem/60).ToString("00")+":"+(bossRem%60).ToString("00")
                :L("Thời gian trong trận","In-game match time");
            GUI.Label(box,new GUIContent("",tip),GUIStyle.none);
        }

        void DrawCornerPauseButton()
        {
            var rect=new Rect(Screen.width-48,Screen.height-48,36,36);
            bool hover=rect.Contains(Event.current.mousePosition);
            PixelPanel(rect,hover?Plum:new Color(.20f,.14f,.32f,.94f));
            if(Event.current.type==EventType.Repaint)
            {
                Color barColor=hover?Color.white:new Color(.93f,.88f,1f,.95f);
                Panel(new Rect(rect.x+11,rect.y+10,4,16),barColor);
                Panel(new Rect(rect.x+21,rect.y+10,4,16),barColor);
            }
            if(GUI.Button(rect,new GUIContent("",L("Tạm dừng [Esc]","Pause [Esc]")),GUIStyle.none))
            {
                inventoryOpen=false;craftOpen=false;pendingQuickAssign=0;
                dragFromCell=-1;draggingItem=false;
                pauseConfirm=false;pauseSettings=false;
                path.Clear();
                menu=true;
                SaveActiveRun(false,asyncWrite:true);
            }
        }

        void DrawSpectateHud()
        {
            if(network)DrawPartyBubbles();
            DrawMatchClock();
            DrawCornerPauseButton();
            float width=Mathf.Min(Screen.width-28,460);
            var panel=new Rect((Screen.width-width)/2,Screen.height-96,width,78);
            PixelPanel(panel,FieldPaper);
            var candidates=SpectateCandidates();
            var target=CameraTarget();
            bool hasTarget=target!=null&&target.Id!=localId&&!target.Ghost;
            if(candidates.Length>1)
            {
                if(UiButton(new Rect(panel.x+10,panel.y+10,42,36),"‹"))CycleSpectate(-1);
                if(UiButton(new Rect(panel.xMax-52,panel.y+10,42,36),"›"))CycleSpectate(1);
            }
            string headline=hasTarget
                ?L("Đang theo dõi: ","Spectating: ")+"P"+target.PartySlot+" · "+target.Name+" ("+EmberLanguage.Text(Catalog.VesselNames[(int)target.Vessel])+")"
                :L("Không còn đồng đội nào đang sống","No living teammates to spectate");
            string detail=hasTarget
                ?"HP "+Mathf.CeilToInt(target.HP)+"/"+Mathf.CeilToInt(target.MaxHP)+"  ·  "+L("Hoảng loạn ","Dread ")+Mathf.CeilToInt(target.Dread)+"%  ·  "+L("A / D đổi góc nhìn","A / D switch view")
                :L("Chờ kết thúc trận hoặc được hồi sinh tại hải đăng","Waiting for run end or beacon revival");
            UiLabel(new Rect(panel.x+60,panel.y+10,panel.width-120,28),headline,label);
            UiLabel(new Rect(panel.x+16,panel.y+44,panel.width-32,26),detail,small);
        }

        GUIStyle marqueeStyle,popupStyle,popupBigStyle;

        void DrawMarqueeLabel(Rect rect,string text,GUIStyle baseStyle,float speed=44f)
        {
            if(string.IsNullOrEmpty(text))return;
            if(marqueeStyle==null)
            {
                marqueeStyle=new GUIStyle(baseStyle)
                {
                    wordWrap=false,
                    clipping=TextClipping.Clip,
                    alignment=TextAnchor.MiddleLeft
                };
            }
            marqueeStyle.fontSize=baseStyle.fontSize;
            marqueeStyle.fontStyle=baseStyle.fontStyle;
            marqueeStyle.normal.textColor=baseStyle.normal.textColor;

            float textW=marqueeStyle.CalcSize(new GUIContent(text)).x;
            if(textW<=rect.width-2f)
            {
                GUI.Label(rect,text,marqueeStyle);
                return;
            }
            string loop=text+"   •   ";
            float cycleW=marqueeStyle.CalcSize(new GUIContent(loop)).x;
            if(cycleW<1f)cycleW=textW+32f;
            float offset=(clock*speed)%cycleW;
            GUI.BeginClip(rect);
            GUI.Label(new Rect(-offset,0,cycleW+12f,rect.height),loop,marqueeStyle);
            GUI.Label(new Rect(cycleW-offset,0,cycleW+12f,rect.height),loop,marqueeStyle);
            if(cycleW<rect.width)GUI.Label(new Rect(cycleW*2f-offset,0,cycleW+12f,rect.height),loop,marqueeStyle);
            GUI.EndClip();
        }

        string BuildHoldingInfo(Player p)
        {
            if(IsHoldingWeaponSlot(heldSlot))
            {
                Weapon activeW=WeaponForSlot(p,heldSlot);
                bool ranged=activeW==Weapon.Crossbow||activeW==Weapon.LumenGun||activeW==Weapon.MirrorShield;
                float range=WeaponRangeFor(activeW);
                float baseDmg=activeW==Weapon.Stick?18f:activeW==Weapon.CrystalAxe?36f:25f;
                float powerMult=p.Stats.PowerMultiplier*(p.Hat==(int)Hat.Jester&&p.Dread>=90?1.15f:1f);
                int dmg=Mathf.Max(1,Mathf.RoundToInt(baseDmg*powerMult));
                float cd=activeW==Weapon.ShadowBlades?.2f:activeW==Weapon.LumenGun?.32f:.5f;
                string kind=heldSlot==8?L("Đào & Chặt","Mine & Chop"):ranged?L("Tầm xa","Ranged"):L("Cận chiến","Melee");
                string info=InventorySlotName(p,heldSlot)+" · "+kind
                    +" · "+L("ST ","DMG ")+dmg
                    +" · "+L("Tầm ","Range ")+range.ToString("0.#")+L(" ô"," tiles")
                    +" · "+L("Nhịp ","CD ")+cd.ToString("0.##")+"s";
                int mag=Catalog.WeaponMagSize(activeW);
                if(mag>0)
                {
                    int curAmmo=p.Ammo<0||p.AmmoWeapon!=activeW?mag:Mathf.Clamp(p.Ammo,0,mag);
                    float reloadSec=Catalog.WeaponReloadTime(activeW);
                    info+=p.Reloading
                        ?" · "+L("Đang nạp ","Reloading ")+p.ReloadTimer.ToString("0.0")+"s"
                        :" · "+L("Đạn ","Ammo ")+curAmmo+"/"+mag+" ("+L("nạp ","reload ")+reloadSec.ToString("0.#")+"s)";
                }
                if(activeW==Weapon.Pickaxe)
                {
                    int mineYield=(p.Has(Perk.Miner)?2+p.Rank(Perk.Miner):2)+p.PickaxeTier;
                    info+=" · "+p.PickaxeName+" ["+L("Độ bền ","Durability ")+p.PickaxeDurability+"/"+p.PickaxeMaxDurability+"] · "+L("Khai thác +"+mineYield+" tài nguyên · Tốn 8 thể lực","Mines +"+mineYield+" yield · 8 stamina");
                }
                else if(activeW==Weapon.CrystalAxe)
                    info+=" · "+L("Đánh lan 1.6 ô & đẩy lùi mạnh · Tốn 8 thể lực","1.6-tile cleave & strong knockback · 8 stamina");
                else if(activeW==Weapon.ShadowBlades)
                    info+=" · "+L("Tốc đánh siêu nhanh · +0.5 Hoảng loạn/s trong tối","Ultra-fast strikes · +0.5 Dread/s in darkness");
                else if(activeW==Weapon.MirrorShield)
                    info+=" · "+L("Bắn tia phản xạ · Tốn 8 thể lực","Reflective bolt · 8 stamina");
                else if(ranged)
                    info+=" · "+L("Tự ngắm mục tiêu gần nhất & đẩy lùi","Auto-aims nearest target & knockback");
                else
                    info+=" · "+L("Đẩy lùi nhẹ · Không tốn thể lực","Light knockback · 0 stamina");
                return info;
            }
            if(heldSlot==6)
                return L("Tay không · Không đốt nhiên liệu đèn · Phím 1–4 đổi trang bị nhanh · [E] Tự cầm cuốc đào mỏ hoặc nhặt đồ",
                    "Empty hands · Saves torch fuel · Keys 1–4 swap quick slots · [E] Auto-equip pickaxe to mine or loot");

            int idx=Mathf.Clamp(heldSlot-2,0,3);
            string itemName=(EmberLanguage.English?ItemNamesEn:ItemNamesVi)[idx];
            int count=p.Items[idx];
            string qty=count>0?L("Còn x","x")+count:L("Đã hết","Empty");
            string detail;
            if(idx==0)detail=L("Cầm tay soi sáng (tốn nhiên liệu) · [F] Đặt xuống đất sáng 45s (mất 1 đuốc) · [L] Tắt/Bật lửa",
                "Hold to light area (uses fuel) · [F] Place on ground for 45s (consumes 1) · [L] Toggle flame");
            else if(idx==1)detail=L("[F] Ném pháo sáng soi rộng 8 ô trong 12s · Xua bóng tối & chống Hoảng loạn",
                "[F] Deploy 8-tile flare for 12s · Banishes dark & prevents Dread");
            else if(idx==2)detail=L("[F] Uống giảm ngay 40% Hoảng loạn để tránh ảo giác & đánh nhầm đồng đội",
                "[F] Drink to calm 40% Dread and prevent hallucinations");
            else detail=L("Đứng gần Hải Đăng bấm [F] để hồi sinh đồng đội gục với 50% HP & 3s bất tử",
                "Stand near Beacon and press [F] to revive a downed ally at 50% HP");
            return itemName+" ("+qty+") · "+detail;
        }

        void DrawEnemyHealthBars()
        {
            if(State?.Enemies==null||Event.current.type!=EventType.Repaint)return;
            var focus=CameraTarget();
            V2 center=focus?.Pos??new V2();
            foreach(var e in State.Enemies)
            {
                V2 eVis=VisualEnemyPos(e);
                if(e.HP<=0||e.IsWorldBoss||e.HP>=e.MaxHP-.1f||V2.Distance(eVis,center)>24f)continue;
                Vector2 top=ScreenPos(eVis+new V2(0,e.Kind==EnemyKind.Boss?1.35f:e.Elite?1.05f:.85f));
                float w=e.Kind==EnemyKind.Boss?56f:e.Elite?46f:36f;
                float h=5f;
                var rail=new Rect(Mathf.Round(top.x-w*.5f),Mathf.Round(top.y-8f),w,h);
                Panel(new Rect(rail.x-1,rail.y-1,rail.width+2,rail.height+2),new Color(.04f,.02f,.06f,.85f));
                Panel(rail,new Color(.24f,.10f,.14f,.92f));
                float ratio=Mathf.Clamp01(e.HP/Mathf.Max(1f,e.MaxHP));
                Panel(new Rect(rail.x,rail.y,Mathf.Round(rail.width*ratio),rail.height),new Color(.96f,.26f,.32f,.98f));
            }
        }

        void DrawCombatPopups()
        {
            if(State?.Popups==null||State.Popups.Length==0||Event.current.type!=EventType.Repaint)return;
            if(popupStyle==null)
            {
                popupStyle=new GUIStyle(small)
                {
                    alignment=TextAnchor.MiddleCenter,
                    fontStyle=FontStyle.Bold,
                    fontSize=13,
                    wordWrap=false,
                    clipping=TextClipping.Overflow
                };
                popupBigStyle=new GUIStyle(popupStyle){fontSize=16};
            }
            string[] resVi=Catalog.ResourceNames;
            string[] resEn={"Dust","Core","Umbra","Wood","Ore"};
            foreach(var pop in State.Popups)
            {
                if(pop.Life<=0||pop.MaxLife<=0)continue;
                float progress=1f-Mathf.Clamp01(pop.Life/pop.MaxLife);
                float rise=Mathf.Pow(progress,.65f)*1.15f;
                float alpha=Mathf.Clamp01(pop.Life/Mathf.Min(.32f,pop.MaxLife*.4f));
                Vector2 pt=ScreenPos(pop.Pos+new V2(0,rise));

                string text;
                Color tint;
                bool big=false;
                if(pop.Kind==0)
                {
                    text="-"+pop.Amount;
                    big=pop.Amount>=30;
                    tint=big?new Color(1f,.66f,.18f,alpha):new Color(1f,.94f,.62f,alpha);
                }
                else if(pop.Kind==1)
                {
                    text="-"+pop.Amount;
                    big=pop.Amount>=22;
                    tint=new Color(1f,.26f,.32f,alpha);
                }
                else if(pop.Kind==2)
                {
                    text="+"+pop.Amount+" HP";
                    tint=new Color(.34f,1f,.66f,alpha);
                }
                else
                {
                    int rIdx=Mathf.Clamp(pop.SubKind,0,resVi.Length-1);
                    string resName=(EmberLanguage.English?resEn:resVi)[rIdx];
                    text="+"+pop.Amount+" "+resName;
                    tint=rIdx==1?new Color(.42f,.96f,1f,alpha):rIdx==2?new Color(.82f,.58f,1f,alpha):new Color(1f,.86f,.34f,alpha);
                }

                var style=big?popupBigStyle:popupStyle;
                var box=new Rect(Mathf.Round(pt.x-72f),Mathf.Round(pt.y-12f),144f,24f);
                style.normal.textColor=new Color(0,0,0,.85f*alpha);
                GUI.Label(new Rect(box.x-1,box.y,box.width,box.height),text,style);
                GUI.Label(new Rect(box.x+1,box.y,box.width,box.height),text,style);
                GUI.Label(new Rect(box.x,box.y-1,box.width,box.height),text,style);
                GUI.Label(new Rect(box.x,box.y+1,box.width,box.height),text,style);
                style.normal.textColor=tint;
                GUI.Label(box,text,style);
            }
        }

        void DrawFieldHud(Player p)
        {
            if(p.Ghost){DrawSpectateHud();return;}
            DrawMatchClock();
            DrawCornerPauseButton();
            float toolWidth=Mathf.Clamp((Screen.width-120)/2f,72,130);
            float toolX=Screen.width-116-toolWidth;
            if(UiButton(new Rect(toolX,12,toolWidth,29),"Chế tạo [Tab]"))craftOpen=!craftOpen;
            float upperWidth=Mathf.Min(180,(Screen.width-40)/2f);
            float upperY=CompactFieldHud?45:12;
            DrawFieldMeter(16,upperY,upperWidth,L("Hoảng loạn","Dread"),p.Dread,100,new Color(.65f,.4f,.85f));
            if(heldSlot==2&&p.Items[0]>0)
                DrawFieldMeter(22+upperWidth,upperY,upperWidth,L("Nhiên liệu đuốc","Torch fuel"),State.Fuel,100,Gold);

            if(UiButton(new Rect(toolX-toolWidth-8,12,toolWidth,29),L("Kho đồ [I]","Inventory [I]"))){inventoryOpen=!inventoryOpen;craftOpen=false;}
            if(network)DrawPartyBubbles();

            bool anyDowned=State.Players.Any(q=>q.Ghost);
            if(anyDowned&&p.Pos.Length<=5)
            {
                bool explore=State.Phase==Phase.Explore;
                bool canRevive=explore?State.Charge>=EmberWorld.ReviveChargeCost:(State.Phase==Phase.Eclipse||State.Phase==Phase.Extraction)&&State.BeaconHP>EmberWorld.ReviveBeaconHpCost;
                string costText=explore?L("-100 NL trụ","-100 energy"):L("-150 HP trụ","-150 beacon HP");
                float rw=Mathf.Min(290,Screen.width-28);
                GUI.enabled=canRevive&&!debugOpen;
                if(UiButton(new Rect((Screen.width-rw)/2,Screen.height-210,rw,32),L("Hồi sinh đồng đội [H] · ","Revive ally [H] · ")+costText))intent.Revive=true;
                GUI.enabled=true;
            }

            float width=Mathf.Min(Screen.width-26,570);
            float x=(Screen.width-width)/2;
            float y=Screen.height-110;
            PixelPanel(new Rect(x,y,width,33),FieldPaper);
            float step=(width-18)/6;
            for(int i=0;i<5;i++)
            {
                float left=x+9+i*step;
                PixelArt(new Rect(left,y+5,22,22),ResourceIcons[i]);
                UiLabel(new Rect(left+25,y+6,step-23,22),p.Bag[i].ToString(),small);
            }
            int maxCap=Mathf.FloorToInt(p.Capacity);
            bool overloaded=p.Load>p.Capacity;
            var prevCapCol=GUI.contentColor;
            if(overloaded)GUI.contentColor=Coral;
            GUI.Label(new Rect(x+6+5*step,y+6,step+6,22),
                new GUIContent(L("Tải ","Wt ")+p.Load+"/"+maxCap,
                    L("Tải trọng balo — vượt mức sẽ bị giảm 45% tốc chạy","Carry load — exceeding capacity slows movement by 45%")),small);
            GUI.contentColor=prevCapCol;

            var itemChip=new Rect(x,y-36,width,32);
            PixelPanel(itemChip,FieldPaper);
            int activeMag=IsHoldingWeaponSlot(heldSlot)?Catalog.WeaponMagSize(WeaponForSlot(p,heldSlot)):0;
            bool showReloadBtn=IsHoldingWeaponSlot(heldSlot)&&activeMag>0&&!p.Reloading&&p.CurrentAmmo<activeMag;
            bool showUseBtn=!showReloadBtn&&heldSlot>=2&&heldSlot<=5;
            float rightReserve=(showReloadBtn||showUseBtn)?106f:16f;
            DrawMarqueeLabel(new Rect(itemChip.x+8,itemChip.y+4,Mathf.Max(40,itemChip.width-rightReserve),24),BuildHoldingInfo(p),small);
            if(showReloadBtn)
            {
                GUI.enabled=!debugOpen;
                if(UiButton(new Rect(itemChip.xMax-96,itemChip.y+2,92,28),L("Nạp [X]","Reload [X]")))intent.Reload=true;
                GUI.enabled=true;
            }
            else if(showUseBtn)
            {
                GUI.enabled=CanUseSelectedItem(p)&&!debugOpen;
                if(UiButton(new Rect(itemChip.xMax-96,itemChip.y+2,92,28),L("Dùng [U]","Use [U]")))intent.Use=selectedItem;
                GUI.enabled=true;
            }
            // Floating status banner removed by user request
            int count=p.Vessel==Vessel.Wanderer?4:5;
            float cell=Mathf.Min(72,(width-12)/count);
            float start=(Screen.width-count*cell)/2;
            float flank=Mathf.Min(220,start-30);
            if(flank>=106)
            {
                DrawFieldMeter(start-flank-14,y+48,flank,"HP",p.HP,p.MaxHP,Coral);
                DrawFieldMeter(start+count*cell+10,y+48,flank,L("Thể lực","Stamina"),p.Stamina,100,Cyan);
            }
            else
            {
                float compact=Mathf.Min(154,(width-20)/2);
                float upper=upperY+40;
                DrawFieldMeter(16,upper,compact,"HP",p.HP,p.MaxHP,Coral);
                DrawFieldMeter(23+compact,upper,compact,L("Thể lực","Stamina"),p.Stamina,100,Cyan);
            }
            for(int i=0;i<count;i++)
            {
                bool skill=i==4;
                int slot=skill?0:quickSlots[i];
                bool enabled=!debugOpen&&(!skill||(!p.Ghost&&!p.Extracted&&p.SkillCD<=0&&HasSkillFocus(p)));
                Rect tile=new Rect(start+i*cell,y+38,Mathf.Min(58,cell-4),Mathf.Min(58,cell-4));
                PixelPanel(tile,!skill&&heldSlot==slot?new Color(.51f,.34f,.72f):enabled?new Color(.22f,.16f,.34f):FieldPaper);
                Rect icon=new Rect(tile.center.x-14,tile.y+5,28,28);
                if(skill)
                {
                    if(p.Vessel!=Vessel.LittleMoone)DrawSpritePreview(icon,Frame(p.Vessel,"cast",0));
                    else PixelArt(icon,"meteor");
                }
                else DrawSpritePreview(icon,SlotIconSprite(p,slot));
                string key=skill?"J":(i+1).ToString();
                string value=skill?(p.SkillCD<=0?(HasSkillFocus(p)?L("Sẵn","Ready"):L("Chờ","Aim")):p.SkillCD.ToString("0.0")+"s"):SlotShortValue(p,slot);
                UiLabel(new Rect(tile.x+5,tile.y+36,tile.width-8,20),key+" · "+value,small);
                string tooltip=skill?L("Kỹ năng","Skill"):InventorySlotName(p,slot);
                GUI.enabled=enabled;
                if(GUI.Button(tile,new GUIContent("",tooltip),GUIStyle.none))
                {
                    if(skill)intent.Skill=true;
                    else EquipSlot(slot);
                }
                GUI.enabled=true;
            }
        }
    }
}
