using System;
using UnityEngine;

namespace BrickStacker
{
    public static class GameSession
    {
        public static int SelectedLevel = 1;
        public static int JourneyLevel = 1;
        // Bật khi vào màn chơi ở chế độ HƯỚNG DẪN (tutorial tương tác). Reset về false ngay
        // khi vào game để các màn thường sau đó không bị coi là tutorial.
        public static bool IsTutorial = false;
    }

    public static class LevelProgress
    {
        public const int MaxLevels = 20;
        // Giá trị chuỗi key giữ nguyên tên "TOWER" cũ để không mất save hiện có.
        public const string UnlockedLevelKey = "BLOCKFALL_TOWER_UNLOCKED_FLOOR";
        public const string CoinsKey = "BLOCKFALL_COINS";
        public const string TotalLinesClearedKey = "BLOCKFALL_TOTAL_LINES_CLEARED";
        // CÚP = điểm CỘNG DỒN của mọi lần chơi (mỗi lần qua màn / thua đều cộng phần điểm
        // kiếm được trong lần đó). Khác với tổng điểm-cao-nhất-mỗi-màn dùng trước đây.
        public const string TotalScoreKey = "BLOCKFALL_TOTAL_SCORE";

        // Mốc reset tiến trình: tăng số này để ÉP reset 1 lần cho MỌI máy (bản phát hành).
        // Máy có version thấp hơn sẽ tự reset màn+điểm+sao+XU và ghi đè cloud.
        public const int ProgressResetVersion = 2;
        public const string ProgressResetVersionKey = "BLOCKFALL_PROGRESS_RESET_VERSION";

        // Khóa theo tiến trình: chỉ mở tới màn cao nhất đã qua (mặc định 1 = chỉ mở màn đầu).
        public static int CurrentUnlockedLevel =>
            Mathf.Clamp(PlayerPrefs.GetInt(UnlockedLevelKey, 1), 1, MaxLevels);

        // Chạy trước mọi scene: đảm bảo reset 1 lần được áp dụng trước khi game đọc tiến trình.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureResetBeforeLoad()
        {
            ApplyOneTimeResetIfNeeded();
            MigrateTotalScoreIfNeeded();
        }

        // Máy đã chơi từ bản cũ (chưa có key cộng dồn): lấy tổng điểm cao nhất các màn làm mốc
        // khởi đầu để không ai bị mất cúp khi đổi cách tính.
        static void MigrateTotalScoreIfNeeded()
        {
            if (PlayerPrefs.HasKey(TotalScoreKey))
                return;

            PlayerPrefs.SetInt(TotalScoreKey, TotalBestScore());
            PlayerPrefs.Save();
        }

        // Reset 1 lần theo ProgressResetVersion: mở khóa về màn 1, xoá điểm + sao mọi màn + XU.
        // Máy mới cài phải bắt đầu với 0 xu (xu chỉ đến từ phần thưởng qua màn), nên xu cũng bị
        // xoá ở mốc reset này. Đặt cờ version để không reset lại ở lần sau.
        public static void ApplyOneTimeResetIfNeeded()
        {
            if (PlayerPrefs.GetInt(ProgressResetVersionKey, 0) >= ProgressResetVersion)
                return;

            PlayerPrefs.DeleteKey(UnlockedLevelKey); // -> CurrentUnlockedLevel về mặc định 1
            PlayerPrefs.DeleteKey(CoinsKey);         // -> Coins về mặc định 0
            PlayerPrefs.DeleteKey(TotalScoreKey);    // -> cúp về 0
            PlayerPrefs.DeleteKey(TotalLinesClearedKey);
            for (int level = 1; level <= MaxLevels; level++)
            {
                PlayerPrefs.DeleteKey(StarKey(level));
                PlayerPrefs.DeleteKey(BestScoreKeyForLevel(level));
            }
            PlayerPrefs.SetInt(ProgressResetVersionKey, ProgressResetVersion);
            PlayerPrefs.Save();
            Debug.Log("[Progress] Đã reset tiến trình (màn + điểm + sao + xu) về mặc định.");
        }

        public static string StarKey(int level)
        {
            return "BLOCKFALL_TOWER_FLOOR_" + Mathf.Clamp(level, 1, MaxLevels) + "_STARS";
        }

        public static string BestScoreKeyForLevel(int level)
        {
            return "BLOCKFALL_TOWER_LEVEL_" + Mathf.Clamp(level, 1, MaxLevels) + "_BEST_SCORE";
        }

        public static int Coins => PlayerPrefs.GetInt(CoinsKey, 0);

        public static void AddCoins(int amount)
        {
            PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, Coins + amount));
        }

        // Cúp hiện có (điểm cộng dồn mọi lần chơi).
        public static int TotalScore => PlayerPrefs.GetInt(TotalScoreKey, 0);

        // Cộng điểm vừa kiếm được vào cúp. Gọi MỘT lần cho mỗi lần chơi (thắng hoặc thua).
        public static void AddScore(int amount)
        {
            if (amount <= 0)
                return;
            PlayerPrefs.SetInt(TotalScoreKey, TotalScore + amount);
        }

        public static int StarsForLevel(int level)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(StarKey(level), 0), 0, 3);
        }

        public static void SaveLevelResult(int level, int stars)
        {
            level = Mathf.Clamp(level, 1, MaxLevels);
            stars = Mathf.Clamp(stars, 1, 3);
            if (stars > StarsForLevel(level))
                PlayerPrefs.SetInt(StarKey(level), stars);

            PlayerPrefs.SetInt(UnlockedLevelKey, Mathf.Max(CurrentUnlockedLevel, Mathf.Min(MaxLevels, level + 1)));
            PlayerPrefs.Save();
        }

        public static void SaveLevelBestScore(int level, int score)
        {
            string key = BestScoreKeyForLevel(level);
            if (score > PlayerPrefs.GetInt(key, 0))
                PlayerPrefs.SetInt(key, score);
        }

        // Tổng điểm CAO NHẤT của tất cả các màn. Không còn là "cúp" (xem TotalScore) — giữ lại
        // để làm mốc chuyển đổi cho máy chơi từ bản cũ.
        public static int TotalBestScore()
        {
            int total = 0;
            for (int level = 1; level <= MaxLevels; level++)
                total += PlayerPrefs.GetInt(BestScoreKeyForLevel(level), 0);
            return total;
        }
    }

    [Serializable]
    public class LevelRules
    {
        public Color BackgroundB;
        public float FallInterval;
        public float SpeedRampSeconds;
        public float MaxFallSpeedMultiplier = 1f;
        public int GarbageEveryPieces;
        public float SurpriseGarbageChance;
        public int ScoreMultiplier;
        public int ForcedPieceType = -1;
        public bool AllowSpecialBlocks = true;
        // GD v3: bật cơ chế ghép cụm tài nguyên thay cho xóa hàng ngang. Mặc định tắt để
        // giữ nguyên game hiện tại cho tới khi cơ chế mới được playtest.
        public bool UseResourceClusters = false;
        public bool GhostPreview = true;
        public bool FastBlocks;
        public bool HasStoneBlocks;
        public bool HasFixedObstacles;
        public int RotationLimit;
        public int RisingDangerSeconds;
        public int CoinReward = 50;
        public TacticalLevelData TacticalData;

        public static LevelRules CreateJourney(int level)
        {
            int stage = Mathf.Max(1, level);
            var rules = new LevelRules
            {
                BackgroundB = new Color(0.08f, 0.22f, 0.18f),
                FallInterval = Mathf.Max(0.34f, 0.80f - Mathf.Min(stage - 1, 30) * 0.010f),
                // Every level ramps up gently over time; pattern levels may override
                // with a stronger ramp in ApplyLevelConfig.
                SpeedRampSeconds = 150f,
                MaxFallSpeedMultiplier = 1.6f,
                GarbageEveryPieces = 0,
                SurpriseGarbageChance = 0f,
                ScoreMultiplier = 1,
                AllowSpecialBlocks = true,
                // Bóng mờ (ghost) hiện SUỐT ván ở mọi màn — người chơi luôn biết khối sẽ đáp đâu.
                GhostPreview = true,
                FastBlocks = stage >= 15,
                RotationLimit = 0,
                RisingDangerSeconds = 0,
                CoinReward = 45 + stage * 5
            };

            ApplyLevelConfig(rules, stage);
            rules.TacticalData = TacticalLevelData.Create(stage);
            rules.FallInterval = rules.TacticalData.InitialFallSpeed;
            rules.CoinReward = rules.TacticalData.CoinReward;
            // GD v3: bật ghép cụm tài nguyên cho MỌI level (offline + online 1v1) — cơ chế cụm v3
            // đã thay hẳn xóa-hàng-ngang cũ.
            rules.UseResourceClusters = true;
            return rules;
        }

        static void ApplyLevelConfig(LevelRules rules, int level)
        {
            int pattern = (level - 1) % 10;
            if (pattern == 5)
            {
                rules.SpeedRampSeconds = Mathf.Max(80f, 180f - level * 3f);
                rules.MaxFallSpeedMultiplier = Mathf.Min(2.8f, 1.35f + level * 0.04f);
            }
            else if (pattern == 6)
                rules.SurpriseGarbageChance = Mathf.Min(0.20f, 0.08f + level * 0.004f);
            else if (pattern == 8)
            {
                rules.SpeedRampSeconds = 150f;
                rules.MaxFallSpeedMultiplier = Mathf.Min(2.7f, 1.45f + level * 0.035f);
            }
            else if (pattern == 9)
                rules.HasStoneBlocks = true;

            if (level >= 12 && level % 4 == 0)
                rules.RotationLimit = Mathf.Max(10, 24 - level / 2);

            if (level >= 18 && level % 6 == 0)
                rules.RisingDangerSeconds = Mathf.Clamp(34 - level / 2, 16, 34);

            if (level >= 24 && level % 8 == 0)
                rules.HasFixedObstacles = true;
        }
    }
}
