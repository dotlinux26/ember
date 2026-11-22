using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Ember
{
    public sealed partial class EmberGame
    {
        readonly Dictionary<string,Light2D> lights=new Dictionary<string,Light2D>();
        readonly HashSet<Light2D> activeLights=new HashSet<Light2D>();
        readonly Structure[] topLightStructures=new Structure[24];
        readonly float[] topLightStructureDists=new float[24];
        readonly TransientLight[] topTransientLights=new TransientLight[16];
        readonly float[] topTransientLightDists=new float[16];
        readonly Projectile[] topEnemyProjectiles=new Projectile[8];
        readonly float[] topEnemyProjectileDists=new float[8];
        readonly List<string> staleLightKeys=new List<string>();
        MaterialPropertyBlock outlineProperties,crystalProperties;
        Material outlineMaterial,emissionMaterial,crystalFillMaterial;
        void SetupLighting()
        {
            var ambient=new GameObject("Night ambient").AddComponent<Light2D>();ambient.lightType=Light2D.LightType.Global;ambient.targetSortingLayers=new[]{0};ambient.intensity=.12f;ambient.color=new Color(.48f,.55f,.8f);
            EnsureLightingMaterials();
        }
        void Illuminate(string key,V2 pos,float radius,Color color,float intensity)
        {
            if(!lights.TryGetValue(key,out var light)){light=new GameObject("Light / "+key).AddComponent<Light2D>();light.lightType=Light2D.LightType.Point;light.targetSortingLayers=new[]{0};lights[key]=light;}
            activeLights.Add(light);
            if(!light.enabled)light.enabled=true;light.transform.position=new Vector3(pos.X,pos.Y+.35f,0);light.pointLightOuterRadius=radius;light.pointLightInnerRadius=radius*.4f;light.falloffIntensity=.65f;light.color=color;light.intensity=intensity;
        }
        static Color EnemyOrbColor(int colorKind)
        {
            switch(colorKind)
            {
                case 1:return new Color(0.66f, 0.33f, 0.97f);
                case 2:return new Color(1f, 0.48f, 0.16f);
                case 3:return new Color(0.92f, 0.70f, 0.03f);
                case 4:return new Color(.68f,.46f,1f);
                case 5:return new Color(.38f,.95f,.54f);
                default:return new Color(1f,.22f,.32f);
            }
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
        static int InsertTopK<T>(T[] items,float[] dists,int count,int maxCount,T item,float dist)
        {
            if(count<maxCount)
            {
                int i=count;
                while(i>0&&dists[i-1]>dist){items[i]=items[i-1];dists[i]=dists[i-1];i--;}
                items[i]=item;dists[i]=dist;
                return count+1;
            }
            if(dist>=dists[maxCount-1])return count;
            int idx=maxCount-1;
            while(idx>0&&dists[idx-1]>dist){items[idx]=items[idx-1];dists[idx]=dists[idx-1];idx--;}
            items[idx]=item;dists[idx]=dist;
            return count;
        }
        void RenderLighting()
        {
            activeLights.Clear();
            var local=Local;
            var focus=CameraTarget();
            float beaconRadius=5.5f+(State.Charge/100f)*2.4f;
            if(State.Charge>0||State.BeaconHP>0)Illuminate("beacon",new V2(),beaconRadius,new Color(.72f,.95f,1f),1.15f);
            else Illuminate("beacon_dormant",new V2(),5.5f,new Color(.35f,.55f,.75f),.45f);
            if(State.BossArenaActive)
            {
                Color accent=BossAccentColor(State.BossType);
                Illuminate("bossArena",State.BossArenaCenter,State.BossArenaRadius+2.5f,Color.Lerp(accent,Color.white,.22f),.55f);
                Enemy boss=null;
                if(State.Enemies!=null)
                {
                    for(int i=0;i<State.Enemies.Length;i++)
                    {
                        var e=State.Enemies[i];
                        if(e!=null&&e.Id==State.BossId&&e.HP>0){boss=e;break;}
                    }
                }
                if(boss!=null)Illuminate("worldBoss",VisualEnemyPos(boss),5.5f,accent,.95f);
            }
            if(State.Players!=null)
            {
                for(int i=0;i<State.Players.Length;i++)
                {
                    var p=State.Players[i];
                    if(p.Extracted||p.Ghost)continue;
                    V2 pPos=VisualPlayerPos(p);
                    float flicker=local!=null&&!local.Ghost&&local.Id!=p.Id&&local.Dread>=60&&!local.Safe?.55f+.45f*Mathf.PerlinNoise(clock*7,p.Id):1;
                    if(p.HasHeldTorch&&(State.Fuel>0||State.LastLight>0))Illuminate("lamp"+p.Id,pPos,p.Lantern==Lantern.Relic?9:6,p.Lantern==Lantern.Relic?Cyan:p.Lantern==Lantern.Inferno?new Color(1,.43f,.16f):new Color(1,.78f,.48f),1.2f*flicker);
                    else if(p.Hat==(int)Hat.Candle)Illuminate("candle"+p.Id,pPos,1.5f,Gold,.7f);
                }
            }
            if(State.Structures!=null&&State.Structures.Length>0)
            {
                int structCount=0;
                for(int i=0;i<State.Structures.Length;i++)
                {
                    var s=State.Structures[i];
                    if(s==null||(s.Kind>=2&&s.Kind!=5))continue;
                    float dist=focus==null?0f:V2.Distance(s.Pos,focus.Pos);
                    structCount=InsertTopK(topLightStructures,topLightStructureDists,structCount,24,s,dist);
                }
                for(int i=0;i<structCount;i++)
                {
                    var s=topLightStructures[i];
                    Illuminate("structure"+s.Id,s.Pos,s.Kind==1?9:s.Kind==5?2:5,s.Kind==5?Cyan:Gold,s.Kind==5?.3f:1.1f);
                    topLightStructures[i]=null;
                }
            }
            if(State.Lights!=null&&State.Lights.Length>0)
            {
                int lightCount=0;
                for(int i=0;i<State.Lights.Length;i++)
                {
                    var l=State.Lights[i];
                    if(l==null)continue;
                    float dist=focus==null?0f:V2.Distance(l.Pos,focus.Pos);
                    lightCount=InsertTopK(topTransientLights,topTransientLightDists,lightCount,16,l,dist);
                }
                for(int i=0;i<lightCount;i++)
                {
                    var l=topTransientLights[i];
                    topTransientLights[i]=null;
                    if(l.Life<=0||l.MaxLife<=0)continue;
                    float elapsed=l.MaxLife-l.Life;
                    float fadeIn=Mathf.Clamp01(elapsed/.12f);
                    float fadeOut=Mathf.Clamp01(l.Life/Mathf.Min(.45f,l.MaxLife*.35f));
                    float envelope=fadeIn*fadeOut;
                    float radius=l.Radius*(.75f+.25f*envelope);
                    Illuminate("fxlight"+l.Id,l.Pos,radius,TransientLightColor(l.ColorKind),l.Intensity*envelope);
                }
            }
            if(State.Projectiles!=null&&State.Projectiles.Length>0)
            {
                V2 focusPos=focus!=null?VisualPlayerPos(focus):new V2();
                int projCount=0;
                for(int i=0;i<State.Projectiles.Length;i++)
                {
                    var shot=State.Projectiles[i];
                    if(shot==null||!shot.FromEnemy)continue;
                    float dist=V2.Distance(shot.Pos,focusPos);
                    projCount=InsertTopK(topEnemyProjectiles,topEnemyProjectileDists,projCount,8,shot,dist);
                }
                int slot=0;
                for(int i=0;i<projCount;i++)
                {
                    var shot=topEnemyProjectiles[i];
                    topEnemyProjectiles[i]=null;
                    V2 sPos=VisualProjectilePos(shot);
                    if(V2.Distance(sPos,focusPos)>20f)continue;
                    float radius=shot.ColorKind>=4?1.85f:2.15f;
                    Illuminate("eshot"+slot,sPos,radius,EnemyOrbColor(shot.ColorKind),.55f);
                    slot++;
                }
            }
            foreach(var light in lights.Values)
                if(light.enabled&&!activeLights.Contains(light))light.enabled=false;
            if(lights.Count>64)
            {
                staleLightKeys.Clear();
                foreach(var kv in lights)
                    if(kv.Value==null||!kv.Value.enabled)staleLightKeys.Add(kv.Key);
                for(int i=0;i<staleLightKeys.Count;i++)
                {
                    string key=staleLightKeys[i];
                    if(lights.TryGetValue(key,out var l)&&l!=null)Destroy(l.gameObject);
                    lights.Remove(key);
                }
            }
        }
        void Outline(SpriteRenderer renderer,Sprite sprite)
        {
            if(outlineMaterial==null)return;renderer.sharedMaterial=outlineMaterial;
            if(outlineProperties==null)outlineProperties=new MaterialPropertyBlock();
            var props=outlineProperties;Rect r=sprite.rect;var tex=sprite.texture;
            props.SetVector("_CellUV",new Vector4(r.xMin/tex.width,r.yMin/tex.height,r.xMax/tex.width,r.yMax/tex.height));renderer.SetPropertyBlock(props);
        }
        void EnsureLightingMaterials()
        {
            if(outlineMaterial==null||outlineMaterial.shader==null||!outlineMaterial.shader.isSupported)
                outlineMaterial=Resources.Load<Material>("Ember/EmberOutline");

            if(emissionMaterial==null||emissionMaterial.shader==null||!emissionMaterial.shader.isSupported)
            {
                emissionMaterial=Resources.Load<Material>("Ember/EmberUnlit");
                if(emissionMaterial==null||emissionMaterial.shader==null||!emissionMaterial.shader.isSupported)
                {
                    var s=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    if(s==null||!s.isSupported)s=Shader.Find("Sprites/Default");
                    if(s==null||!s.isSupported)s=Shader.Find("Unlit/Texture");
                    if(s!=null)emissionMaterial=new Material(s);
                }
            }

            if(crystalFillMaterial==null||crystalFillMaterial.shader==null||!crystalFillMaterial.shader.isSupported)
            {
                crystalFillMaterial=Resources.Load<Material>("Ember/EmberCrystalFill");
                if(crystalFillMaterial==null||crystalFillMaterial.shader==null||!crystalFillMaterial.shader.isSupported)
                {
                    var cShader=Shader.Find("Ember/CrystalFill");
                    if(cShader!=null&&cShader.isSupported)
                        crystalFillMaterial=new Material(cShader);
                    else if(emissionMaterial!=null)
                        crystalFillMaterial=emissionMaterial;
                }
            }
        }
        void ApplyCrystalFill(SpriteRenderer renderer,float fillLevel,float timeVal)
        {
            if(renderer==null)return;
            EnsureLightingMaterials();
            var mat=(crystalFillMaterial!=null&&crystalFillMaterial.shader!=null&&crystalFillMaterial.shader.isSupported)?crystalFillMaterial:emissionMaterial;
            if(mat==null)return;
            renderer.sharedMaterial=mat;
            if(mat.HasProperty("_FillLevel"))
            {
                if(crystalProperties==null)crystalProperties=new MaterialPropertyBlock();
                crystalProperties.SetFloat("_FillLevel",Mathf.Clamp01(fillLevel));
                crystalProperties.SetFloat("_TimeVal",timeVal);
                renderer.SetPropertyBlock(crystalProperties);
            }
            else
            {
                renderer.SetPropertyBlock(null);
            }
        }
    }
}
