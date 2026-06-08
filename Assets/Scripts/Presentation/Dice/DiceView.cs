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
        private Image _triggerFlash;   // 「效果觸發」一次性光爆閃（顏色依效果類型區分）
        private Coroutine _triggerFlashCo;
        private Image _lockOverlay;
        private Image _wildIcon;             // 萬用骰標記：D6 用生成圖示
        private TextMeshProUGUI _wildText;   // 萬用骰標記：D12/D20（無臉圖）退回顯示 "W"
        private Image _forecastGlow;   // 重擲預報金色脈動
        private bool _forecastActive;
        private Image _selectRing;     // 「選取重轉」白色外框
        private TextMeshProUGUI _valueText;

        private DiceSkin _skin;
        private int _maxFace = 6;
        private float _pixelSize = 120f;
        private bool _built;

        /// <summary>建立子物件並套用外觀。可重複呼叫（僅首次建構）。</summary>
        public void Build(DiceSkin skin, int maxFace, float pixelSize = 120f)
        {
            _skin = skin ?? new DiceSkin();
            _maxFace = maxFace;
            _pixelSize = pixelSize;
            EnsureBuilt(pixelSize);
            ApplySkin();
        }

        public void SetDiceType(int maxFace)
        {
            _maxFace = maxFace;
        }

        // D6 且備齊 6 張面圖才用貼圖模式；D12/D20 一律用框+數字
        private bool HasFaces =>
            _maxFace == 6 &&
            _skin?.Faces != null &&
            _skin.Faces.Length >= 6 &&
            _skin.Faces[0] != null;

        private Sprite GetFrameSprite()
        {
            if (_maxFace == 12 && _skin.FrameD12 != null) return _skin.FrameD12;
            if (_maxFace == 20 && _skin.FrameD20 != null) return _skin.FrameD20;
            return _skin.Frame != null
                ? _skin.Frame
                : PlaceholderArt.RoundedFrame(_skin.FrameTint, new Color(0.1f, 0.05f, 0.05f));
        }

        private void EnsureBuilt(float pixelSize)
        {
            if (_built) return;

            _rect = GetComponent<RectTransform>();
            if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();
            _rect.sizeDelta = new Vector2(pixelSize, pixelSize);

            _frame = UiFactory.Image("Frame", transform, null, Color.white);
            UiFactory.Stretch((RectTransform)_frame.transform);
            _frame.raycastTarget = true;

            _valueText = UiFactory.Text("Value", transform, "", pixelSize * 0.5f,
                Color.black);
            UiFactory.Stretch((RectTransform)_valueText.transform);

            _abyssOverlay = UiFactory.Image("AbyssGlow", transform, null, Color.clear);
            UiFactory.Stretch((RectTransform)_abyssOverlay.transform);
            _abyssOverlay.enabled = false;

            // 「效果觸發」一次性光爆閃：白色光暈貼圖 + 執行時依效果類型上色，標示「這裡發生了變化」
            _triggerFlash = UiFactory.Image("TriggerFlash", transform,
                PlaceholderArt.RadialGlow(Color.white), new Color(1f, 1f, 1f, 0f));
            UiFactory.Center((RectTransform)_triggerFlash.transform,
                new Vector2(pixelSize * 1.7f, pixelSize * 1.7f));
            _triggerFlash.transform.SetAsFirstSibling();
            _triggerFlash.raycastTarget = false;
            _triggerFlash.enabled = false;

            // 重擲預報用的金色光暈（置於最底層，當作骰子外圍光暈）
            _forecastGlow = UiFactory.Image("ForecastGlow", transform,
                PlaceholderArt.RadialGlow(new Color(1f, 0.85f, 0.3f)),
                new Color(1f, 0.85f, 0.3f, 0f));
            UiFactory.Center((RectTransform)_forecastGlow.transform,
                new Vector2(pixelSize * 1.6f, pixelSize * 1.6f));
            _forecastGlow.transform.SetAsFirstSibling();
            _forecastGlow.raycastTarget = false;
            _forecastGlow.enabled = false;

            _lockOverlay = UiFactory.Image("LockMark", transform, null, Color.white);
            RectTransform lr = (RectTransform)_lockOverlay.transform;
            UiFactory.Anchor(lr, new Vector2(1f, 1f),
                new Vector2(pixelSize * 0.3f, pixelSize * 0.3f),
                new Vector2(-4f, -4f));
            _lockOverlay.enabled = false;

            // 「萬用骰」標記徽章（左上角，與右上角鎖定圖互不重疊）：
            // D6 用美術提供的 dice_wild 圖示；D12/D20（無臉圖）退回顯示 "W" 文字
            _wildIcon = UiFactory.Image("WildIcon", transform, null, Color.white);
            RectTransform wir = (RectTransform)_wildIcon.transform;
            UiFactory.Anchor(wir, new Vector2(0f, 1f),
                new Vector2(pixelSize * 0.34f, pixelSize * 0.34f),
                new Vector2(4f, -4f));
            _wildIcon.raycastTarget = false;
            _wildIcon.enabled = false;

            _wildText = UiFactory.Text("WildText", transform, "W",
                pixelSize * 0.34f, new Color(0.78f, 0.34f, 0.97f));
            RectTransform wtr = (RectTransform)_wildText.transform;
            UiFactory.Anchor(wtr, new Vector2(0f, 1f),
                new Vector2(pixelSize * 0.34f, pixelSize * 0.34f),
                new Vector2(4f, -4f));
            _wildText.fontStyle = FontStyles.Bold;
            _wildText.raycastTarget = false;
            _wildText.enabled = false;

            // 「選取重轉」白色外框（疊在最上層）
            _selectRing = UiFactory.Image("SelectRing", transform,
                PlaceholderArt.RoundedRect(Color.clear, new Color(0.96f, 0.96f, 1f), 96, 22, 5),
                Color.white);
            _selectRing.type = Image.Type.Sliced;
            UiFactory.Stretch((RectTransform)_selectRing.transform);
            _selectRing.raycastTarget = false;
            _selectRing.enabled = false;

            _built = true;
        }

        private void ApplySkin()
        {
            if (HasFaces)
            {
                _frame.sprite = _skin.Faces[0];
                _frame.color = Color.white;
                _valueText.enabled = false;
            }
            else
            {
                _frame.sprite = GetFrameSprite();
                _frame.color = Color.white;
                _valueText.enabled = true;
                _valueText.color = _skin.NumberColor;
                // D12/D20 有兩位數，縮小字體避免超出骰框
                _valueText.fontSize = _maxFace <= 6 ? _pixelSize * 0.5f : _pixelSize * 0.3f;
            }

            _abyssOverlay.sprite = _skin.Abyss != null
                ? _skin.Abyss
                : PlaceholderArt.RadialGlow(_skin.AbyssGlow);

            _lockOverlay.sprite = _skin.Locked != null
                ? _skin.Locked
                : PlaceholderArt.SolidSprite(new Color(0.1f, 0.1f, 0.12f, 0.9f));
        }

        /// <summary>開始擲骰動畫，最終停在 finalValue；abyss 為真則顯示深淵光效。</summary>
        public void PlayRoll(int finalValue, bool abyssTriggered)
        {
            if (!_built) Build(_skin, _maxFace);
            if (!gameObject.activeInHierarchy) { ShowImmediate(finalValue, abyssTriggered); return; }
            StopAllCoroutines();
            StartCoroutine(RollRoutine(finalValue, abyssTriggered));
        }

        /// <summary>
        /// 播放擲骰動畫，但全程維持「未揭露」外觀（? + 半透明框），
        /// 結束時仍停在隱藏狀態，永不顯示真實點數。供 AI（撒旦）擲骰用。
        /// </summary>
        public void PlayRollHidden()
        {
            if (!_built) Build(_skin, _maxFace);
            if (!gameObject.activeInHierarchy) { ShowHidden(); return; }
            StopAllCoroutines();
            StartCoroutine(RollHiddenRoutine());
        }

        /// <summary>不播動畫，直接顯示點數（AI 揭露 / 重整畫面用）。</summary>
        public void ShowImmediate(int value, bool abyssTriggered = false)
        {
            if (!_built) Build(_skin, _maxFace);
            CurrentValue = value;
            SetFaceOrText(value);
            _rect.localRotation = Quaternion.identity;
            _rect.localScale = Vector3.one;
            _abyssOverlay.enabled = abyssTriggered;
            IsAnimating = false;
        }

        /// <summary>顯示為未揭露狀態（AI 隱藏點數）。</summary>
        public void ShowHidden()
        {
            if (!_built) Build(_skin, _maxFace);
            SetHiddenVisual();
            _rect.localRotation = Quaternion.identity;
            _rect.localScale = Vector3.one;
            _abyssOverlay.enabled = false;
        }

        /// <summary>僅套用「未揭露」外觀（? + 半透明框），不動 transform。</summary>
        private void SetHiddenVisual()
        {
            if (HasFaces)
            {
                _frame.sprite = _skin.Faces[0];
                _frame.color = new Color(1f, 1f, 1f, 0.4f);
                _valueText.enabled = true;
                _valueText.text = "?";
                _valueText.color = Color.white;
            }
            else
            {
                _frame.sprite = GetFrameSprite();
                _frame.color = Color.white;
                _valueText.enabled = true;
                _valueText.text = "?";
                _valueText.color = _skin.NumberColor;
            }
        }

        private void SetFaceOrText(int value)
        {
            if (HasFaces)
            {
                int idx = Mathf.Clamp(value - 1, 0, _skin.Faces.Length - 1);
                _frame.sprite = _skin.Faces[idx];
                _frame.color = Color.white;
                _valueText.enabled = false;
            }
            else
            {
                _frame.sprite = GetFrameSprite();
                _frame.color = Color.white;
                _valueText.enabled = true;
                _valueText.text = value.ToString();
            }
        }

        public void SetLocked(bool locked)
        {
            IsLocked = locked;
            if (_lockOverlay != null) _lockOverlay.enabled = locked;
        }

        /// <summary>
        /// 開關「萬用骰」標記徽章（左上角）。
        /// 有臉圖（D6）且美術圖已就位時顯示 dice_wild 圖示；
        /// 無臉圖（D12/D20）或圖未就位時退回顯示 "W" 文字。
        /// </summary>
        public void SetWildMarker(bool on)
        {
            if (!_built) return;
            bool useIcon = on && HasFaces && _skin != null && _skin.WildIcon != null;
            if (_wildIcon != null)
            {
                if (useIcon) _wildIcon.sprite = _skin.WildIcon;
                _wildIcon.enabled = useIcon;
            }
            if (_wildText != null) _wildText.enabled = on && !useIcon;
        }

        /// <summary>開關「選取重轉」白色外框。</summary>
        public void SetRerollSelected(bool on)
        {
            if (_selectRing != null) _selectRing.enabled = on;
        }

        /// <summary>壓灰（結算時未參與牌型的骰子用）。還原為正常顯示傳 false。</summary>
        public void SetDim(bool dim)
        {
            if (!_built) return;
            if (dim)
            {
                _frame.color = new Color(0.42f, 0.42f, 0.46f, 0.5f);
                if (_valueText.enabled) _valueText.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            }
            else
            {
                _frame.color = Color.white;
                if (_valueText.enabled) _valueText.color = _skin.NumberColor;
            }
        }

        /// <summary>開關「重擲預報」金色脈動高亮（在 Update 內做 alpha 呼吸）。</summary>
        public void SetForecast(bool on)
        {
            _forecastActive = on;
            if (_forecastGlow == null) return;
            _forecastGlow.enabled = on;
            if (!on)
            {
                Color c = _forecastGlow.color;
                c.a = 0f;
                _forecastGlow.color = c;
            }
        }

        /// <summary>
        /// 播放一次性「效果觸發」光爆閃（依效果類型上色），標示這顆骰子發生了實際變化。
        /// 用於深淵效果生效當下提示玩家「變更發生在這裡」。
        /// </summary>
        public void PlayTriggerFlash(Color color)
        {
            if (!_built) return;
            if (_triggerFlashCo != null) StopCoroutine(_triggerFlashCo);
            _triggerFlashCo = StartCoroutine(TriggerFlashRoutine(color));
        }

        private IEnumerator TriggerFlashRoutine(Color color)
        {
            const float dur = 0.7f;
            float t = 0f;
            _triggerFlash.enabled = true;
            while (t < dur)
            {
                t += Time.deltaTime;
                float burst = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI); // 0 → 1 → 0
                Color c = color;
                c.a = burst;
                _triggerFlash.color = c;
                yield return null;
            }
            _triggerFlash.enabled = false;
            _triggerFlashCo = null;
        }

        private void Update()
        {
            if (_forecastActive && _forecastGlow != null)
            {
                Color c = _forecastGlow.color;
                c.a = 0.35f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 3f));
                _forecastGlow.color = c;
            }
        }

        private IEnumerator RollRoutine(int finalValue, bool abyssTriggered)
        {
            IsAnimating = true;
            _abyssOverlay.enabled = false;

            // --- 播放：抖動 + 翻轉 + 面/數字亂跳 ---
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
                    if (HasFaces)
                    {
                        _frame.sprite = _skin.Faces[Random.Range(0, _skin.Faces.Length)];
                        _frame.color = Color.white;
                    }
                    else
                    {
                        _valueText.text = Random.Range(1, _maxFace + 1).ToString();
                    }
                    flicker = FlickerInterval;
                }
                yield return null;
            }

            // --- 收尾：旋轉歸零、ScaleX 回 1，鎖定最終點數 ---
            SetFaceOrText(finalValue);
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

        private IEnumerator RollHiddenRoutine()
        {
            IsAnimating = true;
            _abyssOverlay.enabled = false;
            SetHiddenVisual(); // 整段動畫都維持 ? 外觀，不顯示真實點數

            // --- 播放：抖動 + 翻轉（不變更顯示內容）---
            float t = 0f;
            while (t < SpinDuration)
            {
                t += Time.deltaTime;
                _rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-180f, 180f));
                float sx = Mathf.Abs(Mathf.Sin(t * SpinSpeed));
                _rect.localScale = new Vector3(Mathf.Max(0.12f, sx), 1f, 1f);
                yield return null;
            }

            // --- 收尾：旋轉歸零、ScaleX 回 1 ---
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
