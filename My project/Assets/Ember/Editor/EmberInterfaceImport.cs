using System;
using UnityEditor;
using UnityEngine;

namespace Ember.Editor
{
    sealed class EmberInterfaceImport : AssetPostprocessor
    {
        const string InterfaceFolder = "Assets/Resources/Ember/Interface/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(InterfaceFolder, StringComparison.Ordinal) ||
                !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return;

            var texture = (TextureImporter)assetImporter;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.mipmapEnabled = false;
            texture.npotScale = TextureImporterNPOTScale.None;
            texture.textureCompression = TextureImporterCompression.Uncompressed;
            texture.crunchedCompression = false;
        }
    }
}
