#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TRnK.Toolkit
{
    public static class PaginationEditor
    {
        // Visual constants tuned for compact inspector toolbars
        private static readonly Color BorderColor = new(0.5f, 0.5f, 0.5f, 0.3f);

        /// <summary>
        /// Mutable pagination state (page index and input text). You can own this state or let the utility own it by ID.
        /// </summary>
        [Serializable]
        public struct State
        {
            public int PageIndex;
            public string PageText;
        }

        /// <summary>
        /// Result of drawing the pagination bar and computing the visible slice.
        /// </summary>
        public readonly struct Slice
        {
            public readonly int Start;
            public readonly int End;
            public readonly int PageIndex;
            public readonly int TotalPages;
            public readonly int TotalItems;

            public Slice(int start, int end, int pageIndex, int totalPages, int totalItems)
            {
                Start = start;
                End = end;
                PageIndex = pageIndex;
                TotalPages = totalPages;
                TotalItems = totalItems;
            }
        }

        // Internal ID-based state storage for convenience
        private static readonly Dictionary<int, State> _states = new();

        /// <summary>
        /// Draw a pagination bar using a persistent ID-based state (e.g., target.GetInstanceID()).
        /// </summary>
        public static Slice Draw(int id, int totalItems, int itemsPerPage = 10, float? lineHeightOverride = null, string itemLabelSingular = "Item", string itemLabelPlural = "Items")
        {
            if (!_states.TryGetValue(id, out var s)) s = new State { PageIndex = 0, PageText = "1" };
            var slice = Draw(ref s, totalItems, itemsPerPage, lineHeightOverride, itemLabelSingular, itemLabelPlural);
            _states[id] = s;
            return slice;
        }

        /// <summary>
        /// Draw a pagination bar using a caller-owned state. Returns the computed slice.
        /// </summary>
        public static Slice Draw(ref State state, int totalItems, int itemsPerPage = 10, float? lineHeightOverride = null, string itemLabelSingular = "Item", string itemLabelPlural = "Items")
        {
            if (itemsPerPage <= 0) itemsPerPage = 10;
            int totalPages = Mathf.CeilToInt(totalItems / (float)itemsPerPage);
            state.PageIndex = Mathf.Clamp(state.PageIndex, 0, Mathf.Max(0, totalPages - 1));

            // UI Layout
            float lineH = lineHeightOverride ?? EditorGUIUtility.singleLineHeight;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var miniLabel = EditorStyles.miniLabel;

                // Page text field style (centered, compact)
                var pageFieldStyle = new GUIStyle(EditorStyles.miniTextField)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = miniLabel.fontSize,
                    fixedHeight = lineH,
                    padding = new RectOffset(2, 2, 2, 2)
                };

                string itemWord = totalItems == 1 ? itemLabelSingular : itemLabelPlural;
                EditorGUILayout.LabelField($"{totalItems} {itemWord}", miniLabel, GUILayout.Width(80), GUILayout.Height(lineH));
                GUILayout.FlexibleSpace();

                // Prev
                using (new EditorGUI.DisabledScope(state.PageIndex <= 0))
                {
                    if (GUILayout.Button("\u25C0", EditorStyles.miniButton, GUILayout.Width(22), GUILayout.Height(lineH))) // ◀
                    {
                        state.PageIndex--;
                        state.PageText = (state.PageIndex + 1).ToString();
                    }
                }

                // Ensure text present
                if (string.IsNullOrEmpty(state.PageText)) state.PageText = (state.PageIndex + 1).ToString();

                GUILayout.Space(2);

                if (totalPages <= 0)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        var zeroRect = GUILayoutUtility.GetRect(22, lineH, GUILayout.Width(22), GUILayout.Height(lineH));
                        EditorGUI.TextField(zeroRect, "0", pageFieldStyle);
                    }
                    EditorGUILayout.LabelField("/ 0", miniLabel, GUILayout.Width(24), GUILayout.Height(lineH));
                }
                else
                {
                    // [ n ] / total
                    string oldText = state.PageText;
                    var fieldRect = GUILayoutUtility.GetRect(22, lineH, GUILayout.Width(22), GUILayout.Height(lineH));
                    string newText = EditorGUI.TextField(fieldRect, oldText, pageFieldStyle);
                    if (!ReferenceEquals(oldText, newText))
                    {
                        if (int.TryParse(newText, out int pageNum))
                        {
                            if (pageNum >= 1 && pageNum <= totalPages)
                            {
                                state.PageIndex = pageNum - 1;
                                state.PageText = pageNum.ToString();
                            }
                            else
                            {
                                state.PageText = (state.PageIndex + 1).ToString();
                            }
                        }
                        else
                        {
                            state.PageText = (state.PageIndex + 1).ToString();
                        }
                    }
                    EditorGUILayout.LabelField($"/ {totalPages}", miniLabel, GUILayout.Width(24), GUILayout.Height(lineH));
                }

                GUILayout.Space(2);

                // Next
                using (new EditorGUI.DisabledScope(state.PageIndex >= totalPages - 1))
                {
                    if (GUILayout.Button("\u25B6", EditorStyles.miniButton, GUILayout.Width(22), GUILayout.Height(lineH))) // ▶
                    {
                        state.PageIndex++;
                        state.PageText = (state.PageIndex + 1).ToString();
                    }
                }
            }

            // Dedicated 1px separator line reserved after the bar so it won't be painted over by following controls
            var sepRect = EditorGUILayout.GetControlRect(false, 1);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(sepRect.x, sepRect.y, sepRect.width, 1), BorderColor);

            int start = state.PageIndex * itemsPerPage;
            int end = Mathf.Min(start + itemsPerPage, totalItems);
            return new Slice(start, end, state.PageIndex, totalPages, totalItems);
        }

        // Intentionally no extra helpers; separator above handles bottom edge consistently across skins
    }
}
#endif