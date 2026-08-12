using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BrickStacker
{
    public static class Ui
    {
        public static Canvas CreateCanvas(string name)
        {
            EnsureEventSystem();
            var canvasObject = new GameObject(name);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            float screenAspect = Screen.height > 0 ? Screen.width / (float)Screen.height : scaler.referenceResolution.x / scaler.referenceResolution.y;
            float referenceAspect = scaler.referenceResolution.x / scaler.referenceResolution.y;
            scaler.matchWidthOrHeight = screenAspect >= referenceAspect ? 1f : 0f;
            canvasObject.AddComponent<ResponsiveCanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        public static GameObject Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = color;
            return go;
        }

        public static Text Text(Transform parent, string value, Font font, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(12, size);
            text.resizeTextMaxSize = size;
            return text;
        }

        public static Button Button(Transform parent, string label, Font font, int size, UnityEngine.Events.UnityAction action)
        {
            var go = Panel(parent, "Button", new Color(0.12f, 0.24f, 0.28f, 0.92f));
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(action);
            var colors = button.colors;
            colors.highlightedColor = new Color(0.25f, 0.65f, 0.72f);
            colors.pressedColor = new Color(0.12f, 0.9f, 0.7f);
            button.colors = colors;

            var text = Text(go.transform, label, font, size, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.gameObject);
            return button;
        }

        public static void Rect(Component component, Vector2 min, Vector2 max, Vector2 size)
        {
            Rect(component.gameObject, min, max, size);
        }

        public static void Rect(GameObject go, Vector2 min, Vector2 max, Vector2 size)
        {
            var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        public static void Stretch(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Screen.safeArea can be stale in the Editor when the Game View size changes
        // (it may still describe the previous resolution). On devices the safe area is
        // always consistent with the screen, so implausible values are treated as
        // full-screen instead of blindly trusted.
        public static Rect SafeArea()
        {
            Rect safe = Screen.safeArea;
            float w = Mathf.Max(1, Screen.width);
            float h = Mathf.Max(1, Screen.height);
            // Real devices only inset one axis at a time (top/bottom in portrait,
            // left/right in landscape), so the safe area spans the full screen on at
            // least one axis. Editor game views often report the raw panel pixel size
            // instead, which shrinks both axes — reject those too.
            bool valid = safe.width > 0f && safe.height > 0f
                && safe.xMin >= 0f && safe.yMin >= 0f
                && safe.xMax <= w + 0.5f && safe.yMax <= h + 0.5f
                && safe.width >= w * 0.7f && safe.height >= h * 0.7f
                && (Mathf.Abs(safe.width - w) <= 1f || Mathf.Abs(safe.height - h) <= 1f);
            return valid ? safe : new Rect(0f, 0f, w, h);
        }

        public static void ApplySafeArea(RectTransform rect)
        {
            Rect safe = SafeArea();
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Mathf.Max(1, Screen.width);
            min.y /= Mathf.Max(1, Screen.height);
            max.x /= Mathf.Max(1, Screen.width);
            max.y /= Mathf.Max(1, Screen.height);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
