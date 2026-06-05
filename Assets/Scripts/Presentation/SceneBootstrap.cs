using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AbyssProtocol.Core;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 以程式建構整個遊戲場景（Canvas / 面板 / 骰子區 / 系統元件）。
    /// 用法：開一個空場景，建立一個空 GameObject，掛上此腳本，按 Play 即可。
    /// AI 美術匯入後，把 Background / DealerHand / DiceSkin 換成正式貼圖即可。
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

        private static readonly Color Parchment = new Color(0.86f, 0.82f, 0.72f);
        private static readonly Color Ink = new Color(0.9f, 0.85f, 0.78f);

        private void Awake()
        {
            ResolveArtFromResources();

            EnsureEventSystem();
            RectTransform canvas = BuildCanvas();

            // 系統元件集中於一個 GameObject。
            GameObject systems = new GameObject("Systems");
            _ui = systems.AddComponent<UIManager>();
            _roller = systems.AddComponent<DiceRollerController>();
            _result = systems.AddComponent<ResultDisplay>();
            _fx = systems.AddComponent<FXController>();
            _gm = systems.AddComponent<GameManager>();
            _roller.Skin = DiceSkin;

            BuildBackground(canvas);
            BuildGamePanel(canvas);
            BuildIdlePanel(canvas);
            BuildInkOverlay(canvas);
            BuildResultPanel(canvas);
            BuildFGPanel(canvas);

            _gm.Roller = _roller;
            _gm.UI = _ui;
            _gm.Result = _result;
            _gm.FX = _fx;
        }

        // ---------------- 美術自動載入 ----------------

        /// <summary>
        /// Inspector 欄位留空時，自動從 Resources 載入對應圖
        /// （Assets/Art/Resources/ 下的 background / dealer_hand / dice_*）。
        /// 已在 Inspector 指派者不覆蓋。
        /// </summary>
        private void ResolveArtFromResources()
        {
            if (DiceSkin == null) DiceSkin = new DiceSkin();
            if (BackgroundSprite == null) BackgroundSprite = Resources.Load<Sprite>("background");
            if (DealerHandSprite == null) DealerHandSprite = Resources.Load<Sprite>("dealer_hand");
            if (DiceSkin.Frame == null) DiceSkin.Frame = Resources.Load<Sprite>("dice_frame");
            if (DiceSkin.Locked == null) DiceSkin.Locked = Resources.Load<Sprite>("dice_locked");
            if (DiceSkin.Abyss == null) DiceSkin.Abyss = Resources.Load<Sprite>("dice_abyss");
        }

        // ---------------- 基礎 ----------------

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem",
                    typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private static RectTransform BuildCanvas()
        {
            GameObject go = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return (RectTransform)go.transform;
        }

        private void BuildBackground(RectTransform canvas)
        {
            Image bg = UiFactory.Image("Background", canvas,
                BackgroundSprite != null
                    ? BackgroundSprite
                    : PlaceholderArt.SolidSprite(new Color(0.06f, 0.05f, 0.07f)),
                Color.white);
            UiFactory.Stretch((RectTransform)bg.transform);

            Image dealer = UiFactory.Image("DealerHand", canvas,
                DealerHandSprite != null
                    ? DealerHandSprite
                    : PlaceholderArt.RoundedFrame(new Color(0.3f, 0.08f, 0.1f),
                        new Color(0.05f, 0.02f, 0.03f)),
                Color.white);
            UiFactory.Anchor((RectTransform)dealer.transform,
                new Vector2(0.5f, 1f), new Vector2(280f, 280f), new Vector2(0f, -150f));
            _fx.DealerHand = (RectTransform)dealer.transform;
        }

        // ---------------- Game 面板 ----------------

        private void BuildGamePanel(RectTransform canvas)
        {
            RectTransform panel = UiFactory.Panel("GamePanel", canvas);
            _ui.GamePanel = panel.gameObject;

            UiFactory.Text("SatanLabel", panel, "撒旦", 32f, Ink).rectTransform
                .anchoredPosition = new Vector2(0f, 380f);

            _roller.AIRow = RowAt(panel, 250f, 130f, 20f);
            _roller.AIRow.gameObject.name = "AIRow";

            _roller.PlayerRow = RowAt(panel, -250f, 130f, 20f);
            _roller.PlayerRow.gameObject.name = "PlayerRow";

            UiFactory.Text("YouLabel", panel, "你的骰子", 28f, Ink).rectTransform
                .anchoredPosition = new Vector2(0f, -140f);

            // 特殊骰槽（右下）
            RectTransform special = UiFactory.Panel("SpecialSlot", panel);
            UiFactory.Anchor(special, new Vector2(1f, 0f),
                new Vector2(150f, 150f), new Vector2(-60f, 230f));
            _roller.SpecialSlot = special;

            // 重擲控制
            GameObject controls = new GameObject("RerollControls", typeof(RectTransform));
            controls.transform.SetParent(panel, false);
            RectTransform crt = (RectTransform)controls.transform;
            UiFactory.Anchor(crt, new Vector2(0.5f, 0f), new Vector2(800f, 120f),
                new Vector2(0f, 110f));
            _ui.RerollControls = controls;

            UiFactory.Button("RerollBtn", crt, "重擲", new Vector2(220f, 80f),
                () => _gm.OnRerollPressed()).GetComponent<RectTransform>()
                .anchoredPosition = new Vector2(-160f, 0f);
            UiFactory.Button("DoneBtn", crt, "確定", new Vector2(220f, 80f),
                () => _gm.OnDonePressed()).GetComponent<RectTransform>()
                .anchoredPosition = new Vector2(160f, 0f);
            TextMeshProUGUI rinfo = UiFactory.Text("RerollInfo", crt, "", 24f, Ink);
            rinfo.rectTransform.anchoredPosition = new Vector2(0f, 70f);
            _ui.RerollInfo = rinfo;
            controls.SetActive(false);

            // FG 計數（右上）、最高分（左上）
            TextMeshProUGUI fgCounter = UiFactory.Text("FGCounter", panel, "", 26f,
                new Color(0.95f, 0.8f, 0.3f), TextAlignmentOptions.Right);
            UiFactory.Anchor(fgCounter.rectTransform, new Vector2(1f, 1f),
                new Vector2(500f, 40f), new Vector2(-40f, -40f));
            fgCounter.gameObject.SetActive(false);
            _ui.FGCounter = fgCounter;

            TextMeshProUGUI high = UiFactory.Text("HighScore", panel, "最高單局: 0", 26f,
                Ink, TextAlignmentOptions.Left);
            UiFactory.Anchor(high.rectTransform, new Vector2(0f, 1f),
                new Vector2(500f, 40f), new Vector2(40f, -40f));
            _ui.HighScoreText = high;

            panel.gameObject.SetActive(false);
        }

        // ---------------- Idle 面板 ----------------

        private void BuildIdlePanel(RectTransform canvas)
        {
            RectTransform panel = UiFactory.Panel("IdlePanel", canvas);
            _ui.IdlePanel = panel.gameObject;

            UiFactory.Text("Title", panel, "深淵協議", 64f,
                new Color(0.8f, 0.1f, 0.12f)).rectTransform
                .anchoredPosition = new Vector2(0f, 400f);

            RectTransform modeRow = RowAt(panel, 220f, 80f, 30f);
            UiFactory.Button("General", modeRow, "一般模式", new Vector2(220f, 70f),
                () => _ui.SetMode(GameMode.General));
            UiFactory.Button("Chaos", modeRow, "混沌模式", new Vector2(220f, 70f),
                () => _ui.SetMode(GameMode.Chaos));

            RectTransform diceRow = RowAt(panel, 110f, 80f, 30f);
            UiFactory.Button("D6", diceRow, "D6", new Vector2(150f, 70f),
                () => _ui.SetDice(DiceType.D6));
            UiFactory.Button("D12", diceRow, "D12", new Vector2(150f, 70f),
                () => _ui.SetDice(DiceType.D12));
            UiFactory.Button("D20", diceRow, "D20", new Vector2(150f, 70f),
                () => _ui.SetDice(DiceType.D20));

            RectTransform betRow = RowAt(panel, 0f, 80f, 18f);
            foreach (int tier in GameConfig.BetTiers)
            {
                int captured = tier;
                UiFactory.Button("Bet" + tier, betRow, tier.ToString(),
                    new Vector2(130f, 70f), () => _ui.SetBet(captured));
            }

            RectTransform specialRow = RowAt(panel, -110f, 80f, 30f);
            specialRow.gameObject.name = "SpecialDiceRow";
            UiFactory.Button("DoubleEdge", specialRow, "雙刃骰", new Vector2(220f, 70f),
                () => _ui.SetSpecial(SpecialDiceKind.DoubleEdge));
            UiFactory.Button("Cursed", specialRow, "詛咒骰", new Vector2(220f, 70f),
                () => _ui.SetSpecial(SpecialDiceKind.Cursed));
            _ui.SpecialDiceRow = specialRow.gameObject;
            specialRow.gameObject.SetActive(false);

            TextMeshProUGUI selection = UiFactory.Text("Selection", panel, "", 26f, Ink);
            selection.rectTransform.anchoredPosition = new Vector2(0f, -210f);
            _ui.SelectionText = selection;

            UiFactory.Button("StartBtn", panel, "與撒旦對賭", new Vector2(300f, 90f),
                () => _gm.OnStartPressed()).GetComponent<RectTransform>()
                .anchoredPosition = new Vector2(0f, -330f);
        }

        // ---------------- 墨水特效層 ----------------

        private void BuildInkOverlay(RectTransform canvas)
        {
            Image ink = UiFactory.Image("InkOverlay", canvas,
                PlaceholderArt.SolidSprite(Color.white), new Color(0.55f, 0.02f, 0.04f, 0f));
            UiFactory.Stretch((RectTransform)ink.transform);
            _fx.InkOverlay = ink;
        }

        // ---------------- Result 面板 ----------------

        private void BuildResultPanel(RectTransform canvas)
        {
            RectTransform panel = UiFactory.Panel("ResultPanel", canvas);
            _ui.ResultPanel = panel.gameObject;

            Image dim = UiFactory.Image("Dim", panel,
                PlaceholderArt.SolidSprite(Color.black), new Color(0f, 0f, 0f, 0.6f));
            UiFactory.Stretch((RectTransform)dim.transform);
            dim.raycastTarget = true;

            TextMeshProUGUI hand = UiFactory.Text("Hand", panel, "", 34f, Ink);
            hand.rectTransform.anchoredPosition = new Vector2(0f, 80f);
            _result.HandText = hand;

            TextMeshProUGUI payout = UiFactory.Text("Payout", panel, "", 48f, Ink);
            payout.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            _result.PayoutText = payout;

            UiFactory.Button("Continue", panel, "繼續", new Vector2(260f, 80f),
                () => _gm.OnContinuePressed()).GetComponent<RectTransform>()
                .anchoredPosition = new Vector2(0f, -120f);

            panel.gameObject.SetActive(false);
        }

        // ---------------- FG 選擇面板 ----------------

        private void BuildFGPanel(RectTransform canvas)
        {
            RectTransform panel = UiFactory.Panel("FGPanel", canvas);
            _ui.FGPanel = panel.gameObject;

            Image dim = UiFactory.Image("Dim", panel,
                PlaceholderArt.SolidSprite(Color.black), new Color(0.2f, 0f, 0f, 0.7f));
            UiFactory.Stretch((RectTransform)dim.transform);
            dim.raycastTarget = true;

            UiFactory.Text("FGTitle", panel, "FREE GAME 觸發！選擇特殊骰", 36f,
                new Color(0.95f, 0.8f, 0.3f)).rectTransform
                .anchoredPosition = new Vector2(0f, 120f);

            RectTransform row = RowAt(panel, 0f, 100f, 40f);
            UiFactory.Button("FGDoubleEdge", row, "雙刃骰", new Vector2(240f, 90f),
                () => _gm.OnChooseFG(SpecialDiceKind.DoubleEdge));
            UiFactory.Button("FGCursed", row, "詛咒骰", new Vector2(240f, 90f),
                () => _gm.OnChooseFG(SpecialDiceKind.Cursed));

            panel.gameObject.SetActive(false);
        }

        // ---------------- 輔助 ----------------

        /// <summary>建立一條水平置中、滿版寬度、固定高度、指定垂直位置的列。</summary>
        private static RectTransform RowAt(Transform parent, float y, float height, float spacing)
        {
            RectTransform r = UiFactory.Row("Row", parent, spacing);
            r.anchorMin = new Vector2(0f, 0.5f);
            r.anchorMax = new Vector2(1f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(0f, height);
            r.anchoredPosition = new Vector2(0f, y);
            return r;
        }
    }
}
