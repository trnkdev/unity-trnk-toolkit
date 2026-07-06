#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
#if UNITY_6000_3_OR_NEWER
using UnityEditor.Toolbars;
#endif
#if UNITY_2020_1_OR_NEWER
using UnityEditor.UIElements;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TRnK.Toolkit
{
    /// <summary>
    /// Adds a scene switching dropdown / menu to the main editor toolbar.
    /// Uses ToolbarMenu when available (Unity 2020.1+ UI Toolkit) which stays visually consistent across
    /// Unity versions including Unity 6 (6000.x). Falls back to a DropdownField if needed.
    /// </summary>
    [InitializeOnLoad]
    internal static class SceneSwitcherToolbar
    {
        #region Fields
#if UNITY_2020_1_OR_NEWER
        private static ToolbarMenu toolbarMenu;
#endif
        private static DropdownField fallbackDropdown;
        private static Image sceneIcon;
        private static string[] sceneNames = Array.Empty<string>();
        private static string[] scenePaths = Array.Empty<string>();
        private static Dictionary<string, int> duplicateNameCounts = new Dictionary<string, int>();
        private const string NoScenesLabel = "No Scenes (Build Settings)";
        private static bool initialized;
        private static VisualElement containerRef;

        private static string startupScenePath;
        private static bool playSwitched;
        private static string originalSceneBeforePlay;
        private const string SessionOriginalSceneKey = "TRnK.Toolkit:OriginalSceneBeforePlay";
        private const string SessionPlaySwitchedKey = "TRnK.Toolkit:PlaySwitchedFlag";

        private const string PrefActivateLoadedAdditive = "TRnK.Toolkit:ActivateLoadedAdditiveOnSelect";
        #endregion
        static SceneSwitcherToolbar()
        {
            EditorApplication.delayCall += () =>
            {
                RefreshSceneList();
                // Install on startup unless the global HideToolbar preference is set
#if !UNITY_6000_3_OR_NEWER
                try { if (!TRnKSettings.GetOrCreate().hideToolbar) EditorApplication.update += TryInstall; } catch { EditorApplication.update += TryInstall; }
#endif
                LoadStartupPrefs();
                // Recover persisted session info (domain reload safety)
                if (string.IsNullOrEmpty(originalSceneBeforePlay))
                {
                    var stored = SessionState.GetString(SessionOriginalSceneKey, string.Empty);
                    if (!string.IsNullOrEmpty(stored)) originalSceneBeforePlay = stored;
                }
                if (!playSwitched)
                {
                    playSwitched = SessionState.GetBool(SessionPlaySwitchedKey, false);
                }
            };
            EditorBuildSettings.sceneListChanged += RefreshSceneList;
            EditorSceneManager.sceneOpened += (_, __) => UpdateSelectionVisual();
            SceneManager.activeSceneChanged += (_, __) => UpdateSelectionVisual();
#if UNITY_2021_1_OR_NEWER
            EditorSceneManager.activeSceneChangedInEditMode += (_, __) => UpdateSelectionVisual();
#endif
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#if !UNITY_6000_3_OR_NEWER
            EditorApplication.update += LegacySelfHeal;
#endif
        }

#if UNITY_6000_3_OR_NEWER
        private const string ToolbarElementId = "TRnK.Toolkit/Scene Switcher";

        private static Texture2D s_unity6000Icon;

        // The factory is re-invoked by MainToolbar.Refresh(ToolbarElementId), so it must
        // build the dropdown from current state and hold no element instance.
        [MainToolbarElement(ToolbarElementId, defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement CreateMainToolbarElement()
        {
            if (s_unity6000Icon == null)
            {
                s_unity6000Icon = ToolbarUtils.GetBestIcon(
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

            var content = new MainToolbarContent(TruncateDisplayName(baseDisplay), s_unity6000Icon, "Switch active scene (Build Settings)");
            return new MainToolbarDropdown(content, ShowUnity6000Menu);
        }

        private static void ApplyUnity6000Visual()
        {
            // Rebuilds the element via the factory so the label reflects the active scene.
            MainToolbar.Refresh(ToolbarElementId);
        }

        private static void ShowUnity6000Menu(Rect rect)
        {
            RefreshSceneList();

            // MainToolbarDropdown provides only a Rect, so use GenericMenu (the documented
            // pattern) — GenericDropdownMenu needs a VisualElement anchor we don't have.
            var menu = new GenericMenu();

            bool hasScenes = sceneNames != null && sceneNames.Length > 0;
            string current = SceneManager.GetActiveScene().name;

            if (!hasScenes)
            {
                menu.AddItem(new GUIContent("Open Scene List..."), false, SceneSwitcherSettingsWindow.Open);
                if (CanAddActiveScene())
                    menu.AddItem(new GUIContent("Add Active Scene To Build Settings"), false, AddActiveSceneToBuild);
                else
                    menu.AddDisabledItem(new GUIContent("Add Active Scene To Build Settings"));
                menu.AddSeparator(string.Empty);
                AppendStartupMarkItemUnity6000(menu);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Refresh"), false, RefreshSceneList);
            }
            else
            {
                var groupLookup = BuildGroupLookup();

                // Ungrouped scenes
                bool hadUngrouped = false;
                for (int i = 0; i < sceneNames.Length; i++)
                {
                    if (groupLookup.TryGetValue(scenePaths[i], out var grpU) && !string.IsNullOrEmpty(grpU)) continue;
                    string itemLabel = sceneNames[i];
                    if (ScenePathMatchesStartup(scenePaths[i])) itemLabel += " ★";
                    string captured = sceneNames[i];
                    menu.AddItem(new GUIContent(itemLabel), captured == current, () => SwitchToScene(captured));
                    hadUngrouped = true;
                }

                // Grouped scenes
                bool hasAnyGrouped = false;
                for (int j = 0; j < sceneNames.Length; j++)
                    if (groupLookup.TryGetValue(scenePaths[j], out var gj) && !string.IsNullOrEmpty(gj)) { hasAnyGrouped = true; break; }
                if (hadUngrouped && hasAnyGrouped)
                    menu.AddSeparator(string.Empty);
                for (int i = 0; i < sceneNames.Length; i++)
                {
                    if (!groupLookup.TryGetValue(scenePaths[i], out var grp) || string.IsNullOrEmpty(grp)) continue;
                    string itemLabel = sceneNames[i];
                    if (ScenePathMatchesStartup(scenePaths[i])) itemLabel += " ★";
                    string captured = sceneNames[i];
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
                AppendStartupMarkItemUnity6000(menu);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Refresh"), false, RefreshSceneList);
            }

            menu.DropDown(rect);
        }

        private static void AppendStartupMarkItemUnity6000(GenericMenu menu)
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

            bool actAdd = EditorPrefs.GetBool(PrefActivateLoadedAdditive, true);
            menu.AddItem(new GUIContent("Activate Loaded Additive Scenes On Select"), actAdd, ToggleActivateLoadedAdditive);
        }

        private static void MarkActiveSceneAsStartup()
        {
            var active = SceneManager.GetActiveScene();
            if (!active.IsValid() || string.IsNullOrEmpty(active.path)) return;
            startupScenePath = active.path;
            PersistStartupScenePath(startupScenePath);
            UpdateSelectionVisual();
        }

        private static void ClearStartupScene()
        {
            startupScenePath = string.Empty;
            EditorSceneManager.playModeStartScene = null;
            var settings = TRnKSettings.GetOrCreate();
            settings.startupScenePath = string.Empty;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            UpdateSelectionVisual();
        }

        private static void ToggleActivateLoadedAdditive()
        {
            bool current = EditorPrefs.GetBool(PrefActivateLoadedAdditive, true);
            EditorPrefs.SetBool(PrefActivateLoadedAdditive, !current);
        }
#endif

        private static void TryInstall()
        {
            try { if (TRnKSettings.GetOrCreate().hideToolbar) return; } catch { }
            if (initialized) return;
            var root = ToolbarUtils.GetToolbarRoot();
            if (root == null) return;

            ToolbarUtils.RemoveAllByName(root, "TRnK.ToolkitSceneSwitcherContainer");

            initialized = true;
            EditorApplication.update -= TryInstall;

            var container = new VisualElement { name = "TRnK.ToolkitSceneSwitcherContainer" };
            containerRef = container;
            container.style.position = Position.Absolute;
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            PositionContainer(root, container);

#if UNITY_2020_1_OR_NEWER
            toolbarMenu = new ToolbarMenu
            {
                name = "TRnK.ToolkitSceneSwitcher",
                tooltip = "Switch active scene (Build Settings)"
            };
            ApplyControlSizing(toolbarMenu);
            MatchControlHeightToContainer(container, toolbarMenu);
            ApplyRoundedStyling(toolbarMenu);
            EnsureSceneIcon(container);
            container.Add(toolbarMenu);
            root.Add(container);
            if (sceneNames.Length == 0)
            {
                toolbarMenu.text = TruncateDisplayName(NoScenesLabel);
                toolbarMenu.SetEnabled(true);
            }
            else
            {
                PopulateToolbarMenu();
            }
            container.RegisterCallback<GeometryChangedEvent>(_ => MatchControlHeightToContainer(container, toolbarMenu));
            toolbarMenu.RegisterCallback<GeometryChangedEvent>(_ => { if (string.IsNullOrEmpty(toolbarMenu.text)) InstallFallbackDropdown(container); });

            TryRegisterSceneSwitcherWatcher();
#else
            InstallFallbackDropdown(container);
            root.Add(container);
#endif
        }

#if !UNITY_6000_3_OR_NEWER
        private static void LegacySelfHeal()
        {
            if (!initialized) return;
            try { if (TRnKSettings.GetOrCreate().hideToolbar) return; } catch { }

            if (containerRef != null && containerRef.parent != null) return;
            initialized = false;
            containerRef = null;
            EditorApplication.update += TryInstall;
        }
#endif

        private static void Uninstall()
        {
            if (!initialized) return;
            initialized = false;
#if UNITY_2020_1_OR_NEWER
            toolbarMenu = null;
#endif
            fallbackDropdown = null;
            sceneIcon = null;
            if (containerRef != null && containerRef.parent != null)
            {
                containerRef.parent.Remove(containerRef);
            }
            containerRef = null;
            if (!TRnKSettings.GetOrCreate().hideToolbar)
            {
                EditorApplication.update += TryInstall;
            }
        }

        internal static void ApplyPreferenceChange(bool enabled)
        {
#if UNITY_6000_3_OR_NEWER
            MainToolbarActivator.SetDisplayed(enabled);
            return;
#else
            if (enabled)
            {
                if (!initialized)
                    EditorApplication.update += TryInstall;
            }
            else
            {
                Uninstall();
            }
#endif
        }

        private static void InstallFallbackDropdown(VisualElement container)
        {
            if (fallbackDropdown != null) return;
            fallbackDropdown = new DropdownField
            {
                name = "TRnK.ToolkitSceneSwitcherFallback",
                tooltip = "Switch active scene (Build Settings)",
                choices = new List<string>(sceneNames)
            };
            ApplyControlSizing(fallbackDropdown);
            MatchControlHeightToContainer(container, fallbackDropdown);
            fallbackDropdown.style.marginLeft = 4;
            fallbackDropdown.RegisterValueChangedCallback(e => SwitchToScene(e.newValue));
            ApplyRoundedStyling(fallbackDropdown);
            EnsureSceneIcon(container, before: fallbackDropdown);
            container.Add(fallbackDropdown);

            if (sceneNames.Length == 0)
            {
                fallbackDropdown.choices = new List<string> { NoScenesLabel };
                fallbackDropdown.value = NoScenesLabel;
                fallbackDropdown.SetEnabled(false);
            }
            else
            {
                string current = SceneManager.GetActiveScene().name;
                if (Array.IndexOf(sceneNames, current) < 0) current = sceneNames[0];
                fallbackDropdown.value = current;
            }
            container.RegisterCallback<GeometryChangedEvent>(_ => MatchControlHeightToContainer(container, fallbackDropdown));

            TryRegisterSceneSwitcherWatcher();
        }

#if UNITY_2020_1_OR_NEWER
        private static void PopulateToolbarMenu()
        {
            if (toolbarMenu == null) return;
            toolbarMenu.menu.MenuItems().Clear();
            string current = SceneManager.GetActiveScene().name;
            var activeScene = SceneManager.GetActiveScene();
            bool hasScenes = sceneNames.Length > 0;
            if (!hasScenes)
            {
                toolbarMenu.text = TruncateDisplayName(NoScenesLabel);
                toolbarMenu.SetEnabled(true);
                toolbarMenu.menu.AppendAction("Open Scene List...", _ => OpenBuildProfilesSceneList(), _ => DropdownMenuAction.Status.Normal);
                toolbarMenu.menu.AppendAction("Scene Switcher Settings...", _ => SceneSwitcherSettingsWindow.Open(), _ => DropdownMenuAction.Status.Normal);
                toolbarMenu.menu.AppendAction("Add Active Scene To Build Settings", _ => AddActiveSceneToBuild(), _ => CanAddActiveScene() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                toolbarMenu.menu.AppendSeparator("");
                AppendStartupMarkItem();
                toolbarMenu.menu.AppendSeparator("");
                toolbarMenu.menu.AppendAction("Refresh", _ => RefreshSceneList(), _ => DropdownMenuAction.Status.Normal);
                return;
            }
            bool currentInList = Array.IndexOf(sceneNames, current) >= 0;
            if (!currentInList && sceneNames.Length > 0)
            {
                string baseDisplay = string.IsNullOrEmpty(current) ? sceneNames[0] : current + " *";
                if (HasStartupScene() && activeScene.IsValid() && ScenePathMatchesStartup(activeScene.path))
                    baseDisplay += " ★"; // append startup star suffix
                toolbarMenu.text = TruncateDisplayName(baseDisplay);
                toolbarMenu.menu.AppendAction("Add Active Scene To Build Settings", _ => AddActiveSceneToBuild(), _ => CanAddActiveScene() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                toolbarMenu.menu.AppendSeparator("");
            }
            else
            {
                string baseDisplay = currentInList ? current : sceneNames[0];
                if (HasStartupScene() && activeScene.IsValid() && ScenePathMatchesStartup(activeScene.path))
                    baseDisplay += " ★";
                toolbarMenu.text = TruncateDisplayName(baseDisplay);
            }
            var groupLookup = BuildGroupLookup();

            // Ungrouped scenes
            bool hadUngrouped = false;
            for (int i = 0; i < sceneNames.Length; i++)
            {
                if (groupLookup.TryGetValue(scenePaths[i], out var grpU) && !string.IsNullOrEmpty(grpU)) continue;
                string itemLabel = sceneNames[i] + "\t" + i;
                if (ScenePathMatchesStartup(scenePaths[i])) itemLabel += " ★";
                string captured = sceneNames[i];
                toolbarMenu.menu.AppendAction(itemLabel, a => SwitchToScene(captured), a => captured == current ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                hadUngrouped = true;
            }

            // Grouped scenes
            bool hasAnyGrouped = false;
            for (int j = 0; j < sceneNames.Length; j++)
                if (groupLookup.TryGetValue(scenePaths[j], out var gj) && !string.IsNullOrEmpty(gj)) { hasAnyGrouped = true; break; }
            if (hadUngrouped && hasAnyGrouped)
                toolbarMenu.menu.AppendSeparator("");
            for (int i = 0; i < sceneNames.Length; i++)
            {
                if (!groupLookup.TryGetValue(scenePaths[i], out var grp) || string.IsNullOrEmpty(grp)) continue;
                string itemLabel = sceneNames[i] + "\t" + i;
                if (ScenePathMatchesStartup(scenePaths[i])) itemLabel += " ★";
                string captured = sceneNames[i];
                toolbarMenu.menu.AppendAction(grp + "/" + itemLabel, a => SwitchToScene(captured), a => captured == current ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }

            toolbarMenu.menu.AppendSeparator("");
            toolbarMenu.menu.AppendAction("Open Scene List...", _ => OpenBuildProfilesSceneList(), _ => DropdownMenuAction.Status.Normal);
            toolbarMenu.menu.AppendAction("Scene Switcher Settings...", _ => SceneSwitcherSettingsWindow.Open(), _ => DropdownMenuAction.Status.Normal);
            AppendStartupMarkItem();
            toolbarMenu.menu.AppendAction("Refresh", _ => RefreshSceneList(), _ => DropdownMenuAction.Status.Normal);
        }
#endif

        private static bool CanAddActiveScene()
        {
            var active = SceneManager.GetActiveScene();
            if (!active.IsValid() || string.IsNullOrEmpty(active.path)) return false;
            if (Array.IndexOf(scenePaths, active.path) >= 0) return false;
            return true;
        }

        private static void OpenBuildProfilesSceneList()
        {
#if UNITY_6000_0_OR_NEWER
            EditorApplication.ExecuteMenuItem("File/Build Profiles");
#else
            EditorWindow.GetWindow(typeof(BuildPlayerWindow));
#endif
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
            sceneNames = new string[count];
            scenePaths = new string[count];
            duplicateNameCounts.Clear();
            for (int i = 0; i < count; i++)
            {
                string path = buildScenes[i].path;
                scenePaths[i] = path;
                string baseName = string.IsNullOrEmpty(path) ? "Unnamed" : System.IO.Path.GetFileNameWithoutExtension(path);
                if (duplicateNameCounts.ContainsKey(baseName)) duplicateNameCounts[baseName]++; else duplicateNameCounts[baseName] = 1;
                sceneNames[i] = baseName;
            }
            for (int i = 0; i < count; i++)
            {
                string bn = sceneNames[i];
                if (duplicateNameCounts.TryGetValue(bn, out var c) && c > 1)
                    sceneNames[i] = bn + " (" + (i + 1) + ")";
            }
#if UNITY_2020_1_OR_NEWER
            PopulateToolbarMenu();
#endif
            if (fallbackDropdown != null)
            {
                fallbackDropdown.choices = new List<string>(sceneNames.Length > 0 ? sceneNames : new[] { NoScenesLabel });
                if (sceneNames.Length == 0)
                {
                    fallbackDropdown.value = NoScenesLabel;
                    fallbackDropdown.SetEnabled(false);
                }
                else
                {
                    fallbackDropdown.SetEnabled(true);
                    string current = SceneManager.GetActiveScene().name;
                    if (Array.IndexOf(sceneNames, current) < 0) current = sceneNames[0];
                    fallbackDropdown.value = current;
                }
            }
        }

        private static void UpdateSelectionVisual()
        {
#if UNITY_6000_3_OR_NEWER
            ApplyUnity6000Visual();
#else
#if UNITY_2020_1_OR_NEWER
            PopulateToolbarMenu();
#endif
            if (fallbackDropdown != null)
            {
                string active = SceneManager.GetActiveScene().name;
                if (Array.IndexOf(sceneNames, active) >= 0)
                    fallbackDropdown.value = active;
            }
#endif
        }

        private static void SwitchToScene(string sceneName)
        {
            if (sceneName == NoScenesLabel) return;
            int index = Array.IndexOf(sceneNames, sceneName);
            if (index < 0 || index >= scenePaths.Length) return;
            string path = scenePaths[index];
            if (string.IsNullOrEmpty(path)) return;
            bool activateLoadedAdditive = EditorPrefs.GetBool(PrefActivateLoadedAdditive, false);
            if (activateLoadedAdditive)
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

        private static VisualElement FindPlayControlsRightMost(VisualElement root) => FindCandidateRecursive(root, 0);

        private static VisualElement FindCandidateRecursive(VisualElement ve, int depth)
        {
            if (depth > 6) return null;
            int buttons = 0;
            for (int i = 0; i < ve.childCount; i++) if (ToolbarUtils.LooksLikeToolbarButton(ve[i])) buttons++;
            if (buttons >= 3) return ve.childCount > 0 ? ve[ve.childCount - 1] : ve;
            for (int i = 0; i < ve.childCount; i++)
            {
                var found = FindCandidateRecursive(ve[i], depth + 1);
                if (found != null) return found;
            }
            return null;
        }

        private static void PositionContainer(VisualElement toolbarRoot, VisualElement container)
        {
            var anchor = FindPlayControlsRightMost(toolbarRoot);
            float left = 220f, top = 0f, height = 0f;
            if (anchor != null)
            {
                left = ToolbarUtils.GetWorldX(anchor, toolbarRoot) + anchor.layout.width + 18f;
                top = ToolbarUtils.GetWorldY(anchor, toolbarRoot);
                height = anchor.layout.height;
            }
            container.style.left = left;
            container.style.top = top;
            if (height > 0) container.style.height = height;
        }

        private static void MatchControlHeightToContainer(VisualElement container, VisualElement control)
        {
            var h = container.resolvedStyle.height;
            if (h > 0) { control.style.height = h; control.style.unityTextAlign = TextAnchor.MiddleLeft; }
        }

        private static void ApplyRoundedStyling(VisualElement ve) => ToolbarUtils.ApplyRoundedStyling(ve);

        private static void ApplyControlSizing(VisualElement ve)
        {
            ve.style.minWidth = 110;
            ve.style.maxWidth = 160;
        }

        private static void TryRegisterSceneSwitcherWatcher()
        {
            ToolbarUtils.TryRegisterLayoutWatcher(() =>
            {
                var root = ToolbarUtils.GetToolbarRoot();
                if (root != null && containerRef != null)
                {
                    PositionContainer(root, containerRef);
                }
            });
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

        private static void EnsureSceneIcon(VisualElement container, VisualElement before = null)
        {
            if (sceneIcon != null) return;
            var tex = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            sceneIcon = new Image { image = tex, scaleMode = ScaleMode.ScaleToFit, name = "TRnK.ToolkitSceneIcon" };
            sceneIcon.style.width = 16;
            sceneIcon.style.height = 16;
            sceneIcon.style.marginRight = 4;
            if (before != null) container.Insert(container.IndexOf(before), sceneIcon); else container.Add(sceneIcon);
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
                    startupScenePath = fromSettings;
                    EditorSceneManager.playModeStartScene = sceneAsset;
                    return;
                }

                startupScenePath = string.Empty;
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
                startupScenePath = startPath;
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
                startupScenePath = string.Empty;
            }
        }

        private static bool HasStartupScene() => !string.IsNullOrEmpty(startupScenePath);

        private static void ToggleStartupScene()
        {
            var active = SceneManager.GetActiveScene();
            if (!HasStartupScene())
            {
                if (active.IsValid() && !string.IsNullOrEmpty(active.path))
                {
                    startupScenePath = active.path;
                    PersistStartupScenePath(startupScenePath);
                }
            }
            else
            {
                if (active.IsValid() && !string.IsNullOrEmpty(active.path) && !string.Equals(active.path, startupScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    startupScenePath = active.path;
                    PersistStartupScenePath(startupScenePath);
                }
                else
                {
                    startupScenePath = string.Empty;
                    EditorSceneManager.playModeStartScene = null;
                    // Mirror clear into TRnKSettings asset
                    var settings = TRnKSettings.GetOrCreate();
                    settings.startupScenePath = string.Empty;
                    EditorAssetUtils.MarkDirtyAndSave(settings);
                }
            }
#if UNITY_2020_1_OR_NEWER
            PopulateToolbarMenu();
#endif
        }

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
            string baseName = System.IO.Path.GetFileNameWithoutExtension(startupScenePath);
            for (int i = 0; i < scenePaths.Length; i++)
            {
                if (string.Equals(scenePaths[i], startupScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (duplicateNameCounts.TryGetValue(baseName, out var c) && c > 1)
                        return baseName + " (" + (i + 1) + ")";
                    return baseName;
                }
            }
            return baseName;
        }

        private static bool ScenePathMatchesStartup(string path)
        {
            if (!HasStartupScene()) return false;
            return string.Equals(path, startupScenePath, StringComparison.OrdinalIgnoreCase);
        }

#if UNITY_2020_1_OR_NEWER
        private static void AppendStartupMarkItem()
        {
            var active = SceneManager.GetActiveScene();
            bool activeValid = active.IsValid() && !string.IsNullOrEmpty(active.path);

            string ActiveSceneDisplayName()
            {
                if (!activeValid) return "(Invalid)";
                string baseName = active.name;
                for (int i = 0; i < scenePaths.Length; i++)
                {
                    if (string.Equals(scenePaths[i], active.path, StringComparison.OrdinalIgnoreCase))
                    {
                        if (duplicateNameCounts.TryGetValue(baseName, out var c) && c > 1)
                            return baseName + " (" + (i + 1) + ")";
                        return baseName;
                    }
                }
                return baseName;
            }

            if (!HasStartupScene())
            {
                var label = "Mark Active Scene As Startup";
                toolbarMenu.menu.AppendAction(label, _ => ToggleStartupScene(), _ => activeValid ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
            else
            {
                bool activeIsStartup = activeValid && ScenePathMatchesStartup(active.path);
                string label;
                if (activeIsStartup)
                {
                    var name = StartupSceneName();
                    label = string.IsNullOrEmpty(name) ? "Unmark Startup Scene" : $"Unmark Startup Scene ({name})";
                    toolbarMenu.menu.AppendAction(label, _ => ToggleStartupScene(), _ => DropdownMenuAction.Status.Normal);
                }
                else
                {
                    label = $"Switch Startup Scene from ({StartupSceneName()}) to ({ActiveSceneDisplayName()})";
                    toolbarMenu.menu.AppendAction(label, _ => ToggleStartupScene(), _ => activeValid ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }
            }
        }
#endif

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

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    if (!HasStartupScene()) { playSwitched = false; SessionState.SetBool(SessionPlaySwitchedKey, false); return; }
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(startupScenePath) == null) { startupScenePath = string.Empty; playSwitched = false; SessionState.SetBool(SessionPlaySwitchedKey, false); return; }
                    var active = SceneManager.GetActiveScene();
                    if (string.Equals(active.path, startupScenePath, StringComparison.OrdinalIgnoreCase)) { playSwitched = false; SessionState.SetBool(SessionPlaySwitchedKey, false); return; }
                    originalSceneBeforePlay = active.path;
                    SessionState.SetString(SessionOriginalSceneKey, originalSceneBeforePlay);
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorApplication.isPlaying = false; playSwitched = false; originalSceneBeforePlay = null; SessionState.EraseString(SessionOriginalSceneKey); SessionState.SetBool(SessionPlaySwitchedKey, false); return;
                    }
                    var opened = EditorSceneManager.OpenScene(startupScenePath);
                    SceneManager.SetActiveScene(opened);
                    playSwitched = true; SessionState.SetBool(SessionPlaySwitchedKey, true);
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    if ((playSwitched || SessionState.GetBool(SessionPlaySwitchedKey, false)) && !string.IsNullOrEmpty(originalSceneBeforePlay) && System.IO.File.Exists(originalSceneBeforePlay))
                    {
                        var current = SceneManager.GetActiveScene();
                        if (!string.Equals(current.path, originalSceneBeforePlay, StringComparison.OrdinalIgnoreCase))
                        {
                            var restored = EditorSceneManager.OpenScene(originalSceneBeforePlay);
                            SceneManager.SetActiveScene(restored);
                        }
                    }
                    playSwitched = false; SessionState.SetBool(SessionPlaySwitchedKey, false);
                    originalSceneBeforePlay = null; SessionState.EraseString(SessionOriginalSceneKey);
                    break;
            }
        }
    }
}
#endif