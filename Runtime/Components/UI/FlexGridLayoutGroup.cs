using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TRnK.Components
{
    /// <summary>Grid layout with a fixed cell size that aligns every row on its own, so a partial last row follows Child Alignment.</summary>
    [AddComponentMenu("TRnK/Flex Grid Layout Group")]
    public sealed class FlexGridLayoutGroup : LayoutGroup
    {
        [SerializeField] private Vector2 _cellSize = new(100f, 100f);
        [SerializeField] private Vector2 _spacing = Vector2.zero;
        [SerializeField, Min(1)] private int _maxPerRow = 3;
        [SerializeField, Tooltip("Shrinks cells and spacing when a row is wider than this rect. The rect's width must be set from outside.")]
        private bool _shrinkToFit = true;

        private readonly List<int> _rowCounts = new();

        public Vector2 CellSize => _cellSize;
        public int MaxPerRow => _maxPerRow;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            RebuildRows();
            float width = padding.horizontal + RowWidth(WidestRow(), _cellSize.x, _spacing.x);
            SetLayoutInputForAxis(width, width, -1f, 0);
        }

        // The width is final by now, so the height uses the shrunk cells.
        public override void CalculateLayoutInputVertical()
        {
            float scale = GetShrinkScale();
            float height = padding.vertical + ColumnHeight(_rowCounts.Count, _cellSize.y * scale, _spacing.y * scale);
            SetLayoutInputForAxis(height, height, -1f, 1);
        }

        public override void SetLayoutHorizontal()
        {
        }

        public override void SetLayoutVertical()
        {
            PlaceCells();
        }

        private void RebuildRows()
        {
            _rowCounts.Clear();
            int perRow = Mathf.Max(1, _maxPerRow);
            int remaining = rectChildren.Count;
            while (remaining > 0)
            {
                int inRow = Mathf.Min(perRow, remaining);
                _rowCounts.Add(inRow);
                remaining -= inRow;
            }
        }

        private void PlaceCells()
        {
            RebuildRows();
            float scale = GetShrinkScale();
            Vector2 cell = _cellSize * scale;
            Vector2 spacing = _spacing * scale;
            float startY = GetStartOffset(1, ColumnHeight(_rowCounts.Count, cell.y, spacing.y));

            int childIndex = 0;
            for (int row = 0; row < _rowCounts.Count; row++)
            {
                int inRow = _rowCounts[row];
                float startX = GetStartOffset(0, RowWidth(inRow, cell.x, spacing.x));
                float y = startY + row * (cell.y + spacing.y);
                for (int column = 0; column < inRow; column++)
                {
                    RectTransform child = rectChildren[childIndex++];
                    SetChildAlongAxis(child, 0, startX + column * (cell.x + spacing.x), cell.x);
                    SetChildAlongAxis(child, 1, y, cell.y);
                }
            }
        }

        private float GetShrinkScale()
        {
            if (!_shrinkToFit) return 1f;

            float needed = RowWidth(WidestRow(), _cellSize.x, _spacing.x);
            float available = rectTransform.rect.width - padding.horizontal;
            if (needed <= 0f || available <= 0f || needed <= available) return 1f;
            return available / needed;
        }

        private int WidestRow()
        {
            int widest = 0;
            for (int i = 0; i < _rowCounts.Count; i++) widest = Mathf.Max(widest, _rowCounts[i]);
            return widest;
        }

        private static float RowWidth(int cells, float cellWidth, float spacing) =>
            cells <= 0 ? 0f : cells * cellWidth + (cells - 1) * spacing;

        private static float ColumnHeight(int rows, float cellHeight, float spacing) =>
            rows <= 0 ? 0f : rows * cellHeight + (rows - 1) * spacing;
    }
}
