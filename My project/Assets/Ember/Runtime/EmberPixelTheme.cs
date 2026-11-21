using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        GUISkin fieldSkin;
        GUIStyle frameStyle;

        static Texture2D PixelFrame(Color fill, Color rim, Color gleam)
        {
            const int size=14;
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false)
            {
                filterMode=FilterMode.Point,
                wrapMode=TextureWrapMode.Clamp,
                hideFlags=HideFlags.HideAndDontSave
            };
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)
            for(int x=0;x<size;x++)
            {
                int dx=Mathf.Min(x,size-1-x),dy=Mathf.Min(y,size-1-y);
                if(dx+dy<2)continue;
                pixels[y*size+x]=dx==0||dy==0||dx+dy==2?rim:y>=size-3?gleam:fill;
            }
            tex.SetPixels(pixels);
            tex.Apply(false,true);
            return tex;
        }

        void PaintPixelSkin(GUISkin skin,bool light)
        {
            Color surface=light?Lilac:new Color(.21f,.17f,.32f);
            Color edge=light?new Color(.39f,.28f,.55f):new Color(.48f,.37f,.65f);
            Color text=light?Ink:new Color(.98f,.93f,1);
            var normal=PixelFrame(surface,edge,Color.Lerp(surface,Color.white,.24f));
            var hover=PixelFrame(Color.Lerp(surface,edge,.19f),edge,Color.Lerp(surface,Color.white,.38f));
            var active=PixelFrame(Plum,edge,new Color(.66f,.51f,.85f));
            var input=PixelFrame(light?Paper:new Color(.13f,.10f,.21f),edge,surface);
            var rail=PixelFrame(light?new Color(.85f,.80f,.91f):new Color(.10f,.08f,.16f),edge,surface);
            foreach(var style in new[]{skin.button,skin.toggle,skin.box,skin.textField,skin.textArea})
            {
                style.border=new RectOffset(6,6,6,6);
                style.normal.background=style==skin.textField||style==skin.textArea?input:normal;
                style.hover.background=style.focused.background=hover;
                style.active.background=style.onNormal.background=style.onHover.background=style.onActive.background=style.onFocused.background=active;
                style.normal.textColor=style.hover.textColor=style.focused.textColor=text;
                style.active.textColor=style.onNormal.textColor=style.onHover.textColor=style.onActive.textColor=style.onFocused.textColor=Color.white;
            }
            skin.button.fontStyle=FontStyle.Bold;
            skin.button.padding=new RectOffset(11,11,6,8);
            skin.button.clipping=TextClipping.Overflow;
            skin.box.clipping=TextClipping.Overflow;
            skin.label.clipping=TextClipping.Overflow;
            skin.label.normal.background=null;
            foreach(var bar in new[]{skin.verticalScrollbar,skin.horizontalScrollbar,skin.verticalScrollbarThumb,skin.horizontalScrollbarThumb})
            {
                bar.border=new RectOffset(5,5,5,5);
                bar.normal.background=bar==skin.verticalScrollbarThumb||bar==skin.horizontalScrollbarThumb?active:rail;
                bar.hover.background=bar.active.background=bar.normal.background;
            }
            var slider=skin.horizontalSlider;
            slider.border=new RectOffset(6,6,4,4);
            slider.fixedHeight=12;
            slider.normal.background=slider.hover.background=slider.active.background=rail;

            var grip=skin.horizontalSliderThumb;
            grip.border=new RectOffset(5,5,5,5);
            grip.fixedWidth=18;
            grip.fixedHeight=16;
            grip.normal.background=active;
            grip.hover.background=hover;
            grip.active.background=active;

            skin.toggle.padding=new RectOffset(9,9,6,6);
            skin.toggle.fontStyle=FontStyle.Bold;
        }

        void PixelPanel(Rect rect,Color fill)
        {
            if(Event.current.type!=EventType.Repaint||rect.width<14||rect.height<14)return;
            RegisterHudRect(rect);
            if(frameStyle==null)
            {
                frameStyle=new GUIStyle{border=new RectOffset(6,6,6,6)};
                frameStyle.normal.background=PixelFrame(Color.white,new Color(.57f,.48f,.68f),Color.white);
            }
            Color previous=GUI.color;
            GUI.color=fill;
            frameStyle.Draw(rect,GUIContent.none,false,false,false,false);
            GUI.color=previous;
        }
    }
}
