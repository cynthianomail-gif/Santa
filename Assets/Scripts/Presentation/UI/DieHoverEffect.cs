using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// FG 選骰畫面的骰子互動效果：滑鼠移入放大 + 光暈顯示，移出縮回 + 光暈隱藏。
    /// </summary>
    public sealed class DieHoverEffect : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        public Image GlowImage;

        private RectTransform _rt;
        private Vector3 _baseScale;
        private Coroutine _anim;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _baseScale = _rt.localScale;
        }

        public void OnPointerEnter(PointerEventData _)
        {
            Animate(1.18f);
            if (GlowImage) GlowImage.enabled = true;
        }

        public void OnPointerExit(PointerEventData _)
        {
            Animate(1f);
            if (GlowImage) GlowImage.enabled = false;
        }

        private void Animate(float targetScale)
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(ScaleRoutine(_baseScale * targetScale, 0.12f));
        }

        private IEnumerator ScaleRoutine(Vector3 target, float duration)
        {
            Vector3 start = _rt.localScale;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _rt.localScale = Vector3.Lerp(start, target, t / duration);
                yield return null;
            }
            _rt.localScale = target;
        }
    }
}
