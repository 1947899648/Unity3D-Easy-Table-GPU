using System.Collections.Generic;
using UnityEngine;
using WPZ0325.EasyTableGPU;

/// <summary>
/// 海量中文数据性能测试 Demo（3D 场景）。
/// 按数字键 1~5 生成不同规模的数据，按 S 生成默认 100 行。
/// 左上角显示 FPS 和当前数据行数。
/// </summary>
public class TableGpuDemo_Stress : MonoBehaviour
{
    [SerializeField] TableGpuController _controller;

    [SerializeField] KeyCode _clearKey  = KeyCode.A;
    [SerializeField] KeyCode _genKey    = KeyCode.S;
    [SerializeField] KeyCode _addRowKey = KeyCode.Z;
    [SerializeField] KeyCode _delRowKey = KeyCode.X;

    [SerializeField] int _defaultRows = 100;
    [SerializeField] int _columnCount = 20;

    float _scrollPosY;
    float _scrollPosX;
    bool  _subscribed;

    float _deltaTime;

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
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

        if (_controller == null) return;

        if (Input.GetKeyDown(_clearKey))  { _controller.ClearTable(); ResetScroll(); }

        if (Input.GetKeyDown(KeyCode.Alpha1)) { Generate(1000,   _columnCount); ResetScroll(); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { Generate(5000,   _columnCount); ResetScroll(); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { Generate(10000,  _columnCount); ResetScroll(); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { Generate(50000,  _columnCount); ResetScroll(); }
        if (Input.GetKeyDown(KeyCode.Alpha5)) { Generate(100000, _columnCount); ResetScroll(); }

        if (Input.GetKeyDown(_genKey))    { Generate(_defaultRows, _columnCount); ResetScroll(); }
        if (Input.GetKeyDown(_addRowKey) && _controller.RowCount > 0)
            _controller.AddRow(GenerateRow(_controller.ColumnCount));
        if (Input.GetKeyDown(_delRowKey) && _controller.RowCount > 0)
            _controller.RemoveRow(_controller.RowCount - 1);

        float scrollY = Input.mouseScrollDelta.y * 60f;
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

    void OnGUI()
    {
        float fps = _deltaTime > 0f ? 1f / _deltaTime : 0f;
        int rowCount = _controller != null ? _controller.RowCount : 0;

        GUIStyle style = new GUIStyle();
        style.fontSize = 36;
        style.normal.textColor = Color.green;

        GUILayout.BeginArea(new Rect(20, 20, 600, 200));
        GUILayout.Label(string.Format("FPS: {0:F0}  行数: {1:N0}", fps, rowCount), style);
        GUILayout.Label("[1] 1K  [2] 5K  [3] 10K  [4] 50K  [5] 100K  [S] 100  [A] 清空", style);
        GUILayout.EndArea();
    }

    void ResetScroll() { _scrollPosY = 0f; _scrollPosX = 0f; }

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
            return;
        }

        if (_controller.HitTest(mp, out int row, out int col))
        {
        }
    }

    void OnCellClicked(int row, int col)   { }
    void OnToggleChanged(int row, bool on) { }
    void OnButtonClicked(int row)          { }

    void Generate(int rows, int cols)
    {
        List<string> headers = GenerateHeaders(cols);
        List<List<string>> data = new List<List<string>>(rows);

        for (int r = 0; r < rows; r++)
            data.Add(GenerateRow(cols));

        _controller.SetData(headers, data);
        Debug.Log(string.Format("[Stress] 生成 {0:N0} 行 × {1} 列", rows, cols));
    }

    List<string> GenerateRow(int cols)
    {
        List<string> row = new List<string>(cols);
        for (int c = 0; c < cols; c++)
            row.Add(GenerateCell(c));
        return row;
    }

    List<string> GenerateHeaders(int cols)
    {
        string[] presets =
        {
            "编号", "订单名称", "客户姓名", "联系电话", "收货地址",
            "商品名称", "规格型号", "单价(元)", "数量", "金额(元)",
            "下单日期", "发货日期", "物流单号", "配送方式", "支付方式",
            "订单状态", "备注说明", "客服人员", "所属区域", "数据来源"
        };

        List<string> headers = new List<string>(cols);
        for (int i = 0; i < cols; i++)
            headers.Add(i < presets.Length ? presets[i] : string.Format("列{0}", i + 1));
        return headers;
    }

    string GenerateCell(int col)
    {
        switch (col % 20)
        {
            case 0:  return string.Format("ORD{0:D8}", Random.Range(1, 99999999));
            case 1:  return GenText(4, 12);
            case 2:  return GenName();
            case 3:  return "1" + Random.Range(0, 1000000000).ToString("D10");
            case 4:  return GenText(6, 20);
            case 5:  return GenText(2, 8);
            case 6:  return GenText(2, 12);
            case 7:  return string.Format("¥{0:F2}", Random.Range(1f, 9999f));
            case 8:  return Random.Range(1, 1000).ToString();
            case 9:  return string.Format("¥{0:F2}", Random.Range(1f, 999999f));
            case 10: return string.Format("2026-{0:D2}-{1:D2}", Random.Range(1, 13), Random.Range(1, 29));
            case 11: return Random.value > 0.3f ? string.Format("2026-{0:D2}-{1:D2}", Random.Range(1, 13), Random.Range(1, 29)) : "";
            case 12: return GenText(8, 16);
            case 13: return GenDelivery();
            case 14: return GenPayment();
            case 15: return GenStatus();
            case 16: return GenText(0, 10);
            case 17: return GenName();
            case 18: return GenRegion();
            case 19: return Random.value > 0.5f ? "线上" : "线下";
            default: return "";
        }
    }

    string GenText(int minLen, int maxLen)
    {
        int len = Random.Range(minLen, maxLen + 1);
        if (len <= 0) return "";
        char[] chars = new char[len];
        for (int i = 0; i < len; i++)
            chars[i] = (char)Random.Range(0x4E00, 0x9FFF + 1);
        return new string(chars);
    }

    static readonly string[] _surnames = { "王", "李", "张", "刘", "陈", "杨", "赵", "黄", "周", "吴", "徐", "孙", "马", "朱", "胡", "郭", "何", "高", "林", "罗" };
    static readonly string[] _givenNames = { "伟", "芳", "娜", "敏", "静", "丽", "强", "磊", "洋", "勇", "艳", "杰", "涛", "明", "超", "秀", "霞", "平", "刚", "桂" };

    string GenName()
    {
        string s = _surnames[Random.Range(0, _surnames.Length)];
        int n = Random.Range(1, 3);
        for (int i = 0; i < n; i++)
            s += _givenNames[Random.Range(0, _givenNames.Length)];
        return s;
    }

    static readonly string[] _delivery = { "顺丰速运", "圆通快递", "中通快递", "韵达快递", "京东物流", "邮政EMS", "极兔速递", "德邦物流", "自提", "同城配送" };
    string GenDelivery() => _delivery[Random.Range(0, _delivery.Length)];

    static readonly string[] _payment = { "微信支付", "支付宝", "银行卡", "货到付款", "对公转账", "白条", "花呗", "余额支付" };
    string GenPayment() => _payment[Random.Range(0, _payment.Length)];

    static readonly string[] _status = { "待付款", "已付款", "处理中", "已发货", "运输中", "已签收", "已完成", "已取消", "退款中", "已退货" };
    string GenStatus() => _status[Random.Range(0, _status.Length)];

    static readonly string[] _regions = { "华东区", "华南区", "华北区", "华中区", "西南区", "西北区", "东北区", "港澳台", "海外" };
    string GenRegion() => _regions[Random.Range(0, _regions.Length)];
}
