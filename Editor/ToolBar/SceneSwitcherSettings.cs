#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TRnK.Toolkit
{
    [Serializable]
    internal class SceneGroup
    {
        public string label = "Group";
        public List<string> scenePaths = new List<string>();
    }

#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.HideMonoScript]
#endif
    internal class SceneSwitcherSettings : ScriptableObject
    {
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.ReadOnly]
#endif
        public List<SceneGroup> groups = new List<SceneGroup>();

        private const string SettingsFolder = "Assets/Plugins/TRnK/Toolkit/Editor";
        private const string AssetPath = SettingsFolder + "/SceneSwitcherSettings.asset";

        private static SceneSwitcherSettings s_instance;
        private static SceneSwitcherSettings s_transient;

        public static SceneSwitcherSettings GetOrCreate() =>
            EditorAssetUtils.GetOrCreateSettings(SettingsFolder, AssetPath, ref s_instance, ref s_transient);
    }
}
#endif