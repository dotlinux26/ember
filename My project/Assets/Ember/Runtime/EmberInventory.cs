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
            if(slot>=2&&slot<=5)return p.Items[slot-2]>0?HeldItemSprite(slot-2):null;
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
                return slot==8?(p.PickaxeDurability+"/"+p.PickaxeMaxDurability):"∞";
            }
            if(slot>=2&&slot<=5)return p.Items[slot-2]>0?p.Items[slot-2].ToString():"—";
            return "—";
        }

        bool HasHarvestTool(Player p)
        {
            if(p==null)return false;
            return p.HasPickaxe||p.Weapon==Weapon.Pickaxe||p.Weapon==Weapon.CrystalAxe||p.SideWeapon==Weapon.Pickaxe||p.SideWeapon==Weapon.CrystalAxe;
        }

        bool ShouldAutoEquipPickaxe(Player p)
        {
            if(p==null||!HasHarvestTool(p))return false;
            if(State==null||(State.Phase==Phase.Extraction&&p.Pos.Length<3))return false;
            if(State.Structures!=null&&State.Structures.Any(s=>s.Kind==4&&V2.Distance(s.Pos,p.Pos)<2))return false;
            var node=State.Nodes?.Where(n=>V2.Distance(n.Pos,p.Pos)<2.4f)
                .OrderBy(n=>V2.Distance(n.Pos,p.Pos)).FirstOrDefault();
            if(node!=null&&!node.Dropped)return true;
            if(currentTiles!=null)
            {
                int size=State.Size;
                int px=Mathf.FloorToInt(p.Pos.X+size/2f),py=Mathf.FloorToInt(p.Pos.Y+size/2f);
                for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                {
                    int tx=px+dx,ty=py+dy;
                    if(tx<0||ty<0||tx>=size||ty>=size)continue;
                    byte t=currentTiles[ty*size+tx];
                    if(t==3||t==2)
                    {
                        V2 blockPos=new V2(tx-size/2f+.5f,ty-size/2f+.5f);
                        if(V2.Distance(p.Pos,blockPos)<2.6f)return true;
                    }
                }
            }
            return false;
        }

        void AutoEquipHarvestTool(Player p)
        {
            if(p==null)return;
            if(p.HasPickaxe)
            {
                if(heldSlot!=8)EquipSlot(8,toggle:false);
            }
            else if(p.Weapon==Weapon.Pickaxe||p.Weapon==Weapon.CrystalAxe)
            {
                if(heldSlot!=1)EquipSlot(1,toggle:false);
            }
            else if(p.SideWeapon==Weapon.Pickaxe||p.SideWeapon==Weapon.CrystalAxe)
            {
                if(heldSlot!=7)EquipSlot(7,toggle:false);
            }
        }

        void EquipSlot(int slot,bool toggle=true)
        {
            if(toggle&&heldSlot==slot)slot=6; // Toggle: pressing/clicking active held slot unequips to empty hands
            heldSlot=slot;
            if(slot>=2&&slot<=5)selectedItem=slot-2;
            intent.Equip=slot;
        }

        void DrawHands(Player player,SpriteRenderer body,V2 visualPos)
        {
            int slot=player.Id==localId?heldSlot:player.HeldSlot;
            bool harvesting=player.HarvestCD>0&&slot==8;
            bool weapon=IsHoldingWeaponSlot(slot);
            bool consumable=!weapon&&slot>=2&&slot<=5&&player.Items[slot-2]>0;
            float sign=player.Right?1:-1;
            int handOrder=body.sortingOrder+4;
            int heldOrder=handOrder-1;
            var pivots=HandPivotFor(player.Vessel);
            V2 holdingHand=visualPos+new V2(sign*pivots.FrontX/48f,pivots.FrontY/48f);
            V2 offHand=visualPos+new V2(-sign*pivots.BackX/48f,pivots.BackY/48f);

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
            if(slot==8)return p.PickaxeName;
            if(slot==6)return L("Tay không","Empty hands");
            return (EmberLanguage.English?ItemNamesEn:ItemNamesVi)[slot-2];
        }

        const int BackpackCapacity=30;
        struct BackpackCell
        {
            public int Kind; // 0 empty, 1 equipSlot (1/7/8), 2 consumable (0..3), 3 resource (0..4)
            public int Id;
            public int Count;
            public bool Empty=>Kind==0||Count<=0;
        }
        readonly BackpackCell[] backpackCells=new BackpackCell[BackpackCapacity];
        Vector2 inventoryScroll;
        int dragFromCell=-1,inspectedCell=-1;
        Vector2 dragStartMouse;
        bool draggingItem;
        bool contextMenuOpen;
        int contextMenuCell=-1;
        Vector2 contextMenuPos;

        void DropBackpackItem(Player p,int cellIdx,int amount)
        {
            if(cellIdx<0||cellIdx>=BackpackCapacity||backpackCells[cellIdx].Empty||amount<=0)return;
            var cell=backpackCells[cellIdx];
            if(host&&World!=null)
            {
                World.DropItem(p,cell.Kind,cell.Id,amount);
            }
            else
            {
                if(cell.Kind==3)
                {
                    int r=Mathf.Clamp(cell.Id,0,p.Bag.Length-1);
                    int actual=Mathf.Min(amount,p.Bag[r]);
                    p.Bag[r]-=actual;
                }
                else if(cell.Kind==2)
                {
                    int c=Mathf.Clamp(cell.Id,0,3);
                    int actual=Mathf.Min(amount,p.Items[c]);
                    p.Items[c]-=actual;
                }
                else if(cell.Kind==1&&cell.Id==8)
                {
                    p.HasPickaxe=false;
                    if(heldSlot==8)heldSlot=6;
                }
            }
            SyncBackpack(p);
        }

        static int StackLimitFor(int kind,int id)
        {
            if(kind==1)return 1;
            if(kind==2)return 5;
            if(kind==3)return (id==(int)Resource.Core||id==(int)Resource.Umbra)?10:20;
            return 1;
        }

        void SyncBackpack(Player p)
        {
            // Required counts per (kind, id).
            var needed=new Dictionary<int,int>();
            needed[100+1]=1; // primary weapon
            needed[100+7]=1; // side gun
            if(p.HasPickaxe||p.Weapon==Weapon.Pickaxe)needed[100+8]=1;
            for(int c=0;c<4;c++)if(p.Items[c]>0)needed[200+c]=p.Items[c];
            for(int r=0;r<(int)Resource.Count;r++)if(p.Bag[r]>0)needed[300+r]=p.Bag[r];

            // 1. Clamp or remove cells that no longer exist or exceed live totals.
            var remaining=new Dictionary<int,int>(needed);
            for(int i=0;i<BackpackCapacity;i++)
            {
                var cell=backpackCells[i];
                if(cell.Empty){backpackCells[i]=default;continue;}
                int key=cell.Kind*100+cell.Id;
                if(!remaining.TryGetValue(key,out int rem)||rem<=0)
                {
                    backpackCells[i]=default;
                    continue;
                }
                int limit=StackLimitFor(cell.Kind,cell.Id);
                int keep=Mathf.Min(cell.Count,Mathf.Min(limit,rem));
                backpackCells[i].Count=keep;
                remaining[key]=rem-keep;
            }

            // 2. Top up existing partial stacks of the same item.
            for(int i=0;i<BackpackCapacity;i++)
            {
                var cell=backpackCells[i];
                if(cell.Empty)continue;
                int key=cell.Kind*100+cell.Id;
                if(!remaining.TryGetValue(key,out int rem)||rem<=0)continue;
                int limit=StackLimitFor(cell.Kind,cell.Id);
                int add=Mathf.Min(limit-cell.Count,rem);
                if(add>0){backpackCells[i].Count+=add;remaining[key]=rem-add;}
            }

            // 3. Place any brand-new items or overflow stacks into first free cells.
            foreach(var kv in remaining.ToArray())
            {
                int rem=kv.Value;
                if(rem<=0)continue;
                int kind=kv.Key/100,id=kv.Key%100;
                int limit=StackLimitFor(kind,id);
                for(int i=0;i<BackpackCapacity&&rem>0;i++)
                {
                    if(!backpackCells[i].Empty)continue;
                    int put=Mathf.Min(limit,rem);
                    backpackCells[i]=new BackpackCell{Kind=kind,Id=id,Count=put};
                    rem-=put;
                }
            }
        }

        void SortBackpack(Player p)
        {
            for(int i=0;i<BackpackCapacity;i++)backpackCells[i]=default;
            int cursor=0;
            void AddSorted(int kind,int id,int total)
            {
                int limit=StackLimitFor(kind,id);
                while(total>0&&cursor<BackpackCapacity)
                {
                    int put=Mathf.Min(limit,total);
                    backpackCells[cursor++]=new BackpackCell{Kind=kind,Id=id,Count=put};
                    total-=put;
                }
            }
            AddSorted(1,1,1);
            AddSorted(1,7,1);
            if(p.HasPickaxe||p.Weapon==Weapon.Pickaxe)AddSorted(1,8,1);
            for(int c=0;c<4;c++)if(p.Items[c]>0)AddSorted(2,c,p.Items[c]);
            for(int r=0;r<(int)Resource.Count;r++)if(p.Bag[r]>0)AddSorted(3,r,p.Bag[r]);
            inspectedCell=-1;
        }

        int UsedBackpackSlots()
        {
            int used=0;
            for(int i=0;i<BackpackCapacity;i++)if(!backpackCells[i].Empty)used++;
            return used;
        }

        int EquipSlotForCell(BackpackCell cell)=>cell.Kind==1?cell.Id:cell.Kind==2?cell.Id+2:0;

        Sprite CellSprite(Player p,BackpackCell cell)
        {
            if(cell.Empty)return null;
            if(cell.Kind==1)return SlotIconSprite(p,cell.Id);
            if(cell.Kind==2)return HeldItemSprite(cell.Id);
            if(cell.Kind==3)return MaterialSprite(ResourceIcons[Mathf.Clamp(cell.Id,0,ResourceIcons.Length-1)]);
            return null;
        }

        string CellTitle(Player p,BackpackCell cell)
        {
            if(cell.Empty)return L("Trống","Empty");
            if(cell.Kind==1)return InventorySlotName(p,cell.Id);
            if(cell.Kind==2)return (EmberLanguage.English?ItemNamesEn:ItemNamesVi)[cell.Id];
            string[] resEn={"Lumen Dust","Core","Umbra Shard","Wood","Ore"};
            return (EmberLanguage.English?resEn:Catalog.ResourceNames)[Mathf.Clamp(cell.Id,0,4)];
        }

        string CellShortTag(Player p,BackpackCell cell)
        {
            if(cell.Empty)return "";
            if(cell.Kind==1)return SlotShortValue(p,cell.Id);
            return cell.Count+"/"+StackLimitFor(cell.Kind,cell.Id);
        }

        string CellDetailInfo(Player p,BackpackCell cell)
        {
            if(cell.Empty)return BuildHoldingInfo(p);
            if(cell.Kind==1&&cell.Id==8)
            {
                string info=p.PickaxeName+" ["+L("Cấp ","Tier ")+(p.PickaxeTier+1)+"] · "+L("Độ bền: ","Durability: ")+p.PickaxeDurability+"/"+p.PickaxeMaxDurability;
                info+=" · "+L("Sản lượng: +","Yield: +")+p.PickaxeTier+" · "+L("Tốc độ đào: +","Speed: +")+(p.PickaxeTier*12)+"%";
                return info;
            }
            int eqSlot=EquipSlotForCell(cell);
            if(eqSlot>0)
            {
                int prevHeld=heldSlot;
                heldSlot=eqSlot;
                string info=BuildHoldingInfo(p);
                heldSlot=prevHeld;
                return info;
            }
            string name=CellTitle(p,cell);
            int limit=StackLimitFor(cell.Kind,cell.Id);
            string desc=cell.Id==0?L("Nhiên liệu nạp đèn [Q] và nguyên liệu chế tạo cơ bản.","Refuels lantern [Q] and basic crafting.")
                :cell.Id==1?L("Mang về Hải Đăng bấm [B] để nạp +25 năng lượng trụ.","Bring to Beacon and press [B] for +25 beacon energy.")
                :cell.Id==2?L("Nguyên liệu hiếm từ quái tinh anh/tổ/Boss để chế trang bị cao cấp.","Rare drop from Elites/Nests/Bosses for high-tier gear.")
                :cell.Id==3?L("Dùng chế tạo Đuốc, Tường, Rương chung và Vũ khí.","Used to craft Torches, Walls, Chests, and Weapons.")
                :L("Dùng chế tạo Pháo sáng, Bẫy điện và Vũ khí tầm xa.","Used to craft Flares, Traps, and Ranged Weapons.");
            return name+" ("+cell.Count+"/"+limit+") · "+L("Tài nguyên · ","Resource · ")+desc;
        }

        void MoveOrSwapBackpackCells(int from,int to)
        {
            if(from<0||from>=BackpackCapacity||to<0||to>=BackpackCapacity||from==to)return;
            var a=backpackCells[from];
            var b=backpackCells[to];
            if(a.Empty)return;
            if(!b.Empty&&a.Kind==b.Kind&&a.Id==b.Id)
            {
                int limit=StackLimitFor(b.Kind,b.Id);
                int space=limit-b.Count;
                if(space>0)
                {
                    int transfer=Mathf.Min(space,a.Count);
                    backpackCells[to].Count+=transfer;
                    backpackCells[from].Count-=transfer;
                    if(backpackCells[from].Count<=0)backpackCells[from]=default;
                    return;
                }
            }
            backpackCells[to]=a;
            backpackCells[from]=b;
        }

        void DrawInventory(Player p)
        {
            SyncBackpack(p);
            const int cols=5,rows=6;
            float width=Mathf.Min(492,Screen.width-24);
            float cellSize=Mathf.Floor(Mathf.Min(72,(width-74)/cols));
            float gap=6f;
            float viewHeight=Mathf.Min(232,Mathf.Max(154,Screen.height-320));
            float boxHeight=viewHeight+244;
            var box=new Rect((Screen.width-width)/2,Mathf.Max(52,(Screen.height-boxHeight)/2),width,boxHeight);
            PixelPanel(box,FieldPaper);
            RegisterHudRect(box);

            int usedSlots=UsedBackpackSlots();
            UiLabel(new Rect(box.x+14,box.y+6,120,34),L("Kho đồ","Inventory"),title);
            var countChip=new Rect(box.x+132,box.y+11,148,26);
            PixelPanel(countChip,new Color(.19f,.13f,.30f));
            UiLabel(new Rect(countChip.x+8,countChip.y+3,countChip.width-12,20),
                L("Ô đồ: ","Slots: ")+usedSlots+"/"+BackpackCapacity+"  ·  "+p.Load+"/"+Mathf.FloorToInt(p.Capacity),small);

            if(UiButton(new Rect(box.xMax-138,box.y+9,66,30),L("Xếp","Sort")))SortBackpack(p);
            if(UiButton(new Rect(box.xMax-68,box.y+9,54,30),L("Đóng","Close"))){inventoryOpen=false;pendingQuickAssign=0;dragFromCell=-1;draggingItem=false;contextMenuOpen=false;}

            UiLabel(new Rect(box.x+14,box.y+42,width-28,20),
                L("Kéo vào ô 1–4 để gán · Nhấn để cầm/cất · Chuột phải mở menu vứt đồ & gán ô",
                  "Drag onto slots 1–4 to bind · Click to equip/unequip · Right-click for drop & bind menu"),small);

            float gridTop=box.y+64;
            var scrollFrame=new Rect(box.x+12,gridTop,width-24,viewHeight);
            PixelPanel(scrollFrame,new Color(.11f,.08f,.19f));

            float contentW=cols*cellSize+(cols-1)*gap;
            float contentH=rows*cellSize+(rows-1)*gap+10;
            var viewRect=new Rect(scrollFrame.x+8,scrollFrame.y+6,scrollFrame.width-16,scrollFrame.height-12);
            var contentRect=new Rect(0,0,Mathf.Max(contentW,viewRect.width-20),contentH);

            var ev=Event.current;
            Vector2 globalMouse=ev.mousePosition;
            int hoveredCell=-1;

            inventoryScroll=GUI.BeginScrollView(viewRect,inventoryScroll,contentRect,false,true);
            Vector2 localMouse=ev.mousePosition;
            bool insideViewport=viewRect.Contains(globalMouse);
            for(int i=0;i<BackpackCapacity;i++)
            {
                int col=i%cols,row=i/cols;
                var tile=new Rect(col*(cellSize+gap)+2,row*(cellSize+gap)+4,cellSize,cellSize);
                var cell=backpackCells[i];
                int eqSlot=EquipSlotForCell(cell);
                bool active=eqSlot>0&&heldSlot==eqSlot;
                bool assigning=eqSlot>0&&pendingQuickAssign==eqSlot;
                bool inspected=inspectedCell==i&&!cell.Empty;
                bool isDragSource=draggingItem&&dragFromCell==i;
                bool hovered=insideViewport&&tile.Contains(localMouse);
                if(hovered)hoveredCell=i;

                Color bg=isDragSource?new Color(.14f,.10f,.24f):
                    (draggingItem&&hovered)?new Color(.34f,.52f,.62f):
                    assigning?new Color(.68f,.44f,.18f):
                    active?Plum:
                    inspected?new Color(.32f,.24f,.50f):
                    cell.Empty?new Color(.14f,.10f,.23f):new Color(.23f,.17f,.36f);
                PixelPanel(tile,bg);

                if(!cell.Empty&&!isDragSource)
                {
                    DrawSpritePreview(new Rect(tile.center.x-19,tile.y+6,38,38),CellSprite(p,cell));
                    if(eqSlot>0)
                    {
                        int qIdx=System.Array.IndexOf(quickSlots,eqSlot);
                        if(qIdx>=0)
                        {
                            var badge=new Rect(tile.xMax-18,tile.y+3,15,15);
                            Panel(badge,new Color(.09f,.07f,.17f,.92f));
                            UiLabel(new Rect(badge.x+3,badge.y-2,12,17),(qIdx+1).ToString(),small);
                        }
                    }
                    string shortTag=CellShortTag(p,cell);
                    if(!string.IsNullOrEmpty(shortTag))
                    {
                        UiLabel(new Rect(tile.x+4,tile.yMax-20,cellSize-8,18),shortTag,small);
                    }
                }
                else if(cell.Empty)
                {
                    UiLabel(new Rect(tile.x+5,tile.y+3,24,16),(i+1).ToString(),small);
                }

                if(hovered&&ev.type==EventType.MouseDown)
                {
                    if(ev.button==1&&!cell.Empty)
                    {
                        contextMenuOpen=true;
                        contextMenuCell=i;
                        contextMenuPos=globalMouse;
                        inspectedCell=i;
                        ev.Use();
                    }
                    else if(ev.button==0&&!cell.Empty)
                    {
                        contextMenuOpen=false;
                        dragFromCell=i;
                        dragStartMouse=globalMouse;
                        draggingItem=false;
                        inspectedCell=i;
                        ev.Use();
                    }
                }
            }
            GUI.EndScrollView();

            if(dragFromCell>=0&&ev.type==EventType.MouseDrag&&Vector2.Distance(globalMouse,dragStartMouse)>6f)
                draggingItem=true;

            // Detail marquee bar.
            float detailY=scrollFrame.yMax+8;
            var detail=new Rect(box.x+12,detailY,width-24,34);
            PixelPanel(detail,new Color(.22f,.16f,.34f));
            var infoCell=(inspectedCell>=0&&inspectedCell<BackpackCapacity&&!backpackCells[inspectedCell].Empty)
                ?backpackCells[inspectedCell]:default;
            string detailText=infoCell.Empty?L("Đang cầm: ","Holding: ")+BuildHoldingInfo(p):CellDetailInfo(p,infoCell);
            DrawMarqueeLabel(new Rect(detail.x+9,detail.y+5,detail.width-18,24),detailText,small);

            // Quick slots 1-4 (also valid drop targets!).
            float quickLabelY=detail.yMax+6;
            string quickHint=pendingQuickAssign>0
                ?L("Bấm ô 1–4 (hoặc phím 1–4) để gán: ","Click slot 1–4 (or press 1–4) to bind: ")+InventorySlotName(p,pendingQuickAssign)
                :draggingItem?L("Thả vào ô trong kho để di chuyển, hoặc thả xuống ô 1–4 bên dưới để gán","Drop onto a slot to move, or drop onto quick slots 1–4 below to bind")
                :L("Ô phím tắt nhanh 1–4 · Kéo vũ khí/đồ dùng thả vào đây để gán","Quick slots 1–4 · Drag any weapon/consumable here to bind");
            var oldContent=GUI.contentColor;
            if(pendingQuickAssign>0||draggingItem)GUI.contentColor=Gold;
            UiLabel(new Rect(box.x+14,quickLabelY,width-28,22),quickHint,small);
            GUI.contentColor=oldContent;

            float qY=quickLabelY+24;
            float qSize=Mathf.Min(64,(width-46)/4);
            float qGridW=4*qSize+18;
            int hoveredQuick=-1;
            for(int q=0;q<4;q++)
            {
                int boundSlot=quickSlots[q];
                var qTile=new Rect(box.center.x-qGridW/2+q*(qSize+6),qY,qSize,qSize);
                bool qHover=qTile.Contains(globalMouse);
                if(qHover)hoveredQuick=q;
                bool dragAcceptable=draggingItem&&dragFromCell>=0&&EquipSlotForCell(backpackCells[dragFromCell])>0;
                Color qBg=(dragAcceptable&&qHover)?new Color(.38f,.65f,.80f):
                    (qHover&&!draggingItem)?new Color(.28f,.22f,.42f):
                    pendingQuickAssign>0?new Color(.42f,.28f,.60f):
                    heldSlot==boundSlot?Plum:new Color(.18f,.13f,.28f);
                PixelPanel(qTile,qBg);
                DrawSpritePreview(new Rect(qTile.center.x-16,qTile.y+5,32,32),SlotIconSprite(p,boundSlot));
                UiLabel(new Rect(qTile.x+4,qTile.yMax-21,qSize-8,20),(q+1)+" · "+SlotShortValue(p,boundSlot),small);
                if(!draggingItem&&qHover&&ev.type==EventType.MouseDown&&ev.button==0)
                {
                    if(pendingQuickAssign>0)BindQuickSlot(q,pendingQuickAssign);
                    else EquipSlot(boundSlot);
                    ev.Use();
                }
            }

            // Finish click or drag-and-drop on mouse up.
            if(ev.type==EventType.MouseUp&&ev.button==0&&dragFromCell>=0)
            {
                if(draggingItem)
                {
                    if(hoveredCell>=0)
                    {
                        MoveOrSwapBackpackCells(dragFromCell,hoveredCell);
                        inspectedCell=hoveredCell;
                    }
                    else if(hoveredQuick>=0)
                    {
                        int eqSlot=EquipSlotForCell(backpackCells[dragFromCell]);
                        if(eqSlot>0)
                        {
                            BindQuickSlot(hoveredQuick,eqSlot);
                            notice=(EmberLanguage.English?"Bound slot ":"Đã gán ô ")+(hoveredQuick+1)+": "+InventorySlotName(p,eqSlot);
                        }
                    }
                }
                else
                {
                    int eqSlot=EquipSlotForCell(backpackCells[dragFromCell]);
                    if(eqSlot>0){pendingQuickAssign=0;EquipSlot(eqSlot);}
                }
                dragFromCell=-1;
                draggingItem=false;
                ev.Use();
            }

            // Floating ghost icon following the cursor while dragging.
            if(draggingItem&&dragFromCell>=0&&!backpackCells[dragFromCell].Empty&&ev.type==EventType.Repaint)
            {
                var ghost=new Rect(globalMouse.x-26,globalMouse.y-26,52,52);
                GUI.color=new Color(1,1,1,.88f);
                PixelPanel(ghost,new Color(.38f,.28f,.58f,.92f));
                DrawSpritePreview(new Rect(ghost.x+7,ghost.y+7,38,38),CellSprite(p,backpackCells[dragFromCell]));
                GUI.color=Color.white;
            }

            DrawContextMenu(p,box);
        }

        void DrawContextMenu(Player p,Rect parentBox)
        {
            if(!contextMenuOpen||contextMenuCell<0||contextMenuCell>=BackpackCapacity)return;
            var cell=backpackCells[contextMenuCell];
            if(cell.Empty){contextMenuOpen=false;return;}
            int eqSlot=EquipSlotForCell(cell);
            float mw=256,mh=eqSlot>0?200:150;
            float mx=Mathf.Clamp(contextMenuPos.x,parentBox.x+10,parentBox.xMax-mw-10);
            float my=Mathf.Clamp(contextMenuPos.y,parentBox.y+10,parentBox.yMax-mh-10);
            var menuRect=new Rect(mx,my,mw,mh);
            PixelPanel(menuRect,new Color(.11f,.08f,.19f,.98f));
            DrawSpritePreview(new Rect(menuRect.x+8,menuRect.y+7,26,26),CellSprite(p,cell));
            string titleName=cell.Kind==1&&cell.Id==8?p.PickaxeName:CellTitle(p,cell);
            UiLabel(new Rect(menuRect.x+38,menuRect.y+7,mw-46,24),titleName,small);

            var menuBtnStyle=new GUIStyle(GUI.skin.button)
            {
                fontSize=12,
                padding=new RectOffset(4,4,2,2),
                alignment=TextAnchor.MiddleCenter
            };

            float curY=menuRect.y+36;
            if(eqSlot>0)
            {
                UiLabel(new Rect(menuRect.x+8,curY,mw-16,18),L("Gán ô phím tắt:","Assign Quick Slot:"),small);
                curY+=20;
                float bw=(mw-28)/4f;
                for(int k=0;k<4;k++)
                {
                    int qSlot=k;
                    if(GUI.Button(new Rect(menuRect.x+8+k*(bw+4),curY,bw,24),""+(k+1),menuBtnStyle))
                    {
                        BindQuickSlot(qSlot,eqSlot);
                        contextMenuOpen=false;
                    }
                }
                curY+=28;
                string equipText=heldSlot==eqSlot?L("Cất đồ (Tay không)","Unequip (Hands)"):L("Cầm lên tay","Equip");
                if(GUI.Button(new Rect(menuRect.x+8,curY,mw-16,26),equipText,menuBtnStyle))
                {
                    EquipSlot(eqSlot);
                    contextMenuOpen=false;
                }
                curY+=30;
            }
            if(cell.Kind==3||cell.Kind==2)
            {
                if(cell.Count>1)
                {
                    float halfW=(mw-22)/2f;
                    if(GUI.Button(new Rect(menuRect.x+8,curY,halfW,26),L("Vứt 1","Drop 1"),menuBtnStyle))
                    {
                        DropBackpackItem(p,contextMenuCell,1);
                        contextMenuOpen=false;
                    }
                    if(GUI.Button(new Rect(menuRect.x+12+halfW,curY,halfW,26),L("Vứt hết ("+cell.Count+")","Drop all ("+cell.Count+")"),menuBtnStyle))
                    {
                        DropBackpackItem(p,contextMenuCell,cell.Count);
                        contextMenuOpen=false;
                    }
                    curY+=30;
                }
                else
                {
                    if(GUI.Button(new Rect(menuRect.x+8,curY,mw-16,26),L("Vứt ra đất","Drop to ground"),menuBtnStyle))
                    {
                        DropBackpackItem(p,contextMenuCell,1);
                        contextMenuOpen=false;
                    }
                    curY+=30;
                }
            }
            else if(cell.Kind==1&&cell.Id==8)
            {
                if(GUI.Button(new Rect(menuRect.x+8,curY,mw-16,26),L("Vứt cuốc đào","Drop pickaxe"),menuBtnStyle))
                {
                    DropBackpackItem(p,contextMenuCell,1);
                    contextMenuOpen=false;
                }
                curY+=30;
            }
            if(GUI.Button(new Rect(menuRect.x+8,curY,mw-16,24),L("Đóng","Close"),menuBtnStyle))
            {
                contextMenuOpen=false;
            }
            var ev=Event.current;
            if(ev.type==EventType.MouseDown&&!menuRect.Contains(ev.mousePosition))
            {
                contextMenuOpen=false;
            }
        }
    }
}
