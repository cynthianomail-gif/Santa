namespace AbyssProtocol.Core
{
    /// <summary>
    /// 評估階段：Wild 解析 → Destroyer → 牌型比較 → 收益 → 結算。
    /// </summary>
    public sealed class EvaluationState : GameState
    {
        public EvaluationState(GameStateMachine machine) : base(machine) { }

        public override GamePhase Phase { get { return GamePhase.Evaluation; } }

        public override void Enter()
        {
            Machine.EvaluateRound();
            Machine.Transition(GamePhase.Settlement);
        }
    }
}
