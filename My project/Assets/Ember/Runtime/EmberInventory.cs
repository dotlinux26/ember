using System.Linq;
using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        bool inventoryOpen;
        int heldSlot=6;
        static readonly string[] InventoryWeaponArt={"stick","crystal_axe","shadow_blades","crossbow","lumen_gun","mirror_shield","pickaxe"};

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
            bool weapon=slot==1;
            bool consumable=slot>=2&&slot<=5&&player.Items[slot-2]>0;
            float sign=player.Right?1:-1;
            int handOrder=body.sortingOrder+4;
            int heldOrder=handOrder-1;
            var pivots=HandPivotFor(player.Vessel);
            V2 holdingHand=player.Pos+new V2(sign*pivots.FrontX/48f,pivots.FrontY/48f);
            V2 offHand=player.Pos+new V2(-sign*pivots.BackX/48f,pivots.BackY/48f);

            V2 rightHand=player.Right?holdingHand:offHand;
            V2 leftHand=player.Right?offHand:holdingHand;
            Sprite hand=Glove(false);
            bool flipHands=!player.Right;
            Put("handRight"+player.Id,hand,rightHand,Color.white,.5f,handOrder,flipHands);
            Put("handLeft"+player.Id,hand,leftHand,Color.white,.5f,handOrder,flipHands);
            if(weapon)
            {
                var target=State.Enemies.Where(e=>e.HP>0&&V2.Distance(e.Pos,player.Pos)<player.Range)
                    .OrderBy(e=>V2.Distance(e.Pos,player.Pos)).FirstOrDefault();
                V2? aim=target?.Pos;
                if(player.Mad&&!player.Safe)
                {
                    var ally=State.Players.Where(q=>q.Id!=player.Id&&!q.Safe&&!q.Ghost&&!q.Extracted&&V2.Distance(q.Pos,player.Pos)<player.Range)
                        .OrderBy(q=>V2.Distance(q.Pos,player.Pos)).FirstOrDefault();
                    if(ally!=null&&(target==null||V2.Distance(ally.Pos,player.Pos)<V2.Distance(target.Pos,player.Pos)))aim=ally.Pos;
                }

                var held=Put("weapon"+player.Id,Primitive(InventoryWeaponArt[(int)player.Weapon]),
                    holdingHand+new V2(sign*.075f,.12f),Color.white,.85f,heldOrder,!player.Right);
                float angle=aim.HasValue?Mathf.Atan2(aim.Value.Y-holdingHand.Y,aim.Value.X-holdingHand.X)*Mathf.Rad2Deg-90:-sign*30;
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
            if(slot==6)return L("Tay không","Empty hands");
            return (EmberLanguage.English?ItemNamesEn:ItemNamesVi)[slot-2];
        }

        void DrawInventory(Player p)
        {
            float width=Mathf.Min(410,Screen.width-28);
            var box=new Rect((Screen.width-width)/2,Mathf.Max(98,(Screen.height-300)/2),width,284);
            PixelPanel(box,FieldPaper);
            UiLabel(new Rect(box.x+14,box.y+4,width-112,43),L("Kho đồ","Inventory"),title);
            if(UiButton(new Rect(box.xMax-92,box.y+9,76,34),L("Đóng","Close")))inventoryOpen=false;
            float size=Mathf.Min(76,(width-38)/3);
            float gridWidth=3*size+12;
            for(int i=0;i<6;i++)
            {
                int slot=i+1;
                var tile=new Rect(box.center.x-gridWidth/2+(i%3)*(size+6),box.y+53+(i/3)*(size+6),size,size);
                bool active=heldSlot==slot;
                PixelPanel(tile,active?Plum:new Color(.22f,.16f,.34f));
                Sprite sprite=i==0?Primitive(InventoryWeaponArt[(int)p.Weapon]):i<5?HeldItemSprite(i-1):null;
                DrawSpritePreview(new Rect(tile.center.x-21,tile.y+7,42,42),sprite);
                string name=InventorySlotName(p,slot);
                string line=i>0&&i<5?i+" · "+p.Items[i-1]:i==0?L("0 · Vũ khí","0 · Weapon"):name;
                UiLabel(new Rect(tile.x+4,tile.yMax-22,size-7,21),line,small);
                if(GUI.Button(tile,new GUIContent("",name),GUIStyle.none))EquipSlot(slot);
            }

            var detail=new Rect(box.x+14,box.y+67+2*size,width-28,39);
            PixelPanel(detail,new Color(.22f,.16f,.34f));
            string equipped=InventorySlotName(p,heldSlot);
            if(heldSlot>=2&&heldSlot<=5&&p.Items[heldSlot-2]==0)equipped+=L(" · Hết đồ"," · Empty");
            UiLabel(new Rect(detail.x+9,detail.y+6,detail.width-18,30),L("Đang cầm: ","Holding: ")+equipped,label);
        }
    }
}
