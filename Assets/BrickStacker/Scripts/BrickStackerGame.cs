using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BrickStacker
{
    public static class GameSession
    {
        public static int SelectedLevel = 1;
    }

    [Serializable]
    public class LevelRules
    {
        public string Name;
        public string Tagline;
        public Color BackgroundA;
        public Color BackgroundB;
        public float FallInterval;
        public float SpeedRampSeconds;
        public float MaxFallSpeedMultiplier = 1f;
        public int TargetLines;
        public int GarbageEveryPieces;
        public float SurpriseGarbageChance;
        public int ScoreMultiplier;
        public int ForcedPieceType = -1;
        public bool AllowSpecialBlocks = true;

        public static LevelRules Create(int level)
        {
            if (level == 3)
            {
                return new LevelRules
                {
                    Name = "Hard",
                    Tagline = "Nhanh hon, co hang rac bat ngo.",
                    BackgroundA = new Color(0.02f, 0.03f, 0.09f),
                    BackgroundB = new Color(0.0f, 0.32f, 0.42f),
                    FallInterval = 0.72f,
                    SpeedRampSeconds = 180f,
                    MaxFallSpeedMultiplier = 2.5f,
                    TargetLines = 0,
                    GarbageEveryPieces = 0,
                    SurpriseGarbageChance = 0.16f,
                    ScoreMultiplier = 1
                };
            }

            if (level == 4)
            {
                return new LevelRules
                {
                    Name = "T-Block Trial",
                    Tagline = "Chi co khoi T, toc do tang dan.",
                    BackgroundA = new Color(0.05f, 0.02f, 0.08f),
                    BackgroundB = new Color(0.35f, 0.08f, 0.16f),
                    FallInterval = 0.72f,
                    SpeedRampSeconds = 180f,
                    MaxFallSpeedMultiplier = 2.5f,
                    TargetLines = 0,
                    GarbageEveryPieces = 0,
                    SurpriseGarbageChance = 0f,
                    ScoreMultiplier = 1,
                    ForcedPieceType = 5,
                    AllowSpecialBlocks = false
                };
            }

            if (level == 2)
            {
                return new LevelRules
                {
                    Name = "Normal",
                    Tagline = "Tu canh diem roi, khong co bong mo.",
                    BackgroundA = new Color(0.02f, 0.06f, 0.08f),
                    BackgroundB = new Color(0.08f, 0.22f, 0.18f),
                    FallInterval = 0.72f,
                    TargetLines = 0,
                    GarbageEveryPieces = 0,
                    SurpriseGarbageChance = 0f,
                    ScoreMultiplier = 1
                };
            }

            return new LevelRules
            {
                Name = "Easy",
                Tagline = "De vao nhip, co bong mo goi y.",
                BackgroundA = new Color(0.02f, 0.06f, 0.08f),
                BackgroundB = new Color(0.08f, 0.22f, 0.18f),
                FallInterval = 0.72f,
                TargetLines = 0,
                GarbageEveryPieces = 0,
                SurpriseGarbageChance = 0f,
                ScoreMultiplier = 1
            };
        }
    }

    public class MenuController : MonoBehaviour
    {
        Font font;

        void Start()
        {
            font = LoadFont();
            Time.timeScale = 1f;
            BuildCamera();
            BuildBackground();
            BuildUi();
        }

        void BuildCamera()
        {
            var cam = Camera.main ?? FindAnyObjectByType<Camera>();
            GameObject cameraObject;
            if (cam == null)
            {
                cameraObject = new GameObject("Main Camera");
                cam = cameraObject.AddComponent<Camera>();
            }
            else
            {
                cameraObject = cam.gameObject;
            }
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.backgroundColor = new Color(0.33f, 0.15f, 0.055f);
            cameraObject.transform.position = new Vector3(0, 0, -10);
            if (cameraObject.GetComponent<AudioListener>() == null)
                cameraObject.AddComponent<AudioListener>();
        }

        void BuildBackground()
        {
            RuntimeArt.CreateWoodBackdrop("Menu Wood Backdrop", Camera.main, 1.2f, new Color(0.12f, 0.045f, 0.015f, 0.24f));
        }

        void BuildUi()
        {
            var canvas = Ui.CreateCanvas("Menu Canvas");
            var safe = Ui.Panel(canvas.transform, "Menu Safe Area", new Color(0, 0, 0, 0));
            Ui.Stretch(safe);
            safe.AddComponent<SafeAreaFitter>();
            var panel = Ui.Panel(safe.transform, "Menu Panel", new Color(0, 0, 0, 0));
            Ui.Stretch(panel);

            var centerPanel = Ui.Panel(panel.transform, "Menu Center Panel", new Color(0.10f, 0.045f, 0.022f, 0.46f));
            Ui.Rect(centerPanel, new Vector2(0.5f, 0.51f), new Vector2(0.5f, 0.51f), new Vector2(500, 700));
            AddMenuGlowFrame(panel.transform, new Vector2(0.5f, 0.51f), new Vector2(514, 714));

            var menuStack = Ui.Panel(panel.transform, "Menu Stack", new Color(0, 0, 0, 0));
            Ui.Rect(menuStack, new Vector2(0.5f, 0.545f), new Vector2(0.5f, 0.545f), new Vector2(820, 650));

            var titleGroup = Ui.Panel(menuStack.transform, "Title Group", new Color(0, 0, 0, 0));
            Ui.Rect(titleGroup, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820, 190));
            titleGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 148);

            var titleShadow = Ui.Text(titleGroup.transform, "BLOCKFALL", font, 104, new Color(0.08f, 0.03f, 0.012f, 0.95f), TextAnchor.MiddleCenter);
            Ui.Rect(titleShadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820, 140));
            titleShadow.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, 10);
            titleShadow.GetComponent<RectTransform>().localScale = new Vector3(1.08f, 1f, 1f);

            var title = Ui.Text(titleGroup.transform, "BLOCKFALL", font, 104, new Color(1f, 0.84f, 0.42f), TextAnchor.MiddleCenter);
            Ui.Rect(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820, 140));
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 27);
            title.GetComponent<RectTransform>().localScale = new Vector3(1.08f, 1f, 1f);
            AddDarkWoodTextEdge(title, 1.25f, 0.82f);

            var titleDropShadow = Ui.Text(titleGroup.transform, "BLOCKFALL", font, 104, new Color(0.035f, 0.012f, 0.004f, 0.48f), TextAnchor.MiddleCenter);
            Ui.Rect(titleDropShadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820, 140));
            titleDropShadow.GetComponent<RectTransform>().anchoredPosition = new Vector2(16, -6);
            titleDropShadow.GetComponent<RectTransform>().localScale = new Vector3(1.08f, 1f, 1f);
            titleDropShadow.transform.SetSiblingIndex(titleShadow.transform.GetSiblingIndex());

            var titleLine = Ui.Panel(titleGroup.transform, "Title Accent", new Color(1f, 0.64f, 0.32f, 0.85f));
            Ui.Rect(titleLine, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(430, 6));
            titleLine.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -58);

            var optionsGroup = Ui.Panel(menuStack.transform, "Options Group", new Color(0, 0, 0, 0));
            Ui.Rect(optionsGroup, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460, 450));
            optionsGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -115);

            var subtitle = Ui.Text(optionsGroup.transform, "SELECT MODE", font, 25, new Color(1f, 0.92f, 0.76f), TextAnchor.MiddleCenter);
            Ui.Rect(subtitle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360, 42));
            subtitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 142);
            AddDarkWoodTextEdge(subtitle, 1.15f, 0.95f);

            AddLevelButton(optionsGroup.transform, 1, new Vector2(0.5f, 0.5f), new Vector2(0, 64));
            AddLevelButton(optionsGroup.transform, 2, new Vector2(0.5f, 0.5f), new Vector2(0, -23));
            AddLevelButton(optionsGroup.transform, 3, new Vector2(0.5f, 0.5f), new Vector2(0, -110));
            AddLevelButton(optionsGroup.transform, 4, new Vector2(0.5f, 0.5f), new Vector2(0, -197));
        }

        void AddMenuGlowFrame(Transform parent, Vector2 anchor, Vector2 size)
        {
            var frame = Ui.Panel(parent, "Menu Warm Glow Frame", new Color(0, 0, 0, 0));
            Ui.Rect(frame, anchor, anchor, size);

            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;
            Color soft = new Color(1f, 0.55f, 0.20f, 0.12f);
            Color mid = new Color(1f, 0.64f, 0.30f, 0.23f);
            Color core = new Color(1f, 0.78f, 0.44f, 0.72f);

            AddGlowStrip(frame.transform, new Vector2(-halfW + 5, 0), new Vector2(24, size.y - 34), soft);
            AddGlowStrip(frame.transform, new Vector2(halfW - 5, 0), new Vector2(24, size.y - 34), soft);
            AddGlowStrip(frame.transform, new Vector2(0, halfH - 5), new Vector2(size.x - 34, 24), soft);
            AddGlowStrip(frame.transform, new Vector2(0, -halfH + 5), new Vector2(size.x - 34, 24), soft);

            AddGlowStrip(frame.transform, new Vector2(-halfW + 5, 0), new Vector2(10, size.y - 42), mid);
            AddGlowStrip(frame.transform, new Vector2(halfW - 5, 0), new Vector2(10, size.y - 42), mid);
            AddGlowStrip(frame.transform, new Vector2(0, halfH - 5), new Vector2(size.x - 42, 10), mid);
            AddGlowStrip(frame.transform, new Vector2(0, -halfH + 5), new Vector2(size.x - 42, 10), mid);

            AddGlowStrip(frame.transform, new Vector2(-halfW + 5, 0), new Vector2(3, size.y - 54), core);
            AddGlowStrip(frame.transform, new Vector2(halfW - 5, 0), new Vector2(3, size.y - 54), core);
            AddGlowStrip(frame.transform, new Vector2(0, halfH - 5), new Vector2(size.x - 54, 3), core);
            AddGlowStrip(frame.transform, new Vector2(0, -halfH + 5), new Vector2(size.x - 54, 3), core);
        }

        void AddGlowStrip(Transform parent, Vector2 offset, Vector2 size, Color color)
        {
            var strip = Ui.Panel(parent, "Glow Strip", color);
            Ui.Rect(strip, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size);
            strip.GetComponent<RectTransform>().anchoredPosition = offset;
        }

        void AddMenuBlocks(Transform parent)
        {
            var colors = new[]
            {
                new Color(0.98f, 0.68f, 0.38f, 0.9f),
                new Color(1f, 0.80f, 0.48f, 0.9f),
                new Color(0.78f, 0.42f, 0.20f, 0.9f)
            };

            var left = new[] { new Vector2(-38, 0), new Vector2(0, 0), new Vector2(38, 0), new Vector2(0, 38) };
            var right = new[] { new Vector2(-19, 19), new Vector2(19, 19), new Vector2(-19, -19), new Vector2(19, -19) };
            AddMiniPiece(parent, new Vector2(0.23f, 0.615f), left, colors[0]);
            AddMiniPiece(parent, new Vector2(0.77f, 0.615f), right, colors[1]);
        }

        void AddMiniPiece(Transform parent, Vector2 anchor, Vector2[] offsets, Color color)
        {
            foreach (var offset in offsets)
            {
                var block = Ui.Panel(parent, "Menu Block", color);
                Ui.Rect(block, anchor, anchor, new Vector2(34, 34));
                block.GetComponent<RectTransform>().anchoredPosition = offset;
            }
        }

        void AddLevelButton(Transform parent, int level, Vector2 anchor)
        {
            AddLevelButton(parent, level, anchor, Vector2.zero);
        }

        void AddLevelButton(Transform parent, int level, Vector2 anchor, Vector2 offset)
        {
            var rules = LevelRules.Create(level);
            var shadow = Ui.Panel(parent, "Level Shadow", new Color(0.07f, 0.03f, 0.015f, 0.7f));
            Ui.Rect(shadow, anchor, anchor, new Vector2(420, 78));
            shadow.GetComponent<RectTransform>().anchoredPosition = offset + new Vector2(0, -6);

            var button = Ui.Button(parent, rules.Name, font, 24, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                GameSession.SelectedLevel = level;
                SceneManager.LoadScene("BrickGame");
            });
            Ui.Rect(button.gameObject, anchor, anchor, new Vector2(405, 68));
            button.GetComponent<RectTransform>().anchoredPosition = offset;
            StyleWoodRectButton(button, 27);
            var label = button.GetComponentInChildren<Text>();
            label.text = rules.Name;
            label.fontSize = 27;
            AddDarkWoodTextEdge(label, 1.05f, 0.92f);
        }

        void AddDarkWoodTextEdge(Text text, float thickness, float alpha)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.13f, 0.050f, 0.014f, alpha);
            outline.effectDistance = new Vector2(thickness, thickness);
            outline.useGraphicAlpha = true;

            var depth = text.gameObject.AddComponent<Shadow>();
            depth.effectColor = new Color(0.045f, 0.016f, 0.005f, 0.62f);
            depth.effectDistance = new Vector2(0.75f, -0.85f);
            depth.useGraphicAlpha = true;
        }

        void StyleWoodRectButton(Button button, int fontSize)
        {
            var image = button.GetComponent<Image>();
            image.sprite = RuntimeArt.CreateWoodButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var text = button.GetComponentInChildren<Text>();
            text.color = new Color(1f, 0.90f, 0.68f);
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextMinSize = Mathf.Min(16, fontSize);
            text.resizeTextMaxSize = fontSize;
            AddDarkWoodTextEdge(text, 0.95f, 0.88f);

            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.055f, 0.018f, 0.006f, 0.58f);
            shadow.effectDistance = new Vector2(0.8f, -0.9f);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.74f, 1f);
            colors.pressedColor = new Color(0.78f, 0.52f, 0.30f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
        }

        Font LoadFont()
        {
            return RuntimeArt.LoadDisplayFont();
        }
    }

    public class BrickGameController : MonoBehaviour
    {
        const int Width = 10;
        const int Height = 20;
        readonly Color[] palette =
        {
            new Color(0.34f, 0.86f, 0.86f), new Color(0.34f, 0.62f, 0.98f), new Color(1.00f, 0.48f, 0.20f),
            new Color(1.00f, 0.78f, 0.24f), new Color(0.42f, 0.86f, 0.30f), new Color(0.78f, 0.34f, 0.86f),
            new Color(0.96f, 0.24f, 0.20f)
        };

        readonly Vector2Int[][] shapes =
        {
            new [] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) },
            new [] { new Vector2Int(-1,1), new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0) },
            new [] { new Vector2Int(1,1), new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0) },
            new [] { new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(0,0), new Vector2Int(1,0) },
            new [] { new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(-1,0), new Vector2Int(0,0) },
            new [] { new Vector2Int(0,1), new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0) },
            new [] { new Vector2Int(-1,1), new Vector2Int(0,1), new Vector2Int(0,0), new Vector2Int(1,0) }
        };

        LevelRules rules;
        Font font;
        Sprite blockSprite;
        Transform boardRoot;
        Transform activeRoot;
        Transform ghostRoot;
        Transform settledRoot;
        Transform fxRoot;
        Camera cam;
        Text scoreText;
        Text levelText;
        Text bestText;
        Text linesText;
        Text statusText;
        Text nextText;
        List<Image> nextPreviewCells = new List<Image>();
        List<Image> holdPreviewCells = new List<Image>();
        Button pauseButton;
        Button rotateButton;
        ParticleSystem clearParticles;
        AudioSource audioSource;
        AudioSource musicSource;
        GameObject pauseOverlay;
        GameObject gameOverOverlay;
        Text gameOverTitleText;
        Text gameOverScoreText;
        Vector3 cameraHome;
        RectTransform safeAreaRoot;
        RectTransform hudPanelRect;
        RectTransform hudShadowRect;
        RectTransform holdWidgetRect;
        RectTransform holdWidgetShadowRect;
        RectTransform nextWidgetRect;
        RectTransform nextWidgetShadowRect;
        RectTransform pauseButtonRect;
        RectTransform rotateButtonRect;
        int lastScreenWidth;
        int lastScreenHeight;
        Vector2 gestureStart;
        Vector2 gestureLastPosition;
        float gestureStartTime;
        float gameplayTime;
        bool gestureTracking;
        bool gestureMoved;
        bool gestureMovedHorizontally;

        int[,] grid = new int[Width, Height];
        GameObject[,] lockedBlocks = new GameObject[Width, Height];
        List<GameObject> activeBlocks = new List<GameObject>();
        List<GameObject> ghostBlocks = new List<GameObject>();
        Queue<int> nextBag = new Queue<int>();
        int currentType;
        int holdType = -1;
        bool currentPieceIsSpecial;
        bool canHold;
        Vector2Int origin;
        int rotation;
        float fallTimer;
        int score;
        int bestScore;
        int lines;
        int combo;
        int piecesLocked;
        bool gameOver;
        bool paused;
        bool resolving;
        float shake;

        void Start()
        {
            font = RuntimeArt.LoadDisplayFont();
            rules = LevelRules.Create(GameSession.SelectedLevel);
            bestScore = PlayerPrefs.GetInt(BestScoreKey(), 0);
            blockSprite = RuntimeArt.CreateBlockSprite();
            BuildWorld();
            BuildUi();
            FillBag();
            SpawnPiece();
            UpdateUi();
        }

        void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
                ConfigureResponsiveCamera();

            if (gameOver)
            {
                if (KeyPressed(KeyCode.R, Key.R)) Restart();
                if (KeyPressed(KeyCode.Escape, Key.Escape)) BackToMenu();
                return;
            }

            if (KeyPressed(KeyCode.P, Key.P) || KeyPressed(KeyCode.Escape, Key.Escape))
                TogglePause();

            if (paused || resolving)
                return;

            HandleInput();
            gameplayTime += Time.deltaTime;
            fallTimer += Time.deltaTime;
            if (fallTimer >= CurrentFallInterval())
            {
                fallTimer = 0f;
                StepDown();
            }

            UpdateCameraShake();
        }

        void BuildWorld()
        {
            cam = Camera.main ?? FindAnyObjectByType<Camera>();
            GameObject camObject;
            if (cam == null)
            {
                camObject = new GameObject("Main Camera");
                cam = camObject.AddComponent<Camera>();
            }
            else
            {
                camObject = cam.gameObject;
            }

            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.33f, 0.15f, 0.055f);
            ConfigureResponsiveCamera();
            if (camObject.GetComponent<AudioListener>() == null)
                camObject.AddComponent<AudioListener>();
            audioSource = gameObject.AddComponent<AudioSource>();
            musicSource = gameObject.AddComponent<AudioSource>();
            StartBackgroundMusic();

            boardRoot = new GameObject("Board").transform;
            settledRoot = new GameObject("Locked Blocks").transform;
            activeRoot = new GameObject("Active Piece").transform;
            ghostRoot = new GameObject("Ghost Piece").transform;
            fxRoot = new GameObject("Effects").transform;

            CreateBackdrop();
            CreateBoardFrame();
            CreateGrid();
            clearParticles = RuntimeArt.CreateLineParticles(fxRoot, rules.BackgroundB);
        }

        void CreateBackdrop()
        {
            RuntimeArt.CreateWoodBackdrop("Warm Wood Backdrop", cam, 1.2f, new Color(0.08f, 0.028f, 0.01f, 0.34f));
        }

        void CreateGrid()
        {
            if (RuntimeArt.HasBoardFrameSprite())
                return;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    var cell = NewBlock("Grid Cell", RuntimeArt.GridColor, boardRoot);
                    cell.transform.position = CellToWorld(x, y);
                    cell.transform.localScale = Vector3.one * 0.94f;
                    cell.GetComponent<SpriteRenderer>().sortingOrder = 0;
                }
        }

        void CreateBoardFrame()
        {
            var boardSprite = RuntimeArt.CreateBoardFrameSprite();
            if (boardSprite != null)
            {
                var boardArt = new GameObject("Wood Board Art");
                boardArt.transform.SetParent(boardRoot);
                boardArt.transform.position = new Vector3(0, 0, 0.35f);
                var renderer = boardArt.AddComponent<SpriteRenderer>();
                renderer.sprite = boardSprite;
                renderer.sortingOrder = -20;

                Vector2 targetSize = new Vector2(11.25f, 21.35f);
                Vector2 spriteSize = boardSprite.bounds.size;
                boardArt.transform.localScale = new Vector3(targetSize.x / spriteSize.x, targetSize.y / spriteSize.y, 1f);
                CreateAlignedPlayfieldOverlay();
                return;
            }

            var stage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stage.name = "Board Stage";
            stage.transform.SetParent(boardRoot);
            stage.transform.position = new Vector3(0, 0, 0.55f);
            stage.transform.localScale = new Vector3(11.75f, 21.85f, 0.06f);
            stage.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.Material(new Color(0.045f, 0.018f, 0.008f, 0.70f));

            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Board Frame";
            frame.transform.SetParent(boardRoot);
            frame.transform.position = new Vector3(0, 0, 0.35f);
            frame.transform.localScale = new Vector3(10.35f, 20.35f, 0.08f);
            frame.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.Material(new Color(0.12f, 0.065f, 0.035f, 0.95f));
        }

        void CreateAlignedPlayfieldOverlay()
        {
            var playfieldSprite = RuntimeArt.CreateRoundedWoodSprite();
            if (playfieldSprite != null)
            {
                var playfield = new GameObject("Aligned Wood Playfield");
                playfield.transform.SetParent(boardRoot);
                playfield.transform.position = new Vector3(0, 0, 0.18f);
                var renderer = playfield.AddComponent<SpriteRenderer>();
                renderer.sprite = playfieldSprite;
                renderer.color = new Color(0.82f, 0.55f, 0.36f, 0.96f);
                renderer.sortingOrder = -10;
                Vector2 spriteSize = playfieldSprite.bounds.size;
                playfield.transform.localScale = new Vector3(10.05f / spriteSize.x, 20.05f / spriteSize.y, 1f);
            }
            else
            {
                var playfield = GameObject.CreatePrimitive(PrimitiveType.Cube);
                playfield.name = "Aligned Wood Playfield";
                playfield.transform.SetParent(boardRoot);
                playfield.transform.position = new Vector3(0, 0, 0.18f);
                playfield.transform.localScale = new Vector3(10.05f, 20.05f, 0.03f);
                playfield.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.Material(new Color(0.30f, 0.16f, 0.075f, 0.92f));
            }

            var verticalLineMaterial = RuntimeArt.Material(new Color(0.055f, 0.026f, 0.012f, 0.86f));
            for (int x = 0; x <= Width; x++)
            {
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Aligned Board Vertical Line";
                line.transform.SetParent(boardRoot);
                line.transform.position = new Vector3(x - Width * 0.5f, 0, 0.08f);
                line.transform.localScale = new Vector3(0.035f, Height, 0.02f);
                line.GetComponent<MeshRenderer>().sharedMaterial = verticalLineMaterial;
                line.GetComponent<MeshRenderer>().sortingOrder = -5;
            }

            var horizontalLineMaterial = RuntimeArt.Material(new Color(0.050f, 0.022f, 0.010f, 0.84f));
            for (int y = 0; y <= Height; y++)
            {
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Aligned Board Horizontal Line";
                line.transform.SetParent(boardRoot);
                line.transform.position = new Vector3(0, y - Height * 0.5f, 0.07f);
                line.transform.localScale = new Vector3(Width, 0.035f, 0.02f);
                line.GetComponent<MeshRenderer>().sharedMaterial = horizontalLineMaterial;
                line.GetComponent<MeshRenderer>().sortingOrder = -5;
            }
        }

        void BuildUi()
        {
            var canvas = Ui.CreateCanvas("Game Canvas");
            var safe = Ui.Panel(canvas.transform, "Safe Area", new Color(0, 0, 0, 0));
            safeAreaRoot = safe.GetComponent<RectTransform>();
            Ui.ApplySafeArea(safeAreaRoot);
            safe.AddComponent<SafeAreaFitter>();

            var hudShadow = Ui.Panel(safe.transform, "Hud Shadow", new Color(0, 0, 0, 0));
            hudShadowRect = hudShadow.GetComponent<RectTransform>();
            Ui.Rect(hudShadow, new Vector2(0.055f, 0.858f), new Vector2(0.790f, 0.968f), new Vector2(0, 0));
            hudShadowRect.anchoredPosition = new Vector2(0, -6);

            var hudPanel = Ui.Panel(safe.transform, "Hud Panel", new Color(0, 0, 0, 0));
            hudPanelRect = hudPanel.GetComponent<RectTransform>();
            Ui.Rect(hudPanel, new Vector2(0.055f, 0.866f), new Vector2(0.790f, 0.976f), new Vector2(0, 0));

            var hudTopAccent = Ui.Panel(hudPanel.transform, "Hud Top Accent", new Color(0, 0, 0, 0));
            Ui.Rect(hudTopAccent, new Vector2(0.035f, 0.89f), new Vector2(0.965f, 0.925f), new Vector2(0, 0));

            var hudBottomLine = Ui.Panel(hudPanel.transform, "Hud Bottom Line", new Color(0, 0, 0, 0));
            Ui.Rect(hudBottomLine, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.085f), new Vector2(0, 0));

            linesText = Ui.Text(hudPanel.transform, "Normal - Scores 0", font, 26, new Color(1f, 0.92f, 0.78f), TextAnchor.MiddleLeft);
            Ui.Rect(linesText, new Vector2(0.00f, 0.58f), new Vector2(0.96f, 0.94f), new Vector2(0, 0));
            AddDarkWoodTextEdge(linesText, 0.95f, 0.86f);

            levelText = Ui.Text(hudPanel.transform, "Line 0", font, 26, new Color(1f, 0.78f, 0.52f), TextAnchor.MiddleLeft);
            Ui.Rect(levelText, new Vector2(0.00f, 0.50f), new Vector2(0.76f, 0.56f), new Vector2(0, 0));

            bestText = Ui.Text(hudPanel.transform, "", font, 31, new Color(1f, 0.72f, 0.32f), TextAnchor.MiddleLeft);
            Ui.Rect(bestText, new Vector2(0.00f, 0.22f), new Vector2(0.98f, 0.58f), new Vector2(0, 0));
            AddDarkWoodTextEdge(bestText, 1.05f, 0.88f);

            scoreText = Ui.Text(hudPanel.transform, "", font, 1, new Color(1f, 1f, 1f, 0f), TextAnchor.MiddleRight);
            Ui.Rect(scoreText, new Vector2(0.96f, 0.92f), new Vector2(0.98f, 0.94f), new Vector2(0, 0));

            pauseButton = Ui.Button(safe.transform, "II", font, 24, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                TogglePause();
            });
            pauseButtonRect = pauseButton.GetComponent<RectTransform>();
            Ui.Rect(pauseButton.gameObject, new Vector2(0.855f, 0.905f), new Vector2(0.955f, 0.975f), new Vector2(0, 0));
            StylePauseButton(pauseButton);

            rotateButton = Ui.Button(safe.transform, "R", font, 24, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                RotateFromButton();
            });
            rotateButtonRect = rotateButton.GetComponent<RectTransform>();
            Ui.Rect(rotateButton.gameObject, new Vector2(0.835f, 0.260f), new Vector2(0.945f, 0.340f), new Vector2(0, 0));
            StyleRoundWoodButton(rotateButton, "R", 30);

            var holdWidgetShadow = Ui.Panel(safe.transform, "Hold Widget Shadow", new Color(0, 0, 0, 0));
            holdWidgetShadowRect = holdWidgetShadow.GetComponent<RectTransform>();
            Ui.Rect(holdWidgetShadow, new Vector2(0.790f, 0.470f), new Vector2(0.960f, 0.640f), new Vector2(0, 0));
            holdWidgetShadowRect.anchoredPosition = new Vector2(0, -5);

            var holdWidget = Ui.Panel(safe.transform, "Hold Widget", new Color(0, 0, 0, 0));
            holdWidgetRect = holdWidget.GetComponent<RectTransform>();
            Ui.Rect(holdWidget, new Vector2(0.790f, 0.477f), new Vector2(0.960f, 0.647f), new Vector2(0, 0));
            var holdButton = holdWidget.AddComponent<Button>();
            holdButton.onClick.AddListener(SwapHoldPiece);

            var holdLabel = Ui.Text(holdWidget.transform, "HOLD", font, 29, new Color(1f, 0.90f, 0.72f), TextAnchor.MiddleCenter);
            StyleSideWidgetTitle(holdLabel);
            Ui.Rect(holdLabel, new Vector2(0.00f, 0.76f), new Vector2(1.00f, 1.00f), new Vector2(0, 0));

            var holdPanel = Ui.Panel(holdWidget.transform, "Hold Piece Panel", new Color(1f, 1f, 1f, 1f));
            Ui.Rect(holdPanel, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.73f), new Vector2(0, 0));
            ApplyBoardFrameToPreviewPanel(holdPanel.transform);
            var holdPanelButton = holdPanel.AddComponent<Button>();
            holdPanelButton.onClick.AddListener(SwapHoldPiece);
            holdPreviewCells = CreatePiecePreview(holdPanel.transform, new Vector2(0.5f, 0.48f), 15.0f);

            var nextWidgetShadow = Ui.Panel(safe.transform, "Next Widget Shadow", new Color(0, 0, 0, 0));
            nextWidgetShadowRect = nextWidgetShadow.GetComponent<RectTransform>();
            Ui.Rect(nextWidgetShadow, new Vector2(0.790f, 0.665f), new Vector2(0.960f, 0.835f), new Vector2(0, 0));
            nextWidgetShadowRect.anchoredPosition = new Vector2(0, -5);

            var nextWidget = Ui.Panel(safe.transform, "Next Widget", new Color(0, 0, 0, 0));
            nextWidgetRect = nextWidget.GetComponent<RectTransform>();
            Ui.Rect(nextWidget, new Vector2(0.790f, 0.672f), new Vector2(0.960f, 0.842f), new Vector2(0, 0));

            nextText = Ui.Text(nextWidget.transform, "NEXT", font, 29, new Color(1f, 0.90f, 0.72f), TextAnchor.MiddleCenter);
            StyleSideWidgetTitle(nextText);
            Ui.Rect(nextText, new Vector2(0.00f, 0.76f), new Vector2(1.00f, 1.00f), new Vector2(0, 0));

            var nextPanel = Ui.Panel(nextWidget.transform, "Next Piece Panel", new Color(1f, 1f, 1f, 1f));
            Ui.Rect(nextPanel, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.73f), new Vector2(0, 0));
            ApplyBoardFrameToPreviewPanel(nextPanel.transform);
            nextPreviewCells = CreatePiecePreview(nextPanel.transform, new Vector2(0.5f, 0.48f), 15.0f);

            statusText = Ui.Text(safe.transform, "", font, 34, Color.white, TextAnchor.MiddleCenter);
            Ui.Rect(statusText, new Vector2(0.12f, 0.34f), new Vector2(0.88f, 0.58f), new Vector2(0, 0));

            pauseOverlay = Ui.Panel(canvas.transform, "Pause Overlay", new Color(0, 0, 0, 0.65f));
            Ui.Stretch(pauseOverlay);
            BuildPausePopup(pauseOverlay.transform);
            pauseOverlay.SetActive(false);

            gameOverOverlay = Ui.Panel(canvas.transform, "Game Over Overlay", new Color(0, 0, 0, 0.68f));
            Ui.Stretch(gameOverOverlay);
            BuildGameOverPopup(gameOverOverlay.transform);
            gameOverOverlay.SetActive(false);

            LayoutGameplayChrome();
        }

        void AddHudWoodDetails(Transform parent)
        {
            var innerTop = Ui.Panel(parent, "Hud Inner Top Bevel", new Color(0.34f, 0.16f, 0.07f, 0.55f));
            Ui.Rect(innerTop, new Vector2(0.018f, 0.84f), new Vector2(0.982f, 0.875f), new Vector2(0, 0));

            var innerLeft = Ui.Panel(parent, "Hud Inner Left Bevel", new Color(0.30f, 0.13f, 0.055f, 0.44f));
            Ui.Rect(innerLeft, new Vector2(0.018f, 0.12f), new Vector2(0.032f, 0.875f), new Vector2(0, 0));

            var innerRight = Ui.Panel(parent, "Hud Inner Right Bevel", new Color(0.06f, 0.024f, 0.012f, 0.46f));
            Ui.Rect(innerRight, new Vector2(0.968f, 0.12f), new Vector2(0.982f, 0.875f), new Vector2(0, 0));

            var woodLines = new[]
            {
                new Vector4(0.08f, 0.80f, 0.38f, 0.818f),
                new Vector4(0.12f, 0.48f, 0.31f, 0.494f),
                new Vector4(0.42f, 0.17f, 0.79f, 0.186f),
                new Vector4(0.66f, 0.56f, 0.92f, 0.574f),
                new Vector4(0.70f, 0.34f, 0.94f, 0.352f)
            };

            for (int i = 0; i < woodLines.Length; i++)
            {
                var line = woodLines[i];
                var grain = Ui.Panel(parent, "Hud Wood Grain", new Color(0.55f, 0.26f, 0.105f, 0.30f));
                Ui.Rect(grain, new Vector2(line.x, line.y), new Vector2(line.z, line.w), new Vector2(0, 0));
            }

            AddHudStud(parent, new Vector2(0.045f, 0.82f));
            AddHudStud(parent, new Vector2(0.955f, 0.82f));
            AddHudStud(parent, new Vector2(0.045f, 0.15f));
            AddHudStud(parent, new Vector2(0.955f, 0.15f));
        }

        void AddHudStud(Transform parent, Vector2 anchor)
        {
            var shadow = Ui.Panel(parent, "Hud Brass Stud Shadow", new Color(0.04f, 0.015f, 0.006f, 0.62f));
            Ui.Rect(shadow, anchor, anchor, new Vector2(14, 14));
            shadow.GetComponent<RectTransform>().anchoredPosition = new Vector2(1, -1);

            var stud = Ui.Panel(parent, "Hud Brass Stud", new Color(0.86f, 0.48f, 0.18f, 0.92f));
            Ui.Rect(stud, anchor, anchor, new Vector2(11, 11));
        }

        void AddSideWidgetWoodDetails(Transform parent)
        {
            var top = Ui.Panel(parent, "Side Widget Top Bevel", new Color(0.46f, 0.23f, 0.105f, 0.36f));
            Ui.Rect(top, new Vector2(0.16f, 0.875f), new Vector2(0.84f, 0.925f), new Vector2(0, 0));

            var bottom = Ui.Panel(parent, "Side Widget Bottom Bevel", new Color(0.045f, 0.018f, 0.008f, 0.40f));
            Ui.Rect(bottom, new Vector2(0.16f, 0.055f), new Vector2(0.84f, 0.105f), new Vector2(0, 0));

            var grainA = Ui.Panel(parent, "Side Widget Wood Grain", new Color(0.55f, 0.26f, 0.105f, 0.24f));
            Ui.Rect(grainA, new Vector2(0.18f, 0.82f), new Vector2(0.78f, 0.84f), new Vector2(0, 0));

            var grainB = Ui.Panel(parent, "Side Widget Wood Grain", new Color(0.55f, 0.26f, 0.105f, 0.20f));
            Ui.Rect(grainB, new Vector2(0.24f, 0.28f), new Vector2(0.82f, 0.30f), new Vector2(0, 0));

        }

        void ApplyBoardFrameToPreviewPanel(Transform parent)
        {
            var image = parent.GetComponent<Image>();
            var sprite = RuntimeArt.CreateBoardFrameSprite();
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;

            var inner = Ui.Panel(parent, "Preview Dark Interior", new Color(0.22f, 0.095f, 0.040f, 0.99f));
            Ui.Rect(inner, new Vector2(0.055f, 0.055f), new Vector2(0.945f, 0.945f), new Vector2(0, 0));

            var shade = Ui.Panel(parent, "Preview Inner Shade", new Color(0.040f, 0.015f, 0.006f, 0.30f));
            Ui.Rect(shade, new Vector2(0.055f, 0.055f), new Vector2(0.945f, 0.945f), new Vector2(0, 0));
        }

        void StyleSideWidgetTitle(Text label)
        {
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(1f, 0.88f, 0.62f);
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = label.fontSize;
            AddDarkWoodTextEdge(label, 1.05f, 0.90f);

            var warmEdge = label.gameObject.AddComponent<Shadow>();
            warmEdge.effectColor = new Color(0.22f, 0.085f, 0.025f, 0.48f);
            warmEdge.effectDistance = new Vector2(0.45f, -0.45f);

            var carvedDepth = label.gameObject.AddComponent<Shadow>();
            carvedDepth.effectColor = new Color(0.045f, 0.016f, 0.005f, 0.72f);
            carvedDepth.effectDistance = new Vector2(1.0f, -1.1f);
        }

        void StylePauseButton(Button button)
        {
            StyleRoundWoodButton(button, "II", 28);
        }

        void StyleRoundWoodButton(Button button, string labelText, int fontSize)
        {
            var image = button.GetComponent<Image>();
            image.sprite = RuntimeArt.CreatePauseButtonSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;

            var buttonShadow = button.gameObject.AddComponent<Shadow>();
            buttonShadow.effectColor = new Color(0.025f, 0.008f, 0.002f, 0.90f);
            buttonShadow.effectDistance = new Vector2(4.5f, -5.5f);

            var label = button.GetComponentInChildren<Text>();
            label.text = labelText;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.22f, 0.095f, 0.035f, 1f);
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = fontSize;

            var iconShadow = label.gameObject.AddComponent<Shadow>();
            iconShadow.effectColor = new Color(1f, 0.80f, 0.45f, 0.35f);
            iconShadow.effectDistance = new Vector2(-0.7f, 0.7f);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.78f, 1f);
            colors.pressedColor = new Color(0.78f, 0.50f, 0.28f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
        }

        void StyleWoodPopupFrame(GameObject panel)
        {
            var image = panel.GetComponent<Image>();
            image.sprite = RuntimeArt.CreateWoodPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var innerShade = Ui.Panel(panel.transform, "Popup Inner Shade", new Color(0.045f, 0.018f, 0.008f, 0.18f));
            Ui.Rect(innerShade, new Vector2(0.055f, 0.055f), new Vector2(0.945f, 0.945f), new Vector2(0, 0));

            var topShine = Ui.Panel(panel.transform, "Popup Top Shine", new Color(0.84f, 0.52f, 0.28f, 0.30f));
            Ui.Rect(topShine, new Vector2(0.075f, 0.905f), new Vector2(0.925f, 0.920f), new Vector2(0, 0));
        }

        void StyleWoodPopupShadow(GameObject panel)
        {
            var image = panel.GetComponent<Image>();
            image.sprite = RuntimeArt.CreateWoodPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.045f, 0.018f, 0.008f, 0.82f);
        }

        void StyleWoodRectButton(Button button, int fontSize)
        {
            var image = button.GetComponent<Image>();
            image.sprite = RuntimeArt.CreateWoodButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var text = button.GetComponentInChildren<Text>();
            text.color = new Color(1f, 0.90f, 0.68f);
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextMinSize = Mathf.Min(16, fontSize);
            text.resizeTextMaxSize = fontSize;
            AddDarkWoodTextEdge(text, 0.95f, 0.88f);

            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.055f, 0.018f, 0.006f, 0.58f);
            shadow.effectDistance = new Vector2(0.8f, -0.9f);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.74f, 1f);
            colors.pressedColor = new Color(0.78f, 0.52f, 0.30f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
        }

        void AddDarkWoodTextEdge(Text text, float thickness, float alpha)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.13f, 0.050f, 0.014f, alpha);
            outline.effectDistance = new Vector2(thickness, thickness);
            outline.useGraphicAlpha = true;

            var depth = text.gameObject.AddComponent<Shadow>();
            depth.effectColor = new Color(0.045f, 0.016f, 0.005f, 0.62f);
            depth.effectDistance = new Vector2(0.75f, -0.85f);
            depth.useGraphicAlpha = true;
        }

        void BuildPausePopup(Transform parent)
        {
            var shadow = Ui.Panel(parent, "Pause Popup Shadow", new Color(0.04f, 0.018f, 0.008f, 0.78f));
            Ui.Rect(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460, 450));
            shadow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -12);
            StyleWoodPopupShadow(shadow);

            var box = Ui.Panel(parent, "Pause Popup", Color.white);
            Ui.Rect(box, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(450, 440));
            StyleWoodPopupFrame(box);

            var accent = Ui.Panel(box.transform, "Pause Accent", new Color(0.80f, 0.48f, 0.24f, 0.58f));
            Ui.Rect(accent, new Vector2(0.5f, 0.705f), new Vector2(0.5f, 0.705f), new Vector2(170, 4));

            var title = Ui.Text(box.transform, "PAUSED", font, 44, new Color(1f, 0.86f, 0.56f), TextAnchor.MiddleCenter);
            Ui.Rect(title, new Vector2(0.5f, 0.760f), new Vector2(0.5f, 0.760f), new Vector2(340, 72));
            AddDarkWoodTextEdge(title, 1.15f, 0.90f);

            AddPauseButton(box.transform, "Resume", new Vector2(0.5f, 0.555f), TogglePause);
            AddPauseButton(box.transform, "Retry", new Vector2(0.5f, 0.395f), Restart);
            AddPauseButton(box.transform, "Menu", new Vector2(0.5f, 0.235f), BackToMenu);
        }

        void AddPauseButton(Transform parent, string label, Vector2 anchor, UnityEngine.Events.UnityAction action)
        {
            var shadow = Ui.Panel(parent, label + " Shadow", new Color(0.055f, 0.022f, 0.01f, 0.65f));
            Ui.Rect(shadow, anchor, anchor, new Vector2(290, 64));
            shadow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -5);

            var button = Ui.Button(parent, label, font, 24, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                action.Invoke();
            });
            Ui.Rect(button.gameObject, anchor, anchor, new Vector2(280, 58));
            StyleWoodRectButton(button, 24);
        }

        void BuildGameOverPopup(Transform parent)
        {
            var shadow = Ui.Panel(parent, "Game Over Popup Shadow", new Color(0.04f, 0.018f, 0.008f, 0.82f));
            Ui.Rect(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(470, 430));
            shadow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -12);
            StyleWoodPopupShadow(shadow);

            var box = Ui.Panel(parent, "Game Over Popup", Color.white);
            Ui.Rect(box, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(460, 420));
            StyleWoodPopupFrame(box);

            var accent = Ui.Panel(box.transform, "Game Over Accent", new Color(0.80f, 0.48f, 0.24f, 0.58f));
            Ui.Rect(accent, new Vector2(0.5f, 0.705f), new Vector2(0.5f, 0.705f), new Vector2(190, 4));

            gameOverTitleText = Ui.Text(box.transform, "GAME OVER", font, 40, new Color(1f, 0.74f, 0.42f), TextAnchor.MiddleCenter);
            Ui.Rect(gameOverTitleText, new Vector2(0.5f, 0.755f), new Vector2(0.5f, 0.755f), new Vector2(360, 70));
            AddDarkWoodTextEdge(gameOverTitleText, 1.15f, 0.90f);

            gameOverScoreText = Ui.Text(box.transform, "", font, 24, Color.white, TextAnchor.MiddleCenter);
            Ui.Rect(gameOverScoreText, new Vector2(0.5f, 0.620f), new Vector2(0.5f, 0.620f), new Vector2(340, 54));
            AddDarkWoodTextEdge(gameOverScoreText, 0.95f, 0.86f);

            AddPauseButton(box.transform, "Retry", new Vector2(0.5f, 0.410f), Restart);
            AddPauseButton(box.transform, "Menu", new Vector2(0.5f, 0.255f), BackToMenu);
        }

        void ConfigureResponsiveCamera()
        {
            float aspect = Mathf.Max(0.35f, cam.aspect);
            bool portrait = aspect < 0.8f;
            float boardHalfHeight = Height * 0.5f;
            float boardHalfWidth = Width * 0.5f;

            if (portrait)
            {
                GetPortraitGameplayLayout(aspect, out float boardLeft, out float boardRight, out float boardBottom, out float boardTop, out _, out _);
                float boardWidthScreenFraction = boardRight - boardLeft;
                float boardHeightScreenFraction = boardTop - boardBottom;
                float sizeForHeight = boardHalfHeight / boardHeightScreenFraction;
                float sizeForWidth = boardHalfWidth / (boardWidthScreenFraction * aspect);
                cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);

                float boardCenterScreenX = (boardLeft + boardRight) * 0.5f;
                float boardCenterScreenY = (boardBottom + boardTop) * 0.5f;
                float cameraX = -(boardCenterScreenX - 0.5f) * 2f * cam.orthographicSize * aspect;
                float cameraY = -(boardCenterScreenY - 0.5f) * 2f * cam.orthographicSize;
                cameraHome = new Vector3(cameraX, cameraY, -10f);
            }
            else
            {
                float sizeForHeight = boardHalfHeight + 0.9f;
                float sizeForWidth = (boardHalfWidth + 2.4f) / aspect;
                cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
                cameraHome = new Vector3(0f, 0.7f, -10f);
            }

            cam.transform.position = cameraHome;
            if (safeAreaRoot != null)
                Ui.ApplySafeArea(safeAreaRoot);
            LayoutGameplayChrome();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }

        void LayoutGameplayChrome()
        {
            if (nextWidgetRect == null || nextWidgetShadowRect == null)
                return;

            float aspect = Screen.height > 0 ? Mathf.Max(0.35f, (float)Screen.width / Screen.height) : 0.56f;
            if (aspect >= 0.8f)
            {
                if (hudPanelRect != null)
                    ApplyAnchoredRect(hudPanelRect, new Vector2(0.045f, 0.825f), new Vector2(0.445f, 0.945f), Vector2.zero);
                if (hudShadowRect != null)
                    ApplyAnchoredRect(hudShadowRect, new Vector2(0.045f, 0.817f), new Vector2(0.445f, 0.937f), new Vector2(0, -6));
                if (pauseButtonRect != null)
                    ApplyAnchoredRect(pauseButtonRect, new Vector2(0.864f, 0.828f), new Vector2(0.986f, 0.974f), new Vector2(-10, 0));

                ApplyAnchoredRect(nextWidgetRect, new Vector2(0.805f, 0.595f), new Vector2(0.970f, 0.830f), Vector2.zero);
                ApplyAnchoredRect(nextWidgetShadowRect, new Vector2(0.805f, 0.588f), new Vector2(0.970f, 0.823f), new Vector2(0, -5));
                if (holdWidgetRect != null)
                    ApplyAnchoredRect(holdWidgetRect, new Vector2(0.805f, 0.330f), new Vector2(0.970f, 0.565f), Vector2.zero);
                if (holdWidgetShadowRect != null)
                    ApplyAnchoredRect(holdWidgetShadowRect, new Vector2(0.805f, 0.323f), new Vector2(0.970f, 0.558f), new Vector2(0, -5));
                if (rotateButtonRect != null)
                    ApplyAnchoredRect(rotateButtonRect, new Vector2(0.835f, 0.190f), new Vector2(0.955f, 0.300f), Vector2.zero);
                return;
            }

            GetPortraitGameplayLayout(aspect, out _, out _, out _, out float boardTop, out float sideMin, out float sideMax);
            float gap = aspect < 0.5f ? 0.026f : 0.034f;
            float widgetHeight = Mathf.Clamp(0.195f + (0.56f - Mathf.Min(aspect, 0.56f)) * 0.20f, 0.195f, 0.220f);
            float nextTop = Mathf.Min(0.850f, boardTop);
            float nextBottom = nextTop - widgetHeight;
            float holdTop = nextBottom - gap;
            float holdBottom = holdTop - widgetHeight;
            float hudBoardGap = 40f / Mathf.Max(1f, Screen.height);
            float hudBottom = boardTop + hudBoardGap;
            float hudTop = hudBottom + 0.110f;
            float hudShadowBottom = hudBottom - 0.008f;
            float hudShadowTop = hudTop - 0.008f;
            float pauseBottom = hudBottom;
            float pauseTop = pauseBottom + 0.150f;

            if (hudPanelRect != null)
                ApplyAnchoredRect(hudPanelRect, new Vector2(0.055f, hudBottom), new Vector2(sideMin - 0.035f, hudTop), Vector2.zero);
            if (hudShadowRect != null)
                ApplyAnchoredRect(hudShadowRect, new Vector2(0.055f, hudShadowBottom), new Vector2(sideMin - 0.035f, hudShadowTop), new Vector2(0, -6));
            if (pauseButtonRect != null)
                ApplyAnchoredRect(pauseButtonRect, new Vector2(Mathf.Max(0.835f, sideMax - 0.150f), pauseBottom), new Vector2(sideMax, pauseTop), new Vector2(-10, 0));

            ApplyAnchoredRect(nextWidgetRect, new Vector2(sideMin, nextBottom), new Vector2(sideMax, nextTop), Vector2.zero);
            ApplyAnchoredRect(nextWidgetShadowRect, new Vector2(sideMin, nextBottom - 0.007f), new Vector2(sideMax, nextTop - 0.007f), new Vector2(0, -5));

            if (holdWidgetRect != null)
                ApplyAnchoredRect(holdWidgetRect, new Vector2(sideMin, holdBottom), new Vector2(sideMax, holdTop), Vector2.zero);
            if (holdWidgetShadowRect != null)
                ApplyAnchoredRect(holdWidgetShadowRect, new Vector2(sideMin, holdBottom - 0.007f), new Vector2(sideMax, holdTop - 0.007f), new Vector2(0, -5));

            if (rotateButtonRect != null)
            {
                float rotateHeight = Mathf.Clamp(widgetHeight * 0.48f, 0.095f, 0.112f);
                float rotateTop = holdBottom - gap * 0.80f;
                float rotateBottom = Mathf.Max(0.055f, rotateTop - rotateHeight);
                float rotateInset = Mathf.Clamp((sideMax - sideMin) * 0.18f, 0.020f, 0.034f);
                ApplyAnchoredRect(rotateButtonRect, new Vector2(sideMin + rotateInset, rotateBottom), new Vector2(sideMax - rotateInset, rotateTop), Vector2.zero);
            }
        }

        void GetPortraitGameplayLayout(float aspect, out float boardLeft, out float boardRight, out float boardBottom, out float boardTop, out float sideMin, out float sideMax)
        {
            float narrow = Mathf.InverseLerp(0.62f, 0.42f, aspect);
            sideMax = Mathf.Lerp(0.972f, 0.985f, narrow);
            sideMin = Mathf.Lerp(0.770f, 0.800f, narrow);
            boardLeft = Mathf.Lerp(0.075f, 0.055f, narrow);
            boardRight = sideMin - Mathf.Lerp(0.066f, 0.052f, narrow);
            boardBottom = Mathf.Lerp(0.065f, 0.075f, narrow);
            boardTop = Mathf.Lerp(0.805f, 0.785f, narrow);
        }

        void ApplyAnchoredRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offset)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = offset;
            rect.sizeDelta = Vector2.zero;
        }

        List<Image> CreatePiecePreview(Transform parent, Vector2 center, float cellSize)
        {
            var cells = new List<Image>();
            for (int i = 0; i < 16; i++)
            {
                int x = i % 4;
                int y = i / 4;
                var cell = Ui.Panel(parent, "Preview Cell", new Color(1, 1, 1, 0)).GetComponent<Image>();
                cell.sprite = blockSprite;
                cell.type = Image.Type.Simple;
                Ui.Rect(cell.gameObject, center, center, new Vector2(cellSize, cellSize));
                float step = cellSize * 1.20f;
                cell.rectTransform.anchoredPosition = new Vector2((x - 1.5f) * step, (1.5f - y) * step);
                cells.Add(cell);
            }
            return cells;
        }

        void HandleInput()
        {
            HandleGestureInput();

            if (KeyPressed(KeyCode.LeftArrow, Key.LeftArrow) || KeyPressed(KeyCode.A, Key.A)) TryMove(Vector2Int.left);
            if (KeyPressed(KeyCode.RightArrow, Key.RightArrow) || KeyPressed(KeyCode.D, Key.D)) TryMove(Vector2Int.right);
            if (KeyPressed(KeyCode.DownArrow, Key.DownArrow) || KeyPressed(KeyCode.S, Key.S)) SoftDrop();
            if (KeyPressed(KeyCode.UpArrow, Key.UpArrow) || KeyPressed(KeyCode.W, Key.W)) TryRotate(1);
            if (KeyPressed(KeyCode.Z, Key.Z)) TryRotate(-1);
            if (KeyPressed(KeyCode.Space, Key.Space)) HardDrop();
            if (KeyPressed(KeyCode.C, Key.C) || KeyPressed(KeyCode.LeftShift, Key.LeftShift)) Hold();
            if (KeyPressed(KeyCode.R, Key.R)) Restart();
        }

        void HandleGestureInput()
        {
            bool pressed;
            bool released;
            Vector2 position;
            if (!ReadPointer(out pressed, out released, out position))
                return;

            if (pressed && PointerOverInteractiveUi(position))
                return;

            if (pressed)
            {
                gestureStart = position;
                gestureLastPosition = position;
                gestureStartTime = Time.unscaledTime;
                gestureTracking = true;
                gestureMoved = false;
                gestureMovedHorizontally = false;
                return;
            }

            if (!gestureTracking)
                return;

            float dragStep = Mathf.Min(Screen.width, Screen.height) * 0.055f;
            Vector2 dragDelta = position - gestureLastPosition;

            while (Mathf.Abs(dragDelta.x) >= dragStep && Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y) * 0.55f)
            {
                int direction = dragDelta.x < 0 ? -1 : 1;
                TryMove(direction < 0 ? Vector2Int.left : Vector2Int.right);
                gestureLastPosition.x += dragStep * direction;
                dragDelta = position - gestureLastPosition;
                gestureMoved = true;
                gestureMovedHorizontally = true;
            }

            if (!released)
                return;

            gestureTracking = false;
            Vector2 delta = position - gestureStart;
            float distance = delta.magnitude;
            float elapsed = Mathf.Max(0.01f, Time.unscaledTime - gestureStartTime);
            float swipeThreshold = Mathf.Min(Screen.width, Screen.height) * 0.08f;

            if (!gestureMoved && distance < swipeThreshold && elapsed < 0.35f)
            {
                TryRotate(1);
                return;
            }

            if (!gestureMovedHorizontally && delta.y < -swipeThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
            {
                HardDrop();
            }
        }

        bool PointerOverInteractiveUi(Vector2 position)
        {
            if (EventSystem.current == null)
                return false;

            var eventData = new PointerEventData(EventSystem.current) { position = position };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                var button = hits[i].gameObject.GetComponentInParent<Button>();
                if (button != null && button.isActiveAndEnabled && button.interactable)
                    return true;
            }
            return false;
        }

        bool ReadPointer(out bool pressed, out bool released, out Vector2 position)
        {
            pressed = false;
            released = false;
            position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame)
                {
                    position = touch.position.ReadValue();
                    pressed = touch.press.wasPressedThisFrame;
                    released = touch.press.wasReleasedThisFrame;
                    return true;
                }
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    position = Mouse.current.position.ReadValue();
                    pressed = Mouse.current.leftButton.wasPressedThisFrame;
                    released = Mouse.current.leftButton.wasReleasedThisFrame;
                    return true;
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                position = touch.position;
                pressed = touch.phase == TouchPhase.Began;
                released = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                return true;
            }

            position = Input.mousePosition;
            pressed = Input.GetMouseButtonDown(0);
            released = Input.GetMouseButtonUp(0);
            return Input.GetMouseButton(0) || pressed || released;
#else
            return false;
#endif
        }

        bool KeyPressed(KeyCode legacyKey, Key inputSystemKey)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current[inputSystemKey].wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(legacyKey);
#else
            return false;
#endif
        }

        void SpawnPiece()
        {
            if (nextBag.Count < 7)
                FillBag();

            currentPieceIsSpecial = rules.AllowSpecialBlocks && piecesLocked > 4 && UnityEngine.Random.value < 0.055f;
            currentType = currentPieceIsSpecial ? UnityEngine.Random.Range(0, palette.Length) : nextBag.Dequeue();
            origin = currentPieceIsSpecial ? new Vector2Int(Width / 2, Height - 1) : new Vector2Int(Width / 2, Height - 2);
            rotation = 0;
            canHold = true;
            ClearActive();

            if (!IsValid(origin, rotation))
            {
                EndGame(false);
                return;
            }

            DrawActive();
            UpdateUi();
        }

        void FillBag()
        {
            if (rules.ForcedPieceType >= 0)
            {
                while (nextBag.Count < 14)
                    nextBag.Enqueue(rules.ForcedPieceType);
                return;
            }

            var bag = new List<int> { 0, 1, 2, 3, 4, 5, 6 };
            while (bag.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, bag.Count);
                nextBag.Enqueue(bag[index]);
                bag.RemoveAt(index);
            }
        }

        void TryMove(Vector2Int delta)
        {
            if (!IsValid(origin + delta, rotation))
                return;

            origin += delta;
            DrawActive();
            Beep(520f, 0.025f, 0.08f);
        }

        void SoftDrop()
        {
            if (IsValid(origin + Vector2Int.down, rotation))
            {
                origin += Vector2Int.down;
                DrawActive();
                UpdateUi();
            }
            else
            {
                LockPiece();
            }
        }

        void StepDown()
        {
            if (IsValid(origin + Vector2Int.down, rotation))
            {
                origin += Vector2Int.down;
                DrawActive();
            }
            else
            {
                LockPiece();
            }
        }

        void HardDrop()
        {
            while (IsValid(origin + Vector2Int.down, rotation))
            {
                origin += Vector2Int.down;
            }
            LockPiece();
            shake = 0.16f;
            Beep(110f, 0.08f, 0.18f);
        }

        void TryRotate(int direction)
        {
            int nextRotation = (rotation + direction + 4) % 4;
            var kicks = new[] { Vector2Int.zero, Vector2Int.left, Vector2Int.right, new Vector2Int(0, 1), new Vector2Int(-2, 0), new Vector2Int(2, 0) };
            foreach (var kick in kicks)
            {
                if (IsValid(origin + kick, nextRotation))
                {
                    origin += kick;
                    rotation = nextRotation;
                    DrawActive();
                    Beep(720f, 0.035f, 0.08f);
                    return;
                }
            }
        }

        void RotateFromButton()
        {
            if (paused || resolving || gameOver)
                return;

            TryRotate(1);
        }

        void SwapHoldPiece()
        {
            if (paused || resolving || gameOver || !canHold || currentPieceIsSpecial)
                return;

            RuntimeArt.PlayUiSwitchSound();
            ClearActive();
            if (holdType < 0)
            {
                holdType = currentType;
                SpawnPiece();
            }
            else
            {
                int old = currentType;
                currentType = holdType;
                holdType = old;
                origin = new Vector2Int(Width / 2, Height - 2);
                rotation = 0;
                fallTimer = 0f;
                if (!IsValid(origin, rotation))
                {
                    EndGame(false);
                    return;
                }
                DrawActive();
                UpdateUi();
            }

            canHold = false;
            Beep(330f, 0.06f, 0.12f);
        }

        void Hold()
        {
            SwapHoldPiece();
        }

        void LockPiece()
        {
            if (resolving)
                return;

            resolving = true;
            bool bombShouldExplode = false;
            Vector2Int bombCell = Vector2Int.zero;

            if (currentPieceIsSpecial)
            {
                bombCell = origin;
                if (bombCell.y < Height)
                {
                    grid[bombCell.x, bombCell.y] = currentType + 1;
                    var bombBlock = NewBlock("Bomb Block", RuntimeArt.SpecialBlockColor, settledRoot);
                    bombBlock.transform.position = CellToWorld(bombCell.x, bombCell.y);
                    lockedBlocks[bombCell.x, bombCell.y] = bombBlock;
                    bombShouldExplode = true;
                }
            }
            else
            {
                foreach (var localCell in shapes[currentType])
                {
                    var cell = CellFromLocal(localCell, origin, rotation);
                    if (cell.y >= Height)
                        continue;

                    grid[cell.x, cell.y] = currentType + 1;
                    var block = NewBlock("Locked Block", palette[currentType], settledRoot);
                    block.transform.position = CellToWorld(cell.x, cell.y);
                    lockedBlocks[cell.x, cell.y] = block;
                }
            }

            piecesLocked++;
            currentPieceIsSpecial = false;
            ClearActive();
            StartCoroutine(ResolveLinesThenSpawn(bombShouldExplode, bombCell));
        }

        IEnumerator ResolveLinesThenSpawn(bool bombShouldExplode, Vector2Int bombCell)
        {
            if (bombShouldExplode)
                yield return ExplodeSpecialBlock(bombCell);

            int cleared = FindFullRows().Count;
            if (cleared > 0)
            {
                yield return ClearRows();
            }
            else
            {
                combo = 0;
            }

            bool timedGarbage = rules.GarbageEveryPieces > 0 && piecesLocked % rules.GarbageEveryPieces == 0;
            bool surpriseGarbage = cleared == 0 && piecesLocked > 5 && rules.SurpriseGarbageChance > 0f && UnityEngine.Random.value < rules.SurpriseGarbageChance;
            if (!gameOver && (timedGarbage || surpriseGarbage))
            {
                AddGarbageRow();
                shake = 0.2f;
                Beep(82f, 0.12f, 0.2f);
            }

            if (rules.TargetLines > 0 && lines >= rules.TargetLines)
            {
                EndGame(true);
                yield break;
            }

            resolving = false;
            SpawnPiece();
        }

        IEnumerator ExplodeSpecialBlock(Vector2Int center)
        {
            int removed = 0;
            int touchedRow = Mathf.Clamp(center.y - 1, 0, Height - 1);
            var rowsToClear = new List<int> { touchedRow };
            int belowTouchedRow = touchedRow - 1;
            if (belowTouchedRow >= 0)
                rowsToClear.Add(belowTouchedRow);

            if (center.x >= 0 && center.x < Width && center.y >= 0 && center.y < Height)
            {
                if (!rowsToClear.Contains(center.y))
                    grid[center.x, center.y] = 0;
                if (lockedBlocks[center.x, center.y] != null)
                {
                    PulseAndDestroy(lockedBlocks[center.x, center.y]);
                    lockedBlocks[center.x, center.y] = null;
                }
            }

            foreach (int y in rowsToClear)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (grid[x, y] == 0)
                        continue;

                    grid[x, y] = 0;
                    removed++;
                    if (lockedBlocks[x, y] != null)
                    {
                        PulseAndDestroy(lockedBlocks[x, y]);
                        lockedBlocks[x, y] = null;
                    }
                }

                clearParticles.transform.position = CellToWorld(Width / 2, y);
                clearParticles.Play();
            }

            score += Mathf.Max(removed, Width) * 90;
            lines += rowsToClear.Count;
            shake = 0.32f;
            Beep(160f, 0.16f, 0.28f);
            yield return new WaitForSeconds(0.18f);
            CompactRows(rowsToClear);
            RedrawLocked();
            UpdateUi();
        }

        List<int> FindFullRows()
        {
            var rows = new List<int>();
            for (int y = 0; y < Height; y++)
            {
                bool full = true;
                for (int x = 0; x < Width; x++)
                    full &= grid[x, y] > 0;
                if (full)
                    rows.Add(y);
            }
            return rows;
        }

        IEnumerator ClearRows()
        {
            var rows = FindFullRows();
            combo++;
            int clearCount = rows.Count;
            lines += clearCount;
            score += clearCount * 120 * rules.ScoreMultiplier;
            shake = 0.1f + clearCount * 0.05f;
            Beep(880f + clearCount * 120f, 0.12f, 0.24f);

            foreach (int row in rows)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (lockedBlocks[x, row] != null)
                    {
                        PulseAndDestroy(lockedBlocks[x, row]);
                        lockedBlocks[x, row] = null;
                    }
                    grid[x, row] = 0;
                }
                clearParticles.transform.position = CellToWorld(Width / 2, row);
                clearParticles.Play();
            }

            yield return new WaitForSeconds(0.18f);

            CompactRows(rows);

            RedrawLocked();
            UpdateUi();
        }

        void CompactRows(List<int> rows)
        {
            var cleared = new HashSet<int>(rows);
            var nextGrid = new int[Width, Height];
            int writeY = 0;

            for (int y = 0; y < Height; y++)
            {
                if (cleared.Contains(y))
                    continue;

                for (int x = 0; x < Width; x++)
                    nextGrid[x, writeY] = grid[x, y];
                writeY++;
            }

            grid = nextGrid;
        }

        void AddGarbageRow()
        {
            for (int y = Height - 1; y > 0; y--)
            {
                for (int x = 0; x < Width; x++)
                    grid[x, y] = grid[x, y - 1];
            }

            int hole = UnityEngine.Random.Range(0, Width);
            for (int x = 0; x < Width; x++)
                grid[x, 0] = x == hole ? 0 : UnityEngine.Random.Range(1, 8);

            RedrawLocked();
        }

        void RedrawLocked()
        {
            foreach (Transform child in settledRoot)
                Destroy(child.gameObject);

            lockedBlocks = new GameObject[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (grid[x, y] <= 0)
                        continue;

                    var block = NewBlock("Locked Block", palette[grid[x, y] - 1], settledRoot);
                    block.transform.position = CellToWorld(x, y);
                    lockedBlocks[x, y] = block;
                }
            }
        }

        bool IsValid(Vector2Int testOrigin, int testRotation)
        {
            foreach (var cell in Cells(testOrigin, testRotation))
            {
                if (cell.x < 0 || cell.x >= Width || cell.y < 0)
                    return false;

                if (cell.y < Height && grid[cell.x, cell.y] > 0)
                    return false;
            }
            return true;
        }

        IEnumerable<Vector2Int> Cells(Vector2Int testOrigin, int testRotation)
        {
            if (currentPieceIsSpecial)
            {
                yield return testOrigin;
                yield break;
            }

            foreach (var cell in shapes[currentType])
                yield return CellFromLocal(cell, testOrigin, testRotation);
        }

        Vector2Int CellFromLocal(Vector2Int cell, Vector2Int testOrigin, int testRotation)
        {
            var rotated = cell;
            if (currentType != 3)
            {
                for (int i = 0; i < testRotation; i++)
                    rotated = new Vector2Int(rotated.y, -rotated.x);
            }
            return testOrigin + rotated;
        }

        void DrawActive()
        {
            ClearActive();
            if (currentPieceIsSpecial)
            {
                var bomb = NewBlock("Active Bomb", RuntimeArt.SpecialBlockColor, activeRoot);
                bomb.transform.position = CellToWorld(origin.x, origin.y);
                bomb.transform.localScale = Vector3.one;
                activeBlocks.Add(bomb);
                DrawGhost();
                return;
            }

            foreach (var localCell in shapes[currentType])
            {
                var cell = CellFromLocal(localCell, origin, rotation);
                var block = NewBlock("Active Block", palette[currentType], activeRoot);
                block.transform.position = CellToWorld(cell.x, cell.y);
                activeBlocks.Add(block);
            }

            DrawGhost();
        }

        void DrawGhost()
        {
            foreach (var block in ghostBlocks)
                Destroy(block);
            ghostBlocks.Clear();

            if (GameSession.SelectedLevel != 1)
                return;

            var ghostOrigin = origin;
            while (IsValid(ghostOrigin + Vector2Int.down, rotation))
                ghostOrigin += Vector2Int.down;

            foreach (var cell in Cells(ghostOrigin, rotation))
            {
                var block = NewBlock("Ghost Block", new Color(1f, 1f, 1f, 0.22f), ghostRoot);
                block.transform.position = CellToWorld(cell.x, cell.y);
                block.transform.localScale = Vector3.one * 0.86f;
                block.GetComponent<SpriteRenderer>().sortingOrder = 5;
                ghostBlocks.Add(block);
            }
        }

        void ClearActive()
        {
            foreach (var block in activeBlocks)
                Destroy(block);
            activeBlocks.Clear();

            foreach (var block in ghostBlocks)
                Destroy(block);
            ghostBlocks.Clear();
        }

        GameObject NewBlock(string name, Color color, Transform parent)
        {
            var block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.localScale = Vector3.one * 0.9f;
            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = blockSprite;
            renderer.color = color;
            renderer.sortingOrder = 10;
            return block;
        }

        Vector3 CellToWorld(int x, int y)
        {
            return new Vector3(x - Width * 0.5f + 0.5f, y - Height * 0.5f + 0.5f, 0);
        }

        float CurrentFallInterval()
        {
            if (rules.MaxFallSpeedMultiplier > 1f && rules.SpeedRampSeconds > 0f)
            {
                float ramp = Mathf.Clamp01(gameplayTime / rules.SpeedRampSeconds);
                float smoothRamp = Mathf.SmoothStep(0f, 1f, ramp);
                float multiplier = Mathf.Lerp(1f, rules.MaxFallSpeedMultiplier, smoothRamp);
                return Mathf.Max(0.09f, rules.FallInterval / multiplier);
            }

            float speedUp = Mathf.Clamp(lines / 10f, 0f, 0.22f);
            return Mathf.Max(0.09f, rules.FallInterval - speedUp);
        }

        void UpdateUi()
        {
            if (score > bestScore)
            {
                bestScore = score;
                PlayerPrefs.SetInt(BestScoreKey(), bestScore);
                PlayerPrefs.Save();
            }

            scoreText.text = score.ToString();
            linesText.text = rules.Name + " - Scores " + score;
            levelText.text = "";
            bestText.text = score > 0 && score >= bestScore ? "NEW BEST!" : "BEST SCORES: " + bestScore;
            int nextType = PeekNext(0);
            nextText.text = "NEXT";
            RenderPiecePreview(nextPreviewCells, nextType, true);
            RenderPiecePreview(holdPreviewCells, holdType, holdType >= 0);
        }

        string BestScoreKey()
        {
            return "BLOCKFALL_BEST_SCORE_MODE_" + GameSession.SelectedLevel;
        }

        void RenderPiecePreview(List<Image> cells, int type, bool visible)
        {
            for (int i = 0; i < cells.Count; i++)
                cells[i].color = new Color(1f, 1f, 1f, 0f);

            if (!visible || type < 0)
                return;

            var shape = shapes[type];
            int minX = shape[0].x;
            int maxX = shape[0].x;
            int minY = shape[0].y;
            int maxY = shape[0].y;
            foreach (var cell in shape)
            {
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxY = Mathf.Max(maxY, cell.y);
            }

            float shapeCenterX = (minX + maxX) * 0.5f;
            float shapeCenterY = (minY + maxY) * 0.5f;
            float cellSize = cells.Count > 0 ? cells[0].rectTransform.sizeDelta.x : 12.5f;
            float step = cellSize * 1.20f;
            for (int i = 0; i < shape.Length && i < cells.Count; i++)
            {
                var cell = shape[i];
                var image = cells[i];
                image.rectTransform.anchoredPosition = new Vector2((cell.x - shapeCenterX) * step, -(cell.y - shapeCenterY) * step);
                image.color = palette[type];
            }
        }

        int PeekNext(int offset)
        {
            if (nextBag.Count <= offset)
                FillBag();
            return new List<int>(nextBag)[offset];
        }

        string PieceName(int type)
        {
            return new[] { "I", "J", "L", "O", "S", "T", "Z" }[type];
        }

        void EndGame(bool won)
        {
            gameOver = true;
            resolving = false;
            StopBackgroundMusic();
            ClearActive();
            statusText.text = "";
            gameOverTitleText.text = won ? "LEVEL CLEAR" : "GAME OVER";
            gameOverTitleText.color = won ? new Color(1f, 0.86f, 0.56f) : new Color(1f, 0.62f, 0.36f);
            gameOverScoreText.text = rules.TargetLines > 0 ? "Score  " + score + "\nLines  " + lines + "/" + rules.TargetLines : "Score  " + score + "\nLines  " + lines;
            gameOverOverlay.SetActive(true);
            shake = won ? 0.35f : 0.2f;
            if (won)
                Beep(1180f, 0.22f, 0.35f);
            else
                RuntimeArt.PlayGameOverSound();
        }

        void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("BrickGame");
        }

        void BackToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("BrickMenu");
        }

        void TogglePause()
        {
            if (gameOver)
                return;
            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
            pauseOverlay.SetActive(paused);
            pauseButton.GetComponentInChildren<Text>().text = "II";
            if (musicSource != null)
            {
                if (paused)
                    musicSource.Pause();
                else
                    musicSource.UnPause();
            }
        }

        void UpdateCameraShake()
        {
            if (shake <= 0)
            {
                cam.transform.position = Vector3.Lerp(cam.transform.position, cameraHome, Time.deltaTime * 8f);
                return;
            }

            shake -= Time.deltaTime;
            cam.transform.position = cameraHome + new Vector3(UnityEngine.Random.Range(-shake, shake), UnityEngine.Random.Range(-shake, shake), 0);
        }

        void PulseAndDestroy(GameObject target)
        {
            if (target != null)
                StartCoroutine(PulseRoutine(target));
        }

        IEnumerator PulseRoutine(GameObject target)
        {
            float t = 0;
            var renderer = target.GetComponent<SpriteRenderer>();
            while (t < 0.14f && target != null)
            {
                t += Time.deltaTime;
                target.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.35f, t / 0.14f);
                renderer.color = Color.Lerp(renderer.color, Color.white, 0.2f);
                yield return null;
            }
            if (target != null)
                Destroy(target);
        }

        void Beep(float frequency, float seconds, float volume)
        {
            var clip = AudioClip.Create("beep", Mathf.CeilToInt(44100 * seconds), 1, 44100, false);
            var data = new float[clip.samples];
            for (int i = 0; i < data.Length; i++)
            {
                float fade = 1f - i / (float)data.Length;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / 44100f) * fade * volume;
            }
            clip.SetData(data, 0);
            audioSource.PlayOneShot(clip);
        }

        void StartBackgroundMusic()
        {
            if (musicSource == null)
                return;

            var clip = Resources.Load<AudioClip>("BrickStacker/gameplay_music");
            if (clip == null)
                return;

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.18f;
            musicSource.spatialBlend = 0f;
            musicSource.Play();
        }

        void StopBackgroundMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }
    }

    public class FloatyBrick : MonoBehaviour
    {
        public float Speed = 0.25f;
        float seed;

        void Awake()
        {
            seed = UnityEngine.Random.Range(0f, 10f);
        }

        void Update()
        {
            transform.position += Vector3.up * Mathf.Sin(Time.time * Speed + seed) * Time.deltaTime * 0.24f;
            transform.Rotate(0, 0, Speed * 12f * Time.deltaTime);
        }
    }

    public static class RuntimeArt
    {
        public static readonly Color GridColor = new Color(0.20f, 0.12f, 0.065f, 0.92f);
        public static readonly Color SpecialBlockColor = new Color(1f, 0.92f, 0.34f, 1f);
        static Sprite boardFrameSprite;
        static Sprite roundedWoodSprite;
        static Sprite woodBackdropSprite;
        static Sprite blurredWoodBackdropSprite;
        static Sprite pauseButtonSprite;
        static Sprite woodPanelSprite;
        static Sprite woodButtonSprite;
        static Sprite solidSprite;
        static Font displayFont;
        static AudioSource oneShotSource;
        static AudioClip uiSwitchClip;
        static AudioClip gameOverClip;

        public static void PlayUiSwitchSound()
        {
            if (uiSwitchClip == null)
                uiSwitchClip = Resources.Load<AudioClip>("BrickStacker/ui_switch");
            PlayGlobalClip(uiSwitchClip, 0.42f);
        }

        public static void PlayGameOverSound()
        {
            if (gameOverClip == null)
                gameOverClip = Resources.Load<AudioClip>("BrickStacker/game_over_negative");
            PlayGlobalClip(gameOverClip, 0.70f);
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

            displayFont = Resources.Load<Font>("BrickStacker/Moment Vintage");
            if (displayFont != null)
                return displayFont;

            displayFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return displayFont;
        }

        public static Sprite CreateBlockSprite()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var woodData = Resources.Load<TextAsset>("BrickStacker/board_frame_source") ?? Resources.Load<TextAsset>("BrickStacker/wood_background_source");
            Texture2D woodTexture = null;
            if (woodData != null)
            {
                woodTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!woodTexture.LoadImage(woodData.bytes))
                    woodTexture = null;
            }

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    bool edge = x < 2 || y < 2 || x > size - 3 || y > size - 3;
                    bool darkEdge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    bool shine = x > 4 && x < 14 && y > 21 && y < 27;
                    Color wood = woodTexture != null
                        ? woodTexture.GetPixelBilinear(0.36f + x / (float)size * 0.22f, 0.28f + y / (float)size * 0.24f)
                        : new Color(0.76f, 0.62f, 0.45f, 1f);
                    float luminance = wood.grayscale;
                    float fineGrain = (Mathf.PerlinNoise(x * 0.22f, y * 0.055f) - 0.5f) * 0.24f;
                    float longGrain = Mathf.Sin((x * 0.20f) + Mathf.PerlinNoise(y * 0.07f, x * 0.025f) * 2.8f) * 0.10f;
                    float streak = Mathf.Sin((x + y * 0.18f) * 0.72f) * 0.045f;
                    float value = Mathf.Clamp01(0.82f + (luminance - 0.5f) * 0.56f + fineGrain + longGrain + streak);
                    Color color = new Color(value, value, value, 1f);

                    if (shine)
                        color = Color.Lerp(color, Color.white, 0.36f);
                    if (edge)
                        color = Color.Lerp(color, new Color(0.30f, 0.30f, 0.30f, 1f), darkEdge ? 0.72f : 0.32f);

                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        public static Sprite CreatePauseButtonSprite()
        {
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

        static Material WoodMaterial()
        {
            var texture = Resources.Load<Texture2D>("BrickStacker/wood_background");
            if (texture == null)
                return Material(new Color(0.42f, 0.20f, 0.08f));

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader);
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            material.color = Color.white;
            return material;
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
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = Screen.width <= Screen.height ? 0f : 1f;
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

        public static void ApplySafeArea(RectTransform rect)
        {
            Rect safe = Screen.safeArea;
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
            scaler.matchWidthOrHeight = Screen.width <= Screen.height ? 0f : 1f;
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
}
