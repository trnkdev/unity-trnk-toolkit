#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TRnK.Toolkit
{
    internal class TmpFontReplacerWindow : EditorWindow
    {
        [MenuItem("Tools/TRnK/TMP Font Replacer")]
        public static void Open() => GetWindow<TmpFontReplacerWindow>("TMP Font Replacer");

        // --- Settings ---
        private TmpFontReplacerSettings _settings;

        // --- Font ---
        private TMP_FontAsset _targetFont;
        private Material[] _sdfMaterials = System.Array.Empty<Material>();
        private string[] _sdfMatNames = System.Array.Empty<string>();
        private int _sdfMatIndex;
        private bool _materialsScanned;

        // --- Asset lists ---
        // Only assets containing at least one TMP_Text component are stored.
        private int _tab;
        private Vector2 _sceneScroll;
        private Vector2 _prefabScroll;
        private List<AssetEntry> _scenes = new();
        private List<AssetEntry> _prefabs = new();
        private int _selectedSceneCount;
        private int _selectedPrefabCount;
        private bool _hasScanned;
        private bool _hasStaleResults;

        // --- Exclusions ---
        private List<DefaultAsset> _excludedFolders = new();
        private Vector2 _exclusionScroll;
        private bool _exclusionFoldout = true;

        // --- Cached styles & textures ---
        private Texture2D _rowEvenTex;
        private Texture2D _rowOddTex;
        private Texture2D _rowChildEvenTex;
        private Texture2D _rowChildOddTex;
        private GUIStyle _rowEvenStyle;
        private GUIStyle _rowOddStyle;
        private GUIStyle _rowChildEvenStyle;
        private GUIStyle _rowChildOddStyle;
        private GUIStyle _iconBtnStyle;
        private GUIStyle _infoLabelStyle;

        private float _windowHeight;

        // -------------------------------------------------------------------------

        private class AssetEntry
        {
            public string Path;
            public string Name;
            public bool Selected;
            public bool Foldout;
            public List<ObjectEntry> Objects = new();

            // Cached count of selected children — maintained incrementally to
            // avoid iterating Objects every OnGUI frame in ChildCheckState.
            public int SelectedObjectCount;

            public enum CheckState { None, Mixed, All }

            public CheckState ChildCheckState()
            {
                if (SelectedObjectCount == 0) return CheckState.None;
                if (SelectedObjectCount == Objects.Count) return CheckState.All;
                return CheckState.Mixed;
            }
        }

        private class ObjectEntry
        {
            public string HierarchyPath;
            public string Name;
            public string FontName;
            public string MaterialName;
            public bool Selected;

            // Measured once on first draw, reset to 0 when FontName/MaterialName changes.
            public float CachedInfoWidth;

            public string InfoText => $"{FontName}  ·  {MaterialName}";
            public void InvalidateInfoWidth() => CachedInfoWidth = 0f;
        }

        // -------------------------------------------------------------------------

        private void OnEnable()
        {
            LoadSettings();
            RebuildMaterialList();
            LoadScanResults();
        }

        private void OnDisable()
        {
            DestroyTexture(ref _rowEvenTex);
            DestroyTexture(ref _rowOddTex);
            DestroyTexture(ref _rowChildEvenTex);
            DestroyTexture(ref _rowChildOddTex);

            _rowEvenStyle = null;
            _rowOddStyle = null;
            _rowChildEvenStyle = null;
            _rowChildOddStyle = null;
            _iconBtnStyle = null;
            _infoLabelStyle = null;
        }

        private void LoadSettings()
        {
            _settings = TmpFontReplacerSettings.GetOrCreate();
            _targetFont = _settings.lastFont;
            _excludedFolders = _settings.excludedFolders ?? new List<DefaultAsset>();
        }

        private void SaveSettings()
        {
            if (_settings == null) return;
            _settings.lastFont = _targetFont;
            _settings.lastMaterial = SelectedMaterial;
            _settings.excludedFolders = _excludedFolders;
            _settings.Save();
        }

        // -------------------------------------------------------------------------

        private static void DestroyTexture(ref Texture2D tex)
        {
            if (tex == null) return;
            DestroyImmediate(tex);
            tex = null;
        }

        // Built once when first needed. GUIStyle objects that reference EditorStyles
        // must be created inside OnGUI — not in OnEnable — because the editor skin
        // isn't guaranteed to be loaded before the first paint.
        private void EnsureStyles()
        {
            if (_rowEvenTex != null) return;

            _rowEvenTex = MakeTex(EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.88f, 0.88f, 0.88f));
            _rowOddTex = MakeTex(EditorGUIUtility.isProSkin
                ? new Color(0.19f, 0.19f, 0.19f) : new Color(0.84f, 0.84f, 0.84f));
            _rowChildEvenTex = MakeTex(EditorGUIUtility.isProSkin
                ? new Color(0.20f, 0.20f, 0.20f) : new Color(0.86f, 0.86f, 0.86f));
            _rowChildOddTex = MakeTex(EditorGUIUtility.isProSkin
                ? new Color(0.17f, 0.17f, 0.17f) : new Color(0.82f, 0.82f, 0.82f));

            _rowEvenStyle = BuildRowStyle(_rowEvenTex);
            _rowOddStyle = BuildRowStyle(_rowOddTex);
            _rowChildEvenStyle = BuildRowStyle(_rowChildEvenTex);
            _rowChildOddStyle = BuildRowStyle(_rowChildOddTex);

            _iconBtnStyle = new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(2, 2, 1, 1),
                fixedWidth = 20,
                fixedHeight = 18,
            };

            _infoLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = EditorGUIUtility.isProSkin
                    ? new Color(0.5f, 0.5f, 0.5f)
                    : new Color(0.4f, 0.4f, 0.4f) },
                clipping = TextClipping.Clip,
            };
        }

        private static GUIStyle BuildRowStyle(Texture2D tex) => new GUIStyle(GUIStyle.none)
        {
            normal = { background = tex },
            padding = new RectOffset(4, 4, 1, 1),
            margin = new RectOffset(0, 0, 0, 0),
            fixedHeight = 20,
        };

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        private GUIStyle RowStyle(int index, bool child = false)
        {
            if (child) return index % 2 == 0 ? _rowChildEvenStyle : _rowChildOddStyle;
            return index % 2 == 0 ? _rowEvenStyle : _rowOddStyle;
        }

        // Measured once per ObjectEntry, cached. CalcSize is not free —
        // skipping it on every frame matters for large lists.
        private float GetInfoWidth(ObjectEntry obj)
        {
            if (obj.CachedInfoWidth > 0f) return obj.CachedInfoWidth;
            const float padding = 16f;
            const float hardCap = 320f;
            float measured = _infoLabelStyle.CalcSize(new GUIContent(obj.InfoText)).x;
            obj.CachedInfoWidth = Mathf.Min(measured + padding, hardCap);
            return obj.CachedInfoWidth;
        }

        // -------------------------------------------------------------------------
        // Scan — opens every scene additively, filters to TMP-only, caches all data.
        // Called once after boot via delayCall, and again on Rescan button.
        // Foldouts show pre-cached data — zero I/O on click.
        // -------------------------------------------------------------------------

        private void Scan()
        {
            if (EditorApplication.isPlaying) return;

            var excludedPaths = _excludedFolders
                .Where(f => f != null)
                .Select(f => AssetDatabase.GetAssetPath(f))
                .Where(p => !string.IsNullOrEmpty(p))
                .ToHashSet();

            bool IsExcluded(string path)
                => excludedPaths.Any(ex => path.StartsWith(ex + "/") || path == ex);

            var scenePaths = AssetDatabase
                .FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !IsExcluded(p))
                .OrderBy(p => p)
                .ToList();

            var prefabPaths = AssetDatabase
                .FindAssets("t:Prefab", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !IsExcluded(p))
                .OrderBy(p => p)
                .ToList();

            int total = scenePaths.Count + prefabPaths.Count;
            int progress = 0;

            _scenes = new List<AssetEntry>(scenePaths.Count);
            foreach (var path in scenePaths)
            {
                EditorUtility.DisplayProgressBar(
                    "Scanning Project",
                    $"Scene: {System.IO.Path.GetFileName(path)}",
                    total > 0 ? (float)progress / total : 1f);

                var entry = BuildSceneEntry(path);
                if (entry.Objects.Count > 0)
                    _scenes.Add(entry);

                progress++;
            }

            _prefabs = new List<AssetEntry>(prefabPaths.Count);
            foreach (var path in prefabPaths)
            {
                EditorUtility.DisplayProgressBar(
                    "Scanning Project",
                    $"Prefab: {System.IO.Path.GetFileName(path)}",
                    total > 0 ? (float)progress / total : 1f);

                var entry = BuildPrefabEntry(path);
                if (entry.Objects.Count > 0)
                    _prefabs.Add(entry);

                progress++;
            }

            EditorUtility.ClearProgressBar();

            _selectedSceneCount = 0;
            _selectedPrefabCount = 0;
            _hasScanned = true;
            _hasStaleResults = false;
            SaveScanResults();
        }

        // Opens the scene additively (if not already loaded), collects all GameObjects
        // with TMP_Text along with their current font/material info, then closes it.
        // Checking all loaded scenes first avoids opening a duplicate of a scene that
        // is already open in a multi-scene setup, which would discard in-memory edits.
        private static AssetEntry BuildSceneEntry(string path)
        {
            Scene scene = default;
            bool alreadyLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == path && s.isLoaded)
                {
                    scene = s;
                    alreadyLoaded = true;
                    break;
                }
            }

            if (!alreadyLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (!scene.IsValid())
                    return new AssetEntry { Path = path, Name = System.IO.Path.GetFileName(path) };
            }

            var objects = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<TMP_Text>(true))
                .GroupBy(t => t.gameObject)
                .Select(g => MakeObjectEntry(g.First()))
                .ToList();

            if (!alreadyLoaded)
                EditorSceneManager.CloseScene(scene, true);

            return new AssetEntry
            {
                Path = path,
                Name = System.IO.Path.GetFileName(path),
                Objects = objects,
            };
        }

        private static AssetEntry BuildPrefabEntry(string path)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var objects = root != null
                ? root.GetComponentsInChildren<TMP_Text>(true)
                      .GroupBy(t => t.gameObject)
                      .Select(g => MakeObjectEntry(g.First()))
                      .ToList()
                : new List<ObjectEntry>();

            return new AssetEntry
            {
                Path = path,
                Name = System.IO.Path.GetFileName(path),
                Objects = objects,
            };
        }

        private static string GetHierarchyPath(GameObject go)
        {
            var parts = new List<string>();
            var t = go.transform;
            while (t != null) { parts.Add(t.name); t = t.parent; }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static ObjectEntry MakeObjectEntry(TMP_Text text) => new ObjectEntry
        {
            HierarchyPath = GetHierarchyPath(text.gameObject),
            Name = text.gameObject.name,
            FontName = text.font != null ? text.font.name : "None",
            MaterialName = text.fontSharedMaterial != null
                ? text.fontSharedMaterial.name : "None",
            Selected = false,
            CachedInfoWidth = 0f,
        };

        // Scoped to the font's folder — TMP material presets always live alongside
        // the font. Avoids a full project scan which triggers the search indexer,
        // causing lag and native memory leaks.
        private void RebuildMaterialList()
        {
            _materialsScanned = false;
            _sdfMaterials = System.Array.Empty<Material>();
            _sdfMatNames = System.Array.Empty<string>();
            _sdfMatIndex = 0;

            if (_targetFont == null) return;

            Texture2D atlas = _targetFont.atlasTexture;
            if (atlas == null)
            {
                _sdfMatNames = new[] { "Atlas not ready" };
                _materialsScanned = true;
                return;
            }

            string fontPath = AssetDatabase.GetAssetPath(_targetFont);
            // Guard: GetAssetPath returns empty string for assets not in the database.
            // An empty search path would fall back to a full project scan.
            if (string.IsNullOrEmpty(fontPath))
            {
                _sdfMatNames = new[] { "Font not in AssetDatabase" };
                _materialsScanned = true;
                return;
            }

            string fontFolder = System.IO.Path.GetDirectoryName(fontPath)
                                    ?.Replace('\\', '/') ?? "Assets";

            _sdfMaterials = AssetDatabase
                .FindAssets("t:Material", new[] { fontFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.LoadAssetAtPath<Material>(p))
                .Where(m => m != null &&
                            m.HasProperty("_MainTex") &&
                            m.GetTexture("_MainTex") == atlas)
                .OrderBy(m => m.name)
                .ToArray();

            _sdfMatNames = _sdfMaterials.Select(m => m.name).ToArray();

            // Try to restore the previously saved material. Fall back to the
            // font's own base material if the saved one isn't in the list.
            Material savedMat = _settings != null ? _settings.lastMaterial : null;
            _sdfMatIndex = savedMat != null
                ? System.Array.IndexOf(_sdfMaterials, savedMat)
                : -1;

            if (_sdfMatIndex < 0)
                _sdfMatIndex = System.Array.IndexOf(_sdfMaterials, _targetFont.material);

            if (_sdfMatIndex < 0) _sdfMatIndex = 0;

            _materialsScanned = true;
        }

        private Material SelectedMaterial
            => _sdfMaterials.Length > 0 ? _sdfMaterials[_sdfMatIndex] : null;

        // -------------------------------------------------------------------------
        // Scan result persistence — survives play mode and window reopen via SO.
        // -------------------------------------------------------------------------

        private void SaveScanResults()
        {
            if (_settings == null) return;
            _settings.lastScanCompleted = true;
            _settings.lastScanTimestamp = DateTime.UtcNow.Ticks;
            _settings.lastScanScenes = _scenes.Select(ToSerialized).ToList();
            _settings.lastScanPrefabs = _prefabs.Select(ToSerialized).ToList();
            _settings.Save();
        }

        private void LoadScanResults()
        {
            if (_settings == null || !_settings.lastScanCompleted) return;
            _scenes = (_settings.lastScanScenes ?? new()).Select(FromSerialized).ToList();
            _prefabs = (_settings.lastScanPrefabs ?? new()).Select(FromSerialized).ToList();
            _selectedSceneCount = 0;
            _selectedPrefabCount = 0;
            _hasScanned = true;
            ValidateScanResults();
        }

        // Removes entries whose assets have been deleted since the last scan.
        // Cheap: only checks GUIDs, never opens a scene or loads a prefab.
        private void ValidateScanResults()
        {
            int removedS = _scenes.RemoveAll(e =>
                string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(e.Path)));
            int removedP = _prefabs.RemoveAll(e =>
                string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(e.Path)));

            if (removedS > 0 || removedP > 0)
            {
                _hasStaleResults = true;
                SaveScanResults(); // persist the pruned list
            }
        }

        private string GetScanAgeText()
        {
            if (_settings == null || _settings.lastScanTimestamp == 0) return null;
            var elapsed = DateTime.UtcNow -
                new DateTime(_settings.lastScanTimestamp, DateTimeKind.Utc);
            if (elapsed.TotalMinutes < 1) return "just now";
            if (elapsed.TotalHours < 1) return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours}h ago";
            if (elapsed.TotalDays < 30) return $"{(int)elapsed.TotalDays}d ago";
            return $"{(int)(elapsed.TotalDays / 30)}mo ago";
        }

        private static TmpFontReplacerSettings.SerializedAssetEntry ToSerialized(AssetEntry e) =>
            new TmpFontReplacerSettings.SerializedAssetEntry
            {
                Path = e.Path,
                Name = e.Name,
                Objects = e.Objects.Select(o =>
                    new TmpFontReplacerSettings.SerializedObjectEntry
                    {
                        HierarchyPath = o.HierarchyPath,
                        Name = o.Name,
                        FontName = o.FontName,
                        MaterialName = o.MaterialName,
                    }).ToList(),
            };

        private static AssetEntry FromSerialized(TmpFontReplacerSettings.SerializedAssetEntry s) =>
            new AssetEntry
            {
                Path = s.Path,
                Name = s.Name,
                Objects = s.Objects.Select(o =>
                    new ObjectEntry
                    {
                        HierarchyPath = o.HierarchyPath,
                        Name = o.Name,
                        FontName = o.FontName,
                        MaterialName = o.MaterialName,
                        Selected = false,
                        CachedInfoWidth = 0f,
                    }).ToList(),
            };

        // -------------------------------------------------------------------------
        // Selection helpers
        // -------------------------------------------------------------------------

        private void SetAssetSelected(AssetEntry entry, List<AssetEntry> list, bool value)
        {
            entry.Selected = value;
            foreach (var obj in entry.Objects)
                obj.Selected = value;
            // Sync cached child count with the new blanket selection state.
            entry.SelectedObjectCount = value ? entry.Objects.Count : 0;
            UpdateListCount(list);
        }

        private void SetObjectSelected(AssetEntry asset, List<AssetEntry> list,
            ObjectEntry obj, bool value)
        {
            if (obj.Selected == value) return;
            obj.Selected = value;
            asset.SelectedObjectCount += value ? 1 : -1;
            asset.Selected = asset.SelectedObjectCount > 0;
            UpdateListCount(list);
        }

        private void SetAllSelected(List<AssetEntry> list, bool value)
        {
            foreach (var e in list)
            {
                e.Selected = value;
                foreach (var o in e.Objects) o.Selected = value;
                e.SelectedObjectCount = value ? e.Objects.Count : 0;
            }
            if (list == _scenes) _selectedSceneCount = value ? list.Count : 0;
            else _selectedPrefabCount = value ? list.Count : 0;
        }

        private void UpdateListCount(List<AssetEntry> list)
        {
            int count = 0;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Selected) count++;
            if (list == _scenes) _selectedSceneCount = count;
            else _selectedPrefabCount = count;
        }

        // -------------------------------------------------------------------------
        // OnGUI
        // -------------------------------------------------------------------------

        private void OnGUI()
        {
            // Cache on Repaint only — position.height is unreliable during Layout.
            if (Event.current.type == EventType.Repaint)
                _windowHeight = position.height;

            EnsureStyles();
            DrawFontSection();
            EditorGUIUtils.DrawHorizontalSeparator();
            DrawExclusionSection();
            EditorGUIUtils.DrawHorizontalSeparator();
            DrawAssetSection();
            GUILayout.FlexibleSpace();
            EditorGUIUtils.DrawHorizontalSeparator();
            DrawFooter();
        }

        private static void DrawSectionHeader(string title)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.76f, 0.76f, 0.76f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height),
                new Color(0.27f, 0.55f, 0.98f));
            EditorGUI.LabelField(
                new Rect(rect.x + 10f, rect.y, rect.width - 10f, rect.height),
                title, EditorStyles.boldLabel);
        }


        // -------------------------------------------------------------------------
        // Section 1 — Font Settings
        // -------------------------------------------------------------------------

        private void DrawFontSection()
        {
            DrawSectionHeader("Font Settings");
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            var newFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "Font Asset", _targetFont, typeof(TMP_FontAsset), allowSceneObjects: false);
            if (EditorGUI.EndChangeCheck())
            {
                _targetFont = newFont;
                RebuildMaterialList();
                SaveSettings();
            }

            EditorGUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (_targetFont != null && _materialsScanned && _sdfMaterials.Length > 0)
                {
                    EditorGUI.BeginChangeCheck();
                    _sdfMatIndex = EditorGUILayout.Popup(
                        "SDF Material", _sdfMatIndex, _sdfMatNames);
                    if (EditorGUI.EndChangeCheck())
                        SaveSettings();
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.Popup("SDF Material", 0,
                            new[] { _targetFont == null
                                ? "-"
                                : !_materialsScanned
                                    ? "Not scanned"
                                    : "No presets found" });
                }

                // Re-scans the font folder for material presets. Use after
                // creating a new material preset since the window was opened.
                using (new EditorGUI.DisabledScope(_targetFont == null))
                {
                    if (GUILayout.Button(
                            new GUIContent("↺", "Refresh material list for this font"),
                            EditorStyles.miniButton,
                            GUILayout.Width(22)))
                    {
                        RebuildMaterialList();
                        SaveSettings();
                    }
                }
            }

            EditorGUILayout.Space(4);
        }

        // -------------------------------------------------------------------------
        // Section 2 — Excluded Folders
        // -------------------------------------------------------------------------

        private void DrawExclusionSection()
        {
            Rect hr = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(hr, EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.76f, 0.76f, 0.76f));
            EditorGUI.DrawRect(new Rect(hr.x, hr.y, 3f, hr.height),
                new Color(0.27f, 0.55f, 0.98f));

            _exclusionFoldout = EditorGUI.Foldout(
                new Rect(hr.x + 6f, hr.y, hr.width - 80f, hr.height),
                _exclusionFoldout, "Excluded Folders", true, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUI.Button(
                        new Rect(hr.xMax - 72f, hr.y + 3f, 68f, 16f),
                        "Rescan", EditorStyles.miniButton))
                    Scan();
            }

            if (!_exclusionFoldout) return;

            EditorGUILayout.Space(2);

            if (_excludedFolders.Count > 0)
            {
                float maxH = 20f * Mathf.Min(_excludedFolders.Count, 4);
                using (var sv = new EditorGUILayout.ScrollViewScope(
                    _exclusionScroll, GUILayout.Height(maxH)))
                {
                    _exclusionScroll = sv.scrollPosition;
                    for (int i = _excludedFolders.Count - 1; i >= 0; i--)
                    {
                        using (new EditorGUILayout.HorizontalScope(RowStyle(i)))
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(
                                _excludedFolders[i], typeof(DefaultAsset),
                                allowSceneObjects: false, GUILayout.Height(16));
                            EditorGUI.EndDisabledGroup();

                            if (GUILayout.Button("✕", _iconBtnStyle))
                            {
                                _excludedFolders.RemoveAt(i);
                                SaveSettings();
                                break;
                            }
                        }
                    }
                }
            }

            Rect dropRect = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect,
                new GUIContent("", "Drag folders from the Project window to exclude them"),
                EditorStyles.helpBox);
            HandleFolderDrop(dropRect);
            EditorGUILayout.Space(2);
        }

        private void HandleFolderDrop(Rect dropRect)
        {
            Event evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition)) return;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;

            bool anyFolder = DragAndDrop.objectReferences.Any(
                obj => obj is DefaultAsset da &&
                       AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(da)));

            DragAndDrop.visualMode = anyFolder
                ? DragAndDropVisualMode.Copy
                : DragAndDropVisualMode.Rejected;

            if (evt.type == EventType.DragPerform && anyFolder)
            {
                DragAndDrop.AcceptDrag();
                bool added = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is not DefaultAsset da) continue;
                    if (!AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(da))) continue;
                    if (_excludedFolders.Contains(da)) continue;
                    _excludedFolders.Add(da);
                    added = true;
                }

                if (added) SaveSettings();
            }

            evt.Use();
        }

        // -------------------------------------------------------------------------
        // Section 3 — Asset Selection
        // -------------------------------------------------------------------------

        private void DrawAssetSection()
        {
            DrawSectionHeader("Assets to Process");
            EditorGUILayout.Space(2);

            var scenes = _scenes;
            var prefabs = _prefabs;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Toggle(_tab == 0,
                        $"Scenes ({scenes.Count})", EditorStyles.toolbarButton))
                    _tab = 0;
                if (GUILayout.Toggle(_tab == 1,
                        $"Prefabs ({prefabs.Count})", EditorStyles.toolbarButton))
                    _tab = 1;

                GUILayout.FlexibleSpace();

                var list = _tab == 0 ? scenes : prefabs;
                int selectedCount = _tab == 0 ? _selectedSceneCount : _selectedPrefabCount;

                GUILayout.Label($"{selectedCount} / {list.Count}",
                    EditorStyles.toolbarButton, GUILayout.Width(56));

                if (GUILayout.Button("All", EditorStyles.toolbarButton, GUILayout.Width(36)))
                    SetAllSelected(list, true);
                if (GUILayout.Button("None", EditorStyles.toolbarButton, GUILayout.Width(42)))
                    SetAllSelected(list, false);
            }

            if (_hasScanned)
            {
                string ageText = GetScanAgeText();
                if (ageText != null)
                    EditorGUILayout.LabelField($"Last scanned: {ageText}",
                        EditorStyles.centeredGreyMiniLabel);

                if (_hasStaleResults)
                    EditorGUILayout.HelpBox(
                        "Some scanned assets no longer exist. Rescan to refresh.",
                        MessageType.Warning);
            }

            var currentList = _tab == 0 ? scenes : prefabs;

            // Plain loop — avoids per-frame LINQ allocation.
            float rowH = EditorGUIUtility.singleLineHeight + 2f;
            int visibleRows = 0;
            for (int i = 0; i < currentList.Count; i++)
                visibleRows += 1 + (currentList[i].Foldout ? currentList[i].Objects.Count : 0);

            float contentH = Mathf.Max(visibleRows * rowH, 1f);
            float available = Mathf.Max(_windowHeight - 280f, 60f);
            float listHeight = Mathf.Min(contentH, available);

            Vector2 scroll = _tab == 0 ? _sceneScroll : _prefabScroll;

            using (var sv = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.Height(listHeight)))
            {
                scroll = sv.scrollPosition;

                if (currentList.Count == 0)
                {
                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField(
                        _hasScanned ? "No assets found." : "Press Rescan to scan the project.",
                        EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    for (int i = 0; i < currentList.Count; i++)
                        DrawAssetRow(currentList[i], currentList, i);
                }
            }

            if (_tab == 0) _sceneScroll = scroll;
            else _prefabScroll = scroll;
        }

        private void DrawAssetRow(AssetEntry entry, List<AssetEntry> list, int index)
        {
            // All entries have at least one Object (enforced at scan time),
            // so foldout is always shown.
            using (new EditorGUILayout.HorizontalScope(RowStyle(index)))
            {
                Rect foldRect = GUILayoutUtility.GetRect(
                    14f, 20f, GUILayout.Width(14), GUILayout.Height(20));
                bool newFoldout = EditorGUI.Foldout(foldRect, entry.Foldout, GUIContent.none);
                if (newFoldout != entry.Foldout)
                    entry.Foldout = newFoldout;

                // BeginChangeCheck ensures SetAssetSelected fires only on actual
                // user click, never on repaint — prevents the select-all bug.
                var state = entry.ChildCheckState();
                bool mixed = state == AssetEntry.CheckState.Mixed;
                bool current = state == AssetEntry.CheckState.All;

                EditorGUI.BeginChangeCheck();
                using (new EditorGUI.MixedValueScope(mixed))
                    EditorGUILayout.Toggle(current, GUILayout.Width(14));

                if (EditorGUI.EndChangeCheck())
                    SetAssetSelected(entry, list, state != AssetEntry.CheckState.All);

                EditorGUILayout.LabelField(
                    new GUIContent(entry.Name, entry.Path),
                    GUILayout.Height(16));

                if (GUILayout.Button(
                        new GUIContent("→", "Ping in Project window"),
                        _iconBtnStyle))
                    EditorGUIUtility.PingObject(
                        AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.Path));
            }

            if (!entry.Foldout) return;

            for (int j = 0; j < entry.Objects.Count; j++)
            {
                var obj = entry.Objects[j];

                Rect rowRect = GUILayoutUtility.GetRect(
                    0f, 20f, GUILayout.ExpandWidth(true), GUILayout.Height(20));

                if (Event.current.type == EventType.Repaint)
                    RowStyle(j, child: true).Draw(rowRect, false, false, false, false);

                const float indent = 28f;
                const float toggleWidth = 18f;
                const float rightMargin = 8f;
                const float minNameWidth = 60f;

                // infoWidth cached per ObjectEntry — nameWidth takes the remainder.
                float infoWidth = GetInfoWidth(obj);
                float totalWidth = rowRect.width - indent - toggleWidth;
                float nameWidth = Mathf.Max(totalWidth - infoWidth - rightMargin, minNameWidth);

                var toggleRect = new Rect(
                    rowRect.x + indent, rowRect.y + 2f,
                    toggleWidth, rowRect.height - 4f);

                var nameRect = new Rect(
                    toggleRect.xMax, rowRect.y,
                    nameWidth, rowRect.height);

                var infoRect = new Rect(
                    nameRect.xMax, rowRect.y,
                    rowRect.xMax - nameRect.xMax - rightMargin, rowRect.height);

                EditorGUI.BeginChangeCheck();
                bool next = EditorGUI.Toggle(toggleRect, obj.Selected);
                if (EditorGUI.EndChangeCheck())
                    SetObjectSelected(entry, list, obj, next);

                EditorGUI.LabelField(nameRect,
                    new GUIContent(obj.Name, "GameObject containing TMP_Text"),
                    EditorStyles.miniLabel);

                EditorGUI.LabelField(infoRect,
                    new GUIContent(obj.InfoText,
                        $"Font: {obj.FontName}\nMaterial: {obj.MaterialName}"),
                    _infoLabelStyle);
            }
        }

        // -------------------------------------------------------------------------
        // Section 4 — Footer
        // -------------------------------------------------------------------------

        private void DrawFooter()
        {
            EditorGUILayout.Space(4);

            int totalSelected = _selectedSceneCount + _selectedPrefabCount;
            bool canRun = _targetFont != null && totalSelected > 0 && !EditorApplication.isPlaying;

            EditorGUILayout.LabelField(
                $"{_selectedSceneCount} scene(s)  ·  {_selectedPrefabCount} prefab(s) selected",
                EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(!canRun))
            {
                if (GUILayout.Button("Replace Fonts", GUILayout.Height(28)))
                    RunReplacement();
            }

            if (!canRun)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField(
                    _targetFont == null
                        ? "Assign a Font Asset to continue."
                        : EditorApplication.isPlaying
                            ? "Exit play mode to replace fonts."
                            : "Select at least one scene or prefab.",
                    EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.Space(4);
        }

        // -------------------------------------------------------------------------
        // Replacement
        // -------------------------------------------------------------------------

        private void RunReplacement()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Cannot Replace",
                    "Font replacement is not available during play mode.", "OK");
                return;
            }

            Material mat = SelectedMaterial;

            bool confirm = EditorUtility.DisplayDialog(
                "Replace TMP Fonts",
                $"Font: {_targetFont.name}\nMaterial: {(mat != null ? mat.name : "none")}" +
                "\n\nCommit or stash before proceeding.",
                "Replace", "Cancel");

            if (!confirm) return;

            int totalFound = 0;
            int totalReplaced = 0;

            string newFontName = _targetFont.name;
            string newMatName = mat != null ? mat.name : "None";

            // Updates display strings in-place and invalidates cached width so
            // the info label remeasures on next draw without a rescan.
            void UpdateEntryInfo(AssetEntry asset, string hierarchyPath)
            {
                foreach (var obj in asset.Objects)
                {
                    if (obj.HierarchyPath != hierarchyPath) continue;
                    obj.FontName = newFontName;
                    obj.MaterialName = newMatName;
                    obj.InvalidateInfoWidth();
                }
            }

            // Undo.RecordObject omitted for prefabs — EditPrefabContentsScope manages
            // its own serialization; Undo is not valid in asset-editing mode.
            void ProcessTextInPrefab(TMP_Text text, AssetEntry entry)
            {
                totalFound++;
                bool changed = false;

                if (text.font != _targetFont)
                {
                    text.font = _targetFont;
                    EditorUtility.SetDirty(text);
                    changed = true;
                }

                if (mat != null && text.fontSharedMaterial != mat)
                {
                    text.fontSharedMaterial = mat;
                    EditorUtility.SetDirty(text);
                    changed = true;
                }

                if (changed)
                {
                    totalReplaced++;
                    UpdateEntryInfo(entry, GetHierarchyPath(text.gameObject));
                }
            }

            void ProcessTextInScene(TMP_Text text, AssetEntry entry)
            {
                totalFound++;
                bool changed = false;

                if (text.font != _targetFont)
                {
                    Undo.RecordObject(text, "Replace TMP Font");
                    text.font = _targetFont;
                    EditorUtility.SetDirty(text);
                    changed = true;
                }

                if (mat != null && text.fontSharedMaterial != mat)
                {
                    Undo.RecordObject(text, "Replace TMP Font");
                    text.fontSharedMaterial = mat;
                    EditorUtility.SetDirty(text);
                    changed = true;
                }

                if (changed)
                {
                    totalReplaced++;
                    UpdateEntryInfo(entry, GetHierarchyPath(text.gameObject));
                }
            }

            // --- Prefabs ---
            foreach (var entry in _prefabs.Where(e => e.Selected))
            {
                var selectedPaths = new HashSet<string>(
                    entry.Objects.Where(o => o.Selected).Select(o => o.HierarchyPath));

                using var scope = new PrefabUtility.EditPrefabContentsScope(entry.Path);
                foreach (var text in scope.prefabContentsRoot
                    .GetComponentsInChildren<TMP_Text>(true))
                {
                    if (selectedPaths.Contains(GetHierarchyPath(text.gameObject)))
                        ProcessTextInPrefab(text, entry);
                }
            }

            // --- Scenes ---
            foreach (var entry in _scenes.Where(e => e.Selected))
            {
                var selectedPaths = new HashSet<string>(
                    entry.Objects.Where(o => o.Selected).Select(o => o.HierarchyPath));

                // Check all currently loaded scenes before opening additively.
                Scene scene = default;
                bool alreadyLoaded = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    if (s.path == entry.Path && s.isLoaded)
                    {
                        scene = s;
                        alreadyLoaded = true;
                        break;
                    }
                }
                if (!alreadyLoaded)
                    scene = EditorSceneManager.OpenScene(entry.Path, OpenSceneMode.Additive);

                if (!scene.IsValid()) continue;

                bool dirty = false;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (!selectedPaths.Contains(GetHierarchyPath(text.gameObject))) continue;
                        int before = totalReplaced;
                        ProcessTextInScene(text, entry);
                        if (totalReplaced > before) dirty = true;
                    }

                // An already open scene may hold the user's unsaved edits, so it's left dirty for them to save
                if (dirty && alreadyLoaded) EditorSceneManager.MarkSceneDirty(scene);
                else if (dirty) EditorSceneManager.SaveScene(scene);
                if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true);
            }

            AssetDatabase.SaveAssets();
            SaveScanResults();

            string message = totalFound == 0
                ? "No TMP_Text components found in the selected assets."
                : totalReplaced == 0
                    ? $"Found {totalFound} TMP_Text component(s) — all already up to date. Nothing changed."
                    : $"Found {totalFound} TMP_Text component(s). Updated {totalReplaced}, {totalFound - totalReplaced} already correct.";

            EditorUtility.DisplayDialog("Done", message, "OK");
        }
    }
}
#endif