using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

namespace BrickStacker
{
    // Đồng bộ LevelProgress (PlayerPrefs) với Unity Cloud Save.
    // - Khi đăng nhập xong: kéo dữ liệu cloud về, merge với local theo hướng lấy giá trị cao hơn
    //   (level mở khóa, sao, điểm, xu) để không mất tiến trình dù chơi trên thiết bị nào.
    // - Sau mỗi lần qua màn: gọi Push() để đẩy toàn bộ tiến trình local lên cloud.
    // Mọi lỗi mạng chỉ log warning — game vẫn chạy offline bình thường với PlayerPrefs.
    public static class CloudSaveSync
    {
        const string ProgressKey = "PROGRESS";

        // Bắn khi dữ liệu cloud kéo về làm thay đổi tiến trình local (UI nên refresh).
        public static event Action ProgressPulled;

        [Serializable]
        class ProgressData
        {
            public int unlockedLevel;
            public int coins;
            public int totalLines;
            public int[] stars;      // index 0 = level 1
            public int[] bestScores; // index 0 = level 1
        }

        static bool _pushing;
        static bool _pushQueued;

        // AfterSceneLoad: EnsureSignedInAsync kích hoạt UnityServices.InitializeAsync,
        // phải chạy sau khi các package UGS đăng ký xong (BeforeSceneLoad) — xem ServicesManager.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            _ = PullAsync();
        }

        // Đẩy tiến trình local lên cloud (fire-and-forget, gộp các lần gọi liên tiếp).
        public static void Push()
        {
            if (_pushing)
            {
                _pushQueued = true;
                return;
            }
            _ = PushAsync();
        }

        static async Task PullAsync()
        {
            if (!await ServicesManager.EnsureSignedInAsync())
                return;

            try
            {
                var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
                    new HashSet<string> { ProgressKey });

                bool localChanged = false;
                bool cloudBehind = true;

                if (result.TryGetValue(ProgressKey, out var item))
                {
                    var cloud = JsonUtility.FromJson<ProgressData>(item.Value.GetAs<string>());
                    if (cloud != null)
                    {
                        var local = CaptureLocal();
                        localChanged = MergeIntoLocal(cloud);
                        cloudBehind = IsAhead(local, cloud);
                    }
                }

                if (localChanged)
                {
                    PlayerPrefs.Save();
                    ProgressPulled?.Invoke();
                    Debug.Log("[CloudSave] Đã cập nhật tiến trình từ cloud.");
                }

                // Cloud chưa có dữ liệu hoặc local mới hơn → đẩy lên.
                if (cloudBehind)
                    Push();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CloudSave] Không kéo được dữ liệu cloud: {e.Message}");
            }
        }

        static async Task PushAsync()
        {
            _pushing = true;
            try
            {
                do
                {
                    _pushQueued = false;

                    if (!await ServicesManager.EnsureSignedInAsync())
                        return;

                    string json = JsonUtility.ToJson(CaptureLocal());
                    await CloudSaveService.Instance.Data.Player.SaveAsync(
                        new Dictionary<string, object> { { ProgressKey, json } });
                }
                while (_pushQueued);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CloudSave] Không đẩy được dữ liệu lên cloud: {e.Message}");
            }
            finally
            {
                _pushing = false;
            }
        }

        static ProgressData CaptureLocal()
        {
            var data = new ProgressData
            {
                unlockedLevel = LevelProgress.CurrentUnlockedLevel,
                coins = LevelProgress.Coins,
                totalLines = PlayerPrefs.GetInt(LevelProgress.TotalLinesClearedKey, 0),
                stars = new int[LevelProgress.MaxLevels],
                bestScores = new int[LevelProgress.MaxLevels]
            };

            for (int level = 1; level <= LevelProgress.MaxLevels; level++)
            {
                data.stars[level - 1] = LevelProgress.StarsForLevel(level);
                data.bestScores[level - 1] = PlayerPrefs.GetInt(LevelProgress.BestScoreKeyForLevel(level), 0);
            }

            return data;
        }

        // Ghi các giá trị cloud cao hơn vào PlayerPrefs. Trả về true nếu local thay đổi.
        static bool MergeIntoLocal(ProgressData cloud)
        {
            bool changed = false;

            changed |= RaiseInt(LevelProgress.UnlockedLevelKey,
                Mathf.Clamp(cloud.unlockedLevel, 1, LevelProgress.MaxLevels), 1);
            changed |= RaiseInt(LevelProgress.CoinsKey, cloud.coins, 0);
            changed |= RaiseInt(LevelProgress.TotalLinesClearedKey, cloud.totalLines, 0);

            for (int level = 1; level <= LevelProgress.MaxLevels; level++)
            {
                int index = level - 1;
                if (cloud.stars != null && index < cloud.stars.Length)
                    changed |= RaiseInt(LevelProgress.StarKey(level), Mathf.Clamp(cloud.stars[index], 0, 3), 0);
                if (cloud.bestScores != null && index < cloud.bestScores.Length)
                    changed |= RaiseInt(LevelProgress.BestScoreKeyForLevel(level), cloud.bestScores[index], 0);
            }

            return changed;
        }

        static bool RaiseInt(string key, int cloudValue, int defaultValue)
        {
            if (cloudValue <= PlayerPrefs.GetInt(key, defaultValue))
                return false;
            PlayerPrefs.SetInt(key, cloudValue);
            return true;
        }

        // Local (chụp trước khi merge) có giá trị nào vượt cloud không?
        static bool IsAhead(ProgressData local, ProgressData cloud)
        {
            if (local.unlockedLevel > cloud.unlockedLevel) return true;
            if (local.coins > cloud.coins) return true;
            if (local.totalLines > cloud.totalLines) return true;

            for (int i = 0; i < LevelProgress.MaxLevels; i++)
            {
                int cloudStars = cloud.stars != null && i < cloud.stars.Length ? cloud.stars[i] : 0;
                int cloudScore = cloud.bestScores != null && i < cloud.bestScores.Length ? cloud.bestScores[i] : 0;
                if (local.stars[i] > cloudStars) return true;
                if (local.bestScores[i] > cloudScore) return true;
            }

            return false;
        }
    }
}
