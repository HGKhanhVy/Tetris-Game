using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace BrickStacker.EditorTools
{
    /// <summary>
    /// One-click setup of a Unity Recorder preset tuned for capturing offline gameplay
    /// at portrait mobile resolution. Editor-only helper, never shipped in a build.
    /// </summary>
    public static class OfflineGameplayRecorder
    {
        private const string RecorderName = "Offline Gameplay";
        private const string OutputFolderName = "Recordings";
        private const string OutputFile = OutputFolderName + "/offline_gameplay_<Take>";
        private const int OutputWidth = 1080;
        private const int OutputHeight = 1920;
        private const float TargetFrameRate = 60f;

        [MenuItem("BrickStacker/Recording/Setup Offline Gameplay Recorder (1080x1920)")]
        public static void SetupRecorder()
        {
            CloseRecorderWindow();

            RecorderControllerSettings controllerSettings = RecorderControllerSettings.GetGlobalSettings();
            ConfigureController(controllerSettings);
            ConfigureMovieRecorder(GetOrCreateMovieRecorder(controllerSettings));
            controllerSettings.Save();

            var window = EditorWindow.GetWindow<RecorderWindow>();
            window.SetRecorderControllerSettings(controllerSettings);
            window.Show();

            Debug.Log($"[Recorder] Ready: {OutputWidth}x{OutputHeight} H.264 MP4 @ {TargetFrameRate}fps -> {OutputFolder}");
        }

        [MenuItem("BrickStacker/Recording/Open Output Folder")]
        public static void OpenOutputFolder()
        {
            string folder = OutputFolder;
            Directory.CreateDirectory(folder);
            EditorUtility.RevealInFinder(folder + Path.DirectorySeparatorChar);
        }

        private static string OutputFolder
        {
            get
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                return Path.Combine(projectRoot, OutputFolderName);
            }
        }

        private static void ConfigureController(RecorderControllerSettings controllerSettings)
        {
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
            controllerSettings.FrameRate = TargetFrameRate;
            // Slows the Editor down instead of dropping frames, so the clip stays smooth.
            controllerSettings.CapFrameRate = true;
        }

        private static MovieRecorderSettings GetOrCreateMovieRecorder(RecorderControllerSettings controllerSettings)
        {
            foreach (RecorderSettings existing in controllerSettings.RecorderSettings)
            {
                if (existing is MovieRecorderSettings movie && movie.name == RecorderName)
                {
                    return movie;
                }
            }

            var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movieSettings.name = RecorderName;
            controllerSettings.AddRecorderSettings(movieSettings);
            return movieSettings;
        }

        private static void ConfigureMovieRecorder(MovieRecorderSettings movieSettings)
        {
            movieSettings.Enabled = true;
            movieSettings.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
            };
            movieSettings.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = OutputWidth,
                OutputHeight = OutputHeight
            };
            movieSettings.CaptureAudio = true;
            movieSettings.OutputFile = OutputFile;
        }

        private static void CloseRecorderWindow()
        {
            if (!EditorWindow.HasOpenInstances<RecorderWindow>())
            {
                return;
            }

            EditorWindow.GetWindow<RecorderWindow>().Close();
        }
    }
}
