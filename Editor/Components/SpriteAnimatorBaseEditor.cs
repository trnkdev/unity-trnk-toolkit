#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace TRnK.Components
{
    [CustomEditor(typeof(SpriteAnimatorBase), true)]
    internal class SpriteAnimatorEditorBase :
#if ODIN_INSPECTOR
        OdinEditor
#else
        Editor
#endif
    {
        private const string TabSessionKey = "TRnK.SpriteAnimatorEditorBase.Tab";
        private SerializedProperty _sprites;
        private SerializedProperty _frameRate;
        private SerializedProperty _loopMode;
        private SerializedProperty _playOnAwake;
        private SerializedProperty _speedMultiplier;
        private SerializedProperty _useUnscaledTime;
        private SerializedProperty _startAtRandomFrame;
        private SerializedProperty _frameEvents;
        private SerializedProperty _onCycleComplete;

        private Sprite[] _previousSprites;
        private int _selectedFrameToAdd = 0;
        private List<bool> _eventFoldouts = new();
        private int _selectedTab = 0; // 0: Settings, 1: Events
        private static GUIStyle _foldoutNoFocusStyle;
        private static readonly GUIContent RemoveEventContent = new("\u00d7");
        private static readonly GUIContent OnFrameContent = new("OnFrame");
        private static readonly GUIContent AddEventContent = new("Add Event");
        private static readonly GUIContent SetSprite0Content = new("Frame 0", "Set the renderer sprite to the first frame of the animation.");
        private static readonly GUIContent PreviewPlayContent = new("Preview", "Preview the animation in the editor.");
        private static readonly GUIContent PreviewStopContent = new("Stop", "Stop the editor preview.");

        private readonly HashSet<int> _usedFrames = new();
        private readonly HashSet<int> _duplicateFrames = new();
        private readonly Dictionary<int, int> _frameCount = new();
        private readonly List<int> _availableFrames = new();
        private readonly List<string> _availableFrameOptions = new();
        private string[] _availableFrameOptionsArray = Array.Empty<string>();

        private bool _isPreviewPlaying;
        private Sprite[] _previewSprites;
        private Sprite _spriteBeforePreview;
        private int _previewCurrentFrame;
        private bool _previewReversed;
        private float _previewTimer;
        private double _previewLastTime;

#if ODIN_INSPECTOR
        private PropertyTree _tree;
#endif

#if ODIN_INSPECTOR
        protected override void OnEnable()
        {
            base.OnEnable();
#else
    protected virtual void OnEnable()
    {
#endif
            _sprites = serializedObject.FindProperty("_sprites");
            _frameRate = serializedObject.FindProperty("_frameRate");
            _loopMode = serializedObject.FindProperty("_loopMode");
            _playOnAwake = serializedObject.FindProperty("_playOnAwake");
            _speedMultiplier = serializedObject.FindProperty("_speedMultiplier");
            _useUnscaledTime = serializedObject.FindProperty("_useUnscaledTime");
            _startAtRandomFrame = serializedObject.FindProperty("_startAtRandomFrame");
            _frameEvents = serializedObject.FindProperty("_frameEvents");
            _onCycleComplete = serializedObject.FindProperty("_onCycleComplete");

            _selectedTab = SessionState.GetInt(TabSessionKey, 0);
            CacheCurrentSprites();
            SyncEventFoldoutsSize();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

#if ODIN_INSPECTOR
            _tree?.Dispose();
            _tree = PropertyTree.Create(serializedObject);
            _tree.DrawMonoScriptObjectField = false;
#endif
        }

#if ODIN_INSPECTOR
        protected override void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StopPreview();
            _tree?.Dispose();
            _tree = null;
            base.OnDisable();
        }

        protected void DrawOdinUnityProperty(string unityPath)
        {
            if (_tree == null)
            {
                _tree = PropertyTree.Create(serializedObject);
                _tree.DrawMonoScriptObjectField = false;
            }

            var property = _tree.GetPropertyAtUnityPath(unityPath);
            property?.Draw();
        }

        protected void DrawOdinUnityPropertyPath(string unityPropertyPath)
        {
            if (_tree == null)
            {
                _tree = PropertyTree.Create(serializedObject);
                _tree.DrawMonoScriptObjectField = false;
            }

            var property = _tree.GetPropertyAtUnityPath(unityPropertyPath);
            property?.Draw();
        }
#endif

#if !ODIN_INSPECTOR
        protected virtual void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StopPreview();
        }
#endif

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

#if ODIN_INSPECTOR
            if (_tree == null)
            {
                _tree = PropertyTree.Create(serializedObject);
                _tree.DrawMonoScriptObjectField = false;
            }
            _tree.UpdateTree();
#endif

            // Tabs: Settings | Events
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                int eventCount = _frameEvents != null ? _frameEvents.arraySize : 0;
                string frameEventsLabel = eventCount > 0 ? $"Frame Events ({eventCount})" : "Frame Events";
                GUIContent[] tabs =
                {
                    new("Settings"),
                    new(frameEventsLabel)
                };
                int newTab = GUILayout.Toolbar(_selectedTab, tabs);
                if (newTab != _selectedTab)
                {
                    _selectedTab = newTab;
                    SessionState.SetInt(TabSessionKey, _selectedTab);
                }
            }
            EditorGUILayout.Space();

            if (_selectedTab == 0)
            {
                DrawAnimationSettings();

                if (HasAdditionalProperties())
                {
                    DrawAdditionalProperties();
                }
            }
            else
            {
                DrawFrameEvents();
            }

            EditorGUILayout.Space();
            DrawEvents();
            EditorGUILayout.Space();
            DrawPreviewControls();
            EditorGUILayout.Space();

            bool changed;
#if ODIN_INSPECTOR
            changed = _tree.ApplyChanges();
            changed |= serializedObject.ApplyModifiedProperties();
            _tree.InvokeDelayedActions();
#else
            changed = serializedObject.ApplyModifiedProperties();
#endif

            if (changed)
            {
                CheckForSpriteChanges();
            }
        }

        private void DrawAnimationSettings()
        {

#if ODIN_INSPECTOR
            DrawOdinUnityProperty("_sprites");
            DrawOdinUnityProperty("_frameRate");
            DrawOdinUnityProperty("_speedMultiplier");
#else
            EditorGUILayout.PropertyField(_sprites);
            EditorGUILayout.PropertyField(_frameRate);
            EditorGUILayout.PropertyField(_speedMultiplier);
#endif

#if ODIN_INSPECTOR
            DrawOdinUnityProperty("_loopMode");
#else
            EditorGUILayout.PropertyField(_loopMode);
#endif

#if ODIN_INSPECTOR
            DrawOdinUnityProperty("_playOnAwake");
            DrawOdinUnityProperty("_useUnscaledTime");
            DrawOdinUnityProperty("_startAtRandomFrame");
#else
            EditorGUILayout.PropertyField(_playOnAwake);
            EditorGUILayout.PropertyField(_useUnscaledTime);
            EditorGUILayout.PropertyField(_startAtRandomFrame);
#endif
        }

        protected virtual void DrawAdditionalProperties()
        {
            // Override in derived editors for component-specific properties
        }

        protected virtual bool HasAdditionalProperties()
        {
            return false;
        }

        private void DrawFrameEvents()
        {
            // When no events exist, show an instructional info box; otherwise no title/header
            if (_frameEvents.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No frame events yet. Select a frame below and click 'Add Event' to create one.", MessageType.Info);
            }

            EditorGUI.indentLevel++;

            if (_sprites.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Add sprites to enable frame events", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            // Ensure foldout list size matches events
            SyncEventFoldoutsSize();

            // Build used/duplicate frame data
            bool hasDuplicates = false;
            _usedFrames.Clear();
            _duplicateFrames.Clear();
            _frameCount.Clear();

            for (int i = 0; i < _frameEvents.arraySize; i++)
            {
                SerializedProperty frameEvent = _frameEvents.GetArrayElementAtIndex(i);
                SerializedProperty frameIndex = frameEvent.FindPropertyRelative("_frameIndex");
                int frame = frameIndex.intValue;

                _usedFrames.Add(frame);

                if (_frameCount.ContainsKey(frame))
                {
                    _frameCount[frame]++;
                    _duplicateFrames.Add(frame);
                    hasDuplicates = true;
                }
                else
                {
                    _frameCount[frame] = 1;
                }
            }

            if (hasDuplicates)
            {
                string duplicateList = string.Join(", ", _duplicateFrames);
                EditorGUILayout.HelpBox($"Duplicate frame events detected for frame(s): {duplicateList}. Each frame should only have one event.", MessageType.Error);
            }

            // Display existing frame events (each with its own foldout)
            for (int i = 0; i < _frameEvents.arraySize; i++)
            {
                SerializedProperty frameEvent = _frameEvents.GetArrayElementAtIndex(i);
                SerializedProperty frameIndex = frameEvent.FindPropertyRelative("_frameIndex");
                SerializedProperty unityEvent = frameEvent.FindPropertyRelative("_onFrame");

                // Foldout header per event with fixed height and aligned remove button
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                float headerH = EditorGUIUtility.singleLineHeight + 4f;
                Rect headerRect = EditorGUILayout.GetControlRect(false, headerH);

                bool isDuplicate = _duplicateFrames.Contains(frameIndex.intValue);
                string spriteName = "(null)";
                var si = frameIndex.intValue;
                if (si >= 0 && si < _sprites.arraySize)
                {
                    var sprop = _sprites.GetArrayElementAtIndex(si).objectReferenceValue as Sprite;
                    if (sprop != null) spriteName = sprop.name;
                }

                string foldLabel = $"Frame {frameIndex.intValue}: {spriteName}";

                // Compute rects
                Rect closeRect = new Rect(headerRect.xMax - 22f, headerRect.y + 2f, 18f, EditorGUIUtility.singleLineHeight);
                Rect foldRect = new Rect(headerRect.x + 4f, headerRect.y + 2f, headerRect.width - 30f, EditorGUIUtility.singleLineHeight);

                // Duplicate background tint
                var hover = headerRect.Contains(Event.current.mousePosition);
                if (isDuplicate)
                {
                    var tint = new Color(1f, 0.6f, 0.6f, 0.18f);
                    EditorGUI.DrawRect(headerRect, tint);
                }
                else if (hover)
                {
                    var baseTint = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.05f) : new Color(0f, 0f, 0f, 0.05f);
                    EditorGUI.DrawRect(headerRect, baseTint);
                }

                // Use custom foldout style that doesn't turn blue on focus (fallback to default if unavailable)
                EnsureStyles();
                if (_foldoutNoFocusStyle != null)
                {
                    _eventFoldouts[i] = EditorGUI.Foldout(foldRect, _eventFoldouts[i], foldLabel, true, _foldoutNoFocusStyle);
                }
                else
                {
                    _eventFoldouts[i] = EditorGUI.Foldout(foldRect, _eventFoldouts[i], foldLabel, true);
                }

                if (GUI.Button(closeRect, RemoveEventContent, EditorStyles.miniButton))
                {
                    _frameEvents.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndVertical();
                    break; // Exit after modification
                }

                if (_eventFoldouts[i])
                {
                    int prevIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0; // minimize left padding for cleaner look
                    EditorGUILayout.PropertyField(unityEvent, OnFrameContent);
                    EditorGUI.indentLevel = prevIndent;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6);
            }

            // Add new frame event section (outside bordered boxes)
            // Use a style with no margins/padding so the popup can align flush left
            EditorGUILayout.BeginVertical(GUIStyle.none);

            // Create available frame options (excluding already used frames)
            _availableFrames.Clear();
            _availableFrameOptions.Clear();

            for (int i = 0; i < _sprites.arraySize; i++)
            {
                if (!_usedFrames.Contains(i))
                {
                    _availableFrames.Add(i);
                    var sprite = _sprites.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                    _availableFrameOptions.Add(sprite != null ? $"Frame {i}: {sprite.name}" : $"Frame {i}: (null)");
                }
            }

            if (_availableFrames.Count > 0)
            {
                // Ensure selected frame index is valid
                if (_selectedFrameToAdd >= _availableFrames.Count)
                {
                    _selectedFrameToAdd = 0;
                }

                // Remove left padding for the add row by drawing at indent level 0
                int prevIndentForAdd = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                // A little breathing room above the add row
                EditorGUILayout.Space(4);

                // Full-width dropdown on the left, Add button on the right
                float btnH = EditorGUIUtility.singleLineHeight + 2f;
                Rect rowRect = EditorGUILayout.GetControlRect(false, btnH);
                const float buttonW = 110f;
                const float spacing = 6f;
                Rect buttonRect = new Rect(rowRect.xMax - buttonW, rowRect.y, buttonW, btnH);
                Rect dropdownRect = new Rect(rowRect.x, rowRect.y, rowRect.width - buttonW - spacing, btnH);

                EnsureAvailableFrameOptionsArray();
                _selectedFrameToAdd = EditorGUI.Popup(dropdownRect, _selectedFrameToAdd, _availableFrameOptionsArray);

                if (GUI.Button(buttonRect, AddEventContent))
                {
                    _frameEvents.arraySize++;
                    SerializedProperty newEvent = _frameEvents.GetArrayElementAtIndex(_frameEvents.arraySize - 1);
                    newEvent.FindPropertyRelative("_frameIndex").intValue = _availableFrames[_selectedFrameToAdd];

                    // Reset selection to first available frame after adding
                    _selectedFrameToAdd = 0;
                }

                // Restore previous indent
                EditorGUI.indentLevel = prevIndentForAdd;
            }
            else
            {
                // Draw the info box without left padding as well
                int prevIndentForInfo = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                EditorGUILayout.HelpBox("All frames already have events assigned.", MessageType.Info);
                EditorGUI.indentLevel = prevIndentForInfo;
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void EnsureAvailableFrameOptionsArray()
        {
            int count = _availableFrameOptions.Count;
            if (_availableFrameOptionsArray.Length != count)
            {
                _availableFrameOptionsArray = new string[count];
            }

            for (int i = 0; i < count; i++)
            {
                _availableFrameOptionsArray[i] = _availableFrameOptions[i];
            }
        }

        private void EnsureStyles()
        {
            if (_foldoutNoFocusStyle != null) return;
            try
            {
                var baseStyle = EditorStyles.foldout;
                if (baseStyle == null) return; // EditorStyles not ready yet
                _foldoutNoFocusStyle = new GUIStyle(baseStyle);
                // Align text colors to regular label to avoid blue-ish focus tint
                Color c = EditorStyles.label.normal.textColor;
                _foldoutNoFocusStyle.normal.textColor = c;
                _foldoutNoFocusStyle.onNormal.textColor = c;
                _foldoutNoFocusStyle.active.textColor = c;
                _foldoutNoFocusStyle.onActive.textColor = c;
                _foldoutNoFocusStyle.focused.textColor = c;
                _foldoutNoFocusStyle.onFocused.textColor = c;
                _foldoutNoFocusStyle.hover.textColor = c;
                _foldoutNoFocusStyle.onHover.textColor = c;
            }
            catch
            {
                // In case EditorStyles is not initialized; will try again later during OnGUI
                _foldoutNoFocusStyle = null;
            }
        }

        private void SyncEventFoldoutsSize()
        {
            int size = _frameEvents != null ? _frameEvents.arraySize : 0;
            if (_eventFoldouts == null)
            {
                _eventFoldouts = new List<bool>(size);
            }
            while (_eventFoldouts.Count < size) _eventFoldouts.Add(true);
            while (_eventFoldouts.Count > size) _eventFoldouts.RemoveAt(_eventFoldouts.Count - 1);
        }

        private void DrawEvents()
        {
            EditorGUILayout.PropertyField(_onCycleComplete);
        }



        private void CacheCurrentSprites()
        {
            if (_sprites.arraySize > 0)
            {
                _previousSprites = new Sprite[_sprites.arraySize];
                for (int i = 0; i < _sprites.arraySize; i++)
                {
                    _previousSprites[i] = _sprites.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                }
            }
            else
            {
                _previousSprites = null;
            }
        }

        private void CheckForSpriteChanges()
        {
            bool spritesChanged = false;

            // Check if array size changed
            if (_previousSprites == null && _sprites.arraySize > 0)
            {
                spritesChanged = true;
            }
            else if (_previousSprites != null && _sprites.arraySize != _previousSprites.Length)
            {
                spritesChanged = true;
            }
            else if (_previousSprites != null)
            {
                // Check if any sprite references changed
                for (int i = 0; i < _sprites.arraySize; i++)
                {
                    var currentSprite = _sprites.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                    if (currentSprite != _previousSprites[i])
                    {
                        spritesChanged = true;
                        break;
                    }
                }
            }

            if (spritesChanged)
            {
                // Clear invalid frame events
                ClearInvalidFrameEvents();
                CacheCurrentSprites();
            }
        }

        private void ClearInvalidFrameEvents()
        {
            if (_frameEvents.arraySize == 0) return;

            // Remove invalid frame events by iterating backwards
            for (int i = _frameEvents.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty frameEvent = _frameEvents.GetArrayElementAtIndex(i);
                SerializedProperty frameIndex = frameEvent.FindPropertyRelative("_frameIndex");

                if (frameIndex.intValue < 0 || frameIndex.intValue >= _sprites.arraySize)
                {
                    _frameEvents.DeleteArrayElementAtIndex(i);
                }
            }
        }

        // --- Editor preview controls ---

        protected virtual void SetSprite0AsPreview(Sprite sprite) { }
        protected virtual Sprite GetPreviewSprite() => null;
        protected virtual void SetPreviewSprite(Sprite sprite) { }

        private void DrawPreviewControls()
        {
            bool hasSprites = _sprites.arraySize > 0;
            bool multiEdit = targets.Length > 1;
            bool canPreview = hasSprites && !multiEdit && !Application.isPlaying;

            using (new EditorGUI.DisabledScope(!canPreview))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(SetSprite0Content))
                    HandleSetSprite0();

                GUIContent playStopLabel = _isPreviewPlaying ? PreviewStopContent : PreviewPlayContent;
                if (GUILayout.Button(playStopLabel))
                {
                    if (_isPreviewPlaying) StopPreview();
                    else StartPreview();
                }
            }

            if (multiEdit)
                EditorGUILayout.HelpBox("Preview is not available when editing multiple objects.", MessageType.None);
            else if (!hasSprites)
                EditorGUILayout.HelpBox("Add sprites to enable preview.", MessageType.None);
        }

        private void HandleSetSprite0()
        {
            if (_sprites.arraySize == 0) return;
            var sprite = _sprites.GetArrayElementAtIndex(0).objectReferenceValue as Sprite;
            if (sprite == null) return;
            SetSprite0AsPreview(sprite);
        }

        private void StartPreview()
        {
            if (_sprites.arraySize == 0) return;

            _previewSprites = new Sprite[_sprites.arraySize];
            for (int i = 0; i < _sprites.arraySize; i++)
                _previewSprites[i] = _sprites.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;

            _previewCurrentFrame = 0;
            _previewReversed = false;
            _previewTimer = 0f;
            _previewLastTime = EditorApplication.timeSinceStartup;
            _isPreviewPlaying = true;
            _spriteBeforePreview = GetPreviewSprite();

            EditorApplication.update += OnEditorUpdate;

            if (_previewSprites[0] != null)
                SetPreviewSprite(_previewSprites[0]);
        }

        // Puts back the sprite from before the preview, so previewing never leaves the scene changed
        private void StopPreview()
        {
            if (!_isPreviewPlaying) return;
            _isPreviewPlaying = false;
            EditorApplication.update -= OnEditorUpdate;
            if (target != null) SetPreviewSprite(_spriteBeforePreview);
            _previewSprites = null;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            StopPreview();
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!_isPreviewPlaying || target == null)
            {
                StopPreview();
                Repaint();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - _previewLastTime);
            _previewLastTime = now;

            float frameRate = _frameRate.floatValue * _speedMultiplier.floatValue;
            if (frameRate <= 0f) return;

            _previewTimer += delta;
            float frameDuration = 1f / frameRate;
            if (_previewTimer < frameDuration) return;
            _previewTimer -= frameDuration;

            int spriteCount = _previewSprites.Length;
            if (spriteCount == 0) return;

            var loopMode = (SpriteAnimatorLoopMode)_loopMode.enumValueIndex;

            if (!_previewReversed) _previewCurrentFrame++;
            else _previewCurrentFrame--;

            if (_previewCurrentFrame >= spriteCount || _previewCurrentFrame < 0)
            {
                switch (loopMode)
                {
                    case SpriteAnimatorLoopMode.Once:
                        _previewCurrentFrame = _previewReversed ? 0 : spriteCount - 1;
                        StopPreview();
                        Repaint();
                        return;
                    case SpriteAnimatorLoopMode.Loop:
                        _previewCurrentFrame = _previewReversed ? spriteCount - 1 : 0;
                        break;
                    case SpriteAnimatorLoopMode.PingPong:
                        _previewReversed = !_previewReversed;
                        _previewCurrentFrame = _previewReversed ? spriteCount - 2 : 1;
                        _previewCurrentFrame = Mathf.Clamp(_previewCurrentFrame, 0, spriteCount - 1);
                        break;
                }
            }

            var sprite = _previewSprites[_previewCurrentFrame];
            if (sprite != null)
            {
                SetPreviewSprite(sprite);
                SceneView.RepaintAll();
            }
        }

    }
}
#endif