using UnityEngine;

namespace WPZ0325.EasyTableGPU
{
    [CreateAssetMenu(fileName = "TableGpuStyle", menuName = "WPZ0325/Easy Table GPU Style")]
    public class TableStyleConfig : ScriptableObject
    {
        [Header("Table Type")]
        public bool IsShowToggleColumn = false;
        public bool IsShowButtonColumn = false;

        [Header("Table Size")]
        public float ToggleColumnWidth  = 80f;
        public float ButtonColumnWidth  = 120f;
        public float HeaderRowHeight    = 60f;
        public float ContentRowHeight   = 50f;
        public float TableTotalWidth    = 800f;

        [Header("Column Widths")]
        [Tooltip("空数组或元素为0的列 = 均分剩余宽度")]
        public float[] ColumnWidths;

        [Header("Text Style")]
        public int   TextFontSize       = 14;
        public float CellPaddingLeft    = 6f;
        public float CellPaddingRight   = 6f;
        public float CharTracking       = 0f;
        public Color CellTextColor      = Color.black;
        public Color HeaderTextColor    = Color.black;

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

        [Header("Highlight")]
        public Color HighlightColor = new Color(0.2f, 0.5f, 0.9f, 0.25f);

        public bool IsOdd(int n) => (n & 1) == 1;

        public Color GetContentCellBg(int row, int col)
        {
            Color rowColor = IsOdd(row) ? ContentOddRowColor : ContentEvenRowColor;
            Color colColor = IsOdd(col) ? ContentItemOddColumnColor : ContentItemEvenColumnColor;
            if (colColor.a <= 0f) return rowColor;
            return (rowColor + colColor) * 0.5f;
        }
    }
}
