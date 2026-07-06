using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;
using TMPro;

namespace WPZ0325.EasyTableGPU
{
    public class TableFontHelper
    {
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

        TMP_FontAsset _fontAsset;
        float _targetFontSize;
        float _metricScale;
        Dictionary<char, GlyphInfo> _cache = new Dictionary<char, GlyphInfo>();

        public Texture FontTexture => _fontAsset?.atlasTexture;
        public TMP_FontAsset FontAsset => _fontAsset;

        public TableFontHelper(TMP_FontAsset fontAsset, float targetFontSize)
        {
            _fontAsset = fontAsset;
            _targetFontSize = targetFontSize;
            if (_fontAsset != null && _fontAsset.faceInfo.pointSize > 0)
                _metricScale = targetFontSize / _fontAsset.faceInfo.pointSize * _fontAsset.faceInfo.scale;
            else
                _metricScale = 1f;
        }

        public void PreCache(string chars)
        {
            foreach (char c in chars)
            {
                if (!_cache.ContainsKey(c))
                    CacheChar(c);
            }
        }

        public void PreCacheAscii()
        {
            for (int i = 32; i <= 126; i++)
                CacheChar((char)i);
        }

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
    }
}
