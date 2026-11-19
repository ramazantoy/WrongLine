using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace WrongLine.UI.Editor
{
    /// <summary>The UI table's keys that the screens use, with their English and Turkish text. Written by the screen builder.</summary>
    internal static class UiStrings
    {
        private static readonly (string key, string en, string tr)[] Entries =
        {
            ("menu.play", "PLAY", "OYNA"),
            ("menu.settings", "SETTINGS", "AYARLAR"),
            ("menu.quit", "QUIT", "ÇIKIŞ"),
            ("menu.best", "BEST", "EN İYİ"),
            ("loading.title", "LOADING", "YÜKLENİYOR"),
            ("menu.quit.title", "QUIT THE GAME?", "OYUNDAN ÇIKILSIN MI?"),
            ("menu.quit.yes", "QUIT", "ÇIK"),
            ("menu.quit.no", "STAY", "KAL"),
            ("settings.title", "SETTINGS", "AYARLAR"),
            ("settings.master", "Volume", "Ses"),
            ("settings.music", "Music", "Müzik"),
            ("settings.sfx", "Effects", "Efekt"),
            ("settings.haptics", "Vibration", "Titreşim"),
            ("settings.language", "Language", "Dil"),
            ("settings.back", "BACK", "GERİ"),
        };

        internal static void Write()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(UiFactory.TABLE);
            var en = collection.GetTable("en") as StringTable;
            var tr = collection.GetTable("tr") as StringTable;
            foreach (var (key, enText, trText) in Entries)
            {
                var shared = collection.SharedData;
                var entry = shared.GetEntry(key) ?? shared.AddKey(key);
                Set(en, entry.Id, enText);
                Set(tr, entry.Id, trText);
            }

            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(en);
            EditorUtility.SetDirty(tr);
            AssetDatabase.SaveAssetIfDirty(collection.SharedData);
            AssetDatabase.SaveAssetIfDirty(en);
            AssetDatabase.SaveAssetIfDirty(tr);
        }

        private static void Set(StringTable table, long id, string value)
        {
            var existing = table.GetEntry(id);
            if (existing == null)
            {
                table.AddEntry(id, value);
            }
            else
            {
                existing.Value = value;
            }
        }
    }
}
