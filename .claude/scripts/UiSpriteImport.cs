using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class UiImport
{
    public static string Run()
    {
        AssetDatabase.Refresh();
        // 9-slice borders (left, bottom, right, top) in source pixels: bars stretch in the middle only.
        var borders = new Dictionary<string, Vector4>
        {
            { "Brush_Button_A", new Vector4(60, 0, 60, 0) }, { "Brush_Button_B", new Vector4(60, 0, 60, 0) },
            { "Brush_Button_C", new Vector4(60, 0, 60, 0) }, { "Brush_Underline", new Vector4(40, 0, 40, 0) },
            { "Brush_Divider", new Vector4(30, 0, 30, 0) }, { "Brush_Panel", new Vector4(140, 140, 140, 140) },
            { "Slider_Track", new Vector4(24, 0, 24, 0) }, { "Slider_Fill", new Vector4(24, 0, 24, 0) },
        };
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Game/UI/Sprites" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.sRGBTexture = true;
            imp.spritePixelsPerUnit = 100;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.maxTextureSize = 2048;
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            imp.spriteBorder = borders.TryGetValue(name, out var b) ? b : Vector4.zero;
            imp.SaveAndReimport();
            n++;
        }
        return n + " sprites imported";
    }
}
