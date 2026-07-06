using System.Collections.Generic;
using UnityEngine;
using WPZ0325.EasyTableGPU;

public class TableGpuDemoUI : MonoBehaviour
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
        if (Mathf.Abs(scrollY) > 0.01f && _controller.IsMouseOverTable(Input.mousePosition))
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

        if (Input.GetMouseButtonDown(0))
            TryClick();

        if (Input.GetMouseButtonUp(0))
            _controller.SetButtonReleased();
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
            Debug.Log($"[TableGpuDemoUI] Button pressed: row={br}");
            return;
        }

        if (_controller.HitTest(mp, out int row, out int col))
        {
            Debug.Log($"[TableGpuDemoUI] Cell clicked: ({row},{col})");
        }
    }

    void ResetScroll() { _scrollPosY = 0f; _scrollPosX = 0f; }

    void OnCellClicked(int row, int col)   { Debug.Log($"[TableGpuDemoUI] Cell clicked: ({row},{col})"); }
    void OnToggleChanged(int row, bool on) { Debug.Log($"[TableGpuDemoUI] Toggle row={row} -> {on}"); }
    void OnButtonClicked(int row)          { Debug.Log($"[TableGpuDemoUI] Button clicked: row={row}"); }

    void Generate(int rows, int cols)
    {
        string[] headersArr = { "编号", "订单名称", "ユーザー名", "地址", "金額(円)", "Status", "担当者", "備考", "電話番号", "登録日" };
        List<string> headers = new List<string>();
        for (int c = 0; c < cols && c < headersArr.Length; c++) headers.Add(headersArr[c]);
        for (int c = headersArr.Length; c < cols; c++) headers.Add($"列{c}");

        List<List<string>> data = new List<List<string>>();
        for (int r = 0; r < rows; r++)
        {
            List<string> row = new List<string>();
            for (int c = 0; c < cols; c++)
                row.Add(GenerateCell(c, r));
            data.Add(row);
        }
        _controller.SetData(headers, data);
        Debug.Log($"[TableGpuDemoUI] {rows} rows x {cols} cols");
    }

    List<string> GenerateRow(int cols)
    {
        int r = _controller.RowCount;
        List<string> row = new List<string>();
        for (int c = 0; c < cols; c++)
            row.Add(GenerateCell(c, r));
        return row;
    }

    static readonly string[] _cnNames  = { "张伟", "李娜", "王磊", "赵敏", "陈静", "刘洋", "黄丽", "周强", "吴芳", "孙涛", "马超", "林黛玉" };
    static readonly string[] _cnCities = { "北京市", "上海市", "广州市", "深圳市", "成都市", "杭州市", "南京市", "武汉市", "西安市", "重庆市" };
    static readonly string[] _cnItems  = { "笔记本电脑", "机械键盘", "显示器", "无线鼠标", "打印机", "路由器", "移动硬盘", "蓝牙耳机", "充电器", "数据线" };
    static readonly string[] _cnStatus = { "处理中", "已完成", "待审核", "已发货", "已取消", "退款中" };

    static readonly string[] _jpNames  = { "田中太郎", "鈴木花子", "佐藤健", "山田優", "伊藤美咲", "渡辺蓮", "中村葵", "小林翼", "加藤楓", "吉田空" };
    static readonly string[] _jpCities = { "東京都", "大阪府", "横浜市", "名古屋市", "札幌市", "福岡市", "神戸市", "京都市", "仙台市", "広島市" };
    static readonly string[] _jpItems  = { "ノートPC", "キーボード", "モニター", "マウス", "プリンター", "ルーター", "HDDケース", "イヤホン", "充電器", "ケーブル" };
    static readonly string[] _jpStatus = { "処理中", "完了", "審査中", "発送済", "取消済", "返金中" };

    static readonly string[] _enNames  = { "John Smith", "Emma Davis", "Michael Chen", "Sarah Wilson", "David Brown", "Lisa Taylor", "Robert Jones", "Anna Miller", "James White", "Emily Clark" };
    static readonly string[] _enCities = { "New York", "London", "Tokyo", "Paris", "Berlin", "Sydney", "Toronto", "Seoul", "Singapore", "Dubai" };
    static readonly string[] _enStatus = { "Pending", "Completed", "Reviewing", "Shipped", "Cancelled", "Refunding" };

    static readonly string[] _phones = { "13800138000", "090-1234-5678", "555-0100", "18612345678", "070-9876-5432", "555-0200", "13987654321", "080-1111-2222", "555-0300" };
    static readonly string[] _dates  = { "2026/01/15", "2026/02/28", "2026/03/10", "2026/04/05", "2026/05/20", "2026/06/01", "2026/07/04", "2026/08/12", "2026/09/30", "2026/10/25" };

    string GenerateCell(int col, int row)
    {
        switch (col % 10)
        {
            case 0: return $"ORD-{row:D4}";
            case 1: return string.Format("{0} {1}", _cnNames[row % _cnNames.Length], Random.value > 0.3f ? _cnItems[row % _cnItems.Length] : _enNames[row % _enNames.Length]);
            case 2: return string.Format("{0} ({1})", _jpNames[row % _jpNames.Length], Random.value > 0.5f ? _jpCities[row % _jpCities.Length] : _enCities[row % _enCities.Length]);
            case 3: { string[] arr = Random.value > 0.5f ? _cnCities : _jpCities; return arr[row % arr.Length]; }
            case 4: return $"¥{Random.Range(1000, 200000):N0}";
            case 5:
            {
                float rv = Random.value;
                if (rv < 0.33f) return _cnStatus[row % _cnStatus.Length];
                if (rv < 0.66f) return _jpStatus[row % _jpStatus.Length];
                return _enStatus[row % _enStatus.Length];
            }
            case 6: { string[] arr = Random.value > 0.5f ? _cnNames : _enNames; return arr[row % arr.Length]; }
            case 7: return string.Format("{0} [{1}]", _cnItems[row % _cnItems.Length], _jpItems[row % _jpItems.Length]);
            case 8: return _phones[row % _phones.Length];
            case 9: return _dates[row % _dates.Length];
            default: return $"({row},{col})";
        }
    }
}
