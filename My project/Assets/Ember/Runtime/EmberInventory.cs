using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        bool inventoryOpen;
        int heldSlot=1;
        int pendingQuickAssign;
        readonly int[] quickSlots={1,7,8,2};
        static readonly string[] InventoryWeaponArt={"stick","crystal_axe","shadow_blades","crossbow","lumen_gun","mirror_shield","pickaxe"};
        static bool IsHoldingWeaponSlot(int slot)=>slot==1||slot==7||slot==8;
        static Weapon WeaponForSlot(Player p,int slot)=>slot==7?p.SideWeapon:slot==8?Weapon.Pickaxe:p.Weapon;
        static float WeaponRangeFor(Weapon w)=>(w==Weapon.Crossbow||w==Weapon.LumenGun||w==Weapon.MirrorShield)?7:w==Weapon.CrystalAxe?2.6f:w==Weapon.Pickaxe?2.4f:2;
        readonly Dictionary<int,(Vector2 dir,float weight)> weaponAimState=new Dictionary<int,(Vector2,float)>();
        readonly Dictionary<int,(float join,float holdUntil,V2 offset)> offHandJoinState=new Dictionary<int,(float,float,V2)>();

        void LoadQuickSlots()
        {
            string raw=PlayerPrefs.GetString("Ember.QuickSlots","");
            if(string.IsNullOrEmpty(raw))return;
            string[] parts=raw.Split(',');
            if(parts.Length!=4)return;
            for(int i=0;i<4;i++)
                if(int.TryParse(parts[i],out int slot)&&slot>=1&&slot<=8)quickSlots[i]=slot;
        }

        void SaveQuickSlots()
        {
            PlayerPrefs.SetString("Ember.QuickSlots",string.Join(",",quickSlots));
        }

        void BindQuickSlot(int quickIndex,int slot)
        {
            if(quickIndex<0||quickIndex>=quickSlots.Length||slot<1||slot>8)return;
            int existing=System.Array.IndexOf(quickSlots,slot);
            if(existing>=0&&existing!=quickIndex)quickSlots[existing]=quickSlots[quickIndex];
            quickSlots[quickIndex]=slot;
            pendingQuickAssign=0;
            SaveQuickSlots();
        }

        Sprite SlotIconSprite(Player p,int slot)
        {
            if(IsHoldingWeaponSlot(slot))return Primitive(InventoryWeaponArt[(int)WeaponForSlot(p,slot)]);
            if(slot>=2&&slot<=5)return HeldItemSprite(slot-2);
            return null;
        }

        string SlotShortValue(Player p,int slot)
        {
            if(IsHoldingWeaponSlot(slot))
            {
                Weapon w=WeaponForSlot(p,slot);
                int mag=Catalog.WeaponMagSize(w);
                if(mag>0)
                {
                    if(p.Reloading&&p.ActiveWeapon==w)return L("Nạp","Rld");
                    int cur=p.Ammo<0||p.AmmoWeapon!=w?mag:Mathf.Clamp(p.Ammo,0,mag);
                    return cur+"/"+mag;
                }
                return slot==8?L("Đào","Dig"):"∞";
            }
            if(slot>=2&&slot<=5)return p.Items[slot-2].ToString();
            return "—";
        }

        bool ShouldAutoEquipPickaxe(Player p)
        {
            if(p==null||(!p.HasPickaxe&&p.Weapon!=Weapon.Pickaxe))return false;
            if(State.Phase==Phase.Extraction&&p.Pos.Length<3)return false;
            if(State.Structures.Any(s=>s.Kind==4&&V2.Distance(s.Pos,p.Pos)<2))return false;
            var node=State.Nodes.Where(n=>V2.Distance(n.Pos,p.Pos)<2.4f)
                .OrderBy(n=>V2.Distance(n.Pos,p.Pos)).FirstOrDefault();
            return node!=null&&!node.Dropped;
        }

        void EquipSlot(int slot)
        {
            if(heldSlot==slot)return;
            heldSlot=slot;
            if(slot>=2&&slot<=5)selectedItem=slot-2;
            intent.Equip=slot;
        }

        void DrawHands(Player player,SpriteRenderer body)
        {
            int slot=player.Id==localId?heldSlot:player.HeldSlot;
            bool harvesting=player.HarvestCD>0&&slot==8;
            bool weapon=IsHoldingWeaponSlot(slot);
            bool consumable=!weapon&&slot>=2&&slot<=5&&player.Items[slot-2]>0;
            float sign=player.Right?1:-1;
            int handOrder=body.sortingOrder+4;
            int heldOrder=handOrder-1;
            var pivots=HandPivotFor(player.Vessel);
            V2 holdingHand=player.Pos+new V2(sign*pivots.FrontX/48f,pivots.FrontY/48f);
            V2 offHand=player.Pos+new V2(-sign*pivots.BackX/48f,pivots.BackY/48f);

            Weapon activeWeapon=harvesting?Weapon.Pickaxe:WeaponForSlot(player,slot);
            float aimWeight=0;
            Vector2 aimDir=new Vector2(sign,0);
            float swingAngle=0;
            V2 weaponHandOffset=new V2();
            bool flipWeapon=!player.Right;
            float desiredOffJoin=0;
            V2 offJoinOffset=new V2(-sign*.07f,-.05f);
            bool rangedWeapon=activeWeapon==Weapon.Crossbow||activeWeapon==Weapon.LumenGun||activeWeapon==Weapon.MirrorShield;

            if(weapon)
            {
                if(harvesting)
                {
                    float maxCd=Mathf.Max(.08f,player.Stats.HarvestCooldown);
                    float t=1f-Mathf.Clamp01(player.HarvestCD/maxCd);
                    float chop=Mathf.Sin(t*Mathf.PI);
                    weaponHandOffset=new V2(sign*(.08f+.14f*chop),.14f-.22f*chop);
                    swingAngle=-sign*(-32f+108f*chop);
                    desiredOffJoin=1f;
                    offJoinOffset=new V2(-sign*.08f,-.07f);
                }
                else
                {
                    float range=WeaponRangeFor(activeWeapon);
                    var target=State.Enemies.Where(e=>e.HP>0&&V2.Distance(e.Pos,player.Pos)<range)
                        .OrderBy(e=>V2.Distance(e.Pos,player.Pos)).FirstOrDefault();
                    V2? aim=target?.Pos;
                    if(player.Mad&&!player.Safe)
                    {
                        var ally=State.Players.Where(q=>q.Id!=player.Id&&!q.Safe&&!q.Ghost&&!q.Extracted&&V2.Distance(q.Pos,player.Pos)<range)
                            .OrderBy(q=>V2.Distance(q.Pos,player.Pos)).FirstOrDefault();
                        if(ally!=null&&(target==null||V2.Distance(ally.Pos,player.Pos)<V2.Distance(target.Pos,player.Pos)))aim=ally.Pos;
                    }
                    if(slot==8&&!aim.HasValue)
                    {
                        var node=State.Nodes.Where(n=>V2.Distance(n.Pos,player.Pos)<range)
                            .OrderBy(n=>V2.Distance(n.Pos,player.Pos)).FirstOrDefault();
                        if(node!=null)aim=node.Pos;
                    }

                    Vector2 desiredDir=aim.HasValue
                        ?new Vector2(aim.Value.X-player.Pos.X,aim.Value.Y-player.Pos.Y).normalized
                        :new Vector2(sign,0);
                    if(desiredDir.sqrMagnitude<.001f)desiredDir=new Vector2(sign,0);
                    float desiredWeight=aim.HasValue?1f:0f;

                    if(!weaponAimState.TryGetValue(player.Id,out var prev))prev=(desiredDir,desiredWeight);
                    float blend=Mathf.Clamp01(Time.deltaTime*14f);
                    aimDir=Vector2.Lerp(prev.dir,desiredDir,blend);
                    if(aimDir.sqrMagnitude<.001f)aimDir=desiredDir; else aimDir.Normalize();
                    aimWeight=Mathf.Lerp(prev.weight,desiredWeight,blend);
                    weaponAimState[player.Id]=(aimDir,aimWeight);

                    if(aimWeight>.01f)
                    {
                        weaponHandOffset=new V2(aimDir.x*.22f*aimWeight,aimDir.y*.18f*aimWeight);
                        if(Mathf.Abs(aimDir.x)>.08f)flipWeapon=aimDir.x<0;
                    }

                    if(player.Reloading&&rangedWeapon)
                    {
                        desiredOffJoin=1f;
                        float pump=Mathf.Sin(clock*15f)*.06f;
                        offJoinOffset=new V2(-sign*.05f,-.04f+pump);
                    }
                    else if(player.AttackCD>0)
                    {
                        float cdMax=activeWeapon==Weapon.ShadowBlades?.2f:activeWeapon==Weapon.LumenGun?.32f:.5f;
                        float kick=Mathf.Clamp01(player.AttackCD/cdMax);
                        desiredOffJoin=1f;
                        if(rangedWeapon)
                        {
                            weaponHandOffset+=new V2(-aimDir.x*.09f*kick,-aimDir.y*.09f*kick);
                            offJoinOffset=new V2(aimDir.x*.10f,aimDir.y*.10f-.03f);
                        }
                        else
                        {
                            float arc=Mathf.Sin(kick*Mathf.PI);
                            swingAngle=-sign*55f*arc;
                            weaponHandOffset+=new V2(sign*.10f*arc,.06f*arc);
                            offJoinOffset=new V2(-sign*.08f,-.06f);
                        }
                    }
                    else if(aimWeight>.05f)
                    {
                        desiredOffJoin=rangedWeapon?aimWeight*.92f:aimWeight*.65f;
                        offJoinOffset=rangedWeapon
                            ?new V2(aimDir.x*.10f,aimDir.y*.10f-.02f)
                            :new V2(-sign*.09f,-.05f);
                    }
                }
            }

            if(!offHandJoinState.TryGetValue(player.Id,out var prevOff))prevOff=(0,0,offJoinOffset);
            bool activeAction=harvesting||player.AttackCD>0||(player.Reloading&&rangedWeapon);
            float holdUntil=activeAction?clock+.18f:prevOff.holdUntil;
            if(!activeAction&&desiredOffJoin<=0&&clock<holdUntil)
            {
                desiredOffJoin=.92f;
                offJoinOffset=prevOff.offset;
            }
            float joinSpeed=desiredOffJoin>prevOff.join?(activeAction?45f:24f):22f;
            float offJoin=activeAction&&prevOff.join<.65f?Mathf.Max(.72f,Mathf.Lerp(prevOff.join,desiredOffJoin,Mathf.Clamp01(Time.deltaTime*joinSpeed))):Mathf.Lerp(prevOff.join,desiredOffJoin,Mathf.Clamp01(Time.deltaTime*joinSpeed));
            V2 smoothOffset=new V2(
                Mathf.Lerp(prevOff.offset.X,offJoinOffset.X,Mathf.Clamp01(Time.deltaTime*28f)),
                Mathf.Lerp(prevOff.offset.Y,offJoinOffset.Y,Mathf.Clamp01(Time.deltaTime*28f)));
            offHandJoinState[player.Id]=(offJoin,holdUntil,smoothOffset);

            V2 activeHandPos=holdingHand+weaponHandOffset;
            V2 joinedOffHand=activeHandPos+smoothOffset;
            V2 finalOffHand=new V2(
                Mathf.Lerp(offHand.X,joinedOffHand.X,offJoin),
                Mathf.Lerp(offHand.Y,joinedOffHand.Y,offJoin));

            V2 rightHand=player.Right?activeHandPos:finalOffHand;
            V2 leftHand=player.Right?finalOffHand:activeHandPos;
            Sprite hand=Glove(false);
            bool flipHands=!player.Right;
            Put("handRight"+player.Id,hand,rightHand,Color.white,.5f,handOrder,flipHands);
            Put("handLeft"+player.Id,hand,leftHand,Color.white,.5f,handOrder,flipHands);

            if(weapon)
            {
                bool horizontalBarrel=activeWeapon==Weapon.LumenGun;
                V2 itemPos=activeHandPos+(aimWeight>.05f&&!harvesting
                    ?new V2(aimDir.x*.12f,aimDir.y*.12f+.05f)
                    :new V2(sign*.075f,.12f));
                var held=Put("weapon"+player.Id,Primitive(InventoryWeaponArt[(int)activeWeapon]),
                    itemPos,Color.white,.85f,heldOrder,flipWeapon);
                float angle;
                if(harvesting)
                {
                    angle=swingAngle;
                }
                else if(player.Reloading&&activeWeapon==Weapon.LumenGun||player.Reloading&&activeWeapon==Weapon.Crossbow||player.Reloading&&activeWeapon==Weapon.MirrorShield)
                {
                    angle=-sign*44f;
                }
                else if(aimWeight>.05f)
                {
                    float aimDeg=Mathf.Atan2(aimDir.y,aimDir.x)*Mathf.Rad2Deg;
                    float targetAngle=horizontalBarrel?(flipWeapon?aimDeg+180f:aimDeg):(aimDeg-90f);
                    float restAngle=horizontalBarrel?0f:-sign*30f;
                    angle=Mathf.LerpAngle(restAngle,targetAngle,aimWeight)+swingAngle;
                }
                else
                {
                    angle=(horizontalBarrel?0f:-sign*30f)+swingAngle;
                }
                held.transform.rotation=Quaternion.Euler(0,0,angle);
                held.flipY=false;
            }
            else if(consumable)
            {
                var held=Put("heldItem"+player.Id,HeldItemSprite(slot-2),holdingHand+new V2(sign*.055f,.105f),
                    Color.white,.57f,heldOrder,!player.Right);
                held.transform.rotation=Quaternion.identity;
                held.flipY=false;
            }
        }

        Sprite HeldItemSprite(int index)
        {
            if(index==0)return Primitive("torch");
            return MaterialSprite(ActionIcons[index]);
        }

        Sprite MaterialSprite(string icon)
        {
            string key="material_"+icon;
            if(primitives.TryGetValue(key,out var sprite))return sprite;
            var texture=Resources.Load<Texture2D>("Ember/Interface/"+icon);
            if(texture==null)return Primitive("diamond");
            texture.filterMode=FilterMode.Point;
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),24);
            primitives[key]=sprite;
            return sprite;
        }

        static readonly string[] WeaponNamesVi={"Gậy Gỗ","Rìu Quang Tinh","Song Đao Bóng Đêm","Nỏ Tinh Thể","Súng Quang Năng","Khiên Gương","Cuốc Sắt"};
        static readonly string[] WeaponNamesEn={"Wooden Stick","Light Axe","Shadow Blades","Crystal Crossbow","Lumen Gun","Mirror Shield","Iron Pickaxe"};

        string InventorySlotName(Player p,int slot)
        {
            if(slot==1)return (EmberLanguage.English?WeaponNamesEn:WeaponNamesVi)[(int)p.Weapon];
            if(slot==7)return (EmberLanguage.English?WeaponNamesEn:WeaponNamesVi)[(int)p.SideWeapon];
            if(slot==8)return (EmberLanguage.English?WeaponNamesEn:WeaponNamesVi)[(int)Weapon.Pickaxe];
            if(slot==6)return L("Tay không","Empty hands");
            return (EmberLanguage.English?ItemNamesEn:ItemNamesVi)[slot-2];
        }

        static readonly int[] InventoryGridSlots={1,7,8,2,3,4,5,6};

        void DrawInventory(Player p)
        {
            float width=Mathf.Min(456,Screen.width-28);
            float size=Mathf.Min(78,(width-46)/4);
            float boxHeight=210+2*size;
            var box=new Rect((Screen.width-width)/2,Mathf.Max(94,(Screen.height-boxHeight)/2),width,boxHeight);
            PixelPanel(box,FieldPaper);
            UiLabel(new Rect(box.x+14,box.y+4,width-112,40),L("Kho đồ","Inventory"),title);
            if(UiButton(new Rect(box.xMax-92,box.y+9,76,34),L("Đóng","Close"))){inventoryOpen=false;pendingQuickAssign=0;}
            float gridWidth=4*size+18;
            var ev=Event.current;
            for(int i=0;i<InventoryGridSlots.Length;i++)
            {
                int slot=InventoryGridSlots[i];
                var tile=new Rect(box.center.x-gridWidth/2+(i%4)*(size+6),box.y+50+(i/4)*(size+6),size,size);
                bool active=heldSlot==slot;
                bool assigning=pendingQuickAssign==slot;
                Color bg=assigning?new Color(.68f,.44f,.18f):active?Plum:new Color(.22f,.16f,.34f);
                PixelPanel(tile,bg);
                DrawSpritePreview(new Rect(tile.center.x-21,tile.y+7,42,42),SlotIconSprite(p,slot));
                int qIdx=System.Array.IndexOf(quickSlots,slot);
                if(qIdx>=0)
                {
                    var badge=new Rect(tile.xMax-20,tile.y+3,17,17);
                    Panel(badge,new Color(.10f,.08f,.18f,.90f));
                    UiLabel(new Rect(badge.x+4,badge.y-1,14,18),(qIdx+1).ToString(),small);
                }
                string name=InventorySlotName(p,slot);
                string line=slot==1?L("Cận chiến","Melee"):
                    slot==7?L("Súng","Gun"):
                    slot==8?L("Cuốc","Pickaxe"):
                    slot>=2&&slot<=5?name.Split(' ')[0]+" · "+p.Items[slot-2]:name;
                UiLabel(new Rect(tile.x+4,tile.yMax-22,size-7,21),line,small);
                if(ev.type==EventType.MouseDown&&ev.button==1&&tile.Contains(ev.mousePosition))
                {
                    pendingQuickAssign=pendingQuickAssign==slot?0:slot;
                    ev.Use();
                }
                else if(GUI.Button(tile,new GUIContent("",name),GUIStyle.none))
                {
                    pendingQuickAssign=0;
                    EquipSlot(slot);
                }
            }

            float detailY=box.y+62+2*size;
            var detail=new Rect(box.x+14,detailY,width-28,36);
            PixelPanel(detail,new Color(.22f,.16f,.34f));
            string equipped=InventorySlotName(p,heldSlot);
            if(IsHoldingWeaponSlot(heldSlot))
            {
                Weapon activeW=WeaponForSlot(p,heldSlot);
                bool ranged=activeW==Weapon.Crossbow||activeW==Weapon.LumenGun||activeW==Weapon.MirrorShield;
                float range=WeaponRangeFor(activeW);
                string kind=heldSlot==8?L("Đào/Chặt","Mine/Chop"):ranged?L("Tầm xa","Ranged"):L("Cận chiến","Melee");
                equipped+=" · "+kind+" ("+range.ToString("0.#")+L(" ô)"," tiles)");
                int mag=Catalog.WeaponMagSize(activeW);
                if(mag>0)equipped+=" · "+L("Băng ","Mag ")+mag+L(" · Nạp "," · Reload ")+Catalog.WeaponReloadTime(activeW).ToString("0.#")+"s";
            }
            else if(heldSlot>=2&&heldSlot<=5&&p.Items[heldSlot-2]==0)equipped+=L(" · Hết đồ"," · Empty");
            UiLabel(new Rect(detail.x+9,detail.y+5,detail.width-18,26),L("Đang cầm: ","Holding: ")+equipped,small);

            float quickLabelY=detailY+42;
            string quickHint=pendingQuickAssign>0
                ?L("Bấm ô 1–4 (hoặc phím 1–4) để gán: ","Click slot 1–4 (or press 1–4) to bind: ")+InventorySlotName(p,pendingQuickAssign)
                :L("Ô phím tắt nhanh 1–4 · Chuột phải vào đồ để gán","Quick slots 1–4 · Right-click any item to assign");
            var oldContent=GUI.contentColor;
            if(pendingQuickAssign>0)GUI.contentColor=Gold;
            UiLabel(new Rect(box.x+14,quickLabelY,width-28,22),quickHint,small);
            GUI.contentColor=oldContent;

            float qY=quickLabelY+24;
            float qSize=Mathf.Min(64,(width-46)/4);
            float qGridW=4*qSize+18;
            for(int q=0;q<4;q++)
            {
                int boundSlot=quickSlots[q];
                var qTile=new Rect(box.center.x-qGridW/2+q*(qSize+6),qY,qSize,qSize);
                Color qBg=pendingQuickAssign>0?new Color(.42f,.28f,.60f):heldSlot==boundSlot?Plum:new Color(.18f,.13f,.28f);
                PixelPanel(qTile,qBg);
                DrawSpritePreview(new Rect(qTile.center.x-16,qTile.y+5,32,32),SlotIconSprite(p,boundSlot));
                UiLabel(new Rect(qTile.x+4,qTile.yMax-21,qSize-8,20),(q+1)+" · "+SlotShortValue(p,boundSlot),small);
                if(GUI.Button(qTile,new GUIContent("",InventorySlotName(p,boundSlot)),GUIStyle.none))
                {
                    if(pendingQuickAssign>0)BindQuickSlot(q,pendingQuickAssign);
                    else EquipSlot(boundSlot);
                }
            }
        }
    }
}
