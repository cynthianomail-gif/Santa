using System;
using System.Linq;

namespace AbyssProtocol.Core
{
    /// <summary>
    /// 撲克牌型判定器。輸入恰好 5 個整數（特殊骰已於呼叫前排除、
    /// Wild 已於呼叫前解析），輸出對應 HandRank。
    /// </summary>
    public static class PokerEvaluator
    {
        /// <summary>判定 5 顆骰子的牌型。</summary>
        public static HandRank Evaluate(int[] values)
        {
            if (values == null || values.Length != 5)
            {
                throw new ArgumentException("PokerEvaluator requires exactly 5 values.");
            }

            // 各點數出現次數，由多到少排序：[5]=五條, [4,1]=四條, [3,2]=葫蘆...
            int[] counts = values
                .GroupBy(v => v)
                .Select(g => g.Count())
                .OrderByDescending(c => c)
                .ToArray();

            int[] distinct = values.Distinct().OrderBy(v => v).ToArray();

            bool isLargeStraight = IsLargeStraight(distinct);
            bool isSmallStraight = HasConsecutiveRun(distinct, 4);

            // 依牌型強度（HandRank 數值由高到低）逐一判定。
            if (counts[0] == 5) return HandRank.FiveOfAKind;          // 8
            if (counts[0] == 4) return HandRank.FourOfAKind;          // 7
            if (isLargeStraight) return HandRank.LargeStraight;       // 6
            if (counts[0] == 3 && counts.Length > 1 && counts[1] == 2)
                return HandRank.FullHouse;                            // 5
            if (isSmallStraight) return HandRank.SmallStraight;       // 4
            if (counts[0] == 3) return HandRank.ThreeOfAKind;         // 3
            if (counts[0] == 2 && counts.Length > 1 && counts[1] == 2)
                return HandRank.TwoPairs;                             // 2
            if (counts[0] == 2) return HandRank.OnePair;              // 1
            return HandRank.HighCard;                                 // 0
        }

        /// <summary>大順：5 個相異點數且恰好連續。</summary>
        private static bool IsLargeStraight(int[] distinctSorted)
        {
            return distinctSorted.Length == 5 &&
                   distinctSorted[4] - distinctSorted[0] == 4;
        }

        /// <summary>是否存在長度達 runLength 的連續點數區段。</summary>
        private static bool HasConsecutiveRun(int[] distinctSorted, int runLength)
        {
            int run = 1;
            for (int i = 1; i < distinctSorted.Length; i++)
            {
                if (distinctSorted[i] == distinctSorted[i - 1] + 1)
                {
                    run++;
                    if (run >= runLength) return true;
                }
                else
                {
                    run = 1;
                }
            }
            return false;
        }
    }
}
