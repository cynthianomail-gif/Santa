using System.Collections.Generic;
using System.Linq;

namespace AbyssProtocol.Core
{
    /// <summary>
    /// 計算「哪幾顆骰子實際參與了所判定的牌型」遮罩，供表現層在結算時
    /// 高亮參與骰、壓灰未參與骰。純 C#、零 UnityEngine 相依。
    /// </summary>
    public static class HandMaskEvaluator
    {
        /// <summary>回傳長度 5 的遮罩：true = 該骰參與牌型。</summary>
        public static bool[] ScoringMask(int[] values)
        {
            bool[] mask = new bool[values.Length];
            if (values.Length != 5) return mask;

            HandRank rank = PokerEvaluator.Evaluate(values);

            // 點數 → 出現次數
            Dictionary<int, int> counts = values
                .GroupBy(v => v)
                .ToDictionary(g => g.Key, g => g.Count());

            switch (rank)
            {
                case HandRank.FiveOfAKind:
                case HandRank.FullHouse:
                case HandRank.LargeStraight:
                    for (int i = 0; i < values.Length; i++) mask[i] = true;
                    break;

                case HandRank.FourOfAKind:
                    MarkValues(values, mask, ValuesWithCount(counts, 4));
                    break;

                case HandRank.ThreeOfAKind:
                    MarkValues(values, mask, ValuesWithCount(counts, 3));
                    break;

                case HandRank.TwoPairs:
                case HandRank.OnePair:
                    MarkValues(values, mask, ValuesWithCount(counts, 2));
                    break;

                case HandRank.SmallStraight:
                    MarkValues(values, mask, FourLongRun(counts.Keys));
                    break;

                default: // HighCard：標記最大點數那顆
                    MarkValues(values, mask, new HashSet<int> { values.Max() });
                    break;
            }
            return mask;
        }

        private static HashSet<int> ValuesWithCount(Dictionary<int, int> counts, int n)
        {
            var set = new HashSet<int>();
            foreach (var kv in counts) if (kv.Value == n) set.Add(kv.Key);
            return set;
        }

        private static void MarkValues(int[] values, bool[] mask, HashSet<int> targets)
        {
            for (int i = 0; i < values.Length; i++)
                if (targets.Contains(values[i])) mask[i] = true;
        }

        /// <summary>從相異點數中找出一段長度 4 的連續區段，回傳其點數集合。</summary>
        private static HashSet<int> FourLongRun(IEnumerable<int> distinctValues)
        {
            int[] sorted = distinctValues.Distinct().OrderBy(v => v).ToArray();
            for (int start = 0; start + 3 < sorted.Length; start++)
            {
                bool ok = true;
                for (int k = 1; k < 4; k++)
                    if (sorted[start + k] != sorted[start] + k) { ok = false; break; }
                if (ok)
                    return new HashSet<int> { sorted[start], sorted[start] + 1,
                                              sorted[start] + 2, sorted[start] + 3 };
            }
            return new HashSet<int>();
        }
    }
}
