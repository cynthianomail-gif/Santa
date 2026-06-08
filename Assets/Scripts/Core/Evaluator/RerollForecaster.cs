using System;

namespace AbyssProtocol.Core
{
    /// <summary>重擲預報結果：是否「差一顆就成大牌」、目標牌型、建議保留的骰子遮罩。</summary>
    public struct RerollForecast
    {
        public readonly bool HasForecast;
        public readonly HandRank Target;
        public readonly bool[] KeepMask; // true = 關鍵骰（建議保留 / 高亮），false = 建議重擲的那顆

        public RerollForecast(bool hasForecast, HandRank target, bool[] keepMask)
        {
            HasForecast = hasForecast;
            Target = target;
            KeepMask = keepMask;
        }
    }

    /// <summary>
    /// 重擲預報：判斷「只改變其中一顆骰子的點數」是否能組成比目前更高的「大牌」
    /// （葫蘆 FullHouse 以上）。若可，回報目標牌型與建議保留的 4 顆。
    /// 純 C#、零 UnityEngine 相依，供表現層計算金色脈動高亮用。
    /// </summary>
    public static class RerollForecaster
    {
        /// <summary>視為「大牌」的門檻：葫蘆（FullHouse）以上。</summary>
        public static bool IsBigHand(HandRank rank)
        {
            return (int)rank >= (int)HandRank.FullHouse;
        }

        /// <summary>
        /// 以目前 5 顆點數計算預報。maxFace 為骰子面數（D6/D12/D20）。
        /// 取「可達到的最高大牌」對應的設定；建議重擲那一顆，其餘為關鍵骰。
        /// </summary>
        public static RerollForecast Evaluate(int[] values, int maxFace)
        {
            if (values == null || values.Length != 5)
                return new RerollForecast(false, HandRank.HighCard, null);

            HandRank current = PokerEvaluator.Evaluate(values);
            HandRank best = HandRank.HighCard;
            int rerollIndex = -1;

            int[] tmp = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                for (int v = 1; v <= maxFace; v++)
                {
                    if (v == values[i]) continue; // 只看「改變一顆」的可能
                    Array.Copy(values, tmp, values.Length);
                    tmp[i] = v;
                    HandRank r = PokerEvaluator.Evaluate(tmp);

                    // 必須是大牌、且嚴格高於目前牌型（避免已成大牌時仍提示）
                    if (IsBigHand(r) && (int)r > (int)best && (int)r > (int)current)
                    {
                        best = r;
                        rerollIndex = i;
                    }
                }
            }

            if (rerollIndex < 0)
                return new RerollForecast(false, HandRank.HighCard, null);

            bool[] keep = new bool[values.Length];
            for (int i = 0; i < values.Length; i++) keep[i] = i != rerollIndex;
            return new RerollForecast(true, best, keep);
        }
    }
}
