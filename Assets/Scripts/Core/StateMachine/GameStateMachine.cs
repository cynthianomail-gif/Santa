using System;
using System.Collections.Generic;

namespace AbyssProtocol.Core
{
    /// <summary>單局評估結果。</summary>
    public struct RoundResult
    {
        public readonly HandRank PlayerRank;
        public readonly HandRank AIRank;
        public readonly Winner Winner;
        public readonly int Payout;

        public RoundResult(HandRank playerRank, HandRank aiRank, Winner winner, int payout)
        {
            PlayerRank = playerRank;
            AIRank = aiRank;
            Winner = winner;
            Payout = payout;
        }
    }

    /// <summary>
    /// 遊戲流程狀態機（純 C#，零 UnityEngine 相依）。
    ///
    /// 表現層透過事件接收狀態變化，並以公開輸入方法驅動流程：
    ///   ConfigureRound → BeginRound → (自動擲骰/效果) → ToggleLock/ConfirmReroll/EndReroll
    ///   → (自動結算) → AcknowledgeSettlement / ChooseFGSpecialDie
    ///
    /// 深淵效果時序：
    ///   - Scry / Reroll：EffectTrigger 階段（玩家重擲前），依「首次擲骰」結果觸發。
    ///   - Wild / Destroyer：Evaluation 階段（玩家重擲後），依「最終骰子」結果觸發。
    ///   - 深淵效果僅由「玩家」標準骰觸發；AI 一翻兩瞪眼。
    /// </summary>
    public sealed class GameStateMachine
    {
        private readonly IRandom _random;
        private readonly AbyssEffectHandler _effects;
        private readonly Dictionary<GamePhase, IGameState> _states;
        private IGameState _current;

        public GameContext Context { get; private set; }

        public GamePhase CurrentPhase
        {
            get { return _current != null ? _current.Phase : GamePhase.Idle; }
        }

        // ---- 表現層事件 ----
        public event Action<GamePhase> PhaseChanged;
        public event Action DiceRolled;
        public event Action<int[]> ScryRevealed;
        public event Action<AbyssEffect> AbyssEffectApplied;
        public event Action<HandRank, HandRank, Winner, int> RoundEvaluated;
        public event Action<int> RoundSettled;
        public event Action FGStarted;
        public event Action<int> FGFinished;

        public GameStateMachine(IRandom random)
        {
            _random = random;
            _effects = new AbyssEffectHandler();
            Context = new GameContext();

            _states = new Dictionary<GamePhase, IGameState>();
            Register(new IdleState(this));
            Register(new BaseGameRollState(this));
            Register(new EffectTriggerState(this));
            Register(new PlayerRerollState(this));
            Register(new EvaluationState(this));
            Register(new FGTransitionState(this));
            Register(new FGLoopState(this));
            Register(new SettlementState(this));

            _current = _states[GamePhase.Idle];
            _current.Enter();
        }

        private void Register(IGameState state)
        {
            _states[state.Phase] = state;
        }

        /// <summary>切換狀態：退出當前 → 設定新狀態 → 廣播 → 進入新狀態。</summary>
        public void Transition(GamePhase next)
        {
            if (_current != null) _current.Exit();
            _current = _states[next];
            RaisePhaseChanged(next);
            _current.Enter();
        }

        // ================= 玩家輸入 API =================

        /// <summary>Idle：設定本局模式、骰型、押注與（Chaos）特殊骰。</summary>
        public void ConfigureRound(GameMode mode, DiceType diceType, int baseBet, SpecialDiceKind specialKind)
        {
            if (CurrentPhase != GamePhase.Idle) return;
            Context.Mode = mode;
            Context.ActiveDiceType = diceType;
            Context.BaseBet = baseBet;
            Context.SpecialDice = mode == GameMode.Chaos ? specialKind : SpecialDiceKind.None;
        }

        /// <summary>Idle → BaseGameRoll，開始一局。</summary>
        public void BeginRound()
        {
            if (CurrentPhase != GamePhase.Idle) return;
            Transition(GamePhase.BaseGameRoll);
        }

        /// <summary>PlayerReroll：切換某顆玩家標準骰的鎖定狀態。</summary>
        public void ToggleLock(int playerDiceIndex)
        {
            if (CurrentPhase != GamePhase.PlayerReroll) return;
            if (Context.PlayerDice == null) return;
            if (playerDiceIndex < 0 || playerDiceIndex >= Context.PlayerDice.Length) return;
            Context.PlayerDice[playerDiceIndex].IsLocked =
                !Context.PlayerDice[playerDiceIndex].IsLocked;
        }

        /// <summary>PlayerReroll：重擲所有未鎖定的玩家標準骰，消耗一次重擲。</summary>
        public void ConfirmReroll()
        {
            if (CurrentPhase != GamePhase.PlayerReroll) return;
            if (Context.RerollsUsed >= Context.PlayerRerollLimit) return;

            for (int i = 0; i < Context.PlayerDice.Length; i++)
            {
                if (!Context.PlayerDice[i].IsLocked)
                {
                    Context.PlayerDice[i].Roll();
                }
            }
            Context.RerollsUsed++;
            RaiseDiceRolled();

            if (Context.RerollsUsed >= Context.PlayerRerollLimit)
            {
                Transition(GamePhase.Evaluation);
            }
        }

        /// <summary>PlayerReroll：玩家選擇結束重擲（或放棄剩餘次數）→ Evaluation。</summary>
        public void EndReroll()
        {
            if (CurrentPhase != GamePhase.PlayerReroll) return;
            Transition(GamePhase.Evaluation);
        }

        /// <summary>FGTransition：玩家二選一特殊骰，確認後進入 FG 連續對局。</summary>
        public void ChooseFGSpecialDie(SpecialDiceKind kind)
        {
            if (CurrentPhase != GamePhase.FGTransition) return;
            if (kind != SpecialDiceKind.DoubleEdge && kind != SpecialDiceKind.Cursed) return;

            Context.SpecialDice = kind;
            Context.IsInFG = true;
            Context.FGRoundsRemaining = GameConfig.FGRounds;
            // 觸發 FG 的那一局收益併入 FG 累計總分。
            Context.FGAccumulated = Context.LastPayout;
            RaiseFGStarted();
            Transition(GamePhase.BaseGameRoll);
        }

        /// <summary>Settlement → Idle。</summary>
        public void AcknowledgeSettlement()
        {
            if (CurrentPhase != GamePhase.Settlement) return;
            Transition(GamePhase.Idle);
        }

        // ================= 狀態使用的協調方法 =================

        /// <summary>建立並投擲本局所有骰子，偵測 FG 觸發。</summary>
        internal void SetupAndRoll()
        {
            DiceType dt = Context.ActiveDiceType;

            Context.PlayerDice = new Die[5];
            for (int i = 0; i < 5; i++)
            {
                Context.PlayerDice[i] = new Die(dt, _random);
                Context.PlayerDice[i].Roll();
            }

            Context.AIDice = new Die[5];
            for (int i = 0; i < 5; i++)
            {
                Context.AIDice[i] = new Die(dt, _random);
                Context.AIDice[i].Roll();
            }

            SpecialDiceKind kind = ResolveActiveSpecialKind();
            Context.PlayerSpecialDie = new SpecialDie(kind, _random);
            Context.PlayerSpecialDie.Roll();

            Context.PlayerRerollLimit = GameConfig.DefaultRerollLimit;
            Context.RerollsUsed = 0;
            Context.PendingFG = Context.PlayerSpecialDie.FGTriggered;
        }

        /// <summary>決定本局第 6 顆特殊骰的種類。</summary>
        private SpecialDiceKind ResolveActiveSpecialKind()
        {
            if (Context.IsInFG) return Context.SpecialDice;               // FG 期間用玩家選的特殊骰
            if (Context.Mode == GameMode.General) return SpecialDiceKind.FGTrigger;
            return Context.SpecialDice;                                   // Chaos 用玩家選的特殊骰
        }

        /// <summary>重擲前效果：Scry（揭露 AI）與 Reroll（+次數），依首次擲骰結果。</summary>
        internal void ApplyPreRerollEffects()
        {
            for (int i = 0; i < Context.PlayerDice.Length; i++)
            {
                AbyssEffect effect = Context.PlayerDice[i].PendingEffect;
                if (effect == AbyssEffect.Scry)
                {
                    int[] revealed = _effects.HandleScry(Context.AIDice);
                    RaiseScryRevealed(revealed);
                    RaiseAbyssEffectApplied(AbyssEffect.Scry);
                }
                else if (effect == AbyssEffect.Reroll)
                {
                    _effects.HandleReroll(Context);
                    RaiseAbyssEffectApplied(AbyssEffect.Reroll);
                }
            }
        }

        /// <summary>評估本局：Wild 解析 → Destroyer → 牌型比較 → 收益。</summary>
        internal RoundResult EvaluateRound()
        {
            int[] playerValues = Context.GetPlayerValues();

            // Wild：依最終骰子標記並求最優解
            bool[] isWild = new bool[playerValues.Length];
            bool anyWild = false;
            for (int i = 0; i < Context.PlayerDice.Length; i++)
            {
                if (Context.PlayerDice[i].PendingEffect == AbyssEffect.Wild)
                {
                    isWild[i] = true;
                    anyWild = true;
                }
            }

            int[] resolved = playerValues;
            if (anyWild)
            {
                resolved = WildResolver.Resolve(
                    playerValues, isWild, GameConfig.GetFaces(Context.ActiveDiceType));
                RaiseAbyssEffectApplied(AbyssEffect.Wild);
            }

            // Destroyer：每顆各自對 AI 最大骰執行一次
            int destroyerCount = 0;
            for (int i = 0; i < Context.PlayerDice.Length; i++)
            {
                if (Context.PlayerDice[i].PendingEffect == AbyssEffect.Destroyer)
                {
                    destroyerCount++;
                }
            }
            for (int i = 0; i < destroyerCount; i++)
            {
                _effects.HandleDestroyer(Context.AIDice);
                RaiseAbyssEffectApplied(AbyssEffect.Destroyer);
            }

            HandRank playerRank = PokerEvaluator.Evaluate(resolved);
            HandRank aiRank = PokerEvaluator.Evaluate(Context.GetAIValues());
            Winner winner = PayoutCalculator.DetermineWinner(playerRank, aiRank);

            int bet = Context.IsInFG ? Context.FGBaseBet : Context.BaseBet;
            float specialMult = Context.PlayerSpecialDie.Multiplier;
            int payout = PayoutCalculator.ResolveRound(
                bet, playerRank, aiRank, Context.ActiveDiceType, specialMult);

            Context.LastPayout = payout;
            RaiseRoundEvaluated(playerRank, aiRank, winner, payout);
            return new RoundResult(playerRank, aiRank, winner, payout);
        }

        // ================= 事件廣播（null-safe） =================

        private void RaisePhaseChanged(GamePhase p) { var h = PhaseChanged; if (h != null) h(p); }
        internal void RaiseDiceRolled() { var h = DiceRolled; if (h != null) h(); }
        internal void RaiseScryRevealed(int[] v) { var h = ScryRevealed; if (h != null) h(v); }
        internal void RaiseAbyssEffectApplied(AbyssEffect e) { var h = AbyssEffectApplied; if (h != null) h(e); }
        internal void RaiseRoundEvaluated(HandRank p, HandRank a, Winner w, int pay) { var h = RoundEvaluated; if (h != null) h(p, a, w, pay); }
        internal void RaiseRoundSettled(int score) { var h = RoundSettled; if (h != null) h(score); }
        internal void RaiseFGStarted() { var h = FGStarted; if (h != null) h(); }
        internal void RaiseFGFinished(int total) { var h = FGFinished; if (h != null) h(total); }
    }
}
