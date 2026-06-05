using System;

namespace AbyssProtocol.Core
{
    /// <summary>
    /// 賠率與收益計算。
    ///
    /// 公式：Payout = BaseBet x HandMultiplier x DifficultyMultiplier x SpecialMultiplier
    /// 勝負：玩家牌型須「嚴格高於」AI；平手（同階）莊家（撒旦）勝。
    /// </summary>
    public static class PayoutCalculator
    {
        /// <summary>
        /// 純公式：計算某牌型在指定條件下的收益乘積。
        /// 不含勝負判定（HighCard 因倍率 0 自然回傳 0）。
        /// </summary>
        public static int CalculatePayout(
            int baseBet,
            HandRank rank,
            DiceType diceType,
            float specialMultiplier)
        {
            float payout = baseBet
                           * GameConfig.HandMultiplier[rank]
                           * GameConfig.DifficultyMultiplier[diceType]
                           * specialMultiplier;

            return (int)Math.Round(payout, MidpointRounding.AwayFromZero);
        }

        /// <summary>勝負判定：玩家嚴格高於 AI 才算贏，平手或更低皆莊家勝。</summary>
        public static Winner DetermineWinner(HandRank playerRank, HandRank aiRank)
        {
            return playerRank > aiRank ? Winner.Player : Winner.AI;
        }

        /// <summary>
        /// 結算一局：玩家贏才計算收益，否則回傳 0。
        /// </summary>
        public static int ResolveRound(
            int baseBet,
            HandRank playerRank,
            HandRank aiRank,
            DiceType diceType,
            float specialMultiplier)
        {
            if (DetermineWinner(playerRank, aiRank) != Winner.Player)
            {
                return 0;
            }
            return CalculatePayout(baseBet, playerRank, diceType, specialMultiplier);
        }
    }
}
