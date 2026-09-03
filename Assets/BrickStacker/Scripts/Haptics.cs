using Lofelt.NiceVibrations;
using UnityEngine;

namespace BrickStacker
{
    // Rung phản hồi (haptics) tập trung, dùng Feel/NiceVibrations để tăng "game feel".
    // Gọi 1 dòng tại các mốc gameplay (khóa khối, ăn cụm, combo, thắng/thua, nút bấm).
    // - Bọc trong 1 nơi (DRY): đổi tông rung toàn game chỉ sửa ở đây.
    // - Chỉ chạy trên thiết bị di động thật; Editor/PC là no-op để không gây lỗi.
    // - Có công tắc bật/tắt lưu vào PlayerPrefs cho phần Cài đặt.
    public static class Haptics
    {
        const string EnabledKey = "BLOCKFALL_HAPTICS_ENABLED";

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            set => PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
        }

        public static void Selection() => Play(HapticPatterns.PresetType.Selection);   // chạm nút / chọn
        public static void Light() => Play(HapticPatterns.PresetType.LightImpact);     // khóa khối
        public static void Soft() => Play(HapticPatterns.PresetType.SoftImpact);       // chạm nhẹ
        public static void Medium() => Play(HapticPatterns.PresetType.MediumImpact);   // ăn cụm
        public static void Heavy() => Play(HapticPatterns.PresetType.HeavyImpact);     // combo lớn / đòn mạnh
        public static void Success() => Play(HapticPatterns.PresetType.Success);       // thắng màn
        public static void Warning() => Play(HapticPatterns.PresetType.Warning);       // cảnh báo nguy hiểm
        public static void Failure() => Play(HapticPatterns.PresetType.Failure);       // thua

        // Rung theo cấp combo: cấp càng cao rung càng mạnh (nhẹ -> vừa -> mạnh).
        public static void Combo(int chain)
        {
            if (chain >= 4)
                Heavy();
            else if (chain >= 2)
                Medium();
            else
                Light();
        }

        static void Play(HapticPatterns.PresetType preset)
        {
            if (!Enabled || preset == HapticPatterns.PresetType.None)
                return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            try { HapticPatterns.PlayPreset(preset); } catch { }
#endif
        }
    }
}
