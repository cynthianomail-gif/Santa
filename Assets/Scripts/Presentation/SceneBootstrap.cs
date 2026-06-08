using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AbyssProtocol.Core;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 以程式建構整個遊戲場景。
    /// 底部固定工具列（餘額 / 模式 / 骰子 / 押注 / Spin / Auto），
    /// 骰子區常駐，結算/FG 以 Overlay 呈現。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SceneBootstrap : MonoBehaviour
    {
        [Header("Optional art overrides (留空則用程式佔位圖)")]
        public Sprite BackgroundSprite;
        public Sprite DealerHandSprite;
        public DiceSkin DiceSkin = new DiceSkin();

        private GameManager _gm;
        private UIManager _ui;
        private DiceRollerController _roller;
        private ResultDisplay _result;
        private FXController _fx;

        private static readonly Color Ink    = new Color(0.9f,  0.85f, 0.78f);
        private static readonly Color Dim    = new Color(0.7f,  0.65f, 0.60f);
        private static readonly Color BarBg  = new Color(0.04f, 0.02f, 0.02f, 0.97f);
        private static readonly Color WgtBg  = new Color(0.13f, 0.05f, 0.06f, 1f);
        private static readonly Color Crimson = new Color(0.48f, 0.03f, 0.05f, 1f);

        private const float BarH = 100f;

        private void Awake()
        {
            ResolveArtFromResources();
            EnsureEventSystem();
            RectTransform canvas = BuildCanvas();

            GameObject systems = new GameObject("Systems");
            _ui     = systems.AddComponent<UIManager>();
            _roller = systems.AddComponent<DiceRollerController>();
            _result = systems.AddComponent<ResultDisplay>();
            _fx     = systems.AddComponent<FXController>();
            _gm     = systems.AddComponent<GameManager>();
            _roller.Skin = DiceSkin;

            BuildBackground(canvas);
            BuildDiceArea(canvas);
            BuildRerollArea(canvas);
            BuildInkOverlay(canvas);
            BuildResultPanel(canvas);
            BuildAbyssBanner(canvas);
            BuildBottomBar(canvas);

            _gm.Roller = _roller;
            _gm.UI     = _ui;
            _gm.Result = _result;
            _gm.FX     = _fx;
        }

        // ──────────────────────────────────────────
        // 美術自動載入
        // ──────────────────────────────────────────

        private void ResolveArtFromResources()
        {
            if (DiceSkin == null) DiceSkin = new DiceSkin();
            if (BackgroundSprite == null)  BackgroundSprite  = Resources.Load<Sprite>("background");
            if (DealerHandSprite == null)  DealerHandSprite  = Resources.Load<Sprite>("dealer_hand");
            if (DiceSkin.Frame   == null)  DiceSkin.Frame    = Resources.Load<Sprite>("dice_frame");
            if (DiceSkin.FrameD12 == null) DiceSkin.FrameD12 = Resources.Load<Sprite>("dice_frame_d12");
            if (DiceSkin.FrameD20 == null) DiceSkin.FrameD20 = Resources.Load<Sprite>("dice_frame_d20");
            if (DiceSkin.Locked  == null)  DiceSkin.Locked   = Resources.Load<Sprite>("dice_locked");
            if (DiceSkin.Abyss   == null)  DiceSkin.Abyss    = Resources.Load<Sprite>("dice_abyss");
            if (DiceSkin.WildIcon == null) DiceSkin.WildIcon = Resources.Load<Sprite>("dice_wild");
            if (DiceSkin.Faces   == null)  DiceSkin.Faces    = new Sprite[6];
            for (int i = 0; i < 6; i++)
                if (DiceSkin.Faces[i] == null)
                    DiceSkin.Faces[i] = Resources.Load<Sprite>("face_" + (i + 1));

            if (DiceSkin.SpecialDouble == null) DiceSkin.SpecialDouble = Resources.Load<Sprite>("special_double");
            if (DiceSkin.SpecialCursed == null) DiceSkin.SpecialCursed = Resources.Load<Sprite>("special_cursed");
        }

        // ──────────────────────────────────────────
        // 基礎建構
        // ──────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static RectTransform BuildCanvas()
        {
            GameObject go = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920f, 1080f);
            sc.matchWidthOrHeight = 0.5f;
            return (RectTransform)go.transform;
        }

        // ──────────────────────────────────────────
        // 場景層
        // ──────────────────────────────────────────

        private void BuildBackground(RectTransform canvas)
        {
            Image bg = UiFactory.Image("Background", canvas,
                BackgroundSprite ?? PlaceholderArt.SolidSprite(new Color(0.06f, 0.05f, 0.07f)),
                Color.white);
            UiFactory.Stretch((RectTransform)bg.transform);

            Image dealer = UiFactory.Image("DealerHand", canvas,
                DealerHandSprite ?? PlaceholderArt.RoundedFrame(
                    new Color(0.3f, 0.08f, 0.1f), new Color(0.05f, 0.02f, 0.03f)),
                Color.white);
            dealer.preserveAspect = true;
            // 縮小並上移，讓手不要壓到 AI 骰列
            UiFactory.Anchor((RectTransform)dealer.transform,
                new Vector2(0.5f, 1f), new Vector2(200f, 200f), new Vector2(0f, -10f));
            _fx.DealerHand = (RectTransform)dealer.transform;
        }

        private void BuildDiceArea(RectTransform canvas)
        {
            // AI 骰列（上方，下移避開撒旦之手）
            _roller.AIRow = RowAt(canvas, 245f, 130f, 20f);
            _roller.AIRow.gameObject.name = "AIRow";

            // 撒旦牌型文字（結算時顯示於 AI 骰下方），預設隱藏
            TextMeshProUGUI aiHand = UiFactory.Text("AIHandLabel", canvas, "", 24f, Dim);
            aiHand.rectTransform.anchoredPosition = new Vector2(0f, 160f);
            aiHand.gameObject.SetActive(false);
            _ui.AIHandLabel = aiHand;

            // 玩家骰列（中下，Bar 上方留空間）
            UiFactory.Text("YouLabel", canvas, "YOUR DICE", 26f, Ink)
                .rectTransform.anchoredPosition = new Vector2(0f, 90f);

            _roller.PlayerRow = RowAt(canvas, -30f, 130f, 20f);
            _roller.PlayerRow.gameObject.name = "PlayerRow";

            // 玩家牌型文字（結算時顯示於玩家骰下方），預設隱藏
            TextMeshProUGUI playerHand = UiFactory.Text("PlayerHandLabel", canvas, "", 24f, Dim);
            playerHand.rectTransform.anchoredPosition = new Vector2(0f, -125f);
            playerHand.gameObject.SetActive(false);
            _ui.PlayerHandLabel = playerHand;

            // 特殊骰槽（右側）
            RectTransform special = UiFactory.Panel("SpecialSlot", canvas);
            UiFactory.Anchor(special, new Vector2(1f, 0.5f),
                new Vector2(130f, 130f), new Vector2(-90f, -30f));
            _roller.SpecialSlot = special;
        }

        private void BuildRerollArea(RectTransform canvas)
        {
            GameObject controls = new GameObject("RerollControls", typeof(RectTransform));
            controls.transform.SetParent(canvas, false);
            RectTransform crt = (RectTransform)controls.transform;
            UiFactory.Anchor(crt, new Vector2(0.5f, 0f),
                new Vector2(700f, 60f), new Vector2(0f, 160f));

            UiFactory.Button("RerollBtn", crt, "REROLL", new Vector2(200f, 54f),
                () => _gm.OnRerollPressed())
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(-170f, 0f);

            UiFactory.Button("DoneBtn", crt, "DONE", new Vector2(200f, 54f),
                () => _gm.OnDonePressed())
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(170f, 0f);

            TextMeshProUGUI rinfo = UiFactory.Text("RerollInfo", crt, "", 20f, Ink);
            rinfo.rectTransform.anchoredPosition  = new Vector2(0f, 0f);
            rinfo.rectTransform.sizeDelta         = new Vector2(220f, 50f);
            _ui.RerollInfo    = rinfo;
            _ui.RerollControls = controls;
            controls.SetActive(false);
        }

        private void BuildAbyssBanner(RectTransform canvas)
        {
            RectTransform root = UiFactory.Panel("AbyssBanner", canvas);

            // 深色圓角底條（置中偏上，不擋骰子）
            Image bar = UiFactory.Image("AbyssBar", root,
                PlaceholderArt.RoundedRect(
                    new Color(0.10f, 0.02f, 0.04f, 0.92f),
                    new Color(0.62f, 0.08f, 0.10f, 1f), 96, 22, 3),
                Color.white);
            bar.type = Image.Type.Sliced;
            bar.raycastTarget = false;
            RectTransform barRt = (RectTransform)bar.transform;
            UiFactory.Anchor(barRt, new Vector2(0.5f, 0.5f),
                new Vector2(700f, 84f), new Vector2(0f, 230f));

            TextMeshProUGUI t = UiFactory.Text("AbyssText", barRt, "", 34f,
                new Color(0.97f, 0.5f, 0.33f));
            t.fontStyle = FontStyles.Bold;
            UiFactory.Stretch((RectTransform)t.transform);

            _ui.AbyssBanner = root.gameObject;
            _ui.AbyssBannerText = t;
            root.gameObject.SetActive(false);
        }

        private void BuildInkOverlay(RectTransform canvas)
        {
            Image ink = UiFactory.Image("InkOverlay", canvas,
                PlaceholderArt.SolidSprite(Color.white), new Color(0.55f, 0.02f, 0.04f, 0f));
            UiFactory.Stretch((RectTransform)ink.transform);
            _fx.InkOverlay = ink;
        }

        private void BuildResultPanel(RectTransform canvas)
        {
            RectTransform panel = UiFactory.Panel("ResultPanel", canvas);
            _ui.ResultPanel = panel.gameObject;
            // 不壓黑：panel 根節點本身無背景圖，後方畫面照常顯示。

            // 圓角深色卡片（中央彈出）
            Image card = UiFactory.Image("Card", panel,
                PlaceholderArt.RoundedRect(
                    new Color(0.09f, 0.11f, 0.16f, 0.98f),  // 深藍灰底
                    new Color(0.30f, 0.36f, 0.50f, 1f),     // 較亮描邊
                    96, 24, 3),
                Color.white);
            RectTransform cardRt = (RectTransform)card.transform;
            UiFactory.Center(cardRt, new Vector2(320f, 200f));
            card.type = Image.Type.Sliced;
            card.raycastTarget = true; // 擋住點擊穿透到後方骰子

            // 倍數（大、粗體、偏上）
            TextMeshProUGUI hand = UiFactory.Text("Mult", cardRt, "", 56f, Color.white);
            hand.fontStyle = FontStyles.Bold;
            hand.rectTransform.anchoredPosition = new Vector2(0f, 32f);
            _result.HandText = hand;

            // 分隔線
            Image divider = UiFactory.Image("Divider", cardRt,
                PlaceholderArt.SolidSprite(Color.white),
                new Color(0.35f, 0.40f, 0.52f, 0.85f));
            UiFactory.Center((RectTransform)divider.transform, new Vector2(210f, 2f));
            ((RectTransform)divider.transform).anchoredPosition = new Vector2(0f, -6f);

            // 贏分（小、淺灰、偏下）
            TextMeshProUGUI payout = UiFactory.Text("Payout", cardRt, "", 30f,
                new Color(0.80f, 0.84f, 0.92f));
            payout.rectTransform.anchoredPosition = new Vector2(0f, -46f);
            _result.PayoutText = payout;

            // 結算面板不再有「CONTINUE」按鈕，改為顯示約 1 秒後自動關閉（見 GameManager）。

            panel.gameObject.SetActive(false);
        }

        // ──────────────────────────────────────────
        // 底部工具列
        // ──────────────────────────────────────────

        private void BuildBottomBar(RectTransform canvas)
        {
            // 底色面板
            GameObject barGo = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
            barGo.transform.SetParent(canvas, false);
            RectTransform bar = (RectTransform)barGo.transform;
            bar.anchorMin       = new Vector2(0f, 0f);
            bar.anchorMax       = new Vector2(1f, 0f);
            bar.pivot           = new Vector2(0.5f, 0f);
            bar.sizeDelta       = new Vector2(0f, BarH);
            bar.anchoredPosition = Vector2.zero;
            barGo.GetComponent<Image>().color = BarBg;

            // 分隔線（上緣）
            Image sep = UiFactory.Image("Separator", bar,
                PlaceholderArt.SolidSprite(new Color(0.4f, 0.1f, 0.12f)), Color.white);
            RectTransform sepRt = (RectTransform)sep.transform;
            sepRt.anchorMin = new Vector2(0f, 1f);
            sepRt.anchorMax = new Vector2(1f, 1f);
            sepRt.pivot     = new Vector2(0.5f, 1f);
            sepRt.sizeDelta = new Vector2(0f, 2f);
            sepRt.anchoredPosition = Vector2.zero;

            // ── 左側：餘額 / 模式 / 骰子 ──
            _ui.BalanceText    = BarStaticWidget(bar, "BAL", -750f, "1,000,000");
            _ui.ModeCycleLabel = BarCycleWidget(bar, "MODE", -540f, _ui.GetModeLabel(),
                                    () => _gm.OnCycleMode());
            _ui.DiceCycleLabel = BarCycleWidget(bar, "DICE", -330f, _ui.GetDiceLabel(),
                                    () => _gm.OnCycleDice());

            // ── 中央：分數 ──
            TextMeshProUGUI scoreText = UiFactory.Text("ScoreText", bar, "", 26f, Ink);
            RectTransform scorRt = scoreText.rectTransform;
            scorRt.anchorMin = scorRt.anchorMax = scorRt.pivot = new Vector2(0.5f, 0.5f);
            scorRt.sizeDelta        = new Vector2(380f, 50f);
            scorRt.anchoredPosition = Vector2.zero;
            _ui.ScoreText = scoreText;

            // ── 右側：押注 / Spin / Auto ──
            BarBetWidget(bar, 300f);
            // Spin 按鈕：放大、往上偏移（突出 Bar 上緣）
            _ui.SpinBtn     = BarCircleBtn(bar, "SpinBtn",  490f, 120f, ">", Crimson,
                                () => _gm.OnStartPressed(), yOffset: 18f);
            // Auto 按鈕：透明背景，只顯示圖示
            _ui.AutoSpinBtn = BarCircleBtn(bar, "AutoBtn",  620f, 56f, "A", Color.clear,
                                () => _gm.OnToggleAutoSpin());
        }

        // ── 靜態文字 Widget（餘額）──
        private TextMeshProUGUI BarStaticWidget(RectTransform bar,
            string title, float x, string initial)
        {
            RectTransform w = BarWidgetRoot(title, bar, x, 170f);
            SmallLabel(title + "Lbl", w, title, 20f);
            return BigValue(title + "Val", w, initial, 22f);
        }

        // ── 可點擊循環 Widget（模式 / 骰子）──
        private TextMeshProUGUI BarCycleWidget(RectTransform bar,
            string title, float x, string initial, System.Action onClick)
        {
            RectTransform w = BarWidgetRoot(title, bar, x, 170f);
            SmallLabel(title + "Lbl", w, title, 20f);

            GameObject btnGo = new GameObject(title + "Btn",
                typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(w, false);
            RectTransform btnRt = (RectTransform)btnGo.transform;
            btnRt.anchorMin = btnRt.anchorMax = btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.sizeDelta        = new Vector2(158f, 36f);
            btnRt.anchoredPosition = new Vector2(0f, -14f);
            Image btnImg = btnGo.GetComponent<Image>();
            btnImg.color = WgtBg;
            btnGo.GetComponent<Button>().onClick.AddListener(() => onClick());

            TextMeshProUGUI val = UiFactory.Text(title + "Val", btnRt, initial, 22f, Ink);
            UiFactory.Stretch((RectTransform)val.transform);
            return val;
        }

        // ── 押注 Widget（▼ 金額 ▲）──
        private void BarBetWidget(RectTransform bar, float x)
        {
            RectTransform w = BarWidgetRoot("BetWidget", bar, x, 210f);
            SmallLabel("BetLbl", w, "BET", 20f);

            UiFactory.Button("BetDown", w, "<", new Vector2(30f, 34f),
                () => _gm.OnDecreaseBet())
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(-80f, -14f);

            TextMeshProUGUI betVal = UiFactory.Text("BetVal", w, "$ 100", 22f, Ink);
            betVal.rectTransform.anchorMin = betVal.rectTransform.anchorMax =
                betVal.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            betVal.rectTransform.sizeDelta        = new Vector2(110f, 34f);
            betVal.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            _ui.BetLabel = betVal;

            UiFactory.Button("BetUp", w, ">", new Vector2(30f, 34f),
                () => _gm.OnIncreaseBet())
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(80f, -14f);
        }

        // ── 圓形按鈕（Spin / Auto）──
        private Button BarCircleBtn(RectTransform bar, string name,
            float x, float size, string label, Color bg, System.Action onClick,
            float yOffset = 0f)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(bar, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(x, yOffset);

            Image img = go.GetComponent<Image>();
            bool isSpinBtn = name == "SpinBtn";
            if (isSpinBtn)
            {
                // 程式畫正圓：深紅底 + 血色邊框，避免 AI 圖橢圓問題
                img.sprite = PlaceholderArt.CircleSprite(
                    new Color(0.30f, 0.02f, 0.03f),
                    new Color(0.65f, 0.08f, 0.10f), 128, 7);
                img.color = Color.white;
                img.type  = Image.Type.Simple;
            }
            else
            {
                img.color = bg; // Color.clear → 透明
            }

            Button btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick());

            Color lblColor = isSpinBtn
                ? new Color(0.95f, 0.82f, 0.35f)   // 金色 ▶，在深紅圓上清晰
                : new Color(0.75f, 0.70f, 0.65f);   // Auto 淡灰
            TextMeshProUGUI lbl = UiFactory.Text(name + "Lbl", rt, label,
                size * 0.40f, lblColor);
            UiFactory.Stretch((RectTransform)lbl.transform);
            return btn;
        }

        // ── 共用：建立 Widget 根節點 ──
        private static RectTransform BarWidgetRoot(string name, RectTransform bar,
            float x, float width)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(bar, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(width, BarH - 8f);
            rt.anchoredPosition = new Vector2(x, 0f);
            return rt;
        }

        private static void SmallLabel(string name, RectTransform parent,
            string text, float fontSize)
        {
            TextMeshProUGUI lbl = UiFactory.Text(name, parent, text, fontSize,
                new Color(0.68f, 0.62f, 0.58f));
            lbl.rectTransform.anchorMin = lbl.rectTransform.anchorMax =
                lbl.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            lbl.rectTransform.sizeDelta        = new Vector2(160f, 26f);
            lbl.rectTransform.anchoredPosition = new Vector2(0f, 22f);
        }

        private static TextMeshProUGUI BigValue(string name, RectTransform parent,
            string text, float fontSize)
        {
            TextMeshProUGUI val = UiFactory.Text(name, parent, text, fontSize,
                new Color(0.9f, 0.85f, 0.78f));
            val.rectTransform.anchorMin = val.rectTransform.anchorMax =
                val.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            val.rectTransform.sizeDelta        = new Vector2(160f, 32f);
            val.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            return val;
        }

        // ──────────────────────────────────────────
        // 輔助
        // ──────────────────────────────────────────

        private static RectTransform RowAt(Transform parent,
            float y, float height, float spacing)
        {
            RectTransform r = UiFactory.Row("Row", parent, spacing);
            r.anchorMin = new Vector2(0f, 0.5f);
            r.anchorMax = new Vector2(1f, 0.5f);
            r.pivot     = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(0f, height);
            r.anchoredPosition = new Vector2(0f, y);
            return r;
        }
    }
}
