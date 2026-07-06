#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TRnK.Toolkit
{
    [Serializable]
    internal class PackagesSettings : ScriptableObject
    {
        private const string AssetDir = "Assets/Plugins/TRnK/Toolkit/Editor";
        private const string AssetName = "PackagesSettings.asset";

        private static readonly string[] DefaultGitUrls =
        {
            "https://github.com/trnkdev/unity-trnk-signal.git",
            "https://github.com/trnkdev/unity-trnk-flow.git",
            "https://github.com/trnkdev/unity-trnk-serializer.git",
            "https://github.com/trnkdev/unity-trnk-big-number.git",
            "https://github.com/trnkdev/unity-trnk-nav.git",
            "https://github.com/trnkdev/unity-trnk-spreadsheets.git",
        };

        [Serializable]
        internal class GitPackage
        {
            public string url;
        }

        [HideInInspector]
        [SerializeField] private List<GitPackage> _packages = new();
        public IReadOnlyList<GitPackage> Packages => _packages;

        public void SetDefaults()
        {
            _packages = new List<GitPackage>();
            foreach (var url in DefaultGitUrls)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                _packages.Add(new GitPackage { url = url });
            }
        }

        /// <summary>
        /// Adds any newly-introduced default Git URLs that are missing from this asset.
        /// Does not remove or overwrite existing entries.
        /// Returns how many entries were added.
        /// </summary>
        public int RefreshMissingDefaults()
        {
            _packages ??= new List<GitPackage>();

            var existing = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in _packages)
            {
                if (p == null) continue;
                string key = PackagesTool.NormalizeGitUrl(p.url);
                if (!string.IsNullOrEmpty(key)) existing.Add(key);
            }

            int added = 0;
            foreach (var url in DefaultGitUrls)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                string key = PackagesTool.NormalizeGitUrl(url);
                if (string.IsNullOrEmpty(key)) continue;
                if (existing.Contains(key)) continue;
                _packages.Add(new GitPackage { url = url });
                existing.Add(key);
                added++;
            }
            return added;
        }

        private static PackagesSettings s_instance;
        private static PackagesSettings s_transient;

        public static PackagesSettings LoadOrCreate()
        {
            string assetPath = Path.Combine(AssetDir, AssetName).Replace("\\", "/");
            return EditorAssetUtils.GetOrCreateSettings(AssetDir, assetPath, ref s_instance, ref s_transient,
                settings => settings.SetDefaults());
        }
    }
}
#endif
