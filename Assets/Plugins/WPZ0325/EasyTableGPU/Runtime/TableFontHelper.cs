using System.Collections.Generic;
using UnityEngine;

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

        Font _font;
        int _fontSize;
        Dictionary<char, GlyphInfo> _cache = new Dictionary<char, GlyphInfo>();

        public Font Font => _font;
        public Texture FontTexture => _font.material.mainTexture;
        public int FontSize => _fontSize;

        public TableFontHelper(Font font, int fontSize)
        {
            _font = font;
            _fontSize = fontSize;
        }

        public void PreCache(string chars)
        {
            _font.RequestCharactersInTexture(chars, _fontSize);
            foreach (char c in chars)
            {
                if (!_cache.ContainsKey(c))
                    CacheChar(c);
            }
        }

        public void PreCacheAscii()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(95);
            for (int i = 32; i <= 126; i++)
                sb.Append((char)i);
            PreCache(sb.ToString());
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
            _font.RequestCharactersInTexture(c.ToString(), _fontSize);

            if (!_font.GetCharacterInfo(c, out CharacterInfo ci, _fontSize))
                return false;

            _cache[c] = new GlyphInfo
            {
                uvBottomLeft = ci.uvBottomLeft,
                uvTopRight   = ci.uvTopRight,
                advance      = ci.advance,
                minX         = ci.minX,
                maxY         = ci.maxY,
                glyphWidth   = ci.glyphWidth,
                glyphHeight  = ci.glyphHeight,
            };
            return true;
        }
    }
}
