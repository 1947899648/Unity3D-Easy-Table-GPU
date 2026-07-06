using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;
using TMPro;

namespace WPZ0325.EasyTableGPU
{
    /// <summary>
    /// 字体度量辅助器。封装 TMP_FontAsset 的 SDF 字形查询，
    /// 提供字符 UV 坐标和像素度量信息的缓存与检索。
    /// </summary>
    public class TableFontHelper
    {
        #region 内部类型

        /// <summary>缓存单个字符的 SDF 纹理坐标和排版度量。</summary>
        struct GlyphInfo
        {
            public Vector2 uvBottomLeft;
            public Vector2 uvTopRight;
            public float advance;
            public float minX;
            public float maxY;
            public float glyphWidth;
            public float glyphHeight;
        }

        #endregion

        #region 字段

        TMP_FontAsset _fontAsset;
        float _targetFontSize;
        float _metricScale;
        Dictionary<char, GlyphInfo> _cache = new Dictionary<char, GlyphInfo>();

        #endregion

        #region 属性

        /// <summary>SDF 字体图集纹理（TMP_FontAsset 的主图集页）。</summary>
        public Texture FontTexture => _fontAsset?.atlasTexture;

        /// <summary>当前使用的 TMP 字体资产引用。</summary>
        public TMP_FontAsset FontAsset => _fontAsset;

        #endregion

        #region 构造

        /// <summary>
        /// 初始化字体辅助器，计算度量缩放因子。
        /// 缩放公式与 TMP 内部一致：targetSize / pointSize × scale。
        /// </summary>
        public TableFontHelper(TMP_FontAsset fontAsset, float targetFontSize)
        {
            _fontAsset = fontAsset;
            _targetFontSize = targetFontSize;
            if (_fontAsset != null && _fontAsset.faceInfo.pointSize > 0)
                _metricScale = targetFontSize / _fontAsset.faceInfo.pointSize * _fontAsset.faceInfo.scale;
            else
                _metricScale = 1f;
        }

        #endregion

        #region 预缓存

        /// <summary>预缓存指定字符串中的所有字符，避免运行时按需加载卡顿。</summary>
        public void PreCache(string chars)
        {
            foreach (char c in chars)
            {
                if (!_cache.ContainsKey(c))
                    CacheChar(c);
            }
        }

        /// <summary>预缓存 ASCII 可打印字符（32-126，共 95 个）。</summary>
        public void PreCacheAscii()
        {
            for (int i = 32; i <= 126; i++)
                CacheChar((char)i);
        }

        #endregion

        #region 字形查询

        /// <summary>
        /// 获取字符的 SDF 字形度量与 UV 坐标。
        /// 首次查询时按需从 TMP_FontAsset 加载并缓存。
        /// 返回 false 表示字符不在当前字体资产的字符集中。
        /// </summary>
        public bool TryGetGlyph(char c,
            out float advance, out float bearingX, out float bearingY,
            out float width, out float height,
            out Vector2 uvBL, out Vector2 uvTR)
        {
            if (!_cache.TryGetValue(c, out GlyphInfo info))
            {
                if (!CacheChar(c))
                {
                    advance = 0; bearingX = 0; bearingY = 0;
                    width = 0; height = 0;
                    uvBL = Vector2.zero; uvTR = Vector2.zero;
                    return false;
                }
                info = _cache[c];
            }

            advance = info.advance;
            bearingX = info.minX;
            bearingY = info.maxY;
            width = info.glyphWidth;
            height = info.glyphHeight;
            uvBL = info.uvBottomLeft;
            uvTR = info.uvTopRight;
            return true;
        }

        /// <summary>
        /// 从 TMP_FontAsset 的字符查找表读取字形的 SDF 图集坐标和度量信息，
        /// 将 design-unit 值按缩放因子转换为目标字号的像素值后缓存。
        /// </summary>
        bool CacheChar(char c)
        {
            if (_fontAsset == null) return false;

            if (!_fontAsset.characterLookupTable.TryGetValue(c, out TMP_Character tmpChar))
                return false;

            if (!_fontAsset.glyphLookupTable.TryGetValue(tmpChar.glyphIndex, out Glyph glyph))
                return false;

            float aw = _fontAsset.atlasWidth;
            float ah = _fontAsset.atlasHeight;
            float rx = glyph.glyphRect.x;
            float ry = glyph.glyphRect.y;
            float rw = glyph.glyphRect.width;
            float rh = glyph.glyphRect.height;

            float sc = _metricScale;

            _cache[c] = new GlyphInfo
            {
                uvBottomLeft = new Vector2(rx / aw, ry / ah),
                uvTopRight   = new Vector2((rx + rw) / aw, (ry + rh) / ah),
                advance      = glyph.metrics.horizontalAdvance * sc,
                minX         = glyph.metrics.horizontalBearingX * sc,
                maxY         = glyph.metrics.horizontalBearingY * sc,
                glyphWidth   = glyph.metrics.width * sc,
                glyphHeight  = glyph.metrics.height * sc,
            };
            return true;
        }

        #endregion
    }
}
