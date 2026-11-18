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

        void DrawOverheadStatus(Player player)
        {
            if(!renderers.TryGetValue("player"+player.Id,out var body)||!body.enabled||body.sprite==null)return;
            float top=body.bounds.max.y;
            if(renderers.TryGetValue("hat"+player.Id,out var hat)&&hat.enabled&&hat.sprite!=null)
                top=Mathf.Max(top,hat.bounds.max.y);
            Vector2 anchor=ScreenPos(new V2(player.Pos.X,top));
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
        }

        Texture2D partyBubble;
        GUIStyle partyName;

        bool CompactFieldHud=>Screen.width<760;
        float PartyPortraitY=>CompactFieldHud?163:126;
        float ObjectiveCardY=>network?PartyPortraitY+76:CompactFieldHud?138:99;

        void DrawPartyBubbles()
        {
            if(Event.current.type!=EventType.Repaint)return;
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
            var members=State.Players;
            int count=Mathf.Min(members.Length,4);
            float step=Mathf.Min(102,(Screen.width-28f)/Mathf.Max(1,count));
            int i=0;
            for(int slot=1;slot<=4;slot++)
            {
                Player teammate=null;
                foreach(var member in members)if(member.PartySlot==slot){teammate=member;break;}
                if(teammate==null)continue;
                float x=14+step*(i++ +.5f), y=PartyPortraitY;
                var center=new Vector2(x,y);
                GUI.DrawTexture(new Rect(x-30,y-30,60,60),partyBubble);
                DrawSpritePreview(new Rect(x-23,y-28,46,46),teammate.Ghost?Primitive("pet_ghost"):Frame(teammate.Vessel,"idle",0));
                DrawPartyArc(center,26,teammate.HP/Mathf.Max(1,teammate.MaxHP),Cyan);
                DrawPartyArc(center,31,teammate.Dread/100,Coral);
                string name="P"+teammate.PartySlot+" · "+teammate.Name;
                string stats=name+"\nHP "+Mathf.CeilToInt(teammate.HP)+"/"+Mathf.CeilToInt(teammate.MaxHP)+
                    " ( "+L("vòng xanh","cyan arc")+" )\n"+L("Hoảng Loạn ","Dread ")+Mathf.CeilToInt(teammate.Dread)+
                    "% ( "+L("vòng hồng","pink arc")+" )";
                GUI.Label(new Rect(x-30,y-30,60,60),new GUIContent("",stats),GUIStyle.none);
                GUI.Label(new Rect(x-step/2,y+34,step,20),new GUIContent(name,stats),partyName);
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
                heading=L("Hồn ma","Wisp");
                hint=L("Đồng đội dùng Đá Hồi Sinh tại hải đăng","An ally can use a Revival Stone at the beacon");
                progress=0;
                icon="revival";
            }
            else if(State.Phase==Phase.Eclipse)
            {
                heading=L("Nhật thực","Eclipse")+" · "+Mathf.CeilToInt(State.Wave)+"s";
                hint=L("Bảo vệ hải đăng","Defend the beacon");
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
                heading=L("Thắp sáng hải đăng","Light the beacon")+" · "+Mathf.RoundToInt(State.Charge)+"%";
                hint=L("Mang lõi về trụ · B để nạp","Bring cores to the beacon · B to deposit");
                progress=State.Charge/100;
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

        void DrawFieldHud(Player p)
        {
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
            UiLabel(new Rect(x+9+5*step,y+6,step,22),p.Load+"/"+p.Capacity,small);

            var itemChip=new Rect(x,y-36,width,32);
            PixelPanel(itemChip,FieldPaper);
            string itemName=(EmberLanguage.English?ItemNamesEn:ItemNamesVi)[selectedItem];
            string itemStatus=p.Items[selectedItem]==0?L("Hết đồ","Empty"):
                !CanUseSelectedItem(p)&&selectedItem==3?L("Cần hải đăng và đồng đội gục","Requires beacon and a downed teammate"):
                L("Đã chọn","Selected");
            UiLabel(new Rect(x+8,itemChip.y+2,width-110,22),heldSlot==1?L("Đang cầm vũ khí","Weapon equipped"):heldSlot==6?L("Tay không","Empty hands"):itemName+" · "+itemStatus,small);
            if(heldSlot>=2&&heldSlot<=5)
            {
                GUI.enabled=CanUseSelectedItem(p)&&!debugOpen;
                if(UiButton(new Rect(itemChip.xMax-96,itemChip.y+2,92,28),L("Dùng [U]","Use [U]")))intent.Use=selectedItem;
                GUI.enabled=true;
            }
            if(p.Vessel!=Vessel.Wanderer)
            {
                var rangeChip=new Rect(x,y-63,width,25);
                PixelPanel(rangeChip,FieldPaper);
                UiLabel(new Rect(rangeChip.x+9,rangeChip.y+2,width-18,22),SkillRangeText(p.Vessel),small);
            }
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
                bool enabled=!debugOpen&&(!skill||(!p.Ghost&&!p.Extracted&&p.SkillCD<=0&&HasSkillFocus(p)));
                Rect tile=new Rect(start+i*cell,y+38,Mathf.Min(58,cell-4),Mathf.Min(58,cell-4));
                PixelPanel(tile,!skill&&heldSlot==i+2?new Color(.51f,.34f,.72f):enabled?new Color(.22f,.16f,.34f):FieldPaper);
                Rect icon=new Rect(tile.center.x-14,tile.y+5,28,28);
                if(!skill&&i==0)DrawSpritePreview(icon,Primitive("torch"));
                else if(skill&&p.Vessel!=Vessel.LittleMoone)DrawSpritePreview(icon,Frame(p.Vessel,"cast",0));
                else PixelArt(icon,skill?"meteor":ActionIcons[i]);
                string key=skill?"J":(i+1).ToString();
                string value=skill?(p.SkillCD<=0?(HasSkillFocus(p)?L("Sẵn","Ready"):L("Chờ","Aim")):p.SkillCD.ToString("0.0")+"s"):p.Items[i].ToString();
                UiLabel(new Rect(tile.x+5,tile.y+36,tile.width-8,20),key+"  ·  "+value,small);
                string tooltip=skill?L("Kỹ năng","Skill"):(EmberLanguage.English?ItemNamesEn:ItemNamesVi)[i];
                GUI.enabled=enabled;
                if(GUI.Button(tile,new GUIContent("",tooltip),GUIStyle.none))
                {
                    if(skill)intent.Skill=true;
                    else EquipSlot(i+2);
                }
                GUI.enabled=true;
            }
        }
    }
}
