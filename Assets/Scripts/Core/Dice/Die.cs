namespace AbyssProtocol.Core
{
    /// <summary>
    /// 標準骰單體（D6 / D12 / D20，玩家與 AI 共用）。
    /// 負責投擲、鎖定狀態，以及落入深淵區間時抽取待發動效果。
    /// </summary>
    public sealed class Die
    {
        private static readonly AbyssEffect[] AbyssEffectPool =
        {
            AbyssEffect.Wild,
            AbyssEffect.Scry,
            AbyssEffect.Destroyer,
            AbyssEffect.Reroll
        };

        private readonly IRandom _random;

        public DiceType Type { get; private set; }
        public int Value { get; private set; }
        public bool IsLocked { get; set; }

        /// <summary>本次投擲後待發動的深淵效果；未觸發為 None。</summary>
        public AbyssEffect PendingEffect { get; private set; }

        public Die(DiceType type, IRandom random)
        {
            Type = type;
            _random = random;
            Value = 0;
            IsLocked = false;
            PendingEffect = AbyssEffect.None;
        }

        /// <summary>
        /// 投擲骰子。已鎖定的骰子不會改變。
        /// 若點數落入深淵觸發區間，隨機抽一個深淵效果存入 PendingEffect。
        /// </summary>
        public void Roll()
        {
            if (IsLocked) return;

            int faces = GameConfig.GetFaces(Type);
            Value = _random.Next(1, faces + 1);

            PendingEffect = GameConfig.IsAbyssTrigger(Type, Value)
                ? AbyssEffectPool[_random.Next(0, AbyssEffectPool.Length)]
                : AbyssEffect.None;
        }

        /// <summary>
        /// 強制覆寫點數（Destroyer 效果用）。
        /// 覆寫不會重新觸發深淵效果。
        /// </summary>
        public void ForceValue(int value)
        {
            Value = value;
            PendingEffect = AbyssEffect.None;
        }

        /// <summary>清除鎖定與待發動效果，準備下一局。</summary>
        public void Reset()
        {
            IsLocked = false;
            PendingEffect = AbyssEffect.None;
            Value = 0;
        }
    }
}
