using UnityEngine;

namespace WPZ0325.EasyTableGPU
{
    /// <summary>
    /// 表格样式配置资产（ScriptableObject）。集中管理颜色主题、尺寸、字体参数。
    /// 通过 Assets 菜单 "WPZ0325 / Easy Table GPU Style" 创建。
    /// </summary>
    [CreateAssetMenu(fileName = "TableGpuStyle", menuName = "WPZ0325/Easy Table GPU Style")]
    public class TableStyleConfig : ScriptableObject
    {
        #region 表格类型

        [Header("Table Type")]
        public bool IsShowToggleColumn = false;
        public bool IsShowButtonColumn = false;

        #endregion

        #region 尺寸

        [Header("Table Size")]
        public float ToggleColumnWidth  = 80f;
        public float ButtonColumnWidth  = 120f;
        public float HeaderRowHeight    = 60f;
        public float ContentRowHeight   = 50f;

        #endregion

        #region 列宽

        [Header("Column Widths")]
        [Tooltip("空数组或元素<=0的列 = 使用Controller中的DefaultColumnWidth")]
        public float[] ColumnWidths;

        #endregion

        #region 文本样式

        [Header("Text Style")]
        public int   TextFontSize       = 14;
        public float CellPaddingLeft    = 6f;
        public float CellPaddingRight   = 6f;
        public float CharTracking       = 0f;
        public Color CellTextColor      = Color.black;
        public Color HeaderTextColor    = Color.black;

        #endregion

        #region 颜色主题

        [Header("Table Color")]
        public Color ToggleColumnHeaderColor   = Color.white;
        public Color ToggleColumnOddRowColor   = Color.white;
        public Color ToggleColumnEvenRowColor  = new Color(0.95f, 0.95f, 0.95f);
        public Color ToggleColumnOnColor       = new Color(0.4f, 0.9f, 0.4f);
        public Color ToggleColumnOffColor      = new Color(0.8f, 0.8f, 0.8f);

        public Color ButtonColumnHeaderColor   = Color.white;
        public Color ButtonColumnOddRowColor   = Color.white;
        public Color ButtonColumnEvenRowColor  = new Color(0.95f, 0.95f, 0.95f);

        public Color HeaderBackgroundColor     = new Color(0.85f, 0.85f, 0.85f);
        public Color HeaderItemOddColumnColor  = new Color(0.8f, 0.8f, 0.8f);
        public Color HeaderItemEvenColumnColor = new Color(0.7f, 0.7f, 0.7f);

        public Color ContentOddRowColor        = Color.white;
        public Color ContentEvenRowColor       = new Color(0.96f, 0.96f, 0.96f);
        public Color ContentItemOddColumnColor = Color.clear;
        public Color ContentItemEvenColumnColor = Color.clear;

        #endregion

        #region 高亮与视口

        [Header("Highlight")]
        public Color HighlightColor = new Color(0.2f, 0.5f, 0.9f, 0.25f);

        [Header("Viewport")]
        public Color ViewportFillColor = Color.black;

        #endregion

        #region SDF 渲染参数

        [Header("SDF Rendering")]
        [Range(0f, 1f)]  public float SdfThreshold = 0.5f;
        [Range(0f, 0.2f)] public float SdfSpread   = 0.05f;

        #endregion

        #region 工具方法

        /// <summary>判断整数是否为奇数（位运算）。</summary>
        public bool IsOdd(int n) => (n & 1) == 1;

        /// <summary>
        /// 根据行列奇偶性计算数据单元格背景色。
        /// 当 ContentItemOdd/EvenColumnColor 的 alpha 为 0 时仅使用行色，
        /// 否则取行列颜色的平均值实现交叉着色效果。
        /// </summary>
        public Color GetContentCellBg(int row, int col)
        {
            Color rowColor = IsOdd(row) ? ContentOddRowColor : ContentEvenRowColor;
            Color colColor = IsOdd(col) ? ContentItemOddColumnColor : ContentItemEvenColumnColor;
            if (colColor.a <= 0f) return rowColor;
            return (rowColor + colColor) * 0.5f;
        }

        #endregion
    }
}
