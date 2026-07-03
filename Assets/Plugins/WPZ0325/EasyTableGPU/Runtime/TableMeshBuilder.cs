using System.Collections.Generic;
using UnityEngine;

namespace WPZ0325.EasyTableGPU
{
    public class TableMeshBuilder
    {
        TableFontHelper _fontHelper;

        List<Vector3> _verts  = new List<Vector3>(16384);
        List<Vector2> _uv0    = new List<Vector2>(16384);
        List<Vector2> _uv1    = new List<Vector2>(16384);
        List<Color>   _colors = new List<Color>(16384);
        List<int>     _tris   = new List<int>(16384);

        const float TEXT_Z  = -0.01f;
        const float HIGHL_Z = -0.005f;
        float _scale = 1f;

        public void SetFont(TableFontHelper fontHelper) { _fontHelper = fontHelper; }
        public void SetScale(float scale) { _scale = scale; }

        public void BuildTableMesh(Mesh mesh,
            List<string> headers, List<List<string>> data,
            int firstRow, int visibleRowCount, float[] colWidths,
            float rowHeight, float headerHeight, float fineScrollY,
            int firstVisibleCol, int visibleColCount, float contentScrollX,
            TableStyleConfig style,
            bool showToggle, float toggleWidth, List<bool> toggleStates,
            bool showButton, float buttonWidth, List<string> buttonTexts,
            int pressedButtonRow,
            int highlightRow, int highlightCol)
        {
            ClearLists();
            if (data == null || data.Count == 0 || colWidths == null || colWidths.Length == 0)
                { ApplyToMesh(mesh); return; }

            float padL = style.CellPaddingLeft;
            float padR = style.CellPaddingRight;
            float fixedW = (showToggle ? toggleWidth : 0f) + (showButton ? buttonWidth : 0f);

            int lastVisibleCol = Mathf.Min(firstVisibleCol + visibleColCount, colWidths.Length);
            float totalContentW = 0f;
            for (int c = 0; c < colWidths.Length; c++) totalContentW += colWidths[c];
            float totalW = fixedW + totalContentW;

            if (fixedW > 0f)
                AddBgQuad(0f, 0f, fixedW, headerHeight, style.HeaderBackgroundColor);

            float hBaseline = CalcBaseline(0f, headerHeight);
            float hx = fixedW - contentScrollX;
            for (int c = firstVisibleCol; c < lastVisibleCol && c < headers.Count; c++)
            {
                float cw = colWidths[c];
                Color bg = style.IsOdd(c) ? style.HeaderItemOddColumnColor : style.HeaderItemEvenColumnColor;
                AddBgQuad(hx, 0f, cw, headerHeight, bg);
                string text = headers[c] ?? "";
                if (text.Length > 0)
                    LayoutText(text, hx + padL, hx + cw - padR, hBaseline, style.HeaderTextColor);
                hx += cw;
            }

            int endRow = Mathf.Min(firstRow + visibleRowCount, data.Count);
            float rowYBase = -headerHeight + fineScrollY;
            for (int r = firstRow; r < endRow; r++)
            {
                float cellY = rowYBase - (r - firstRow) * rowHeight;
                float baseline = CalcBaseline(cellY, rowHeight);
                float cx = 0f;

                if (showToggle)
                {
                    int idx = r < toggleStates.Count ? r : (toggleStates.Count - 1);
                    Color baseCol = toggleStates[idx] ? style.ToggleColumnOnColor : style.ToggleColumnOffColor;
                    Color rowCol = style.IsOdd(r) ? style.ToggleColumnOddRowColor : style.ToggleColumnEvenRowColor;
                    AddBgQuad(cx, cellY, toggleWidth, rowHeight, Color.Lerp(baseCol, rowCol, 0.15f));
                    cx += toggleWidth;
                }

                if (showButton)
                {
                    int idx = r < buttonTexts.Count ? r : (buttonTexts.Count - 1);
                    Color bg = style.IsOdd(r) ? style.ButtonColumnOddRowColor : style.ButtonColumnEvenRowColor;
                    if (r == pressedButtonRow)
                        bg = new Color(bg.r * 0.6f, bg.g * 0.6f, bg.b * 0.6f, bg.a);
                    AddBgQuad(cx, cellY, buttonWidth, rowHeight, bg);
                    string text = buttonTexts[idx] ?? "";
                    if (text.Length > 0)
                        LayoutText(text, cx + padL, cx + buttonWidth - padR, baseline, style.CellTextColor);
                    cx += buttonWidth;
                }

                float dStart = fixedW - contentScrollX;
                for (int c = firstVisibleCol; c < lastVisibleCol && c < data[r].Count; c++)
                {
                    float cw = colWidths[c];
                    AddBgQuad(dStart, cellY, cw, rowHeight, style.GetContentCellBg(r, c));
                    string text = data[r][c] ?? "";
                    if (text.Length > 0)
                        LayoutText(text, dStart + padL, dStart + cw - padR, baseline, style.CellTextColor);
                    dStart += cw;
                }
            }

            if (highlightRow >= firstRow && highlightRow < endRow)
            {
                float hy = rowYBase - (highlightRow - firstRow) * rowHeight;
                Color hc = style.HighlightColor;
                AddHighlightQuad(0f, hy, totalW, rowHeight, hc);
                if (highlightCol >= firstVisibleCol && highlightCol < lastVisibleCol)
                {
                    float colX = fixedW - contentScrollX;
                    for (int c = firstVisibleCol; c < highlightCol; c++)
                        colX += colWidths[c];
                    float colW = colWidths[highlightCol];
                    float visibleBot = rowYBase - (visibleRowCount - 1) * rowHeight - rowHeight;
                    AddHighlightQuad(colX, 0f, colW, -visibleBot, new Color(hc.r, hc.g, hc.b, hc.a * 0.5f));
                }
            }

            ApplyToMesh(mesh);
        }

        void AddHighlightQuad(float x, float y, float w, float h, Color color)
        {
            int vi = _verts.Count;
            float z = HIGHL_Z;
            _verts.Add(new Vector3(x,         y,     z));
            _verts.Add(new Vector3(x + w,     y,     z));
            _verts.Add(new Vector3(x,         y - h, z));
            _verts.Add(new Vector3(x + w,     y - h, z));
            for (int i = 0; i < 4; i++) _uv0.Add(Vector2.zero);
            for (int i = 0; i < 4; i++) _uv1.Add(new Vector2(0f, 0f));
            for (int i = 0; i < 4; i++) _colors.Add(color);
            _tris.Add(vi); _tris.Add(vi + 1); _tris.Add(vi + 2);
            _tris.Add(vi + 1); _tris.Add(vi + 3); _tris.Add(vi + 2);
        }

        void LayoutText(string text, float left, float right, float baseline, Color color)
        {
            float penX = left;
            bool truncated = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (!_fontHelper.TryGetGlyph(text[i],
                    out float adv, out float bx, out float by,
                    out float gw, out float gh,
                    out Vector2 uvBL, out Vector2 uvTR))
                    continue;
                if (penX + adv > right) { truncated = true; break; }
                AddGlyphQuad(penX + bx, baseline + by, gw, gh, uvBL, uvTR, color);
                penX += adv;
            }
            if (truncated) DrawEllipsis(right, baseline, color);
        }

        void DrawEllipsis(float right, float baseline, Color color)
        {
            float tw = 0f; float[] ws = { 0f, 0f, 0f };
            for (int i = 0; i < 3; i++)
                if (_fontHelper.TryGetGlyph('.', out float a, out _, out _, out _, out _, out _, out _))
                    { ws[i] = a; tw += a; }
            float px = right - tw;
            for (int i = 0; i < 3; i++)
                if (_fontHelper.TryGetGlyph('.', out _, out float bx, out float by,
                    out float gw, out float gh,
                    out Vector2 uvBL, out Vector2 uvTR))
                {
                    AddGlyphQuad(px + bx, baseline + by, gw, gh, uvBL, uvTR, color);
                    px += ws[i];
                }
        }

        float CalcBaseline(float cellTop, float cellH)
        {
            if (_fontHelper.TryGetGlyph('A', out _, out _, out float maxY,
                out _, out float gh, out _, out _))
                return (cellTop - cellH * 0.5f) - (2f * maxY - gh) * 0.5f;
            return cellTop - cellH * 0.35f;
        }

        void AddBgQuad(float x, float y, float w, float h, Color color)
        {
            int vi = _verts.Count;
            _verts.Add(new Vector3(x, y, 0f)); _verts.Add(new Vector3(x + w, y, 0f));
            _verts.Add(new Vector3(x, y - h, 0f)); _verts.Add(new Vector3(x + w, y - h, 0f));
            for (int i = 0; i < 4; i++) _uv0.Add(Vector2.zero);
            for (int i = 0; i < 4; i++) _uv1.Add(new Vector2(0f, 0f));
            for (int i = 0; i < 4; i++) _colors.Add(color);
            _tris.Add(vi); _tris.Add(vi + 1); _tris.Add(vi + 2);
            _tris.Add(vi + 1); _tris.Add(vi + 3); _tris.Add(vi + 2);
        }

        void AddGlyphQuad(float x, float y, float w, float h, Vector2 uvBL, Vector2 uvTR, Color color)
        {
            int vi = _verts.Count; float z = TEXT_Z;
            _verts.Add(new Vector3(x, y, z)); _verts.Add(new Vector3(x + w, y, z));
            _verts.Add(new Vector3(x, y - h, z)); _verts.Add(new Vector3(x + w, y - h, z));
            _uv0.Add(new Vector2(uvBL.x, uvTR.y)); _uv0.Add(new Vector2(uvTR.x, uvTR.y));
            _uv0.Add(new Vector2(uvBL.x, uvBL.y)); _uv0.Add(new Vector2(uvTR.x, uvBL.y));
            for (int i = 0; i < 4; i++) _uv1.Add(new Vector2(1f, 0f));
            for (int i = 0; i < 4; i++) _colors.Add(color);
            _tris.Add(vi); _tris.Add(vi + 1); _tris.Add(vi + 2);
            _tris.Add(vi + 1); _tris.Add(vi + 3); _tris.Add(vi + 2);
        }

        void ClearLists()
            { _verts.Clear(); _uv0.Clear(); _uv1.Clear(); _colors.Clear(); _tris.Clear(); }

        void ApplyToMesh(Mesh mesh)
        {
            mesh.Clear();
            if (_verts.Count > 0)
            {
                if (_scale != 1f)
                {
                    Vector3[] s = new Vector3[_verts.Count];
                    for (int i = 0; i < _verts.Count; i++) s[i] = _verts[i] * _scale;
                    mesh.SetVertices(s);
                }
                else mesh.SetVertices(_verts);
                mesh.SetUVs(0, _uv0); mesh.SetUVs(1, _uv1);
                mesh.SetColors(_colors); mesh.SetTriangles(_tris, 0);
            }
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(10000, 10000, 10000));
        }
    }
}
