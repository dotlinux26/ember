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

        void DrawMeteorEffect(Effect effect)
        {
            if(Event.current.type!=EventType.Repaint)return;
            Vector2 target=ScreenPos(effect.To);
            if(effect.Kind==4)
            {
                Ring(effect.To,2.2f,new Color(.79f,.59f,1,.45f+.25f*Mathf.Sin(clock*15)));
                const float fallTime=.60f;
                if(effect.Life>fallTime)return;
                float t=1-Mathf.Clamp01(effect.Life/fallTime);
                float height=Mathf.Max(target.y+85,280);
                Vector2 head=target-new Vector2(65,height)*(1-t*t);
                for(int i=8;i>=1;i--)
                {
                    float size=24-i*2;
                    Vector2 trail=head-new Vector2(i*3,i*13);
                    Panel(new Rect(trail.x-size/2,trail.y-size/2,size,size),new Color(1,.32f+i*.025f,.46f,(9-i)*.085f));
                }
                PixelArt(new Rect(head.x-19,head.y-19,38,38),"meteor");
                return;
            }

            float progress=1-Mathf.Clamp01(effect.Life/.3f);
            Ring(effect.To,.25f+2.05f*progress,new Color(1,.73f,.42f,1-progress));
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;
                Vector2 spark=target+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(8+56*progress);
                float size=2+6*(1-progress);
                Panel(new Rect(spark.x-size/2,spark.y-size/2,size,size),new Color(1,.66f+i%2*.22f,.38f,1-progress));
            }
        }
    }
}
