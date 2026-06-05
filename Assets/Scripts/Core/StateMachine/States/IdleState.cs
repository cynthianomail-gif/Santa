namespace AbyssProtocol.Core
{
    /// <summary>等待玩家選擇模式、骰型、押注與特殊骰。重置跨局暫態。</summary>
    public sealed class IdleState : GameState
    {
        public IdleState(GameStateMachine machine) : base(machine) { }

        public override GamePhase Phase { get { return GamePhase.Idle; } }

        public override void Enter()
        {
            Machine.Context.IsInFG = false;
            Machine.Context.FGRoundsRemaining = 0;
            Machine.Context.FGAccumulated = 0;
            Machine.Context.PendingFG = false;
        }
    }
}
