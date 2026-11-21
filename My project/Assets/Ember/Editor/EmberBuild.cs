using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ember.Editor
{
    public static class EmberBuild
    {
        [MenuItem("Ember/Create or open Ember scene")]
        public static void Prepare()
        {
            const string materialPath="Assets/Resources/Ember/EmberSprites.mat";
            if(!File.Exists(materialPath))AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")),materialPath);
            var worldMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);worldMaterial.shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");EditorUtility.SetDirty(worldMaterial);
            if(!File.Exists("Assets/Resources/Ember/EmberOutline.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Ember/PixelOutline")),"Assets/Resources/Ember/EmberOutline.mat");
            if(!File.Exists("Assets/Resources/Ember/EmberUnlit.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")),"Assets/Resources/Ember/EmberUnlit.mat");
            const string path="Assets/Scenes/Ember.unity";
            if(!File.Exists("Assets/Resources/Ember/EmberAbyss.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Ember/AbyssFog")),"Assets/Resources/Ember/EmberAbyss.mat");
            if(!File.Exists(path))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Ember",typeof(EmberGame));EditorSceneManager.SaveScene(scene,path);
            }
            else EditorSceneManager.OpenScene(path);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};
            PlayerSettings.companyName="Ember";PlayerSettings.productName="Ember — Don't Trust The Torch";
            PlayerSettings.bundleVersion="0.3.0";
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=800;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;

            var iconImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/app_icon.png");
            if(iconImporter!=null)
            {
                iconImporter.textureType=TextureImporterType.Default;
                iconImporter.alphaIsTransparency=true;
                iconImporter.isReadable=true;
                iconImporter.SaveAndReimport();
            }
            var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/app_icon.png");
            if(icon!=null)
            {
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone,new[]{icon});
            }

            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Ember/Active","Assets/Resources/Ember/Props"}))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));importer.textureType=TextureImporterType.Default;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();Debug.Log("EMBER_PREPARE_PASS");
        }
        [MenuItem("Ember/Build Windows")]
        public static void Windows()
        {
            Prepare();
            string outDir="D:/Ember/Builds/Windows";
            Directory.CreateDirectory(outDir);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Ember.unity"},locationPathName=Path.Combine(outDir,"Ember.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Ember build failed: "+report.summary.result);
            Debug.Log("EMBER_BUILD_PASS "+report.summary.totalSize);
        }
    }
}
