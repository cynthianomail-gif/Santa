using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AbyssProtocol.Core;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 面板顯示切換與玩家於 Idle 階段的選擇狀態（模式 / 骰型 / 押注 / 特殊骰）。
    /// 不含流程邏輯——僅持有 UI 狀態，由 GameManager 讀取與驅動。
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject IdlePanel;
        public GameObject GamePanel;
        public GameObject ResultPanel;
        public GameObject FGPanel;

        [Header("Game panel widgets")]
        public GameObject RerollControls;
        public TextMeshProUGUI RerollInfo;
        public TextMeshProUGUI FGCounter;
        public TextMeshProUGUI HighScoreText;

        [Header("Idle selection feedback")]
        public TextMeshProUGUI SelectionText;
        public GameObject SpecialDiceRow; // Chaos 時才顯示

        // ---- 選擇狀態 ----
        public GameMode SelectedMode { get; private set; }
        public DiceType SelectedDice { get; private set; }
        public int SelectedBet { get; private set; }
        public SpecialDiceKind SelectedSpecial { get; private set; }

        private void Awake()
        {
            SelectedMode = GameMode.General;
            SelectedDice = DiceType.D6;
            SelectedBet = GameConfig.DefaultBet;
            SelectedSpecial = SpecialDiceKind.DoubleEdge;
        }

        // ---- 選擇 setter（由 Idle 按鈕呼叫） ----

        public void SetMode(GameMode mode)
        {
            SelectedMode = mode;
            if (SpecialDiceRow != null) SpecialDiceRow.SetActive(mode == GameMode.Chaos);
            RefreshSelection();
        }

        public void SetDice(DiceType dice) { SelectedDice = dice; RefreshSelection(); }
        public void SetBet(int bet) { SelectedBet = bet; RefreshSelection(); }
        public void SetSpecial(SpecialDiceKind kind) { SelectedSpecial = kind; RefreshSelection(); }

        private void RefreshSelection()
        {
            if (SelectionText == null) return;
            string s = SelectedMode + " / " + SelectedDice + " / 押注 " + SelectedBet;
            if (SelectedMode == GameMode.Chaos) s += " / " + SelectedSpecial;
            SelectionText.text = s;
        }

        // ---- 面板切換 ----

        public void ShowIdle()
        {
            ShowOnly(IdlePanel);
            if (SpecialDiceRow != null) SpecialDiceRow.SetActive(SelectedMode == GameMode.Chaos);
            RefreshSelection();
        }

        public void ShowGame()
        {
            ShowOnly(GamePanel);
        }

        public void ShowResult()
        {
            if (ResultPanel != null) ResultPanel.SetActive(true);
        }

        public void ShowFGChooser()
        {
            if (FGPanel != null) FGPanel.SetActive(true);
        }

        public void HideOverlays()
        {
            if (ResultPanel != null) ResultPanel.SetActive(false);
            if (FGPanel != null) FGPanel.SetActive(false);
        }

        public void ShowRerollControls(bool canReroll, int remaining)
        {
            if (RerollControls != null) RerollControls.SetActive(true);
            if (RerollInfo != null)
            {
                RerollInfo.text = canReroll
                    ? ("可重擲次數: " + remaining)
                    : "無重擲次數";
            }
        }

        public void HideRerollControls()
        {
            if (RerollControls != null) RerollControls.SetActive(false);
        }

        public void SetFGCounter(bool inFG, int remaining)
        {
            if (FGCounter == null) return;
            FGCounter.gameObject.SetActive(inFG);
            if (inFG) FGCounter.text = "FREE GAME  剩餘 " + remaining + " 局";
        }

        public void SetHighScore(int high)
        {
            if (HighScoreText != null) HighScoreText.text = "最高單局: " + high;
        }

        private void ShowOnly(GameObject panel)
        {
            if (IdlePanel != null) IdlePanel.SetActive(panel == IdlePanel);
            if (GamePanel != null) GamePanel.SetActive(panel == GamePanel);
            HideOverlays();
        }
    }
}
