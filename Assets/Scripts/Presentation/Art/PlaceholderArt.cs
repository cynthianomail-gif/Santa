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
