using System;
using UnityEngine;

namespace BrickStacker
{
    public static class RuntimeArt
    {
        public static readonly Color GridColor = new Color(0.20f, 0.12f, 0.065f, 0.92f);
        public static readonly Color SpecialBlockColor = new Color(1f, 0.92f, 0.34f, 1f);
        static Sprite boardFrameSprite;
        static Sprite roundedWoodSprite;
        static Sprite woodBackdropSprite;
        static Sprite blurredWoodBackdropSprite;
        static Sprite pauseButtonSprite;
        static Sprite rotateButtonSprite;
        static Sprite tacticalCellSprite;
        static Sprite tacticalHighlightSprite;
        static Sprite tacticalWallSprite;
        static Sprite tacticalPlayerSprite;
        static Sprite tacticalEnemySprite;
        static Sprite tacticalMonsterSprite;

        public static void ResetTacticalSpriteCache()
        {
            tacticalPlayerSprite  = null;
            tacticalEnemySprite   = null;
            tacticalMonsterSprite = null;
        }
        static Sprite woodPanelSprite;
        static Sprite woodButtonSprite;
        static Sprite solidSprite;
        static Font displayFont;
        static Font uiFont;
        static Font menuButtonFont;
        static AudioSource oneShotSource;
        static AudioClip uiSwitchClip;
        static AudioClip gameOverClip;

        public static void PlayUiSwitchSound()
        {
            if (uiSwitchClip == null)
                uiSwitchClip = Resources.Load<AudioClip>("BrickStacker/ui_switch");
            PrepareAudioClip(uiSwitchClip, "Resources/BrickStacker/ui_switch");
            PlayGlobalClip(uiSwitchClip, 0.42f);
        }

        public static void PlayGameOverSound()
        {
            if (gameOverClip == null)
                gameOverClip = Resources.Load<AudioClip>("BrickStacker/game_over_negative");
            PrepareAudioClip(gameOverClip, "Resources/BrickStacker/game_over_negative");
            PlayGlobalClip(gameOverClip, 0.70f);
        }

        static void PrepareAudioClip(AudioClip clip, string path)
        {
            if (clip == null)
            {
                Debug.LogWarning("BLOCKFALL audio missing: " + path);
                return;
            }

            if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
        }

        static void PlayGlobalClip(AudioClip clip, float volume)
        {
            if (clip == null)
                return;

            if (oneShotSource == null)
            {
                var audioObject = new GameObject("Blockfall One Shot Audio");
                UnityEngine.Object.DontDestroyOnLoad(audioObject);
                oneShotSource = audioObject.AddComponent<AudioSource>();
                oneShotSource.playOnAwake = false;
                oneShotSource.spatialBlend = 0f;
            }

            oneShotSource.PlayOneShot(clip, volume);
        }

        public static Font LoadDisplayFont()
        {
            if (displayFont != null)
                return displayFont;

            displayFont = Resources.Load<Font>("BrickStacker/Batangas_Bold");
            if (displayFont != null)
                return displayFont;

            displayFont = Resources.Load<Font>("BrickStacker/DFVN_Moju_Light");
            if (displayFont != null)
                return displayFont;

            displayFont = Resources.Load<Font>("BrickStacker/VietnameseArial");
            if (displayFont != null)
                return displayFont;

            displayFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (displayFont != null)
                return displayFont;

            return displayFont;
        }

        // Font riêng cho 4 nút chính màn menu (Paytone One). Fallback về UI font nếu thiếu.
        public static Font LoadMenuButtonFont()
        {
            if (menuButtonFont != null)
                return menuButtonFont;

            menuButtonFont = Resources.Load<Font>("BrickStacker/BTDanta-Bold");
            if (menuButtonFont != null)
                return menuButtonFont;

            return LoadUiFont();
        }

        public static Font LoadUiFont()
        {
            if (uiFont != null)
                return uiFont;

            uiFont = Resources.Load<Font>("BrickStacker/DFVN_Moju_Light");
            if (uiFont != null)
                return uiFont;

            uiFont = Resources.Load<Font>("BrickStacker/VietnameseArial");
            if (uiFont != null)
                return uiFont;

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont != null)
                return uiFont;

            return LoadDisplayFont();
        }

        public static Sprite[] LoadPieceBlockSprites()
        {
            var sprites = new Sprite[7];
            for (int i = 0; i < sprites.Length; i++)
            {
                string assetName = "BrickStacker/SlicedAssets/block_pieces_" + (i + 1).ToString("00");
                sprites[i] = Resources.Load<Sprite>(assetName);
                if (sprites[i] == null)
                {
                    var texture = Resources.Load<Texture2D>(assetName);
                    if (texture != null)
                    {
                        texture.filterMode = FilterMode.Bilinear;
                        texture.wrapMode = TextureWrapMode.Clamp;
                        sprites[i] = CreateFullRectSpriteSafe(texture);
                    }
                }
            }
            return sprites;
        }

        // GD v3: 4 ô tài nguyên lẻ, đánh chỉ số theo ResourceType (Move, Attack, Shield, Energy).
        // Tự cắt lề trong suốt (trim) để phần khối lấp đầy ô đều nhau giữa 4 file (art có lề khác nhau).
        public static Sprite[] LoadResourceSprites()
        {
            string[] names =
            {
                "Assets-v3.0/block-tainguyen/block-dichuyen",  // Move
                "Assets-v3.0/block-tainguyen/block-kiem",      // Attack
                "Assets-v3.0/block-tainguyen/block-khien",     // Shield
                "Assets-v3.0/block-tainguyen/block-nangluong"  // Energy
            };

            var sprites = new Sprite[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var texture = Resources.Load<Texture2D>(names[i]);
                if (texture != null)
                {
                    var trimmed = CreateTrimmedSpriteSafe(texture);
                    if (trimmed != null)
                    {
                        sprites[i] = trimmed;
                        continue;
                    }
                }

                // Fallback: sprite nguyên khung nếu texture không đọc được pixel.
                sprites[i] = Resources.Load<Sprite>(names[i]) ?? CreateFullRectSpriteSafe(texture);
            }
            return sprites;
        }

        // Cắt sprite theo hộp bao các pixel không trong suốt để phần vẽ lấp đầy, đều giữa các icon.
        static Sprite CreateTrimmedSpriteSafe(Texture2D texture)
        {
            if (texture == null)
                return null;

            try
            {
                var pixels = texture.GetPixels32();
                int w = texture.width;
                int h = texture.height;
                int minX = w, minY = h, maxX = -1, maxY = -1;
                const byte alphaThreshold = 10;

                for (int y = 0; y < h; y++)
                {
                    int row = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        if (pixels[row + x].a > alphaThreshold)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                if (maxX < minX || maxY < minY)
                    return CreateFullRectSpriteSafe(texture); // ảnh trống → dùng nguyên khung

                var rect = new Rect(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            catch (Exception)
            {
                return CreateFullRectSpriteSafe(texture);
            }
        }

        static Sprite CreateFullRectSpriteSafe(Texture2D texture)
        {
            if (texture == null)
                return null;

            try
            {
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Sprite CreateBlockSprite()
        {
            const int size = 40;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    float edgeDistance = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                    float cornerDistance = Mathf.Min(
                        Vector2.Distance(new Vector2(x, y), new Vector2(4f, 4f)),
                        Mathf.Min(
                            Vector2.Distance(new Vector2(x, y), new Vector2(size - 5f, 4f)),
                            Mathf.Min(
                                Vector2.Distance(new Vector2(x, y), new Vector2(4f, size - 5f)),
                                Vector2.Distance(new Vector2(x, y), new Vector2(size - 5f, size - 5f)))));

                    bool roundedCorner = (x < 5 || x > size - 6) && (y < 5 || y > size - 6) && cornerDistance > 5.4f;
                    if (roundedCorner)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float bevel = Mathf.Clamp01(edgeDistance / 7f);
                    float topLeftLight = Mathf.Clamp01((1f - u) * 0.42f + v * 0.34f);
                    float bottomRightShade = Mathf.Clamp01(u * 0.32f + (1f - v) * 0.38f);
                    float fineGrain = (Mathf.PerlinNoise(x * 0.17f, y * 0.19f) - 0.5f) * 0.055f;
                    float softStreak = Mathf.Sin((x * 0.28f + y * 0.11f) + Mathf.PerlinNoise(y * 0.05f, x * 0.04f) * 1.4f) * 0.018f;
                    float centerGlow = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.42f, 0.58f)) * 1.65f) * 0.08f;
                    float value = 0.86f + fineGrain + softStreak + centerGlow + topLeftLight * 0.10f - bottomRightShade * 0.08f;

                    if (edgeDistance < 1.5f)
                        value = 0.34f;
                    else if (edgeDistance < 3.0f)
                        value = Mathf.Lerp(0.42f, value, 0.38f);
                    else
                        value = Mathf.Lerp(0.55f, value, bevel);

                    if (x > 6 && x < size - 7 && y > size - 11 && y < size - 5)
                        value = Mathf.Lerp(value, 1f, 0.16f);

                    Color color = new Color(Mathf.Clamp01(value), Mathf.Clamp01(value), Mathf.Clamp01(value), 1f);

                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // Vòng nhẫn (annulus) trắng dùng cho đồng hồ đếm ngược: đặt Image type = Filled,
        // fillMethod = Radial360 rồi tăng fillAmount -> đầy vòng = hết giờ. Tô trắng để
        // đổi màu qua Image.color (vàng khi còn nhiều, đỏ khi sắp hết) mà không tạo texture mới.
        public static Sprite CreateTimerRingSprite()
        {
            const int size = 128;
            const float outer = 63f;   // bán kính ngoài (px) -> mép ngoài rãnh navy
            const float inner = 40f;   // bán kính trong -> dày ~23px, khít bề rộng rãnh
            const float aa = 1.4f;     // dải khử răng cưa
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    // Alpha = giao của "trong bán kính ngoài" và "ngoài bán kính trong".
                    float outerA = Mathf.Clamp01((outer - dist) / aa);
                    float innerA = Mathf.Clamp01((dist - inner) / aa);
                    float alpha = Mathf.Min(outerA, innerA);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // Load sprite từ thư mục Assets-v3.0.
        // Editor: đọc trực tiếp từ đĩa qua Application.dataPath.
        // Build runtime: cần copy file vào Assets/Resources/Assets-v3.0/ trước khi build.
        // Cache sprite v3.0 theo đường dẫn: mỗi ảnh chỉ nạp texture MỘT lần, tránh nạp lại
        // nhiều lần (vd 10 dòng bảng xếp hạng) gây áp lực bộ nhớ khiến texture nạp lỗi -> trắng.
        static readonly System.Collections.Generic.Dictionary<string, Sprite> v3SpriteCache = new System.Collections.Generic.Dictionary<string, Sprite>();

        public static Sprite LoadV3Sprite(string relativePath)
        {
            if (v3SpriteCache.TryGetValue(relativePath, out var cached) && cached != null)
                return cached;

            Sprite result = null;
            string resourceKey = "Assets-v3.0/" + relativePath.Replace(".png", "");
            var tex = Resources.Load<Texture2D>(resourceKey);
            if (tex != null)
                result = CreateFullRectSpriteSafe(tex);

#if UNITY_EDITOR
            if (result == null)
            {
                string fullPath = Application.dataPath + "/Assets-v3.0/" + relativePath;
                if (System.IO.File.Exists(fullPath))
                {
                    var bytes = System.IO.File.ReadAllBytes(fullPath);
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (texture.LoadImage(bytes))
                    {
                        texture.filterMode = FilterMode.Bilinear;
                        texture.wrapMode = TextureWrapMode.Clamp;
                        result = CreateFullRectSpriteSafe(texture);
                    }
                }
            }
#endif
            if (result != null)
                v3SpriteCache[relativePath] = result;
            return result;
        }

        // Cắt một vùng (theo tọa độ chuẩn hóa 0..1, gốc dưới-trái) từ ảnh v3.0 thành sprite riêng.
        // Dùng cho landscape: tách logo từ bg-menu.png dọc thay vì cần ảnh ngang mới.
        public static Sprite LoadV3SubSprite(string relativePath, Rect normalizedRect)
        {
            var full = LoadV3Sprite(relativePath);
            if (full == null)
                return null;

            var tex = full.texture;
            var pixelRect = new Rect(
                Mathf.Clamp(normalizedRect.x * tex.width, 0, tex.width - 1),
                Mathf.Clamp(normalizedRect.y * tex.height, 0, tex.height - 1),
                Mathf.Clamp(normalizedRect.width * tex.width, 1, tex.width),
                Mathf.Clamp(normalizedRect.height * tex.height, 1, tex.height));
            if (pixelRect.xMax > tex.width) pixelRect.width = tex.width - pixelRect.x;
            if (pixelRect.yMax > tex.height) pixelRect.height = tex.height - pixelRect.y;
            return Sprite.Create(tex, pixelRect, new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite CreatePauseButtonSprite()
        {
            if (pauseButtonSprite != null)
                return pauseButtonSprite;

            pauseButtonSprite = CreateSpriteFromAtlas("BrickStacker/ui_wood_buttons_atlas", 0.105f, 0.160f, 0.165f, 0.255f, 100f);
            if (pauseButtonSprite != null)
                return pauseButtonSprite;

            const int size = 112;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.47f;
            float innerRadius = size * 0.34f;
            var woodData = Resources.Load<TextAsset>("BrickStacker/wood_background_source");
            Texture2D woodTexture = null;
            if (woodData != null)
            {
                woodTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!woodTexture.LoadImage(woodData.bytes))
                    woodTexture = null;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float distance = Vector2.Distance(p, center);
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float t = distance / radius;
                    Color wood = woodTexture != null
                        ? woodTexture.GetPixelBilinear(0.30f + x / (float)size * 0.34f, 0.18f + y / (float)size * 0.34f)
                        : new Color(0.58f, 0.31f, 0.13f, 1f);
                    float grain = Mathf.PerlinNoise(x * 0.070f, y * 0.020f) * 0.10f;
                    float stripe = Mathf.Sin((x + y * 0.18f) * 0.18f) * 0.035f;
                    Color color = Color.Lerp(wood, new Color(0.26f, 0.11f, 0.040f, 1f), 0.34f + t * 0.16f);
                    color = Color.Lerp(color, new Color(0.76f, 0.45f, 0.20f, 1f), 0.22f);
                    color += new Color(grain + stripe, (grain + stripe) * 0.50f, (grain + stripe) * 0.22f, 0f);

                    if (distance > radius - 7f)
                        color = Color.Lerp(color, new Color(0.035f, 0.012f, 0.004f, 1f), 0.96f);
                    else if (distance > radius - 13f)
                        color = Color.Lerp(color, new Color(0.18f, 0.070f, 0.022f, 1f), 0.72f);
                    else if (Mathf.Abs(distance - innerRadius) < 3.2f)
                    {
                        float ringLight = Mathf.Clamp01(1f - distance / radius);
                        Color ringColor = Color.Lerp(new Color(0.18f, 0.070f, 0.024f, 1f), new Color(0.74f, 0.47f, 0.24f, 1f), ringLight);
                        color = Color.Lerp(color, ringColor, 0.62f);
                    }

                    float highlight = Mathf.Clamp01(1f - Vector2.Distance(p, center + new Vector2(-18f, 20f)) / 54f);
                    color = Color.Lerp(color, new Color(1f, 0.78f, 0.42f, 1f), highlight * 0.28f);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            pauseButtonSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return pauseButtonSprite;
        }

        public static Sprite CreateRotateButtonSprite()
        {
            if (rotateButtonSprite != null)
                return rotateButtonSprite;

            rotateButtonSprite = CreateSpriteFromAtlas("BrickStacker/ui_wood_buttons_atlas", 0.305f, 0.160f, 0.165f, 0.255f, 100f);
            return rotateButtonSprite ?? CreatePauseButtonSprite();
        }

        public static Sprite CreateTacticalCellSprite()
        {
            if (tacticalCellSprite != null)
                return tacticalCellSprite;

            tacticalCellSprite = CreateSpriteFromAtlas("BrickStacker/tactical_tiles_atlas", 0.050f, 0.105f, 0.145f, 0.205f, 100f);
            return tacticalCellSprite ?? CreateWoodButtonSprite();
        }

        public static Sprite CreateTacticalHighlightSprite()
        {
            if (tacticalHighlightSprite != null)
                return tacticalHighlightSprite;

            tacticalHighlightSprite = CreateSpriteFromAtlas("BrickStacker/tactical_tiles_atlas", 0.395f, 0.095f, 0.175f, 0.230f, 100f);
            return tacticalHighlightSprite ?? CreateTacticalCellSprite();
        }

        public static Sprite CreateTacticalWallSprite()
        {
            if (tacticalWallSprite != null)
                return tacticalWallSprite;

            tacticalWallSprite = CreateSpriteFromAtlas("BrickStacker/tactical_tiles_atlas", 0.610f, 0.095f, 0.170f, 0.230f, 100f);
            return tacticalWallSprite ?? CreateTacticalCellSprite();
        }

        public static Sprite CreateTacticalPlayerSprite()
        {
            if (tacticalPlayerSprite != null)
                return tacticalPlayerSprite;

            tacticalPlayerSprite = CreateSpriteFromAtlas("BrickStacker/tactical_pieces_atlas", 0.2307f, 0.1335f, 0.1278f, 0.2339f, 100f);
            return tacticalPlayerSprite;
        }

        public static Sprite CreateTacticalEnemySprite()
        {
            if (tacticalEnemySprite != null)
                return tacticalEnemySprite;

            tacticalEnemySprite = CreateSpriteFromAtlas("BrickStacker/tactical_pieces_atlas", 0.4406f, 0.1400f, 0.1312f, 0.2256f, 100f);
            return tacticalEnemySprite;
        }

        public static Sprite CreateTacticalMonsterSprite()
        {
            if (tacticalMonsterSprite != null)
                return tacticalMonsterSprite;

            tacticalMonsterSprite = CreateSpriteFromAtlas("BrickStacker/tactical_pieces_atlas", 0.6457f, 0.1538f, 0.1506f, 0.2155f, 100f);
            return tacticalMonsterSprite;
        }

        static Sprite CreateSpriteFromAtlas(string resourcePath, float normalizedX, float normalizedTop, float normalizedWidth, float normalizedHeight, float pixelsPerUnit)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return null;

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            float x = Mathf.Clamp01(normalizedX) * texture.width;
            float width = Mathf.Clamp01(normalizedWidth) * texture.width;
            float height = Mathf.Clamp01(normalizedHeight) * texture.height;
            float y = texture.height - (Mathf.Clamp01(normalizedTop) * texture.height) - height;
            var rect = new Rect(Mathf.Round(x), Mathf.Round(Mathf.Clamp(y, 0, texture.height - height)), Mathf.Round(width), Mathf.Round(height));
            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        public static Sprite CreateWoodPanelSprite()
        {
            if (woodPanelSprite != null)
                return woodPanelSprite;

            woodPanelSprite = CreateWoodUiSprite(192, 192, 30, 20, true);
            return woodPanelSprite;
        }

        public static Sprite CreateWoodButtonSprite()
        {
            if (woodButtonSprite != null)
                return woodButtonSprite;

            woodButtonSprite = CreateWoodUiSprite(192, 72, 12, 10, false);
            return woodButtonSprite;
        }

        // Nút vuông bóng (glossy) màu tùy chọn — dùng cho hàng nút menu để đồng bộ với btn-bxh.

        static Sprite CreateWoodUiSprite(int width, int height, int radius, int border, bool deepPanel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var woodData = Resources.Load<TextAsset>("BrickStacker/wood_background_source");
            Texture2D woodTexture = null;
            if (woodData != null)
            {
                woodTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!woodTexture.LoadImage(woodData.bytes))
                    woodTexture = null;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (OutsideRoundedRect(x, y, width, height, radius))
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    int edgeDistance = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    Color wood = woodTexture != null
                        ? woodTexture.GetPixelBilinear(0.18f + x / (float)width * 0.48f, 0.18f + y / (float)height * 0.42f)
                        : new Color(0.48f, 0.24f, 0.095f, 1f);

                    float grain = Mathf.PerlinNoise(x * 0.055f, y * 0.025f) * 0.08f;
                    Color color = Color.Lerp(wood, deepPanel ? new Color(0.18f, 0.070f, 0.024f, 1f) : new Color(0.28f, 0.12f, 0.045f, 1f), deepPanel ? 0.55f : 0.36f);
                    color = Color.Lerp(color, new Color(0.72f, 0.42f, 0.20f, 1f), deepPanel ? 0.08f : 0.18f);
                    color += new Color(grain, grain * 0.45f, grain * 0.18f, 0f);

                    if (edgeDistance < border)
                    {
                        float edge = 1f - edgeDistance / (float)Mathf.Max(1, border);
                        color = Color.Lerp(color, new Color(0.045f, 0.016f, 0.006f, 1f), edge * 0.88f);
                    }
                    else if (edgeDistance < border + 5)
                    {
                        color = Color.Lerp(color, new Color(0.80f, 0.50f, 0.27f, 1f), 0.18f);
                    }

                    if (y > height - border - 8 && edgeDistance >= border)
                        color = Color.Lerp(color, new Color(0.95f, 0.65f, 0.36f, 1f), 0.10f);

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static bool OutsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            if (x >= radius && x < width - radius)
                return false;
            if (y >= radius && y < height - radius)
                return false;

            int cx = x < radius ? radius : width - radius - 1;
            int cy = y < radius ? radius : height - radius - 1;
            return DistanceSq(x, y, cx, cy) > radius * radius;
        }

        public static bool HasBoardFrameSprite()
        {
            return Resources.Load<TextAsset>("BrickStacker/board_frame_source") != null;
        }

        public static Sprite CreateBoardFrameSprite()
        {
            if (boardFrameSprite != null)
                return boardFrameSprite;

            var data = Resources.Load<TextAsset>("BrickStacker/board_frame_source");
            if (data == null)
                return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(data.bytes))
                return null;

            texture = CreateRoundedTexture(texture, Mathf.RoundToInt(Mathf.Min(texture.width, texture.height) * 0.085f));
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            boardFrameSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            return boardFrameSprite;
        }

        public static Sprite CreateRoundedWoodSprite()
        {
            if (roundedWoodSprite != null)
                return roundedWoodSprite;

            var data = Resources.Load<TextAsset>("BrickStacker/wood_background_source");
            if (data == null)
                return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(data.bytes))
                return null;

            var rounded = CreateRoundedTexture(texture, Mathf.RoundToInt(Mathf.Min(texture.width, texture.height) * 0.055f));
            rounded.filterMode = FilterMode.Bilinear;
            rounded.wrapMode = TextureWrapMode.Clamp;
            roundedWoodSprite = Sprite.Create(rounded, new Rect(0, 0, rounded.width, rounded.height), new Vector2(0.5f, 0.5f), 100f);
            return roundedWoodSprite;
        }

        static Texture2D CreateRoundedTexture(Texture2D source, int radius)
        {
            var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            var pixels = source.GetPixels32();
            int width = source.width;
            int height = source.height;
            int radiusSq = radius * radius;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool outside = false;
                    if (x < radius && y < radius)
                        outside = DistanceSq(x, y, radius, radius) > radiusSq;
                    else if (x >= width - radius && y < radius)
                        outside = DistanceSq(x, y, width - radius - 1, radius) > radiusSq;
                    else if (x < radius && y >= height - radius)
                        outside = DistanceSq(x, y, radius, height - radius - 1) > radiusSq;
                    else if (x >= width - radius && y >= height - radius)
                        outside = DistanceSq(x, y, width - radius - 1, height - radius - 1) > radiusSq;

                    var color = pixels[y * width + x];
                    if (outside)
                        color.a = 0;
                    pixels[y * width + x] = color;
                }
            }

            output.SetPixels32(pixels);
            output.Apply();
            return output;
        }

        static int DistanceSq(int x, int y, int cx, int cy)
        {
            int dx = x - cx;
            int dy = y - cy;
            return dx * dx + dy * dy;
        }

        public static Material Material(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            material.color = color;
            return material;
        }

        public static void CreateWoodBackdrop(string name, Camera cam, float z, Color overlayColor)
        {
            if (cam == null)
                cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();

            Vector3 center = cam != null ? new Vector3(cam.transform.position.x, cam.transform.position.y, z) : new Vector3(0, 0, z);

            var sprite = CreateWoodBackdropSprite();
            var back = new GameObject(name);
            back.name = name;
            back.transform.position = center;
            var backRenderer = back.AddComponent<SpriteRenderer>();
            backRenderer.sprite = sprite;
            backRenderer.sortingOrder = -1000;
            var backFitter = back.AddComponent<CameraSpriteFitter>();
            backFitter.Target = cam;
            backFitter.Depth = z;
            backFitter.Overscan = 2.18f;

            var blurSprite = CreateBlurredWoodBackdropSprite();
            var blur = new GameObject(name + " Soft Dark Blur");
            blur.name = name + " Soft Dark Blur";
            blur.transform.position = center + new Vector3(0, 0, -0.03f);
            var blurRenderer = blur.AddComponent<SpriteRenderer>();
            blurRenderer.sprite = blurSprite;
            blurRenderer.color = new Color(0.18f, 0.075f, 0.025f, 0.38f);
            blurRenderer.sortingOrder = -999;
            var blurFitter = blur.AddComponent<CameraSpriteFitter>();
            blurFitter.Target = cam;
            blurFitter.Depth = z - 0.03f;
            blurFitter.Overscan = 2.18f;

            var overlay = new GameObject(name + " Shade");
            overlay.name = name + " Shade";
            overlay.transform.position = center + new Vector3(0, 0, -0.04f);
            var overlayRenderer = overlay.AddComponent<SpriteRenderer>();
            overlayRenderer.sprite = CreateSolidSprite();
            overlayRenderer.color = overlayColor;
            overlayRenderer.sortingOrder = -998;
            var overlayFitter = overlay.AddComponent<CameraSpriteFitter>();
            overlayFitter.Target = cam;
            overlayFitter.Depth = z - 0.04f;
            overlayFitter.Overscan = 2.18f;
        }

        static Sprite CreateWoodBackdropSprite()
        {
            if (woodBackdropSprite != null)
                return woodBackdropSprite;

            var texture = Resources.Load<Texture2D>("BrickStacker/wood_background");
            if (texture == null)
                return CreateSolidSprite();

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            woodBackdropSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            return woodBackdropSprite;
        }

        static Sprite CreateBlurredWoodBackdropSprite()
        {
            if (blurredWoodBackdropSprite != null)
                return blurredWoodBackdropSprite;

            var data = Resources.Load<TextAsset>("BrickStacker/wood_background_source");
            if (data == null)
                return CreateWoodBackdropSprite();

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(data.bytes))
                return CreateWoodBackdropSprite();

            int width = Mathf.Min(128, texture.width);
            int height = Mathf.Max(1, Mathf.RoundToInt(texture.height * (width / (float)texture.width)));
            var small = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;
                    small.SetPixel(x, y, texture.GetPixelBilinear(u, v));
                }
            }

            small.Apply();
            for (int i = 0; i < 3; i++)
                small = BoxBlur(small);

            small.filterMode = FilterMode.Bilinear;
            small.wrapMode = TextureWrapMode.Clamp;
            blurredWoodBackdropSprite = Sprite.Create(small, new Rect(0, 0, small.width, small.height), new Vector2(0.5f, 0.5f), 100f);
            return blurredWoodBackdropSprite;
        }

        static Texture2D BoxBlur(Texture2D source)
        {
            int width = source.width;
            int height = source.height;
            var output = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color sum = Color.clear;
                    int count = 0;
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        int py = Mathf.Clamp(y + oy, 0, height - 1);
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int px = Mathf.Clamp(x + ox, 0, width - 1);
                            sum += source.GetPixel(px, py);
                            count++;
                        }
                    }

                    output.SetPixel(x, y, sum / count);
                }
            }

            output.Apply();
            return output;
        }

        static Sprite CreateSolidSprite()
        {
            if (solidSprite != null)
                return solidSprite;

            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    texture.SetPixel(x, y, Color.white);
            texture.Apply();
            solidSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1f);
            return solidSprite;
        }

        static Material TransparentMaterial(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            material.color = color;
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            return material;
        }

        public static ParticleSystem CreateLineParticles(Transform parent, Color color)
        {
            var go = new GameObject("Wood Dust Particles");
            go.transform.SetParent(parent);
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.78f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.105f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.88f, 0.84f, 0.76f, 0.42f), new Color(0.52f, 0.52f, 0.50f, 0.16f));
            main.maxParticles = 180;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.03f;
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 46) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(9.6f, 0.28f, 0.08f);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.28f, 0.28f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.16f, 0.78f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.65f;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = TransparentMaterial(new Color(0.78f, 0.74f, 0.66f, 0.55f));

            particles.Stop();
            return particles;
        }
    }
}
