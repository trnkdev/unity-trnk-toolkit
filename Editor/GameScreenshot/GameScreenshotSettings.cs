#if UNITY_EDITOR
using UnityEngine;

namespace TRnK.Toolkit
{
    internal enum CaptureMode { GameView, SpecificCamera }

    internal class GameScreenshotSettings : ScriptableObject
    {
        public CaptureMode captureMode = CaptureMode.GameView;
        public int supersize = 2;
        public string saveFolder = "";
        public bool revealOnSave = true;

        private const string SettingsFolder = "Assets/Plugins/TRnK/Toolkit/Editor";
        private const string AssetPath = SettingsFolder + "/GameScreenshotSettings.asset";

        private static GameScreenshotSettings s_instance;
        private static GameScreenshotSettings s_transient;

        public static GameScreenshotSettings GetOrCreate() =>
            EditorAssetUtils.GetOrCreateSettings(SettingsFolder, AssetPath, ref s_instance, ref s_transient);
    }
}
#endif
