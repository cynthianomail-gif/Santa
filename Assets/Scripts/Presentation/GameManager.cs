using System.Collections;
using UnityEngine;
using AbyssProtocol.Core;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 表現層與 Core 狀態機之間的橋接器。持有純 C# 的 GameStateMachine，
    /// 訂閱其事件，並把玩家 UI 操作轉發為狀態機輸入。
    ///
    /// Core FSM 同步求值（呼叫輸入方法後流程會立即推進到下一個等待點），
    /// 因此本類以一條 Drive 協程把「已算好的結果」演出成動畫。
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public DiceRollerController Roller;
        public UIManager UI;
        public ResultDisplay Result;
        public FXController FX;

        public int RandomSeed = 0;
        public bool UseFixedSeed = false;

        private GameStateMachine _fsm;

        // 事件緩衝
        private bool _fullRollPending;
        private bool _rollDirty;
        private bool _scryPending;
        private int[] _scryValues;
        private bool _aiRevealed;

        private HandRank _evalPlayer;
        private HandRank _evalAI;
        private Winner _evalWinner;
        private int _evalPayout;
        private int _fgTotal;

        private bool _busy;

        private void Start()
        {
            IRandom rng = UseFixedSeed ? (IRandom)new SystemRandom(RandomSeed) : new SystemRandom();
            _fsm = new GameStateMachine(rng);

            _fsm.PhaseChanged += OnPhaseChanged;
            _fsm.DiceRolled += () => _rollDirty = true;
            _fsm.ScryRevealed += v => { _scryValues = v; _scryPending = true; };
            _fsm.AbyssEffectApplied += OnAbyssEffect;
            _fsm.RoundEvaluated += OnRoundEvaluated;
            _fsm.FGFinished += t => _fgTotal = t;

            UI.SetHighScore(0);
            UI.ShowIdle();
        }

        // ---------------- 事件處理 ----------------

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.BaseGameRoll)
            {
                _fullRollPending = true;
                _aiRevealed = false;
            }
        }

        private void OnAbyssEffect(AbyssEffect effect)
        {
            if (FX != null) FX.PlayAbyss(effect);
        }

        private void OnRoundEvaluated(HandRank player, HandRank ai, Winner winner, int payout)
        {
            _evalPlayer = player;
            _evalAI = ai;
            _evalWinner = winner;
            _evalPayout = payout;
        }

        // ---------------- UI 輸入（由按鈕呼叫） ----------------

        public void OnStartPressed()
        {
            if (_busy || _fsm.CurrentPhase != GamePhase.Idle) return;

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
            Roller.SetLockVisual(index, _fsm.Context.PlayerDice[index].IsLocked);
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

        public void OnChooseFG(SpecialDiceKind kind)
        {
            if (_busy || _fsm.CurrentPhase != GamePhase.FGTransition) return;
            UI.HideOverlays();
            _fsm.ChooseFGSpecialDie(kind);
            StartCoroutine(Drive());
        }

        public void OnContinuePressed()
        {
            if (_busy || _fsm.CurrentPhase != GamePhase.Settlement) return;
            _fsm.AcknowledgeSettlement();
            StartCoroutine(Drive());
        }

        // ---------------- 視覺驅動 ----------------

        private IEnumerator Drive()
        {
            if (_busy) yield break;
            _busy = true;

            GameContext ctx = _fsm.Context;
            UI.HideRerollControls();

            // 1) 演出本次擲骰
            if (_fullRollPending)
            {
                _fullRollPending = false;
                _rollDirty = false;
                yield return Roller.AnimateInitialRoll(
                    ctx.GetPlayerValues(), PlayerAbyssFlags(ctx),
                    ctx.GetAIValues(), true, SpecialLabel(ctx));
            }
            else if (_rollDirty)
            {
                _rollDirty = false;
                yield return Roller.AnimatePlayerReroll(
                    ctx.GetPlayerValues(), PlayerAbyssFlags(ctx));
            }

            // 2) 重擲前效果：Scry 揭露
            if (_scryPending)
            {
                _scryPending = false;
                Roller.RevealAI(_scryValues);
                _aiRevealed = true;
            }

            // 3) 依目前停駐階段呈現
            switch (_fsm.CurrentPhase)
            {
                case GamePhase.PlayerReroll:
                    UI.SetFGCounter(ctx.IsInFG, ctx.FGRoundsRemaining);
                    bool canReroll = ctx.RerollsUsed < ctx.PlayerRerollLimit;
                    UI.ShowRerollControls(canReroll, ctx.PlayerRerollLimit - ctx.RerollsUsed);
                    break;

                case GamePhase.Settlement:
                    if (!_aiRevealed) Roller.RevealAI(ctx.GetAIValues());
                    if (ctx.IsInFG) Result.ShowFGTotal(_fgTotal);
                    else Result.Show(_evalPlayer, _evalAI, _evalWinner, _evalPayout);
                    if (FX != null) FX.PlayResult(_evalWinner);
                    UI.SetHighScore(ctx.SessionHighScore);
                    UI.ShowResult();
                    break;

                case GamePhase.FGTransition:
                    if (!_aiRevealed) Roller.RevealAI(ctx.GetAIValues());
                    if (FX != null) FX.PlayFGTransition();
                    UI.ShowFGChooser();
                    break;

                case GamePhase.Idle:
                    UI.ShowIdle();
                    break;
            }

            _busy = false;
        }

        // ---------------- 輔助 ----------------

        private static bool[] PlayerAbyssFlags(GameContext ctx)
        {
            bool[] flags = new bool[ctx.PlayerDice.Length];
            for (int i = 0; i < ctx.PlayerDice.Length; i++)
            {
                flags[i] = ctx.PlayerDice[i].PendingEffect != AbyssEffect.None;
            }
            return flags;
        }

        private static string SpecialLabel(GameContext ctx)
        {
            SpecialDie die = ctx.PlayerSpecialDie;
            if (die == null) return "";
            switch (die.Kind)
            {
                case SpecialDiceKind.FGTrigger:
                    return die.FGTriggered ? "FG!" : "—";
                case SpecialDiceKind.DoubleEdge:
                    return "x" + die.Multiplier;
                case SpecialDiceKind.Cursed:
                    return die.Multiplier == 6f ? "x6" : "x0";
                default:
                    return "";
            }
        }
    }
}
