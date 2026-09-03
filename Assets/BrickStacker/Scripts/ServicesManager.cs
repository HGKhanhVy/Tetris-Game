using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace BrickStacker
{
    // Khởi tạo Unity Gaming Services + đăng nhập ẩn danh khi game khởi động.
    // Các service khác (Cloud Save, Leaderboards, Multiplayer) gọi EnsureSignedInAsync()
    // trước khi dùng để chắc chắn đã đăng nhập xong.
    public static class ServicesManager
    {
        public static bool IsInitialized => UnityServices.State == ServicesInitializationState.Initialized;
        public static bool IsSignedIn => IsInitialized && AuthenticationService.Instance.IsSignedIn;
        public static string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : null;

        // Tên hiển thị (đã bỏ hậu tố "#1234" Unity tự sinh). Rỗng = chưa đặt tên.
        public static string PlayerName { get; private set; } = "";

        // Tên đã lưu ở máy. Dùng để: (1) hiện tên ngay cả khi đang mất mạng, (2) đẩy lại lên máy chủ
        // ở lần đăng nhập sau nếu lần lưu trước rớt mạng — người chơi đặt tên một lần là xong.
        const string LocalNameKey = "BLOCKFALL_PLAYER_NAME";

        static string LocalName
        {
            get => PlayerPrefs.GetString(LocalNameKey, "");
            set
            {
                PlayerPrefs.SetString(LocalNameKey, value ?? "");
                PlayerPrefs.Save();
            }
        }


        public static event Action SignedIn;

        static Task<bool> _signInTask;

        // AfterSceneLoad để các package UGS (Leaderboards đăng ký ở BeforeSceneLoad)
        // kịp đăng ký trước khi InitializeAsync chạy — nếu không LeaderboardsService.Instance
        // sẽ ném "has not been initialized" dù Core đã Initialized.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            _ = EnsureSignedInAsync();
        }

        // Trả về task đăng nhập đang chạy; nếu lần trước thất bại (mất mạng...) thì thử lại.
        public static Task<bool> EnsureSignedInAsync()
        {
            if (_signInTask == null || _signInTask.IsFaulted)
                _signInTask = InitAndSignInAsync();
            return _signInTask;
        }

        static async Task<bool> InitAndSignInAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();

                Debug.Log($"[Services] Đã đăng nhập. PlayerId: {AuthenticationService.Instance.PlayerId}");

                try
                {
                    // autoGenerate: false — KHÔNG để Unity tự sinh tên random;
                    // chưa đặt tên thì PlayerName rỗng, game sẽ hỏi người chơi.
                    PlayerName = StripNameSuffix(await AuthenticationService.Instance.GetPlayerNameAsync(false));

                    // Máy có tên nhưng máy chủ chưa có (lần lưu trước rớt mạng) -> đẩy lại.
                    if (string.IsNullOrEmpty(PlayerName) && !string.IsNullOrEmpty(LocalName))
                        await SetPlayerNameAsync(LocalName);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Services] Không đọc được tên người chơi: {e.Message}");
                    if (string.IsNullOrEmpty(PlayerName))
                        PlayerName = LocalName;   // vẫn hiện được tên khi mạng chập chờn
                }

                SignedIn?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Services] Không thể đăng nhập Unity Services: {e.Message}");
                return false;
            }
        }

        // Đặt tên hiển thị (leaderboard + trận 1v1 dùng chung tên này).
        // Unity không cho khoảng trắng — thay bằng "_".
        // rawName: đúng những gì người chơi gõ. Chỉ đổi khoảng trắng thành '_' (Unity cấm
        // khoảng trắng) — dấu tiếng Việt giữ nguyên nên tên trên bảng xếp hạng đúng như họ đặt.
        public static async Task<bool> SetPlayerNameAsync(string rawName)
        {
            string name = PlayerNameFormatter.Sanitize(rawName);
            if (!PlayerNameFormatter.IsValid(name))
                return false;

            // Lưu ở máy TRƯỚC: mất mạng thì tên vẫn còn và được đẩy lên ở lần đăng nhập sau.
            LocalName = name;
            if (string.IsNullOrEmpty(PlayerName))
                PlayerName = name;

            if (!await EnsureSignedInAsync())
                return false;

            try
            {
                string result = await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
                PlayerName = StripNameSuffix(result);
                LocalName = PlayerName;
                Debug.Log($"[Services] Đã đổi tên thành: {PlayerName}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Services] Không đổi được tên: {e.Message}");
                return false;
            }
        }

        static string StripNameSuffix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";
            int hash = name.IndexOf('#');
            return hash > 0 ? name.Substring(0, hash) : name;
        }
    }
}
