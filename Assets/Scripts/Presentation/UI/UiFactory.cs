using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 以程式建構 uGUI 元件的輔助工廠，讓 SceneBootstrap 維持可讀。
    /// </summary>
    public static class UiFactory
    {
        public static RectTransform Panel(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Center((RectTransform)go.transform, new Vector2(100f, 100f));
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string content,
            float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Center((RectTransform)go.transform, new Vector2(700f, 80f));
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.enableWordWrapping = false;
            return t;
        }

        public static Button Button(string name, Transform parent, string label,
            Vector2 size, Action onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            Center(rt, size);

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.04f, 0.06f, 0.95f);

            Button btn = go.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = new Color(1f, 1f, 1f, 1f);
            cb.highlightedColor = new Color(1.0f, 0.75f, 0.75f, 1f);
            cb.pressedColor     = new Color(0.6f, 0.4f, 0.4f, 1f);
            cb.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            cb.colorMultiplier  = 1f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            TextMeshProUGUI t = Text("Label", rt, label, 28f,
                new Color(0.9f, 0.85f, 0.78f));
            Stretch((RectTransform)t.transform);

            return btn;
        }

        /// <summary>水平排列容器（用於骰子列、按鈕列）。</summary>
        public static RectTransform Row(string name, Transform parent, float spacing,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            HorizontalLayoutGroup h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = false;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return (RectTransform)go.transform;
        }

        // ---- RectTransform 錨點輔助 ----

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>置中錨點（anchoredPosition 以父物件中心為原點）。</summary>
        public static void Center(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 anchoredPos)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
        }
    }
}
