#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Toolbars;
using UnityEngine;

namespace TRnK.Toolkit
{
    [InitializeOnLoad]
    internal static class TimeScaleTool
    {
        private const float DefaultTimeScale = 1f;
        private const float MinTimeScale = 0f;
        private static float MaxTimeScale => Mathf.Max(10f, (float)TRnKSettings.GetOrCreate().timeScaleMax);

        private static float s_lastAppliedTimeScale = 1f;

        // TimeManager sync
        private static SerializedObject s_timeManager;
        private static SerializedProperty s_timeScaleProp;
        private static double s_lastPollTime; // throttle external polling slightly

        static TimeScaleTool()
        {
            EditorApplication.update += UpdateExternalSync;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private const string ToolbarElementId = "TRnK.Toolkit/Time Scale";

        // The factory is re-invoked by MainToolbar.Refresh(ToolbarElementId), so it must
        // build the elements from current state and hold no element instances.
        // Returning IEnumerable groups the slider and reset button into one docked unit.
        [MainToolbarElement(ToolbarElementId, defaultDockPosition = MainToolbarDockPosition.Left)]
        public static IEnumerable<MainToolbarElement> CreateMainToolbarElement()
        {
            LoadTimeManager();
            float v = GetCurrentTimeScale();
            var content = new MainToolbarContent("Time Scale");
            var slider = new MainToolbarSlider(content, v, GetMin(), GetMax(), SetTimeScale);
            slider.populateContextMenu = menu =>
                menu.AppendAction("Reset (1.0)", _ =>
                {
                    ResetToDefault();
                    MainToolbar.Refresh(ToolbarElementId);
                });
            yield return slider;

            var resetIcon = ToolbarUtils.GetBestIcon("d_Refresh", "Refresh", "TreeEditor.Refresh", "d_RotateTool", "RotateTool");
            var resetContent = new MainToolbarContent(resetIcon, "Reset Time Scale (1.0)");
            yield return new MainToolbarButton(resetContent, () =>
            {
                ResetToDefault();
                MainToolbar.Refresh(ToolbarElementId);
            });
        }

        internal static float GetCurrentTimeScale()
        {
            if (s_timeManager == null || s_timeScaleProp == null) LoadTimeManager();
            if (s_timeManager != null && s_timeScaleProp != null)
            {
                s_timeManager.Update();
                s_lastAppliedTimeScale = Mathf.Clamp(s_timeScaleProp.floatValue, MinTimeScale, MaxTimeScale);
            }
            return s_lastAppliedTimeScale;
        }

        internal static float GetMin() => MinTimeScale;
        internal static float GetMax() => MaxTimeScale;

        internal static void SetTimeScale(float v)
        {
            v = Mathf.Clamp(v, MinTimeScale, MaxTimeScale);
            if (Mathf.Approximately(v, s_lastAppliedTimeScale)) return;
            s_lastAppliedTimeScale = v;
            PushValueToTimeManager();
            ApplyTimeScaleRuntimeOnly();
        }

        internal static void ResetToDefault()
        {
            s_lastAppliedTimeScale = DefaultTimeScale;
            PushValueToTimeManager();
            ApplyTimeScaleRuntimeOnly();
        }

        private static void LoadTimeManager()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset");
            if (assets != null && assets.Length > 0)
            {
                s_timeManager = new SerializedObject(assets[0]);
                s_timeScaleProp = s_timeManager.FindProperty("m_TimeScale");
            }
        }

        internal static void RefreshFromSettings()
        {
            // Called when project settings change to update slider bounds and clamp current
            float newMax = MaxTimeScale;
            s_lastAppliedTimeScale = Mathf.Clamp(s_lastAppliedTimeScale, MinTimeScale, newMax);
            MainToolbar.Refresh(ToolbarElementId);
            ApplyTimeScaleRuntimeOnly();
        }

        // Apply only to runtime (not editor ProjectSettings value) – value already stored in asset by PushValueToTimeManager.
        private static void ApplyTimeScaleRuntimeOnly()
        {
            if (Application.isPlaying) Time.timeScale = s_lastAppliedTimeScale;
        }

        private static void PushValueToTimeManager()
        {
            if (s_timeManager == null || s_timeScaleProp == null) LoadTimeManager();
            if (s_timeManager != null && s_timeScaleProp != null)
            {
                s_timeManager.Update();
                s_timeScaleProp.floatValue = s_lastAppliedTimeScale;
                s_timeManager.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void UpdateExternalSync()
        {
            // Poll at ~10Hz to avoid unnecessary allocations
            if (EditorApplication.timeSinceStartup - s_lastPollTime < 0.1d) return;
            s_lastPollTime = EditorApplication.timeSinceStartup;
            if (s_timeManager == null || s_timeScaleProp == null) LoadTimeManager();
            if (s_timeManager == null || s_timeScaleProp == null) return;
            s_timeManager.Update();
            float ext = Mathf.Clamp(s_timeScaleProp.floatValue, MinTimeScale, MaxTimeScale);
            if (!Mathf.Approximately(ext, s_lastAppliedTimeScale))
            {
                s_lastAppliedTimeScale = ext;
                MainToolbar.Refresh(ToolbarElementId);
                ApplyTimeScaleRuntimeOnly();
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Re-apply in case value changed while not playing
                ApplyTimeScaleRuntimeOnly();
            }
        }
    }
}
#endif