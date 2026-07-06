#if UNITY_EDITOR
using UnityEngine;

namespace TRnK.Toolkit
{
    // Project-scoped settings container for TRnK.Toolkit
    internal class TRnKSettings : ScriptableObject
    {
        public string startupScenePath;
        public bool activateLoadedAdditiveOnSelect = false;
        public int timeScaleMax = 10;
        public bool hideToolbar = false;
        public bool autoReenterPlayAfterClear = true;

        private const string SettingsFolder = "Assets/Plugins/TRnK/Toolkit/Editor";
        private const string AssetPath = SettingsFolder + "/TRnKSettings.asset";

        private static TRnKSettings s_instance;
        private static TRnKSettings s_transient;

        public static TRnKSettings GetOrCreate() =>
            EditorAssetUtils.GetOrCreateSettings(SettingsFolder, AssetPath, ref s_instance, ref s_transient);
    }
}
#endif