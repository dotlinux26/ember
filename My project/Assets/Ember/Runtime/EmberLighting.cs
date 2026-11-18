using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Ember
{
    public sealed partial class EmberGame
    {
        readonly Dictionary<string,Light2D> lights=new Dictionary<string,Light2D>();
        readonly HashSet<Light2D> activeLights=new HashSet<Light2D>();
        MaterialPropertyBlock outlineProperties;
        Material outlineMaterial,emissionMaterial;
        void SetupLighting()
        {
            var ambient=new GameObject("Night ambient").AddComponent<Light2D>();ambient.lightType=Light2D.LightType.Global;ambient.targetSortingLayers=new[]{0};ambient.intensity=.12f;ambient.color=new Color(.48f,.55f,.8f);
            outlineMaterial=Resources.Load<Material>("Ember/EmberOutline");emissionMaterial=Resources.Load<Material>("Ember/EmberUnlit");
        }
        void Illuminate(string key,V2 pos,float radius,Color color,float intensity)
        {
            if(!lights.TryGetValue(key,out var light)){light=new GameObject("Light / "+key).AddComponent<Light2D>();light.lightType=Light2D.LightType.Point;light.targetSortingLayers=new[]{0};lights[key]=light;}
            activeLights.Add(light);
            if(!light.enabled)light.enabled=true;light.transform.position=new Vector3(pos.X,pos.Y+.35f,0);light.pointLightOuterRadius=radius;light.pointLightInnerRadius=radius*.4f;light.falloffIntensity=.65f;light.color=color;light.intensity=intensity;
        }
        static Color TransientLightColor(LightColorKind kind)
        {
            switch(kind)
            {
                case LightColorKind.Starlight:return new Color(.84f,.95f,1f);
                case LightColorKind.Moonlight:return new Color(.56f,.94f,.96f);
                case LightColorKind.Flash:return new Color(1f,.72f,.32f);
                case LightColorKind.RuneGold:return new Color(1f,.84f,.42f);
                case LightColorKind.VoidPurple:return new Color(.74f,.46f,1f);
                default:return new Color(1f,.78f,.48f);
            }
        }
        void RenderLighting()
        {
            activeLights.Clear();
            var local=Local;
            var focus=CameraTarget();
            if(State.BeaconHP>0&&State.Charge>=State.ChargeTarget)Illuminate("beacon",new V2(),7,new Color(.72f,.95f,1),1.1f);
            if(State.BossArenaActive)
            {
                Color accent=BossAccentColor(State.BossType);
                Illuminate("bossArena",State.BossArenaCenter,State.BossArenaRadius+2.5f,Color.Lerp(accent,Color.white,.22f),.55f);
                var boss=State.Enemies?.FirstOrDefault(e=>e.Id==State.BossId&&e.HP>0);
                if(boss!=null)Illuminate("worldBoss",boss.Pos,5.5f,accent,.95f);
            }
            foreach(var p in State.Players)
            {
                if(p.Extracted||p.Ghost)continue;
                V2 pPos=VisualPlayerPos(p);
                float flicker=local!=null&&!local.Ghost&&local.Id!=p.Id&&local.Dread>=60&&!local.Safe?.55f+.45f*Mathf.PerlinNoise(clock*7,p.Id):1;
                if(p.HasHeldTorch&&(State.Fuel>0||State.LastLight>0))Illuminate("lamp"+p.Id,pPos,p.Lantern==Lantern.Relic?9:6,p.Lantern==Lantern.Relic?Cyan:p.Lantern==Lantern.Inferno?new Color(1,.43f,.16f):new Color(1,.78f,.48f),1.2f*flicker);
                else if(p.Hat==(int)Hat.Candle)Illuminate("candle"+p.Id,pPos,1.5f,Gold,.7f);
            }
            foreach(var s in State.Structures.Where(s=>s.Kind<2||s.Kind==5).OrderBy(s=>focus==null?0:V2.Distance(s.Pos,focus.Pos)).Take(24))Illuminate("structure"+s.Id,s.Pos,s.Kind==1?9:s.Kind==5?2:5,s.Kind==5?Cyan:Gold,s.Kind==5?.3f:1.1f);
            foreach(var l in (State.Lights??System.Array.Empty<TransientLight>()).OrderBy(l=>focus==null?0:V2.Distance(l.Pos,focus.Pos)).Take(16))
            {
                if(l.Life<=0||l.MaxLife<=0)continue;
                float elapsed=l.MaxLife-l.Life;
                float fadeIn=Mathf.Clamp01(elapsed/.12f);
                float fadeOut=Mathf.Clamp01(l.Life/Mathf.Min(.45f,l.MaxLife*.35f));
                float envelope=fadeIn*fadeOut;
                float radius=l.Radius*(.75f+.25f*envelope);
                Illuminate("fxlight"+l.Id,l.Pos,radius,TransientLightColor(l.ColorKind),l.Intensity*envelope);
            }
            foreach(var light in lights.Values)
                if(light.enabled&&!activeLights.Contains(light))light.enabled=false;
            if(lights.Count>64)foreach(var key in lights.Where(p=>!p.Value.enabled).Select(p=>p.Key).ToArray()){Destroy(lights[key].gameObject);lights.Remove(key);}
        }
        void Outline(SpriteRenderer renderer,Sprite sprite)
        {
            if(outlineMaterial==null)return;renderer.sharedMaterial=outlineMaterial;
            if(outlineProperties==null)outlineProperties=new MaterialPropertyBlock();
            var props=outlineProperties;Rect r=sprite.rect;var tex=sprite.texture;
            props.SetVector("_CellUV",new Vector4(r.xMin/tex.width,r.yMin/tex.height,r.xMax/tex.width,r.yMax/tex.height));renderer.SetPropertyBlock(props);
        }
    }
}
