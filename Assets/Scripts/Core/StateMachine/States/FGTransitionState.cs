namespace AbyssProtocol.Core
{
    /// <summary>
    /// FG 轉場。記錄 FGBaseBet，等待玩家二選一特殊骰（ChooseFGSpecialDie）。
    /// 表現層在此播放全螢幕墨水轉場並顯示特殊骰選擇 UI。
    /// </summary>
    public sealed class FGTransitionState : GameState
    {
        public FGTransitionState(GameStateMachine machine) : base(machine) { }

        public override GamePhase Phase { get { return GamePhase.FGTransition; } }

        public override void Enter()
        {
            // 記錄觸發當下的押注，後續 5 局固定。
            Machine.Context.FGBaseBet = Machine.Context.BaseBet;
            // 等待 ChooseFGSpecialDie 輸入。
        }
    }
}
