namespace AbyssProtocol.Core
{
    /// <summary>
    /// 結算階段。一般局回報該局收益；FG 局回報 5 局累計總分。
    /// 更新最高分後等待 AcknowledgeSettlement 返回 Idle。
    /// </summary>
    public sealed class SettlementState : GameState
    {
        public SettlementState(GameStateMachine machine) : base(machine) { }

        public override GamePhase Phase { get { return GamePhase.Settlement; } }

        public override void Enter()
        {
            GameContext ctx = Machine.Context;
            int finalScore = ctx.IsInFG ? ctx.FGAccumulated : ctx.LastPayout;

            if (ctx.IsInFG)
            {
                Machine.RaiseFGFinished(ctx.FGAccumulated);
            }

            ctx.TryUpdateHighScore(finalScore);
            Machine.RaiseRoundSettled(finalScore);
            // 等待 AcknowledgeSettlement → Idle。
        }
    }
}
