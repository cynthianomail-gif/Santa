using System;
using System.Collections.Generic;
using AbyssProtocol.Core;

namespace AbyssProtocol.Verify
{
    /// <summary>
    /// 提供預先排定數值的亂數來源，讓狀態機流程完全可重現。
    /// Next(min,max) 依序回傳佇列中的整數（會驗證落在範圍內）。
    /// </summary>
    internal sealed class ScriptedRandom : IRandom
    {
        private readonly Queue<int> _ints;

        public ScriptedRandom(int[] values)
        {
            _ints = new Queue<int>(values);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (_ints.Count == 0)
                throw new InvalidOperationException("ScriptedRandom int queue exhausted.");
            int v = _ints.Dequeue();
            if (v < minInclusive || v >= maxExclusive)
                throw new InvalidOperationException(
                    "ScriptedRandom value " + v + " out of range [" +
                    minInclusive + "," + maxExclusive + ").");
            return v;
        }

        public double NextDouble()
        {
            return 0.0;
        }
    }

    internal static class Program
    {
        private static int _passed;
        private static int _failed;

        private static int Main()
        {
            Console.WriteLine("=== Abyss Protocol Core Verification ===\n");

            TestPokerEvaluator();
            TestWildResolver();
            TestPayoutCalculator();
            TestAbyssTriggerRanges();
            TestDestroyer();
            TestSpecialDieProbability();
            TestFullRoundGeneralNoFG();

            Console.WriteLine("\n=== Results: " + _passed + " passed, " + _failed + " failed ===");
            return _failed == 0 ? 0 : 1;
        }

        // ---------- PokerEvaluator ----------
        private static void TestPokerEvaluator()
        {
            Section("PokerEvaluator");
            AssertRank(new[] { 3, 3, 3, 7, 7 }, HandRank.FullHouse, "5xD12 [3,3,3,7,7] => FullHouse");
            AssertRank(new[] { 1, 2, 3, 4, 5 }, HandRank.LargeStraight, "[1,2,3,4,5] => LargeStraight");
            AssertRank(new[] { 1, 2, 3, 4, 6 }, HandRank.SmallStraight, "[1,2,3,4,6] => SmallStraight");
            AssertRank(new[] { 5, 5, 5, 5, 5 }, HandRank.FiveOfAKind, "[5,5,5,5,5] => FiveOfAKind");
            AssertRank(new[] { 2, 2, 2, 2, 9 }, HandRank.FourOfAKind, "[2,2,2,2,9] => FourOfAKind");
            AssertRank(new[] { 1, 2, 4, 6, 6 }, HandRank.OnePair, "[1,2,4,6,6] => OnePair");
            AssertRank(new[] { 8, 8, 3, 3, 1 }, HandRank.TwoPairs, "[8,8,3,3,1] => TwoPairs");
            AssertRank(new[] { 7, 7, 7, 2, 9 }, HandRank.ThreeOfAKind, "[7,7,7,2,9] => ThreeOfAKind");
            AssertRank(new[] { 2, 4, 6, 8, 11 }, HandRank.HighCard, "[2,4,6,8,11] => HighCard");
            // 大順優先於葫蘆無法同時成立；驗證 rank 數值關係。
            AssertTrue((int)HandRank.LargeStraight > (int)HandRank.FullHouse,
                "LargeStraight rank > FullHouse rank");
        }

        // ---------- WildResolver ----------
        private static void TestWildResolver()
        {
            Section("WildResolver");

            // [3,3,3,7,Wild] D12 -> 最佳為 FourOfAKind(Wild=3)，高於 FullHouse(Wild=7)
            int[] r1 = WildResolver.Resolve(
                new[] { 3, 3, 3, 7, 0 },
                new[] { false, false, false, false, true }, 12);
            AssertRank(r1, HandRank.FourOfAKind, "[3,3,3,7,W] => FourOfAKind");

            // [W,W,7,7,7] D6 -> FiveOfAKind
            int[] r2 = WildResolver.Resolve(
                new[] { 0, 0, 7, 7, 7 },
                new[] { true, true, false, false, false }, 12);
            AssertRank(r2, HandRank.FiveOfAKind, "[W,W,7,7,7] => FiveOfAKind");

            // 單 Wild 補大順：[2,3,4,5,W] -> LargeStraight
            int[] r3 = WildResolver.Resolve(
                new[] { 2, 3, 4, 5, 0 },
                new[] { false, false, false, false, true }, 6);
            AssertRank(r3, HandRank.LargeStraight, "[2,3,4,5,W] => LargeStraight");

            // 4 顆 Wild 的捷徑路徑 -> FiveOfAKind
            int[] r4 = WildResolver.Resolve(
                new[] { 0, 0, 0, 0, 9 },
                new[] { true, true, true, true, false }, 20);
            AssertRank(r4, HandRank.FiveOfAKind, "[W,W,W,W,9] => FiveOfAKind");
        }

        // ---------- PayoutCalculator ----------
        private static void TestPayoutCalculator()
        {
            Section("PayoutCalculator");

            AssertInt(PayoutCalculator.CalculatePayout(100, HandRank.FullHouse, DiceType.D12, 1.0f),
                2500, "bet100 FullHouse D12 x1 => 2500");
            AssertInt(PayoutCalculator.CalculatePayout(500, HandRank.HighCard, DiceType.D6, 1.0f),
                0, "bet500 HighCard D6 => 0");
            AssertInt(PayoutCalculator.CalculatePayout(100, HandRank.FiveOfAKind, DiceType.D20, 6.0f),
                150000, "bet100 FiveOfAKind D20 xCursed6 => 150000");

            // 勝負判定：平手莊家勝
            AssertTrue(PayoutCalculator.DetermineWinner(HandRank.OnePair, HandRank.OnePair) == Winner.AI,
                "Tie => AI wins");
            AssertTrue(PayoutCalculator.DetermineWinner(HandRank.FullHouse, HandRank.OnePair) == Winner.Player,
                "Higher => Player wins");
            AssertInt(PayoutCalculator.ResolveRound(100, HandRank.OnePair, HandRank.OnePair, DiceType.D6, 1.0f),
                0, "Tie pays 0");
        }

        // ---------- Abyss trigger ranges ----------
        private static void TestAbyssTriggerRanges()
        {
            Section("Abyss Trigger Ranges");

            AssertTrue(GameConfig.IsAbyssTrigger(DiceType.D6, 6) &&
                       !GameConfig.IsAbyssTrigger(DiceType.D6, 5), "D6 trigger = {6}");
            AssertTrue(GameConfig.IsAbyssTrigger(DiceType.D12, 11) &&
                       GameConfig.IsAbyssTrigger(DiceType.D12, 12) &&
                       !GameConfig.IsAbyssTrigger(DiceType.D12, 10), "D12 trigger = {11,12}");
            AssertTrue(GameConfig.IsAbyssTrigger(DiceType.D20, 17) &&
                       GameConfig.IsAbyssTrigger(DiceType.D20, 20) &&
                       !GameConfig.IsAbyssTrigger(DiceType.D20, 16), "D20 trigger = {17..20}");
        }

        // ---------- Destroyer ----------
        private static void TestDestroyer()
        {
            Section("Destroyer Effect");

            IRandom rng = new SystemRandom(1);
            Die[] ai = new Die[5];
            int[] forced = { 4, 9, 2, 12, 5 };
            for (int i = 0; i < 5; i++)
            {
                ai[i] = new Die(DiceType.D12, rng);
                ai[i].ForceValue(forced[i]);
            }

            new AbyssEffectHandler().HandleDestroyer(ai);
            AssertInt(ai[3].Value, 1, "Destroyer sets AI max die (12) -> 1");
        }

        // ---------- Special die probability ----------
        private static void TestSpecialDieProbability()
        {
            Section("Special Die Probability (statistical)");

            IRandom rng = new SystemRandom(12345);
            int cursedHits = 0;
            const int n = 6000;

            for (int i = 0; i < n; i++)
            {
                SpecialDie cursed = new SpecialDie(SpecialDiceKind.Cursed, rng);
                cursed.Roll();
                if (cursed.Multiplier == 6f) cursedHits++;
            }

            double expected = n / 6.0;
            AssertTrue(Math.Abs(cursedHits - expected) < 200,
                "Cursed x6 ~1/6 (got " + cursedHits + " / exp ~" + (int)expected + ")");
        }

        // ---------- Full round: General, no FG ----------
        private static void TestFullRoundGeneralNoFG()
        {
            Section("FSM: General round (no FG)");

            // 玩家 [3,3,3,5,5]=FullHouse, AI [1,1,2,2,4]=TwoPairs, 特殊骰 None face=3
            int[] script =
            {
                3, 3, 3, 5, 5,   // player standard
                1, 1, 2, 2, 4,   // ai standard
                3                // special (General=None，僅消耗一個亂數)
            };
            GameStateMachine fsm = new GameStateMachine(new ScriptedRandom(script));

            int settled = -1;
            fsm.RoundSettled += s => settled = s;

            fsm.ConfigureRound(GameMode.General, DiceType.D6, 100, SpecialDiceKind.None);
            fsm.BeginRound();
            AssertTrue(fsm.CurrentPhase == GamePhase.PlayerReroll, "reaches PlayerReroll");
            fsm.EndReroll();
            AssertTrue(fsm.CurrentPhase == GamePhase.Settlement, "reaches Settlement");

            // FullHouse(10) x D6(1.0) x special(1.0) x bet100 = 1000
            AssertInt(fsm.Context.LastPayout, 1000, "payout = 1000");
            AssertInt(settled, 1000, "RoundSettled event = 1000");
            AssertInt(fsm.Context.SessionHighScore, 1000, "high score = 1000");

            fsm.AcknowledgeSettlement();
            AssertTrue(fsm.CurrentPhase == GamePhase.Idle, "returns to Idle");
        }

        // ---------- assert helpers ----------
        private static void Section(string name)
        {
            Console.WriteLine("-- " + name);
        }

        private static void AssertRank(int[] values, HandRank expected, string label)
        {
            HandRank actual = PokerEvaluator.Evaluate(values);
            Record(actual == expected, label,
                "expected " + expected + " got " + actual);
        }

        private static void AssertInt(int actual, int expected, string label)
        {
            Record(actual == expected, label, "expected " + expected + " got " + actual);
        }

        private static void AssertTrue(bool condition, string label)
        {
            Record(condition, label, "condition false");
        }

        private static void Record(bool ok, string label, string failDetail)
        {
            if (ok)
            {
                _passed++;
                Console.WriteLine("   [PASS] " + label);
            }
            else
            {
                _failed++;
                Console.WriteLine("   [FAIL] " + label + " (" + failDetail + ")");
            }
        }
    }
}
