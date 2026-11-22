using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        static readonly string[] ActionIcons={"torch","flare","sedative","revival"};
        static readonly Color FieldPaper=new Color(.085f,.068f,.14f,.97f);
        GUIStyle hudBeaconHeaderStyle,hudBeaconEnergyStyle,hudArrowStyle,hudPctStyle,hudCollapseBtnStyle;
        GUIStyle hudWaveStyle,hudMarqueeOutlineStyle,hudAnnounceStyle,hudMapIconStyle,hudMapHeaderStyle;
        GUIStyle hudBuffLabelStyle,hudSlotLabelStyle,hudModalTitleStyle,hudModalStatusStyle,hudModalMeterLabelStyle;
        GUIStyle hudModalQuickBtnStyle,hudModalFooterStyle,hudDepositNameStyle,hudDepositRateStyle,hudDepositOwnedStyle,hudDepositBtnStyle;

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
            if(p==null||p.Ghost||p.Extracted||p.Items==null)return false;
            int itm=(heldSlot>=2&&heldSlot<=5)?(heldSlot-2):(heldSlot>=10&&heldSlot<40)?(heldSlot-10):selectedItem;
            if(itm<0||itm>=p.Items.Length||p.Items[itm]<=0)return false;
            if(itm==3)return p.Pos.Length<=5&&State?.Players!=null&&System.Array.Exists(State.Players,q=>q.Ghost);
            return true;
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
                    // Tường đá (Wall / Barricade) - Luôn hiển thị thanh máu overhead nổi bật
                    if(s.HP<=0)continue;
                    float ratio=Mathf.Clamp01(s.HP/150f);
                    Vector2 anchor=ScreenPos(s.Pos+new V2(0,.92f));
                    float x=Mathf.Round(anchor.x), y=Mathf.Round(anchor.y);
                    // Khung viền đen nổi bật
                    Panel(new Rect(x-22,y-8,44,9),new Color(.06f,.05f,.10f,.95f));
                    Panel(new Rect(x-21,y-7,42,7),new Color(.18f,.16f,.22f,.90f));
                    Color fill=ratio<=.25f?Coral:ratio<=.6f?Gold:new Color(.35f,.88f,.72f);
                    if(ratio>0)
                    {
                        Panel(new Rect(x-20,y-6,Mathf.Round(40*ratio),5),fill);
                        Panel(new Rect(x-20,y-6,Mathf.Round(40*ratio),2),Color.Lerp(fill,Color.white,.35f));
                    }
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
                    var def=Catalog.GetTileHarvestDef(ht.Tx,ht.Ty,ht.Tile);
                    float barW=def.Tier==2?54f:def.Tier==0?32f:40f;
                    float yOff=ht.Tile==3?(def.Tier==2?2.35f:def.Tier==0?1.45f:1.85f):(def.Tier==2?1.15f:def.Tier==0?0.70f:0.90f);
                    Vector2 anchor=ScreenPos(tPos+new V2(0,yOff));
                    float x=Mathf.Round(anchor.x), y=Mathf.Round(anchor.y);
                    Panel(new Rect(x-barW/2f,y-7,barW,7),new Color(.05f,.04f,.09f,.92f));
                    Color fill=ratio<.3f?Coral:def.Tier==2?new Color(1f,.82f,.25f):ht.Tile==3?new Color(.35f,.95f,.45f):new Color(.38f,.85f,1f);
                    Panel(new Rect(x-barW/2f+1,y-6,(barW-2)*ratio,5),fill);
                    Panel(new Rect(x-barW/2f+1,y-6,(barW-2)*ratio,1),new Color(1f,1f,1f,.25f));
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

            float bw=156f, bh=48f;
            Rect panelRect=new Rect(Mathf.Round(screen.x-bw/2),Mathf.Round(screen.y-bh),bw,bh);
            RegisterHudRect(panelRect);
            if(Event.current.type==EventType.MouseDown&&panelRect.Contains(Event.current.mousePosition)&&p.Pos.Length<=4.5f)
            {
                beaconDepositOpen=!beaconDepositOpen;
                if(beaconDepositOpen){craftOpen=false;inventoryOpen=false;}
                Event.current.Use();
            }

            PixelPanel(panelRect,new Color(.08f,.06f,.14f,.94f));

            // Header title
            if(hudBeaconHeaderStyle==null)hudBeaconHeaderStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
            hudBeaconHeaderStyle.normal.textColor=new Color(.92f,.90f,1f);
            GUI.Label(new Rect(panelRect.x+4,panelRect.y+4,panelRect.width-8,16),tierTitle,hudBeaconHeaderStyle);

            // Energy meter
            float chargeRatio=Mathf.Clamp01(curCharge/(float)targetCharge);
            float mw=panelRect.width-20;
            Rect barBg=new Rect(panelRect.x+10,panelRect.y+22,mw,5);
            Panel(barBg,new Color(.04f,.03f,.08f,.95f));
            Color meterCol=overcharged?new Color(.35f,1f,.85f):new Color(.28f,.85f,1f);
            Panel(new Rect(barBg.x+1,barBg.y+1,(mw-2)*chargeRatio,3),meterCol);

            // Energy text under meter
            if(hudBeaconEnergyStyle==null)hudBeaconEnergyStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter};
            hudBeaconEnergyStyle.normal.textColor=overcharged?new Color(.4f,1f,.88f):new Color(.78f,.92f,1f);
            GUI.Label(new Rect(panelRect.x+4,panelRect.y+28,panelRect.width-8,16),energyText,hudBeaconEnergyStyle);
        }

        void DrawInteractPrompt(Player p)
        {
            V2 targetPos;
            string caption;
            Structure merchant=null,mysteryChest=null,chest=null,groundTorch=null,groundWall=null;
            float merchantDist=EmberMerchant.MerchantInteractionDistance,mysteryDist=2.2f,chestDist=2f,torchDist=2.0f,wallDist=2.0f;
            if(State.Structures!=null)
            {
                for(int si=0;si<State.Structures.Length;si++)
                {
                    var s=State.Structures[si];
                    if(s==null)continue;
                    float d=V2.Distance(s.Pos,p.Pos);
                    if(EmberMerchant.IsMerchant(s)&&d<merchantDist){merchantDist=d;merchant=s;}
                    if(s.Kind==12&&s.HP>0&&d<mysteryDist){mysteryDist=d;mysteryChest=s;}
                    if(s.Kind==4&&d<chestDist){chestDist=d;chest=s;}
                    if(s.Kind==0&&s.Life>0&&d<torchDist){torchDist=d;groundTorch=s;}
                    if(s.Kind==2&&s.HP>0&&d<wallDist){wallDist=d;groundWall=s;}
                }
            }
            if(mysteryChest!=null)
            {
                targetPos=mysteryChest.Pos+new V2(0,.65f);
                caption=L("Mở Rương Bỏ Hoang","Open Mystery Chest");
            }
            else if(merchant!=null&&!EmberMerchant.DialogOpen)
            {
                targetPos=merchant.Pos+new V2(0,1.85f);
                caption=L("Giao dịch Thương Nhân","Trade with Merchant");
            }
            else if(State.Phase==Phase.Extraction&&p.Pos.Length<3)
            {
                targetPos=new V2(0,1.2f);
                caption=L("Rút lui","Extract");
            }
            else
            {
                if(chest!=null)
                {
                    targetPos=chest.Pos+new V2(0,.65f);
                    caption=L("Rương chung","Shared chest");
                }
                else if(groundTorch!=null)
                {
                    targetPos=groundTorch.Pos+new V2(0,.65f);
                    caption=L("Thu Hồi Đuốc","Retrieve Torch");
                }
                else if(groundWall!=null)
                {
                    targetPos=groundWall.Pos+new V2(0,.78f);
                    caption=L("Nhấc Tường Đá","Pick up Stone Wall");
                }
                else
                {
                    Node node=null;
                    float nodeDist=2.4f;
                    if(State.Nodes!=null)
                    {
                        for(int ni=0;ni<State.Nodes.Length;ni++)
                        {
                            var n=State.Nodes[ni];
                            if(n==null)continue;
                            float d=V2.Distance(n.Pos,p.Pos);
                            if(d<nodeDist){nodeDist=d;node=n;}
                        }
                    }
                    if(node!=null)
                    {
                        // Undropped resource veins/bushes/mushrooms have dynamic interactive glow when approached.
                        // Only show interact prompt for dropped loot on the ground!
                        if(!node.Dropped) return;
                        else
                        {
                            string itemName=Catalog.GetDroppedDisplayName(node.DropKind,node.DropId,node.Resource,EmberLanguage.English);
                            string verb=(node.DropKind==0&&node.Resource==Resource.Wood)
                                ?L("Nhặt cành","Pick up branch")
                                :L("Nhặt","Pick up");
                            string countStr=(node.DropKind==1&&node.Amount<=1)?"":$" ×{node.Amount}";
                            targetPos=node.Pos+new V2(0,.55f);
                            caption=$"{verb} · {itemName}{countStr}";
                        }
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
            float w=Mathf.Clamp(small.CalcSize(new GUIContent(full)).x+26,104,320);
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
                bool hasNearbyStructure=false;
                if(State?.Structures!=null)
                {
                    for(int si=0;si<State.Structures.Length;si++)
                    {
                        var s=State.Structures[si];
                        if(s!=null&&((s.Kind==0&&s.Life>0)||s.Kind==2||s.Kind==4||(s.Kind==12&&s.HP>0))&&V2.Distance(s.Pos,p.Pos)<2.5f)
                        {
                            hasNearbyStructure=true;
                            break;
                        }
                    }
                }
                if(!hasNearbyStructure&&State?.Nodes!=null)
                {
                    for(int ni=0;ni<State.Nodes.Length;ni++)
                    {
                        var n=State.Nodes[ni];
                        if(n!=null&&(n.Dropped||(!n.Dropped&&n.DropKind==2&&n.DropId==16))&&V2.Distance(n.Pos,p.Pos)<2.5f)
                        {
                            hasNearbyStructure=true;
                            break;
                        }
                    }
                }
                if(merchant!=null&&!EmberMerchant.DialogOpen)
                {
                    EmberMerchant.DialogOpen=true;
                    inventoryOpen=false;
                    craftOpen=false;
                }
                else if(!hasNearbyStructure&&ShouldAutoEquipPickaxe(p))
                {
                    AutoEquipHarvestTool(p);
                    intent.Interact=true;
                }
                else if(!hasNearbyStructure&&IsNearTreeOrRock(p)&&!HasHarvestTool(p))
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
                    alignment=TextAnchor.MiddleCenter,clipping=TextClipping.Overflow,
                    normal={textColor=Color.black}
                };
                overheadGlow=new GUIStyle(overheadName){normal={textColor=Cyan}};
            }
            var text=new GUIContent(player.Name??"");
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
                partyName=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=11,wordWrap=false,clipping=TextClipping.Overflow};
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
                hint=L("Mang tài nguyên về trụ · B mở menu nạp năng lượng","Bring resources to beacon · B to open deposit menu");
                progress=State.Charge/target;
                icon="core";
            }

            if(QuestCollapsed)
            {
                // Compact circular quest bubble (enlarged & with separated expand button)
                Vector2 center=new Vector2(46,y+34);
                float rad=26f;
                Rect bubbleRect=new Rect(center.x-rad,center.y-rad,rad*2,rad*2);
                float arrowX=center.x+rad+10f;
                Rect arrowBadge=new Rect(arrowX,center.y-11,22,22);
                Rect clickArea=new Rect(center.x-rad,center.y-rad,(arrowX+22)-(center.x-rad),rad*2+20);
                RegisterHudRect(clickArea);

                bool hoverBubble=clickArea.Contains(Event.current.mousePosition);

                // Dark circular background
                GUI.color=blackout?new Color(.35f,.11f,.22f,.95f):hoverBubble?new Color(.24f,.16f,.38f,.98f):new Color(.11f,.08f,.18f,.95f);
                DrawSpritePreview(new Rect(center.x-22,center.y-22,44,44),Primitive("circle"));
                GUI.color=Color.white;

                // Circular progress ring
                Color ringColor=blackout?Coral:progress>=1f?Cyan:Gold;
                DrawCircularProgress(center,25,progress,ringColor);

                // Quest icon in center
                Rect iconRect=new Rect(center.x-13,center.y-13,26,26);
                if(icon=="torch")DrawSpritePreview(iconRect,Primitive("torch"));
                else PixelArt(iconRect,icon);

                // Square expand button [ > ] separated by a clean 10px gap from bubble
                PixelPanel(arrowBadge,hoverBubble?new Color(.52f,.32f,.78f,.98f):new Color(.26f,.18f,.42f,.95f));
                var oldContent=GUI.contentColor;
                GUI.contentColor=hoverBubble?Gold:new Color(1f,.92f,.55f);
                if(hudArrowStyle==null)hudArrowStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=13};
                GUI.Label(arrowBadge,">",hudArrowStyle);
                GUI.contentColor=oldContent;

                // Percentage text under the bubble
                string pctText=Mathf.RoundToInt(Mathf.Clamp01(progress)*100)+"%";
                if(hudPctStyle==null)hudPctStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=13};
                hudPctStyle.normal.textColor=hoverBubble?Gold:new Color(.90f,.92f,1f);
                GUI.Label(new Rect(center.x-26,center.y+26,52,16),pctText,hudPctStyle);

                // Click to expand
                if(GUI.Button(clickArea,new GUIContent("",heading+"\n"+hint+"\n"+L("Click để mở rộng nhiệm vụ","Click to expand quest")),GUIStyle.none))
                {
                    QuestCollapsed=false;
                }
                return;
            }

            var card=new Rect(16,y,Mathf.Min(380,Screen.width-32),78);
            RegisterHudRect(card);
            PixelPanel(card,blackout?new Color(.31f,.11f,.21f):FieldPaper);

            var cIconRect=new Rect(card.x+12,card.y+12,32,32);
            if(icon=="torch")DrawSpritePreview(cIconRect,Primitive("torch"));
            else PixelArt(cIconRect,icon);
            UiLabel(new Rect(card.x+54,card.y+6,card.width-94,22),heading,label);
            UiLabel(new Rect(card.x+54,card.y+28,card.width-94,38),hint,small);

            // Large, clear collapse button in upper right of card
            Rect collapseBtn=new Rect(card.xMax-34,card.y+6,28,28);
            bool hoverCollapse=collapseBtn.Contains(Event.current.mousePosition);
            PixelPanel(collapseBtn,hoverCollapse?new Color(.48f,.30f,.72f,.98f):new Color(.26f,.19f,.42f,.95f));
            var oldCol=GUI.contentColor;
            GUI.contentColor=hoverCollapse?Gold:new Color(.90f,.92f,1f);
            if(hudCollapseBtnStyle==null)hudCollapseBtnStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=16};
            GUI.Label(collapseBtn,"—",hudCollapseBtnStyle);
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
                    string actor=State.EventActor;
                    string evtText=EmberLanguage.Text(State.Event);
                    string target=State.EventTarget;
                    string message;
                    if(!string.IsNullOrEmpty(actor))
                    {
                        message=string.IsNullOrEmpty(target)?$"{actor}: {evtText}":$"{actor}: {evtText} {target}";
                    }
                    else
                    {
                        message=string.IsNullOrEmpty(target)?evtText:$"{evtText} {target}";
                    }
                    y+=DrawFieldNotice(y,"beacon",message);
                }
            }

            bool hasNearbyEnemy=false;
            if(player.Pet==(int)Pet.Bat&&State.Enemies!=null)
            {
                for(int ei=0;ei<State.Enemies.Length;ei++)
                {
                    var e=State.Enemies[ei];
                    if(e!=null&&e.HP>0&&V2.Distance(e.Pos,player.Pos)<10){hasNearbyEnemy=true;break;}
                }
            }
            if(hasNearbyEnemy)
            {
                DrawFieldNotice(y,"pet_bat",L("Dơi phát hiện quái ở gần","Bat spotted a nearby enemy"));
            }
            else if(player.Pet==(int)Pet.CrystalCat&&(int)State.Time%30<5)
            {
                Node core=null;
                float bestCoreDist=float.MaxValue;
                if(State.Nodes!=null)
                {
                    for(int ni=0;ni<State.Nodes.Length;ni++)
                    {
                        var n=State.Nodes[ni];
                        if(n!=null&&n.Resource==Resource.Core)
                        {
                            float d=V2.Distance(n.Pos,player.Pos);
                            if(d<bestCoreDist){bestCoreDist=d;core=n;}
                        }
                    }
                }
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
                    if(hudWaveStyle==null)hudWaveStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=11,fontStyle=FontStyle.Bold};
                    hudWaveStyle.normal.textColor=eclipse?Coral:new Color(1f,.88f,.45f);
                    string text=eclipse?L($"NGUYỆT THỰC {Mathf.CeilToInt(State.Wave)}s",$"ECLIPSE {Mathf.CeilToInt(State.Wave)}s"):L("ĐỢT ","WAVE ")+State.PeriodicWave;
                    GUI.Label(waveBox,text,hudWaveStyle);
                }
            }
        }

        void DrawMarqueeOutlineLabel(Rect rect,string text,GUIStyle baseStyle,Color textColor,Color shadowColor,float speed=52f)
        {
            if(string.IsNullOrEmpty(text))return;
            if(hudMarqueeOutlineStyle==null)
            {
                hudMarqueeOutlineStyle=new GUIStyle(baseStyle)
                {
                    wordWrap=false,
                    clipping=TextClipping.Clip,
                    alignment=TextAnchor.MiddleCenter
                };
            }
            hudMarqueeOutlineStyle.font=baseStyle.font;
            hudMarqueeOutlineStyle.fontSize=baseStyle.fontSize;
            hudMarqueeOutlineStyle.fontStyle=baseStyle.fontStyle;
            hudMarqueeOutlineStyle.alignment=TextAnchor.MiddleCenter;
            var style=hudMarqueeOutlineStyle;
            float textW=style.CalcSize(new GUIContent(text)).x;
            if(textW<=rect.width-6f)
            {
                // Fits without overflow: Draw centered with 8-way pixel outline & drop shadow
                style.normal.textColor=shadowColor;
                for(int ox=-1;ox<=1;ox++)
                for(int oy=-1;oy<=1;oy++)
                {
                    if(ox!=0||oy!=0)
                        GUI.Label(new Rect(rect.x+ox,rect.y+oy,rect.width,rect.height),text,style);
                }
                GUI.Label(new Rect(rect.x+1,rect.y+2,rect.width,rect.height),text,style);
                GUI.Label(new Rect(rect.x+2,rect.y+2,rect.width,rect.height),text,style);

                style.normal.textColor=textColor;
                GUI.Label(rect,text,style);
                return;
            }

            // Exceeds banner width: Smooth marquee horizontal scrolling
            string loop=text+"       ✦       ";
            style.alignment=TextAnchor.MiddleLeft;
            float cycleW=style.CalcSize(new GUIContent(loop)).x;
            if(cycleW<1f)cycleW=textW+48f;
            float offset=(clock*speed)%cycleW;

            GUI.BeginClip(rect);
            // 8-way outline & shadow in clipped space
            style.normal.textColor=shadowColor;
            for(int ox=-1;ox<=1;ox++)
            for(int oy=-1;oy<=1;oy++)
            {
                if(ox!=0||oy!=0)
                {
                    GUI.Label(new Rect(-offset+ox,oy,cycleW+16f,rect.height),loop,style);
                    GUI.Label(new Rect(cycleW-offset+ox,oy,cycleW+16f,rect.height),loop,style);
                    if(cycleW<rect.width)
                        GUI.Label(new Rect(cycleW*2f-offset+ox,oy,cycleW+16f,rect.height),loop,style);
                }
            }
            GUI.Label(new Rect(-offset+1,2,cycleW+16f,rect.height),loop,style);
            GUI.Label(new Rect(cycleW-offset+1,2,cycleW+16f,rect.height),loop,style);
            if(cycleW<rect.width)
                GUI.Label(new Rect(cycleW*2f-offset+1,2,cycleW+16f,rect.height),loop,style);

            // Front colored text in clipped space
            style.normal.textColor=textColor;
            GUI.Label(new Rect(-offset,0,cycleW+16f,rect.height),loop,style);
            GUI.Label(new Rect(cycleW-offset,0,cycleW+16f,rect.height),loop,style);
            if(cycleW<rect.width)
                GUI.Label(new Rect(cycleW*2f-offset,0,cycleW+16f,rect.height),loop,style);
            GUI.EndClip();
        }

        string lastNotice;
        float noticeTime;

        void DrawHeaderAnnouncements(Player p)
        {
            if(State==null)return;
            float topY=64f;
            float centerX=Screen.width*0.5f;

            // 1. When near the beacon (within 4.5 tiles): pulsing / blinking [B] NẠP NĂNG LƯỢNG
            bool nearBeacon=p!=null&&!p.Ghost&&p.Pos.Length<=4.5f;
            if(nearBeacon)
            {
                float bannerW=Mathf.Clamp(Screen.width-360f,340f,560f);
                Rect textRect=new Rect(centerX-bannerW*0.5f,topY,bannerW,26f);
                RegisterHudRect(textRect);

                if(Event.current.type==EventType.MouseDown&&textRect.Contains(Event.current.mousePosition))
                {
                    beaconDepositOpen=!beaconDepositOpen;
                    if(beaconDepositOpen){craftOpen=false;inventoryOpen=false;}
                    Event.current.Use();
                }

                if(Event.current.type==EventType.Repaint)
                {
                    float pulse=Mathf.PingPong(Time.time*3.6f,1f);
                    Color blinkCol=Color.Lerp(Gold,Cyan,pulse);
                    int cores=p.Bag[(int)Resource.Core];
                    string prompt=beaconDepositOpen
                        ?L("[B] ĐÓNG MENU NẠP HẢI ĐĂNG","[B] CLOSE BEACON DEPOSIT")
                        :cores>0
                            ?L($"[B] NẠP NĂNG LƯỢNG (Đang có {cores} Lõi · Click mở menu)","[B] DEPOSIT ENERGY ({cores} Cores · Click to open)")
                            :L("[B] NẠP NĂNG LƯỢNG HẢI ĐĂNG (Click mở menu)","[B] BEACON ENERGY DEPOSIT (Click to open)");

                    if(hudAnnounceStyle==null)hudAnnounceStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=15};
                    DrawMarqueeOutlineLabel(textRect,prompt,hudAnnounceStyle,blinkCol,new Color(0f,0f,0f,.95f),52f);
                }
                return;
            }

            if(Event.current.type!=EventType.Repaint)return;

            // 2. Active World Event / Announcement text (large, pixel-style with outline, no background, reusable for wave/boss alerts)
            if(!string.IsNullOrEmpty(State.Event)&&State.Tick-State.EventTick<260)
            {
                string evt=State.Event;
                Color evtColor=Gold;
                if(evt.Contains("Boss")||evt.Contains("BOSS")||evt.Contains("Thực thể"))evtColor=Coral;
                else if(evt.Contains("Hộ Vệ")||evt.Contains("Sentinel")||evt.Contains("Lõi")||evt.Contains("Core"))evtColor=Cyan;
                else if(evt.Contains("Đợt")||evt.Contains("Wave"))evtColor=new Color(1f,.85f,.35f);

                if(hudAnnounceStyle==null)hudAnnounceStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=15};
                float bannerW=Mathf.Clamp(Screen.width-360f,340f,560f);
                Rect evtRect=new Rect(centerX-bannerW*0.5f,topY,bannerW,26f);
                DrawMarqueeOutlineLabel(evtRect,evt,hudAnnounceStyle,evtColor,new Color(0f,0f,0f,.95f),52f);
                return;
            }

            // 3. Active player notice / feedback (e.g. tool hints, deposit warnings, MOBA toggle)
            if(!string.IsNullOrEmpty(notice))
            {
                if(notice!=lastNotice)
                {
                    lastNotice=notice;
                    noticeTime=clock;
                }
                if(clock-noticeTime<3.8f)
                {
                    if(hudAnnounceStyle==null)hudAnnounceStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=15};
                    float bannerW=Mathf.Clamp(Screen.width-360f,340f,560f);
                    Rect noticeRect=new Rect(centerX-bannerW*0.5f,topY,bannerW,26f);
                    DrawMarqueeOutlineLabel(noticeRect,notice,hudAnnounceStyle,new Color(1f,.92f,.55f),new Color(0f,0f,0f,.95f),52f);
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
                inventoryOpen=false;craftOpen=false;beaconDepositOpen=false;pendingQuickAssign=0;
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
            if(craftOpen||inventoryOpen||beaconDepositOpen)return;

            float mapSize=176f;
            float rightPad=12f;
            float mapRight=Screen.width-rightPad;
            float mapY=54f;

            if(minimapCollapsed)
            {
                Rect collRect=new Rect(mapRight-38,mapY,38,38);
                RegisterHudRect(collRect);
                bool hoverMap=collRect.Contains(Event.current.mousePosition);
                PixelPanel(collRect,hoverMap?new Color(.38f,.26f,.58f,.98f):new Color(.16f,.12f,.25f,.95f));
                if(hudMapIconStyle==null)hudMapIconStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=13,fontStyle=FontStyle.Bold,normal={textColor=Color.white}};
                GUI.Label(collRect,"MAP",hudMapIconStyle);
                if(GUI.Button(collRect,new GUIContent("",L("Mở bản đồ","Open minimap")),GUIStyle.none))
                    minimapCollapsed=false;
                return;
            }

            float headerH=26f;
            Rect frameRect=new Rect(mapRight-(mapSize+8),mapY-4,mapSize+8,mapSize+headerH+8);
            float mapX=frameRect.x+4;
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

            // Count visible / explored monsters
            int visibleMonsters=0;
            if(State.Enemies!=null)
            {
                for(int ei=0;ei<State.Enemies.Length;ei++)
                {
                    var e=State.Enemies[ei];
                    if(e!=null&&e.HP>0&&IsExplored(e.Pos))visibleMonsters++;
                }
            }

            // Header label & crisp 18x18 square collapse button (flat 2px minus bar)
            string mapTitle=L($"Bản đồ · {visibleMonsters} quái",$"Minimap · {visibleMonsters} mobs");
            if(hudMapHeaderStyle==null)hudMapHeaderStyle=new GUIStyle(small){alignment=TextAnchor.MiddleLeft,fontStyle=FontStyle.Bold,clipping=TextClipping.Overflow};
            hudMapHeaderStyle.normal.textColor=new Color(.92f,.90f,1f);
            GUI.Label(new Rect(mapX+jx+6,mapY+jy+2,mapSize-30,22),mapTitle,hudMapHeaderStyle);

            Rect collBtn=new Rect(mapX+jx+mapSize-20,mapY+jy+4,18,18);
            bool hoverMinimapColl=collBtn.Contains(Event.current.mousePosition);
            Panel(collBtn,hoverMinimapColl?new Color(.48f,.28f,.70f,.98f):new Color(.20f,.14f,.32f,.92f));
            Panel(new Rect(collBtn.x,collBtn.y,collBtn.width,1),new Color(.55f,.40f,.78f,.90f));
            Panel(new Rect(collBtn.x,collBtn.yMax-1,collBtn.width,1),new Color(.10f,.08f,.16f,.90f));
            Panel(new Rect(collBtn.x,collBtn.y,1,collBtn.height),new Color(.55f,.40f,.78f,.90f));
            Panel(new Rect(collBtn.xMax-1,collBtn.y,1,collBtn.height),new Color(.10f,.08f,.16f,.90f));
            Panel(new Rect(collBtn.x+4,collBtn.y+8,10,2),hoverMinimapColl?Gold:new Color(.92f,.90f,1f));
            if(GUI.Button(collBtn,new GUIContent("",L("Thu gọn bản đồ","Collapse minimap")),GUIStyle.none))
            {
                minimapCollapsed=true;
                return;
            }

            // Explored terrain texture
            Rect texRect=new Rect(mapX+jx,mapY+jy+headerH,mapSize,mapSize);
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

            // Enemies (red pips for explored / visible enemies)
            if(State.Enemies!=null)
            {
                for(int ei=0;ei<State.Enemies.Length;ei++)
                {
                    var e=State.Enemies[ei];
                    if(e==null||e.HP<=0||!IsExplored(e.Pos))continue;
                    float normX=(e.Pos.X+size/2f)/size;
                    float normY=1f-(e.Pos.Y+size/2f)/size;
                    if(normX<0||normX>1||normY<0||normY>1)continue;
                    float pipX=texRect.x+normX*mapSize;
                    float pipY=texRect.y+normY*mapSize;
                    if(e.IsWorldBoss||e.Kind==EnemyKind.Boss)
                    {
                        Panel(new Rect(pipX-3,pipY-3,6,6),new Color(.4f,0f,0f,.95f));
                        Panel(new Rect(pipX-2,pipY-2,4,4),Coral);
                    }
                    else if(e.Elite||e.Kind==EnemyKind.Sentinel)
                    {
                        Panel(new Rect(pipX-2,pipY-2,4,4),Coral);
                    }
                    else
                    {
                        Panel(new Rect(pipX-1.5f,pipY-1.5f,3,3),Coral);
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
            DrawHeaderAnnouncements(null);
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

        GUIStyle marqueeStyle,popupStyle,popupBigStyle,popupHugeStyle;

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
                int stamCost=Catalog.WeaponStaminaCost(activeW);
                string stamText=stamCost>0?L($"Tốn {stamCost} thể lực",$"{stamCost} stamina"):L("Không tốn thể lực","0 stamina");
                if(activeW==Weapon.Pickaxe)
                {
                    int bonus=(p.Has(Perk.Miner)?p.Rank(Perk.Miner):0)+p.PickaxeTier;
                    int minYield=1+bonus;
                    int maxYield=3+bonus;
                    string perkStr=p.Has(Perk.Miner)?(EmberLanguage.English?" + Miner":" + Thợ mỏ"):"";
                    string yieldDesc=EmberLanguage.English
                        ?$"Harvest Yield: {minYield}~{maxYield} per strike (Pickaxe T{p.PickaxeTier}{perkStr}) · 8 stamina"
                        :$"Sản lượng khai thác: {minYield}~{maxYield} tài nguyên/lần (Cuốc Cấp {p.PickaxeTier}{perkStr}) · Tốn 8 thể lực";
                    info+=" · "+p.PickaxeName+" ["+L("Độ bền ","Durability ")+p.PickaxeDurability+"/"+p.PickaxeMaxDurability+"] · "+yieldDesc;
                }
                else if(activeW==Weapon.CrystalAxe)
                    info+=" · "+L("Đánh lan 1.6 ô & đẩy lùi mạnh · ","1.6-tile cleave & strong knockback · ")+stamText;
                else if(activeW==Weapon.ShadowBlades)
                    info+=" · "+L("Tốc đánh siêu nhanh · ","Ultra-fast strikes · ")+stamText;
                else if(activeW==Weapon.MirrorShield)
                    info+=" · "+L("Bắn tia phản xạ · ","Reflective bolt · ")+stamText;
                else if(ranged)
                    info+=" · "+L("Tự ngắm mục tiêu gần nhất · ","Auto-aims nearest target · ")+stamText;
                else
                    info+=" · "+L("Đẩy lùi nhẹ · ","Light knockback · ")+stamText;
                return info;
            }
            if(heldSlot==6)
                return L("Tay không · Không đốt nhiên liệu đèn · Phím 1–4 đổi trang bị nhanh · [E] Tự cầm cuốc đào mỏ hoặc nhặt đồ",
                    "Empty hands · Saves torch fuel · Keys 1–4 swap quick slots · [E] Auto-equip pickaxe to mine or loot");

            if(heldSlot>=10&&heldSlot<40)
            {
                int itmId=heldSlot-10;
                string itmName=Catalog.GetItemDisplayName(itmId);
                int itmCount=(p.Items!=null&&itmId<p.Items.Length)?p.Items[itmId]:0;
                string itmQty=itmCount>0?L("Còn x","x")+itmCount:L("Đã hết","Empty");
                string itmDesc=itmId==4?L("Chuột trái hoặc [U] để xây Tường Đá chặn đường quái vật (Khối ảo mờ xem trước)","L-Click or [U] to build Stone Wall (Ghost preview)")
                    :itmId==5?L("Chuột trái hoặc [U] để đặt Bẫy Điện làm chậm & choáng quái","L-Click or [U] to place Shock Trap (stuns monsters)")
                    :itmId==6?L("Chuột trái hoặc [U] để đặt Rương Chung chia sẻ tài nguyên","L-Click or [U] to place Shared Chest")
                    :itmId==14?L("Chuột trái hoặc [U] uống: +35% Tốc chạy & Thể lực vô tận [30s]","L-Click or [U] Drink: +35% Speed & Infinite Stamina [30s]")
                    :itmId==15?L("Chuột trái hoặc [U] ăn: Hồi 25 HP","L-Click or [U] Eat: Restores 25 HP")
                    :itmId==16?L("Chuột trái hoặc [U] ăn: Hồi 15 HP","L-Click or [U] Eat: Restores 15 HP")
                    :itmId==17?L("Chuột trái hoặc [U] ăn: Hồi 45 HP & No Ấm (Kháng hoảng loạn)","L-Click or [U] Eat: Restores 45 HP & Well Fed")
                    :itmId==18?L("Chuột trái hoặc [U] ăn: Hồi 50 HP & Hồi phục thể lực","L-Click or [U] Eat: Restores 50 HP & Regen")
                    :L("Chuột trái hoặc [U] để sử dụng","L-Click or [U] to use");
                return itmName+" ("+itmQty+") · "+itmDesc;
            }
            if(heldSlot>=50&&heldSlot<60)
            {
                int resId=heldSlot-50;
                string[] resEn={"Lumen Dust","Core","Umbra Shard","Wood","Ore"};
                string resName=(EmberLanguage.English?resEn:Catalog.ResourceNames)[Mathf.Clamp(resId,0,4)];
                int resCount=(p.Bag!=null&&resId<p.Bag.Length)?p.Bag[resId]:0;
                return resName+" (x"+resCount+") · "+L("Tài nguyên thô dùng để chế tạo hoặc nộp vào Hải Đăng [Q]","Raw resource used to craft or deposit into Beacon [Q]");
            }

            int idx=Mathf.Clamp(heldSlot-2,0,3);
            string itemName=(EmberLanguage.English?ItemNamesEn:ItemNamesVi)[idx];
            int count=(p.Items!=null&&idx<p.Items.Length)?p.Items[idx]:0;
            string qty=count>0?L("Còn x","x")+count:L("Đã hết","Empty");
            string detail;
            if(idx==0)detail=L("Cầm tay soi sáng (tốn nhiên liệu) · [U]/Chuột trái để cắm đuốc xuống đất (45s) · [T] Tắt/Bật lửa",
                "Hold to light area (uses fuel) · [U]/L-Click to place torch on ground (45s) · [T] Toggle flame");
            else if(idx==1)detail=L("[U]/Chuột trái: Bắn pháo sáng soi rộng 8 ô trong 12s · Xua bóng tối & chống Hoảng loạn",
                "[U]/L-Click: Deploy 8-tile flare for 12s · Banishes dark & prevents Dread");
            else if(idx==2)detail=L("[U]/Chuột trái: Uống giảm ngay 40% Hoảng loạn để tránh ảo giác & đánh nhầm đồng đội",
                "[U]/L-Click: Drink to calm 40% Dread and prevent hallucinations");
            else detail=L("Đứng gần Hải Đăng bấm [U]/Chuột trái để hồi sinh đồng đội gục với 50% HP & 3s bất tử",
                "Stand near Beacon and press [U]/L-Click to revive a downed ally at 50% HP");
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
                    fontSize=18,
                    wordWrap=false,
                    clipping=TextClipping.Overflow
                };
                popupBigStyle=new GUIStyle(popupStyle){fontSize=24};
                popupHugeStyle=new GUIStyle(popupStyle){fontSize=30};
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
                GUIStyle style=popupStyle;
                if(pop.Kind==0)
                {
                    bool isSkillHit=pop.SubKind==1;
                    if(isSkillHit||pop.Amount>=32)
                    {
                        text=isSkillHit?$"✦ -{pop.Amount}":$"-{pop.Amount}";
                        style=isSkillHit?popupHugeStyle:popupBigStyle;
                        tint=isSkillHit?new Color(1f,.86f,.25f,alpha):new Color(1f,.66f,.18f,alpha);
                    }
                    else
                    {
                        text="-"+pop.Amount;
                        style=pop.Amount>=18?popupBigStyle:popupStyle;
                        tint=new Color(1f,.94f,.62f,alpha);
                    }
                }
                else if(pop.Kind==1)
                {
                    text="-"+pop.Amount;
                    style=pop.Amount>=22?popupBigStyle:popupStyle;
                    tint=new Color(1f,.26f,.32f,alpha);
                }
                else if(pop.Kind==2)
                {
                    text="+"+pop.Amount+" HP";
                    style=popupBigStyle;
                    tint=new Color(.34f,1f,.66f,alpha);
                }
                else
                {
                    int rIdx=Mathf.Clamp(pop.SubKind,0,resVi.Length-1);
                    string resName=(EmberLanguage.English?resEn:resVi)[rIdx];
                    text="+"+pop.Amount+" "+resName;
                    style=pop.Amount>=4?popupBigStyle:popupStyle;
                    tint=rIdx==1?new Color(.42f,.96f,1f,alpha):rIdx==2?new Color(.82f,.58f,1f,alpha):new Color(1f,.86f,.34f,alpha);
                }

                var box=new Rect(Mathf.Round(pt.x-105f),Mathf.Round(pt.y-16f),210f,32f);
                style.normal.textColor=new Color(0,0,0,.92f*alpha);
                GUI.Label(new Rect(box.x-2,box.y,box.width,box.height),text,style);
                GUI.Label(new Rect(box.x+2,box.y,box.width,box.height),text,style);
                GUI.Label(new Rect(box.x,box.y-2,box.width,box.height),text,style);
                GUI.Label(new Rect(box.x,box.y+2,box.width,box.height),text,style);
                style.normal.textColor=tint;
                GUI.Label(box,text,style);
            }
        }

        void DrawActiveBuffs(Player p,float startX,float startY)
        {
            float curX=startX,curY=startY;

            // 1. Stamina Buff (Nước Tăng Lực / Energy Drink - Infinite Stamina & +35% Speed)
            if(p!=null&&p.StaminaBuffTimer>0)
            {
                float t=p.StaminaBuffTimer;
                string text=L($"✦ Nước Tăng Lực: Thể lực vô tận [{Mathf.CeilToInt(t)}s]",$"✦ Energy Drink: Infinite Stamina [{Mathf.CeilToInt(t)}s]");
                float badgeW=Mathf.Clamp(small.CalcSize(new GUIContent(text)).x+20,150,270);
                Rect badgeRect=new Rect(curX,curY,badgeW,23);
                PixelPanel(badgeRect,new Color(.08f,.28f,.36f,.95f));
                float ratio=Mathf.Clamp01(t/30f);
                Panel(new Rect(badgeRect.x+2,badgeRect.yMax-3,(badgeRect.width-4)*ratio,2),Cyan);
                if(hudBuffLabelStyle==null)hudBuffLabelStyle=new GUIStyle(small){fontStyle=FontStyle.Bold};
                hudBuffLabelStyle.normal.textColor=new Color(.4f,1f,.95f);
                GUI.Label(new Rect(badgeRect.x+6,badgeRect.y+2,badgeRect.width-12,20),text,hudBuffLabelStyle);
                curX+=badgeW+6;
                if(curX+150>Screen.width-150){curX=startX;curY+=27;}
            }

            // 2. Warmth Buff (No Ấm / Well Fed - +50% Dread Resistance)
            if(p!=null&&p.WarmthBuffTimer>0)
            {
                float t=p.WarmthBuffTimer;
                string text=L($"✦ No Ấm: Kháng sợ hãi +50% [{Mathf.CeilToInt(t)}s]",$"✦ Well Fed: +50% Dread Resist [{Mathf.CeilToInt(t)}s]");
                float badgeW=Mathf.Clamp(small.CalcSize(new GUIContent(text)).x+20,150,270);
                Rect badgeRect=new Rect(curX,curY,badgeW,23);
                PixelPanel(badgeRect,new Color(.36f,.20f,.08f,.95f));
                float ratio=Mathf.Clamp01(t/45f);
                Panel(new Rect(badgeRect.x+2,badgeRect.yMax-3,(badgeRect.width-4)*ratio,2),Gold);
                if(hudBuffLabelStyle==null)hudBuffLabelStyle=new GUIStyle(small){fontStyle=FontStyle.Bold};
                hudBuffLabelStyle.normal.textColor=new Color(1f,.88f,.45f);
                GUI.Label(new Rect(badgeRect.x+6,badgeRect.y+2,badgeRect.width-12,20),text,hudBuffLabelStyle);
                curX+=badgeW+6;
                if(curX+150>Screen.width-150){curX=startX;curY+=27;}
            }

            // 3. Shadow Veil Invisibility (from Merchant Secret Stash)
            if(p!=null)
            {
                float veilTime=EmberMerchant.GetInvisibilityDuration(p.Id);
                if(veilTime>0)
                {
                    string text=L($"✦ Áo Choàng Tàng Hình [{Mathf.CeilToInt(veilTime)}s]",$"✦ Shadow Veil: Invisible [{Mathf.CeilToInt(veilTime)}s]");
                    float badgeW=Mathf.Clamp(small.CalcSize(new GUIContent(text)).x+20,150,270);
                    Rect badgeRect=new Rect(curX,curY,badgeW,23);
                    PixelPanel(badgeRect,new Color(.22f,.10f,.32f,.95f));
                    float ratio=Mathf.Clamp01(veilTime/15f);
                    Panel(new Rect(badgeRect.x+2,badgeRect.yMax-3,(badgeRect.width-4)*ratio,2),new Color(.75f,.40f,1f));
                    if(hudBuffLabelStyle==null)hudBuffLabelStyle=new GUIStyle(small){fontStyle=FontStyle.Bold};
                    hudBuffLabelStyle.normal.textColor=new Color(.85f,.65f,1f);
                    GUI.Label(new Rect(badgeRect.x+6,badgeRect.y+2,badgeRect.width-12,20),text,hudBuffLabelStyle);
                    curX+=badgeW+6;
                    if(curX+150>Screen.width-150){curX=startX;curY+=27;}
                }
            }

            // 4. Other Active Buffs from p.Buffs
            if(p!=null&&p.Buffs!=null&&p.Buffs.Count>0)
            {
                for(int i=0;i<p.Buffs.Count;i++)
                {
                    var b=p.Buffs[i];
                    if(b.Duration<=0)continue;
                    var def=Catalog.GetBuffDef(b.Kind);
                    string valStr=def.FormatValue(b.Value,b.Stacks);
                    string iconPrefix=b.Kind==BuffKind.Swiftness?"✦ ":b.Kind==BuffKind.AttackPower?"▲ ":b.Kind==BuffKind.IronSkin?"◆ ":b.Kind==BuffKind.Regen?"+ ":b.Kind==BuffKind.StaminaSurge?"★ ":"✦ ";
                    string text=$"{iconPrefix}{def.DisplayName} {valStr} [{Mathf.CeilToInt(b.Duration)}s]";
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
        }

        void DrawFieldHud(Player p)
        {
            if(p.Ghost){DrawSpectateHud();return;}
            DrawMatchClock();
            DrawHeaderAnnouncements(p);
            DrawCornerPauseButton();
            float rightPad=12f;
            float langW=90f;
            float langX=Screen.width-rightPad-langW;
            float btnGap=8f;
            float toolWidth=Mathf.Clamp((Screen.width-240)/3f,76,120);
            float craftX=langX-btnGap-toolWidth;
            if(UiButton(new Rect(craftX,12,toolWidth,28),"Chế tạo [Tab]")){craftOpen=!craftOpen;if(craftOpen){beaconDepositOpen=false;inventoryOpen=false;}}
            float upperWidth=Mathf.Min(180,(Screen.width-40)/2f);
            float upperY=CompactFieldHud?45:12;
            DrawFieldMeter(16,upperY,upperWidth,L("Hoảng loạn","Dread"),p.Dread,100,new Color(.65f,.4f,.85f));
            if(heldSlot==2&&p.Items!=null&&p.Items.Length>0&&p.Items[0]>0)
                DrawFieldMeter(22+upperWidth,upperY,upperWidth,L("Nhiên liệu đuốc","Torch fuel"),State.Fuel,100,Gold);
            DrawActiveBuffs(p,16,upperY+36);

            float invX=craftX-btnGap-toolWidth;
            if(UiButton(new Rect(invX,12,toolWidth,28),L("Kho đồ [I]","Inventory [I]"))){inventoryOpen=!inventoryOpen;craftOpen=false;beaconDepositOpen=false;}
            if(network)DrawPartyBubbles();
            DrawMinimap(p);

            bool anyDowned=false;
            if(State.Players!=null)
            {
                for(int pi=0;pi<State.Players.Length;pi++)
                {
                    if(State.Players[pi]!=null&&State.Players[pi].Ghost){anyDowned=true;break;}
                }
            }
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
                int bagVal=p.Bag!=null&&i<p.Bag.Length?p.Bag[i]:0;
                UiLabel(new Rect(left+25,y+6,step-23,22),bagVal.ToString(),small);
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
            if(hudSlotLabelStyle==null)hudSlotLabelStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,clipping=TextClipping.Overflow};
            var slotLabelStyle=hudSlotLabelStyle;
            for(int i=0;i<count;i++)
            {
                bool skill=i==4;
                int slot=skill?0:quickSlots[i];
                bool enabled=!debugOpen&&(!skill||(!p.Ghost&&!p.Extracted&&p.SkillCD<=0&&HasSkillFocus(p)));
                float tileSize=Mathf.Min(60,cell-4);
                Rect tile=new Rect(start+i*cell,y+36,tileSize,tileSize);
                PixelPanel(tile,!skill&&heldSlot==slot?new Color(.51f,.34f,.72f):enabled?new Color(.22f,.16f,.34f):FieldPaper);
                Rect icon=new Rect(tile.center.x-14,tile.y+4,28,28);
                if(skill)
                {
                    DrawSpritePreview(icon,Frame(p.Vessel,"idle",0));
                }
                else DrawSpritePreview(icon,SlotIconSprite(p,slot));
                string key=skill?"J":(i+1).ToString();
                string value=skill?(p.SkillCD<=0?(HasSkillFocus(p)?L("Sẵn","Ready"):L("Chờ","Aim")):p.SkillCD.ToString("0.0")+"s"):SlotShortValue(p,slot);
                UiLabel(new Rect(tile.x+1,tile.y+34,tile.width-2,22),key+" · "+value,slotLabelStyle);
                string tooltip=skill?L("Kỹ năng","Skill"):InventorySlotName(p,slot);
                if(tile.Contains(Event.current.mousePosition)&&!inventoryOpen&&!craftOpen&&!beaconDepositOpen)
                    GUI.tooltip=tooltip;
                if(tile.Contains(Event.current.mousePosition)&&Event.current.type==EventType.MouseDown)
                {
                    if(Event.current.button==1&&!skill&&quickSlots[i]>0)
                    {
                        quickSlots[i]=0;
                        SaveQuickSlots();
                        Event.current.Use();
                    }
                }
                GUI.enabled=enabled;
                if(GUI.Button(tile,GUIContent.none,GUIStyle.none))
                {
                    if(skill)intent.Skill=true;
                    else ActivateQuickSlot(i);
                }
                GUI.enabled=true;
            }
        }

        public void DrawBeaconDepositModal(Player p)
        {
            if (State == null || p == null || p.Ghost)
            {
                beaconDepositOpen = false;
                return;
            }

            // Close automatically if player walks away from Beacon (> 4.8f)
            if (p.Pos.Length > 4.8f)
            {
                beaconDepositOpen = false;
                return;
            }

            float width = Mathf.Min(480f, Screen.width - 20f);
            float height = Mathf.Min(416f, Screen.height - 30f);
            float modalX = Mathf.Max(10f, (Screen.width - width) * 0.5f);
            float modalY = Mathf.Max(10f, (Screen.height - height) * 0.5f);
            Rect modal = new Rect(modalX, modalY, width, height);
            RegisterHudRect(modal);

            // Dark midnight backdrop panel with gold/cyan double border
            PixelPanel(modal, new Color(0.09f, 0.07f, 0.16f, 0.97f));
            Panel(new Rect(modal.x, modal.y, modal.width, 2f), Gold);
            Panel(new Rect(modal.x, modal.yMax - 2f, modal.width, 2f), Gold);

            // 1. Header: Title & Close [✕] Button
            float headerY = modal.y + 10f;
            if(hudModalTitleStyle == null) hudModalTitleStyle = new GUIStyle(title) { fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            hudModalTitleStyle.normal.textColor = Gold;
            UiLabel(new Rect(modal.x + 16, headerY, modal.width - 60, 30), L("Nạp Năng Lượng Hải Đăng", "Beacon Energy Deposit"), hudModalTitleStyle);

            if (GUI.Button(new Rect(modal.x + modal.width - 36, headerY + 2, 26, 26), "✕", GUI.skin.button))
            {
                beaconDepositOpen = false;
                return;
            }

            // 2. Energy Meter & Safety Status
            float meterY = headerY + 34f;
            int curCharge = Mathf.RoundToInt(State.Charge);
            int targetCharge = Mathf.Max(1, Mathf.RoundToInt(State.ChargeTarget));
            float chargeRatio = Mathf.Clamp01(curCharge / (float)targetCharge);
            int safePercent = Mathf.Clamp(Mathf.RoundToInt(chargeRatio * 100f), 0, 100);
            bool overcharged = curCharge >= targetCharge;

            string statusText = overcharged
                ? (EmberLanguage.English ? $"{curCharge} / {targetCharge} Energy (OVERCHARGED · READY)" : $"{curCharge} / {targetCharge} NL (QUÁ TẢI · SẴN SÀNG)")
                : (EmberLanguage.English ? $"{curCharge} / {targetCharge} Energy ({safePercent}% safety)" : $"{curCharge} / {targetCharge} NL ({safePercent}% an toàn)");

            if(hudModalStatusStyle == null) hudModalStatusStyle = new GUIStyle(small) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            hudModalStatusStyle.normal.textColor = overcharged ? Cyan : Color.Lerp(new Color(0.8f, 0.85f, 1f), Gold, chargeRatio);

            if(hudModalMeterLabelStyle == null) hudModalMeterLabelStyle = new GUIStyle(small) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            hudModalMeterLabelStyle.normal.textColor = new Color(0.85f, 0.88f, 0.95f);

            UiLabel(new Rect(modal.x + 16, meterY, 150, 20), L("Năng lượng trụ:", "Beacon Energy:"), hudModalMeterLabelStyle);
            UiLabel(new Rect(modal.x + 170, meterY, modal.width - 186, 20), statusText, hudModalStatusStyle);

            // Progress Bar
            float barY = meterY + 22f;
            float barW = modal.width - 32f;
            Rect barBg = new Rect(modal.x + 16, barY, barW, 14f);
            Panel(barBg, new Color(0.04f, 0.03f, 0.08f, 0.95f));
            Color barFillCol = overcharged ? Cyan : Color.Lerp(new Color(0.25f, 0.75f, 1f), Gold, chargeRatio);
            Panel(new Rect(barBg.x + 1, barBg.y + 1, (barW - 2) * chargeRatio, barBg.height - 2), barFillCol);

            // Divider line
            Panel(new Rect(modal.x + 14, barY + 20f, modal.width - 28, 1f), new Color(0.3f, 0.25f, 0.45f, 0.6f));

            // 3. Resource Cards List
            float listY = barY + 26f;
            float cardH = 68f;
            float cardGap = 8f;

            // Card 1: Lõi Tinh Thể (Core = 1)
            DrawDepositResourceRow(modal.x + 16, listY, modal.width - 32, cardH,
                resId: 1,
                name: L("Lõi Tinh Thể", "Crystal Core"),
                rate: L("+50 NL / viên", "+50 Energy / core"),
                owned: p.Bag[1],
                btn1Text: L("Nạp 1", "Add 1"), btn1Amt: 1, btn1Enabled: p.Bag[1] >= 1,
                btn2Text: L("Nạp 5", "Add 5"), btn2Amt: 5, btn2Enabled: p.Bag[1] >= 1,
                btn3Text: L("Nạp Hết", "Add All"), btn3Amt: p.Bag[1], btn3Enabled: p.Bag[1] >= 1);

            // Card 2: Bụi Quang (Lumidust = 0)
            DrawDepositResourceRow(modal.x + 16, listY + (cardH + cardGap), modal.width - 32, cardH,
                resId: 0,
                name: L("Bụi Quang", "Lumidust"),
                rate: L("+15 NL / 10 bụi", "+15 Energy / 10 dust"),
                owned: p.Bag[0],
                btn1Text: L("Nạp 10", "Add 10"), btn1Amt: 10, btn1Enabled: p.Bag[0] >= 10,
                btn2Text: null, btn2Amt: 0, btn2Enabled: false,
                btn3Text: L("Nạp Hết", "Add All"), btn3Amt: (p.Bag[0] / 10) * 10, btn3Enabled: p.Bag[0] >= 10);

            // Card 3: Gỗ (Wood = 3)
            DrawDepositResourceRow(modal.x + 16, listY + (cardH + cardGap) * 2, modal.width - 32, cardH,
                resId: 3,
                name: L("Gỗ", "Wood"),
                rate: L("+12 NL / 10 gỗ", "+12 Energy / 10 wood"),
                owned: p.Bag[3],
                btn1Text: L("Nạp 10", "Add 10"), btn1Amt: 10, btn1Enabled: p.Bag[3] >= 10,
                btn2Text: null, btn2Amt: 0, btn2Enabled: false,
                btn3Text: L("Nạp Hết", "Add All"), btn3Amt: (p.Bag[3] / 10) * 10, btn3Enabled: p.Bag[3] >= 10);

            // 4. Quick Deposit All Cores Button
            float bottomY = listY + (cardH + cardGap) * 3 + 2f;
            float quickW = modal.width - 32f;
            bool canQuickDeposit = p.Bag[1] > 0;
            GUI.enabled = canQuickDeposit;
            if(hudModalQuickBtnStyle == null) hudModalQuickBtnStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 13 };
            hudModalQuickBtnStyle.normal.textColor = canQuickDeposit ? Gold : GUI.skin.button.normal.textColor;

            string quickText = p.Bag[1] > 0
                ? L($"⚡ Nạp Tất Cả Lõi Tinh Thể ({p.Bag[1]} viên · +{p.Bag[1] * 50} NL)",
                    $"⚡ Quick Deposit All Cores ({p.Bag[1]} cores · +{p.Bag[1] * 50} Energy)")
                : L("⚡ Nạp Tất Cả Lõi (Hết Lõi)", "⚡ Quick Deposit All Cores (Empty)");

            if (GUI.Button(new Rect(modal.x + 16, bottomY, quickW, 32), quickText, hudModalQuickBtnStyle))
            {
                TriggerDeposit(1, p.Bag[1]);
            }
            GUI.enabled = true;

            // 5. Footnote
            if(hudModalFooterStyle == null) hudModalFooterStyle = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
            hudModalFooterStyle.normal.textColor = new Color(0.7f, 0.7f, 0.8f);
            UiLabel(new Rect(modal.x + 16, bottomY + 34f, quickW, 18),
                L("Phím tắt: Bấm [B] hoặc [Esc] để đóng menu nạp", "Shortcut: Press [B] or [Esc] to close"),
                hudModalFooterStyle);
        }

        void DrawDepositResourceRow(float rx, float ry, float rw, float rh,
            int resId, string name, string rate, int owned,
            string btn1Text, int btn1Amt, bool btn1Enabled,
            string btn2Text, int btn2Amt, bool btn2Enabled,
            string btn3Text, int btn3Amt, bool btn3Enabled)
        {
            Rect cardRect = new Rect(rx, ry, rw, rh);
            PixelPanel(cardRect, new Color(0.14f, 0.11f, 0.24f, 0.92f));

            // Icon Frame
            float iconSize = 34f;
            Rect iconRect = new Rect(cardRect.x + 8, cardRect.y + (rh - iconSize) * 0.5f, iconSize, iconSize);
            PixelPanel(new Rect(iconRect.x - 2, iconRect.y - 2, iconSize + 4, iconSize + 4), new Color(0.08f, 0.06f, 0.14f, 0.95f));
            Sprite sp = MaterialSprite(ResourceIcons[resId]);
            if (sp != null) DrawSpritePreview(iconRect, sp);

            // Name & Owned
            float textX = iconRect.xMax + 8f;
            float textW = 108f;
            if(hudDepositNameStyle == null) hudDepositNameStyle = new GUIStyle(small) { fontStyle = FontStyle.Bold, fontSize = 12 };
            hudDepositNameStyle.normal.textColor = owned > 0 ? Color.white : new Color(0.6f, 0.6f, 0.65f);
            UiLabel(new Rect(textX, cardRect.y + 7, textW, 18), name, hudDepositNameStyle);

            if(hudDepositRateStyle == null) hudDepositRateStyle = new GUIStyle(small) { fontSize = 11 };
            hudDepositRateStyle.normal.textColor = Cyan;
            UiLabel(new Rect(textX, cardRect.y + 25, textW, 16), rate, hudDepositRateStyle);

            if(hudDepositOwnedStyle == null) hudDepositOwnedStyle = new GUIStyle(small) { fontSize = 11 };
            hudDepositOwnedStyle.normal.textColor = owned > 0 ? Gold : new Color(0.55f, 0.55f, 0.60f);
            UiLabel(new Rect(textX, cardRect.y + 43, textW, 16), L($"Đang có: {owned}", $"Owned: {owned}"), hudDepositOwnedStyle);

            // Action Buttons
            float btnH = 28f;
            float btnY = cardRect.y + (rh - btnH) * 0.5f;
            float btnAreaX = textX + textW + 4f;
            float btnAreaW = cardRect.xMax - btnAreaX - 4f;

            int btnCount = (btn1Text != null ? 1 : 0) + (btn2Text != null ? 1 : 0) + (btn3Text != null ? 1 : 0);
            float singleBtnW = (btnAreaW - (btnCount - 1) * 3f) / btnCount;

            if(hudDepositBtnStyle == null)
            {
                hudDepositBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    clipping = TextClipping.Overflow
                };
            }
            var btnStyle = hudDepositBtnStyle;

            float curBtnX = btnAreaX;
            if (btn1Text != null)
            {
                GUI.enabled = btn1Enabled;
                if (GUI.Button(new Rect(curBtnX, btnY, singleBtnW, btnH), btn1Text, btnStyle))
                {
                    TriggerDeposit(resId, btn1Amt);
                }
                curBtnX += singleBtnW + 4f;
            }

            if (btn2Text != null)
            {
                GUI.enabled = btn2Enabled;
                if (GUI.Button(new Rect(curBtnX, btnY, singleBtnW, btnH), btn2Text, btnStyle))
                {
                    TriggerDeposit(resId, Mathf.Min(btn2Amt, owned));
                }
                curBtnX += singleBtnW + 4f;
            }

            if (btn3Text != null)
            {
                GUI.enabled = btn3Enabled;
                if (GUI.Button(new Rect(curBtnX, btnY, singleBtnW, btnH), btn3Text, btnStyle))
                {
                    TriggerDeposit(resId, btn3Amt);
                }
            }
            GUI.enabled = true;
        }

        void TriggerDeposit(int resId, int amount)
        {
            if (amount <= 0 || Local == null) return;
            intent.Deposit = true;
            intent.DepositRes = resId;
            intent.DepositAmt = amount;
            if (audioSource != null && tone != null) audioSource.PlayOneShot(tone, 0.45f);
            string resName = resId == 1 ? L("Lõi Tinh Thể", "Crystal Core") : resId == 0 ? L("Bụi Quang", "Lumidust") : L("Gỗ", "Wood");
            notice = L($"Đã nạp {amount} {resName} vào Hải Đăng!", $"Deposited {amount} {resName} to the Beacon!");
        }
    }
}
