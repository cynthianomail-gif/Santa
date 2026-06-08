using UnityEngine;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 以程式產生佔位 Sprite，讓專案在 AI 美術尚未匯入時即可運作。
    /// 正式美術匯入後，於 Inspector 指派 DiceSkin / Background 即會覆蓋這些佔位圖。
    /// </summary>
    public static class PlaceholderArt
    {
        /// <summary>純色方塊 Sprite。</summary>
        public static Sprite SolidSprite(Color color, int size = 8)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>帶簡單描邊的圓角方塊，用作骰子外框佔位。</summary>
        public static Sprite RoundedFrame(Color fill, Color border, int size = 64, int borderPx = 4)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < borderPx || y < borderPx ||
                                x >= size - borderPx || y >= size - borderPx;
                    tex.SetPixel(x, y, edge ? border : fill);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// 填滿的圓角矩形 Sprite，帶描邊、邊緣抗鋸齒，外部透明。
        /// 回傳的 Sprite 內建 9-slice border（= radius），搭配 Image.Type.Sliced
        /// 可任意拉伸而保持圓角。適合做結算卡片等面板背景。
        /// </summary>
        public static Sprite RoundedRect(Color fill, Color border,
            int size = 96, int radius = 24, int borderPx = 3)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = RoundedInsideDistance(x, y, size, radius); // >0 在形狀內
                    float alpha = Mathf.Clamp01(dist);                      // 外緣抗鋸齒
                    if (alpha <= 0f) { tex.SetPixel(x, y, Color.clear); continue; }
                    Color baseCol = dist < borderPx ? border : fill;
                    tex.SetPixel(x, y,
                        new Color(baseCol.r, baseCol.g, baseCol.b, baseCol.a * alpha));
                }
            }
            tex.Apply();
            float b = radius;
            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size, 0,
                SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        /// <summary>圓角矩形的「到邊緣的有號距離」，形狀內為正、外為負。</summary>
        private static float RoundedInsideDistance(int x, int y, int size, int radius)
        {
            float half = size * 0.5f;
            float px = x + 0.5f - half;
            float py = y + 0.5f - half;
            float bx = half - radius;
            float by = half - radius;
            float dx = Mathf.Abs(px) - bx;
            float dy = Mathf.Abs(py) - by;
            float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f)
                                     + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
            float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
            float sdf = outside + inside - radius; // <0 在形狀內
            return -sdf;                           // >0 在形狀內
        }

        /// <summary>正圓形 Sprite，帶一圈邊框。</summary>
        public static Sprite CircleSprite(Color fill, Color rim, int size = 128, int rimPx = 6)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float outerR = size * 0.5f - 1f;
            float innerR = outerR - rimPx;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    if (d > outerR)      tex.SetPixel(x, y, Color.clear);
                    else if (d > innerR) tex.SetPixel(x, y, rim);
                    else                 tex.SetPixel(x, y, fill);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>放射狀光暈，用作深淵觸發疊加佔位。</summary>
        public static Sprite RadialGlow(Color color, int size = 64)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float maxD = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / maxD;
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, a * color.a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
