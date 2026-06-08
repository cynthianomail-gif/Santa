namespace AbyssProtocol.Core
{
    /// <summary>
    /// 結算階段。回報本局收益並更新最高分，等待 AcknowledgeSettlement 返回 Idle。
    /// </summary>
    public sealed class SettlementState : GameState
    {
        public SettlementState(GameStateMachine machine) : base(machine) { }

        public override GamePhase Phase { get { return GamePhase.Settlement; } }

        public override void Enter()
        {
            GameContext ctx = Machine.Context;
            ctx.TryUpdateHighScore(ctx.LastPayout);
            Machine.RaiseRoundSettled(ctx.LastPayout);
            // 等待 AcknowledgeSettlement → Idle。
        }
    }
}
