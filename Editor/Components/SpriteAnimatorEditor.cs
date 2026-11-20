#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TRnK.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpriteAnimator))]
    internal class SpriteAnimatorEditor : SpriteAnimatorEditorBase
    {
        protected override void SetSprite0AsPreview(Sprite sprite)
        {
            var sr = ((SpriteAnimator)target).GetComponent<SpriteRenderer>();
            if (sr == null) return;
            Undo.RecordObject(sr, "Set Sprite[0] Preview");
            sr.sprite = sprite;
            EditorUtility.SetDirty(sr);
        }

        protected override Sprite GetPreviewSprite() => ((SpriteAnimator)target).GetComponent<SpriteRenderer>().sprite;

        protected override void SetPreviewSprite(Sprite sprite)
        {
            var sr = ((SpriteAnimator)target).GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = sprite;
        }
    }
}
#endif