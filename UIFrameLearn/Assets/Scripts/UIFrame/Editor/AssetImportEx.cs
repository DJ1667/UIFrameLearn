using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssetImportEx : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.Contains("AddressablesResources/Sprites/"))
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
        }
    }
}
