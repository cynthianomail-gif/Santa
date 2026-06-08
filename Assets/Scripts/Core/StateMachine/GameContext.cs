namespace AbyssProtocol.Core
{
    /// <summary>
    /// 狀態機共享資料包。持有「當前牌局」的所有資料，
    /// 在各 State 之間傳遞。表現層也讀取此物件來繪製畫面。
    /// </summary>
    public sealed class GameContext
    {
        // ---- 玩家於 Idle 階段的選擇 ----
        public GameMode Mode;
        public DiceType ActiveDiceType;
        public SpecialDiceKind SpecialDice;
        public int BaseBet = GameConfig.DefaultBet;

        // ---- 本局骰子 ----
        public Die[] PlayerDice;          // 5 顆標準骰
        public SpecialDie PlayerSpecialDie; // 第 6 顆特殊骰
        public Die[] AIDice;              // 5 顆標準骰

        // ---- 重擲 ----
        public int PlayerRerollLimit = GameConfig.DefaultRerollLimit;
        public int RerollsUsed;

        // ---- 結算紀錄 ----
        public int LastPayout;       // 最近一局獲得金額
        public int SessionHighScore; // 本 Session 最高單局

        /// <summary>重置與「單一牌局」相關的暫態，保留跨局/跨 Session 紀錄。</summary>
        public void ResetRoundState()
        {
            PlayerRerollLimit = GameConfig.DefaultRerollLimit;
            RerollsUsed = 0;
            LastPayout = 0;

            if (PlayerDice != null)
            {
                for (int i = 0; i < PlayerDice.Length; i++) PlayerDice[i].Reset();
            }
            if (AIDice != null)
            {
                for (int i = 0; i < AIDice.Length; i++) AIDice[i].Reset();
            }
        }

        /// <summary>取得玩家 5 顆標準骰的當前點數。</summary>
        public int[] GetPlayerValues()
        {
            int[] values = new int[PlayerDice.Length];
            for (int i = 0; i < PlayerDice.Length; i++) values[i] = PlayerDice[i].Value;
            return values;
        }

        /// <summary>取得 AI 5 顆標準骰的當前點數。</summary>
        public int[] GetAIValues()
        {
            int[] values = new int[AIDice.Length];
            for (int i = 0; i < AIDice.Length; i++) values[i] = AIDice[i].Value;
            return values;
        }

        /// <summary>更新最高分紀錄。</summary>
        public void TryUpdateHighScore(int score)
        {
            if (score > SessionHighScore) SessionHighScore = score;
        }
    }
}
