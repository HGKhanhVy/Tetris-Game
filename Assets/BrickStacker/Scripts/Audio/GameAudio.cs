using System.Collections.Generic;
using UnityEngine;

namespace BrickStacker
{
    // Dịch vụ âm thanh dùng chung cho cả game: MỘT nguồn nhạc nền (loop, sống xuyên scene) và
    // một nguồn hiệu ứng. Đặt tập trung ở đây để đổi nhạc/âm lượng chỉ sửa một chỗ, và để nhạc
    // nền không bị phát lại từ đầu mỗi lần đổi scene (menu -> chọn màn dùng chung một bài).
    //
    // File nằm ở Resources/BrickStacker/Audio, đặt tên theo quy ước:
    //   snd-*  = nhạc nền của một màn hình      fx-*  = hiệu ứng phát một lần
    public static class GameAudio
    {
        public const string MusicMenu = "snd-menu-bg";
        public const string MusicGameplay = "snd-gameplay-bg";
        public const string MusicWin = "snd-win-bg";
        public const string MusicLose = "snd-lose-bg";

        public const string FxClick = "fx-click-button";
        public const string FxWin = "fx-win";
        public const string FxLose = "fx-lose";
        public const string FxHighRisk = "fx-high-risk";

        const string AudioFolder = "BrickStacker/Audio/";
        const float MusicVolume = 0.32f;
        const float FxVolume = 0.85f;

        static AudioSource musicSource;
        static AudioSource fxSource;
        static string currentMusic = "";
        static readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

        // Đổi nhạc nền. Gọi lại với ĐÚNG bài đang phát thì bỏ qua (không cắt ngang, không phát
        // lại từ đầu) — nhờ vậy đi giữa các màn hình dùng chung một bài nghe liền mạch.
        public static void PlayMusic(string clipName, float volume = MusicVolume)
        {
            if (string.IsNullOrEmpty(clipName))
                return;
            if (currentMusic == clipName && musicSource != null && musicSource.isPlaying)
                return;

            var clip = LoadClip(clipName);
            if (clip == null)
                return;

            EnsureSources();
            currentMusic = clipName;
            musicSource.clip = clip;
            musicSource.volume = volume;
            musicSource.Play();
        }

        public static void StopMusic()
        {
            currentMusic = "";
            if (musicSource != null)
                musicSource.Stop();
        }

        // Tạm dừng game thì nhạc nền dừng theo, bỏ tạm dừng thì chạy tiếp đúng chỗ cũ.
        public static void SetMusicPaused(bool paused)
        {
            if (musicSource == null)
                return;
            if (paused)
                musicSource.Pause();
            else
                musicSource.UnPause();
        }

        // Hiệu ứng phát một lần, chồng lên nhạc nền.
        public static void PlayFx(string clipName, float volume = FxVolume)
        {
            var clip = LoadClip(clipName);
            if (clip == null)
                return;

            EnsureSources();
            fxSource.PlayOneShot(clip, volume);
        }

        static AudioClip LoadClip(string clipName)
        {
            if (clipCache.TryGetValue(clipName, out var cached))
                return cached;

            var clip = Resources.Load<AudioClip>(AudioFolder + clipName);
            if (clip == null)
                Debug.LogWarning("BLOCKFALL thiếu file âm thanh: Resources/" + AudioFolder + clipName);
            else if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();

            clipCache[clipName] = clip;   // nhớ cả trường hợp null để khỏi tìm lại mỗi lần gọi
            return clip;
        }

        // Tạo một lần rồi giữ qua mọi scene; các scene chỉ việc gọi PlayMusic/PlayFx.
        static void EnsureSources()
        {
            if (musicSource != null && fxSource != null)
                return;

            var host = new GameObject("Blockfall Audio");
            Object.DontDestroyOnLoad(host);

            musicSource = host.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = MusicVolume;

            fxSource = host.AddComponent<AudioSource>();
            fxSource.loop = false;
            fxSource.playOnAwake = false;
            fxSource.spatialBlend = 0f;
        }
    }
}
