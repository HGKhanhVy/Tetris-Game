using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

namespace BrickStacker
{
    // Gửi điểm và đọc bảng xếp hạng Unity Leaderboards.
    // Leaderboard "daily" tạo trên Unity Dashboard — điểm cao nhất trong ngày của mỗi người chơi.
    // Gửi điểm là fire-and-forget: mất mạng chỉ log warning, không chặn gameplay.
    public static class LeaderboardsSync
    {
        public const string DailyLeaderboardId = "daily";

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
                await LeaderboardsService.Instance.AddPlayerScoreAsync(DailyLeaderboardId, score);
                Debug.Log($"[Leaderboards] Đã gửi điểm {score} lên bảng xếp hạng.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Leaderboards] Không gửi được điểm: {e.Message}");
            }
        }

        // Trả về (top N, entry của người chơi hoặc null nếu chưa có điểm).
        // Ném exception khi lỗi mạng — UI bắt để hiện thông báo.
        public static async Task<(List<LeaderboardEntry> Top, LeaderboardEntry Me)> LoadDailyAsync(int limit)
        {
            if (!await ServicesManager.EnsureSignedInAsync())
                throw new InvalidOperationException("Chưa đăng nhập Unity Services");

            var page = await LeaderboardsService.Instance.GetScoresAsync(
                DailyLeaderboardId, new GetScoresOptions { Limit = limit });

            LeaderboardEntry me = null;
            try
            {
                me = await LeaderboardsService.Instance.GetPlayerScoreAsync(DailyLeaderboardId);
            }
            catch
            {
                // Người chơi chưa có điểm trên bảng — không phải lỗi.
            }

            return (page.Results, me);
        }

        // Tên hiển thị: bỏ hậu tố "#1234" Unity tự sinh; rỗng thì dùng tên chung.
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
