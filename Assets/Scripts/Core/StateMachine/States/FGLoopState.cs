namespace AbyssProtocol.Core
{
    /// <summary>
    /// FG 連續對局的銜接狀態。每完成一局 FG 後若仍有剩餘局數，
    /// 由此狀態啟動下一局擲骰。
    /// </summary>
    public sealed class FGLoopState : GameState
    {
        public FGLoopState(GameStateMachine machine) : base(machine) { }

        public override GamePhase Phase { get { return GamePhase.FGLoop; } }

        public override void Enter()
        {
            Machine.Transition(GamePhase.BaseGameRoll);
        }
    }
}
