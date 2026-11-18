using System;
using System.Linq;
using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        Texture2D telegraphDisc;
        GUIStyle bossTitleStyle,bossSubStyle,bossAlertStyle,bossSkillStyle;

        static Color BossAccentColor(BossType type)
        {
            return type==BossType.CinderBehemoth?new Color(1f,.42f,.14f)
                :type==BossType.EclipseWeaver?new Color(.96f,.18f,.54f)
                :new Color(.94f,.18f,.28f);
        }

        static Color BossTintColor(BossType type)
        {
            return type==BossType.CinderBehemoth?new Color(1f,.74f,.58f)
                :type==BossType.EclipseWeaver?new Color(.82f,.76f,1f)
                :new Color(1f,.86f,.94f);
        }

        Texture2D EnsureTelegraphDisc()
        {
            if(telegraphDisc!=null)return telegraphDisc;
            const int size=64;
            telegraphDisc=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear};
            var px=new Color32[size*size];
            float half=size*.5f;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=(x+.5f-half)/half,dy=(y+.5f-half)/half;
                float r=Mathf.Sqrt(dx*dx+dy*dy);
                float a=r>=1f?0f:r>.86f?.9f:.34f+.20f*r;
                px[y*size+x]=new Color(1f,1f,1f,a);
            }
            telegraphDisc.SetPixels32(px);
            telegraphDisc.Apply();
            return telegraphDisc;
        }

        void EnsureBossStyles()
        {
            if(bossTitleStyle!=null)return;
            bossTitleStyle=new GUIStyle(label)
            {
                alignment=TextAnchor.MiddleCenter,
                fontStyle=FontStyle.Bold,
                fontSize=15,
                normal={textColor=new Color(1f,.92f,.92f)}
            };
            bossSubStyle=new GUIStyle(small)
            {
                alignment=TextAnchor.MiddleCenter,
                fontSize=11,
                normal={textColor=new Color(1f,.72f,.74f)}
            };
            bossAlertStyle=new GUIStyle(title)
            {
                alignment=TextAnchor.MiddleCenter,
                fontStyle=FontStyle.Bold,
                fontSize=18,
                normal={textColor=new Color(1f,.94f,.94f)}
            };
            bossSkillStyle=new GUIStyle(small)
            {
                alignment=TextAnchor.MiddleCenter,
                fontStyle=FontStyle.Bold,
                fontSize=12,
                normal={textColor=new Color(1f,.90f,.72f)}
            };
        }

        void DrawBossArena()
        {
            if(State==null||!State.BossArenaActive||Event.current.type!=EventType.Repaint)return;
            Color accent=BossAccentColor(State.BossType);
            float pulse=.72f+.28f*Mathf.Sin(clock*5.5f);
            GlowRing(State.BossArenaCenter,State.BossArenaRadius,new Color(accent.r,accent.g,accent.b,.88f*pulse),3.2f);
            Ring(State.BossArenaCenter,State.BossArenaRadius-.18f,new Color(1f,.72f,.72f,.42f*pulse),1.4f);

            // Animated barrier pillars/ticks along the sealed ring perimeter.
            const int ticks=24;
            for(int i=0;i<ticks;i++)
            {
                float a=i*(Mathf.PI*2f/ticks)+clock*.18f;
                V2 inner=State.BossArenaCenter+new V2(Mathf.Cos(a),Mathf.Sin(a))*(State.BossArenaRadius-.45f);
                V2 outer=State.BossArenaCenter+new V2(Mathf.Cos(a),Mathf.Sin(a))*(State.BossArenaRadius+.25f);
                GUI.color=new Color(accent.r,accent.g,accent.b,.65f*pulse);
                Line(ScreenPos(inner),ScreenPos(outer),2.2f);
            }
            GUI.color=Color.white;
        }

        void DrawBossTelegraphs()
        {
            if(State?.BossTelegraphs==null||State.BossTelegraphs.Length==0||Event.current.type!=EventType.Repaint)return;
            var disc=EnsureTelegraphDisc();
            float pxPerTile=Vector2.Distance(ScreenPos(new V2()),ScreenPos(new V2(1,0)));
            foreach(var bt in State.BossTelegraphs)
            {
                float duration=Mathf.Max(.05f,bt.Duration);
                float progress=Mathf.Clamp01(1f-bt.Timer/duration);
                Color accent=BossAccentColor(bt.BossType);
                float pulse=.78f+.22f*Mathf.Sin(clock*18f+bt.Id);

                if(bt.Kind==0)
                {
                    Vector2 c=ScreenPos(bt.Pos);
                    float rPx=bt.Radius*pxPerTile;
                    GUI.color=new Color(accent.r,accent.g,accent.b,.28f+.22f*progress);
                    GUI.DrawTexture(new Rect(c.x-rPx,c.y-rPx,rPx*2f,rPx*2f),disc);
                    // Expanding inner fill showing exact detonation countdown.
                    float fillR=Mathf.Max(3f,rPx*progress);
                    GUI.color=new Color(1f,.35f,.30f,.38f+.30f*progress);
                    GUI.DrawTexture(new Rect(c.x-fillR,c.y-fillR,fillR*2f,fillR*2f),disc);
                    GUI.color=Color.white;
                    GlowRing(bt.Pos,bt.Radius,new Color(1f,.30f,.32f,.95f*pulse),2.4f);
                }
                else if(bt.Kind==1)
                {
                    Vector2 start=ScreenPos(bt.Pos);
                    Vector2 end=ScreenPos(bt.Pos+bt.Dir*bt.Length);
                    Vector2 delta=end-start;
                    float lenPx=delta.magnitude;
                    float widthPx=Mathf.Max(14f,bt.Radius*2f*pxPerTile);
                    float angle=Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;
                    var prev=GUI.matrix;
                    GUIUtility.RotateAroundPivot(angle,start);
                    var lane=new Rect(start.x,start.y-widthPx*.5f,lenPx,widthPx);
                    // Base danger corridor.
                    Panel(lane,new Color(accent.r,accent.g,accent.b,.24f+.16f*pulse));
                    // Sweeping progress fill along the charge lane.
                    Panel(new Rect(lane.x,lane.y,lane.width*progress,lane.height),new Color(1f,.26f,.26f,.36f+.22f*progress));
                    // Crisp glowing red borders.
                    Color border=new Color(1f,.32f,.32f,.95f*pulse);
                    Panel(new Rect(lane.x,lane.y,lane.width,2.5f),border);
                    Panel(new Rect(lane.x,lane.yMax-2.5f,lane.width,2.5f),border);
                    Panel(new Rect(lane.x,lane.y,2.5f,lane.height),border);
                    Panel(new Rect(lane.xMax-2.5f,lane.y,2.5f,lane.height),border);
                    // Center direction stripe.
                    Panel(new Rect(lane.x+6,start.y-1.5f,Mathf.Max(0,lane.width-12),3f),new Color(1f,.82f,.76f,.55f*pulse));
                    GUI.matrix=prev;
                }
                else if(bt.Kind==2)
                {
                    float ringR=bt.Radius*(.55f+.55f*progress);
                    GlowRing(bt.Pos,ringR,new Color(accent.r,accent.g,accent.b,.92f*pulse),2.8f);
                    Ring(bt.Pos,bt.Radius,new Color(1f,.65f,.70f,.55f*pulse),1.5f);
                }
            }
            GUI.color=Color.white;
        }

        void DrawBossHud()
        {
            if(State==null)return;
            EnsureBossStyles();

            // 1. Boss HP Bar (top-center while World Boss is alive)
            if(State.BossArenaActive)
            {
                var boss=State.Enemies?.FirstOrDefault(e=>e.Id==State.BossId&&e.HP>0);
                if(boss!=null)
                {
                    var spec=Catalog.BossFor(boss.BossType);
                    Color accent=BossAccentColor(boss.BossType);
                    float barW=Mathf.Min(430f,Screen.width-320f);
                    if(barW<240f)barW=Mathf.Min(320f,Screen.width-24f);
                    float x=(Screen.width-barW)*.5f;
                    float y=12f;
                    var box=new Rect(x,y,barW,60f);
                    Panel(new Rect(box.x-2,box.y-2,box.width+4,box.height+4),new Color(accent.r,accent.g,accent.b,.78f));
                    Panel(box,new Color(.11f,.05f,.13f,.92f));

                    string name=EmberLanguage.English?spec.NameEn:spec.NameVi;
                    string sub=EmberLanguage.English?spec.SubtitleEn:spec.SubtitleVi;
                    GUI.Label(new Rect(box.x+8,box.y+3,box.width-16,20),name.ToUpperInvariant(),bossTitleStyle);
                    GUI.Label(new Rect(box.x+8,box.y+21,box.width-16,15),sub+"  ·  "+Mathf.CeilToInt(boss.HP)+" / "+Mathf.CeilToInt(boss.MaxHP),bossSubStyle);

                    var rail=new Rect(box.x+12,box.y+40,box.width-24,12);
                    Panel(rail,new Color(.24f,.10f,.16f));
                    float ratio=Mathf.Clamp01(boss.HP/Mathf.Max(1f,boss.MaxHP));
                    if(ratio>0)
                    {
                        var fill=new Rect(rail.x,rail.y,Mathf.Round(rail.width*ratio),rail.height);
                        Panel(fill,accent);
                        Panel(new Rect(fill.x,fill.y,fill.width,3),Color.Lerp(accent,Color.white,.42f));
                    }

                    if(State.BossSkillCalloutTimer>0&&!string.IsNullOrEmpty(State.BossSkillCallout))
                    {
                        var parts=State.BossSkillCallout.Split('|');
                        string skillLabel=EmberLanguage.English&&parts.Length>1?parts[1]:parts[0];
                        float pillW=Mathf.Min(barW-24,250f);
                        var pill=new Rect((Screen.width-pillW)*.5f,box.yMax+5,pillW,22);
                        float blink=.7f+.3f*Mathf.Sin(clock*14f);
                        Panel(pill,new Color(.34f,.06f,.10f,.88f*blink));
                        Panel(new Rect(pill.x,pill.y,pill.width,2),new Color(1f,.35f,.32f,blink));
                        GUI.Label(pill,"⚠  "+skillLabel.ToUpperInvariant()+"  ⚠",bossSkillStyle);
                    }
                }
            }

            // 2. Dramatic Red Alert Banner when Boss Event triggers
            if(State.BossAlertTimer>0)
            {
                float fade=Mathf.Clamp01(State.BossAlertTimer/.8f);
                float flash=.75f+.25f*Mathf.Sin(clock*12f);
                float w=Mathf.Min(540f,Screen.width-32f);
                float y=State.BossArenaActive?104f:66f;
                var banner=new Rect((Screen.width-w)*.5f,y,w,58f);
                Panel(new Rect(banner.x-3,banner.y-3,banner.width+6,banner.height+6),new Color(1f,.14f,.18f,.92f*fade*flash));
                Panel(banner,new Color(.26f,.03f,.06f,.92f*fade));
                GUI.color=new Color(1,1,1,fade);
                GUI.Label(new Rect(banner.x+10,banner.y+5,banner.width-20,26),
                    L("⚠ CẢNH BÁO ĐỎ · SỰ KIỆN BOSS ⚠","⚠ RED ALERT · WORLD BOSS EVENT ⚠"),bossAlertStyle);
                var spec=Catalog.BossFor(State.BossType);
                string detail=(EmberLanguage.English?spec.NameEn+" — "+spec.SubtitleEn:spec.NameVi+" — "+spec.SubtitleVi)
                    +"  ·  "+L("Toàn đội đã bị kéo vào vùng phong ấn!","All players pulled into the sealed arena!");
                GUI.Label(new Rect(banner.x+10,banner.y+32,banner.width-20,20),detail,bossSubStyle);
                GUI.color=Color.white;
            }
        }
    }
}
