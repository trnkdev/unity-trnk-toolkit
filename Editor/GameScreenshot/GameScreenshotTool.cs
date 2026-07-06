#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TRnK.Toolkit
{
    internal class GameScreenshotTool : EditorWindow
    {
        private const int MinSupersize = 1;
        private const int MaxSupersize = 4; // 1080p x4 = 4K, 4K x4 = 8K
        private const int DefaultCameraWidth = 1920;
        private const int DefaultCameraHeight = 1080;
        private const int FallbackWidth = 1920;
        private const int FallbackHeight = 1080;
        private const string ScreenshotsFolderName = "Screenshots";
        private const string LogPrefix = "[GameScreenshotTool]";
        private const string MsgNeedPlayMode = "Screenshot capture requires Play mode.";

        // Supersize HelpBox string: rebuilt only when supersize or Game View size changes.
        private string _supersizeLabel;
        private int _supersizeLabelForSize;
        private Vector2 _supersizeLabelForGV;

        private GameScreenshotSettings _settings;
        private Camera _targetCamera;
        // Width/Height for SpecificCamera mode — persisted via EditorWindow serialization, not in SO.
        [SerializeField] private int _cameraWidth = DefaultCameraWidth;
        [SerializeField] private int _cameraHeight = DefaultCameraHeight;

        [MenuItem("Tools/TRnK/Screenshot/Quick Capture %#k")]
        private static void QuickCapture()
        {
            if (!EditorApplication.isPlaying)
            {
                ShowToast(MsgNeedPlayMode);
                return;
            }

            var settings = GameScreenshotSettings.GetOrCreate();
            var folder = EnsureScreenshotsFolder();
            var gv = GetGameViewSize();
            var outW = Mathf.Max(1, (int)gv.x) * settings.supersize;
            var outH = Mathf.Max(1, (int)gv.y) * settings.supersize;
            var filePath = Path.Combine(folder, BuildFileName(outW, outH));

            Directory.CreateDirectory(folder);
            ScreenshotCoroutineHelper.Begin(filePath, settings.supersize, settings.revealOnSave);
        }

        [MenuItem("Tools/TRnK/Screenshot/Open Settings")]
        private static void OpenWindow()
        {
            var win = GetWindow<GameScreenshotTool>("Game Screenshot");
            win.minSize = new Vector2(360, 260);
            win.Show();
        }

        private void OnEnable()
        {
            _settings = GameScreenshotSettings.GetOrCreate();

            if (string.IsNullOrEmpty(_settings.saveFolder))
            {
                _settings.saveFolder = EnsureScreenshotsFolder();
                EditorUtility.SetDirty(_settings);
                EditorApplication.delayCall += () => AssetDatabase.SaveAssets();
            }
        }

        private void OnGUI()
        {
            if (_settings == null) _settings = GameScreenshotSettings.GetOrCreate();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Capture", EditorStyles.boldLabel);
            _settings.captureMode = (CaptureMode)EditorGUILayout.EnumPopup("Mode", _settings.captureMode);

            using (new EditorGUI.DisabledScope(_settings.captureMode != CaptureMode.SpecificCamera))
            {
                _targetCamera = (Camera)EditorGUILayout.ObjectField("Camera", _targetCamera, typeof(Camera), true);
            }

            EditorGUILayout.Space();

            if (_settings.captureMode == CaptureMode.GameView)
            {
                _settings.supersize = EditorGUILayout.IntSlider("Supersize", _settings.supersize, MinSupersize, MaxSupersize);
                DrawSupersizeInfo(_settings.supersize);
            }
            else
            {
                _cameraWidth = Mathf.Max(16, EditorGUILayout.IntField("Width", _cameraWidth));
                _cameraHeight = Mathf.Max(16, EditorGUILayout.IntField("Height", _cameraHeight));
                EditorGUILayout.HelpBox("Offscreen camera render — Screen Space Overlay UI will NOT appear.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField("Folder", _settings.saveFolder);
                if (GUILayout.Button("Choose...", GUILayout.Width(90)))
                {
                    var chosen = EditorUtility.OpenFolderPanel("Choose Screenshot Folder", _settings.saveFolder, "");
                    if (!string.IsNullOrEmpty(chosen))
                        _settings.saveFolder = chosen;
                }
            }

            _settings.revealOnSave = EditorGUILayout.ToggleLeft("Reveal in Explorer after save", _settings.revealOnSave);

            EditorGUILayout.Space();
            if (GUILayout.Button("Reset Folder"))
                _settings.saveFolder = EnsureScreenshotsFolder();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_settings);
                if (GUIUtility.hotControl == 0)
                    EditorApplication.delayCall += () => AssetDatabase.SaveAssets();
            }

            GUILayout.FlexibleSpace();

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox(MsgNeedPlayMode, MessageType.Warning);

            using (new EditorGUI.DisabledScope(!CanCapture()))
            {
                if (GUILayout.Button("Capture Screenshot", GUILayout.Height(32)))
                    CaptureFromUI();
            }
        }

        private void DrawSupersizeInfo(int supersize)
        {
            var gv = GetGameViewSize();
            if (_supersizeLabel == null || _supersizeLabelForSize != supersize || _supersizeLabelForGV != gv)
            {
                _supersizeLabelForSize = supersize;
                _supersizeLabelForGV = gv;
                var gw = Mathf.Max(1, (int)gv.x);
                var gh = Mathf.Max(1, (int)gv.y);
                var ow = gw * supersize;
                var oh = gh * supersize;
                _supersizeLabel = $"Output: {ow} x {oh}  ({ResolutionLabel(ow, oh)})\nSupersize multiplies the Game View resolution.";
            }
            EditorGUILayout.HelpBox(_supersizeLabel, MessageType.Info);
        }

        private static string ResolutionLabel(int w, int h)
        {
            long px = (long)w * h;
            if (px >= 7680L * 4320) return "8K UHD";
            if (px >= 3840L * 2160) return "4K UHD";
            if (px >= 2560L * 1440) return "QHD / 1440p";
            if (px >= 1920L * 1080) return "FHD / 1080p";
            if (px >= 1280L * 720) return "HD / 720p";
            return "< HD";
        }

        private static bool s_reflectionWarned;

        // Screen.width/height inside an EditorWindow returns the window's own pixel size, not the Game View's.
        // Unity 6.0 removed GetSizeOfMainGameView (static); use GetPlayModeViewSize on the main GameView instance instead.
        private static Vector2 GetGameViewSize()
        {
            try
            {
                var t = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                if (t == null) return new Vector2(FallbackWidth, FallbackHeight);
#if UNITY_6000_0_OR_NEWER
                var instances = Resources.FindObjectsOfTypeAll(t);
                if (instances != null && instances.Length > 0)
                {
                    var m = t.GetMethod("GetPlayModeViewSize", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (m != null) return (Vector2)m.Invoke(instances[0], null);
                }
#else
                var m = t.GetMethod("GetSizeOfMainGameView", BindingFlags.NonPublic | BindingFlags.Static);
                if (m != null) return (Vector2)m.Invoke(null, null);
#endif
            }
            catch (Exception ex)
            {
                if (!s_reflectionWarned)
                {
                    s_reflectionWarned = true;
                    Debug.LogWarning($"{LogPrefix} Failed to query Game View size via reflection; using fallback {FallbackWidth}×{FallbackHeight}. {ex.Message}");
                }
            }
            return new Vector2(FallbackWidth, FallbackHeight);
        }

        private bool CanCapture()
        {
            if (_settings == null) return false;
            if (!EditorApplication.isPlaying) return false;
            if (string.IsNullOrEmpty(_settings.saveFolder)) return false;
            if (_settings.captureMode == CaptureMode.SpecificCamera && _targetCamera == null) return false;
            return true;
        }

        private void CaptureFromUI()
        {
            var directory = _settings.saveFolder;
            if (string.IsNullOrEmpty(directory)) directory = EnsureScreenshotsFolder();
            Directory.CreateDirectory(directory);

            if (_settings.captureMode == CaptureMode.GameView)
            {
                var gv = GetGameViewSize();
                var outW = Mathf.Max(1, (int)gv.x) * _settings.supersize;
                var outH = Mathf.Max(1, (int)gv.y) * _settings.supersize;
                var finalPath = Path.Combine(directory, BuildFileName(outW, outH));

                ScreenshotCoroutineHelper.Begin(finalPath, _settings.supersize, _settings.revealOnSave);
            }
            else
            {
                var finalPath = Path.Combine(directory, BuildFileName(_cameraWidth, _cameraHeight));
                CaptureFromCameraToPath(_targetCamera, _cameraWidth, _cameraHeight, finalPath);
                AssetDatabase.Refresh();
                if (_settings.revealOnSave) RevealInExplorerIfExists(finalPath);
            }
        }

        private static void CaptureFromCameraToPath(Camera camera, int width, int height, string path)
        {
            if (camera == null)
            {
                Debug.LogError($"{LogPrefix} No camera assigned for camera capture.");
                return;
            }

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var prevRT = camera.targetTexture;
            var prevActive = RenderTexture.active;

            try
            {
                camera.targetTexture = rt;
                camera.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                var png = tex.EncodeToPNG();
                File.WriteAllBytes(path, png);
                DestroyImmediate(tex);

                Debug.Log($"{LogPrefix} Saved camera capture to: {path}");
            }
            finally
            {
                camera.targetTexture = prevRT;
                RenderTexture.active = prevActive;
                rt.Release();
                DestroyImmediate(rt);
            }
        }

        private static string EnsureScreenshotsFolder()
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var folder = Path.Combine(root, ScreenshotsFolderName);
            Directory.CreateDirectory(folder);
            return folder;
        }

        // Output resolution baked into the filename so each capture is self-documenting.
        private static string BuildFileName(int outputW, int outputH)
        {
            var scene = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(scene)) scene = "Untitled";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"{scene}_{outputW}x{outputH}_{timestamp}.png";
        }

        // Shows a docked notification if the window is open, otherwise logs.
        private static void ShowToast(string message)
        {
            var wins = Resources.FindObjectsOfTypeAll<GameScreenshotTool>();
            if (wins.Length > 0)
                wins[0].ShowNotification(new GUIContent(message));
            else
                Debug.LogWarning($"{LogPrefix} {message}");
        }

        private static void RevealInExplorerIfExists(string path)
        {
            if (File.Exists(path))
                EditorUtility.RevealInFinder(path);
        }

        // Spawns a temporary MonoBehaviour to run a WaitForEndOfFrame coroutine.
        // This ensures all cameras (including stacked/background cameras) have finished
        // rendering before capture, and lets us write the file synchronously so
        // RevealInFinder triggers reliably on the first capture.
        private sealed class ScreenshotCoroutineHelper : MonoBehaviour
        {
            internal static void Begin(string filePath, int supersize, bool revealOnSave)
            {
                var go = new GameObject("[ScreenshotHelper]") { hideFlags = HideFlags.HideAndDontSave };
                go.AddComponent<ScreenshotCoroutineHelper>()
                  .StartCoroutine(Capture(go, filePath, supersize, revealOnSave));
            }

            private static IEnumerator Capture(GameObject go, string filePath, int supersize, bool revealOnSave)
            {
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture(supersize);
                try
                {
                    File.WriteAllBytes(filePath, tex.EncodeToPNG());
                    Debug.Log($"{LogPrefix} Captured → {filePath}");
                    AssetDatabase.Refresh();
                    if (revealOnSave)
                        EditorUtility.RevealInFinder(filePath);
                }
                finally
                {
                    DestroyImmediate(tex);
                    DestroyImmediate(go);
                }
            }
        }
    }
}
#endif
