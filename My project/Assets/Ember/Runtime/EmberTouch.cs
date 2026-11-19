using UnityEngine;
using UnityEngine.InputSystem;

namespace Ember
{
    public sealed partial class EmberGame
    {
        int joystickFinger=-1;Vector2 joystickStart,joystickDelta;Texture2D touchDisc;
        bool TouchEnabled=>Touchscreen.current!=null;
        Rect AttackTouch=>new Rect(Screen.width-135,Screen.height-225,105,105);
        Rect SkillTouch=>new Rect(Screen.width-245,Screen.height-215,85,85);
        Rect DashTouch=>new Rect(Screen.width-135,Screen.height-325,85,85);
        Rect RangesTouch=>new Rect(Screen.width-235,Screen.height-310,65,65);
        Rect PushTouch=>new Rect(Screen.width-215,Screen.height-393,65,65);
        Rect DepositTouch=>new Rect(Screen.width-330,Screen.height-290,65,65);
        Rect RefuelTouch=>new Rect(Screen.width-410,Screen.height-205,65,65);
        Rect InteractTouch=>new Rect(Screen.width-330,Screen.height-205,65,65);
        void ApplyTouchInput()
        {
            if(!TouchEnabled||craftOpen||inventoryOpen)return;bool joystickHeld=false;
            foreach(var touch in Touchscreen.current.touches)
            {
                if(!touch.press.isPressed)continue;int id=touch.touchId.ReadValue();var pos=touch.position.ReadValue();var start=touch.startPosition.ReadValue();var gui=new Vector2(pos.x,Screen.height-pos.y);
                if(joystickFinger<0&&start.x<Screen.width*.4f&&start.y<Screen.height*.5f){joystickFinger=id;joystickStart=start;}
                if(id==joystickFinger)
                {
                    joystickHeld=true;joystickDelta=Vector2.ClampMagnitude((pos-joystickStart)/80,1);if(joystickDelta.magnitude<.15f)joystickDelta=Vector2.zero;
                    intent.X=joystickDelta.x;intent.Y=joystickDelta.y;if(joystickDelta.sqrMagnitude>0)path.Clear();continue;
                }
                if(AttackTouch.Contains(gui)&&Local!=null&&IsHoldingWeaponSlot(heldSlot)&&Local.AttackCD<=0)intent.Attack=true;
                if(!touch.press.wasPressedThisFrame)continue;
                if(SkillTouch.Contains(gui)&&Local!=null&&(Local.Vessel!=Vessel.Wanderer||Local.Awake))intent.Skill=true;if(PushTouch.Contains(gui))intent.Push=true;if(DashTouch.Contains(gui))intent.Dash=true;if(RangesTouch.Contains(gui))ranges=!ranges;
                if(InteractTouch.Contains(gui))intent.Interact=true;
                if(DepositTouch.Contains(gui))intent.Deposit=true;
                if(RefuelTouch.Contains(gui))intent.Refuel=true;
            }
            if(!joystickHeld){joystickFinger=-1;joystickDelta=Vector2.zero;}
        }
        void DrawTouchControls()
        {
            if(!TouchEnabled||(network&&!host&&!string.IsNullOrEmpty(lan?.Error))||menu||inventoryOpen||craftOpen||debugOpen||State==null||State.Phase==Phase.Sanctuary||Local==null||Local.Ghost||Local.Extracted||State.Phase==Phase.Won||State.Phase==Phase.Lost)return;
            if(touchDisc==null)
            {
                touchDisc=new Texture2D(64,64,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};var pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));pixels[y*64+x]=r<31?new Color(.4f,.8f,.85f,r>28?.6f:.13f):Color.clear;}touchDisc.SetPixels(pixels);touchDisc.Apply();
            }
            Vector2 center=joystickFinger<0?new Vector2(120,Screen.height-205):new Vector2(joystickStart.x,Screen.height-joystickStart.y);
            GUI.DrawTexture(new Rect(center.x-80,center.y-80,160,160),touchDisc);
            var knob=center+new Vector2(joystickDelta.x,-joystickDelta.y)*55;GUI.DrawTexture(new Rect(knob.x-25,knob.y-25,50,50),touchDisc);
            var p=Local;
            DrawTouchTile(AttackTouch,L("Đánh","Attack"),IsHoldingWeaponSlot(heldSlot)?Primitive(InventoryWeaponArt[(int)WeaponForSlot(p,heldSlot)]):null,IsHoldingWeaponSlot(heldSlot));
            if(p.Vessel!=Vessel.Wanderer||p.Awake)
                DrawTouchTile(SkillTouch,L("Kỹ năng","Skill"),Frame(p.Vessel,"idle",0),
                    p.SkillCD<=0&&HasSkillFocus(p));
            DrawTouchTile(DashTouch,L("Lướt","Dash"),MaterialSprite("perk_2"),p.DashCD<=0);
            DrawTouchTile(PushTouch,L("Đẩy","Shove"),Glove(false),p.PushCD<=0&&p.Stamina>=15);
            DrawTouchTile(RangesTouch,L("Tầm","Range"),Primitive("ring"),ranges);
            DrawTouchTile(InteractTouch,L("Tương tác","Interact"),Glove(true),true);
            DrawTouchTile(DepositTouch,L("Nạp NL","Deposit"),MaterialSprite("core"),true);
            DrawTouchTile(RefuelTouch,L("Nạp đèn","Refuel"),Primitive("torch"),true);
        }

        void DrawTouchTile(Rect rect,string caption,Sprite icon,bool ready)
        {
            PixelPanel(rect,ready?new Color(.28f,.20f,.42f,.91f):FieldPaper);
            float size=Mathf.Min(40,rect.height-25);
            DrawSpritePreview(new Rect(rect.center.x-size/2,rect.y+4,size,size),icon);
            UiLabel(new Rect(rect.x+4,rect.yMax-21,rect.width-8,20),caption,small);
        }
    }
}
