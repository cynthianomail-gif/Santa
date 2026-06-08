namespace AbyssProtocol.Core
{
    /// <summary>遊戲流程的各個階段。</summary>
    public enum GamePhase
    {
        Idle,
        BaseGameRoll,
        EffectTrigger,
        PlayerReroll,
        Evaluation,
        Settlement
    }
}
