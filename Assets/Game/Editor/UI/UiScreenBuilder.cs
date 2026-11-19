using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WrongLine.Menu;
using static WrongLine.UI.Editor.UiFactory;

namespace WrongLine.UI.Editor
{
    /// <summary>
    /// Builds the screens from the brush sprites: main menu, settings, loading cover. Plain #2A2A2A ground, plain white text; the
    /// brush shows in the main button, the underlines, the dividers and the sliders. Rerunnable: each prefab is written over, the
    /// root prefab gets the loading cover, the menu scene its screens, and the UI table its keys. Menu: WrongLine/UI/Rebuild Screens.
    /// </summary>
    public static class UiScreenBuilder
    {
        private const string PREFABS = "Assets/Game/Prefabs/UI/";
        private const string ROOT_PREFAB = "Assets/Game/Prefabs/RootScope.prefab";
        private const string MENU_SCENE = "Assets/Game/Scenes/Menu.unity";

        // Every action button of the game is this size and this brush family.
        private const float ButtonWidth = 760f;
        private const float ButtonHeight = 170f;
        private const float ButtonGap = 36f;
        private const float ButtonFont = 84f;

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);
        private static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);

        [MenuItem("WrongLine/UI/Rebuild Screens")]
        public static void RebuildMenu() => Debug.Log(Rebuild());

        public static string Rebuild()
        {
            Directory.CreateDirectory(PREFABS);
            UiStrings.Write();
            SavePrefab(BuildSettings(), "SettingsScreen");
            SavePrefab(BuildMenu(), "MenuScreen");
            SavePrefab(BuildLoading(), "LoadingScreen");
            WireRoot();
            WireMenuScene();
            AssetDatabase.SaveAssets();
            return "UI screens rebuilt";
        }

        private static void SavePrefab(GameObject built, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(built, PREFABS + name + ".prefab");
            Object.DestroyImmediate(built);
        }

        /// <summary>A full-screen page in the ground colour; it also takes the taps that would reach what is behind it.</summary>
        private static RectTransform Page(string name, Transform parent)
        {
            var page = Rect(name, parent);
            Stretch(page);
            var ground = Image("Ground", page, null, Background, raycast: true);
            Stretch(ground.rectTransform);
            return page;
        }

        private static GameObject BuildMenu()
        {
            var canvas = Canvas("MenuScreen", 10);
            var screen = canvas.gameObject.AddComponent<MenuScreen>();

            var ground = Image("Ground", canvas.transform, null, Background, raycast: true);
            Stretch(ground.rectTransform);

            var logo = Image("Logo", canvas.transform, "Logo_WrongLine", White, sliced: false);
            Place(logo.rectTransform, Top, new Vector2(0f, -400f), new Vector2(920f, 198f));
            logo.preserveAspect = true;
            var logoGroup = logo.gameObject.AddComponent<CanvasGroup>();

            var best = Rect("BestRow", canvas.transform);
            Place(best, Top, new Vector2(0f, -600f), new Vector2(800f, 90f));
            var bestGroup = best.gameObject.AddComponent<CanvasGroup>();
            var row = best.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 24f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            LocalizedText("Label", best, "menu.best", Body, 50f, White);
            var bestValue = PlainText("Value", best, Heading, 76f, White);

            // The actions are one group: the same brush bar and size, stacked by a layout group.
            var buttons = Rect("Buttons", canvas.transform);
            Place(buttons, Center, new Vector2(0f, -260f), new Vector2(800f, 3 * ButtonHeight + 2 * ButtonGap));
            var stack = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.spacing = ButtonGap;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = false;
            stack.childForceExpandHeight = false;
            var play = ActionButton("Play", buttons, "Brush_Button_A", "menu.play");
            var settings = ActionButton("Settings", buttons, "Brush_Button_B", "menu.settings");
            var quit = ActionButton("Quit", buttons, "Brush_Button_C", "menu.quit");
            var playGroup = play.gameObject.AddComponent<CanvasGroup>();
            var settingsGroup = settings.gameObject.AddComponent<CanvasGroup>();
            var quitGroup = quit.gameObject.AddComponent<CanvasGroup>();

            var prompt = Page("QuitPrompt", canvas.transform);
            var title = LocalizedText("Title", prompt, "menu.quit.title", Heading, 84f, White);
            Place(title.rectTransform, Center, new Vector2(0f, 220f), new Vector2(940f, 140f));
            var yes = BrushButton("Yes", prompt, "Brush_Button_C", new Vector2(ButtonWidth, ButtonHeight), "menu.quit.yes", ButtonFont);
            Place((RectTransform)yes.transform, Center, new Vector2(0f, -10f), new Vector2(ButtonWidth, ButtonHeight));
            var no = BrushButton("No", prompt, "Brush_Button_A", new Vector2(ButtonWidth, ButtonHeight), "menu.quit.no", ButtonFont);
            Place((RectTransform)no.transform, Center, new Vector2(0f, -10f - ButtonHeight - ButtonGap), new Vector2(ButtonWidth, ButtonHeight));
            prompt.gameObject.SetActive(false);

            SetRef(screen, "_play", play);
            SetRef(screen, "_settings", settings);
            SetRef(screen, "_quit", quit);
            SetRef(screen, "_quitPrompt", prompt.gameObject);
            SetRef(screen, "_quitYes", yes);
            SetRef(screen, "_quitNo", no);
            SetRef(screen, "_bestRow", best.gameObject);
            SetRef(screen, "_bestValue", bestValue);
            SetRefs(screen, "_intro", new Object[] { logoGroup, bestGroup, playGroup, settingsGroup, quitGroup });
            return canvas.gameObject;
        }

        private static GameObject BuildSettings()
        {
            var canvas = Canvas("SettingsScreen", 50);
            var screen = canvas.gameObject.AddComponent<SettingsScreen>();

            var panel = Page("Panel", canvas.transform);

            var title = LocalizedText("Title", panel, "settings.title", Heading, 110f, White);
            Place(title.rectTransform, Top, new Vector2(0f, -230f), new Vector2(900f, 140f));
            var underline = Image("Underline", panel, "Brush_Underline", White);
            Place(underline.rectTransform, Top, new Vector2(0f, -330f), new Vector2(520f, 44f));

            float[] rows = { -520f, -690f, -860f, -1030f, -1200f };
            for (int i = 0; i < rows.Length - 1; i++)
            {
                Divider(panel, (rows[i] + rows[i + 1]) * 0.5f);
            }

            var master = SliderRow(panel, "settings.master", rows[0]);
            var music = SliderRow(panel, "settings.music", rows[1]);
            var sfx = SliderRow(panel, "settings.sfx", rows[2]);

            RowLabel(panel, "settings.haptics", rows[3]);
            var haptics = Toggle("Haptics", panel, new Vector2(96f, 96f));
            Place((RectTransform)haptics.transform, TopRight, new Vector2(-148f, rows[3]), new Vector2(96f, 96f));

            RowLabel(panel, "settings.language", rows[4]);
            var previous = IconButton("PreviousLanguage", panel, "Icon_ArrowLeft", new Vector2(96f, 96f), White);
            Place((RectTransform)previous.transform, TopRight, new Vector2(-560f, rows[4]), new Vector2(96f, 96f));
            var next = IconButton("NextLanguage", panel, "Icon_ArrowRight", new Vector2(96f, 96f), White);
            Place((RectTransform)next.transform, TopRight, new Vector2(-148f, rows[4]), new Vector2(96f, 96f));
            var languageName = PlainText("LanguageName", panel, Heading, 68f, White);
            Place(languageName.rectTransform, TopRight, new Vector2(-354f, rows[4]), new Vector2(300f, 90f));

            var back = BrushButton("Back", panel, "Brush_Button_B", new Vector2(ButtonWidth, ButtonHeight), "settings.back", ButtonFont);
            Place((RectTransform)back.transform, Bottom, new Vector2(0f, 230f), new Vector2(ButtonWidth, ButtonHeight));

            panel.gameObject.SetActive(false);
            SetRef(screen, "_panel", panel.gameObject);
            SetRef(screen, "_master", master);
            SetRef(screen, "_music", music);
            SetRef(screen, "_sfx", sfx);
            SetRef(screen, "_haptics", haptics);
            SetRef(screen, "_previousLanguage", previous);
            SetRef(screen, "_nextLanguage", next);
            SetRef(screen, "_languageName", languageName);
            SetRef(screen, "_back", back);
            return canvas.gameObject;
        }

        /// <summary>A brush action button sized by its layout group.</summary>
        private static Button ActionButton(string name, Transform parent, string brush, string key)
        {
            var button = BrushButton(name, parent, brush, new Vector2(ButtonWidth, ButtonHeight), key, ButtonFont);
            var element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = ButtonWidth;
            element.preferredHeight = ButtonHeight;
            return button;
        }

        private static void Divider(Transform parent, float y)
        {
            var line = Image("Divider", parent, "Brush_Divider", new Color(1f, 1f, 1f, 0.3f));
            Place(line.rectTransform, Top, new Vector2(0f, y), new Vector2(860f, 18f));
        }

        private static void RowLabel(Transform parent, string key, float y)
        {
            var label = LocalizedText("Label_" + key, parent, key, Body, 58f, White, TextAlignmentOptions.Left);
            Place(label.rectTransform, TopLeft, new Vector2(110f + 180f, y), new Vector2(360f, 90f));
        }

        private static Slider SliderRow(Transform parent, string key, float y)
        {
            RowLabel(parent, key, y);
            var slider = Slider("Slider_" + key, parent, new Vector2(500f, 80f));
            Place((RectTransform)slider.transform, TopRight, new Vector2(-110f - 250f, y), new Vector2(500f, 80f));
            return slider;
        }

        private static GameObject BuildLoading()
        {
            var canvas = Canvas("LoadingScreen", 1000);
            var screen = canvas.gameObject.AddComponent<LoadingScreen>();

            var root = Page("Root", canvas.transform);
            var group = root.gameObject.AddComponent<CanvasGroup>();

            // One road, filled over and over; the text sits below it.
            const float height = 870f;
            const float width = height * 768f / 736f;
            var road = RoadImage("RoadCenter", root, new Vector2(540f, -888f), new Vector2(width, height));

            var label = LocalizedText("Label", root, "loading.title", Heading, 76f, White);
            Place(label.rectTransform, TopLeft, new Vector2(540f, -1603f), new Vector2(900f, 120f));

            root.gameObject.SetActive(false);
            SetRef(screen, "_root", root.gameObject);
            SetRef(screen, "_group", group);
            SetRef(screen, "_road", road);
            return canvas.gameObject;
        }

        /// <summary>The loading road, filled from its bottom (the near end), empty at first.</summary>
        private static Image RoadImage(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var road = Image(name, parent, "Loading_Road", White, sliced: false);
            Place(road.rectTransform, TopLeft, pos, size);
            road.type = UnityEngine.UI.Image.Type.Filled;
            road.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
            road.fillOrigin = 0;
            road.fillAmount = 0f;
            return road;
        }

        /// <summary>Puts the loading cover under the root prefab and points the root scope at it.</summary>
        private static void WireRoot()
        {
            var contents = PrefabUtility.LoadPrefabContents(ROOT_PREFAB);
            try
            {
                var existing = contents.transform.Find("LoadingScreen");
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "LoadingScreen.prefab");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, contents.transform);
                var scope = contents.GetComponent<GameRootScope>();
                SetRef(scope, "_loadingScreen", instance.GetComponent<LoadingScreen>());
                PrefabUtility.SaveAsPrefabAsset(contents, ROOT_PREFAB);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>Replaces the menu scene's old screens with the new ones and points the scope at them.</summary>
        private static void WireMenuScene()
        {
            var scene = EditorSceneManager.OpenScene(MENU_SCENE, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "MenuScreen" || root.name == "SettingsScreen")
                {
                    Object.DestroyImmediate(root);
                }
            }

            var menu = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "MenuScreen.prefab"), scene);
            var settings = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + "SettingsScreen.prefab"), scene);
            var scope = Object.FindFirstObjectByType<MenuScope>();
            SetRef(scope, "_screen", menu.GetComponent<MenuScreen>());
            SetRef(scope, "_settings", settings.GetComponent<SettingsScreen>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
