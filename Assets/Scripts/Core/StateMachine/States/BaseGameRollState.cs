namespace AbyssProtocol.Core
{
    /// <summary>雙方擲骰。玩家 5 標準骰 + 1 特殊骰；AI 5 標準骰。偵測 FG。</summary>
    public sealed class BaseGameRollState : GameState
    {
        public BaseGameRollState(GameStateMachine machine) : base(machine) { }

        public override GamePhase Phase { get { return GamePhase.BaseGameRoll; } }

        public override void Enter()
        {
            Machine.SetupAndRoll();
            Machine.RaiseDiceRolled();
            Machine.Transition(GamePhase.EffectTrigger);
        }
    }
}
