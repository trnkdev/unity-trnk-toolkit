#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TRnK.Toolkit
{
    /// <summary>
    /// Utility methods for creating project folders under a chosen Assets-relative root.
    /// Editor-only.
    /// </summary>
    internal static class FoldersTool
    {
        private const string DefaultRoot = "Assets/Project";

        public static void CreateFolders(FoldersSettings settings, List<string> folderNames)
        {
            if (settings == null) settings = FoldersSettings.LoadOrCreate();
            string root = SanitizeRoot(settings.RootPath);

            // Determine if anything is missing first
            bool rootMissing = !AssetDatabase.IsValidFolder(root);
            var distinctNames = folderNames.Distinct().Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).ToList();
            bool anyMissing = rootMissing || distinctNames.Any(n => !AssetDatabase.IsValidFolder(CombineUnityPath(root, n)));

            if (!anyMissing)
            {
                EditorUtility.DisplayDialog("Getting Started", "All selected folders already exist.", "OK");
                return;
            }

            bool createdAny = false;
            if (rootMissing)
            {
                createdAny |= EnsureFolder(root);
            }

            foreach (var name in distinctNames)
            {
                string folderPath = CombineUnityPath(root, name);
                createdAny |= EnsureFolder(folderPath);
            }

            if (createdAny)
            {
                AssetDatabase.Refresh();
            }
            EditorUtility.DisplayDialog("Getting Started", "Requested folders have been created.", "OK");
        }

        public static string CombineUnityPath(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) a = DefaultRoot;
            if (string.IsNullOrEmpty(b)) return a.Replace("\\", "/");
            var path = a.Trim().TrimEnd('/', '\\') + "/" + b.Trim().TrimStart('/', '\\');
            return path.Replace("\\", "/");
        }

        public static string AbsoluteToAssetsRelative(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return null;
            absolutePath = absolutePath.Replace("\\", "/");
            string dataPath = Application.dataPath.Replace("\\", "/");
            if (!absolutePath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase)) return null;
            string rel = "Assets" + absolutePath.Substring(dataPath.Length);
            return rel.Replace("\\", "/");
        }

        private static string SanitizeRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) return DefaultRoot;
            root = root.Replace("\\", "/").Trim();
            if (!root.StartsWith("Assets")) root = DefaultRoot; // enforce Assets-relative
            return root.TrimEnd('/');
        }

        private static bool EnsureFolder(string assetsRelativePath)
        {
            assetsRelativePath = assetsRelativePath.Replace("\\", "/");
            if (AssetDatabase.IsValidFolder(assetsRelativePath)) return false;

            // Make sure all segments exist
            string[] parts = assetsRelativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || parts[0] != "Assets") return false;

            string current = parts[0]; // Assets
            bool createdAny = false;
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                // IsValidFolder alone is unreliable mid-refresh: CreateFolder on a folder
                // that already exists on disk uniquifies ("Folder 1") instead of no-oping.
                if (!AssetDatabase.IsValidFolder(next) && !System.IO.Directory.Exists(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                    createdAny = true;
                }
                current = next;
            }
            return createdAny;
        }
    }
}
#endif
