using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ReinoOscuridad.UI.Common
{
    public class PooledGridView : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private int _bufferRows = 1;

        private int _columns = 1;
        private Vector2 _cellSize = new Vector2(100f, 100f);
        private Vector2 _spacing = Vector2.zero;
        private int _itemCount;
        private Action<GameObject, int> _bindItem;

        private readonly Dictionary<int, RectTransform> _activeCells = new Dictionary<int, RectTransform>();
        private readonly Stack<RectTransform> _pool = new Stack<RectTransform>();
        private readonly List<int> _toRelease = new List<int>();

        public void Init(GameObject cellPrefab, RectTransform content, RectTransform viewport,
            ScrollRect scrollRect, int columns, Vector2 cellSize, Vector2 spacing,
            Action<GameObject, int> bindItem, int itemCount)
        {
            _cellPrefab = cellPrefab;
            _content    = content;
            _viewport   = viewport;
            _scrollRect = scrollRect;
            _columns    = Mathf.Max(1, columns);
            _cellSize   = cellSize;
            _spacing    = spacing;
            _bindItem   = bindItem;

            _scrollRect.onValueChanged.AddListener(OnScrollChanged);
            SetItemCount(itemCount);
        }

        public void SetItemCount(int itemCount)
        {
            foreach (var cell in _activeCells.Values)
            {
                cell.gameObject.SetActive(false);
                _pool.Push(cell);
            }
            _activeCells.Clear();

            _itemCount = itemCount;
            int rows = Mathf.CeilToInt((float)_itemCount / _columns);
            float totalHeight = rows * _cellSize.y + Mathf.Max(0, rows - 1) * _spacing.y;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalHeight);

            Refresh();
        }

        public static (int firstRow, int lastRow) VisibleRowRange(float scrollY, float viewportHeight,
            float cellHeight, float spacingY, int totalRows, int bufferRows)
        {
            if (totalRows <= 0 || cellHeight <= 0f) return (0, -1);

            float rowStride = cellHeight + spacingY;
            int firstVisible = Mathf.Max(0, Mathf.FloorToInt(scrollY / rowStride) - bufferRows);
            int rowsInView = Mathf.CeilToInt(viewportHeight / rowStride) + bufferRows * 2;
            int lastVisible = Mathf.Min(totalRows - 1, firstVisible + rowsInView);

            return (firstVisible, lastVisible);
        }

        private void OnScrollChanged(Vector2 _) => Refresh();

        private void Refresh()
        {
            if (_itemCount <= 0 || _content == null || _viewport == null) return;

            float scrollableHeight = Mathf.Max(0f, _content.sizeDelta.y - _viewport.rect.height);
            float scrollY = (1f - _scrollRect.verticalNormalizedPosition) * scrollableHeight;

            int totalRows = Mathf.CeilToInt((float)_itemCount / _columns);
            var (firstRow, lastRow) = VisibleRowRange(scrollY, _viewport.rect.height, _cellSize.y, _spacing.y, totalRows, _bufferRows);

            int firstIndex = firstRow * _columns;
            int lastIndex  = Mathf.Min(_itemCount - 1, (lastRow + 1) * _columns - 1);

            _toRelease.Clear();
            foreach (var kv in _activeCells)
                if (kv.Key < firstIndex || kv.Key > lastIndex) _toRelease.Add(kv.Key);

            foreach (var index in _toRelease)
            {
                _activeCells[index].gameObject.SetActive(false);
                _pool.Push(_activeCells[index]);
                _activeCells.Remove(index);
            }

            for (int i = firstIndex; i <= lastIndex; i++)
            {
                if (_activeCells.ContainsKey(i)) continue;

                var cell = _pool.Count > 0
                    ? _pool.Pop()
                    : Instantiate(_cellPrefab, _content).GetComponent<RectTransform>();

                cell.gameObject.SetActive(true);
                PositionCell(cell, i);
                _bindItem?.Invoke(cell.gameObject, i);
                _activeCells[i] = cell;
            }
        }

        private void PositionCell(RectTransform cell, int index)
        {
            int row = index / _columns;
            int col = index % _columns;

            cell.anchorMin = cell.anchorMax = new Vector2(0f, 1f);
            cell.pivot = new Vector2(0f, 1f);
            cell.anchoredPosition = new Vector2(col * (_cellSize.x + _spacing.x), -row * (_cellSize.y + _spacing.y));
            cell.sizeDelta = _cellSize;
        }

        private void OnDestroy()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        }
    }
}
