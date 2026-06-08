using System.Collections.Generic;

namespace AbyssProtocol.Core
{
    /// <summary>標準骰面數類型。</summary>
    public enum DiceType
    {
        D6,
        D12,
        D20
    }

    /// <summary>
    /// 撲克牌型。列舉的整數值「即」比較權重——數字越大牌型越強，
    /// 直接用 (int) 比較即可判定勝負。注意本遊戲 LargeStraight(6) 高於 FullHouse(5)。
    /// </summary>
    public enum HandRank
    {
        HighCard = 0,
        OnePair = 1,
        TwoPairs = 2,
        ThreeOfAKind = 3,
        SmallStraight = 4,
        FullHouse = 5,
        LargeStraight = 6,
        FourOfAKind = 7,
        FiveOfAKind = 8
    }

    /// <summary>深淵效果。標準骰落入觸發區間時隨機選一。</summary>
    public enum AbyssEffect
    {
        None = 0,
        Wild,
        Scry,
        Destroyer,
        Reroll
    }

    /// <summary>特殊骰種類（玩家專用第 6 顆，僅 Chaos 模式使用）。</summary>
    public enum SpecialDiceKind
    {
        None = 0,
        DoubleEdge,
        Cursed
    }

    /// <summary>遊戲模式。</summary>
    public enum GameMode
    {
        General,
        Chaos
    }

    /// <summary>勝負判定結果。</summary>
    public enum Winner
    {
        Player,
        AI
    }

    /// <summary>
    /// 全域常數與數值表。所有可調平衡數值集中於此，
    /// 方便日後不動邏輯就能調整賠率與難度。
    /// </summary>
    public static class GameConfig
    {
        // ---- 押注檔位 ----
        public static readonly int[] BetTiers = { 100, 200, 300, 500, 800, 1000 };
        public const int DefaultBet = 100;

        // ---- 玩家預設重擲次數 ----
        public const int DefaultRerollLimit = 1;

        // ---- 牌型賠率倍數 ----
        public static readonly Dictionary<HandRank, int> HandMultiplier =
            new Dictionary<HandRank, int>
            {
                { HandRank.FiveOfAKind, 50 },
                { HandRank.FourOfAKind, 15 },
                { HandRank.LargeStraight, 12 },
                { HandRank.FullHouse, 10 },
                { HandRank.SmallStraight, 5 },
                { HandRank.ThreeOfAKind, 3 },
                { HandRank.TwoPairs, 2 },
                { HandRank.OnePair, 1 },
                { HandRank.HighCard, 0 }
            };

        // ---- 難度（骰子面數）倍率 ----
        public static readonly Dictionary<DiceType, float> DifficultyMultiplier =
            new Dictionary<DiceType, float>
            {
                { DiceType.D6, 1.0f },
                { DiceType.D12, 2.5f },
                { DiceType.D20, 5.0f }
            };

        /// <summary>取得指定骰型的面數。</summary>
        public static int GetFaces(DiceType type)
        {
            switch (type)
            {
                case DiceType.D6: return 6;
                case DiceType.D12: return 12;
                case DiceType.D20: return 20;
                default: return 6;
            }
        }

        /// <summary>
        /// 判定點數是否落入深淵觸發區間。
        /// D6: {6}; D12: {11,12}; D20: {17,18,19,20}。
        /// </summary>
        public static bool IsAbyssTrigger(DiceType type, int value)
        {
            switch (type)
            {
                case DiceType.D6: return value == 6;
                case DiceType.D12: return value >= 11;
                case DiceType.D20: return value >= 17;
                default: return false;
            }
        }
    }
}
