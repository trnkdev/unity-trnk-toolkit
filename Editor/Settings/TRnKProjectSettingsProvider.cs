#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TRnK.Toolkit
{
    internal static class TRnKProjectSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/TRnK.Toolkit", SettingsScope.Project)
            {
                label = "TRnK.Toolkit",
                guiHandler = _ =>
                {
                    var settings = TRnKSettings.GetOrCreate();

                    using var labelScope = new LabelWidthScope(300f);

                    EditorGUILayout.BeginVertical("box");

                    EditorGUI.BeginChangeCheck();
                    bool hideToolbar = EditorGUILayout.Toggle(new GUIContent("Hide TRnK.Toolkit toolbar", "Hide all TRnK.Toolkit toolbar controls (Scene Switcher, Time Scale, Clear PlayerPrefs)."), settings.hideToolbar);
                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.hideToolbar = hideToolbar;
                        EditorAssetUtils.MarkDirtyAndSave(settings);
                        MainToolbarActivator.SetDisplayed(!hideToolbar);
                    }

                    if (!settings.hideToolbar)
                    {
                        EditorGUI.BeginChangeCheck();
                        bool act = EditorGUILayout.Toggle(new GUIContent("Prefer Activating Loaded Additives", "If selected scene is already loaded additively, make it active instead of reopening."), settings.activateLoadedAdditiveOnSelect);
                        if (EditorGUI.EndChangeCheck()) { settings.activateLoadedAdditiveOnSelect = act; EditorAssetUtils.MarkDirtyAndSave(settings); }

                        EditorGUI.BeginChangeCheck();
                        int maxVal = EditorGUILayout.IntSlider(new GUIContent("Max Time Scale", "Maximum value for the Time Scale slider (10–100)."), Mathf.Clamp(settings.timeScaleMax, 10, 100), 10, 100);
                        if (EditorGUI.EndChangeCheck())
                        {
                            settings.timeScaleMax = maxVal;
                            EditorUtility.SetDirty(settings);
                            // Avoid stutter: don't SaveAssets on every drag; just refresh tool instantly
                            TimeScaleTool.RefreshFromSettings();
                        }

                        EditorGUI.BeginChangeCheck();
                        bool ar = EditorGUILayout.Toggle(new GUIContent("Auto re-enter Play Mode after clearing prefs", "After clearing PlayerPrefs, automatically enter Play Mode if previously playing or explicitly requested."), settings.autoReenterPlayAfterClear);
                        if (EditorGUI.EndChangeCheck()) { settings.autoReenterPlayAfterClear = ar; EditorAssetUtils.MarkDirtyAndSave(settings); }
                    }

                    EditorGUILayout.EndVertical();
                },
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "TRnK.Toolkit", "Scene", "Toolbar", "TimeScale", "PlayerPrefs" })
            };
            return provider;
        }
    }
}
#endif