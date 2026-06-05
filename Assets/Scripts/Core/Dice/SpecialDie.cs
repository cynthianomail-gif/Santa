namespace AbyssProtocol.Core
{
    /// <summary>
    /// 特殊骰（玩家專用第 6 顆）。不參與牌型判定，
    /// 而是提供結算倍率，或觸發 Free Game。
    ///
    /// DoubleEdge 面值: [x2, x0.5, x1, x2, x0.5, x1]
    /// Cursed     面值: [x6, x0, x0, x0, x0, x0]
    /// FGTrigger  面值: 1/6 機率觸發 FG，倍率固定 x1
    /// </summary>
    public sealed class SpecialDie
    {
        private static readonly float[] DoubleEdgeFaces =
            { 2f, 0.5f, 1f, 2f, 0.5f, 1f };

        private readonly IRandom _random;

        public SpecialDiceKind Kind { get; private set; }

        /// <summary>結算倍率（DoubleEdge / Cursed 使用；FGTrigger 固定 1）。</summary>
        public float Multiplier { get; private set; }

        /// <summary>FGTrigger 是否擲出 FG 面。</summary>
        public bool FGTriggered { get; private set; }

        /// <summary>本次擲出的面索引（0..5），供表現層顯示對應 Sprite。</summary>
        public int FaceIndex { get; private set; }

        public SpecialDie(SpecialDiceKind kind, IRandom random)
        {
            Kind = kind;
            _random = random;
            Multiplier = 1f;
            FGTriggered = false;
            FaceIndex = 0;
        }

        /// <summary>投擲特殊骰，依種類計算倍率或 FG 觸發。</summary>
        public void Roll()
        {
            FaceIndex = _random.Next(0, 6);
            FGTriggered = false;
            Multiplier = 1f;

            switch (Kind)
            {
                case SpecialDiceKind.DoubleEdge:
                    Multiplier = DoubleEdgeFaces[FaceIndex];
                    break;

                case SpecialDiceKind.Cursed:
                    Multiplier = FaceIndex == 0 ? 6f : 0f;
                    break;

                case SpecialDiceKind.FGTrigger:
                    FGTriggered = FaceIndex == 0;
                    Multiplier = 1f;
                    break;
            }
        }
    }
}
