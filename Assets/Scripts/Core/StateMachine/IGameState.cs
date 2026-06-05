namespace AbyssProtocol.Core
{
    /// <summary>單一遊戲狀態的生命週期介面。</summary>
    public interface IGameState
    {
        GamePhase Phase { get; }

        /// <summary>進入此狀態時呼叫，執行該階段的自動行為。</summary>
        void Enter();

        /// <summary>離開此狀態時呼叫，清理暫態。</summary>
        void Exit();
    }

    /// <summary>狀態共用基底，持有狀態機參考。</summary>
    public abstract class GameState : IGameState
    {
        protected readonly GameStateMachine Machine;

        protected GameState(GameStateMachine machine)
        {
            Machine = machine;
        }

        public abstract GamePhase Phase { get; }

        public virtual void Enter() { }

        public virtual void Exit() { }
    }
}
