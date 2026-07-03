using System.Collections.Generic;
using UnityEngine;
using WPZ0325.EasyTableGPU;

public class TableGpuDemo3D : MonoBehaviour
{
    [SerializeField] TableGpuController _controller;

    [SerializeField] KeyCode _clearKey  = KeyCode.A;
    [SerializeField] KeyCode _genKey    = KeyCode.S;
    [SerializeField] KeyCode _addRowKey = KeyCode.Z;
    [SerializeField] KeyCode _delRowKey = KeyCode.X;
    [SerializeField] KeyCode _toggleKey = KeyCode.T;

    [SerializeField] int _defaultRows = 100;
    [SerializeField] int _defaultCols = 10;

    float _scrollPosY;
    float _scrollPosX;
    bool  _subscribed;

    int _lastHoverRow = -1;
    int _lastHoverCol = -1;

    void Start()
    {
        if (_controller != null)
        {
            _controller.OnCellClicked   += OnCellClicked;
            _controller.OnToggleChanged += OnToggleChanged;
            _controller.OnButtonClicked += OnButtonClicked;
            _subscribed = true;
        }
    }

    void OnDestroy()
    {
        if (_controller != null && _subscribed)
        {
            _controller.OnCellClicked   -= OnCellClicked;
            _controller.OnToggleChanged -= OnToggleChanged;
            _controller.OnButtonClicked -= OnButtonClicked;
        }
    }

    void Update()
    {
        if (_controller == null) return;

        if (Input.GetKeyDown(_clearKey))  { _controller.ClearTable(); ResetScroll(); }
        if (Input.GetKeyDown(_genKey))    { Generate(_defaultRows, _defaultCols); ResetScroll(); }
        if (Input.GetKeyDown(_addRowKey) && _controller.RowCount > 0)
            _controller.AddRow(GenerateRow(_controller.ColumnCount));
        if (Input.GetKeyDown(_delRowKey) && _controller.RowCount > 0)
            _controller.RemoveRow(_controller.RowCount - 1);
        if (Input.GetKeyDown(_toggleKey) && _controller.RowCount > 0)
        {
            int r = _controller.GetFirstVisibleRow();
            _controller.SetToggleState(r, !_controller.GetToggleState(r));
        }

        float scrollY = Input.mouseScrollDelta.y * 30f;
        if (Mathf.Abs(scrollY) > 0.01f)
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                _scrollPosX = Mathf.Max(0f, _scrollPosX - scrollY);
                _controller.SetScrollOffsetX(_scrollPosX);
            }
            else
            {
                _scrollPosY = Mathf.Max(0f, _scrollPosY - scrollY);
                _controller.SetScrollOffsetY(_scrollPosY);
            }
        }

        UpdateHighlight();

        if (Input.GetMouseButtonDown(0))
            TryClick();

        if (Input.GetMouseButtonUp(0))
            _controller.SetButtonReleased();
    }

    void UpdateHighlight()
    {
        Vector2 mp = Input.mousePosition;

        if (_controller.HitTest(mp, out int row, out int col))
        {
            if (row != _lastHoverRow || col != _lastHoverCol)
            {
                _lastHoverRow = row;
                _lastHoverCol = col;
                _controller.SetHighlight(row, col);
            }
            return;
        }

        if (_controller.HitTestToggleCol(mp, out int tr))
        {
            if (tr != _lastHoverRow || _lastHoverCol != -2)
            {
                _lastHoverRow = tr;
                _lastHoverCol = -2;
                _controller.SetHighlight(tr, -1);
            }
            return;
        }

        if (_controller.HitTestButtonCol(mp, out int br))
        {
            if (br != _lastHoverRow || _lastHoverCol != -3)
            {
                _lastHoverRow = br;
                _lastHoverCol = -3;
                _controller.SetHighlight(br, -1);
            }
            return;
        }

        if (_lastHoverRow >= 0)
        {
            _lastHoverRow = -1;
            _lastHoverCol = -1;
            _controller.ClearHighlight();
        }
    }

    void TryClick()
    {
        Vector2 mp = Input.mousePosition;

        if (_controller.HitTestToggleCol(mp, out int tr))
        {
            _controller.SetToggleState(tr, !_controller.GetToggleState(tr));
            return;
        }

        if (_controller.HitTestButtonCol(mp, out int br))
        {
            _controller.SetButtonPressed(br);
            Debug.Log($"[TableGpuDemo3D] Button pressed: row={br}");
            return;
        }

        if (_controller.HitTest(mp, out int row, out int col))
        {
            Debug.Log($"[TableGpuDemo3D] Cell clicked: ({row},{col})");
        }
    }

    void ResetScroll() { _scrollPosY = 0f; _scrollPosX = 0f; }

    void OnCellClicked(int row, int col)   { Debug.Log($"[TableGpuDemo3D] Cell clicked: ({row},{col})"); }
    void OnToggleChanged(int row, bool on) { Debug.Log($"[TableGpuDemo3D] Toggle row={row} -> {on}"); }
    void OnButtonClicked(int row)          { Debug.Log($"[TableGpuDemo3D] Button clicked: row={row}"); }

    void Generate(int rows, int cols)
    {
        List<string> headers = new List<string>();
        for (int c = 0; c < cols; c++) headers.Add($"Header-{c}");
        List<List<string>> data = new List<List<string>>();
        for (int r = 0; r < rows; r++)
        {
            List<string> row = new List<string>();
            for (int c = 0; c < cols; c++)
                row.Add($"({r},{c})");
            data.Add(row);
        }
        _controller.SetData(headers, data);
        Debug.Log($"[TableGpuDemo3D] {rows} rows x {cols} cols");
    }

    List<string> GenerateRow(int cols)
    {
        int r = _controller.RowCount;
        List<string> row = new List<string>();
        for (int c = 0; c < cols; c++) row.Add($"({r},{c})");
        return row;
    }
}
