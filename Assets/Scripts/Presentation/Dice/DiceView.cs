using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 單顆骰子的視覺呈現。以「方案 B — 旋轉 + 縮放偽 3D」實作擲骰動畫，
    /// 點數由 TextMeshPro 繪製（不需逐面貼圖）。自我建構子物件，
    /// 因此 SceneBootstrap 只需 AddComponent 並呼叫 Build。
    ///
    /// 動畫時序（純程式驅動）：
    ///   播放 0.8s：RotationZ 快速隨機抖動 + ScaleX = |sin| 模擬翻轉消失再出現，數字快速亂跳
    ///   收尾 0.2s：ease-out 旋轉歸零、ScaleX 回 1，鎖定最終點數
    ///   落地 0.15s：ScaleY squash & stretch 反彈
    ///   結果：深淵觸發時疊上光效
    /// </summary>
    public sealed class DiceView : MonoBehaviour
    {
        [Header("Timing")]
        public float SpinDuration = 0.8f;
        public float SettleDuration = 0.2f;
        public float LandDuration = 0.15f;
        public float SpinSpeed = 15f;
        public float FlickerInterval = 0.05f;

        public bool IsAnimating { get; private set; }
        public bool IsLocked { get; private set; }
        public int CurrentValue { get; private set; }

        /// <summary>外框圖，供外部設定點擊用的 Button target。</summary>
        public Image FrameImage { get { return _frame; } }

        private RectTransform _rect;
        private Image _frame;
        private Image _abyssOverlay;
        private Image _lockOverlay;
        private TextMeshProUGUI _valueText;

        private DiceSkin _skin;
        private int _maxFace = 6;
        private bool _built;

        /// <summary>建立子物件並套用外觀。可重複呼叫（僅首次建構）。</summary>
        public void Build(DiceSkin skin, int maxFace, float pixelSize = 120f)
        {
            _skin = skin ?? new DiceSkin();
            _maxFace = maxFace;
            EnsureBuilt(pixelSize);
            ApplySkin();
        }

        public void SetDiceType(int maxFace)
        {
            _maxFace = maxFace;
        }

        private void EnsureBuilt(float pixelSize)
        {
            if (_built) return;

            _rect = GetComponent<RectTransform>();
            if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();
            _rect.sizeDelta = new Vector2(pixelSize, pixelSize);

            _frame = UiFactory.Image("Frame", transform, null, Color.white);
            UiFactory.Stretch((RectTransform)_frame.transform);
            _frame.raycastTarget = true; // 接收鎖定點擊

            _valueText = UiFactory.Text("Value", transform, "", pixelSize * 0.5f,
                Color.black);
            UiFactory.Stretch((RectTransform)_valueText.transform);

            _abyssOverlay = UiFactory.Image("AbyssGlow", transform, null, Color.clear);
            UiFactory.Stretch((RectTransform)_abyssOverlay.transform);
            _abyssOverlay.enabled = false;

            _lockOverlay = UiFactory.Image("LockMark", transform, null, Color.white);
            RectTransform lr = (RectTransform)_lockOverlay.transform;
            UiFactory.Anchor(lr, new Vector2(1f, 1f),
                new Vector2(pixelSize * 0.3f, pixelSize * 0.3f),
                new Vector2(-4f, -4f));
            _lockOverlay.enabled = false;

            _built = true;
        }

        private void ApplySkin()
        {
            _frame.sprite = _skin.Frame != null
                ? _skin.Frame
                : PlaceholderArt.RoundedFrame(_skin.FrameTint, new Color(0.1f, 0.05f, 0.05f));
            _frame.color = Color.white;

            _abyssOverlay.sprite = _skin.Abyss != null
                ? _skin.Abyss
                : PlaceholderArt.RadialGlow(_skin.AbyssGlow);

            _lockOverlay.sprite = _skin.Locked != null
                ? _skin.Locked
                : PlaceholderArt.SolidSprite(new Color(0.1f, 0.1f, 0.12f, 0.9f));

            _valueText.color = _skin.NumberColor;
        }

        /// <summary>開始擲骰動畫，最終停在 finalValue；abyss 為真則顯示深淵光效。</summary>
        public void PlayRoll(int finalValue, bool abyssTriggered)
        {
            if (!_built) Build(_skin, _maxFace);
            if (!gameObject.activeInHierarchy) { ShowImmediate(finalValue, abyssTriggered); return; }
            StopAllCoroutines();
            StartCoroutine(RollRoutine(finalValue, abyssTriggered));
        }

        /// <summary>不播動畫，直接顯示點數（AI 揭露 / 重整畫面用）。</summary>
        public void ShowImmediate(int value, bool abyssTriggered = false)
        {
            if (!_built) Build(_skin, _maxFace);
            CurrentValue = value;
            _valueText.text = value.ToString();
            _rect.localRotation = Quaternion.identity;
            _rect.localScale = Vector3.one;
            _abyssOverlay.enabled = abyssTriggered;
            IsAnimating = false;
        }

        /// <summary>顯示為未揭露狀態（AI 隱藏點數）。</summary>
        public void ShowHidden()
        {
            if (!_built) Build(_skin, _maxFace);
            _valueText.text = "?";
            _rect.localRotation = Quaternion.identity;
            _rect.localScale = Vector3.one;
            _abyssOverlay.enabled = false;
        }

        public void SetLocked(bool locked)
        {
            IsLocked = locked;
            if (_lockOverlay != null) _lockOverlay.enabled = locked;
        }

        private IEnumerator RollRoutine(int finalValue, bool abyssTriggered)
        {
            IsAnimating = true;
            _abyssOverlay.enabled = false;

            // --- 播放：抖動 + 翻轉 + 數字亂跳 ---
            float t = 0f;
            float flicker = 0f;
            while (t < SpinDuration)
            {
                t += Time.deltaTime;
                _rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-180f, 180f));
                float sx = Mathf.Abs(Mathf.Sin(t * SpinSpeed));
                _rect.localScale = new Vector3(Mathf.Max(0.12f, sx), 1f, 1f);

                flicker -= Time.deltaTime;
                if (flicker <= 0f)
                {
                    _valueText.text = Random.Range(1, _maxFace + 1).ToString();
                    flicker = FlickerInterval;
                }
                yield return null;
            }

            // --- 收尾：旋轉歸零、ScaleX 回 1，鎖定最終點數 ---
            _valueText.text = finalValue.ToString();
            CurrentValue = finalValue;
            float startZ = NormalizeAngle(_rect.localEulerAngles.z);
            float startSx = _rect.localScale.x;
            t = 0f;
            while (t < SettleDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / SettleDuration);
                _rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startZ, 0f, k));
                _rect.localScale = new Vector3(Mathf.Lerp(startSx, 1f, k), 1f, 1f);
                yield return null;
            }
            _rect.localRotation = Quaternion.identity;
            _rect.localScale = Vector3.one;

            // --- 落地：squash & stretch ---
            t = 0f;
            while (t < LandDuration)
            {
                t += Time.deltaTime;
                float k = t / LandDuration;
                float squash = 1f - Mathf.Sin(k * Mathf.PI) * 0.15f;
                _rect.localScale = new Vector3(2f - squash, squash, 1f);
                yield return null;
            }
            _rect.localScale = Vector3.one;

            // --- 結果 ---
            if (abyssTriggered) _abyssOverlay.enabled = true;
            IsAnimating = false;
        }

        private static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            return a;
        }
    }
}
