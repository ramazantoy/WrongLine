using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using WrongLine.UI;

namespace WrongLine.UI.Editor
{
    /// <summary>Small builders for the UI screens: rects, brush images, localized text, buttons, sliders, toggles. Editor only.</summary>
    internal static class UiFactory
    {
        internal const string SPRITES = "Assets/Game/UI/Sprites/";
        internal const string TABLE = "UI";

        /// <summary>The screens' ground: plain rgb(42, 42, 42). Every text on it is plain white.</summary>
        internal static readonly Color Background = new Color32(42, 42, 42, 255);

        internal static readonly Color White = Color.white;

        internal static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SPRITES + name + ".png");

        internal static TMP_FontAsset Heading => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/UI/Fonts/Heading SDF.asset");

        internal static TMP_FontAsset Body => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/UI/Fonts/Body SDF.asset");

        internal static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        /// <summary>Puts a rect at <paramref name="pos"/> from <paramref name="anchor"/> (0..1 of the parent), centred on itself.</summary>
        internal static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            r.anchorMin = anchor;
            r.anchorMax = anchor;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        internal static Canvas Canvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        internal static Image Image(string name, Transform parent, string sprite, Color color, bool sliced = true, bool raycast = false)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            if (!string.IsNullOrEmpty(sprite))
            {
                image.sprite = Sprite(sprite);
                image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            }

            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>Text bound to a key of the UI table through a LocalizeStringEvent, so a language switch updates it.</summary>
        internal static TMP_Text LocalizedText(string name, Transform parent, string key, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var text = PlainText(name, parent, font, size, color, align);
            var evt = text.gameObject.AddComponent<LocalizeStringEvent>();
            evt.StringReference = new LocalizedString(TABLE, key);
            var setter = (UnityAction<string>)System.Delegate.CreateDelegate(typeof(UnityAction<string>), text,
                typeof(TMP_Text).GetProperty("text").GetSetMethod());
            UnityEventTools.AddPersistentListener(evt.OnUpdateString, setter);
            return text;
        }

        internal static TMP_Text PlainText(string name, Transform parent, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>A brush button: parchment brush bar with dark localized text, press feedback.</summary>
        internal static Button BrushButton(string name, Transform parent, string brush, Vector2 size, string key, float fontSize,
            bool darkText = true)
        {
            var image = Image(name, parent, brush, White, raycast: true);
            image.rectTransform.sizeDelta = size;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Tint(button);
            image.gameObject.AddComponent<BrushButton>();
            var label = LocalizedText("Label", image.transform, key, Heading, fontSize, darkText ? Background : White);
            Stretch(label.rectTransform);
            // The brush thins out on the right, so its weight sits left of the middle: the text follows it.
            label.rectTransform.offsetMax = new Vector2(-64f, 0f);
            return button;
        }

        /// <summary>An icon-only button: a brush icon, no bar.</summary>
        internal static Button IconButton(string name, Transform parent, string icon, Vector2 size, Color color)
        {
            var image = Image(name, parent, icon, color, sliced: false, raycast: true);
            image.rectTransform.sizeDelta = size;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Tint(button);
            image.gameObject.AddComponent<BrushButton>();
            return button;
        }

        private static void Tint(Button button)
        {
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        /// <summary>
        /// A 0–1 slider: one brush sprite twice, the track at half alpha and the fill at full alpha revealed by fillAmount
        /// (<see cref="SliderFill"/>), so the brush keeps its shape. Both span the range the knob's centre travels.
        /// </summary>
        internal static Slider Slider(string name, Transform parent, Vector2 size)
        {
            var root = Rect(name, parent);
            root.sizeDelta = size;
            var slider = root.gameObject.AddComponent<Slider>();
            float inset = size.y * 0.5f;

            var track = Image("Track", root, "Slider_Track", new Color(1f, 1f, 1f, 0.5f), sliced: false);
            Place(track.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - inset * 2f, 44f));
            var fill = Image("Fill", root, "Slider_Track", White, sliced: false);
            Place(fill.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - inset * 2f, 44f));
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            SetRef(root.gameObject.AddComponent<SliderFill>(), "_fill", fill);

            var slide = Rect("Handle Slide Area", root);
            Stretch(slide);
            slide.offsetMin = new Vector2(inset, 0f);
            slide.offsetMax = new Vector2(-inset, 0f);
            var knob = Image("Handle", slide, "Slider_Knob", White, sliced: false, raycast: true);
            knob.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            knob.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            knob.rectTransform.sizeDelta = new Vector2(size.y, 0f);

            slider.handleRect = knob.rectTransform;
            slider.targetGraphic = knob;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }

        internal static Toggle Toggle(string name, Transform parent, Vector2 size)
        {
            var box = Image(name, parent, "Toggle_Box", White, sliced: false, raycast: true);
            box.rectTransform.sizeDelta = size;
            var check = Image("Check", box.transform, "Toggle_Check", White, sliced: false);
            Stretch(check.rectTransform);
            var toggle = box.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = true;
            toggle.navigation = new Navigation { mode = Navigation.Mode.None };
            return toggle;
        }

        internal static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetRefs(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
