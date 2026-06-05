namespace AbyssProtocol.Core
{
    /// <summary>
    /// 玩家鎖定/重擲階段。等待玩家輸入（ToggleLock / ConfirmReroll / EndReroll）。
    /// 若無重擲次數則自動進入評估。
    /// </summary>
    public sealed class PlayerRerollState : GameState
    {
        public PlayerRerollState(GameStateMachine machine) : base(machine) { }

        public override GamePhase Phase { get { return GamePhase.PlayerReroll; } }

        public override void Enter()
        {
            if (Machine.Context.PlayerRerollLimit <= 0)
            {
                Machine.Transition(GamePhase.Evaluation);
            }
            // 否則等待玩家輸入。
        }
    }
}
