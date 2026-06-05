using System;

namespace AbyssProtocol.Core
{
    /// <summary>
    /// 亂數來源抽象。讓 Core 不直接相依 UnityEngine.Random，
    /// 並可在測試中注入固定種子取得可重現結果。
    /// </summary>
    public interface IRandom
    {
        /// <summary>回傳 [minInclusive, maxExclusive) 範圍內的整數。</summary>
        int Next(int minInclusive, int maxExclusive);

        /// <summary>回傳 [0.0, 1.0) 的浮點數。</summary>
        double NextDouble();
    }

    /// <summary>以 System.Random 實作的亂數來源（可選種子）。</summary>
    public sealed class SystemRandom : IRandom
    {
        private readonly Random _random;

        public SystemRandom()
        {
            _random = new Random();
        }

        public SystemRandom(int seed)
        {
            _random = new Random(seed);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
