using TMPro;
using UnityEngine;
using AbyssProtocol.Core;

namespace AbyssProtocol.Presentation
{
    /// <summary>
    /// 得分面板（僅玩家贏時顯示）：上方倍數、分隔線、下方贏分。
    /// 牌型文字改顯示於骰子下方（見 UIManager.ShowHandLabels）。
    /// </summary>
    public sealed class ResultDisplay : MonoBehaviour
    {
        public TextMeshProUGUI HandText;   // 上方大字（倍數）
        public TextMeshProUGUI PayoutText; // 下方小字（贏分）

        private static readonly Color TopColor    = Color.white;
        private static readonly Color BottomColor = new Color(0.80f, 0.84f, 0.92f);

        /// <summary>顯示倍數與贏分（呼叫端只在玩家贏時呼叫）。</summary>
        public void Show(int payout, int bet)
        {
            float mult = bet > 0 ? (float)payout / bet : 0f;
            if (HandText != null)
            {
                HandText.color = TopColor;
                HandText.text = mult.ToString("0.00") + "×"; // ×
            }
            if (PayoutText != null)
            {
                PayoutText.color = BottomColor;
                PayoutText.text = "$" + payout.ToString("N0");
            }
        }

        public static string HandName(HandRank rank)
        {
            switch (rank)
            {
                case HandRank.FiveOfAKind:   return "Five of a Kind";
                case HandRank.FourOfAKind:   return "Four of a Kind";
                case HandRank.LargeStraight: return "Large Straight";
                case HandRank.FullHouse:     return "Full House";
                case HandRank.SmallStraight: return "Small Straight";
                case HandRank.ThreeOfAKind:  return "Three of a Kind";
                case HandRank.TwoPairs:      return "Two Pairs";
                case HandRank.OnePair:       return "One Pair";
                default:                     return "High Card";
            }
        }
    }
}
