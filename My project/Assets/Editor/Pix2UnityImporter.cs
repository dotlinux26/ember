using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.U2D.Sprites;
#endif

namespace NOIRADIX.Editor
{
    /// <summary>
    /// Automatic Unity 2D Sprite Importer for NOIRADIX Studio (.pix2.json) exports.
    /// Slices multi-frame sprite sheets with exact padding/spacing bleed protection,
    /// sets sprite pivots, and automatically generates Unity .anim AnimationClips.
    /// </summary>
    public static class Pix2UnityImporter
    {
        [MenuItem("Tools/NOIRADIX/Import Pix2 Spritesheet JSON...")]
        public static void ImportPix2Menu()
        {
            string selectedPath = EditorUtility.OpenFilePanel("Select NOIRADIX Spritesheet JSON", "Assets", "json");
            if (string.IsNullOrEmpty(selectedPath)) return;

            ImportPix2Json(selectedPath);
        }

        [MenuItem("Assets/NOIRADIX/Import Spritesheet and Animations", false, 30)]
        public static void ImportSelectedAsset()
        {
            var selected = Selection.activeObject;
            if (selected == null) return;
            string path = AssetDatabase.GetAssetPath(selected);
            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("NOIRADIX Importer", "Please select a NOIRADIX .json spritesheet file.", "OK");
                return;
            }
            ImportPix2Json(Path.GetFullPath(path));
        }

        [MenuItem("Assets/NOIRADIX/Import Spritesheet and Animations", true)]
        public static bool ValidateImportSelectedAsset()
        {
            var selected = Selection.activeObject;
            if (selected == null) return false;
            string path = AssetDatabase.GetAssetPath(selected);
            return path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
        }

        public static void ImportPix2Json(string fullJsonPath)
        {
            try
            {
                if (!File.Exists(fullJsonPath))
                {
                    Debug.LogError($"[NOIRADIX] File not found: {fullJsonPath}");
                    return;
                }

                string jsonText = File.ReadAllText(fullJsonPath);
                Pix2Data data = JsonUtility.FromJson<Pix2Data>(jsonText);
                if (data == null || data.frames == null || data.frames.Length == 0)
                {
                    Debug.LogError("[NOIRADIX] Invalid or empty NOIRADIX JSON format.");
                    return;
                }

                string dir = Path.GetDirectoryName(fullJsonPath);
                string safeImageName = Path.GetFileName(data.meta.image);
                string pngPath = Path.Combine(dir, safeImageName);
                if (!File.Exists(pngPath))
                {
                    string altPng = Path.ChangeExtension(fullJsonPath, ".png");
                    if (File.Exists(altPng)) pngPath = altPng;
                    else
                    {
                        Debug.LogError($"[NOIRADIX] PNG image not found at: {pngPath}");
                        return;
                    }
                }

                string assetPngPath = ToAssetPath(pngPath);
                TextureImporter importer = AssetImporter.GetAtPath(assetPngPath) as TextureImporter;
                if (importer == null)
                {
                    AssetDatabase.ImportAsset(assetPngPath, ImportAssetOptions.ForceUpdate);
                    importer = AssetImporter.GetAtPath(assetPngPath) as TextureImporter;
                }

                if (importer == null)
                {
                    Debug.LogError($"[NOIRADIX] Failed to obtain TextureImporter for {assetPngPath}");
                    return;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = data.meta.cellWidth > 0 ? data.meta.cellWidth : 16;

                List<SpriteFrameMetadata> metas = new List<SpriteFrameMetadata>();
                int texH = data.meta.size.h;

                foreach (var frame in data.frames)
                {
                    int unityY = texH - frame.y - frame.h;
                    metas.Add(new SpriteFrameMetadata
                    {
                        name = frame.name,
                        rect = new Rect(frame.x, unityY, frame.w, frame.h),
                        pivot = new Vector2(frame.pivotX, frame.pivotY),
                    });
                }

#if UNITY_2021_1_OR_NEWER
                var factories = new SpriteDataProviderFactories();
                factories.Init();
                ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
                if (dataProvider == null)
                    throw new InvalidOperationException("Unity Sprite Data Provider is unavailable. Install or enable the 2D Sprite package.");

                dataProvider.InitSpriteEditorDataProvider();
                var existingRects = dataProvider.GetSpriteRects();
                Dictionary<string, GUID> existingIds = new Dictionary<string, GUID>(StringComparer.Ordinal);
                foreach (var existingRect in existingRects)
                {
                    if (!string.IsNullOrEmpty(existingRect.name) && !existingIds.ContainsKey(existingRect.name))
                        existingIds.Add(existingRect.name, existingRect.spriteID);
                }

                SpriteRect[] spriteRects = new SpriteRect[metas.Count];
                for (int i = 0; i < metas.Count; i++)
                {
                    SpriteFrameMetadata frame = metas[i];
                    spriteRects[i] = new SpriteRect
                    {
                        name = frame.name,
                        spriteID = existingIds.TryGetValue(frame.name, out GUID existingId) ? existingId : GUID.Generate(),
                        rect = frame.rect,
                        alignment = SpriteAlignment.Custom,
                        pivot = frame.pivot,
                    };
                }
                dataProvider.SetSpriteRects(spriteRects);

#if UNITY_2021_2_OR_NEWER
                ISpriteNameFileIdDataProvider nameIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
                if (nameIdProvider != null)
                {
                    List<SpriteNameFileIdPair> nameIdPairs = new List<SpriteNameFileIdPair>();
                    foreach (var spriteRect in spriteRects)
                        nameIdPairs.Add(new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID));
                    nameIdProvider.SetNameFileIdPairs(nameIdPairs);
                }
#endif
                dataProvider.Apply();
#else
                List<SpriteMetaData> legacyMetas = new List<SpriteMetaData>(metas.Count);
                foreach (SpriteFrameMetadata frame in metas)
                {
                    legacyMetas.Add(new SpriteMetaData
                    {
                        name = frame.name,
                        rect = frame.rect,
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = frame.pivot,
                    });
                }
                importer.spritesheet = legacyMetas.ToArray();
                EditorUtility.SetDirty(importer);
#endif
                importer.SaveAndReimport();

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                UnityEngine.Object[] loadedSprites = AssetDatabase.LoadAllAssetsAtPath(assetPngPath);
                Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>();
                foreach (var obj in loadedSprites)
                {
                    if (obj is Sprite s) spriteMap[s.name] = s;
                }

                int createdClips = 0;
                string animDir = Path.Combine(Path.GetDirectoryName(assetPngPath), "Animations").Replace('\\', '/');
                if (!Directory.Exists(animDir)) Directory.CreateDirectory(animDir);

                if (data.animations != null)
                {
                    foreach (var anim in data.animations)
                    {
                        if (anim.frameNames == null || anim.frameNames.Length == 0) continue;

                        AnimationClip clip = new AnimationClip();
                        clip.frameRate = anim.fps > 0 ? anim.fps : 6;

                        if (anim.loop)
                        {
                            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                            settings.loopTime = true;
                            AnimationUtility.SetAnimationClipSettings(clip, settings);
                        }

                        EditorCurveBinding binding = new EditorCurveBinding();
                        binding.type = typeof(SpriteRenderer);
                        binding.path = "";
                        binding.propertyName = "m_Sprite";

                        List<ObjectReferenceKeyframe> keyframes = new List<ObjectReferenceKeyframe>();
                        float frameDuration = 1f / clip.frameRate;

                        for (int i = 0; i < anim.frameNames.Length; i++)
                        {
                            string fName = anim.frameNames[i];
                            if (spriteMap.TryGetValue(fName, out Sprite sp))
                            {
                                ObjectReferenceKeyframe key = new ObjectReferenceKeyframe();
                                key.time = i * frameDuration;
                                key.value = sp;
                                keyframes.Add(key);
                            }
                        }

                        if (keyframes.Count > 0)
                        {
                            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());
                            string clipPath = $"{animDir}/{anim.clipName}.anim";
                            AssetDatabase.CreateAsset(clip, clipPath);
                            createdClips++;
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[NOIRADIX] Successfully imported '{Path.GetFileName(pngPath)}': Sliced {metas.Count} frames and created {createdClips} Animation Clips!");
                EditorUtility.DisplayDialog("NOIRADIX Importer", $"Import Complete!\n\nSliced: {metas.Count} frames\nClips Created: {createdClips}\nTarget: {assetPngPath}", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NOIRADIX] Import failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static string ToAssetPath(string fullPath)
        {
            string projectPath = Path.GetFullPath(Application.dataPath + "/..").Replace('\\', '/');
            string norm = fullPath.Replace('\\', '/');
            if (norm.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
            {
                return norm.Substring(projectPath.Length).TrimStart('/');
            }
            return fullPath;
        }

        [Serializable]
        public class Pix2Data
        {
            public Pix2Meta meta;
            public Pix2Frame[] frames;
            public Pix2Anim[] animations;
        }

        [Serializable]
        public class Pix2Meta
        {
            public string image;
            public int cellWidth;
            public int cellHeight;
            public Pix2Size size;
        }

        [Serializable]
        public class Pix2Size
        {
            public int w;
            public int h;
        }

        [Serializable]
        public class Pix2Frame
        {
            public string name;
            public int x;
            public int y;
            public int w;
            public int h;
            public float pivotX = 0.5f;
            public float pivotY = 0f;
        }

        private class SpriteFrameMetadata
        {
            public string name;
            public Rect rect;
            public Vector2 pivot;
        }

        [Serializable]
        public class Pix2Anim
        {
            public string clipName;
            public float fps = 6;
            public bool loop = true;
            public string[] frameNames;
        }
    }
}