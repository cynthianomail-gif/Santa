namespace AbyssProtocol.Core
{
    /// <summary>
    /// 評估階段：Wild 解析 → Destroyer → 牌型比較 → 收益。
    /// 依 FG 狀態決定後續走向。
    /// </summary>
    public sealed class EvaluationState : GameState
    {
        public EvaluationState(GameStateMachine machine) : base(machine) { }

        public override GamePhase Phase { get { return GamePhase.Evaluation; } }

        public override void Enter()
        {
            RoundResult result = Machine.EvaluateRound();
            GameContext ctx = Machine.Context;

            if (ctx.IsInFG)
            {
                ctx.FGAccumulated += result.Payout;
                ctx.FGRoundsRemaining--;

                if (ctx.FGRoundsRemaining > 0)
                {
                    Machine.Transition(GamePhase.FGLoop);
                }
                else
                {
                    Machine.Transition(GamePhase.Settlement);
                }
            }
            else
            {
                if (ctx.PendingFG)
                {
                    Machine.Transition(GamePhase.FGTransition);
                }
                else
                {
                    Machine.Transition(GamePhase.Settlement);
                }
            }
        }
    }
}
