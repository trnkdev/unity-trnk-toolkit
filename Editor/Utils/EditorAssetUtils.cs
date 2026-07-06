#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace TRnK.Toolkit
{
    public static class EditorAssetUtils
    {
        /// <summary>
        /// Loads a settings asset, creating it (and its folder chain) on first use.
        /// Safe against mid-import races: never recreates an asset that already exists
        /// on disk, and caches the instance so repeated callers cannot re-enter creation.
        /// </summary>
        public static T GetOrCreateSettings<T>(string settingsFolder, string assetPath,
            ref T instance, ref T transient, Action<T> initialize = null) where T : ScriptableObject
        {
            if (instance != null) return instance;

            instance = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (instance != null) return instance;

            // Mid-import the asset can exist on disk but not be loadable yet — recreating
            // it would clobber it. Hand out a temporary instance and retry next call.
            if (System.IO.File.Exists(assetPath))
            {
                if (transient == null)
                {
                    transient = ScriptableObject.CreateInstance<T>();
                    transient.hideFlags = HideFlags.HideAndDontSave;
                    initialize?.Invoke(transient);
                }
                return transient;
            }

            EnsureFolderPath(settingsFolder);

            var created = ScriptableObject.CreateInstance<T>();
            initialize?.Invoke(created);
            AssetDatabase.CreateAsset(created, assetPath);
            SaveAndRefresh();
            instance = created;
            return instance;
        }

        // Creates every missing folder segment in the given Assets-relative path.
        public static void EnsureFolderPath(string assetRelativePath)
        {
            string[] parts = assetRelativePath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                // IsValidFolder alone is unreliable mid-import: a folder created moments
                // ago is not registered yet, and CreateFolder on an existing folder
                // uniquifies ("Plugins 1"). The on-disk check closes that window.
                if (!AssetDatabase.IsValidFolder(next) && !System.IO.Directory.Exists(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public static void SaveAndRefresh()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void MarkDirtyAndSave(UnityEngine.Object obj)
        {
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
