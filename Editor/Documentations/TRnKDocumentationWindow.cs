#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TRnK.Toolkit
{
    internal sealed class TRnKDocumentationWindow : EditorWindow
    {
        [MenuItem("Tools/TRnK/Documentation", priority = 100)]
        public static void Open()
        {
            var w = GetWindow<TRnKDocumentationWindow>("TRnK.Toolkit Docs");
            w.minSize = new Vector2(860f, 560f);
            w.Show();
        }

        // Internal view-model wrapping SO entry — keeps the window decoupled from Cat enum
        private sealed class Entry
        {
            public string Title;
            public string Namespace;
            public string Summary;
            public string Description;
            public string Code;
            public string[] Tags;
            public DocCategory Category;
            public DocMember[] Members;
        }

        private List<Entry> _db;
        private int _selCat = -1;   // -1 = All
        private string _search = "";
        private Vector2 _sideScroll;
        private Vector2 _mainScroll;
        private bool _stylesReady;
        // Per-entry fold state (key = Title)
        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
        // Per-entry highlighted code cache
        private readonly Dictionary<string, string> _codeCache = new Dictionary<string, string>();
        // Per-entry copy feedback (key = Title)
        private readonly Dictionary<string, double> _copyUntil = new Dictionary<string, double>();
        // Per-member foldout state (key = "Title::Signature")
        private readonly Dictionary<string, bool> _memberFoldouts = new Dictionary<string, bool>();
        // Per-member copy feedback (key = "Title::Signature")
        private readonly Dictionary<string, double> _memberCopyUntil = new Dictionary<string, double>();
        // Per-member highlighted code cache
        private readonly Dictionary<string, string> _memberCodeCache = new Dictionary<string, string>();
        // Stable content width sampled once per Layout pass from the scroll view
        private float _contentWidth;
        private float _lastContentWidth;
        // Truncated summary cache (key = entry title); cleared on window resize
        private readonly Dictionary<string, string> _summaryCache = new Dictionary<string, string>();
        // Member row height cache (key = "Title::Sig:cw"); cleared on window resize
        private readonly Dictionary<string, float> _rowHeightCache = new Dictionary<string, float>();

        private const float SidebarW = 236f;
        private const float CatRowH = 30f;
        private const float CardPad = 16f;

        private static Color Accent => new Color(0.22f, 0.47f, 0.82f, 1f);
        private static Color AccentText => new Color(0.45f, 0.72f, 1.00f, 1f);

        private Color SidebarBg => Skin ? new Color(0.17f, 0.18f, 0.20f) : new Color(0.86f, 0.87f, 0.89f);
        private Color MainBg => Skin ? new Color(0.19f, 0.20f, 0.22f) : new Color(0.91f, 0.92f, 0.94f);
        private Color Sep => Skin ? new Color(0.27f, 0.27f, 0.29f) : new Color(0.70f, 0.70f, 0.73f);
        private Color RowHover => Skin ? new Color(0.24f, 0.25f, 0.28f) : new Color(0.79f, 0.81f, 0.85f);
        private Color GroupHdr => Skin ? new Color(0.21f, 0.22f, 0.25f) : new Color(0.82f, 0.83f, 0.86f);
        private Color SummaryBox => Skin ? new Color(0.19f, 0.24f, 0.36f) : new Color(0.88f, 0.92f, 0.98f);
        private Color BadgeBg => Skin ? new Color(0.27f, 0.28f, 0.31f) : new Color(0.73f, 0.74f, 0.77f);
        private Color TextMain => Skin ? new Color(0.88f, 0.88f, 0.90f) : new Color(0.12f, 0.12f, 0.15f);
        private Color TextDim => Skin ? new Color(0.55f, 0.55f, 0.58f) : new Color(0.42f, 0.42f, 0.46f);
        private Color TextCode => Skin ? new Color(0.80f, 0.84f, 0.92f) : new Color(0.14f, 0.18f, 0.26f);
        private Color CardCollapsed => Skin ? new Color(0.22f, 0.23f, 0.26f) : new Color(0.87f, 0.88f, 0.90f);
        // Member table — three clearly distinct layers
        // Blue-tinted section title bars ("Properties", "Public Methods", "Callbacks")
        private Color ApiSectionHdr => Skin ? new Color(0.17f, 0.24f, 0.38f) : new Color(0.72f, 0.80f, 0.92f);
        // Very dark neutral column-header bar ("NAME / DESCRIPTION")
        private Color MemberTableHdrBg => Skin ? new Color(0.13f, 0.14f, 0.17f) : new Color(0.74f, 0.75f, 0.78f);
        // Clearly lighter neutral rows
        private Color MemberRowBg => Skin ? new Color(0.25f, 0.26f, 0.29f) : new Color(0.88f, 0.89f, 0.92f);
        // Code block — terminal-dark body + teal-accented header
        private Color CodeBlockBody => Skin ? new Color(0.09f, 0.10f, 0.12f) : new Color(0.95f, 0.96f, 0.97f);
        private Color CodeBlockHdr => Skin ? new Color(0.10f, 0.18f, 0.19f) : new Color(0.71f, 0.83f, 0.81f);
        private Color CodeBlockAccent => Skin ? new Color(0.18f, 0.70f, 0.60f) : new Color(0.04f, 0.50f, 0.42f);

        private bool Skin => EditorGUIUtility.isProSkin;

        private GUIStyle _styleCatName;
        private GUIStyle _styleEntryTitle;
        private GUIStyle _styleEntrySummary;
        private GUIStyle _styleEntryNs;
        private GUIStyle _styleGroupHdr;
        private GUIStyle _styleHeaderTitle;
        private GUIStyle _styleTag;
        private GUIStyle _styleSummaryBox;
        private GUIStyle _styleDesc;
        private GUIStyle _styleCode;
        private GUIStyle _styleSearch;
        private GUIStyle _styleBadge;
        private GUIStyle _styleArrow;
        private GUIStyle _styleSectionHdr;
        private GUIStyle _styleMemberSig;
        private GUIStyle _styleMemberSummary;
        private GUIStyle _styleMemberKind;

        private void OnEnable()
        {
            LoadDatabase();
        }

        private void LoadDatabase()
        {
            var entries = TRnKDocDatabase.GetEntries();
            _db = new List<Entry>(entries.Count);
            foreach (var e in entries)
            {
                _db.Add(new Entry
                {
                    Title = e.Title,
                    Namespace = e.Namespace,
                    Summary = e.Summary,
                    Description = e.Description,
                    Code = e.Code,
                    Tags = e.Tags,
                    Category = e.Category,
                    Members = e.Members
                });
            }
        }

        private void OnGUI()
        {
            if (!_stylesReady)
                BuildStyles();

            // Repaint while any copy feedback is active
            foreach (var kv in _copyUntil)
            {
                if (EditorApplication.timeSinceStartup < kv.Value) { Repaint(); break; }
            }
            foreach (var kv in _memberCopyUntil)
            {
                if (EditorApplication.timeSinceStartup < kv.Value) { Repaint(); break; }
            }

            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), MainBg);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSidebar();
                DrawVerticalSep();
                DrawMain();
            }
        }

        private void DrawSidebar()
        {
            using (new EditorGUILayout.VerticalScope(
                GUILayout.Width(SidebarW), GUILayout.ExpandHeight(true)))
            {
                var sideRect = new Rect(0, 0, SidebarW, position.height);
                EditorGUI.DrawRect(sideRect, SidebarBg);

                DrawLogoBlock();
                DrawHSep();

                GUILayout.Space(6f);
                DrawSearchField();
                GUILayout.Space(4f);
                DrawHSep();

                _sideScroll = EditorGUILayout.BeginScrollView(
                    _sideScroll, GUIStyle.none, GUIStyle.none,
                    GUILayout.ExpandHeight(true));

                DrawCategoryRow(-1, "★", "All");
                foreach (DocCategory c in Enum.GetValues(typeof(DocCategory)))
                {
                    string label = c switch
                    {
                        DocCategory.EditorTools => "Editor Tools",
                        DocCategory.TRnKSignal => "TRnK Signal",
                        DocCategory.TRnKFlow => "TRnK Flow",
                        DocCategory.TRnKTimer => "TRnK Timer",
                        _ => c.ToString()
                    };
                    DrawCategoryRow((int)c, GetCatIcon(c), label);
                }

                GUILayout.Space(6f);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawLogoBlock()
        {
            GUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12f);

                var iconRect = GUILayoutUtility.GetRect(30f, 30f, GUILayout.Width(30f), GUILayout.Height(30f));
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(iconRect, Accent);
                    var inner = new Rect(iconRect.x + 3, iconRect.y + 3, 24f, 24f);
                    EditorGUI.DrawRect(inner, SidebarBg);
                    var nStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 15,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Accent }
                    };
                    GUI.Label(iconRect, "N", nStyle);
                }

                GUILayout.Space(8f);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(4f);
                    GUILayout.Label("TRnK.Toolkit", new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 13,
                        normal = { textColor = TextMain }
                    });
                    GUILayout.Label("Documentation", new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = AccentText }
                    });
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(8f);
        }

        private void DrawSearchField()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10f);
                EditorGUI.BeginChangeCheck();
                _search = EditorGUILayout.TextField(_search, _styleSearch,
                    GUILayout.Width(SidebarW - 20f), GUILayout.Height(24f));
                if (EditorGUI.EndChangeCheck())
                {
                    _mainScroll = Vector2.zero;
                    Repaint();
                }
                GUILayout.Space(10f);
            }
        }

        private void DrawCategoryRow(int index, string icon, string label)
        {
            bool active = _selCat == index;
            var rowRect = GUILayoutUtility.GetRect(SidebarW, CatRowH,
                GUILayout.Width(SidebarW), GUILayout.Height(CatRowH));
            bool hovered = rowRect.Contains(Event.current.mousePosition);
            int count = GetCount(index);

            if (Event.current.type == EventType.Repaint)
            {
                if (active)
                    EditorGUI.DrawRect(rowRect, new Color(Accent.r, Accent.g, Accent.b, 0.14f));
                else if (hovered)
                    EditorGUI.DrawRect(rowRect, RowHover);

                if (active)
                    EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y + 3f, 3f, rowRect.height - 6f), Accent);

                var iconRect = new Rect(rowRect.x + 10f, rowRect.y, 20f, rowRect.height);
                var iconStyle = new GUIStyle(_styleCatName) { fontSize = 11 };
                GUI.Label(iconRect, icon, iconStyle);

                var labelRect = new Rect(rowRect.x + 30f, rowRect.y, rowRect.width - 70f, rowRect.height);
                var labelStyle = new GUIStyle(_styleCatName)
                {
                    normal = { textColor = active ? Color.white : TextMain }
                };
                GUI.Label(labelRect, label, labelStyle);

                string badge = count.ToString();
                float bw = Mathf.Max(22f, _styleBadge.CalcSize(new GUIContent(badge)).x + 10f);
                var badgeRect = new Rect(rowRect.xMax - bw - 8f,
                    rowRect.y + (CatRowH - 16f) * 0.5f, bw, 16f);
                EditorGUI.DrawRect(badgeRect,
                    active ? new Color(1f, 1f, 1f, 0.18f) : BadgeBg);
                var badgeStyle = new GUIStyle(_styleBadge)
                {
                    normal = { textColor = active ? Color.white : TextDim }
                };
                GUI.Label(badgeRect, badge, badgeStyle);
            }

            if (Event.current.type == EventType.MouseDown
                && rowRect.Contains(Event.current.mousePosition))
            {
                _selCat = index;
                _mainScroll = Vector2.zero;
                Event.current.Use();
                Repaint();
            }
        }

        private void DrawMain()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                DrawMainHeader();
                DrawHSep();
                DrawTutorialView();
            }
        }

        private void DrawMainHeader()
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(40f)))
            {
                GUILayout.Space(14f);
                string title = _selCat == -1 ? "All"
                    : ((DocCategory)_selCat) switch
                    {
                        DocCategory.EditorTools => "Editor Tools",
                        DocCategory.TRnKSignal => "TRnK Signal",
                        DocCategory.TRnKFlow => "TRnK Flow",
                        DocCategory.TRnKTimer => "TRnK Timer",
                        var c => c.ToString()
                    };
                GUILayout.Label(title, _styleHeaderTitle,
                    GUILayout.Height(40f), GUILayout.ExpandWidth(false));
                GUILayout.FlexibleSpace();
                // Vertically center buttons inside the 40px row
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Expand All", EditorStyles.miniButton,
                            GUILayout.Height(20f), GUILayout.Width(76f)))
                            SetAllFoldouts(true);
                        GUILayout.Space(4f);
                        if (GUILayout.Button("Collapse All", EditorStyles.miniButton,
                            GUILayout.Height(20f), GUILayout.Width(82f)))
                            SetAllFoldouts(false);
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Space(12f);
            }
        }

        private void SetAllFoldouts(bool open)
        {
            var entries = GetFiltered();
            foreach (var e in entries)
                _foldouts[e.Title] = open;
            Repaint();
        }

        // ── Tutorial (foldout) view ────────────────────────────────────────────
        private void DrawTutorialView()
        {
            var entries = GetFiltered();
            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);

            // Sample content width once per frame from the first rect in the scroll view.
            // Use a zero-height probe rect so it doesn't take up visible space.
            var probe = EditorGUILayout.GetControlRect(false, 0f);
            if (Event.current.type == EventType.Repaint && probe.width > 10f)
            {
                if (Mathf.Abs(probe.width - _lastContentWidth) > 1f)
                {
                    _lastContentWidth = probe.width;
                    _summaryCache.Clear();
                    _rowHeightCache.Clear();
                }
                _contentWidth = probe.width;
            }

            if (entries.Count == 0)
            {
                GUILayout.Space(50f);
                EditorGUILayout.LabelField("No results found.",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 12 });
            }
            else if (_selCat == -1 && string.IsNullOrEmpty(_search.Trim()))
            {
                foreach (DocCategory cat in Enum.GetValues(typeof(DocCategory)))
                {
                    var group = entries.FindAll(e => e.Category == cat);
                    if (group.Count == 0) continue;
                    string catLabel = cat switch
                    {
                        DocCategory.EditorTools => "Editor Tools",
                        DocCategory.TRnKSignal => "TRnK Signal",
                        DocCategory.TRnKFlow => "TRnK Flow",
                        DocCategory.TRnKTimer => "TRnK Timer",
                        _ => cat.ToString()
                    };
                    DrawGroupHeader(GetCatIcon(cat) + "  " + catLabel);
                    foreach (var e in group)
                        DrawEntryCard(e);
                    GUILayout.Space(4f);
                }
            }
            else
            {
                DocCategory? prevCat = null;
                foreach (var e in entries)
                {
                    if (_selCat == -1 && e.Category != prevCat)
                    {
                        string catLabel = e.Category switch
                        {
                            DocCategory.EditorTools => "Editor Tools",
                            DocCategory.TRnKSignal => "TRnK Signal",
                            DocCategory.TRnKFlow => "TRnK Flow",
                            DocCategory.TRnKTimer => "TRnK Timer",
                            _ => e.Category.ToString()
                        };
                        DrawGroupHeader(GetCatIcon(e.Category) + "  " + catLabel);
                        prevCat = e.Category;
                    }
                    DrawEntryCard(e);
                }
            }

            GUILayout.Space(16f);
            EditorGUILayout.EndScrollView();
        }

        private void DrawGroupHeader(string label)
        {
            var rect = EditorGUILayout.GetControlRect(false, 24f);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, GroupHdr);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), Accent);
                GUI.Label(new Rect(rect.x + 10f, rect.y, rect.width - 10f, rect.height), label, _styleGroupHdr);
            }
        }

        // ── Entry card (foldout) ───────────────────────────────────────────────
        private void DrawEntryCard(Entry entry)
        {
            if (!_foldouts.TryGetValue(entry.Title, out bool expanded))
            {
                _foldouts[entry.Title] = false;
                expanded = false;
            }

            // ── Card header row ───────────────────────────────────────────────
            // Height 52 = 20 title/ns block (top) + 2-line summary (up to ~28px) + padding
            var headerRect = EditorGUILayout.GetControlRect(false, 52f);
            bool hovered = headerRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                Color hdrBg = expanded
                    ? new Color(Accent.r, Accent.g, Accent.b, 0.10f)
                    : (hovered ? RowHover : CardCollapsed);
                EditorGUI.DrawRect(headerRect, hdrBg);

                if (expanded)
                    EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, 3f, headerRect.height), Accent);

                string arrow = expanded ? "▾" : "▸";
                GUI.Label(new Rect(headerRect.x + 10f, headerRect.y, 18f, headerRect.height), arrow, _styleArrow);

                // Title + namespace sit in the left ~40% column
                float leftColW = headerRect.width * 0.40f;
                GUI.Label(new Rect(headerRect.x + 30f, headerRect.y + 6f, leftColW, 18f),
                    entry.Title, _styleEntryTitle);
                GUI.Label(new Rect(headerRect.x + 30f, headerRect.y + 26f, leftColW, 14f),
                    entry.Namespace, _styleEntryNs);

                if (!expanded)
                {
                    float summaryX = headerRect.x + leftColW + 36f;
                    float summaryW = headerRect.xMax - summaryX - 10f;
                    if (summaryW > 60f)
                    {
                        // Allow up to 2 lines; truncate to "..." only if it overflows 2 lines
                        string preview = TruncateToFitLines(entry.Summary, _styleEntrySummary, summaryW, 2, entry.Title);
                        float previewH = _styleEntrySummary.CalcHeight(new GUIContent(preview), summaryW);
                        float previewY = headerRect.y + (headerRect.height - previewH) * 0.5f;
                        GUI.Label(new Rect(summaryX, previewY, summaryW, previewH),
                            preview, _styleEntrySummary);
                    }
                }

                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f), Sep);
            }

            if (Event.current.type == EventType.MouseDown
                && headerRect.Contains(Event.current.mousePosition))
            {
                _foldouts[entry.Title] = !expanded;
                Event.current.Use();
                Repaint();
            }

            if (!expanded) return;

            // ── Expanded content ──────────────────────────────────────────────
            // Use _contentWidth (sampled from scroll view) for all height calculations.
            // Subtract CardPad*2 for left+right padding we apply manually.
            float cw = Mathf.Max(100f, _contentWidth - CardPad * 2f);

            GUILayout.Space(8f);

            // Tags
            if (entry.Tags != null && entry.Tags.Length > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(CardPad);
                    foreach (string tag in entry.Tags)
                    {
                        var tagRect = GUILayoutUtility.GetRect(new GUIContent(tag), _styleTag,
                            GUILayout.Height(18f));
                        if (Event.current.type == EventType.Repaint)
                        {
                            EditorGUI.DrawRect(tagRect, new Color(Accent.r, Accent.g, Accent.b, 0.15f));
                            GUI.Label(tagRect, tag, _styleTag);
                        }
                        GUILayout.Space(4f);
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Space(8f);
            }

            // Summary box — full width, with CardPad inset handled inside
            DrawCardSummaryBox(entry, cw);
            GUILayout.Space(8f);

            // Description
            if (!string.IsNullOrEmpty(entry.Description))
            {
                float descH = _styleDesc.CalcHeight(new GUIContent(entry.Description), cw);
                var descR = EditorGUILayout.GetControlRect(false, descH);
                // Inset rect so text starts at CardPad
                var descTextR = new Rect(descR.x + CardPad, descR.y, cw, descH);
                if (Event.current.type == EventType.Repaint)
                    GUI.Label(descTextR, entry.Description, _styleDesc);
                GUILayout.Space(8f);
            }

            // Members section (Properties / Public Methods / Callbacks tables)
            if (entry.Members != null && entry.Members.Length > 0)
            {
                GUILayout.Space(4f);
                DrawMembersSection(entry, cw);
            }

            // Overview code block — hidden when member foldouts are present (each member carries its own example)
            bool hasMembers = entry.Members != null && entry.Members.Length > 0;
            if (!hasMembers && !string.IsNullOrEmpty(entry.Code) && !IsCommentOnlyCode(entry.Code))
            {
                GUILayout.Space(4f);
                DrawCardCodeBlock(entry, cw);
                GUILayout.Space(4f);
            }

            GUILayout.Space(10f);

            var sepRect = EditorGUILayout.GetControlRect(false, 2f);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(sepRect, Sep);
        }

        private void DrawCardSummaryBox(Entry entry, float cw)
        {
            float textH = _styleSummaryBox.CalcHeight(new GUIContent(entry.Summary), cw - 22f);
            float boxH = textH + 22f;
            // GetControlRect gives the full scroll-view-width rect; we inset manually.
            var fullRect = EditorGUILayout.GetControlRect(false, boxH);
            var boxRect = new Rect(fullRect.x + CardPad, fullRect.y, cw, boxH);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(boxRect, SummaryBox);
                EditorGUI.DrawRect(new Rect(boxRect.x, boxRect.y, 4f, boxRect.height), Accent);
                GUI.Label(new Rect(boxRect.x + 14f, boxRect.y + 10f, boxRect.width - 18f, textH),
                    entry.Summary, _styleSummaryBox);
            }
        }

        private void DrawCardCodeBlock(Entry entry, float cw)
        {
            if (!_codeCache.TryGetValue(entry.Title, out string highlighted))
            {
                highlighted = SyntaxHighlight(entry.Code);
                _codeCache[entry.Title] = highlighted;
            }

            bool hasCopyFeedback = _copyUntil.TryGetValue(entry.Title, out double until)
                && EditorApplication.timeSinceStartup < until;

            // Header bar — teal accent
            var hdrFull = EditorGUILayout.GetControlRect(false, 26f);
            var hdrRect = new Rect(hdrFull.x + CardPad, hdrFull.y, cw, 26f);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(hdrRect, CodeBlockHdr);
                EditorGUI.DrawRect(new Rect(hdrRect.x, hdrRect.y, 3f, hdrRect.height), CodeBlockAccent);
                GUI.Label(new Rect(hdrRect.x + 10f, hdrRect.y, 40f, hdrRect.height),
                    "C#", new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = CodeBlockAccent },
                        alignment = TextAnchor.MiddleLeft
                    });
                bool copyHovered = hdrRect.Contains(Event.current.mousePosition);
                GUI.Label(hdrRect, hasCopyFeedback ? "✓ Copied!" : (copyHovered ? "Copy ▸" : "Copy"),
                    new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = hasCopyFeedback
                            ? new Color(0.30f, 0.78f, 0.45f, 1f)
                            : (copyHovered ? TextMain : TextDim) },
                        alignment = TextAnchor.MiddleRight,
                        padding = new RectOffset(0, 10, 0, 0)
                    });
            }

            if (Event.current.type == EventType.MouseDown && hdrRect.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.systemCopyBuffer = entry.Code;
                _copyUntil[entry.Title] = EditorApplication.timeSinceStartup + 1.8;
                Event.current.Use();
                Repaint();
            }

            // Code body — terminal-dark background
            float codeH = _styleCode.CalcHeight(new GUIContent(entry.Code), cw - 28f) + 24f;
            var codeFull = EditorGUILayout.GetControlRect(false, codeH);
            var codeRect = new Rect(codeFull.x + CardPad, codeFull.y, cw, codeH);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(codeRect, CodeBlockBody);
                EditorGUI.DrawRect(new Rect(codeRect.x, codeRect.y, 3f, codeRect.height), CodeBlockAccent);
                GUI.Label(new Rect(codeRect.x + 14f, codeRect.y + 10f, codeRect.width - 28f, codeH - 20f),
                    highlighted, _styleCode);
                EditorGUI.DrawRect(new Rect(codeRect.x, codeRect.yMax - 1f, codeRect.width, 1f),
                    new Color(CodeBlockAccent.r, CodeBlockAccent.g, CodeBlockAccent.b, 0.30f));
            }
        }

        // ── Member tables (Properties / Public Methods / Callbacks) ──────────
        private static readonly DocMemberKind[] MemberKindOrder =
            { DocMemberKind.Property, DocMemberKind.Method, DocMemberKind.Callback };

        private void DrawMembersSection(Entry entry, float cw)
        {
            foreach (var kind in MemberKindOrder)
            {
                // Collect members of this kind that have content
                var group = new List<DocMember>();
                foreach (var m in entry.Members)
                    if (m.Kind == kind) group.Add(m);
                if (group.Count == 0) continue;

                string sectionTitle = kind switch
                {
                    DocMemberKind.Property => "Properties",
                    DocMemberKind.Method   => "Public Methods",
                    DocMemberKind.Callback => "Callbacks",
                    _                      => kind.ToString()
                };

                // Breathing space before each section (except first)
                GUILayout.Space(8f);

                // Section header — blue-tinted, clearly distinct from rows and code blocks
                var hdrFull = EditorGUILayout.GetControlRect(false, 28f);
                var hdrRect = new Rect(hdrFull.x + CardPad, hdrFull.y, cw, 28f);
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(hdrRect, ApiSectionHdr);
                    EditorGUI.DrawRect(new Rect(hdrRect.x, hdrRect.y, 3f, hdrRect.height), Accent);
                    GUI.Label(new Rect(hdrRect.x + 10f, hdrRect.y, hdrRect.width - 10f, hdrRect.height),
                        sectionTitle, _styleSectionHdr);
                }

                // Column header bar
                var colFull = EditorGUILayout.GetControlRect(false, 22f);
                var colRect = new Rect(colFull.x + CardPad, colFull.y, cw, 22f);
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(colRect, MemberTableHdrBg);
                    // Bottom border to separate header from rows
                    EditorGUI.DrawRect(new Rect(colRect.x, colRect.yMax - 1f, colRect.width, 1f), Sep);
                    float sigW = colRect.width * 0.42f;
                    var colHdrStyle = new GUIStyle(_styleMemberKind)
                    {
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = TextDim },
                        fontSize = 10
                    };
                    GUI.Label(new Rect(colRect.x + 28f, colRect.y, sigW - 28f, colRect.height),
                        "NAME", colHdrStyle);
                    GUI.Label(new Rect(colRect.x + sigW + 8f, colRect.y, colRect.width - sigW - 8f, colRect.height),
                        "DESCRIPTION", colHdrStyle);
                }

                // Rows
                foreach (var member in group)
                    DrawMemberRow(entry, member, cw);

                GUILayout.Space(4f);
            }
        }

        private void DrawMemberRow(Entry entry, DocMember member, float cw)
        {
            string key = entry.Title + "::" + member.Signature;
            if (!_memberFoldouts.TryGetValue(key, out bool expanded))
            {
                _memberFoldouts[key] = false;
                expanded = false;
            }

            bool hasCode = !string.IsNullOrEmpty(member.Code) && !IsCommentOnlyCode(member.Code);

            // Row height = tallest of the two columns, cached per (member, width) so CalcHeight
            // only fires once per unique combination instead of every frame.
            float sigW     = cw * 0.42f;
            float summaryW = cw - sigW - 28f;
            string heightKey = key + ":" + (int)cw;
            if (!_rowHeightCache.TryGetValue(heightKey, out float rowH))
            {
                float sigH     = _styleMemberSig.CalcHeight(
                    new GUIContent(member.Signature ?? ""), Mathf.Max(sigW - 24f, 40f));
                float summaryH = _styleMemberSummary.CalcHeight(
                    new GUIContent(member.Summary ?? ""), Mathf.Max(summaryW, 40f));
                rowH = Mathf.Max(32f, Mathf.Max(sigH, summaryH) + 14f);
                _rowHeightCache[heightKey] = rowH;
            }

            var rowFull = EditorGUILayout.GetControlRect(false, rowH);
            var rowRect = new Rect(rowFull.x + CardPad, rowFull.y, cw, rowH);
            bool hovered = rowRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                Color rowBg = expanded
                    ? new Color(Accent.r, Accent.g, Accent.b, 0.10f)
                    : (hovered && hasCode ? RowHover : MemberRowBg);
                EditorGUI.DrawRect(rowRect, rowBg);
                EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.yMax - 1f, rowRect.width, 1f), Sep);

                // Expand arrow (only if has code)
                if (hasCode)
                {
                    string arrow = expanded ? "▾" : "▸";
                    GUI.Label(new Rect(rowRect.x + 4f, rowRect.y + 7f, 14f, 18f), arrow,
                        new GUIStyle(_styleArrow) { fontSize = 10 });
                }

                // Signature — top-aligned, height from cache
                float sigContentH = rowH - 14f;
                GUI.Label(new Rect(rowRect.x + 20f, rowRect.y + 7f, sigW - 24f, sigContentH),
                    member.Signature, _styleMemberSig);

                // Summary — word-wrapped, top-aligned
                float summaryContentH = rowH - 14f;
                GUI.Label(new Rect(rowRect.x + sigW + 8f, rowRect.y + 7f, summaryW, summaryContentH),
                    member.Summary, _styleMemberSummary);
            }

            if (hasCode && Event.current.type == EventType.MouseDown
                && rowRect.Contains(Event.current.mousePosition))
            {
                _memberFoldouts[key] = !expanded;
                Event.current.Use();
                Repaint();
            }

            if (expanded && hasCode)
                DrawMemberCodeBlock(member, key, cw);
        }

        private void DrawMemberCodeBlock(DocMember member, string key, float cw)
        {
            if (!_memberCodeCache.TryGetValue(key, out string highlighted))
            {
                highlighted = SyntaxHighlight(member.Code);
                _memberCodeCache[key] = highlighted;
            }

            bool hasCopyFeedback = _memberCopyUntil.TryGetValue(key, out double until)
                && EditorApplication.timeSinceStartup < until;

            float indentX = CardPad;
            float blockW = cw;

            // Header bar — teal accent to clearly signal "code block"
            var hdrFull = EditorGUILayout.GetControlRect(false, 24f);
            var hdrRect = new Rect(hdrFull.x + indentX, hdrFull.y, blockW, 24f);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(hdrRect, CodeBlockHdr);
                // Left teal accent bar
                EditorGUI.DrawRect(new Rect(hdrRect.x, hdrRect.y, 3f, hdrRect.height), CodeBlockAccent);
                // "C#" label in teal
                GUI.Label(new Rect(hdrRect.x + 10f, hdrRect.y, 40f, hdrRect.height),
                    "C#", new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = CodeBlockAccent },
                        alignment = TextAnchor.MiddleLeft
                    });
                // Copy button
                bool copyHovered = hdrRect.Contains(Event.current.mousePosition);
                GUI.Label(hdrRect, hasCopyFeedback ? "✓ Copied!" : (copyHovered ? "Copy ▸" : "Copy"),
                    new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = hasCopyFeedback
                            ? new Color(0.30f, 0.78f, 0.45f, 1f)
                            : (copyHovered ? TextMain : TextDim) },
                        alignment = TextAnchor.MiddleRight,
                        padding = new RectOffset(0, 10, 0, 0)
                    });
            }

            if (Event.current.type == EventType.MouseDown && hdrRect.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.systemCopyBuffer = member.Code;
                _memberCopyUntil[key] = EditorApplication.timeSinceStartup + 1.8;
                Event.current.Use();
                Repaint();
            }

            // Code body — terminal-dark background
            float codeH = _styleCode.CalcHeight(new GUIContent(member.Code), blockW - 28f) + 22f;
            var codeFull = EditorGUILayout.GetControlRect(false, codeH);
            var codeRect = new Rect(codeFull.x + indentX, codeFull.y, blockW, codeH);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(codeRect, CodeBlockBody);
                // Left accent bar continues through body
                EditorGUI.DrawRect(new Rect(codeRect.x, codeRect.y, 3f, codeRect.height), CodeBlockAccent);
                GUI.Label(new Rect(codeRect.x + 12f, codeRect.y + 8f, codeRect.width - 24f, codeH - 16f),
                    highlighted, _styleCode);
                // Bottom border
                EditorGUI.DrawRect(new Rect(codeRect.x, codeRect.yMax - 1f, codeRect.width, 1f),
                    new Color(CodeBlockAccent.r, CodeBlockAccent.g, CodeBlockAccent.b, 0.30f));
            }
        }

        private string SyntaxHighlight(string src)
        {
            if (string.IsNullOrEmpty(src)) return src;

            // Normalize tabs to 4 spaces for consistent indentation display
            src = src.Replace("\t", "    ");

            string cKw   = Skin ? "#569CD6" : "#0000CC"; // blue   — keywords
            string cType = Skin ? "#4EC9B0" : "#267F99"; // teal   — types / PascalCase
            string cStr  = Skin ? "#CE9178" : "#A31515"; // orange — strings
            string cCmt  = Skin ? "#6A9955" : "#008000"; // green  — comments
            string cNum  = Skin ? "#B5CEA8" : "#098658"; // pale   — numbers
            string cPun  = Skin ? "#808080" : "#666666"; // gray   — punctuation
            string cPre  = Skin ? "#C586C0" : "#AF00DB"; // purple — preprocessor

            var keywords = new HashSet<string>
            {
                "abstract","as","base","bool","break","byte","case","catch","char","checked",
                "class","const","continue","decimal","default","delegate","do","double","else",
                "enum","event","explicit","extern","false","finally","fixed","float","for",
                "foreach","goto","if","implicit","in","int","interface","internal","is","lock",
                "long","namespace","new","null","object","operator","out","override","params",
                "private","protected","public","readonly","ref","return","sbyte","sealed",
                "short","sizeof","stackalloc","static","string","struct","switch","this","throw",
                "true","try","typeof","uint","ulong","unchecked","unsafe","ushort","using",
                "virtual","void","volatile","while","async","await","var","yield","get","set",
                "add","remove","value","partial","where","select","from","let","join","into",
                "orderby","ascending","descending","group","by","on","equals","when","init",
                "record","with","not","and","or","required","file","scoped","managed","unmanaged"
            };

            var sb  = new System.Text.StringBuilder(src.Length * 2);
            int len = src.Length;
            int i   = 0;

            while (i < len)
            {
                // ── Block comment /* ... */ ──────────────────────────────────
                if (i + 1 < len && src[i] == '/' && src[i + 1] == '*')
                {
                    int end = src.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    end = end < 0 ? len : end + 2;
                    sb.Append($"<color={cCmt}>");
                    AppendSafeRange(sb, src, i, end);
                    sb.Append("</color>");
                    i = end;
                    continue;
                }

                // ── Line comment // ──────────────────────────────────────────
                if (i + 1 < len && src[i] == '/' && src[i + 1] == '/')
                {
                    int end = src.IndexOf('\n', i);
                    if (end < 0) end = len;
                    sb.Append($"<color={cCmt}>");
                    AppendSafeRange(sb, src, i, end);
                    sb.Append("</color>");
                    i = end;
                    continue;
                }

                // ── Preprocessor directive  #... ─────────────────────────────
                if (src[i] == '#')
                {
                    int end = src.IndexOf('\n', i);
                    if (end < 0) end = len;
                    sb.Append($"<color={cPre}>");
                    AppendSafeRange(sb, src, i, end);
                    sb.Append("</color>");
                    i = end;
                    continue;
                }

                // ── Interpolated verbatim  $@"..."  or  @$"..." ──────────────
                if (i + 2 < len &&
                    ((src[i] == '$' && src[i + 1] == '@' && src[i + 2] == '"') ||
                     (src[i] == '@' && src[i + 1] == '$' && src[i + 2] == '"')))
                {
                    sb.Append($"<color={cStr}>{SafeText(src[i].ToString())}{SafeText(src[i+1].ToString())}\"");
                    i += 3;
                    while (i < len)
                    {
                        if (src[i] == '"' && i + 1 < len && src[i + 1] == '"')
                        { sb.Append("\"\""); i += 2; }
                        else if (src[i] == '"')
                        { sb.Append('"'); i++; break; }
                        else
                        { AppendSafeRange(sb, src, i, i + 1); i++; }
                    }
                    sb.Append("</color>");
                    continue;
                }

                // ── Interpolated string  $"..." ──────────────────────────────
                if (src[i] == '$' && i + 1 < len && src[i + 1] == '"')
                {
                    sb.Append($"<color={cStr}>$\"");
                    i += 2;
                    while (i < len)
                    {
                        if (src[i] == '\\' && i + 1 < len)
                        { AppendSafeRange(sb, src, i, i + 2); i += 2; }
                        else if (src[i] == '"')
                        { sb.Append('"'); i++; break; }
                        else
                        { AppendSafeRange(sb, src, i, i + 1); i++; }
                    }
                    sb.Append("</color>");
                    continue;
                }

                // ── Verbatim string  @"..." ──────────────────────────────────
                if (src[i] == '@' && i + 1 < len && src[i + 1] == '"')
                {
                    sb.Append($"<color={cStr}>@\"");
                    i += 2;
                    while (i < len)
                    {
                        if (src[i] == '"' && i + 1 < len && src[i + 1] == '"')
                        { sb.Append("\"\""); i += 2; }
                        else if (src[i] == '"')
                        { sb.Append('"'); i++; break; }
                        else
                        { AppendSafeRange(sb, src, i, i + 1); i++; }
                    }
                    sb.Append("</color>");
                    continue;
                }

                // ── Regular string  "..." ────────────────────────────────────
                if (src[i] == '"')
                {
                    sb.Append($"<color={cStr}>\"");
                    i++;
                    while (i < len)
                    {
                        if (src[i] == '\\' && i + 1 < len)
                        { AppendSafeRange(sb, src, i, i + 2); i += 2; }
                        else if (src[i] == '"')
                        { sb.Append('"'); i++; break; }
                        else
                        { AppendSafeRange(sb, src, i, i + 1); i++; }
                    }
                    sb.Append("</color>");
                    continue;
                }

                // ── Char literal  '.' ────────────────────────────────────────
                if (src[i] == '\'')
                {
                    // scan past optional escape then closing quote
                    int j = i + 1;
                    if (j < len && src[j] == '\\') j++; // skip escape char
                    if (j < len) j++;                    // skip the char itself
                    int end = (j < len && src[j] == '\'') ? j + 1 : i + 1;
                    sb.Append($"<color={cStr}>");
                    AppendSafeRange(sb, src, i, end);
                    sb.Append("</color>");
                    i = end;
                    continue;
                }

                // ── Number literal ───────────────────────────────────────────
                if (char.IsDigit(src[i]) || (src[i] == '.' && i + 1 < len && char.IsDigit(src[i + 1])))
                {
                    int start = i;
                    bool isHex = false;
                    // hex prefix 0x / 0X
                    if (src[i] == '0' && i + 1 < len && (src[i + 1] == 'x' || src[i + 1] == 'X'))
                    { i += 2; isHex = true; }

                    while (i < len)
                    {
                        char c = src[i];
                        if (char.IsDigit(c) || c == '_') { i++; continue; }
                        if (isHex && ((c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) { i++; continue; }
                        // decimal point only when next char is also a digit (avoid "5.ToString()")
                        if (!isHex && c == '.' && i + 1 < len && char.IsDigit(src[i + 1])) { i++; continue; }
                        // exponent
                        if (!isHex && (c == 'e' || c == 'E') && i > start)
                        {
                            i++;
                            if (i < len && (src[i] == '+' || src[i] == '-')) i++;
                            continue;
                        }
                        // type suffixes: f F d D m M l L u U — only at end (next char not letter/digit)
                        if ((c == 'f' || c == 'F' || c == 'd' || c == 'D' ||
                             c == 'm' || c == 'M' || c == 'l' || c == 'L' ||
                             c == 'u' || c == 'U') &&
                            (i + 1 >= len || (!char.IsLetterOrDigit(src[i + 1]) && src[i + 1] != '_')))
                        { i++; break; }
                        // UL / LU combos
                        if ((c == 'u' || c == 'U') && i + 1 < len && (src[i + 1] == 'l' || src[i + 1] == 'L'))
                        { i += 2; break; }
                        if ((c == 'l' || c == 'L') && i + 1 < len && (src[i + 1] == 'u' || src[i + 1] == 'U'))
                        { i += 2; break; }
                        break;
                    }
                    sb.Append($"<color={cNum}>");
                    AppendSafeRange(sb, src, start, i);
                    sb.Append("</color>");
                    continue;
                }

                // ── Identifier / keyword / type ──────────────────────────────
                if (char.IsLetter(src[i]) || src[i] == '_')
                {
                    int start = i;
                    while (i < len && (char.IsLetterOrDigit(src[i]) || src[i] == '_'))
                        i++;
                    string word = src.Substring(start, i - start);
                    if (keywords.Contains(word))
                        sb.Append($"<color={cKw}>{SafeText(word)}</color>");
                    else if (word.Length > 1 && char.IsUpper(word[0]))
                        sb.Append($"<color={cType}>{SafeText(word)}</color>");
                    else
                        sb.Append(SafeText(word));
                    continue;
                }

                // ── Punctuation ──────────────────────────────────────────────
                if ("{}[]();,.<>!?:=+-*/%&|^~".IndexOf(src[i]) >= 0)
                {
                    sb.Append($"<color={cPun}>{SafeText(src[i].ToString())}</color>");
                    i++;
                    continue;
                }

                AppendSafeRange(sb, src, i, i + 1);
                i++;
            }

            return sb.ToString();
        }

        private static void AppendSafeRange(System.Text.StringBuilder sb, string src, int start, int end)
        {
            end = Mathf.Min(end, src.Length);
            for (int j = start; j < end; j++)
                sb.Append(SafeChar(src[j]));
        }

        private static string SafeText(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s) sb.Append(SafeChar(c));
            return sb.ToString();
        }

        private static string SafeChar(char c) => c switch
        {
            '<' => "\u003C",
            '>' => "\u003E",
            '&' => "&amp;",
            _ => c.ToString()
        };

        private void DrawHSep()
        {
            var r = EditorGUILayout.GetControlRect(false, 1f);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(r, Sep);
        }

        private void DrawVerticalSep()
        {
            var r = GUILayoutUtility.GetRect(1f, position.height,
                GUILayout.Width(1f), GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(r, Sep);
        }

        private List<Entry> GetFiltered()
        {
            string q = _search.Trim().ToLowerInvariant();
            return _db.FindAll(e =>
            {
                bool catOk = _selCat == -1 || (int)e.Category == _selCat;
                if (!catOk) return false;
                if (string.IsNullOrEmpty(q)) return true;
                return e.Title.ToLowerInvariant().Contains(q)
                    || e.Namespace.ToLowerInvariant().Contains(q)
                    || e.Summary.ToLowerInvariant().Contains(q)
                    || (e.Description != null && e.Description.ToLowerInvariant().Contains(q))
                    || (e.Tags != null && Array.Exists(e.Tags, t => t.ToLowerInvariant().Contains(q)));
            });
        }

        private int GetCount(int catIndex)
        {
            string q = _search.Trim().ToLowerInvariant();
            return _db.FindAll(e =>
            {
                bool catOk = catIndex == -1 || (int)e.Category == catIndex;
                if (!catOk) return false;
                if (string.IsNullOrEmpty(q)) return true;
                return e.Title.ToLowerInvariant().Contains(q)
                    || e.Namespace.ToLowerInvariant().Contains(q)
                    || e.Summary.ToLowerInvariant().Contains(q);
            }).Count;
        }

        private static string GetCatIcon(DocCategory c) => c switch
        {
            DocCategory.Core => "⚙",
            DocCategory.Components => "◈",
            DocCategory.Extensions => "⊕",
            DocCategory.Utilities => "⊞",
            DocCategory.EditorTools => "◧",
            DocCategory.TRnKSignal => "◯",
            DocCategory.TRnKFlow => "↻",
            DocCategory.TRnKTimer => "⏱",
            _ => "•"
        };

        // Truncate text so it fits within maxLines lines at the given pixel width.
        private string TruncateToFitLines(string text, GUIStyle style, float width, int maxLines, string cacheKey)
        {
            if (_summaryCache.TryGetValue(cacheKey, out string cached))
                return cached;

            float singleLineH = style.CalcHeight(new GUIContent("Wg"), width);
            float maxH = singleLineH * maxLines + 2f;

            string result;
            if (style.CalcHeight(new GUIContent(text), width) <= maxH)
            {
                result = text;
            }
            else
            {
                int lo = 0, hi = text.Length;
                while (lo < hi - 1)
                {
                    int mid = (lo + hi) / 2;
                    if (style.CalcHeight(new GUIContent(text.Substring(0, mid) + "..."), width) <= maxH)
                        lo = mid;
                    else
                        hi = mid;
                }
                result = lo > 0 ? text.Substring(0, lo) + "..." : "...";
            }
            _summaryCache[cacheKey] = result;
            return result;
        }

        private static bool IsCommentOnlyCode(string code)
        {
            foreach (var raw in code.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length > 0 && !line.StartsWith("//") && !line.StartsWith("/*") && !line.StartsWith("*"))
                    return false;
            }
            return true;
        }

        private void BuildStyles()
        {
            _stylesReady = true;

            _styleCatName = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = TextMain }
            };

            _styleBadge = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = TextDim }
            };

            _styleEntryTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = TextMain },
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Clip
            };

            _styleEntryNs = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = AccentText },
                alignment = TextAnchor.UpperLeft
            };

            _styleEntrySummary = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = TextDim },
                clipping = TextClipping.Clip
            };

            _styleGroupHdr = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = TextDim },
                alignment = TextAnchor.MiddleLeft
            };

            _styleHeaderTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = TextMain },
                alignment = TextAnchor.MiddleLeft
            };

            _styleArrow = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = TextDim }
            };

            _styleTag = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(7, 7, 2, 2),
                normal = { textColor = AccentText }
            };

            _styleSummaryBox = new GUIStyle(EditorStyles.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = Skin ? new Color(0.80f, 0.90f, 1.00f) : new Color(0.10f, 0.26f, 0.56f) }
            };

            _styleDesc = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = TextDim }
            };

            _styleCode = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true,
                normal   = { textColor = TextCode }
            };

            _styleSearch = new GUIStyle(EditorStyles.toolbarSearchField)
            {
                fixedHeight = 0
            };

            _styleSectionHdr = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = TextMain },
                alignment = TextAnchor.MiddleLeft
            };

            _styleMemberKind = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = TextDim }
            };

            _styleMemberSig = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = Skin ? new Color(0.70f, 0.88f, 1.00f) : new Color(0.08f, 0.28f, 0.56f) },
                clipping = TextClipping.Clip
            };

            _styleMemberSummary = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = TextDim },
                clipping = TextClipping.Clip
            };
        }
    }
}
#endif
