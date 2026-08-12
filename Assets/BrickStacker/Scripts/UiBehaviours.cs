using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrickStacker
{
    public class ResponsiveCanvasScaler : MonoBehaviour
    {
        CanvasScaler scaler;

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        void LateUpdate()
        {
            Apply();
        }

        void Apply()
        {
            if (scaler == null)
                return;

            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            if (scaler.referenceResolution.x <= 0f || scaler.referenceResolution.y <= 0f)
                scaler.referenceResolution = new Vector2(1920f, 1080f);

            float screenAspect = Screen.height > 0 ? Screen.width / (float)Screen.height : scaler.referenceResolution.x / scaler.referenceResolution.y;
            float referenceAspect = scaler.referenceResolution.x / scaler.referenceResolution.y;
            scaler.matchWidthOrHeight = screenAspect >= referenceAspect ? 1f : 0f;
        }
    }

    public class RectTransformSafeAreaFitter : MonoBehaviour
    {
        public float Padding = 24f;
        public float MaxScale = 1f;
        public bool UseOwnRectWhenLarge = true;

        RectTransform rect;
        RectTransform parentRect;
        Vector3 baseScale;
        int lastWidth;
        int lastHeight;
        Rect lastSafeArea;
        bool initialized;

        void Awake()
        {
            Initialize();
            ApplyNow();
        }

        void LateUpdate()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight && Screen.safeArea == lastSafeArea)
                return;
            ApplyNow();
        }

        void Initialize()
        {
            if (initialized)
                return;

            rect = transform as RectTransform;
            parentRect = rect != null ? rect.parent as RectTransform : null;
            baseScale = rect != null ? rect.localScale : transform.localScale;
            initialized = true;
        }

        public void ApplyNow()
        {
            Initialize();
            if (rect == null || parentRect == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Bounds contentBounds = CalculateContentBounds();
            if (contentBounds.size.x <= 1f || contentBounds.size.y <= 1f)
                return;

            Rect safe = Ui.SafeArea();
            Rect parent = parentRect.rect;
            float safeWidth = parent.width * Mathf.Clamp01(safe.width / Screen.width);
            float safeHeight = parent.height * Mathf.Clamp01(safe.height / Screen.height);
            float safeCenterX = parent.xMin + parent.width * Mathf.Clamp01((safe.xMin + safe.width * 0.5f) / Screen.width);
            float safeCenterY = parent.yMin + parent.height * Mathf.Clamp01((safe.yMin + safe.height * 0.5f) / Screen.height);

            float availableWidth = Mathf.Max(1f, safeWidth - Padding * 2f);
            float availableHeight = Mathf.Max(1f, safeHeight - Padding * 2f);
            float fit = Mathf.Min(availableWidth / contentBounds.size.x, availableHeight / contentBounds.size.y);
            float scale = Mathf.Min(MaxScale, fit);

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = baseScale * scale;
            rect.anchoredPosition = new Vector2(safeCenterX, safeCenterY) - (Vector2)contentBounds.center * scale;

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
        }

        Bounds CalculateContentBounds()
        {
            Rect ownRect = rect.rect;
            if (UseOwnRectWhenLarge && ownRect.width > 120f && ownRect.height > 120f)
                return new Bounds(ownRect.center, ownRect.size);

            var children = rect.GetComponentsInChildren<RectTransform>(true);
            bool hasBounds = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            var corners = new Vector3[4];

            for (int i = 0; i < children.Length; i++)
            {
                var child = children[i];
                if (child == null || child == rect || !child.gameObject.activeInHierarchy)
                    continue;

                child.GetWorldCorners(corners);
                for (int c = 0; c < corners.Length; c++)
                {
                    Vector3 local = rect.InverseTransformPoint(corners[c]);
                    if (!hasBounds)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }

            if (!hasBounds)
                bounds = new Bounds(ownRect.center, ownRect.size);

            return bounds;
        }
    }

    public class CameraSpriteFitter : MonoBehaviour
    {
        public Camera Target;
        public float Depth;
        public float Overscan = 2.18f;
        SpriteRenderer spriteRenderer;
        int lastWidth;
        int lastHeight;
        Vector3 lastCameraPosition;
        float lastCameraSize;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            Apply(true);
        }

        void LateUpdate()
        {
            Apply(false);
        }

        void Apply(bool force)
        {
            if (Target == null)
                Target = Camera.main ?? FindAnyObjectByType<Camera>();
            if (Target == null || spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            bool changed = force || Screen.width != lastWidth || Screen.height != lastHeight ||
                Target.transform.position != lastCameraPosition || !Mathf.Approximately(Target.orthographicSize, lastCameraSize);
            if (!changed)
                return;

            float height = Target.orthographic ? Target.orthographicSize * Overscan : 14f;
            float width = height * Mathf.Max(0.35f, Target.aspect);
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            transform.position = new Vector3(Target.transform.position.x, Target.transform.position.y, Depth);
            transform.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastCameraPosition = Target.transform.position;
            lastCameraSize = Target.orthographicSize;
        }
    }

    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rect;
        int lastWidth;
        int lastHeight;
        Rect lastSafeArea;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            Apply();
        }

        void LateUpdate()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight && Screen.safeArea == lastSafeArea)
                return;

            Apply();
        }

        void Apply()
        {
            if (rect == null)
                return;

            Ui.ApplySafeArea(rect);
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
        }
    }

    public class LevelNodePulse : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float Amount = 0.035f;
        public float Speed = 2.2f;
        public float PressedScale = 0.92f;
        RectTransform rect;
        Vector3 baseScale;
        bool pressed;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            baseScale = rect != null ? rect.localScale : transform.localScale;
        }

        void LateUpdate()
        {
            if (rect == null)
                rect = GetComponent<RectTransform>();
            if (rect == null)
                return;

            float scale = (1f + Mathf.Sin(Time.unscaledTime * Speed) * Amount) * (pressed ? PressedScale : 1f);
            rect.localScale = baseScale * scale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pressed = false;
        }
    }

    public class TacticalCellInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public BrickGameController Controller;
        public int X;
        public int Y;

        public void OnPointerDown(PointerEventData eventData)
        {
            Controller?.HandleTacticalPointerDown(X, Y, eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Controller?.HandleTacticalPointerUp(X, Y, eventData.position);
        }
    }

    public class PressScaleFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float PressedScale = 0.94f;
        // When set, scale this target instead of self (use to avoid shrinking the hit area).
        public Transform VisualTarget;

        // Scale phải nhân tương đối với scale gốc bắt tại lúc nhấn — node map level
        // có scale scene ≈ 2.26, ghi đè tuyệt đối làm node co vĩnh viễn khi bấm ô khóa.
        Vector3 baseScale;
        bool pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            var r = TargetRect();
            if (r == null)
                return;
            if (!pressed)
            {
                baseScale = r.localScale;
                pressed = true;
            }
            r.localScale = new Vector3(baseScale.x * PressedScale, baseScale.y * PressedScale, baseScale.z);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        void Release()
        {
            if (!pressed)
                return;
            pressed = false;
            var r = TargetRect();
            if (r != null)
                r.localScale = baseScale;
        }

        RectTransform TargetRect()
        {
            var t = VisualTarget != null ? VisualTarget : transform;
            return t as RectTransform;
        }
    }
}
