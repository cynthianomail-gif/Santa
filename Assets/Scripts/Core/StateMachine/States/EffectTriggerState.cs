namespace AbyssProtocol.Core
{
    /// <summary>重擲前效果：Scry（揭露 AI）與 Reroll（+次數），然後進入重擲階段。</summary>
    public sealed class EffectTriggerState : GameState
    {
        public EffectTriggerState(GameStateMachine machine) : base(machine) { }

        public override GamePhase Phase { get { return GamePhase.EffectTrigger; } }

        public override void Enter()
        {
            Machine.ApplyPreRerollEffects();
            Machine.Transition(GamePhase.PlayerReroll);
        }
    }
}
