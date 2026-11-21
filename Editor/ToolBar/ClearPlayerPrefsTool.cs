#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using TRnK.Logger;
using UnityEditor.Toolbars;

namespace TRnK.Toolkit
{
    [InitializeOnLoad]
    internal static class ClearPlayerPrefsTool
    {
        private const string ResumePlayKey = "TRnK.Toolkit:CPP:ResumePlay";
        private const string PendingClearKey = "TRnK.Toolkit:CPP:PendingClear";
        private const string RequestedFromPlayKey = "TRnK.Toolkit:CPP:RequestedFromPlay";

        static ClearPlayerPrefsTool()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnUpdate;
        }

        internal static void TriggerClearFromToolbar()
        {
            OnClearClicked();
        }

        private const string ToolbarElementId = "TRnK.Toolkit/Clear PlayerPrefs";

        private static Texture2D s_icon;

        [MainToolbarElement(ToolbarElementId, defaultDockPosition = MainToolbarDockPosition.Right, defaultDockIndex = 101)]
        public static MainToolbarElement CreateMainToolbarElement()
        {
            if (s_icon == null)
            {
                s_icon = ToolbarUtils.GetBestIcon(
                    "d_TreeEditor.Trash",
                    "TreeEditor.Trash",
                    "P4_DeletedLocal",
                    "d_P4_DeletedLocal"
                );
            }

            var content = new MainToolbarContent(s_icon, "Clear All PlayerPrefs (with confirmation)");
            return new MainToolbarButton(content, TriggerClearFromToolbar);
        }

        private static void OnClearClicked()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear All PlayerPrefs?",
                "This will delete all PlayerPrefs for this project.\nThis cannot be undone.",
                "Clear",
                "Cancel");
            if (!confirmed) return;

            bool domainReloadDisabled = TRnK.Utilities.Utils.IsReloadDomainDisabled();
            bool autoReenter = TRnKSettings.GetOrCreate().autoReenterPlayAfterClear;

            if (!EditorApplication.isPlaying)
            {
                PerformClear();
                bool willReenter = TRnKSettings.GetOrCreate().autoReenterPlayAfterClear && SessionState.GetBool(RequestedFromPlayKey, false);
                if (domainReloadDisabled)
                {
                    // Domain reload disabled: explicitly request a reload now per spec
                    // But skip if we will immediately enter play, to avoid redundant reload.
                    if (!willReenter) RequestReload();
                }
                else
                {
                    // Domain reload enabled: no extra reload necessary here unless we won't re-enter play soon.
                    // (Play transition causes reload; staying in edit mode may be fine without reload.)
                }
                // Not playing: only re-enter if preference asks us to and caller wants it (we only re-enter on explicit request via flag)
                SessionState.SetBool(ResumePlayKey, autoReenter && SessionState.GetBool(RequestedFromPlayKey, false));
            }
            else
            {
                // We came from play mode
                SessionState.SetBool(RequestedFromPlayKey, true);
                SessionState.SetBool(PendingClearKey, true);
                SessionState.SetBool(ResumePlayKey, autoReenter);
                EditorApplication.isPlaying = false;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (SessionState.GetBool(PendingClearKey, false))
                {
                    PerformClear();
                    SessionState.SetBool(PendingClearKey, false);

                    bool domainReloadDisabled = TRnK.Utilities.Utils.IsReloadDomainDisabled();
                    bool willReenter = SessionState.GetBool(ResumePlayKey, false);
                    if ((domainReloadDisabled && willReenter) || (!domainReloadDisabled && !willReenter))
                    {
                        // Disabled + Will re-enter: reload once before re-entering Play.
                        // Enabled + Won't re-enter: reload once to apply changes in Edit Mode.
                        RequestReload();
                    }
                }
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SessionState.SetBool(PendingClearKey, false);
                SessionState.SetBool(ResumePlayKey, false);
                SessionState.SetBool(RequestedFromPlayKey, false);
            }
        }

        private static void OnUpdate()
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isCompiling)
            {
                if (SessionState.GetBool(ResumePlayKey, false))
                {
                    SessionState.SetBool(ResumePlayKey, false);
                    EditorApplication.isPlaying = true;
                }
                // Clear transient request flag when we're safely in edit mode
                if (!EditorApplication.isPlaying) SessionState.SetBool(RequestedFromPlayKey, false);
            }
        }

        private static void PerformClear()
        {
            try
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to clear PlayerPrefs: {ex}");
            }
        }

        private static void RequestReload()
        {
            try { EditorUtility.RequestScriptReload(); }
            catch { }
        }
    }
}
#endif
