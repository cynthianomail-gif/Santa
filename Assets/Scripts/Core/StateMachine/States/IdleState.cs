namespace AbyssProtocol.Core
{
    /// <summary>等待玩家選擇模式、骰型、押注與特殊骰。</summary>
    public sealed class IdleState : GameState
    {
        public IdleState(GameStateMachine machine) : base(machine) { }

        public override GamePhase Phase { get { return GamePhase.Idle; } }

        public override void Enter()
        {
            // 無跨局暫態需重置。
        }
    }
}
