using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

namespace BrickStacker
{
    // Gửi điểm và đọc bảng xếp hạng Unity Leaderboards.
    // Leaderboard "weekly" tạo trên Unity Dashboard (đặt Reset schedule = hàng tuần) —
    // "cúp" = điểm CỘNG DỒN của mọi lần chơi (xem LevelProgress.TotalScore).
    // Gửi điểm là fire-and-forget: mất mạng chỉ log warning, không chặn gameplay.
    public static class LeaderboardsSync
    {
        public const string WeeklyLeaderboardId = "weekly";

        public static void SubmitScore(int score)
        {
            if (score <= 0)
                return;
            _ = SubmitScoreAsync(score);
        }

        static async Task SubmitScoreAsync(int score)
        {
            try
            {
                if (!await ServicesManager.EnsureSignedInAsync())
                    return;
                await LeaderboardsService.Instance.AddPlayerScoreAsync(WeeklyLeaderboardId, score);
                Debug.Log($"[Leaderboards] Đã gửi điểm {score} lên bảng xếp hạng.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Leaderboards] Không gửi được điểm: {e.Message}");
            }
        }

        // Trả về (top N, entry của người chơi hoặc null nếu chưa có điểm).
        // Ném exception khi lỗi mạng — UI bắt để hiện thông báo.
        public static async Task<(List<LeaderboardEntry> Top, LeaderboardEntry Me)> LoadWeeklyAsync(int limit)
        {
            if (!await ServicesManager.EnsureSignedInAsync())
                throw new InvalidOperationException("Chưa đăng nhập Unity Services");

            var page = await LeaderboardsService.Instance.GetScoresAsync(
                WeeklyLeaderboardId, new GetScoresOptions { Limit = limit });

            LeaderboardEntry me = null;
            try
            {
                me = await LeaderboardsService.Instance.GetPlayerScoreAsync(WeeklyLeaderboardId);
            }
            catch
            {
                // Người chơi chưa có điểm trên bảng — không phải lỗi.
            }

            return (page.Results, me);
        }

        // Tên hiển thị: bỏ hậu tố "#1234" Unity tự sinh; rỗng thì dùng tên chung.
        // Tên trên bảng chính là tên tài khoản, đã giữ nguyên dấu tiếng Việt lúc đặt.
        public static string DisplayName(LeaderboardEntry entry)
        {
            string name = entry != null ? entry.PlayerName : null;
            if (string.IsNullOrEmpty(name))
                return "Người chơi";
            int hash = name.IndexOf('#');
            return hash > 0 ? name.Substring(0, hash) : name;
        }
    }
}
