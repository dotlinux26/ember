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
        void RenderLighting()
        {
            activeLights.Clear();
            var local=Local;
            if(State.BeaconHP>0&&State.Charge>=100)Illuminate("beacon",new V2(),7,new Color(.72f,.95f,1),1.1f);
            foreach(var p in State.Players)
            {
                if(p.Extracted)continue;
                if(p.Ghost){Illuminate("wisp"+p.Id,p.Pos,2,Cyan,.35f);continue;}
                float flicker=local!=null&&local.Id!=p.Id&&local.Dread>=60&&!local.Safe?.55f+.45f*Mathf.PerlinNoise(clock*7,p.Id):1;
                if(p.HasHeldTorch&&(State.Fuel>0||State.LastLight>0))Illuminate("lamp"+p.Id,p.Pos,p.Lantern==Lantern.Relic?9:6,p.Lantern==Lantern.Relic?Cyan:p.Lantern==Lantern.Inferno?new Color(1,.43f,.16f):new Color(1,.78f,.48f),1.2f*flicker);
                else if(p.Hat==(int)Hat.Candle)Illuminate("candle"+p.Id,p.Pos,1.5f,Gold,.7f);
            }
            foreach(var s in State.Structures.Where(s=>s.Kind<2||s.Kind==5).OrderBy(s=>local==null?0:V2.Distance(s.Pos,local.Pos)).Take(24))Illuminate("structure"+s.Id,s.Pos,s.Kind==1?9:s.Kind==5?2:5,s.Kind==5?Cyan:Gold,s.Kind==5?.3f:1.1f);
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
