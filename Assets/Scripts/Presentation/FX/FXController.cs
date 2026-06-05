using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using AbyssProtocol.Core;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 特效控制：全螢幕墨水閃現（紅/黑）、深淵效果回饋、撒旦荷官之手的浮動 Tween。
    /// </summary>
    public sealed class FXController : MonoBehaviour
    {
        public Image InkOverlay;       // 全螢幕，預設透明
        public RectTransform DealerHand;

        [Header("Dealer hover")]
        public float HoverAmplitude = 12f;
        public float HoverSpeed = 1.5f;

        private Vector2 _dealerBase;
        private bool _hasDealer;

        private static readonly Color InkRed = new Color(0.55f, 0.02f, 0.04f, 1f);
        private static readonly Color InkBlack = new Color(0.02f, 0.01f, 0.02f, 1f);

        private void Start()
        {
            if (DealerHand != null)
            {
                _dealerBase = DealerHand.anchoredPosition;
                _hasDealer = true;
            }
            if (InkOverlay != null)
            {
                Color c = InkOverlay.color;
                c.a = 0f;
                InkOverlay.color = c;
                InkOverlay.raycastTarget = false;
            }
        }

        private void Update()
        {
            if (_hasDealer)
            {
                float y = _dealerBase.y + Mathf.Sin(Time.time * HoverSpeed) * HoverAmplitude;
                DealerHand.anchoredPosition = new Vector2(_dealerBase.x, y);
            }
        }

        public void PlayResult(Winner winner)
        {
            Flash(winner == Winner.Player ? InkRed : InkBlack, 0.5f);
        }

        public void PlayFGTransition()
        {
            Flash(InkRed, 1.0f);
        }

        public void PlayAbyss(AbyssEffect effect)
        {
            // 各效果以不同濃度的墨水短閃做回饋。
            Flash(InkRed, 0.3f);
        }

        public void Flash(Color color, float duration)
        {
            if (InkOverlay == null) return;
            StopAllCoroutines();
            StartCoroutine(FlashRoutine(color, duration));
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - (t / duration);          // 由濃轉淡
                Color c = color;
                c.a = Mathf.Clamp01(k) * 0.85f;
                InkOverlay.color = c;
                yield return null;
            }
            Color end = color;
            end.a = 0f;
            InkOverlay.color = end;
        }
    }
}
