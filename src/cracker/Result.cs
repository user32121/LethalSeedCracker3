using LethalSeedCracker3.src.common;
using System.Collections.Generic;
using System.Linq;

namespace LethalSeedCracker3.src.cracker
{
    internal class Result(int seed)
    {
        internal int seed = seed;
        internal EnemyType? infestation;
        internal Dictionary<EnemyType, int> enemies = [];
    }

    internal class FrozenResult(Result result)
    {
        internal int seed = result.seed;
        internal EnemyType infestation = Util.NonNull(result.infestation, nameof(result.infestation));
        internal Dictionary<EnemyType, int> enemies = result.enemies;

        public string ToFormattedString(string majorSeparator, string minorSeparator)
        {
            string enemyList = string.Join(", ", [.. from item in enemies select $"{item.Key.name}: {item.Value}"]);
            return $"seed: {seed}{majorSeparator}infestation: {infestation.name}";
        }
    }
}
