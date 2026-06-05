using System.Collections.Generic;
using System.Linq;

namespace AbyssProtocol.Core
{
    /// <summary>
    /// Wild（百搭）自動最優解。將標記為 Wild 的骰子位置，
    /// 替換成能組成「最高牌型」的點數組合。
    ///
    /// 策略：
    /// - Wild 數量 &lt;= 3：對每顆 Wild 列舉 1..maxFace 的所有組合（最多 20^3 = 8000 種），
    ///   取使 PokerEvaluator 結果最高者；同階時取點數總和較高者。
    /// - Wild 數量 &gt;= 4：必定能湊成五條，直接把所有 Wild 設為剩餘非 Wild 點數
    ///   （若全為 Wild 則設為 maxFace），省去爆量列舉。
    /// </summary>
    public static class WildResolver
    {
        private const int BruteForceWildCap = 3;

        /// <summary>
        /// 解析 Wild。回傳一份新陣列，Wild 位置已被替換為最優點數，
        /// 非 Wild 位置維持原值。輸入長度不限定 5，但通常為 5。
        /// </summary>
        public static int[] Resolve(int[] values, bool[] isWild, int maxFace)
        {
            int[] result = (int[])values.Clone();

            List<int> wildIndices = new List<int>();
            for (int i = 0; i < isWild.Length; i++)
            {
                if (isWild[i]) wildIndices.Add(i);
            }

            if (wildIndices.Count == 0)
            {
                return result;
            }

            // 4 顆以上 Wild：直接湊五條。
            if (wildIndices.Count > BruteForceWildCap)
            {
                int target = maxFace;
                for (int i = 0; i < isWild.Length; i++)
                {
                    if (!isWild[i]) { target = values[i]; break; }
                }
                foreach (int idx in wildIndices)
                {
                    result[idx] = target;
                }
                return result;
            }

            // 1..3 顆 Wild：有界列舉求最優。
            int[] working = (int[])values.Clone();
            int[] best = null;
            HandRank bestRank = HandRank.HighCard;
            int bestTieBreak = -1;

            SearchAssignments(working, wildIndices, 0, maxFace,
                ref best, ref bestRank, ref bestTieBreak);

            return best;
        }

        private static void SearchAssignments(
            int[] working,
            List<int> wildIndices,
            int depth,
            int maxFace,
            ref int[] best,
            ref HandRank bestRank,
            ref int bestTieBreak)
        {
            if (depth == wildIndices.Count)
            {
                HandRank rank = PokerEvaluator.Evaluate(working);
                int tieBreak = working.Sum();

                if (best == null ||
                    rank > bestRank ||
                    (rank == bestRank && tieBreak > bestTieBreak))
                {
                    best = (int[])working.Clone();
                    bestRank = rank;
                    bestTieBreak = tieBreak;
                }
                return;
            }

            int index = wildIndices[depth];
            for (int v = 1; v <= maxFace; v++)
            {
                working[index] = v;
                SearchAssignments(working, wildIndices, depth + 1, maxFace,
                    ref best, ref bestRank, ref bestTieBreak);
            }
        }
    }
}
