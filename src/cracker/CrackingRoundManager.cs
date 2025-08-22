using System;

namespace LethalSeedCracker3.src.cracker
{
    internal class CrackingRoundManager(int seed)
    {
        internal Random LevelRandom = new(seed);
        internal Random AnomalyRandom = new(seed + 5);
        internal Random EnemySpawnRandom = new(seed + 40);
        internal Random OutsideEnemySpawnRandom = new(seed + 41);
        internal Random BreakerBoxRandom = new(seed + 20);

        public static int GetRandomWeightedIndex(int[] weights, Random randomSeed)
        {
            if (weights == null || weights.Length == 0)
            {
                return -1;
            }
            int num = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] >= 0)
                {
                    num += weights[i];
                }
            }
            if (num <= 0)
            {
                return randomSeed.Next(0, weights.Length);
            }
            float num2 = (float)randomSeed.NextDouble();
            float num3 = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if (!(weights[i] <= 0f))
                {
                    num3 += weights[i] / (float)num;
                    if (num3 >= num2)
                    {
                        return i;
                    }
                }
            }
            return randomSeed.Next(0, weights.Length);
        }
    }
}
