using UnityEngine;
using UnityEngine.UI;

namespace TRnK.Extensions
{
    public static class ScrollRectExtensions
    {
        private static readonly Vector3[] s_corners = new Vector3[4];

        /// <summary>Returns the verticalNormalizedPosition that puts the target in the middle of the viewport.</summary>
        public static float GetVerticalPositionToCenter(this ScrollRect scrollRect, RectTransform target, bool rebuildLayout = false)
        {
            Vector2 center = TargetCenter(scrollRect, target, rebuildLayout, out Vector2 view);
            Rect content = scrollRect.content.rect;
            return PositionToCenter(center.y, content.yMin, content.height, view.y, scrollRect.verticalNormalizedPosition);
        }

        /// <summary>Returns the horizontalNormalizedPosition that puts the target in the middle of the viewport.</summary>
        public static float GetHorizontalPositionToCenter(this ScrollRect scrollRect, RectTransform target, bool rebuildLayout = false)
        {
            Vector2 center = TargetCenter(scrollRect, target, rebuildLayout, out Vector2 view);
            Rect content = scrollRect.content.rect;
            return PositionToCenter(center.x, content.xMin, content.width, view.x, scrollRect.horizontalNormalizedPosition);
        }

        // The target's center and the viewport's size, both in content space
        private static Vector2 TargetCenter(ScrollRect scrollRect, RectTransform target, bool rebuildLayout, out Vector2 viewSize)
        {
            RectTransform content = scrollRect.content;
            if (rebuildLayout) LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            // Viewport corners only: RectTransformUtility's bounds also count the content inside it
            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
            viewport.GetWorldCorners(s_corners);
            Vector2 min = content.InverseTransformPoint(s_corners[0]);
            Vector2 max = content.InverseTransformPoint(s_corners[2]);
            viewSize = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));

            return content.InverseTransformPoint(target.TransformPoint(target.rect.center));
        }

        private static float PositionToCenter(float targetCenter, float contentMin, float contentSize, float viewSize, float current)
        {
            float scrollable = contentSize - viewSize;
            return scrollable > 0f ? Mathf.Clamp01((targetCenter - viewSize * 0.5f - contentMin) / scrollable) : current;
        }
    }
}
