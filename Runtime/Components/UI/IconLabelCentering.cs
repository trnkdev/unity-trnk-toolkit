using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using TRnK.Logger;
using UnityEditor;
using UnityEngine.UI;
#endif

namespace TRnK.Components
{
    /// <summary>Centres an icon and the visible text of a label together inside this rect, re-aligning whenever the text changes.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("TRnK/Icon Label Centering")]
    public sealed class IconLabelCentering : MonoBehaviour
    {
        private const float WidthTolerance = 0.01f;

        [SerializeField] private RectTransform _icon;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _spacing = 6f;
        [SerializeField, Min(0f), Tooltip("Space kept free on each side. The label is sized to what's left; its Auto Size, Ellipsis or Truncate handle longer text.")]
        private float _padding;
        [SerializeField] private bool _iconOnRight;

        private Action<Object> _onTextChanged;
        private RectTransform _selfRect;

        private RectTransform SelfRect => _selfRect != null ? _selfRect : _selfRect = (RectTransform)transform;

        private bool CanAlign => isActiveAndEnabled && _icon != null && _label != null;

        private bool OwnsChildren => _icon.parent == transform && _label.transform.parent == transform;

        private void OnEnable()
        {
            _onTextChanged ??= OnTextChanged;
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(_onTextChanged);
            Align();
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(_onTextChanged);
        }

        private void OnRectTransformDimensionsChange()
        {
            Align();
        }

        /// <summary>Sizes the label and re-centres the pair. Call it after showing, hiding or resizing the icon.</summary>
        public void Align()
        {
            if (!CanAlign || !OwnsChildren) return;

            bool hasIcon = _icon.gameObject.activeSelf;
            float iconWidth = hasIcon ? _icon.rect.width : 0f;
            FitLabel(hasIcon ? iconWidth + _spacing : 0f);

            bool hasText = TryGetTextExtents(out float textMin, out float textMax);
            float textWidth = hasText ? textMax - textMin : 0f;
            float gap = hasIcon && hasText ? _spacing : 0f;

            float start = SelfRect.rect.center.x - (iconWidth + gap + textWidth) * 0.5f;
            float iconStart = _iconOnRight ? start + textWidth + gap : start;
            float labelStart = _iconOnRight ? start : start + iconWidth + gap;

            if (hasIcon) SetLocalX(_icon, iconStart - _icon.rect.xMin);
            if (hasText) SetLocalX(_label.rectTransform, labelStart - textMin);
        }

        // The width doesn't depend on the text, so a text change never resizes the label.
        private void FitLabel(float iconSpace)
        {
            float width = Mathf.Max(0f, SelfRect.rect.width - 2f * _padding - iconSpace);
            RectTransform label = _label.rectTransform;
            if (Mathf.Abs(label.rect.width - width) < WidthTolerance) return;

            label.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            // TMP ignores a resize made during a canvas rebuild, so regenerate now; this re-enters Align once through TEXT_CHANGED.
            _label.ForceMeshUpdate();
        }

        private void OnTextChanged(Object changed)
        {
            if (changed == _label) Align();
        }

        private bool TryGetTextExtents(out float min, out float max)
        {
            min = 0f;
            max = 0f;
            if (!_label.isActiveAndEnabled || _label.textInfo == null || _label.textInfo.characterCount == 0) return false;

            Bounds bounds = _label.textBounds;
            if (bounds.size.x <= 0f) return false;

            min = bounds.min.x;
            max = bounds.max.x;
            return true;
        }

        private static void SetLocalX(Transform target, float x)
        {
            Vector3 position = target.localPosition;
            if (Mathf.Approximately(position.x, x)) return;

            position.x = x;
            target.localPosition = position;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EditorApplication.delayCall -= AlignAfterValidate;
            EditorApplication.delayCall += AlignAfterValidate;
        }

        private void AlignAfterValidate()
        {
            if (this == null) return;

            if (_icon != null && _label != null)
            {
                if (!OwnsChildren)
                    Log.Warn($"[IconLabelCentering] '{name}': icon and label must be direct children, nothing will be aligned.", this);
                if (TryGetComponent(out LayoutGroup _))
                    Log.Warn($"[IconLabelCentering] '{name}': a layout group on the same object also moves the icon and label; remove one of them.", this);
                if (_label.TryGetComponent(out ContentSizeFitter fitter) && fitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained)
                    Log.Warn($"[IconLabelCentering] '{name}': the label's Content Size Fitter fights the label width this component sets; set its Horizontal Fit to Unconstrained.", this);
            }

            Align();
        }
#endif
    }
}
