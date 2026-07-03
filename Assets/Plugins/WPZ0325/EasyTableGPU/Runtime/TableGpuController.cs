using System.Collections.Generic;
using UnityEngine;

namespace WPZ0325.EasyTableGPU
{
    public class TableGpuController : MonoBehaviour
    {
        [Header("Render Setup")]
        [SerializeField] MonoBehaviour _rendererComponent;
        [SerializeField] Material _material;
        [SerializeField] Camera _renderCamera;
        [SerializeField] float _viewportHeight = 400f;
        [SerializeField] float _viewportWidth  = 600f;
        [SerializeField] float _worldUnitScale = 0.01f;

        [Header("Config")]
        [SerializeField] TableStyleConfig _styleConfig;
        [SerializeField] Font _customFont;

        List<string>       _headers       = new List<string>();
        List<List<string>> _tableData     = new List<List<string>>();
        List<bool>         _toggleStates  = new List<bool>();
        List<string>       _buttonTexts   = new List<string>();

        TableFontHelper  _fontHelper;
        TableMeshBuilder _builder;
        ITableRenderer   _tableRenderer;
        float[]          _effectiveColWidths;

        float _scrollOffsetY;
        float _scrollOffsetX;
        int   _firstVisibleRow;
        int   _visibleRowCount;
        int   _firstVisibleCol;
        int   _visibleColCount;
        bool  _dataDirty = true;

        int _pressedButtonRow = -1;
        int _highlightRow     = -1;
        int _highlightCol     = -1;

        bool _initialized;

        float _lastScrollY = -999f;
        float _lastScrollX = -999f;

        public event System.Action<int, int>  OnCellClicked;
        public event System.Action<int, bool> OnToggleChanged;
        public event System.Action<int>       OnButtonClicked;

        public int RowCount    => _tableData.Count;
        public int ColumnCount => _headers.Count;

        public float FixedColumnWidth =>
            (_styleConfig.IsShowToggleColumn ? _styleConfig.ToggleColumnWidth : 0f)
          + (_styleConfig.IsShowButtonColumn ? _styleConfig.ButtonColumnWidth : 0f);

        void Awake()
        {
            if (_styleConfig == null) { Debug.LogError("TableGpuController: StyleConfig null"); return; }

            _tableRenderer = _rendererComponent as ITableRenderer;
            if (_tableRenderer == null) { Debug.LogError("TableGpuController: Renderer does not implement ITableRenderer"); return; }

            InitFont();
            _builder = new TableMeshBuilder();
            _builder.SetFont(_fontHelper);
            _builder.SetScale(_worldUnitScale);
            _tableRenderer.SetMaterialProperties(_material, _fontHelper.FontTexture);
            _initialized = true;
        }

        void Update()
        {
            if (!_initialized) return;
            UpdateScroll();
        }

        void InitFont()
        {
            Font f = _customFont;
            if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", _styleConfig.TextFontSize);
            if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _fontHelper = new TableFontHelper(f, _styleConfig.TextFontSize);
            _fontHelper.PreCacheAscii();
        }

        void UpdateScroll()
        {
            TableStyleConfig s = _styleConfig;
            if (s == null || _tableData.Count == 0) return;
            if (_effectiveColWidths == null || _effectiveColWidths.Length == 0) return;

            float rh = s.ContentRowHeight;
            if (rh <= 0f) return;

            int newRow = Mathf.Clamp(Mathf.FloorToInt(_scrollOffsetY / rh), 0, _tableData.Count - 1);
            float colAcc = 0f;
            int newCol = 0;
            for (int i = 0; i < _effectiveColWidths.Length; i++)
            {
                if (colAcc + _effectiveColWidths[i] > _scrollOffsetX) { newCol = i; break; }
                colAcc += _effectiveColWidths[i];
                newCol = i + 1;
            }
            newCol = Mathf.Clamp(newCol, 0, _effectiveColWidths.Length - 1);
            float fixedW = FixedColumnWidth;
            float visContentW = Mathf.Max(1f, _viewportWidth - fixedW);
            int visCols = 1;
            float acc = 0f;
            for (int i = newCol; i < _effectiveColWidths.Length; i++)
            {
                acc += _effectiveColWidths[i];
                visCols++;
                if (acc >= visContentW) break;
            }

            bool rowChanged = newRow != _firstVisibleRow;
            bool colChanged = newCol != _firstVisibleCol || _visibleColCount != visCols;

            if (rowChanged || colChanged || _dataDirty)
            {
                _firstVisibleRow = newRow;
                _visibleRowCount = Mathf.CeilToInt(_viewportHeight / rh) + 1;
                _firstVisibleCol = newCol;
                _visibleColCount = visCols;
            }

            bool needRebuild = _dataDirty
                || rowChanged || colChanged
                || Mathf.Abs(_scrollOffsetY - _lastScrollY) > 0.001f
                || Mathf.Abs(_scrollOffsetX - _lastScrollX) > 0.001f;

            if (needRebuild)
            {
                _firstVisibleRow = newRow;
                _firstVisibleCol = newCol;
                RebuildMesh();
                _dataDirty = false;
            }

            _lastScrollY = _scrollOffsetY;
            _lastScrollX = _scrollOffsetX;
        }

        void RebuildMesh()
        {
            TableStyleConfig s = _styleConfig;
            if (s == null || _tableData.Count == 0) return;

            Mesh mesh = _tableRenderer.GetMesh();
            if (mesh == null) return;

            float fineScrollY = _scrollOffsetY - _firstVisibleRow * s.ContentRowHeight;
            float fineScrollX = _scrollOffsetX;
            for (int i = 0; i < _firstVisibleCol && i < _effectiveColWidths.Length; i++)
                fineScrollX -= _effectiveColWidths[i];
            if (fineScrollX < 0f) fineScrollX = 0f;

            _builder.BuildTableMesh(mesh,
                _headers, _tableData,
                _firstVisibleRow, _visibleRowCount,
                _effectiveColWidths,
                s.ContentRowHeight, s.HeaderRowHeight, fineScrollY,
                _firstVisibleCol, _visibleColCount, fineScrollX,
                s,
                s.IsShowToggleColumn, s.ToggleColumnWidth, _toggleStates,
                s.IsShowButtonColumn, s.ButtonColumnWidth, _buttonTexts,
                _pressedButtonRow,
                _highlightRow, _highlightCol);

            _tableRenderer.ApplyMeshData();
        }

        // ============== Public API ==============

        public void SetData(List<string> headers, List<List<string>> rowData)
        {
            if (headers == null || rowData == null) { ClearTable(); return; }
            _headers = new List<string>(headers);
            _tableData = new List<List<string>>(rowData.Count);
            _toggleStates.Clear(); _buttonTexts.Clear();
            foreach (List<string> row in rowData)
            {
                List<string> c = new List<string>(row);
                while (c.Count < headers.Count) c.Add("");
                _tableData.Add(c); _toggleStates.Add(false); _buttonTexts.Add("Btn");
            }
            _scrollOffsetY = 0f; _scrollOffsetX = 0f;
            _firstVisibleRow = 0; _firstVisibleCol = 0;
            _dataDirty = true;
            _effectiveColWidths = CalcEffectiveColWidths();
        }

        public void ClearTable()
        {
            _headers.Clear(); _tableData.Clear(); _toggleStates.Clear(); _buttonTexts.Clear();
            _scrollOffsetY = 0f; _scrollOffsetX = 0f;
            _firstVisibleRow = 0; _firstVisibleCol = 0;
            _pressedButtonRow = -1; _highlightRow = -1; _highlightCol = -1;
            _dataDirty = true;
            _tableRenderer?.GetMesh()?.Clear();
        }

        public void AddRow(List<string> rowData)
        {
            if (_headers.Count == 0) { Debug.LogWarning("AddRow: no headers"); return; }
            List<string> c = new List<string>(rowData);
            while (c.Count < _headers.Count) c.Add("");
            _tableData.Add(c); _toggleStates.Add(false); _buttonTexts.Add("Btn");
            _dataDirty = true;
        }

        public void RemoveRow(int i)
        {
            if (i < 0 || i >= _tableData.Count) return;
            _tableData.RemoveAt(i); _toggleStates.RemoveAt(i); _buttonTexts.RemoveAt(i);
            if (_pressedButtonRow == i) _pressedButtonRow = -1;
            if (_highlightRow == i) { _highlightRow = -1; _highlightCol = -1; }
            _dataDirty = true;
        }

        public void SetCell(int row, int col, string val)
        {
            if (row < 0 || row >= _tableData.Count || col < 0 || col >= _headers.Count) return;
            _tableData[row][col] = val ?? ""; _dataDirty = true;
        }

        public void SetScrollOffsetY(float o) => _scrollOffsetY = Mathf.Max(0f, o);
        public void SetScrollNormalizedY(float n)
        {
            TableStyleConfig s = _styleConfig; if (s == null) return;
            float totalH = _tableData.Count * s.ContentRowHeight;
            _scrollOffsetY = Mathf.Max(0f, n * Mathf.Max(0f, totalH - _viewportHeight));
        }
        public float GetScrollOffsetY() => _scrollOffsetY;

        public void SetScrollOffsetX(float o) => _scrollOffsetX = Mathf.Max(0f, o);
        public void SetScrollNormalizedX(float n)
        {
            float totalW = 0f;
            if (_effectiveColWidths != null)
                for (int i = 0; i < _effectiveColWidths.Length; i++) totalW += _effectiveColWidths[i];
            float visW = Mathf.Max(1f, _viewportWidth - FixedColumnWidth);
            _scrollOffsetX = Mathf.Max(0f, n * Mathf.Max(0f, totalW - visW));
        }
        public float GetScrollOffsetX() => _scrollOffsetX;

        public int GetFirstVisibleRow() => _firstVisibleRow;

        public void SetToggleState(int row, bool on)
        {
            if (row < 0 || row >= _toggleStates.Count || _toggleStates[row] == on) return;
            _toggleStates[row] = on; _dataDirty = true;
            OnToggleChanged?.Invoke(row, on);
        }

        public bool GetToggleState(int r) => r >= 0 && r < _toggleStates.Count && _toggleStates[r];

        public void SetButtonText(int row, string t)
        {
            if (row < 0 || row >= _buttonTexts.Count) return;
            _buttonTexts[row] = t ?? ""; _dataDirty = true;
        }

        public void SetButtonPressed(int row)
        {
            if (row == _pressedButtonRow) return;
            _pressedButtonRow = row; _dataDirty = true;
        }

        public void SetButtonReleased()
        {
            if (_pressedButtonRow < 0) return;
            _pressedButtonRow = -1; _dataDirty = true;
        }

        public void SetHighlight(int row, int col)
        {
            if (row == _highlightRow && col == _highlightCol) return;
            _highlightRow = row; _highlightCol = col; _dataDirty = true;
        }

        public void ClearHighlight()
        {
            if (_highlightRow < 0 && _highlightCol < 0) return;
            _highlightRow = -1; _highlightCol = -1; _dataDirty = true;
        }

        float[] CalcEffectiveColWidths()
        {
            int n = _headers.Count;
            if (n == 0) return new float[0];
            TableStyleConfig s = _styleConfig;
            float[] w = new float[n];
            float used = 0f; int unspec = 0;
            float[] manual = s.ColumnWidths;
            for (int i = 0; i < n; i++)
            {
                if (manual != null && i < manual.Length && manual[i] > 0f)
                    { w[i] = manual[i]; used += manual[i]; }
                else unspec++;
            }
            float remain = Mathf.Max(0f, s.TableTotalWidth - used);
            float def = unspec > 0 ? remain / unspec : 0f;
            for (int i = 0; i < n; i++)
                if (w[i] <= 0f) w[i] = Mathf.Max(def, 20f);
            return w;
        }

        // ============== Hit Testing ==============

        int RowFromLocalY(float localY)
        {
            TableStyleConfig s = _styleConfig;
            if (s == null) return -1;
            float baseY = -s.HeaderRowHeight + (_scrollOffsetY - _firstVisibleRow * s.ContentRowHeight);
            float fi = (baseY - localY) / s.ContentRowHeight;
            if (fi < 0f) return -1;
            int r = _firstVisibleRow + Mathf.FloorToInt(fi);
            return (r >= 0 && r < _tableData.Count) ? r : -1;
        }

        int ColFromLocalX(float localX)
        {
            if (_effectiveColWidths == null) return -1;
            float xd = localX - FixedColumnWidth + _scrollOffsetX;
            float acc = 0f;
            for (int i = 0; i < _effectiveColWidths.Length; i++)
            {
                if (xd >= acc && xd < acc + _effectiveColWidths[i]) return i;
                acc += _effectiveColWidths[i];
            }
            return -1;
        }

        public bool HitTest(Vector2 screen, out int row, out int col)
        {
            row = -1; col = -1;
            if (_styleConfig == null || _tableData.Count == 0 || _effectiveColWidths == null) return false;

            Vector2 local = _tableRenderer.ScreenToTablePoint(screen, _renderCamera);
            if (local.x < 0f && local.y < 0f) return false;

            int r = RowFromLocalY(local.y);
            if (r < 0) return false;

            float fixedW = FixedColumnWidth;
            if (local.x < fixedW) return false;

            int c = ColFromLocalX(local.x);
            if (c < 0) return false;

            row = r; col = c;
            return true;
        }

        public bool HitTestToggleCol(Vector2 screen, out int row)
        {
            row = -1;
            TableStyleConfig s = _styleConfig;
            if (s == null || !s.IsShowToggleColumn || _toggleStates.Count == 0) return false;

            Vector2 local = _tableRenderer.ScreenToTablePoint(screen, _renderCamera);
            if (local.x < 0f && local.y < 0f) return false;

            float tw = s.ToggleColumnWidth;
            if (local.x < 0f || local.x > tw) return false;
            int r = RowFromLocalY(local.y);
            if (r < 0) return false;
            row = r;
            return true;
        }

        public bool HitTestButtonCol(Vector2 screen, out int row)
        {
            row = -1;
            TableStyleConfig s = _styleConfig;
            if (s == null || !s.IsShowButtonColumn || _buttonTexts.Count == 0) return false;

            Vector2 local = _tableRenderer.ScreenToTablePoint(screen, _renderCamera);
            if (local.x < 0f && local.y < 0f) return false;

            float togW = s.IsShowToggleColumn ? s.ToggleColumnWidth : 0f;
            float bw = s.ButtonColumnWidth;
            if (local.x < togW || local.x > togW + bw) return false;
            int r = RowFromLocalY(local.y);
            if (r < 0) return false;
            row = r;
            return true;
        }
    }
}
