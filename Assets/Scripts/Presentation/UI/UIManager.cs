using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AbyssProtocol.Core;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 底部工具列狀態與面板切換。
    /// 不含流程邏輯——僅持有 UI 狀態，由 GameManager 讀取與驅動。
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Overlays")]
        public GameObject ResultPanel;

        [Header("Game UI")]
        public GameObject RerollControls;
        public TextMeshProUGUI RerollInfo;
        public TextMeshProUGUI HighScoreText;

        [Header("Abyss")]
        public GameObject AbyssBanner;
        public TextMeshProUGUI AbyssBannerText;

        [Header("Hand labels (結算時顯示於骰子下方)")]
        public TextMeshProUGUI PlayerHandLabel;
        public TextMeshProUGUI AIHandLabel;

        private static readonly Color HandWin  = new Color(0.97f, 0.83f, 0.38f);
        private static readonly Color HandLose = new Color(0.62f, 0.64f, 0.70f);

        [Header("Bottom Bar")]
        public TextMeshProUGUI BalanceText;
        public TextMeshProUGUI ModeCycleLabel;
        public TextMeshProUGUI DiceCycleLabel;
        public TextMeshProUGUI BetLabel;
        public TextMeshProUGUI ScoreText;
        public Button SpinBtn;
        public Button AutoSpinBtn;

        // ---- 選擇狀態 ----
        public GameMode SelectedMode { get; private set; }
        public DiceType SelectedDice { get; private set; }
        public int SelectedBet { get; private set; }
        public SpecialDiceKind SelectedSpecial { get; private set; }
        public bool AutoSpin { get; private set; }

        private static readonly DiceType[] DiceOrder = { DiceType.D6, DiceType.D12, DiceType.D20 };

        private void Awake()
        {
            SelectedMode = GameMode.General;
            SelectedDice = DiceType.D6;
            SelectedBet = GameConfig.DefaultBet;
            SelectedSpecial = SpecialDiceKind.DoubleEdge;
        }

        // ---- 循環切換（底部列按鈕呼叫） ----

        public void CycleMode()
        {
            SelectedMode = SelectedMode == GameMode.General ? GameMode.Chaos : GameMode.General;
            RefreshBottomBar();
        }

        public void CycleDice()
        {
            int idx = System.Array.IndexOf(DiceOrder, SelectedDice);
            SelectedDice = DiceOrder[(idx + 1) % DiceOrder.Length];
            RefreshBottomBar();
        }

        public void IncreaseBet()
        {
            int[] t = GameConfig.BetTiers;
            int idx = System.Array.IndexOf(t, SelectedBet);
            if (idx < t.Length - 1) { SelectedBet = t[idx + 1]; RefreshBottomBar(); }
        }

        public void DecreaseBet()
        {
            int[] t = GameConfig.BetTiers;
            int idx = System.Array.IndexOf(t, SelectedBet);
            if (idx > 0) { SelectedBet = t[idx - 1]; RefreshBottomBar(); }
        }

        public void ToggleAutoSpin()
        {
            AutoSpin = !AutoSpin;
            if (AutoSpinBtn)
            {
                ColorBlock cb = AutoSpinBtn.colors;
                cb.normalColor = AutoSpin ? new Color(0.5f, 0.03f, 0.06f) : new Color(0.14f, 0.05f, 0.07f);
                AutoSpinBtn.colors = cb;
            }
        }

        public string GetModeLabel() => SelectedMode == GameMode.General ? "NORMAL" : "CHAOS";
        public string GetDiceLabel() => SelectedDice.ToString();

        public void RefreshBottomBar()
        {
            if (ModeCycleLabel) ModeCycleLabel.text = GetModeLabel();
            if (DiceCycleLabel) DiceCycleLabel.text = GetDiceLabel();
            if (BetLabel) BetLabel.text = "$ " + SelectedBet.ToString("N0");
        }

        // ---- 相容舊介面 ----
        public void SetMode(GameMode m) { SelectedMode = m; RefreshBottomBar(); }
        public void SetDice(DiceType d) { SelectedDice = d; RefreshBottomBar(); }
        public void SetBet(int b) { SelectedBet = b; RefreshBottomBar(); }
        public void SetSpecial(SpecialDiceKind k) { SelectedSpecial = k; }

        // ---- 顯示更新 ----

        public void SetBalance(int credits)
        {
            if (BalanceText) BalanceText.text = credits.ToString("N0");
        }

        public void SetHighScore(int high)
        {
            if (HighScoreText) HighScoreText.text = high.ToString("N0");
            if (ScoreText && high > 0) ScoreText.text = high.ToString("N0");
        }

        public void SetSpinInteractable(bool v)
        {
            if (SpinBtn) SpinBtn.interactable = v;
        }

        // ---- Overlay 管理 ----

        public void ShowResult()
        {
            if (ResultPanel) ResultPanel.SetActive(true);
        }

        public void ShowAbyssBanner(string msg)
        {
            if (AbyssBannerText) AbyssBannerText.text = msg;
            if (AbyssBanner) AbyssBanner.SetActive(true);
        }

        public void HideAbyssBanner()
        {
            if (AbyssBanner) AbyssBanner.SetActive(false);
        }

        /// <summary>結算時於雙方骰子下方顯示牌型文字，勝方高亮。</summary>
        public void ShowHandLabels(string playerText, string aiText, bool playerWon)
        {
            if (PlayerHandLabel)
            {
                PlayerHandLabel.text = playerText;
                PlayerHandLabel.color = playerWon ? HandWin : HandLose;
                PlayerHandLabel.gameObject.SetActive(true);
            }
            if (AIHandLabel)
            {
                AIHandLabel.text = aiText;
                AIHandLabel.color = playerWon ? HandLose : HandWin;
                AIHandLabel.gameObject.SetActive(true);
            }
        }

        public void HideHandLabels()
        {
            if (PlayerHandLabel) PlayerHandLabel.gameObject.SetActive(false);
            if (AIHandLabel) AIHandLabel.gameObject.SetActive(false);
        }

        public void HideOverlays()
        {
            if (ResultPanel) ResultPanel.SetActive(false);
        }

        public void ShowRerollControls(bool canReroll, int remaining)
        {
            if (RerollControls) RerollControls.SetActive(true);
            if (RerollInfo)
                RerollInfo.text = canReroll ? "Rerolls: " + remaining : "No Rerolls";
            SetSpinInteractable(false);
        }

        public void HideRerollControls()
        {
            if (RerollControls) RerollControls.SetActive(false);
        }

        private static readonly Color RerollPulseColor = new Color(1f, 0.82f, 0.32f); // 金：REROLL 效果觸發
        private Coroutine _rerollPulseCo;

        /// <summary>對「重擲次數」文字播放金色觸發脈動，標示「這裡因 REROLL 效果而變化」。</summary>
        public void PulseRerollInfo()
        {
            if (RerollInfo == null) return;
            if (_rerollPulseCo != null) StopCoroutine(_rerollPulseCo);
            _rerollPulseCo = StartCoroutine(RerollInfoPulseRoutine());
        }

        private IEnumerator RerollInfoPulseRoutine()
        {
            const float dur = 0.8f;
            Color baseColor = RerollInfo.color;
            Transform tr = RerollInfo.transform;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float burst = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI); // 0 → 1 → 0
                RerollInfo.color = Color.Lerp(baseColor, RerollPulseColor, burst);
                float scale = 1f + burst * 0.22f;
                tr.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            RerollInfo.color = baseColor;
            tr.localScale = Vector3.one;
            _rerollPulseCo = null;
        }

        // ---- 面板狀態 ----

        public void ShowIdle()
        {
            HideOverlays();
            HideRerollControls();
            SetSpinInteractable(true);
        }

        public void ShowGame()
        {
            HideOverlays();
            HideRerollControls();
            SetSpinInteractable(false);
        }
    }
}
