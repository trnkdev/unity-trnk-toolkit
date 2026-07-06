#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TRnK.Logger;

#if UNITY_2020_1_OR_NEWER
using UnityEditor.UIElements;
#endif
#if UNITY_6000_3_OR_NEWER
using UnityEditor.Toolbars;
#endif

namespace TRnK.Toolkit
{
    [InitializeOnLoad]
    internal static class ClearPlayerPrefsTool
    {
        private const string ContainerName = "TRnK.ToolkitClearPrefsContainer";
        private const string ResumePlayKey = "TRnK.Toolkit:CPP:ResumePlay";
        private const string PendingClearKey = "TRnK.Toolkit:CPP:PendingClear";
        private const string RequestedFromPlayKey = "TRnK.Toolkit:CPP:RequestedFromPlay";

        private static bool installed;
#if !UNITY_6000_3_OR_NEWER
        private static VisualElement container;
        private static Button clearButton;
        private static Image clearIcon;
#endif

        static ClearPlayerPrefsTool()
        {
#if !UNITY_6000_3_OR_NEWER
            EditorApplication.delayCall += EnsureInstall;
#endif
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnUpdate;
        }

        internal static void TriggerClearFromToolbar()
        {
            OnClearClicked();
        }

#if UNITY_6000_3_OR_NEWER
        private static Texture2D unity6000Icon;
        private static MainToolbarButton s_unity6000Button;

        [MainToolbarElement("TRnK.Toolkit/Clear PlayerPrefs", defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarElement CreateMainToolbarElement()
        {
            if (unity6000Icon == null)
            {
                unity6000Icon = ToolbarUtils.GetBestIcon(
                    "d_TreeEditor.Trash",
                    "TreeEditor.Trash",
                    "P4_DeletedLocal",
                    "d_P4_DeletedLocal"
                );
            }

            var content = new MainToolbarContent(unity6000Icon)
            {
                text = string.Empty,
                tooltip = "Clear All PlayerPrefs (with confirmation)"
            };

            s_unity6000Button = new MainToolbarButton(content, TriggerClearFromToolbar);
            ApplyUnity6000Visibility();
            return s_unity6000Button;
        }

        private static void ApplyUnity6000Visibility()
        {
            if (s_unity6000Button == null) return;
            bool hidden = false;
            try { hidden = TRnKSettings.GetOrCreate().hideToolbar; } catch { }
            s_unity6000Button.displayed = !hidden;
            s_unity6000Button.enabled = !hidden;
        }
#endif

        private static void EnsureInstall()
        {
#if UNITY_6000_3_OR_NEWER
            // Unity 6.3+ uses MainToolbarElement integration.
            return;
#else
            if (installed) return;
            // Respect global HideToolbar preference (default: hidden)
            try { if (TRnKSettings.GetOrCreate().hideToolbar) return; } catch { }
            var root = ToolbarUtils.GetToolbarRoot();
            if (root == null)
            {
                EditorApplication.delayCall += EnsureInstall;
                return;
            }

            // Defensive: domain reload / toolbar rebuild can leave old injected elements behind.
            ToolbarUtils.RemoveAllByName(root, ContainerName);

            container = new VisualElement { name = ContainerName };
            container.style.position = Position.Absolute;
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            ToolbarUtils.ApplyRoundedStyling(container);

#if UNITY_2020_1_OR_NEWER
            clearButton = new ToolbarButton(OnClearClicked)
            {
                text = string.Empty,
                tooltip = "Clear All PlayerPrefs (with confirmation)"
            };
            clearButton.AddToClassList("unity-toolbar-button");
#else
            clearButton = new Button(OnClearClicked)
            {
                text = string.Empty,
                tooltip = "Clear All PlayerPrefs (with confirmation)"
            };
#endif
            clearButton.focusable = false;
            clearButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            clearButton.style.paddingLeft = 0;
            clearButton.style.paddingRight = 0;
            clearButton.style.paddingTop = 0;
            clearButton.style.paddingBottom = 0;
            clearButton.style.marginLeft = 0;
            clearButton.style.marginRight = 0;
            clearButton.style.fontSize = 11;
            clearButton.style.backgroundColor = StyleKeyword.Null;
            clearButton.style.flexGrow = 0;
            clearButton.style.flexShrink = 0;
            clearButton.style.justifyContent = Justify.Center;
            clearButton.style.alignItems = Align.Center;

            // add icon image — pick the clearest built-in variant
            var trashTex = ToolbarUtils.GetBestIcon(
                "d_TreeEditor.Trash",
                "TreeEditor.Trash",
                "P4_DeletedLocal",
                "d_P4_DeletedLocal"
            );
            clearIcon = new Image { image = trashTex, scaleMode = ScaleMode.ScaleToFit };
            clearIcon.style.alignSelf = Align.Center;
            clearIcon.style.marginLeft = 0; clearIcon.style.marginRight = 0; clearIcon.style.marginTop = 0; clearIcon.style.marginBottom = 0;
            clearButton.Add(clearIcon);

            container.Add(clearButton);
            root.Add(container);

            container.RegisterCallback<GeometryChangedEvent>(_ => ResizeButtonToContainer());
            // Re-anchor when SceneSwitcher or toolbar geometry changes
            var sceneContainer = ToolbarUtils.FindByName(root, "TRnK.ToolkitSceneSwitcherContainer");
            if (sceneContainer != null)
            {
                sceneContainer.RegisterCallback<GeometryChangedEvent>(_ =>
                {
                    PositionContainer(root, container);
                    ResizeButtonToContainer();
                });
            }
            ToolbarUtils.TryRegisterLayoutWatcher(() =>
            {
                var rootLatest = ToolbarUtils.GetToolbarRoot();
                if (rootLatest != null && container != null)
                {
                    PositionContainer(rootLatest, container);
                    ResizeButtonToContainer();
                }
            });

            PositionContainer(root, container);
            ResizeButtonToContainer();
            installed = true;
#endif
        }

        private static void Uninstall()
        {
#if UNITY_6000_3_OR_NEWER
            installed = false;
            return;
#else
            if (!installed) return;
            installed = false;
            try
            {
                if (container != null && container.parent != null)
                {
                    container.parent.Remove(container);
                }
            }
            catch { }
            container = null;
            clearButton = null;
            clearIcon = null;
#endif
        }

        internal static void ApplyPreferenceChange(bool enabled)
        {
#if UNITY_6000_3_OR_NEWER
            installed = enabled;
            ApplyUnity6000Visibility();
            return;
#else
            if (enabled)
            {
                // Attempt install when enabled
                EditorApplication.delayCall += EnsureInstall;
            }
            else
            {
                // Remove toolbar UI when disabled
                Uninstall();
            }
#endif
        }

        private static void ResizeButtonToContainer()
        {
#if UNITY_6000_3_OR_NEWER
            return;
#else
            if (container == null || clearButton == null) return;
            var h = container.resolvedStyle.height;
            if (h <= 0) return;
            float buttonH = h;
            clearButton.style.minHeight = buttonH;
            clearButton.style.height = buttonH;
            clearButton.style.maxHeight = buttonH;
            clearButton.style.minWidth = buttonH * 2 - 8; // reduce width by ~3px
            clearButton.style.width = buttonH * 2 - 8;
            clearButton.style.maxWidth = buttonH * 3;
            if (clearIcon != null)
            {
                var tex = clearIcon.image as Texture2D;
                int icon = ToolbarUtils.ComputeCrispIconSize(buttonH, tex, 4f);
                clearIcon.style.width = icon;
                clearIcon.style.height = icon;
                clearIcon.style.left = StyleKeyword.Null;
                clearIcon.style.right = StyleKeyword.Null;
                clearIcon.style.top = StyleKeyword.Null;
                clearIcon.style.bottom = StyleKeyword.Null;
                clearIcon.scaleMode = ScaleMode.ScaleToFit;
            }
#if UNITY_2022_1_OR_NEWER
            int br = 5;
            clearButton.style.borderTopLeftRadius = br;
            clearButton.style.borderTopRightRadius = br;
            clearButton.style.borderBottomLeftRadius = br;
            clearButton.style.borderBottomRightRadius = br;
#endif
#endif
        }

        private static void PositionContainer(VisualElement toolbarRoot, VisualElement ve)
        {
            float containerWidth = ve.layout.width > 0 ? ve.layout.width : 60f;
            // Preferred: 20px to the right of SceneSwitcherTool container
            var sceneSwitcherContainer = ToolbarUtils.FindByName(toolbarRoot, "TRnK.ToolkitSceneSwitcherContainer");
            if (sceneSwitcherContainer != null)
            {
                float rightX = ToolbarUtils.GetWorldX(sceneSwitcherContainer, toolbarRoot) + sceneSwitcherContainer.layout.width;
                float topY = ToolbarUtils.GetWorldY(sceneSwitcherContainer, toolbarRoot);
                float sceneHeight = sceneSwitcherContainer.layout.height > 0 ? sceneSwitcherContainer.layout.height : ve.resolvedStyle.height;
                ve.style.left = rightX + ToolbarUtils.AfterControlSpacing;
                ve.style.top = topY;
                if (sceneHeight > 0) ve.style.height = sceneHeight;
                return;
            }

            // Next preference: align next to TimeScale container (to its left keeps one line alignment)
            var timeScaleContainer = ToolbarUtils.FindByName(toolbarRoot, "TRnK.ToolkitTimeScaleContainer");
            if (timeScaleContainer != null)
            {
                float timeLeft = ToolbarUtils.GetWorldX(timeScaleContainer, toolbarRoot);
                float timeTop = ToolbarUtils.GetWorldY(timeScaleContainer, toolbarRoot);
                float timeH = timeScaleContainer.layout.height > 0 ? timeScaleContainer.layout.height : ve.resolvedStyle.height;
                float left = timeLeft - 6f - containerWidth;
                ve.style.left = left;
                ve.style.top = timeTop;
                if (timeH > 0) ve.style.height = timeH;
                return;
            }

            // Fallback: place to the left of play controls cluster like TimeScaleTool
            var playCluster = FindPlayControlsCluster(toolbarRoot);
            float top = 0f; float height = 18f; float leftFallback;
            if (playCluster != null)
            {
                float playX = ToolbarUtils.GetWorldX(playCluster, toolbarRoot);
                leftFallback = playX - 20f - containerWidth;
                if (leftFallback < 4) leftFallback = 4;
                height = playCluster.layout.height > 0 ? playCluster.layout.height : 22f;
                top = ToolbarUtils.GetWorldY(playCluster, toolbarRoot) + (height - ve.resolvedStyle.height) * 0.5f;
            }
            else
            {
                leftFallback = 80f;
            }
            ve.style.left = leftFallback;
            ve.style.top = top;
            ve.style.height = height;
        }

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
            // Keep position in sync in case toolbar contents shift silently
#if !UNITY_6000_3_OR_NEWER
            if (installed)
            {
                // Self-heal: toolbar root can be rebuilt and drop our VisualElement.
                if (container == null || container.parent == null)
                {
                    installed = false;
                    EditorApplication.delayCall += EnsureInstall;
                }
            }

            var root = ToolbarUtils.GetToolbarRoot();
            if (root != null && container != null)
            {
                PositionContainer(root, container);
                ResizeButtonToContainer();
            }
#endif

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
