using System;
using System.Collections.Generic;
using NUnit.Framework;
using AbyssProtocol.Core;

namespace AbyssProtocol.Tests
{
    /// <summary>
    /// Core 邏輯層的 Edit Mode 單元測試（NUnit）。
    /// 與 Tooling/VerifyCore 的 headless 驗證等價，方便在 Unity Test Runner 內執行。
    /// </summary>
    public class CoreLogicTests
    {
        /// <summary>可預先排定數值的亂數來源，讓狀態機流程完全可重現。</summary>
        private sealed class ScriptedRandom : IRandom
        {
            private readonly Queue<int> _ints;

            public ScriptedRandom(int[] values)
            {
                _ints = new Queue<int>(values);
            }

            public int Next(int minInclusive, int maxExclusive)
            {
                int v = _ints.Dequeue();
                if (v < minInclusive || v >= maxExclusive)
                {
                    throw new InvalidOperationException(
                        "ScriptedRandom value out of range: " + v);
                }
                return v;
            }

            public double NextDouble()
            {
                return 0.0;
            }
        }

        // ---------------- PokerEvaluator ----------------

        [Test]
        public void FiveD12_FullHouse_IsDetected()
        {
            Assert.AreEqual(HandRank.FullHouse,
                PokerEvaluator.Evaluate(new[] { 3, 3, 3, 7, 7 }));
        }

        [Test]
        public void LargeStraight_IsDetected()
        {
            Assert.AreEqual(HandRank.LargeStraight,
                PokerEvaluator.Evaluate(new[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void SmallStraight_IsDetected()
        {
            Assert.AreEqual(HandRank.SmallStraight,
                PokerEvaluator.Evaluate(new[] { 1, 2, 3, 4, 6 }));
        }

        [Test]
        public void FiveOfAKind_IsDetected()
        {
            Assert.AreEqual(HandRank.FiveOfAKind,
                PokerEvaluator.Evaluate(new[] { 5, 5, 5, 5, 5 }));
        }

        [Test]
        public void FourOfAKind_IsDetected()
        {
            Assert.AreEqual(HandRank.FourOfAKind,
                PokerEvaluator.Evaluate(new[] { 2, 2, 2, 2, 9 }));
        }

        [Test]
        public void TwoPairs_IsDetected()
        {
            Assert.AreEqual(HandRank.TwoPairs,
                PokerEvaluator.Evaluate(new[] { 8, 8, 3, 3, 1 }));
        }

        [Test]
        public void OnePair_IsDetected()
        {
            Assert.AreEqual(HandRank.OnePair,
                PokerEvaluator.Evaluate(new[] { 1, 2, 4, 6, 6 }));
        }

        [Test]
        public void HighCard_IsDetected()
        {
            Assert.AreEqual(HandRank.HighCard,
                PokerEvaluator.Evaluate(new[] { 2, 4, 6, 8, 11 }));
        }

        [Test]
        public void LargeStraight_OutranksFullHouse()
        {
            Assert.Greater((int)HandRank.LargeStraight, (int)HandRank.FullHouse);
        }

        [Test]
        public void Evaluate_WrongLength_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => PokerEvaluator.Evaluate(new[] { 1, 2, 3 }));
        }

        // ---------------- The spec's headline test ----------------

        [Test]
        public void FiveD12_FullHouse_ScoresExpectedPayout()
        {
            // 規格指定的驗收：5 顆 D12 擲出葫蘆的分數判定。
            HandRank rank = PokerEvaluator.Evaluate(new[] { 3, 3, 3, 7, 7 });
            Assert.AreEqual(HandRank.FullHouse, rank);

            // bet 100 x FullHouse(10) x D12(2.5) x special(1) = 2500
            int payout = PayoutCalculator.CalculatePayout(100, rank, DiceType.D12, 1.0f);
            Assert.AreEqual(2500, payout);
        }

        // ---------------- WildResolver ----------------

        [Test]
        public void Wild_CompletesFourOfAKind_OverFullHouse()
        {
            int[] resolved = WildResolver.Resolve(
                new[] { 3, 3, 3, 7, 0 },
                new[] { false, false, false, false, true }, 12);
            Assert.AreEqual(HandRank.FourOfAKind, PokerEvaluator.Evaluate(resolved));
        }

        [Test]
        public void TwoWilds_CompleteFiveOfAKind()
        {
            int[] resolved = WildResolver.Resolve(
                new[] { 0, 0, 7, 7, 7 },
                new[] { true, true, false, false, false }, 12);
            Assert.AreEqual(HandRank.FiveOfAKind, PokerEvaluator.Evaluate(resolved));
        }

        [Test]
        public void Wild_CompletesLargeStraight()
        {
            int[] resolved = WildResolver.Resolve(
                new[] { 2, 3, 4, 5, 0 },
                new[] { false, false, false, false, true }, 6);
            Assert.AreEqual(HandRank.LargeStraight, PokerEvaluator.Evaluate(resolved));
        }

        // ---------------- PayoutCalculator ----------------

        [Test]
        public void Payout_HighCard_IsZero()
        {
            Assert.AreEqual(0,
                PayoutCalculator.CalculatePayout(500, HandRank.HighCard, DiceType.D6, 1.0f));
        }

        [Test]
        public void Payout_FiveOfAKind_D20_Cursed6()
        {
            Assert.AreEqual(150000,
                PayoutCalculator.CalculatePayout(100, HandRank.FiveOfAKind, DiceType.D20, 6.0f));
        }

        [Test]
        public void Tie_AIWins()
        {
            Assert.AreEqual(Winner.AI,
                PayoutCalculator.DetermineWinner(HandRank.OnePair, HandRank.OnePair));
        }

        [Test]
        public void ResolveRound_Tie_PaysZero()
        {
            Assert.AreEqual(0,
                PayoutCalculator.ResolveRound(100, HandRank.OnePair, HandRank.OnePair,
                    DiceType.D6, 1.0f));
        }

        // ---------------- Abyss trigger ranges ----------------

        [Test]
        public void AbyssTrigger_D6()
        {
            Assert.IsTrue(GameConfig.IsAbyssTrigger(DiceType.D6, 6));
            Assert.IsFalse(GameConfig.IsAbyssTrigger(DiceType.D6, 5));
        }

        [Test]
        public void AbyssTrigger_D12()
        {
            Assert.IsTrue(GameConfig.IsAbyssTrigger(DiceType.D12, 11));
            Assert.IsTrue(GameConfig.IsAbyssTrigger(DiceType.D12, 12));
            Assert.IsFalse(GameConfig.IsAbyssTrigger(DiceType.D12, 10));
        }

        [Test]
        public void AbyssTrigger_D20()
        {
            Assert.IsTrue(GameConfig.IsAbyssTrigger(DiceType.D20, 17));
            Assert.IsTrue(GameConfig.IsAbyssTrigger(DiceType.D20, 20));
            Assert.IsFalse(GameConfig.IsAbyssTrigger(DiceType.D20, 16));
        }

        // ---------------- Destroyer ----------------

        [Test]
        public void Destroyer_SetsAIMaxDieToOne()
        {
            IRandom rng = new SystemRandom(1);
            Die[] ai = new Die[5];
            int[] forced = { 4, 9, 2, 12, 5 };
            for (int i = 0; i < 5; i++)
            {
                ai[i] = new Die(DiceType.D12, rng);
                ai[i].ForceValue(forced[i]);
            }

            new AbyssEffectHandler().HandleDestroyer(ai);
            Assert.AreEqual(1, ai[3].Value);
        }

        // ---------------- Special die probability ----------------

        [Test]
        public void Cursed_HitRate_AboutOneSixth()
        {
            IRandom rng = new SystemRandom(12345);
            int hits = 0;
            const int n = 6000;
            for (int i = 0; i < n; i++)
            {
                SpecialDie d = new SpecialDie(SpecialDiceKind.Cursed, rng);
                d.Roll();
                if (d.Multiplier == 6f) hits++;
            }
            Assert.That(Math.Abs(hits - n / 6.0), Is.LessThan(200));
        }

        [Test]
        public void FGTrigger_HitRate_AboutOneSixth()
        {
            IRandom rng = new SystemRandom(54321);
            int hits = 0;
            const int n = 6000;
            for (int i = 0; i < n; i++)
            {
                SpecialDie d = new SpecialDie(SpecialDiceKind.FGTrigger, rng);
                d.Roll();
                if (d.FGTriggered) hits++;
            }
            Assert.That(Math.Abs(hits - n / 6.0), Is.LessThan(200));
        }

        // ---------------- Full FSM flow ----------------

        [Test]
        public void FullRound_General_NoFG_PaysAndSettles()
        {
            int[] script =
            {
                3, 3, 3, 5, 5,   // player => FullHouse
                1, 1, 2, 2, 4,   // ai => TwoPairs
                3                // FGTrigger face 3 => no FG
            };
            GameStateMachine fsm = new GameStateMachine(new ScriptedRandom(script));

            int settled = -1;
            fsm.RoundSettled += s => settled = s;

            fsm.ConfigureRound(GameMode.General, DiceType.D6, 100, SpecialDiceKind.None);
            fsm.BeginRound();
            Assert.AreEqual(GamePhase.PlayerReroll, fsm.CurrentPhase);

            fsm.EndReroll();
            Assert.AreEqual(GamePhase.Settlement, fsm.CurrentPhase);
            Assert.AreEqual(1000, fsm.Context.LastPayout);
            Assert.AreEqual(1000, settled);
            Assert.AreEqual(1000, fsm.Context.SessionHighScore);

            fsm.AcknowledgeSettlement();
            Assert.AreEqual(GamePhase.Idle, fsm.CurrentPhase);
        }

        [Test]
        public void FullRound_TriggersFG_FiveRounds_Accumulates()
        {
            List<int> script = new List<int>();
            script.AddRange(new[] { 3, 3, 3, 5, 5, 1, 1, 2, 2, 4, 0 }); // trigger FG
            for (int r = 0; r < 5; r++)
            {
                script.AddRange(new[] { 3, 3, 3, 5, 5, 1, 1, 2, 2, 4, 2 }); // DoubleEdge x1
            }

            GameStateMachine fsm = new GameStateMachine(new ScriptedRandom(script.ToArray()));

            int fgFinished = -1;
            fsm.FGFinished += t => fgFinished = t;

            fsm.ConfigureRound(GameMode.General, DiceType.D6, 100, SpecialDiceKind.None);
            fsm.BeginRound();
            fsm.EndReroll();
            Assert.AreEqual(GamePhase.FGTransition, fsm.CurrentPhase);

            fsm.ChooseFGSpecialDie(SpecialDiceKind.DoubleEdge);
            for (int r = 0; r < 5; r++)
            {
                Assert.AreEqual(GamePhase.PlayerReroll, fsm.CurrentPhase);
                fsm.EndReroll();
            }

            Assert.AreEqual(GamePhase.Settlement, fsm.CurrentPhase);
            Assert.AreEqual(6000, fgFinished); // 1000 trigger + 5x1000
            Assert.AreEqual(6000, fsm.Context.SessionHighScore);

            fsm.AcknowledgeSettlement();
            Assert.AreEqual(GamePhase.Idle, fsm.CurrentPhase);
            Assert.IsFalse(fsm.Context.IsInFG);
        }
    }
}
