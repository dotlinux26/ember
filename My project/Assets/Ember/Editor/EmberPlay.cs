using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ember.Editor
{
    [InitializeOnLoad]
    static class EmberPlay
    {
        const string ScenePath = "Assets/Scenes/Ember.unity";

        static EmberPlay()
        {
            EditorApplication.delayCall += SetStartScene;
        }

        static void SetStartScene()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (scene != null)
                EditorSceneManager.playModeStartScene = scene;
        }

        [MenuItem("Ember/Play Ember")]
        static void Play()
        {
            SetStartScene();
            if (EditorSceneManager.playModeStartScene == null)
            {
                Debug.LogError("Ember scene missing: " + ScenePath);
                return;
            }

            EditorApplication.isPlaying = true;
        }

        [MenuItem("Ember/Play Ember", true)]
        static bool CanPlay() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
