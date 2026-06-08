using System.Collections;
using UnityEngine;
using AbyssProtocol.Core;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 表現層與 Core 狀態機之間的橋接器。
    /// 新增：餘額追蹤、底部列按鈕（模式/骰子/押注/AutoSpin）。
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public DiceRollerController Roller;
        public UIManager UI;
        public ResultDisplay Result;
        public FXController FX;

        public int StartingBalance = 1000000;
        public int RandomSeed = 0;
        public bool UseFixedSeed = false;

        private GameStateMachine _fsm;
        private int _balance;

        // 事件緩衝
        private bool _fullRollPending;
        private bool _rollDirty;
        private bool _scryPending;
        private int[] _scryValues;
        private bool _aiRevealed;

        // 深淵效果觸發提示用：記錄本局 Destroyer 命中的 AI 骰索引、是否待提示 Reroll 計數變化
        private readonly bool[] _destroyerHits = new bool[5];
        private bool _rerollFlashPending;

        private HandRank _evalPlayer;
        private HandRank _evalAI;
        private Winner _evalWinner;
        private int _evalPayout;

        private bool _busy;

        private void Start()
        {
            IRandom rng = UseFixedSeed ? (IRandom)new SystemRandom(RandomSeed) : new SystemRandom();
            _fsm = new GameStateMachine(rng);

            _fsm.PhaseChanged       += OnPhaseChanged;
            _fsm.DiceRolled         += () => _rollDirty = true;
            _fsm.ScryRevealed       += v => { _scryValues = v; _scryPending = true; };
            _fsm.AbyssEffectApplied += OnAbyssEffect;
            _fsm.AbyssDestroyerHit  += OnDestroyerHit;
            _fsm.RoundEvaluated     += OnRoundEvaluated;

            _balance = StartingBalance;
            UI.SetBalance(_balance);
            UI.SetHighScore(0);

            // 初始建骰（常駐顯示）
            Roller.BuildDice(GameConfig.GetFaces(UI.SelectedDice), OnDieClicked);
            Roller.ShowAllHidden();

            UI.RefreshBottomBar();
            UI.ShowIdle();
        }

        // ──────────────── 事件處理 ────────────────

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.BaseGameRoll)
            {
                _fullRollPending = true;
                _aiRevealed = false;
                System.Array.Clear(_destroyerHits, 0, _destroyerHits.Length);
                _rerollFlashPending = false;
                Roller.ClearWildMarkers(); // 新局開始，清掉上一局殘留的萬用骰徽章
            }
        }

        private void OnAbyssEffect(AbyssEffect effect)
        {
            if (FX != null) FX.PlayAbyss(effect);
        }

        /// <summary>Destroyer 命中時記下被擊中的 AI 骰索引，待結算揭露時於該骰播放紅色觸發光爆。</summary>
        private void OnDestroyerHit(int aiIndex)
        {
            if (aiIndex >= 0 && aiIndex < _destroyerHits.Length) _destroyerHits[aiIndex] = true;
        }

        private void OnRoundEvaluated(HandRank player, HandRank ai, Winner winner, int payout)
        {
            _evalPlayer = player;
            _evalAI     = ai;
            _evalWinner = winner;
            _evalPayout = payout;
        }

        // ──────────────── 底部列：循環切換 ────────────────

        public void OnCycleMode()
        {
            if (_busy || _fsm.CurrentPhase != GamePhase.Idle) return;
            UI.CycleMode();
        }

        public void OnCycleDice()
        {
            if (_busy || _fsm.CurrentPhase != GamePhase.Idle) return;
            UI.CycleDice();
            // 重建骰子外觀以反映新骰型
            Roller.BuildDice(GameConfig.GetFaces(UI.SelectedDice), OnDieClicked);
            Roller.ShowAllHidden();
        }

        public void OnIncreaseBet()
        {
            if (_busy || _fsm.CurrentPhase != GamePhase.Idle) return;
            UI.IncreaseBet();
        }

        public void OnDecreaseBet()
        {
            if (_busy || _fsm.CurrentPhase != GamePhase.Idle) return;
            UI.DecreaseBet();
        }

        public void OnToggleAutoSpin()
        {
            UI.ToggleAutoSpin();
        }

        // ──────────────── 主要 UI 輸入 ────────────────

        public void OnStartPressed()
        {
            if (_busy || _fsm.CurrentPhase != GamePhase.Idle) return;
            if (_balance < UI.SelectedBet) return;

            _balance -= UI.SelectedBet;
            UI.SetBalance(_balance);

            _fsm.ConfigureRound(UI.SelectedMode, UI.SelectedDice, UI.SelectedBet, UI.SelectedSpecial);
            Roller.BuildDice(GameConfig.GetFaces(UI.SelectedDice), OnDieClicked);
            UI.ShowGame();
            UI.HideRerollControls();
            _fsm.BeginRound();
            StartCoroutine(Drive());
        }

        public void OnDieClicked(int index)
        {
            if (_busy || _fsm.CurrentPhase != GamePhase.PlayerReroll) return;
            _fsm.ToggleLock(index);
            // 反轉呈現：locked=保留、unlocked=要重轉。點擊選取的就是「要重轉」的那顆。
            bool selectedForReroll = !_fsm.Context.PlayerDice[index].IsLocked;
            Roller.SetRerollSelectedVisual(index, selectedForReroll);
        }

        public void OnRerollPressed()
        {
            if (_busy || _fsm.CurrentPhase != GamePhase.PlayerReroll) return;
            _fsm.ConfirmReroll();
            StartCoroutine(Drive());
        }

        public void OnDonePressed()
        {
            if (_busy || _fsm.CurrentPhase != GamePhase.PlayerReroll) return;
            _fsm.EndReroll();
            StartCoroutine(Drive());
        }

        public void OnContinuePressed()
        {
            if (_busy || _fsm.CurrentPhase != GamePhase.Settlement) return;
            _fsm.AcknowledgeSettlement();
            StartCoroutine(Drive());
        }

        // ──────────────── 視覺驅動 ────────────────

        private IEnumerator Drive()
        {
            if (_busy) yield break;
            _busy = true;

            GameContext ctx = _fsm.Context;
            UI.HideRerollControls();

            if (_fullRollPending)
            {
                _fullRollPending = false;
                _rollDirty = false;
                // 依本局特殊骰種類切換槽位圖（有圖才換）
                Roller.SetSpecialVisual(
                    ctx.PlayerSpecialDie != null ? ctx.PlayerSpecialDie.Kind : SpecialDiceKind.None,
                    SpecialLabel(ctx));
                yield return Roller.AnimateInitialRoll(
                    ctx.GetPlayerValues(), PlayerAbyssFlags(ctx),
                    ctx.GetAIValues(), true, SpecialLabel(ctx));
                // 效果生效前先提示玩家「觸發了什麼深淵效果」
                yield return ShowAbyssHintIfAny(ctx);
            }
            else if (_rollDirty)
            {
                _rollDirty = false;
                Roller.ClearRerollForecast();   // 重擲前先收掉舊預報
                Roller.ClearRerollSelections(); // 與選取白框
                yield return Roller.AnimatePlayerReroll(
                    ctx.GetPlayerValues(), PlayerAbyssFlags(ctx), RerolledFlags(ctx));
                // 重擲後骰子物件可能已替換顯示內容，重新同步「萬用骰」徽章標記位置
                Roller.SetWildMarkers(WildMask(ctx));
            }

            if (_scryPending)
            {
                _scryPending = false;
                Roller.RevealAI(_scryValues);
                Roller.FlashRevealOnAIDice(); // REVEAL 觸發：青光爆掃過撒旦骰列，標示「這些被看穿了」
                _aiRevealed = true;
            }

            switch (_fsm.CurrentPhase)
            {
                case GamePhase.PlayerReroll:
                    bool canReroll = ctx.RerollsUsed < ctx.PlayerRerollLimit;
                    UI.ShowRerollControls(canReroll, ctx.PlayerRerollLimit - ctx.RerollsUsed);
                    // REROLL 效果生效：重擲面板剛出現，於次數文字播放一次金色脈動標示「這裡變了」
                    if (_rerollFlashPending)
                    {
                        _rerollFlashPending = false;
                        UI.PulseRerollInfo();
                    }
                    // 預設全部保留（locked）；玩家點選哪顆，才重轉那顆。
                    LockAllPlayerDice();
                    Roller.ClearRerollSelections();
                    // 還有重擲次數才預報：標記「重轉後有機會成大牌」的那顆
                    if (canReroll) Roller.ApplyRerollForecast(ctx.GetPlayerValues());
                    else Roller.ClearRerollForecast();
                    break;

                case GamePhase.Settlement:
                    Roller.ClearRerollForecast();
                    Roller.ClearRerollSelections();
                    yield return RevealAIWithCrushHighlight(ctx);
                    _balance += _evalPayout;
                    UI.SetBalance(_balance);
                    UI.SetHighScore(ctx.SessionHighScore);
                    bool won = _evalWinner == Winner.Player;

                    // ① 壓灰未參與牌型的骰子，並在雙方骰子下方顯示牌型文字，停留讓玩家看懂
                    Roller.MarkHandDice(
                        HandMaskEvaluator.ScoringMask(ctx.GetPlayerValues()),
                        HandMaskEvaluator.ScoringMask(ctx.GetAIValues()));
                    UI.ShowHandLabels(
                        "YOU   " + ResultDisplay.HandName(_evalPlayer),
                        "SATAN   " + ResultDisplay.HandName(_evalAI), won);
                    if (FX != null) FX.PlayResult(_evalWinner);
                    yield return new WaitForSeconds(1.8f);
                    Roller.ClearHandMarks();
                    UI.HideHandLabels();

                    // ② 玩家贏才彈出得分面板（倍數＋贏分）；輸則直接返回
                    if (won)
                    {
                        Result.Show(_evalPayout, UI.SelectedBet);
                        UI.ShowResult();
                        StartCoroutine(AutoCloseResult(1.5f));
                    }
                    else
                    {
                        StartCoroutine(AutoCloseResult(0.3f));
                    }
                    break;

                case GamePhase.Idle:
                    Roller.ClearRerollForecast();
                    Roller.ClearHandMarks();
                    Roller.ClearWildMarkers();
                    UI.ShowIdle();
                    if (UI.AutoSpin)
                        StartCoroutine(AutoSpinDelay());
                    break;
            }

            _busy = false;
        }

        private IEnumerator AutoSpinDelay()
        {
            yield return new WaitForSeconds(0.6f);
            OnStartPressed();
        }

        /// <summary>結算顯示後等待 delay 秒自動關閉並返回 Idle（取代 CONTINUE 按鈕）。</summary>
        private IEnumerator AutoCloseResult(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_fsm.CurrentPhase == GamePhase.Settlement)
            {
                _fsm.AcknowledgeSettlement();
                StartCoroutine(Drive());
            }
        }

        // ──────────────── 輔助 ────────────────

        /// <summary>
        /// 結算階段的撒旦骰揭露：
        ///   • 若 CRUSH 命中且撒旦骰仍為「？」（尚未被 REVEAL 揭露)：
        ///     先「只」揭露並紅光爆那一顆（其餘維持「？」），戲劇性停頓，
        ///     再揭露其餘骰子 → 讓玩家清楚看到「就是這顆被壓制歸一了」。
        ///   • 若撒旦骰已被 REVEAL 揭露過：直接整列重新揭露最終點數
        ///     （CRUSH 可能在 REVEAL 之後又改了某顆數值），並對命中位置補播紅光爆。
        ///   • 無 CRUSH：照常整列揭露。
        /// </summary>
        private IEnumerator RevealAIWithCrushHighlight(GameContext ctx)
        {
            bool crushHappened = false;
            for (int i = 0; i < _destroyerHits.Length; i++)
                if (_destroyerHits[i]) { crushHappened = true; break; }

            int[] aiValues = ctx.GetAIValues();

            if (!_aiRevealed)
            {
                if (crushHappened)
                {
                    // 撒旦骰仍隱藏：先單獨戲劇性揭露＋紅光爆「被壓制的那一顆」
                    for (int i = 0; i < _destroyerHits.Length; i++)
                        if (_destroyerHits[i]) Roller.RevealAndFlashCrush(i, aiValues[i]);
                    yield return new WaitForSeconds(0.9f);
                }
                Roller.RevealAI(aiValues);
                _aiRevealed = true;
            }
            else
            {
                // 已被 REVEAL 揭露過：整列刷新為最終點數，CRUSH 命中處補播紅光爆標示「這裡又變了」
                Roller.RevealAI(aiValues);
                if (crushHappened)
                {
                    for (int i = 0; i < _destroyerHits.Length; i++)
                        if (_destroyerHits[i]) Roller.FlashCrushOnAIDie(i);
                }
            }
        }

        /// <summary>進入重擲時預設全部保留（locked），讓玩家以「點選＝重轉」的方式選取。</summary>
        private void LockAllPlayerDice()
        {
            Die[] dice = _fsm.Context.PlayerDice;
            if (dice == null) return;
            for (int i = 0; i < dice.Length; i++) dice[i].IsLocked = true;
        }

        /// <summary>被重轉的骰子遮罩：未鎖定（玩家選取重轉）的即為已重擲。</summary>
        private static bool[] RerolledFlags(GameContext ctx)
        {
            Die[] dice = ctx.PlayerDice;
            bool[] flags = new bool[dice.Length];
            for (int i = 0; i < dice.Length; i++) flags[i] = !dice[i].IsLocked;
            return flags;
        }

        /// <summary>
        /// 若玩家骰觸發深淵效果，於效果生效前跳出橫幅提示；
        /// 文字顯示後緊接著在「會發生變化的位置」播放對應顏色的觸發光爆，讓玩家一眼看出變更發生在哪：
        ///   WILD   → 紫光爆於該玩家骰（這顆變萬用骰），並常駐顯示左上角萬用骰徽章（D6 圖示／D12-D20 顯示 "W"）
        ///   REROLL → 稍後重擲面板出現時，於次數文字播放金色脈動
        ///   REVEAL → 待 AI 骰真正揭露時於該列播放青光爆（見 Drive 內 _scryPending 處理）
        ///   CRUSH  → 結算時若撒旦骰仍是「？」，直接「只」揭露並紅光爆命中的那一顆（其餘維持隱藏），
        ///            戲劇性停頓後再揭露其餘骰子；若已被 REVEAL 揭露過，則整列刷新並補播紅光爆
        ///            （見 RevealAIWithCrushHighlight）
        /// </summary>
        private IEnumerator ShowAbyssHintIfAny(GameContext ctx)
        {
            bool[] seen = new bool[8]; // 以 (int)AbyssEffect 為索引
            bool[] wildMask = new bool[ctx.PlayerDice.Length];
            bool hasWild = false;
            bool hasReroll = false;
            string names = "";
            for (int i = 0; i < ctx.PlayerDice.Length; i++)
            {
                AbyssEffect e = ctx.PlayerDice[i].PendingEffect;
                if (e == AbyssEffect.Wild) { wildMask[i] = true; hasWild = true; }
                else if (e == AbyssEffect.Reroll) { hasReroll = true; }

                if (e == AbyssEffect.None || seen[(int)e]) continue;
                seen[(int)e] = true;
                if (names.Length > 0) names += " / ";
                names += AbyssEffectName(e);
            }
            if (names.Length == 0) yield break;

            UI.ShowAbyssBanner("ABYSS!   " + names);

            // 文字顯示的同時，標記出「現在就能看見」的變化位置：萬用骰本身、待提示的重擲次數
            if (hasWild)
            {
                Roller.FlashWildOnPlayerDice(wildMask);
                Roller.SetWildMarkers(wildMask); // 萬用骰常駐徽章：D6 顯示圖示／D12-D20 顯示 "W"
            }
            _rerollFlashPending = hasReroll;

            yield return new WaitForSeconds(1.3f);
            UI.HideAbyssBanner();
        }

        private static string AbyssEffectName(AbyssEffect e)
        {
            switch (e)
            {
                case AbyssEffect.Wild:      return "WILD";
                case AbyssEffect.Scry:      return "REVEAL";
                case AbyssEffect.Destroyer: return "CRUSH";
                case AbyssEffect.Reroll:    return "REROLL";
                default:                    return "";
            }
        }

        private static bool[] PlayerAbyssFlags(GameContext ctx)
        {
            bool[] flags = new bool[ctx.PlayerDice.Length];
            for (int i = 0; i < ctx.PlayerDice.Length; i++)
                flags[i] = ctx.PlayerDice[i].PendingEffect != AbyssEffect.None;
            return flags;
        }

        /// <summary>萬用骰遮罩：標記出本局 PendingEffect 為 Wild 的玩家骰，供標記徽章與重擲後重新同步使用。</summary>
        private static bool[] WildMask(GameContext ctx)
        {
            bool[] mask = new bool[ctx.PlayerDice.Length];
            for (int i = 0; i < ctx.PlayerDice.Length; i++)
                mask[i] = ctx.PlayerDice[i].PendingEffect == AbyssEffect.Wild;
            return mask;
        }

        private static string SpecialLabel(GameContext ctx)
        {
            SpecialDie die = ctx.PlayerSpecialDie;
            if (die == null) return "";
            switch (die.Kind)
            {
                case SpecialDiceKind.DoubleEdge:   return "x" + die.Multiplier;
                case SpecialDiceKind.Cursed:       return die.Multiplier == 6f ? "x6" : "x0";
                default:                           return "";
            }
        }
    }
}
