#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TRnK.Toolkit
{
#if ODIN_INSPECTOR
    [HideMonoScript]
#endif
    internal class TmpFontReplacerSettings : ScriptableObject
    {
#if ODIN_INSPECTOR
        [ReadOnly] public TMP_FontAsset lastFont;
        [ReadOnly] public Material lastMaterial;
        [ReadOnly] public List<DefaultAsset> excludedFolders = new();
        [ReadOnly] public bool lastScanCompleted;
        [ReadOnly] public long lastScanTimestamp;
        [ReadOnly] public List<SerializedAssetEntry> lastScanScenes = new();
        [ReadOnly] public List<SerializedAssetEntry> lastScanPrefabs = new();
#else
        public TMP_FontAsset      lastFont;
        public Material           lastMaterial;
        public List<DefaultAsset> excludedFolders = new();
        public bool lastScanCompleted;
        public long lastScanTimestamp;
        public List<SerializedAssetEntry> lastScanScenes  = new();
        public List<SerializedAssetEntry> lastScanPrefabs = new();
#endif

        [System.Serializable]
        internal class SerializedObjectEntry
        {
            public string HierarchyPath;
            public string Name;
            public string FontName;
            public string MaterialName;
        }

        [System.Serializable]
        internal class SerializedAssetEntry
        {
            public string Path;
            public string Name;
            public List<SerializedObjectEntry> Objects = new();
        }

        private const string SettingsFolder = "Assets/Plugins/TRnK/Toolkit/Editor";
        private const string AssetPath = SettingsFolder + "/TmpFontReplacerSettings.asset";

        private static TmpFontReplacerSettings s_instance;
        private static TmpFontReplacerSettings s_transient;

        public static TmpFontReplacerSettings GetOrCreate() =>
            EditorAssetUtils.GetOrCreateSettings(SettingsFolder, AssetPath, ref s_instance, ref s_transient);

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
    }
}
#endif