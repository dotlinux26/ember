using System.Linq;
using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        bool HasSkillFocus(Player p)
        {
            if(p.Vessel==Vessel.SpaceWraith)return true;
            if(p.Vessel==Vessel.MoonMender)return State.Players.Any(q=>!q.Ghost&&!q.Extracted&&
                V2.Distance(q.Pos,p.Pos)<p.SkillRange&&(q.HP<q.MaxHP||q.Dread>0));
            return State.Enemies.Any(e=>e.HP>0&&V2.Distance(e.Pos,p.Pos)<p.SkillRange)||
                State.Players.Any(q=>q.Id!=p.Id&&!q.Ghost&&!q.Extracted&&V2.Distance(q.Pos,p.Pos)<p.SkillRange&&
                    (host&&World!=null?World.CanHarm(p,q):p.Mad&&!p.Safe&&!q.Safe));
        }

        void DrawStarFlare(Vector2 pos,float radius,float thickness,Color core,Color glow)
        {
            float glowSize=radius*1.45f;
            Panel(new Rect(pos.x-glowSize*.42f,pos.y-glowSize*.42f,glowSize*.84f,glowSize*.84f),glow);
            GUI.color=glow;
            Line(pos-new Vector2(radius*1.25f,0),pos+new Vector2(radius*1.25f,0),thickness*2.1f);
            Line(pos-new Vector2(0,radius*1.25f),pos+new Vector2(0,radius*1.25f),thickness*2.1f);
            GUI.color=core;
            Line(pos-new Vector2(radius,0),pos+new Vector2(radius,0),thickness);
            Line(pos-new Vector2(0,radius),pos+new Vector2(0,radius),thickness);
            float diag=radius*.55f;
            Line(pos-new Vector2(diag,diag),pos+new Vector2(diag,diag),thickness*.7f);
            Line(pos-new Vector2(diag,-diag),pos+new Vector2(diag,-diag),thickness*.7f);
            float dot=Mathf.Max(3f,thickness*2.2f);
            Panel(new Rect(pos.x-dot/2,pos.y-dot/2,dot,dot),core);
            GUI.color=Color.white;
        }

        void DrawStarStreak(Vector2 target,float progress,float slant,float height,float scale,float alpha)
        {
            float eased=progress*progress;
            Vector2 origin=target-new Vector2(slant,height);
            Vector2 head=Vector2.Lerp(origin,target,eased);
            Vector2 tailDir=new Vector2(slant,height).normalized;
            float tailLen=(70f+95f*progress)*scale;
            Vector2 tailEnd=head-tailDir*tailLen;

            GUI.color=new Color(.76f,.92f,1f,.30f*alpha);
            Line(tailEnd,head,9f*scale);
            GUI.color=new Color(.93f,.98f,1f,.62f*alpha);
            Line(Vector2.Lerp(tailEnd,head,.22f),head,4.8f*scale);
            GUI.color=new Color(1f,1f,1f,.96f*alpha);
            Line(Vector2.Lerp(tailEnd,head,.45f),head,2.3f*scale);
            GUI.color=Color.white;

            for(int i=1;i<=5;i++)
            {
                float frac=i/6f;
                Vector2 mote=Vector2.Lerp(head,tailEnd,frac)+new Vector2(Mathf.Sin(clock*22f+i*2.3f)*4f*scale,Mathf.Cos(clock*19f+i*1.7f)*3f*scale);
                float moteSize=(4.2f-frac*2.4f)*scale;
                Panel(new Rect(mote.x-moteSize/2,mote.y-moteSize/2,moteSize,moteSize),new Color(1f,1f,1f,(1f-frac)*.88f*alpha));
            }

            DrawStarFlare(head,18f*scale,2.6f*scale,new Color(1f,1f,1f,alpha),new Color(.78f,.93f,1f,.42f*alpha));
        }

        void DrawHarvestChips(Effect effect)
        {
            if(Event.current.type!=EventType.Repaint)return;
            Vector2 hit=ScreenPos(effect.To);
            float progress=1f-Mathf.Clamp01(effect.Life/.3f);
            float fade=1f-progress;
            Ring(effect.To,.18f+.55f*progress,new Color(1f,.88f,.55f,.75f*fade),1.6f);
            for(int i=0;i<7;i++)
            {
                float a=-2.6f+i*.68f;
                Vector2 offset=new Vector2(Mathf.Cos(a)*26f*progress,Mathf.Sin(a)*20f*progress+14f*progress*progress);
                float size=4.5f*fade+1.5f;
                Color chip=i%2==0?new Color(.95f,.82f,.48f,fade):new Color(.76f,.92f,1f,fade);
                Panel(new Rect(hit.x+offset.x-size/2,hit.y+offset.y-size/2,size,size),chip);
            }
        }

        void DrawMeteorEffect(Effect effect)
        {
            if(Event.current.type!=EventType.Repaint)return;
            Vector2 target=ScreenPos(effect.To);
            float seed=Mathf.Sin(effect.To.X*12.9898f+effect.To.Y*78.233f)*43758.5453f;
            float hash=seed-Mathf.Floor(seed);

            if(effect.Kind==4)
            {
                const float fallTime=.55f;
                float pulse=.5f+.5f*Mathf.Sin(clock*18f+hash*6f);
                GlowRing(effect.To,2.2f,new Color(.90f,.96f,1f,.42f+.28f*pulse),1.8f);
                float approach=1f-Mathf.Clamp01(effect.Life/fallTime);
                Ring(effect.To,Mathf.Lerp(2.1f,.28f,approach),new Color(1f,1f,1f,.35f+.55f*approach),1.5f);
                DrawStarFlare(target,7f+5f*pulse,1.5f,new Color(1f,1f,1f,.55f+.35f*pulse),new Color(.75f,.92f,1f,.24f));

                float skyHeight=Mathf.Max(target.y+120f,360f);
                float mainSlant=95f+(hash-.5f)*35f;

                // Companion shimmering white starlight streaks raining alongside the main star.
                for(int c=0;c<3;c++)
                {
                    float offsetPhase=effect.Life-.06f*(c-1);
                    if(offsetPhase>fallTime||offsetPhase<0)continue;
                    float cp=1f-Mathf.Clamp01(offsetPhase/fallTime);
                    float a=hash*6.283f+c*2.094f;
                    Vector2 subTarget=target+new Vector2(Mathf.Cos(a)*46f,Mathf.Sin(a)*32f);
                    DrawStarStreak(subTarget,cp,mainSlant*.92f,skyHeight*.95f,.52f+.14f*(c%2),.78f);
                }

                if(effect.Life>fallTime)return;
                DrawStarStreak(target,approach,mainSlant,skyHeight,1.08f,1f);
                return;
            }

            const float impactDuration=.48f;
            float progress=1f-Mathf.Clamp01(effect.Life/impactDuration);
            float fade=1f-progress;

            if(progress<.35f)
            {
                float flash=(1f-progress/.35f);
                float flashRadius=52f+38f*progress;
                Panel(new Rect(target.x-flashRadius,target.y-flashRadius,flashRadius*2,flashRadius*2),new Color(1f,1f,1f,.45f*flash));
            }

            GlowRing(effect.To,.25f+2.25f*progress,new Color(1f,1f,1f,.95f*fade),2.6f);
            Ring(effect.To,.55f+2.65f*progress,new Color(.74f,.92f,1f,.65f*fade),1.6f);
            DrawStarFlare(target,38f*(.45f+.95f*progress),3.2f*fade+.6f,new Color(1f,1f,1f,.98f*fade),new Color(.76f,.93f,1f,.48f*fade));

            for(int i=0;i<20;i++)
            {
                float a=i*(Mathf.PI*2f/20f)+hash*3.14f;
                float speed=(i%3==0?88f:i%3==1?64f:42f);
                Vector2 spark=target+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(10f+speed*progress);
                float size=(i%2==0?6.5f:4f)*fade+1.2f;
                Color c=i%3==0?new Color(1f,1f,1f,fade):new Color(.82f,.95f,1f,fade);
                Panel(new Rect(spark.x-size/2,spark.y-size/2,size,size),c);
                if(i%4==0&&fade>.25f)
                    DrawStarFlare(spark,8f*fade,1.2f,new Color(1f,1f,1f,.9f*fade),new Color(.76f,.93f,1f,.35f*fade));
            }
        }
    }
}
