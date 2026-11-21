#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TRnK.Toolkit
{
    /// <summary>Adds a scene switching dropdown to the main editor toolbar.</summary>
    [InitializeOnLoad]
    internal static class SceneSwitcherToolbar
    {
        #region Fields
        private static string[] s_sceneNames = Array.Empty<string>();
        private static string[] s_scenePaths = Array.Empty<string>();
        private static Dictionary<string, int> s_duplicateNameCounts = new Dictionary<string, int>();

        private static string s_startupScenePath;
        #endregion
        static SceneSwitcherToolbar()
        {
            EditorApplication.delayCall += () =>
            {
                RefreshSceneList();
                LoadStartupPrefs();
            };
            EditorBuildSettings.sceneListChanged += RefreshSceneList;
            EditorSceneManager.sceneOpened += (_, __) => UpdateSelectionVisual();
            SceneManager.activeSceneChanged += (_, __) => UpdateSelectionVisual();
            EditorSceneManager.activeSceneChangedInEditMode += (_, __) => UpdateSelectionVisual();
        }

        private const string ToolbarElementId = "TRnK.Toolkit/Scene Switcher";

        private static Texture2D s_icon;

        // The factory is re-invoked by MainToolbar.Refresh(ToolbarElementId), so it must
        // build the dropdown from current state and hold no element instance.
        [MainToolbarElement(ToolbarElementId, defaultDockPosition = MainToolbarDockPosition.Right, defaultDockIndex = 100)]
        public static MainToolbarElement CreateMainToolbarElement()
        {
            if (s_icon == null)
            {
                s_icon = ToolbarUtils.GetBestIcon(
                    "d_SceneAsset Icon",
                    "SceneAsset Icon",
                    "d_SceneViewFx",
                    "SceneViewFx"
                );
            }

            string baseDisplay = "Scenes";
            try
            {
                var active = SceneManager.GetActiveScene();
                if (active.IsValid() && !string.IsNullOrEmpty(active.name))
                    baseDisplay = active.name;
                if (HasStartupScene() && active.IsValid() && ScenePathMatchesStartup(active.path))
                    baseDisplay += " ★";
            }
            catch { }

            var content = new MainToolbarContent(TruncateDisplayName(baseDisplay), s_icon, "Switch active scene (Build Settings)");
            return new MainToolbarDropdown(content, ShowMenu);
        }

        private static void ShowMenu(Rect rect)
        {
            RefreshSceneList();

            // MainToolbarDropdown provides only a Rect, so use GenericMenu (the documented
            // pattern) — GenericDropdownMenu needs a VisualElement anchor we don't have.
            var menu = new GenericMenu();

            bool hasScenes = s_sceneNames != null && s_sceneNames.Length > 0;
            string current = SceneManager.GetActiveScene().name;

            if (!hasScenes)
            {
                menu.AddItem(new GUIContent("Open Scene List..."), false, SceneSwitcherSettingsWindow.Open);
                if (CanAddActiveScene())
                    menu.AddItem(new GUIContent("Add Active Scene To Build Settings"), false, AddActiveSceneToBuild);
                else
                    menu.AddDisabledItem(new GUIContent("Add Active Scene To Build Settings"));
                menu.AddSeparator(string.Empty);
                AppendStartupItems(menu);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Refresh"), false, RefreshSceneList);
            }
            else
            {
                var groupLookup = BuildGroupLookup();

                // Ungrouped scenes
                bool hadUngrouped = false;
                for (int i = 0; i < s_sceneNames.Length; i++)
                {
                    if (groupLookup.TryGetValue(s_scenePaths[i], out var grpU) && !string.IsNullOrEmpty(grpU)) continue;
                    string itemLabel = s_sceneNames[i];
                    if (ScenePathMatchesStartup(s_scenePaths[i])) itemLabel += " ★";
                    string captured = s_sceneNames[i];
                    menu.AddItem(new GUIContent(itemLabel), captured == current, () => SwitchToScene(captured));
                    hadUngrouped = true;
                }

                // Grouped scenes
                bool hasAnyGrouped = false;
                for (int j = 0; j < s_sceneNames.Length; j++)
                    if (groupLookup.TryGetValue(s_scenePaths[j], out var gj) && !string.IsNullOrEmpty(gj)) { hasAnyGrouped = true; break; }
                if (hadUngrouped && hasAnyGrouped)
                    menu.AddSeparator(string.Empty);
                for (int i = 0; i < s_sceneNames.Length; i++)
                {
                    if (!groupLookup.TryGetValue(s_scenePaths[i], out var grp) || string.IsNullOrEmpty(grp)) continue;
                    string itemLabel = s_sceneNames[i];
                    if (ScenePathMatchesStartup(s_scenePaths[i])) itemLabel += " ★";
                    string captured = s_sceneNames[i];
                    menu.AddItem(new GUIContent(grp + "/" + itemLabel), captured == current, () => SwitchToScene(captured));
                }

                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Open Scene List..."), false, OpenBuildProfilesSceneList);
                menu.AddItem(new GUIContent("Scene Switcher Settings..."), false, SceneSwitcherSettingsWindow.Open);
                if (CanAddActiveScene())
                    menu.AddItem(new GUIContent("Add Active Scene To Build Settings"), false, AddActiveSceneToBuild);
                else
                    menu.AddDisabledItem(new GUIContent("Add Active Scene To Build Settings"));
                menu.AddSeparator(string.Empty);
                AppendStartupItems(menu);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Refresh"), false, RefreshSceneList);
            }

            menu.DropDown(rect);
        }

        private static void AppendStartupItems(GenericMenu menu)
        {
            string startupDisplay = StartupSceneName();
            if (HasStartupScene())
            {
                menu.AddItem(new GUIContent($"Clear Startup Scene ({startupDisplay})"), false, ClearStartupScene);
            }
            else
            {
                menu.AddItem(new GUIContent("Mark Active Scene As Startup"), false, MarkActiveSceneAsStartup);
            }

            bool actAdd = TRnKSettings.GetOrCreate().activateLoadedAdditiveOnSelect;
            menu.AddItem(new GUIContent("Activate Loaded Additive Scenes On Select"), actAdd, ToggleActivateLoadedAdditive);
        }

        private static void MarkActiveSceneAsStartup()
        {
            var active = SceneManager.GetActiveScene();
            if (!active.IsValid() || string.IsNullOrEmpty(active.path)) return;
            s_startupScenePath = active.path;
            PersistStartupScenePath(s_startupScenePath);
            UpdateSelectionVisual();
        }

        private static void ClearStartupScene()
        {
            s_startupScenePath = string.Empty;
            EditorSceneManager.playModeStartScene = null;
            var settings = TRnKSettings.GetOrCreate();
            settings.startupScenePath = string.Empty;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            UpdateSelectionVisual();
        }

        private static void ToggleActivateLoadedAdditive()
        {
            var settings = TRnKSettings.GetOrCreate();
            settings.activateLoadedAdditiveOnSelect = !settings.activateLoadedAdditiveOnSelect;
            EditorAssetUtils.MarkDirtyAndSave(settings);
        }

        private static bool CanAddActiveScene()
        {
            var active = SceneManager.GetActiveScene();
            if (!active.IsValid() || string.IsNullOrEmpty(active.path)) return false;
            if (Array.IndexOf(s_scenePaths, active.path) >= 0) return false;
            return true;
        }

        private static void OpenBuildProfilesSceneList()
        {
            EditorApplication.ExecuteMenuItem("File/Build Profiles");
        }

        private static void AddActiveSceneToBuild()
        {
            if (!CanAddActiveScene()) return;
            var active = SceneManager.GetActiveScene();
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes) { new EditorBuildSettingsScene(active.path, true) };
            EditorBuildSettings.scenes = list.ToArray();
            RefreshSceneList();
        }

        internal static void RefreshSceneList()
        {
            var buildScenes = EditorBuildSettings.scenes;
            int count = buildScenes.Length;
            s_sceneNames = new string[count];
            s_scenePaths = new string[count];
            s_duplicateNameCounts.Clear();
            for (int i = 0; i < count; i++)
            {
                string path = buildScenes[i].path;
                s_scenePaths[i] = path;
                string baseName = string.IsNullOrEmpty(path) ? "Unnamed" : System.IO.Path.GetFileNameWithoutExtension(path);
                if (s_duplicateNameCounts.ContainsKey(baseName)) s_duplicateNameCounts[baseName]++; else s_duplicateNameCounts[baseName] = 1;
                s_sceneNames[i] = baseName;
            }
            for (int i = 0; i < count; i++)
            {
                string bn = s_sceneNames[i];
                if (s_duplicateNameCounts.TryGetValue(bn, out var c) && c > 1)
                    s_sceneNames[i] = bn + " (" + (i + 1) + ")";
            }
        }

        private static void UpdateSelectionVisual()
        {
            // Rebuilds the element via the factory so the label reflects the active scene
            MainToolbar.Refresh(ToolbarElementId);
        }

        private static void SwitchToScene(string sceneName)
        {
            int index = Array.IndexOf(s_sceneNames, sceneName);
            if (index < 0 || index >= s_scenePaths.Length) return;
            string path = s_scenePaths[index];
            if (string.IsNullOrEmpty(path)) return;
            if (TRnKSettings.GetOrCreate().activateLoadedAdditiveOnSelect)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    if (string.Equals(s.path, path, StringComparison.OrdinalIgnoreCase))
                    {
                        if (s != SceneManager.GetActiveScene())
                        {
                            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            {
                                SceneManager.SetActiveScene(s);
                                UpdateSelectionVisual();
                            }
                        }
                        else
                        {
                            UpdateSelectionVisual();
                        }
                        return;
                    }
                }
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
            else
            {
                UpdateSelectionVisual();
            }
        }

        private static string TruncateDisplayName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            const int max = 22;
            bool hasStar = name.EndsWith(" ★", StringComparison.Ordinal);
            if (name.Length <= max) return name;
            if (hasStar)
            {
                int coreLen = max - 3;
                if (coreLen < 1) return "★";
                return name.Substring(0, coreLen) + "… ★";
            }
            return name.Substring(0, max - 1) + "…";
        }

        private static void LoadStartupPrefs()
        {
            string fromSettings = string.Empty;
            try
            {
                var settings = TRnKSettings.GetOrCreate();
                fromSettings = settings != null ? settings.startupScenePath : string.Empty;
            }
            catch { }

            if (!string.IsNullOrEmpty(fromSettings))
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(fromSettings);
                if (sceneAsset != null)
                {
                    s_startupScenePath = fromSettings;
                    EditorSceneManager.playModeStartScene = sceneAsset;
                    return;
                }

                s_startupScenePath = string.Empty;
                try
                {
                    var settings = TRnKSettings.GetOrCreate();
                    settings.startupScenePath = string.Empty;
                    EditorAssetUtils.MarkDirtyAndSave(settings);
                }
                catch { }
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            var startScene = EditorSceneManager.playModeStartScene;
            if (startScene != null)
            {
                var startPath = AssetDatabase.GetAssetPath(startScene);
                s_startupScenePath = startPath;
                try
                {
                    var settings = TRnKSettings.GetOrCreate();
                    if (settings.startupScenePath != startPath)
                    {
                        settings.startupScenePath = startPath;
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                    }
                }
                catch { }
            }
            else
            {
                s_startupScenePath = string.Empty;
            }
        }

        private static bool HasStartupScene() => !string.IsNullOrEmpty(s_startupScenePath);

        private static void PersistStartupScenePath(string path)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            EditorSceneManager.playModeStartScene = sceneAsset;
            var settings = TRnKSettings.GetOrCreate();
            settings.startupScenePath = path;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static string StartupSceneName()
        {
            if (!HasStartupScene()) return string.Empty;
            string baseName = System.IO.Path.GetFileNameWithoutExtension(s_startupScenePath);
            for (int i = 0; i < s_scenePaths.Length; i++)
            {
                if (string.Equals(s_scenePaths[i], s_startupScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (s_duplicateNameCounts.TryGetValue(baseName, out var c) && c > 1)
                        return baseName + " (" + (i + 1) + ")";
                    return baseName;
                }
            }
            return baseName;
        }

        private static bool ScenePathMatchesStartup(string path)
        {
            if (!HasStartupScene()) return false;
            return string.Equals(path, s_startupScenePath, StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> BuildGroupLookup()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var settings = SceneSwitcherSettings.GetOrCreate();
                if (settings.groups == null) return result;
                foreach (var group in settings.groups)
                {
                    if (string.IsNullOrEmpty(group?.label) || group.scenePaths == null) continue;
                    foreach (var path in group.scenePaths)
                    {
                        if (!string.IsNullOrEmpty(path) && !result.ContainsKey(path))
                            result[path] = group.label;
                    }
                }
            }
            catch { }
            return result;
        }
    }
}
#endif