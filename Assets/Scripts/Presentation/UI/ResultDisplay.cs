using TMPro;
using UnityEngine;
using AbyssProtocol.Core;

namespace AbyssProtocol.Presentation
{
    /// <summary>結算面板：顯示雙方牌型、勝負與本局（或 FG 累計）獲得金額。</summary>
    public sealed class ResultDisplay : MonoBehaviour
    {
        public TextMeshProUGUI HandText;
        public TextMeshProUGUI PayoutText;

        private static readonly Color Win = new Color(0.95f, 0.85f, 0.4f);
        private static readonly Color Lose = new Color(0.8f, 0.2f, 0.2f);

        public void Show(HandRank player, HandRank ai, Winner winner, int payout)
        {
            if (HandText != null)
            {
                HandText.text = "你: " + HandName(player) + "    撒旦: " + HandName(ai);
            }
            if (PayoutText != null)
            {
                bool won = winner == Winner.Player;
                PayoutText.color = won ? Win : Lose;
                PayoutText.text = won ? ("本局獲得  " + payout) : "撒旦獲勝  本局獲得 0";
            }
        }

        public void ShowFGTotal(int total)
        {
            if (HandText != null) HandText.text = "FREE GAME 結束";
            if (PayoutText != null)
            {
                PayoutText.color = Win;
                PayoutText.text = "FG 累計獲得  " + total;
            }
        }

        public static string HandName(HandRank rank)
        {
            switch (rank)
            {
                case HandRank.FiveOfAKind: return "五條";
                case HandRank.FourOfAKind: return "四條";
                case HandRank.LargeStraight: return "大順";
                case HandRank.FullHouse: return "葫蘆";
                case HandRank.SmallStraight: return "小順";
                case HandRank.ThreeOfAKind: return "三條";
                case HandRank.TwoPairs: return "兩對";
                case HandRank.OnePair: return "一對";
                default: return "散牌";
            }
        }
    }
}
