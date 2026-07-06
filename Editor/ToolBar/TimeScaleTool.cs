#if UNITY_EDITOR
using UnityEditor;
#if UNITY_6000_3_OR_NEWER
using UnityEditor.Toolbars;
#endif
#if UNITY_2020_1_OR_NEWER
using UnityEditor.UIElements; // ToolbarButton
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Toolkit
{
    // Original layout preserved. Only logic changed to sync with Project Settings Time Scale (TimeManager.asset m_TimeScale).
    [InitializeOnLoad]
    internal static class TimeScaleTool
    {
        private const string PrefStoredValue = "TRnK.Toolkit:TimeScaleValue";      // kept for backward compatibility (not authoritative anymore)
        private const float DefaultTimeScale = 1f;
        private const float MinTimeScale = 0f;
        private static float MaxTimeScale => Mathf.Max(10f, (float)TRnKSettings.GetOrCreate().timeScaleMax);

        private static bool installed;
#if !UNITY_6000_3_OR_NEWER
        private static VisualElement rootContainer;
        private static Slider timeSlider;
        private static Button resetButton;
        private static Image resetIcon;
        private static Label valueLabel;
        private static Label titleLabel;
#endif
        private static float lastAppliedTimeScale = 1f;

        // TimeManager sync
        private static SerializedObject timeManagerSO;
        private static SerializedProperty timeScaleProp;
        private static double lastPollTime; // throttle external polling slightly

        static TimeScaleTool()
        {
#if !UNITY_6000_3_OR_NEWER
            EditorApplication.delayCall += EnsureInstall;
#endif
            EditorApplication.update += UpdateExternalSync;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

#if UNITY_6000_3_OR_NEWER
        private class Unity6000TimeScaleElement : VisualElement
        {
            private readonly Slider slider;
            private readonly Label valueLabel;

            public Unity6000TimeScaleElement()
            {
                name = "TRnK.ToolkitTimeScaleMainToolbar";
                style.flexDirection = FlexDirection.Row;
                style.alignItems = Align.Center;
                style.paddingLeft = 4;
                style.paddingRight = 4;

                var titleLabel = new Label("Time Scale") { name = "TRnK.ToolkitTimeScaleTitle" };
                titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                titleLabel.style.fontSize = 12;
                titleLabel.style.marginLeft = 1;
                titleLabel.style.marginRight = 2;
                titleLabel.style.minWidth = 72;

                slider = new Slider(GetMin(), GetMax())
                {
                    name = "TRnK.ToolkitTimeScaleSlider",
                    showInputField = false
                };
                slider.style.minWidth = 88;
                slider.style.maxWidth = 88;
                slider.style.marginLeft = 1;
                slider.style.marginRight = 1;

                valueLabel = new Label("1.00") { name = "TRnK.ToolkitTimeScaleValue" };
                valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                valueLabel.style.minWidth = 40;
                valueLabel.style.maxWidth = 40;
                valueLabel.style.fontSize = 12;
                valueLabel.style.marginLeft = 0;
                valueLabel.style.marginRight = 0;

                var refreshTex = ToolbarUtils.GetBestIcon(
                    "d_Refresh",
                    "Refresh",
                    "TreeEditor.Refresh",
                    "d_RotateTool",
                    "RotateTool"
                );

                var resetButton = new Button(() =>
                {
                    ResetToDefault();
                    float v = GetCurrentTimeScale();
                    slider.SetValueWithoutNotify(v);
                    valueLabel.text = v.ToString("0.00");
                })
                {
                    name = "TRnK.ToolkitTimeScaleReset",
                    tooltip = "Reset Time Scale (1.0)"
                };
                resetButton.focusable = false;
                resetButton.style.paddingLeft = 0;
                resetButton.style.paddingRight = 0;
                resetButton.style.paddingTop = 0;
                resetButton.style.paddingBottom = 0;
                resetButton.style.marginLeft = 0;
                resetButton.style.marginRight = 1;
                resetButton.style.backgroundColor = StyleKeyword.Null;
                resetButton.style.justifyContent = Justify.Center;
                resetButton.style.alignItems = Align.Center;

                var resetIcon = new Image { image = refreshTex, scaleMode = ScaleMode.ScaleToFit };
                resetIcon.style.width = 14;
                resetIcon.style.height = 14;
                resetButton.Add(resetIcon);

                Add(titleLabel);
                Add(slider);
                Add(valueLabel);
                Add(resetButton);

                slider.RegisterValueChangedCallback(e =>
                {
                    SetTimeScale(e.newValue);
                    float v = GetCurrentTimeScale();
                    valueLabel.text = v.ToString("0.00");
                });

                RegisterCallback<AttachToPanelEvent>(_ => ApplyVisibilityAndSync());
                schedule.Execute(ApplyVisibilityAndSync).Every(250);
            }

            private void ApplyVisibilityAndSync()
            {
                bool hidden = false;
                try { hidden = TRnKSettings.GetOrCreate().hideToolbar; } catch { }
                style.display = hidden ? DisplayStyle.None : DisplayStyle.Flex;
                SetEnabled(!hidden);

                float v = GetCurrentTimeScale();
                slider.lowValue = GetMin();
                slider.highValue = GetMax();
                slider.SetValueWithoutNotify(v);
                valueLabel.text = v.ToString("0.00");
            }
        }

        [MainToolbarElement("TRnK.Toolkit/Time Scale", defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarElement CreateMainToolbarElement()
        {
            return new MainToolbarCustom(() => new Unity6000TimeScaleElement());
        }
#endif

        internal static float GetCurrentTimeScale()
        {
            if (timeManagerSO == null || timeScaleProp == null) LoadTimeManager();
            if (timeManagerSO != null && timeScaleProp != null)
            {
                timeManagerSO.Update();
                lastAppliedTimeScale = Mathf.Clamp(timeScaleProp.floatValue, MinTimeScale, MaxTimeScale);
            }
            return lastAppliedTimeScale;
        }

        internal static float GetMin() => MinTimeScale;
        internal static float GetMax() => MaxTimeScale;

        internal static void SetTimeScale(float v)
        {
            v = Mathf.Clamp(v, MinTimeScale, MaxTimeScale);
            if (Mathf.Approximately(v, lastAppliedTimeScale)) return;
            lastAppliedTimeScale = v;
            PushValueToTimeManager();
            ApplyTimeScaleRuntimeOnly();
        }

        internal static void ResetToDefault()
        {
            lastAppliedTimeScale = DefaultTimeScale;
            PushValueToTimeManager();
            ApplyTimeScaleRuntimeOnly();
        }

        // Enabled state is determined by the global HideToolbar preference.

        private static void EnsureInstall()
        {
#if UNITY_6000_3_OR_NEWER
            // Unity 6.3+ uses MainToolbarElement integration.
            return;
#else
            try { if (TRnKSettings.GetOrCreate().hideToolbar) return; } catch { }
            if (installed) return;
            var toolbarRoot = ToolbarUtils.GetToolbarRoot();
            if (toolbarRoot == null)
            {
                EditorApplication.delayCall += EnsureInstall; // try again next tick
                return;
            }

            // Defensive: domain reload / toolbar rebuild can leave old injected elements behind.
            ToolbarUtils.RemoveAllByName(toolbarRoot, "TRnK.ToolkitTimeScaleContainer");

            LoadTimeManager();
            // Authoritative value from TimeManager first
            if (timeScaleProp != null) lastAppliedTimeScale = Mathf.Clamp(timeScaleProp.floatValue, MinTimeScale, MaxTimeScale);
            else lastAppliedTimeScale = Mathf.Clamp(EditorPrefs.GetFloat(PrefStoredValue, DefaultTimeScale), MinTimeScale, MaxTimeScale);
            BuildUI(toolbarRoot);
            ApplyTimeScaleRuntimeOnly(); // ensure runtime matches if already in play (domain reload)
            installed = true;
#endif
        }

        private static void LoadTimeManager()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset");
            if (assets != null && assets.Length > 0)
            {
                timeManagerSO = new SerializedObject(assets[0]);
                timeScaleProp = timeManagerSO.FindProperty("m_TimeScale");
            }
        }

        private static void BuildUI(VisualElement toolbarRoot)
        {
#if UNITY_6000_3_OR_NEWER
            return;
#else
            rootContainer = new VisualElement { name = "TRnK.ToolkitTimeScaleContainer" };
            rootContainer.style.position = Position.Absolute;
            rootContainer.style.flexDirection = FlexDirection.Row;
            rootContainer.style.alignItems = Align.Center;
            rootContainer.style.paddingLeft = 4;
            rootContainer.style.paddingRight = 4;
            rootContainer.style.height = 20;
            rootContainer.style.backgroundColor = new Color(0f, 0f, 0f, 0.08f);
            ToolbarUtils.ApplyRoundedStyling(rootContainer);

            timeSlider = new Slider(MinTimeScale, MaxTimeScale)
            {
                value = lastAppliedTimeScale,
                name = "TRnK.ToolkitTimeScaleSlider",
                showInputField = false
            };
            timeSlider.style.minWidth = 88;
            timeSlider.style.maxWidth = 88;
            timeSlider.style.marginLeft = 1;
            timeSlider.style.marginRight = 1; // slightly reduce gap to value label
            timeSlider.lowValue = MinTimeScale;
            timeSlider.highValue = MaxTimeScale;
            timeSlider.RegisterValueChangedCallback(e => OnSliderChanged(e.newValue));

            // FIX: align slider vertically with others
            timeSlider.style.height = Length.Percent(100);
            timeSlider.style.alignSelf = Align.Center;
            timeSlider.style.marginTop = 0;
            timeSlider.style.marginBottom = 0;

#if UNITY_2020_1_OR_NEWER
            timeSlider.focusable = false;
            timeSlider.RegisterCallback<FocusInEvent>(_ => timeSlider.Blur());
            // Try also disabling focus on internal dragger element if present
            var dragger = timeSlider.Q<VisualElement>(className: "unity-dragger");
            if (dragger != null)
            {
                dragger.focusable = false;
                dragger.RegisterCallback<FocusInEvent>(_ => timeSlider.Blur());
            }
#endif

            valueLabel = new Label(FormatValue(lastAppliedTimeScale)) { name = "TRnK.ToolkitTimeScaleValue" };
            valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            valueLabel.style.minWidth = 40; // tighten width slightly
            valueLabel.style.maxWidth = 40;
            valueLabel.style.fontSize = 12;
            valueLabel.style.marginLeft = 0; // bring closer to slider
            valueLabel.style.marginRight = 0;

            // FIX: align label vertically with others
            valueLabel.style.height = Length.Percent(100);
            valueLabel.style.alignSelf = Align.Center;
            valueLabel.style.marginTop = 0;
            valueLabel.style.marginBottom = 0;

#if UNITY_2020_1_OR_NEWER
            resetButton = new ToolbarButton(ResetTimeScale)
            {
                text = string.Empty,
                tooltip = "Reset Time Scale (1.0)",
                name = "TRnK.ToolkitTimeScaleReset"
            };
            resetButton.AddToClassList("unity-toolbar-button");
#else
            resetButton = new Button(ResetTimeScale)
            {
                text = string.Empty,
                tooltip = "Reset Time Scale (1.0)",
                name = "TRnK.ToolkitTimeScaleReset"
            };
#endif
            resetButton.style.width = 26;
            resetButton.style.height = 22;
            resetButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            resetButton.style.paddingLeft = 0;
            resetButton.style.paddingRight = 0;
            resetButton.style.paddingTop = 0;
            resetButton.style.paddingBottom = 0;
            resetButton.style.marginLeft = 0;
            resetButton.style.marginRight = 1;
            resetButton.style.fontSize = 11;
            resetButton.style.backgroundColor = StyleKeyword.Null;
            resetButton.focusable = false;
            resetButton.style.justifyContent = Justify.Center;
            resetButton.style.alignItems = Align.Center;
            // icon (pick clearest built-in)
            var refreshTex = ToolbarUtils.GetBestIcon(
                "d_Refresh",
                "Refresh",
                "TreeEditor.Refresh",
                "d_RotateTool",
                "RotateTool"
            );
            resetIcon = new Image { image = refreshTex, scaleMode = ScaleMode.ScaleToFit };
            resetIcon.style.alignSelf = Align.Center;
            resetIcon.style.marginLeft = 0; resetIcon.style.marginRight = 0; resetIcon.style.marginTop = 0; resetIcon.style.marginBottom = 0;
            resetButton.Add(resetIcon);

            // FIX: align button vertically with others
            resetButton.style.height = Length.Percent(100);
            resetButton.style.alignSelf = Align.Center;
            resetButton.style.marginTop = 0;
            resetButton.style.marginBottom = 0;

#if UNITY_2020_1_OR_NEWER
            resetButton.RegisterCallback<FocusInEvent>(_ => resetButton.Blur());
#endif
            resetButton.style.flexGrow = 0;
            resetButton.style.flexShrink = 0;

            titleLabel = new Label("Time Scale") { name = "TRnK.ToolkitTimeScaleTitle" };
            titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            titleLabel.style.fontSize = 12;
            titleLabel.style.marginLeft = 1;
            titleLabel.style.marginRight = 2; // slightly closer to slider
            titleLabel.style.minWidth = 72;
            titleLabel.style.maxWidth = 100;

            rootContainer.Add(titleLabel);
            rootContainer.Add(timeSlider);
            rootContainer.Add(valueLabel);
            rootContainer.Add(resetButton);

            toolbarRoot.Add(rootContainer);
            PositionContainer(toolbarRoot, rootContainer);
            rootContainer.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                PositionContainer(toolbarRoot, rootContainer);
                var h = rootContainer.resolvedStyle.height;
                if (h > 0)
                {
                    float buttonH = h;
                    float innerH = Mathf.Max(14, h - 6);
                    timeSlider.style.height = innerH;
                    valueLabel.style.height = innerH;
                    resetButton.style.minHeight = buttonH;
                    resetButton.style.height = buttonH;
                    resetButton.style.maxHeight = buttonH;
                    resetButton.style.minWidth = buttonH * 2 - 8; // reduce width by ~3px
                    resetButton.style.width = buttonH * 2 - 8;
                    resetButton.style.maxWidth = buttonH * 3;
                    if (resetIcon != null)
                    {
                        var tex = resetIcon.image as Texture2D;
                        int icon = ToolbarUtils.ComputeCrispIconSize(buttonH, tex, 4f);
                        resetIcon.style.width = icon;
                        resetIcon.style.height = icon;
                        resetIcon.style.left = StyleKeyword.Null;
                        resetIcon.style.right = StyleKeyword.Null;
                        resetIcon.style.top = StyleKeyword.Null;
                        resetIcon.style.bottom = StyleKeyword.Null;
                        resetIcon.scaleMode = ScaleMode.ScaleToFit;
                    }
#if UNITY_2022_1_OR_NEWER
                    int br = 5;
                    resetButton.style.borderTopLeftRadius = br;
                    resetButton.style.borderTopRightRadius = br;
                    resetButton.style.borderBottomLeftRadius = br;
                    resetButton.style.borderBottomRightRadius = br;
#endif
                }
            });

            // Register dynamic reposition with central watcher (no styling changes)
            // Register with ToolbarLayoutWatcher if available
            ToolbarUtils.TryRegisterLayoutWatcher(() =>
            {
                var rootLatest = ToolbarUtils.GetToolbarRoot();
                if (rootLatest != null && rootContainer != null) PositionContainer(rootLatest, rootContainer);
            });
#endif
        }

        private static void OnSliderChanged(float v)
        {
            v = Mathf.Clamp(v, MinTimeScale, MaxTimeScale);
            if (Mathf.Approximately(v, lastAppliedTimeScale)) return;
            lastAppliedTimeScale = v;
#if !UNITY_6000_3_OR_NEWER
            valueLabel.text = FormatValue(v);
#endif
            PushValueToTimeManager();
            ApplyTimeScaleRuntimeOnly();
        }

        internal static void RefreshFromSettings()
        {
            // Called when project settings change to update slider bounds and clamp current
            float newMax = MaxTimeScale;
#if !UNITY_6000_3_OR_NEWER
            if (timeSlider != null)
            {
                timeSlider.highValue = newMax;
                timeSlider.lowValue = MinTimeScale;
                float clamped = Mathf.Clamp(lastAppliedTimeScale, MinTimeScale, newMax);
                lastAppliedTimeScale = clamped;
                timeSlider.SetValueWithoutNotify(clamped);
            }
#endif
#if !UNITY_6000_3_OR_NEWER
            if (valueLabel != null) valueLabel.text = FormatValue(lastAppliedTimeScale);
#endif
            ApplyTimeScaleRuntimeOnly();
        }

        private static void ResetTimeScale()
        {
            lastAppliedTimeScale = DefaultTimeScale;
#if !UNITY_6000_3_OR_NEWER
            if (timeSlider != null) timeSlider.SetValueWithoutNotify(lastAppliedTimeScale);
            if (valueLabel != null) valueLabel.text = FormatValue(lastAppliedTimeScale);
#endif
            PushValueToTimeManager();
            ApplyTimeScaleRuntimeOnly();
        }

        // Apply only to runtime (not editor ProjectSettings value) – value already stored in asset by PushValueToTimeManager.
        private static void ApplyTimeScaleRuntimeOnly()
        {
            if (Application.isPlaying) Time.timeScale = lastAppliedTimeScale;
        }

        private static void PushValueToTimeManager()
        {
            if (timeManagerSO == null || timeScaleProp == null) LoadTimeManager();
            if (timeManagerSO != null && timeScaleProp != null)
            {
                timeManagerSO.Update();
                timeScaleProp.floatValue = lastAppliedTimeScale;
                timeManagerSO.ApplyModifiedPropertiesWithoutUndo();
            }
            // Legacy persistence (non-authoritative now)
            EditorPrefs.SetFloat(PrefStoredValue, lastAppliedTimeScale);
        }

        private static void UpdateExternalSync()
        {
            if (!installed) return;

#if !UNITY_6000_3_OR_NEWER
            // Self-heal: toolbar root can be rebuilt and drop our VisualElement.
            if (rootContainer == null || rootContainer.parent == null)
            {
                installed = false;
                EditorApplication.delayCall += EnsureInstall;
                return;
            }
#endif
            // Poll at ~10Hz to avoid unnecessary allocations
            if (EditorApplication.timeSinceStartup - lastPollTime < 0.1d) return;
            lastPollTime = EditorApplication.timeSinceStartup;
            if (timeManagerSO == null || timeScaleProp == null) LoadTimeManager();
            if (timeManagerSO == null || timeScaleProp == null) return;
            timeManagerSO.Update();
            float ext = Mathf.Clamp(timeScaleProp.floatValue, MinTimeScale, MaxTimeScale);
            if (!Mathf.Approximately(ext, lastAppliedTimeScale))
            {
                lastAppliedTimeScale = ext;
#if !UNITY_6000_3_OR_NEWER
                if (timeSlider != null) timeSlider.SetValueWithoutNotify(ext);
                if (valueLabel != null) valueLabel.text = FormatValue(ext);
#endif
                ApplyTimeScaleRuntimeOnly();
            }
        }

        private static string FormatValue(float v)
        {
            if (v < 1f) return v.ToString("0.00");
            if (v < 10f) return v.ToString("0.00");
            return v.ToString("0.0");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Re-apply in case value changed while not playing
                ApplyTimeScaleRuntimeOnly();
            }
        }

        // GetToolbarRoot moved to ToolbarUtils

        private static VisualElement FindPlayControlsCluster(VisualElement root) => FindCandidateRecursive(root, 0);
        private static VisualElement FindCandidateRecursive(VisualElement ve, int depth)
        {
            if (depth > 6) return null;
            int buttons = 0;
            for (int i = 0; i < ve.childCount; i++) if (ToolbarUtils.LooksLikeToolbarButton(ve[i])) buttons++;
            if (buttons >= 3) return ve.childCount > 0 ? ve[0] : ve; // left-most in cluster
            for (int i = 0; i < ve.childCount; i++)
            {
                var found = FindCandidateRecursive(ve[i], depth + 1);
                if (found != null) return found;
            }
            return null;
        }
        // World position helpers moved to ToolbarUtils

        private static void PositionContainer(VisualElement toolbarRoot, VisualElement container)
        {
            float containerWidth = container.layout.width > 0 ? container.layout.width : 200f;
            var playCluster = FindPlayControlsCluster(toolbarRoot);
            float top = 0f; float height = 18f; float left;
            if (playCluster != null)
            {
                float playX = ToolbarUtils.GetWorldX(playCluster, toolbarRoot);
                left = playX - ToolbarUtils.AfterControlSpacing - containerWidth;
                if (left < 4) left = 4;
                height = playCluster.layout.height > 0 ? playCluster.layout.height : 22f;
                top = ToolbarUtils.GetWorldY(playCluster, toolbarRoot) + (height - container.resolvedStyle.height) * 0.5f;
            }
            else
            {
                left = 120f;
            }
            container.style.left = left;
            container.style.top = top;
            container.style.height = height;
        }

        internal static void ApplyPreferenceChange(bool enabled)
        {
#if UNITY_6000_3_OR_NEWER
            // Unity 6.3+ uses MainToolbarElement integration; the element polls hideToolbar itself.
            installed = enabled;
            return;
#else
            if (enabled)
            {
                if (!installed) EnsureInstall();
            }
            else
            {
                if (installed && rootContainer != null && rootContainer.parent != null)
                {
                    rootContainer.parent.Remove(rootContainer);
                }
                installed = false;
                rootContainer = null;
                timeSlider = null;
                resetButton = null;
                valueLabel = null;
                titleLabel = null;
                timeManagerSO = null; timeScaleProp = null;
            }
#endif
        }
    }
}
#endif