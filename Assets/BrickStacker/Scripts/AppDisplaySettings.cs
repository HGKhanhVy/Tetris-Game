using UnityEngine;

namespace BrickStacker
{
    // Thiết lập hiển thị, chạy một lần lúc app khởi động trước mọi scene.
    public static class AppDisplaySettings
    {
        const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            // vSync phải tắt thì targetFrameRate mới có hiệu lực trên Editor/Standalone (Unity bỏ qua
            // targetFrameRate khi vSyncCount khác 0, và các mức Quality từ Medium trở lên đang để 1).
            // Trên iOS/Android thì ngược lại — vSyncCount bị bỏ qua — nên dòng này chỉ để hai môi
            // trường chạy cùng một khung hình, tránh test trên Editor ra kết quả khác máy thật.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
