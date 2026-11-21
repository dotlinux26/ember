using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        void UiLabel(Rect rect,string text,GUIStyle style)=>GUI.Label(rect,EmberLanguage.Text(text),style);
        void UiText(string text,GUIStyle style)=>GUILayout.Label(EmberLanguage.Text(text),style);
        readonly Dictionary<GUISkin, GUIStyle[]> compactButtonStyles=new Dictionary<GUISkin, GUIStyle[]>();
        bool UiButton(Rect rect,string text)
        {
            RegisterHudRect(rect);
            var content=new GUIContent(EmberLanguage.Text(text));
            if(!compactButtonStyles.TryGetValue(GUI.skin,out var styles))
            {
                styles=new GUIStyle[4];
                for(int i=0;i<styles.Length;i++)
                {
                    styles[i]=new GUIStyle(GUI.skin.button)
                    {
                        fontSize=14-i,
                        padding=new RectOffset(6,6,3,3),
                        alignment=TextAnchor.MiddleCenter,
                        wordWrap=false,
                        clipping=TextClipping.Clip
                    };
                }
                compactButtonStyles.Add(GUI.skin,styles);
            }
            foreach(var style in styles)
            {
                var needed=style.CalcSize(content);
                if(needed.x<=rect.width&&needed.y<=rect.height)
                    return GUI.Button(rect,content,style);
            }
            var fallback=styles[styles.Length-1];
            string full=content.text;
            content.tooltip=full;
            const string suffix="…";
            for(int length=full.Length-1;length>0;length--)
            {
                if(char.IsHighSurrogate(full[length-1]))continue;
                content.text=full.Substring(0,length).TrimEnd()+suffix;
                if(fallback.CalcSize(content).x<=rect.width)
                    return GUI.Button(rect,content,fallback);
            }
            content.text=suffix;
            return GUI.Button(rect,content,fallback);
        }
        bool UiButton(string text,params GUILayoutOption[] options)=>GUILayout.Button(EmberLanguage.Text(text),options);
        int UiSelection(int selected,string[] text,int columns)=>GUILayout.SelectionGrid(selected,text.Select(EmberLanguage.Text).ToArray(),columns);
        int UiToolbar(int selected,string[] text)=>GUILayout.Toolbar(selected,text.Select(EmberLanguage.Text).ToArray());
        void OnGUI()
        {
            if(Event.current.type==EventType.Layout)GUI.tooltip="";
            var original=GUI.skin;
            if(fieldSkin==null)
            {
                fieldSkin=Instantiate(original);
                PaintPixelSkin(fieldSkin,false);
            }
            GUI.skin=fieldSkin;
            try { DrawPixelInterface(); }
            finally { GUI.skin=original; }
        }

        void DrawPixelInterface()
        {
            DrawInterface();
            DrawTouchControls();
            DrawDebugPanel();
            float rightPad=12f;
            float langW=90f;
            if(GUI.Button(new Rect(Screen.width-rightPad-langW,12,langW,28),EmberLanguage.English?"EN  /  VI":"VI  /  EN"))
            {
                EmberLanguage.English=!EmberLanguage.English;PlayerPrefs.SetInt("Ember.Language",EmberLanguage.English?1:0);PlayerPrefs.Save();
            }
            DrawFrameRate();
            DrawPixelTooltip();
        }

        GUIStyle tooltipStyle;
        void DrawPixelTooltip()
        {
            string text=GUI.tooltip;
            GUI.tooltip="";
            if(Event.current.type!=EventType.Repaint||string.IsNullOrWhiteSpace(text))return;
            if(!menu&&!IsMouseOverHud())return;
            if(tooltipStyle==null)
                tooltipStyle=new GUIStyle(small)
                {
                    wordWrap=true,
                    normal={textColor=new Color(.98f,.93f,1)}
                };
            var content=new GUIContent(text);
            float width=Mathf.Min(Mathf.Max(100,tooltipStyle.CalcSize(content).x+22),Mathf.Min(340,Screen.width-16));
            float height=Mathf.Max(30,tooltipStyle.CalcHeight(content,width-20)+12);
            var pointer=Event.current.mousePosition;
            float x=Mathf.Clamp(pointer.x+13,8,Screen.width-width-8);
            float y=pointer.y+19+height<=Screen.height-8?pointer.y+19:pointer.y-height-8;
            y=Mathf.Clamp(y,8,Mathf.Max(8,Screen.height-height-8));
            int depth=GUI.depth;
            GUI.depth=-110;
            PixelPanel(new Rect(x,y,width,height),new Color(.16f,.10f,.25f));
            GUI.Label(new Rect(x+10,y+6,width-20,height-12),content,tooltipStyle);
            GUI.depth=depth;
        }
    }
}
