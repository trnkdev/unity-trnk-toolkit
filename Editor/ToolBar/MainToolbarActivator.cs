#if UNITY_EDITOR && UNITY_6000_3_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace TRnK.Toolkit
{
    /// <summary>
    /// Unity 6.3+ registers [MainToolbarElement] items as overlays on the main toolbar,
    /// and custom overlays start hidden — the user would have to right-click the toolbar
    /// and enable each element manually (MainToolbarElementAttribute has no defaultDisplay).
    /// This shows the TRnK elements once per user+project via the public Overlay.displayed
    /// API; afterwards the user's own show/hide choices are respected.
    /// </summary>
    [InitializeOnLoad]
    internal static class MainToolbarActivator
    {
        internal const string ElementIdPrefix = "TRnK.Toolkit/";

        private const string FirstShowKeyPrefix = "TRnK.Toolkit:MainToolbarFirstShow:";
        private const double RetryInterval      = 0.5d;
        private const double GiveUpAfter        = 60d;

        private static double s_nextProbeTime;
        private static double s_giveUpTime;

        static MainToolbarActivator()
        {
            if (!EditorPrefs.GetBool(FirstShowKey(), false))
                EditorApplication.update += TryShowOnce;
        }

        /// <summary>Shows or hides all TRnK main toolbar overlays (HideToolbar preference).</summary>
        internal static void SetDisplayed(bool show)
        {
            var canvas = FindMainToolbarCanvas();
            if (canvas == null) return;
            ApplyToTRnKOverlays(canvas, show);
        }

        private static string FirstShowKey() => FirstShowKeyPrefix + PlayerSettings.productGUID;

        private static void TryShowOnce()
        {
            // The MainToolbarWindow and its overlays are created after domain reload;
            // probe cheaply until they exist.
            double now = EditorApplication.timeSinceStartup;
            if (now < s_nextProbeTime) return;
            s_nextProbeTime = now + RetryInterval;

            if (s_giveUpTime <= 0d) s_giveUpTime = now + GiveUpAfter;
            if (now > s_giveUpTime)
            {
                // Toolbar never appeared (or internals changed) — retry next session.
                EditorApplication.update -= TryShowOnce;
                return;
            }

            var canvas = FindMainToolbarCanvas();
            if (canvas == null) return;

            bool hidden = false;
            try { hidden = TRnKSettings.GetOrCreate().hideToolbar; } catch { }

            if (!ApplyToTRnKOverlays(canvas, !hidden)) return; // overlays not built yet — retry

            EditorPrefs.SetBool(FirstShowKey(), true);
            EditorApplication.update -= TryShowOnce;
        }

        private static bool ApplyToTRnKOverlays(OverlayCanvas canvas, bool displayed)
        {
            bool foundAny = false;
            foreach (var overlay in canvas.overlays)
            {
                if (overlay == null || string.IsNullOrEmpty(overlay.id)) continue;
                if (!overlay.id.StartsWith(ElementIdPrefix, StringComparison.Ordinal)) continue;
                overlay.displayed = displayed;
                foundAny = true;
            }
            return foundAny;
        }

        private static OverlayCanvas FindMainToolbarCanvas()
        {
            // MainToolbarWindow is internal; locate it by type name. Everything else
            // (overlayCanvas, overlays, id, displayed) is public API.
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i].GetType().FullName == "UnityEditor.MainToolbarWindow")
                    return windows[i].overlayCanvas;
            }
            return null;
        }
    }
}
#endif
