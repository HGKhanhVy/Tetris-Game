using UnityEngine;

namespace BrickStacker
{
    // Vài sprite nhỏ vẽ bằng code cho tutorial (vòng loang, khung viền bo góc). Vẽ một lần rồi
    // dùng lại — không tạo texture mới mỗi lần hiện bước hướng dẫn.
    public static class TutorialSprites
    {
        static Sprite ring;
        static Sprite roundedFrame;
        static Sprite roundedPanel;
        static Sprite roundedGlow;

        const int FrameRadius = 18;   // panel và frame phải chung bán kính thì viền mới ôm đúng nền

        // Vòng tròn rỗng, dùng làm gợn sóng ở điểm chạm.
        public static Sprite Ring()
        {
            if (ring != null)
                return ring;

            const int size = 96;
            const float outer = 46f, inner = 34f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            ring = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return ring;
        }

        // Nền bo góc đặc, mép khử răng cưa. Sprite trắng nên màu do Image.color quyết định.
        public static Sprite RoundedPanel()
        {
            if (roundedPanel != null)
                return roundedPanel;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedRectDistance(x, y, size, FrameRadius);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)));
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            roundedPanel = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(FrameRadius, FrameRadius, FrameRadius, FrameRadius));
            return roundedPanel;
        }

        // Quầng sáng bo góc: một dải viền dày, mờ dần về hai phía để trông như đang phát sáng.
        // Hình được thụt vào trong texture đúng bằng bề dày quầng, nếu không nửa ngoài sẽ bị cắt cụt.
        public static Sprite RoundedGlow()
        {
            if (roundedGlow != null)
                return roundedGlow;

            const int size = 128;
            const int radius = 26;
            const float inset = 14f;
            const float glowWidth = 14f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedRectDistance(x, y, size, radius) + inset;
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d) / glowWidth);
                    a *= a;   // bình phương cho tâm quầng đậm, rìa tan mượt
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            const int border = (int)(inset + glowWidth) + radius;
            roundedGlow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            return roundedGlow;
        }

        // Khung viền bo góc (chỉ có viền, ruột trong suốt) để bao quanh vùng đang được chỉ tới.
        public static Sprite RoundedFrame()
        {
            if (roundedFrame != null)
                return roundedFrame;

            const int size = 64;
            const int radius = FrameRadius;
            const int thickness = 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dOuter = RoundedRectDistance(x, y, size, radius);
                    float dInner = RoundedRectDistance(x, y, size, radius) - thickness;
                    // dOuter <= 0 nằm trong hình; viền là dải mỏng sát mép.
                    float a = Mathf.Clamp01(-dOuter) * Mathf.Clamp01(dInner + 1f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            roundedFrame = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return roundedFrame;
        }

        // Khoảng cách có dấu tới mép hình chữ nhật bo góc (âm = bên trong).
        static float RoundedRectDistance(int x, int y, int size, int radius)
        {
            float half = size * 0.5f;
            float px = Mathf.Abs(x - (half - 0.5f));
            float py = Mathf.Abs(y - (half - 0.5f));
            float cornerX = half - radius;
            float cornerY = half - radius;
            float dx = Mathf.Max(px - cornerX, 0f);
            float dy = Mathf.Max(py - cornerY, 0f);
            float outside = Mathf.Sqrt(dx * dx + dy * dy) - radius;
            float insideX = px - half;
            float insideY = py - half;
            return dx > 0f || dy > 0f ? outside : Mathf.Max(insideX, insideY);
        }
    }
}
