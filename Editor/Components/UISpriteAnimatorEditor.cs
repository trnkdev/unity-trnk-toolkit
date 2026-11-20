#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TRnK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UISpriteAnimator))]
    internal class UISpriteAnimatorEditor : SpriteAnimatorEditorBase
    {
        private SerializedProperty _canvasGroups;

        protected override void OnEnable()
        {
            base.OnEnable();
            _canvasGroups = serializedObject.FindProperty("_canvasGroups");
        }

        protected override bool HasAdditionalProperties() => true;

        protected override void DrawAdditionalProperties()
        {
#if ODIN_INSPECTOR
            DrawOdinUnityProperty("_canvasGroups");
#else
            EditorGUILayout.PropertyField(_canvasGroups);
#endif

            if (_canvasGroups == null || _canvasGroups.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No Canvas Groups assigned. Visibility checks will use only the Image component (enabled + alpha).", MessageType.Info);
            }
        }

        protected override void SetSprite0AsPreview(Sprite sprite)
        {
            var img = ((UISpriteAnimator)target).GetComponent<Image>();
            if (img == null) return;
            Undo.RecordObject(img, "Set Sprite[0] Preview");
            img.sprite = sprite;
            EditorUtility.SetDirty(img);
        }

        protected override Sprite GetPreviewSprite() => ((UISpriteAnimator)target).GetComponent<Image>().sprite;

        protected override void SetPreviewSprite(Sprite sprite)
        {
            var img = ((UISpriteAnimator)target).GetComponent<Image>();
            if (img != null) img.sprite = sprite;
        }
    }
}
#endif