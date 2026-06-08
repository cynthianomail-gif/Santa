using System.Linq;

namespace AbyssProtocol.Core
{
    /// <summary>
    /// 深淵效果執行器。負責 Scry / Destroyer / Reroll 的實際作用。
    /// Wild 由 WildResolver 於 Evaluation 前單獨處理，不在此類。
    /// </summary>
    public sealed class AbyssEffectHandler
    {
        /// <summary>
        /// Scry（透視）：回傳 AI 目前所有骰子的點數，供表現層揭露。
        /// 在玩家重擲決策「之前」呼叫。
        /// </summary>
        public int[] HandleScry(Die[] aiDice)
        {
            if (aiDice == null) return new int[0];
            return aiDice.Select(d => d.Value).ToArray();
        }

        /// <summary>
        /// Destroyer（毀滅）：將 AI 當前「最大點數」的一顆骰子強制變為 1。
        /// 若同局觸發多顆 Destroyer，呼叫端應對每顆各執行一次本方法。
        /// 回傳被擊中的骰子索引（供表現層標示變化位置），無作用對象則回傳 -1。
        /// </summary>
        public int HandleDestroyer(Die[] aiDice)
        {
            if (aiDice == null || aiDice.Length == 0) return -1;

            int maxIndex = 0;
            for (int i = 1; i < aiDice.Length; i++)
            {
                if (aiDice[i].Value > aiDice[maxIndex].Value)
                {
                    maxIndex = i;
                }
            }
            aiDice[maxIndex].ForceValue(1);
            return maxIndex;
        }

        /// <summary>
        /// Reroll（輪迴）：玩家重擲次數上限 +1。
        /// 玩家可選擇不用完，由重擲階段控制。
        /// </summary>
        public void HandleReroll(GameContext context)
        {
            if (context == null) return;
            context.PlayerRerollLimit += 1;
        }
    }
}
