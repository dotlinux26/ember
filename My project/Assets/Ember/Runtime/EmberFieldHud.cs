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
            string distance=Catalog.SkillRange(vessel,Local!=null?Local.Awake:true).ToString("0.#")+L(" ô"," tiles");
            return vessel==Vessel.SpaceWraith
                ?L("Thước 2 · Bán kính kéo đồng đội: ","Range 2 · Ally pickup radius: ")+distance
                :vessel==Vessel.Wanderer
                ?L("Thước 2 · Bán kính tăng tốc đồng đội: ","Range 2 · Ally speedup radius: ")+distance
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

        void DrawHarvestBars()
        {
            if(Event.current.type!=EventType.Repaint)return;
            V2 focus=CameraTarget()?.Pos??new V2();
            if(State?.Nodes!=null)
            {
                foreach(var n in State.Nodes)
                {
                    if(n.Dropped||V2.Distance(n.Pos,focus)>22f)continue;
                    int maxAmt=n.MaxAmount>0?n.MaxAmount:5;
                    if(n.Amount>=maxAmt)continue;
                    float ratio=Mathf.Clamp01(n.Amount/(float)maxAmt);
                    float yOff=(!n.Dropped&&n.Resource==Resource.Wood)?1.85f:.85f;
                    Vector2 anchor=ScreenPos(n.Pos+new V2(0,yOff));
                    float x=Mathf.Round(anchor.x), y=Mathf.Round(anchor.y);
                    Panel(new Rect(x-20,y-7,40,7),new Color(.05f,.04f,.09f,.92f));
                    Color fill=ratio<.3f?Coral:n.Resource==Resource.Dust?new Color(.35f,1f,.90f):n.Resource==Resource.Wood?new Color(.35f,.95f,.45f):new Color(.38f,.85f,1f);
                    Panel(new Rect(x-19,y-6,38*ratio,5),fill);
                    Panel(new Rect(x-19,y-6,38*ratio,1),new Color(1f,1f,1f,.25f));
                }
            }
            if(State?.HarvestTargets!=null)
            {
                int size=State.Size;
                foreach(var ht in State.HarvestTargets)
                {
                    if(ht.HP<=0||ht.MaxHP<=0)continue;
                    V2 tPos=new V2(ht.Tx-size/2f+.5f,ht.Ty-size/2f+.5f);
                    if(V2.Distance(tPos,focus)>22f)continue;
                    float ratio=Mathf.Clamp01(ht.HP/ht.MaxHP);
                    if(ratio>=1f)continue;
                    float yOff=ht.Tile==3?1.85f:.85f;
                    Vector2 anchor=ScreenPos(tPos+new V2(0,yOff));
                    float x=Mathf.Round(anchor.x), y=Mathf.Round(anchor.y);
                    Panel(new Rect(x-20,y-7,40,7),new Color(.05f,.04f,.09f,.92f));
                    Color fill=ratio<.3f?Coral:ht.Tile==3?new Color(.35f,.95f,.45f):new Color(.38f,.85f,1f);
                    Panel(new Rect(x-19,y-6,38*ratio,5),fill);
                    Panel(new Rect(x-19,y-6,38*ratio,1),new Color(1f,1f,1f,.25f));
                }
            }
        }

        void DrawBeaconOverhead(Player p)
        {
            if(State==null)return;
            V2 beaconTop=new V2(0,2.45f);
            Vector2 screen=ScreenPos(beaconTop);
            if(screen.x<-150||screen.x>Screen.width+150||screen.y<-150||screen.y>Screen.height+150)return;

            int tier=Mathf.Clamp(Mathf.FloorToInt(State.Charge/100f)+1,1,10);
            string tierTitle=L($"Trụ Tinh Thể · Cấp {tier}",$"Crystal Beacon · Tier {tier}");
            int curCharge=Mathf.RoundToInt(State.Charge);
            int targetCharge=Mathf.Max(1,Mathf.RoundToInt(State.ChargeTarget));
            bool overcharged=curCharge>=targetCharge;
            string energyText;
            if(State.Phase==Phase.Eclipse)
                energyText=L($"Bảo vệ Hải Đăng · HP {Mathf.CeilToInt(State.BeaconHP)}/600",$"Defend Beacon · HP {Mathf.CeilToInt(State.BeaconHP)}/600");
            else if(overcharged)
                energyText=$"{curCharge}/{targetCharge} NL "+L("(Sẵn sàng)","(Ready)");
            else
                energyText=$"{curCharge}/{targetCharge} NL";

            bool nearBeacon=p!=null&&!p.Ghost&&p.Pos.Length<3.8f;
            float bw=166f, bh=nearBeacon?74f:50f;
            Rect panelRect=new Rect(Mathf.Round(screen.x-bw/2),Mathf.Round(screen.y-bh),bw,bh);

            PixelPanel(panelRect,new Color(.08f,.06f,.14f,.94f));

            // Header title
            var headerStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
            headerStyle.normal.textColor=new Color(.92f,.90f,1f);
            GUI.Label(new Rect(panelRect.x+4,panelRect.y+4,panelRect.width-8,16),tierTitle,headerStyle);

            // Energy meter
            float chargeRatio=Mathf.Clamp01(curCharge/(float)targetCharge);
            float mw=panelRect.width-20;
            Rect barBg=new Rect(panelRect.x+10,panelRect.y+22,mw,6);
            Panel(barBg,new Color(.04f,.03f,.08f,.95f));
            Color meterCol=overcharged?new Color(.35f,1f,.85f):new Color(.28f,.85f,1f);
            Panel(new Rect(barBg.x+1,barBg.y+1,(mw-2)*chargeRatio,4),meterCol);
            Panel(new Rect(barBg.x+1,barBg.y+1,(mw-2)*chargeRatio,1),new Color(1f,1f,1f,.32f));

            // Energy text under meter
            var energyStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter};
            energyStyle.normal.textColor=overcharged?new Color(.4f,1f,.88f):new Color(.78f,.92f,1f);
            GUI.Label(new Rect(panelRect.x+4,panelRect.y+30,panelRect.width-8,16),energyText,energyStyle);

            // Interactive deposit prompt when player is nearby
            if(nearBeacon)
            {
                int cores=p!=null?p.Bag[(int)Resource.Core]:0;
                Rect btnRect=new Rect(panelRect.x+8,panelRect.y+48,panelRect.width-16,22);
                PixelPanel(btnRect,cores>0?new Color(.18f,.14f,.28f,.96f):new Color(.12f,.10f,.18f,.88f));

                Rect keyBox=new Rect(btnRect.x+3,btnRect.y+2,18,18);
                Panel(keyBox,Gold);
                var keyStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
                keyStyle.normal.textColor=Color.black;
                GUI.Label(keyBox,"B",keyStyle);

                var promptStyle=new GUIStyle(small){alignment=TextAnchor.MiddleLeft};
                promptStyle.normal.textColor=cores>0?new Color(1f,.94f,.55f):new Color(.70f,.72f,.80f);
                string depositPrompt=L("Nạp năng lượng","Deposit Energy");
                GUI.Label(new Rect(keyBox.xMax+6,btnRect.y+1,btnRect.width-28,20),depositPrompt,promptStyle);

                if(GUI.Button(btnRect,GUIContent.none,GUIStyle.none))
                {
                    if(cores>0)intent.Deposit=true;
                    else notice=L("Không có vật phẩm nạp!","No items to deposit!");
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
                        // Undropped resource veins/bushes already have overhead health bars and direct Click/K harvest.
                        // Only show interact prompt for dropped loot on the ground!
                        if(!node.Dropped)return;
                        string resName=EmberLanguage.Text(Catalog.ResourceNames[(int)node.Resource]);
                        string verb=node.Resource==Resource.Wood?L("Nhặt cành","Pick up branch"):L("Nhặt","Pick up");
                        targetPos=node.Pos+new V2(0,.55f);
                        caption=verb+" · "+resName+" ×"+node.Amount;
                    }
                    else
                    {
                        // Check for broken bridge tiles (tile 7) to repair
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
                                if(t==7)
                                {
                                    V2 blockPos=new V2(tx-size/2f+.5f,ty-size/2f+.5f);
                                    float d=V2.Distance(p.Pos,blockPos);
                                    if(d<2.2f&&d<bestDist){bestDist=d;bestTx=tx;bestTy=ty;targetTile=t;}
                                }
                            }
                        }
                        if(bestTx>=0&&targetTile==7)
                        {
                            targetPos=new V2(bestTx-size/2f+.5f,bestTy-size/2f+.5f);
                            bool canRepair=p.Bag[(int)Resource.Wood]>=4&&p.Bag[(int)Resource.Ore]>=2;
                            caption=canRepair?L("Sửa cầu · Gỗ ×4, Quặng ×2","Repair · Wood ×4, Ore ×2"):L("Cầu gãy (Cần Gỗ ×4, Quặng ×2)","Broken (Need Wood ×4, Ore ×2)");
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
                if(ShouldAutoEquipPickaxe(p))
                {
                    AutoEquipHarvestTool(p);
                    intent.Interact=true;
                }
                else if(IsNearTreeOrRock(p)&&!HasHarvestTool(p))
                {
                    bool isTree=IsNearTree(p);
                    notice=isTree
                        ?(EmberLanguage.English?"Need an Axe or Pickaxe to chop trees!":"Không có Rìu hoặc Cuốc để chặt cây!")
                        :(EmberLanguage.English?"Need a Pickaxe to mine rocks!":"Không có Cuốc để khai thác quặng!");
                }
                else
                {
                    intent.Interact=true;
                }
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
            if(player.HasBuff(BuffKind.Swiftness))
            {
                var swiftBuff=player.GetBuff(BuffKind.Swiftness);
                string bText=$"⚡ +{Mathf.RoundToInt(swiftBuff.Value*100)}%";
                UiLabel(new Rect(x-30,y-32,60,16),bText,small);
            }
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

        bool questCollapsed;
        bool questCollapsedInitialized;
        bool QuestCollapsed
        {
            get
            {
                if(!questCollapsedInitialized)
                {
                    questCollapsed=PlayerPrefs.GetInt("Ember.QuestCollapsed",0)==1;
                    questCollapsedInitialized=true;
                }
                return questCollapsed;
            }
            set
            {
                questCollapsed=value;
                questCollapsedInitialized=true;
                PlayerPrefs.SetInt("Ember.QuestCollapsed",questCollapsed?1:0);
                PlayerPrefs.Save();
            }
        }

        void DrawCircularProgress(Vector2 center,float radius,float ratio,Color color)
        {
            const int segments=28;
            float filled=Mathf.Clamp01(ratio)*segments;
            for(int i=0;i<segments;i++)
            {
                float a=(-90f+360f*i/segments)*Mathf.Deg2Rad;
                float b=(-90f+360f*(i+1)/segments)*Mathf.Deg2Rad;
                Vector2 from=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
                Vector2 to=center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius;
                GUI.color=new Color(.14f,.10f,.22f,.90f);
                Line(from,to,5);
                if(filled>i)
                {
                    GUI.color=color;
                    Line(from,Vector2.Lerp(from,to,Mathf.Clamp01(filled-i)),4);
                }
            }
            GUI.color=Color.white;
        }

        void DrawObjectiveCard(Player p,float y)
        {
            bool blackout=State.Blackout>0;
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

            if(QuestCollapsed)
            {
                // Compact circular quest bubble
                Vector2 center=new Vector2(38,y+24);
                Rect bubbleRect=new Rect(center.x-24,center.y-24,48,48);
                RegisterHudRect(bubbleRect);

                // Dark circular background
                GUI.color=blackout?new Color(.35f,.11f,.22f,.95f):new Color(.11f,.08f,.18f,.95f);
                DrawSpritePreview(new Rect(center.x-19,center.y-19,38,38),Primitive("circle"));
                GUI.color=Color.white;

                // Circular progress ring
                Color ringColor=blackout?Coral:progress>=1f?Cyan:Gold;
                DrawCircularProgress(center,20,progress,ringColor);

                // Quest icon in center
                Rect iconRect=new Rect(center.x-12,center.y-12,24,24);
                if(icon=="torch")DrawSpritePreview(iconRect,Primitive("torch"));
                else PixelArt(iconRect,icon);

                // Expand badge with arrow >
                Rect arrowBadge=new Rect(center.x+13,center.y-8,16,16);
                PixelPanel(arrowBadge,new Color(.22f,.16f,.35f,.95f));
                var oldContent=GUI.contentColor;
                GUI.contentColor=new Color(1f,.92f,.55f);
                GUI.Label(new Rect(arrowBadge.x+4,arrowBadge.y-2,12,16),">",small);
                GUI.contentColor=oldContent;

                // Percentage text under the bubble
                string pctText=Mathf.RoundToInt(Mathf.Clamp01(progress)*100)+"%";
                var oldAlign=small.alignment;
                small.alignment=TextAnchor.MiddleCenter;
                GUI.Label(new Rect(center.x-24,center.y+19,48,14),pctText,small);
                small.alignment=oldAlign;

                // Click to expand
                if(GUI.Button(new Rect(center.x-24,center.y-24,56,56),new GUIContent("",heading+"\n"+hint+"\n"+L("Click để mở rộng nhiệm vụ","Click to expand quest")),GUIStyle.none))
                {
                    QuestCollapsed=false;
                }
                return;
            }

            var card=new Rect(16,y,Mathf.Min(370,Screen.width-32),72);
            RegisterHudRect(card);
            PixelPanel(card,blackout?new Color(.31f,.11f,.21f):FieldPaper);

            var cIconRect=new Rect(card.x+12,card.y+12,32,32);
            if(icon=="torch")DrawSpritePreview(cIconRect,Primitive("torch"));
            else PixelArt(cIconRect,icon);
            UiLabel(new Rect(card.x+54,card.y+6,card.width-90,25),heading,label);
            UiLabel(new Rect(card.x+54,card.y+31,card.width-90,28),hint,small);

            // Collapse button [ < ] in upper right of card
            Rect collapseBtn=new Rect(card.xMax-26,card.y+6,20,20);
            PixelPanel(collapseBtn,new Color(.24f,.18f,.38f,.92f));
            var oldCol=GUI.contentColor;
            GUI.contentColor=new Color(.88f,.91f,.98f);
            GUI.Label(new Rect(collapseBtn.x+5,collapseBtn.y-2,14,20),"<",small);
            GUI.contentColor=oldCol;
            if(GUI.Button(collapseBtn,new GUIContent("",L("Thu gọn hiển thị nhiệm vụ","Collapse quest display")),GUIStyle.none))
            {
                QuestCollapsed=true;
            }

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
                if(!State.Event.Contains("Thu thập lõi")&&!State.Event.Contains("Gather cores"))
                {
                    string message=(State.EventActor??"")+EmberLanguage.Text(State.Event)+(State.EventTarget??"");
                    y+=DrawFieldNotice(y,"beacon",message);
                }
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
            if(State.Phase==Phase.Explore||State.Phase==Phase.Eclipse)
            {
                var waveBox=new Rect(Mathf.Round((Screen.width-104)*.5f),43f,104f,19f);
                PixelPanel(waveBox,new Color(.16f,.10f,.24f,.95f));
                if(Event.current.type==EventType.Repaint)
                {
                    var waveStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=11,fontStyle=FontStyle.Bold};
                    waveStyle.normal.textColor=eclipse?Coral:new Color(1f,.88f,.45f);
                    string text=eclipse?L($"NGUYỆT THỰC {Mathf.CeilToInt(State.Wave)}s",$"ECLIPSE {Mathf.CeilToInt(State.Wave)}s"):L("ĐỢT ","WAVE ")+State.PeriodicWave;
                    GUI.Label(waveBox,text,waveStyle);
                }
            }
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

        bool minimapCollapsed;
        void DrawMinimap(Player p)
        {
            if(p==null||State==null||minimapTex==null)return;
            if(craftOpen||inventoryOpen)return;

            float mapSize=136f;
            float mapX=Screen.width-mapSize-14f;
            float mapY=48f;

            if(minimapCollapsed)
            {
                Rect collRect=new Rect(Screen.width-44,mapY,32,32);
                RegisterHudRect(collRect);
                PixelPanel(collRect,new Color(.14f,.10f,.22f,.95f));
                var oldContent=GUI.contentColor;
                GUI.contentColor=Cyan;
                GUI.Label(new Rect(collRect.x+2,collRect.y+2,28,28),"🗺️",small);
                GUI.contentColor=oldContent;
                if(GUI.Button(collRect,new GUIContent("",L("Mở bản đồ","Open minimap")),GUIStyle.none))
                    minimapCollapsed=false;
                return;
            }

            Rect frameRect=new Rect(mapX-3,mapY-3,mapSize+6,mapSize+22);
            RegisterHudRect(frameRect);

            // Dread / Hallucination impact
            float dreadRatio=Mathf.Clamp01((p.Dread-50f)/50f);
            bool hallucinating=p.Mad||p.Dread>=60f;
            float jx=0f, jy=0f;
            Color borderTint=new Color(.20f,.15f,.32f,.95f);
            if(hallucinating)
            {
                float speed=p.Mad?32f:22f;
                float amt=dreadRatio*(p.Mad?3.2f:2.0f);
                jx=Mathf.Sin(Time.time*speed)*amt;
                jy=Mathf.Cos(Time.time*(speed*0.9f))*amt;
                float crimsonPulse=Mathf.PingPong(Time.time*3.5f,1f);
                borderTint=Color.Lerp(new Color(.22f,.16f,.34f,.95f),new Color(.82f,.14f,.24f,.95f),crimsonPulse*dreadRatio);
            }

            PixelPanel(new Rect(frameRect.x+jx,frameRect.y+jy,frameRect.width,frameRect.height),borderTint);

            // Header label & collapse button
            UiLabel(new Rect(mapX+jx+2,mapY+jy-2,mapSize-20,16),L("Bản đồ","Minimap"),small);
            Rect collBtn=new Rect(mapX+jx+mapSize-16,mapY+jy-2,16,14);
            var oldCol=GUI.contentColor;
            GUI.contentColor=new Color(.75f,.72f,.85f);
            GUI.Label(collBtn,"-",small);
            GUI.contentColor=oldCol;
            if(GUI.Button(collBtn,new GUIContent("",L("Thu gọn bản đồ","Collapse minimap")),GUIStyle.none))
            {
                minimapCollapsed=true;
                return;
            }

            // Explored terrain texture
            Rect texRect=new Rect(mapX+jx,mapY+jy+16,mapSize,mapSize);
            if(Event.current.type==EventType.Repaint)
            {
                GUI.DrawTexture(texRect,minimapTex,ScaleMode.StretchToFill,false);
            }

            int size=State.Size;

            // Beacon pip at center (0, 0)
            float bNormX=(0f+size/2f)/size;
            float bNormY=1f-(0f+size/2f)/size;
            float bPipX=texRect.x+bNormX*mapSize;
            float bPipY=texRect.y+bNormY*mapSize;
            float beaconPulse=1f+Mathf.Sin(Time.time*4f)*.15f;
            Panel(new Rect(bPipX-3*beaconPulse,bPipY-3*beaconPulse,6*beaconPulse,6*beaconPulse),Cyan);

            // Teammates & Local Player pips
            var players=State.Players;
            if(players!=null)
            {
                for(int pi=0;pi<players.Length;pi++)
                {
                    var p2=players[pi];
                    if(p2.Extracted)continue;
                    float normX=(p2.Pos.X+size/2f)/size;
                    float normY=1f-(p2.Pos.Y+size/2f)/size;
                    if(normX<0||normX>1||normY<0||normY>1)continue;
                    float pipX=texRect.x+normX*mapSize;
                    float pipY=texRect.y+normY*mapSize;
                    if(p2.Id==p.Id)
                    {
                        // Local player: gold/white glowing indicator
                        Panel(new Rect(pipX-3,pipY-3,6,6),Color.white);
                        Panel(new Rect(pipX-2,pipY-2,4,4),Gold);
                    }
                    else if(p2.Ghost)
                    {
                        // Ghost teammate: pulsing pale gray
                        float gA=Mathf.PingPong(Time.time*3f,1f);
                        Panel(new Rect(pipX-2,pipY-2,4,4),new Color(.7f,.75f,.85f,gA));
                    }
                    else
                    {
                        // Active teammate: cyan pip
                        Panel(new Rect(pipX-2,pipY-2,4,4),Cyan);
                    }
                }
            }

            // Phantom hallucination blips when dread is high!
            if(hallucinating)
            {
                int phantomCount=p.Mad?4:2;
                float normX=(p.Pos.X+size/2f)/size;
                float normY=1f-(p.Pos.Y+size/2f)/size;
                float playerPipX=texRect.x+normX*mapSize;
                float playerPipY=texRect.y+normY*mapSize;
                for(int fi=0;fi<phantomCount;fi++)
                {
                    float pt=Time.time*2.2f+fi*3.7f;
                    if(Mathf.Sin(pt*2.3f)>-0.2f)
                    {
                        float phDist=(14f+fi*11f)*dreadRatio;
                        float px=playerPipX+Mathf.Sin(pt)*phDist;
                        float py=playerPipY+Mathf.Cos(pt*1.3f)*phDist;
                        if(px>=texRect.x&&px<=texRect.xMax-3&&py>=texRect.y&&py<=texRect.yMax-3)
                        {
                            Panel(new Rect(px-2,py-2,4,4),Coral);
                        }
                    }
                }
                // Crimson noise scanline
                float scanY=texRect.y+Mathf.Repeat(Time.time*45f,mapSize);
                Panel(new Rect(texRect.x,scanY,mapSize,1),new Color(.9f,.2f,.3f,.25f));
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

        void DrawActiveBuffs(Player p,float startX,float startY)
        {
            if(p.Buffs==null||p.Buffs.Count==0)return;
            float curX=startX,curY=startY;
            for(int i=0;i<p.Buffs.Count;i++)
            {
                var b=p.Buffs[i];
                if(b.Duration<=0)continue;
                var def=Catalog.GetBuffDef(b.Kind);
                string valStr=def.FormatValue(b.Value,b.Stacks);
                string iconPrefix=b.Kind==BuffKind.Swiftness?"⚡ ":b.Kind==BuffKind.AttackPower?"⚔️ ":b.Kind==BuffKind.IronSkin?"🛡️ ":b.Kind==BuffKind.Regen?"💖 ":b.Kind==BuffKind.StaminaSurge?"🏃 ":"✦ ";
                string text=$"{iconPrefix}{def.DisplayName} {valStr} ({b.Duration:0.0}s)";
                float badgeW=Mathf.Clamp(small.CalcSize(new GUIContent(text)).x+18,110,220);
                Rect badgeRect=new Rect(curX,curY,badgeW,22);
                Color badgeBg=b.Kind==BuffKind.Swiftness?new Color(.12f,.28f,.38f,.90f):b.Kind==BuffKind.IronSkin?new Color(.38f,.30f,.12f,.90f):b.Kind==BuffKind.Regen?new Color(.12f,.38f,.18f,.90f):new Color(.20f,.16f,.32f,.90f);
                PixelPanel(badgeRect,badgeBg);
                float ratio=Mathf.Clamp01(b.Duration/Mathf.Max(0.1f,b.MaxDuration));
                Panel(new Rect(badgeRect.x+2,badgeRect.yMax-3,(badgeRect.width-4)*ratio,2),b.Kind==BuffKind.Swiftness?Cyan:Gold);
                UiLabel(new Rect(badgeRect.x+6,badgeRect.y+1,badgeRect.width-12,19),text,small);
                curX+=badgeW+6;
                if(curX+110>Screen.width-150){curX=startX;curY+=26;}
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
            DrawActiveBuffs(p,16,upperY+36);

            if(UiButton(new Rect(toolX-toolWidth-8,12,toolWidth,29),L("Kho đồ [I]","Inventory [I]"))){inventoryOpen=!inventoryOpen;craftOpen=false;}
            if(network)DrawPartyBubbles();
            DrawMinimap(p);

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
            if(p.Pos.Length<4.5f&&!p.Ghost)
            {
                var hearthChip=new Rect(x,itemChip.y-32,width,28);
                PixelPanel(hearthChip,new Color(.16f,.14f,.25f,.92f));
                string hearthText=L("[Z] Pha trà Bụi quang (2 Bụi)   [C] Mài cuốc đá (2 Quặng)","[Z] Lumidust Tea (2 Dust)   [C] Sharpen Pickaxe (2 Ore)");
                UiLabel(new Rect(hearthChip.x+8,hearthChip.y+4,hearthChip.width-16,20),hearthText,small);
            }
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
            int count=(p.Vessel==Vessel.Wanderer&&!p.Awake)?4:5;
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
                    DrawSpritePreview(icon,Frame(p.Vessel,"idle",0));
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
