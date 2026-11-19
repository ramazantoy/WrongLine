using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class UiFont
{
    private const string Chars = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~°·–—…‹›«»’‘“”çÇğĞıİöÖşŞüÜâÂîÎûÛ×";

    public static string Run()
    {
        AssetDatabase.Refresh();
        var a = Make("BarlowCondensed-ExtraBold", "Heading SDF");
        var b = Make("Barlow-SemiBold", "Body SDF");
        return a + " | " + b;
    }

    private static string Make(string file, string assetName)
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/UI/Fonts/" + file + ".ttf");
        var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        asset.name = assetName;
        var path = "Assets/Game/UI/Fonts/" + assetName + ".asset";
        AssetDatabase.CreateAsset(asset, path);
        asset.atlasTexture.name = assetName + " Atlas";
        AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
        asset.material.name = assetName + " Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        asset.TryAddCharacters(Chars, out var missing);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        return assetName + " missing=" + (missing ?? "");
    }
}
