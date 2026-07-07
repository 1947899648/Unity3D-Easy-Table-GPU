using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace WPZ0325.EasyTableGPU
{
    /// <summary>
    /// EasyTableGPU 核心控制器。管理表格数据、虚拟视口滚动、Mesh 重建调度、
    /// 点击测试和交互状态，是整个插件的入口点。
    /// </summary>
    public class TableGpuController : MonoBehaviour
    {
        #region 序列化字段

        [Header("Render Setup")]
        [SerializeField] MonoBehaviour _rendererComponent;
        [SerializeField] Material _material;
        [SerializeField] Camera _renderCamera;
        [SerializeField] RenderLayer _renderLayer = RenderLayer.Normal;
        [SerializeField] float _viewportHeight = 400f;
        [SerializeField] float _viewportWidth  = 600f;
        [SerializeField] float _worldUnitScale = 0.01f;

        [Header("Config")]
        [SerializeField] TableStyleConfig _styleConfig;
        [SerializeField] TMP_FontAsset _fontAsset;
        [SerializeField] float _defaultColumnWidth = 150f;

        [Header("Gizmos")]
        [SerializeField] bool _showViewportGizmo = true;
        [SerializeField] bool _showContentGizmo  = false;

        [Header("Interaction")]
        [SerializeField] bool _enableHoverHighlight = true;

        #endregion

        #region 数据与状态字段

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
        float _visibleRowCount;
        int   _firstVisibleCol;
        float _visibleColCount;
        bool  _dataDirty = true;

        int _pressedButtonRow = -1;
        int _highlightRow     = -1;
        int _highlightCol     = -1;

        bool _initialized;
        RenderLayer _lastRenderLayer;

        int _lastHoverRow = -1;
        int _lastHoverCol = -1;

        float _lastScrollY = -999f;
        float _lastScrollX = -999f;

        #endregion

        #region 事件

        public event System.Action<int, int>  OnCellClicked;
        public event System.Action<int, bool> OnToggleChanged;
        public event System.Action<int>       OnButtonClicked;

        #endregion

        #region 属性

        /// <summary>当前数据行数。</summary>
        public int RowCount    => _tableData.Count;

        /// <summary>当前表头列数。</summary>
        public int ColumnCount => _headers.Count;

        /// <summary>是否启用鼠标悬停十字光标高亮。</summary>
        public bool EnableHoverHighlight
        {
            get => _enableHoverHighlight;
            set => _enableHoverHighlight = value;
        }

        /// <summary>渲染层级模式。Normal 参与遮挡排序，TopMost 始终渲染在最前。</summary>
        public RenderLayer RenderLayer
        {
            get => _renderLayer;
            set
            {
                _renderLayer = value;
                _tableRenderer?.SetRenderLayer(value);
            }
        }

        /// <summary>固定列（Toggle + Button）的总宽度。</summary>
        public float FixedColumnWidth =>
            (_styleConfig.IsShowToggleColumn ? _styleConfig.ToggleColumnWidth : 0f)
          + (_styleConfig.IsShowButtonColumn ? _styleConfig.ButtonColumnWidth : 0f);

        /// <summary>所有数据列（含固定列）的总内容宽度。</summary>
        public float ContentWidth
        {
            get
            {
                if (_effectiveColWidths == null || _effectiveColWidths.Length == 0) return 0f;
                float sum = 0f;
                for (int i = 0; i < _effectiveColWidths.Length; i++) sum += _effectiveColWidths[i];
                return FixedColumnWidth + sum;
            }
        }

        /// <summary>表头行加所有数据行的总内容高度。</summary>
        public float ContentHeight
        {
            get
            {
                if (_styleConfig == null) return 0f;
                return _styleConfig.HeaderRowHeight + RowCount * _styleConfig.ContentRowHeight;
            }
        }

        #endregion

        #region Unity 生命周期

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
            _lastRenderLayer = _renderLayer;
            _tableRenderer.SetRenderLayer(_renderLayer);
            _initialized = true;
        }

        void Update()
        {
            if (!_initialized) return;
            if (_renderLayer != _lastRenderLayer)
            {
                _lastRenderLayer = _renderLayer;
                _tableRenderer?.SetRenderLayer(_renderLayer);
            }
            UpdateScroll();
            if (_enableHoverHighlight)
                UpdateHighlight();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化 TMP SDF 字体辅助器。
        /// 优先使用 Inspector 中指定的 _fontAsset，
        /// 其次使用 TMP Settings 中的默认字体。
        /// </summary>
        void InitFont()
        {
            TMP_FontAsset asset = _fontAsset;
            if (asset == null && TMP_Settings.instance != null)
                asset = TMP_Settings.defaultFontAsset;
            if (asset == null)
            {
                Debug.LogError("TableGpuController: No TMP_FontAsset assigned and no default found");
                return;
            }
            _fontHelper = new TableFontHelper(asset, _styleConfig.TextFontSize);
            _fontHelper.PreCacheAscii();
        }

        #endregion

        #region 滚动与渲染

        /// <summary>
        /// 根据当前滚动偏移计算可见行列范围。
        /// 仅在滚动位置变化或数据标记为脏时触发 Mesh 重建。
        /// </summary>
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
            float visCols = 0f;
            float acc = 0f;
            for (int i = newCol; i < _effectiveColWidths.Length; i++)
            {
                float cw = _effectiveColWidths[i];
                if (acc + cw < visContentW)
                {
                    acc += cw;
                    visCols += 1f;
                }
                else
                {
                    if (visContentW > acc)
                        visCols += (visContentW - acc) / cw;
                    break;
                }
            }
            if (visCols <= 0f) visCols = _viewportWidth / Mathf.Max(1f, _defaultColumnWidth);

            bool rowChanged = newRow != _firstVisibleRow;
            bool colChanged = newCol != _firstVisibleCol || Mathf.Abs(_visibleColCount - visCols) > 0.001f;

            if (rowChanged || colChanged || _dataDirty)
            {
                _firstVisibleRow = newRow;
                _visibleRowCount = Mathf.Max(0f, _viewportHeight - s.HeaderRowHeight) / rh;
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

        /// <summary>
        /// 调用 TableMeshBuilder 重建表格 Mesh，
        /// 更新 Shader 裁剪矩形和 SDF 渲染参数，提交到渲染后端。
        /// </summary>
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
                _viewportWidth, _viewportHeight, s.ViewportFillColor,
                s,
                s.IsShowToggleColumn, s.ToggleColumnWidth, _toggleStates,
                s.IsShowButtonColumn, s.ButtonColumnWidth, _buttonTexts,
                _pressedButtonRow,
                _highlightRow, _highlightCol);

            if (_material != null)
            {
                float sc = _worldUnitScale;
                _material.SetVector("_ClipRect", new Vector4(0, -_viewportHeight * sc, _viewportWidth * sc, 0));
                _material.SetFloat("_SDF_Threshold", s.SdfThreshold);
                _material.SetFloat("_SDF_Spread", s.SdfSpread);
            }

            _tableRenderer.SetViewportSize(_viewportWidth, _viewportHeight);
            _tableRenderer.ApplyMeshData();
        }

        /// <summary>
        /// 鼠标悬停十字光标高亮。根据鼠标位置执行 HitTest，
        /// 更新高亮行、列或清除高亮。仅在 _enableHoverHighlight 为 true 时调用。
        /// </summary>
        void UpdateHighlight()
        {
            Vector2 mp = Input.mousePosition;

            if (HitTest(mp, out int row, out int col))
            {
                if (row != _lastHoverRow || col != _lastHoverCol)
                {
                    _lastHoverRow = row;
                    _lastHoverCol = col;
                    SetHighlight(row, col);
                }
                return;
            }

            if (HitTestToggleCol(mp, out int tr))
            {
                if (tr != _lastHoverRow || _lastHoverCol != -2)
                {
                    _lastHoverRow = tr;
                    _lastHoverCol = -2;
                    SetHighlight(tr, -1);
                }
                return;
            }

            if (HitTestButtonCol(mp, out int br))
            {
                if (br != _lastHoverRow || _lastHoverCol != -3)
                {
                    _lastHoverRow = br;
                    _lastHoverCol = -3;
                    SetHighlight(br, -1);
                }
                return;
            }

            if (_lastHoverRow >= 0)
            {
                _lastHoverRow = -1;
                _lastHoverCol = -1;
                ClearHighlight();
            }
        }

        #endregion

        #region 数据操作 API

        /// <summary>设置表格数据，重置滚动偏移和可见范围，重新计算有效列宽。</summary>
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

        /// <summary>清空所有表格数据和交互状态，清除 Mesh。</summary>
        public void ClearTable()
        {
            _headers.Clear(); _tableData.Clear(); _toggleStates.Clear(); _buttonTexts.Clear();
            _scrollOffsetY = 0f; _scrollOffsetX = 0f;
            _firstVisibleRow = 0; _firstVisibleCol = 0;
            _pressedButtonRow = -1; _highlightRow = -1; _highlightCol = -1;
            _dataDirty = true;
            _tableRenderer?.GetMesh()?.Clear();
        }

        /// <summary>在末尾追加一行数据，自动补齐不足表头宽度。</summary>
        public void AddRow(List<string> rowData)
        {
            if (_headers.Count == 0) { Debug.LogWarning("AddRow: no headers"); return; }
            List<string> c = new List<string>(rowData);
            while (c.Count < _headers.Count) c.Add("");
            _tableData.Add(c); _toggleStates.Add(false); _buttonTexts.Add("Btn");
            _dataDirty = true;
        }

        /// <summary>移除指定索引的行，同时清理关联的按钮和按钮状态。</summary>
        public void RemoveRow(int i)
        {
            if (i < 0 || i >= _tableData.Count) return;
            _tableData.RemoveAt(i); _toggleStates.RemoveAt(i); _buttonTexts.RemoveAt(i);
            if (_pressedButtonRow == i) _pressedButtonRow = -1;
            if (_highlightRow == i) { _highlightRow = -1; _highlightCol = -1; }
            _dataDirty = true;
        }

        /// <summary>修改指定单元格的文本内容。</summary>
        public void SetCell(int row, int col, string val)
        {
            if (row < 0 || row >= _tableData.Count || col < 0 || col >= _headers.Count) return;
            _tableData[row][col] = val ?? ""; _dataDirty = true;
        }

        #endregion

        #region 滚动控制 API

        /// <summary>设置垂直滚动的绝对偏移（像素）。</summary>
        public void SetScrollOffsetY(float o) => _scrollOffsetY = Mathf.Max(0f, o);

        /// <summary>以归一化值 [0,1] 设置垂直滚动位置。</summary>
        public void SetScrollNormalizedY(float n)
        {
            TableStyleConfig s = _styleConfig; if (s == null) return;
            float totalH = _tableData.Count * s.ContentRowHeight;
            _scrollOffsetY = Mathf.Max(0f, n * Mathf.Max(0f, totalH - _viewportHeight));
        }

        /// <summary>获取当前垂直滚动偏移（像素）。</summary>
        public float GetScrollOffsetY() => _scrollOffsetY;

        /// <summary>设置水平滚动的绝对偏移（像素）。</summary>
        public void SetScrollOffsetX(float o) => _scrollOffsetX = Mathf.Max(0f, o);

        /// <summary>以归一化值 [0,1] 设置水平滚动位置。</summary>
        public void SetScrollNormalizedX(float n)
        {
            float totalW = 0f;
            if (_effectiveColWidths != null)
                for (int i = 0; i < _effectiveColWidths.Length; i++) totalW += _effectiveColWidths[i];
            float visW = Mathf.Max(1f, _viewportWidth - FixedColumnWidth);
            _scrollOffsetX = Mathf.Max(0f, n * Mathf.Max(0f, totalW - visW));
        }

        /// <summary>获取当前水平滚动偏移（像素）。</summary>
        public float GetScrollOffsetX() => _scrollOffsetX;

        /// <summary>获取当前第一个可见行的行号。</summary>
        public int GetFirstVisibleRow() => _firstVisibleRow;

        #endregion

        #region 交互状态 API

        /// <summary>设置指定行的 Toggle 状态，触发 OnToggleChanged 事件。</summary>
        public void SetToggleState(int row, bool on)
        {
            if (row < 0 || row >= _toggleStates.Count || _toggleStates[row] == on) return;
            _toggleStates[row] = on; _dataDirty = true;
            OnToggleChanged?.Invoke(row, on);
        }

        /// <summary>获取指定行的 Toggle 开关状态。</summary>
        public bool GetToggleState(int r) => r >= 0 && r < _toggleStates.Count && _toggleStates[r];

        /// <summary>设置指定行 Button 列的显示文本。</summary>
        public void SetButtonText(int row, string t)
        {
            if (row < 0 || row >= _buttonTexts.Count) return;
            _buttonTexts[row] = t ?? ""; _dataDirty = true;
        }

        /// <summary>标记指定行按钮为按下状态（视觉变暗）。</summary>
        public void SetButtonPressed(int row)
        {
            if (row == _pressedButtonRow) return;
            _pressedButtonRow = row; _dataDirty = true;
        }

        /// <summary>释放按钮按下状态，恢复视觉。</summary>
        public void SetButtonReleased()
        {
            if (_pressedButtonRow < 0) return;
            _pressedButtonRow = -1; _dataDirty = true;
        }

        /// <summary>设置高亮单元格（行列交叉高亮）。</summary>
        public void SetHighlight(int row, int col)
        {
            if (row == _highlightRow && col == _highlightCol) return;
            _highlightRow = row; _highlightCol = col; _dataDirty = true;
        }

        /// <summary>清除所有高亮状态。</summary>
        public void ClearHighlight()
        {
            if (_highlightRow < 0 && _highlightCol < 0) return;
            _highlightRow = -1; _highlightCol = -1; _dataDirty = true;
        }

        #endregion

        #region 辅助计算

        /// <summary>
        /// 根据 StyleConfig 中设置的列宽和默认列宽计算每列的实际渲染宽度。
        /// 未设置（<=0）的列使用 _defaultColumnWidth。
        /// </summary>
        float[] CalcEffectiveColWidths()
        {
            int n = _headers.Count;
            if (n == 0) return new float[0];
            float[] w = new float[n];
            float[] manual = _styleConfig != null ? _styleConfig.ColumnWidths : null;
            for (int i = 0; i < n; i++)
            {
                if (manual != null && i < manual.Length && manual[i] > 0f)
                    w[i] = manual[i];
                else
                    w[i] = _defaultColumnWidth;
            }
            return w;
        }

        #endregion

        #region 点击测试

        /// <summary>将视口局部 Y 坐标转换为数据行号，考虑表头偏移和滚动。</summary>
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

        /// <summary>将视口局部 X 坐标转换为数据列号，考虑固定列偏移和水平滚动。</summary>
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

        /// <summary>判断鼠标是否悬停在表格视口区域内。</summary>
        public bool IsMouseOverTable(Vector2 screen)
        {
            if (_tableRenderer == null || _styleConfig == null) return false;
            Vector2 local = _tableRenderer.ScreenToTablePoint(screen, _renderCamera);
            return local.x >= 0f && local.x <= _viewportWidth
                && local.y >= -_viewportHeight && local.y <= 0f;
        }

        /// <summary>对指定屏幕坐标执行点击测试，返回对应的数据单元格行列号。</summary>
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

        /// <summary>测试点击是否落在 Toggle 列范围内，返回对应行号。</summary>
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

        /// <summary>测试点击是否落在 Button 列范围内，返回对应行号。</summary>
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

        #endregion

        #region 编辑器辅助

        void OnDrawGizmos()
        {
            if (_rendererComponent == null) return;

            Transform t = _rendererComponent.transform;
            float scale = _worldUnitScale;

            if (_showViewportGizmo)
            {
                float vw = _viewportWidth * scale;
                float vh = _viewportHeight * scale;
                DrawGizmoRect(t, 0, 0, vw, vh, new Color(0, 1, 1, 0.4f));
            }

            if (_showContentGizmo)
            {
                float cw = ContentWidth * scale;
                float ch = 0f;
                if (_styleConfig != null)
                {
                    int rows = RowCount > 0 ? RowCount : 10;
                    ch = (_styleConfig.HeaderRowHeight + rows * _styleConfig.ContentRowHeight) * scale;
                }
                if (cw > 0f && ch > 0f)
                    DrawGizmoRect(t, 0, 0, cw, ch, new Color(1, 0.9f, 0, 0.3f));
            }
        }

        /// <summary>在场景视图中绘制一个矩形线框，用于可视化视口和内容区域。</summary>
        void DrawGizmoRect(Transform t, float x, float y, float w, float h, Color color)
        {
            Vector3 tl = t.TransformPoint(new Vector3(x, y, 0));
            Vector3 tr = t.TransformPoint(new Vector3(x + w, y, 0));
            Vector3 bl = t.TransformPoint(new Vector3(x, y - h, 0));
            Vector3 br = t.TransformPoint(new Vector3(x + w, y - h, 0));

            Gizmos.color = color;
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
            Gizmos.DrawLine(bl, tl);
        }

        #endregion
    }
}
