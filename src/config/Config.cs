using LethalSeedCracker3.src.cracker;
using System;
using System.Collections.Generic;
using System.IO;

namespace LethalSeedCracker3.src.config
{
    internal class Config
    {
        internal int min_seed = 0;
        internal int max_seed = 100;
        internal SelectableLevel currentLevel;
        internal int daysUntilDeadline = 3;
        internal int daysPlayersSurvivedInARow = 0;
        internal int connectedPlayersAmount = 1;
        internal bool isAnniversary = false;
        internal bool eclipsed = false;

        private static readonly Func<Config, string, int> ParseInt = (config, s) => int.Parse(s);
        private static readonly List<BaseConfigCommand> commands =
        [
            new ConfigParameter<int, int>("seed", ParseInt, "min", ParseInt, "max", (config, min, max) => {
                config.min_seed = min;
                config.max_seed = max;
            }),
            new ConfigParameter<SelectableLevel>("moon", ParseMoon, "moon", (config, moon) => config.currentLevel = moon),
            new ConfigParameter<int>("daystildeadline", ParseInt, "days", (config, days) => config.daysUntilDeadline = days),
            new ConfigParameter<int>("dayssurvived", ParseInt, "days", (config, days) => config.daysPlayersSurvivedInARow = days),
            new ConfigParameter<int>("players", ParseInt, "players", (config, players) => config.connectedPlayersAmount = players),
            new ConfigParameter("anniversary", config => config.isAnniversary = true),
            new ConfigParameter("eclipsed", config => config.eclipsed = true),
            new ConfigFilter<EnemyType?>("infestation", ParseEnemy, null, "enemy", (result, enemy) => enemy == null || enemy == result.infestation),
            new ConfigFilters<EnemyType, Func<float, float, bool>, int>("enemy", ParseEnemy, "enemy", ParseComparator, "comparator", ParseInt, "num", (result, enemy, op, num) => {
                for (int i = 0; i < enemy.Count; ++i) {
                    if(!op[i](result.enemies[enemy[i]], num[i])) {
                        return false;
                    }
                }
                return true;
            }),
        ];

        //convenience name mappings
        private static readonly Dictionary<string, string> colloquialNames = new()
        {
            ["oldbird"] = "radmech",
            ["bracken"] = "flowerman",
            ["lootbug"] = "hoarderbug",
            ["mimic"] = "masked",
        };
        private static readonly Dictionary<string, Func<float, float, bool>> comparators = new()
        {
            [">"] = (x, y) => x > y,
            ["<"] = (x, y) => x < y,
            [">="] = (x, y) => x >= y,
            ["<="] = (x, y) => x <= y,
            ["=="] = (x, y) => x == y,
        };

        private static bool IContains(string text, string substr) => substr.Length > 0 && text.Contains(substr, StringComparison.InvariantCultureIgnoreCase);

        public Config(string fileName)
        {
            //ensure folder exists
            string folderPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "32121", "LethalSeedCracker");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            //create sample config
            string filePath = Path.Join(folderPath, fileName);
            LethalSeedCracker3.Logger.LogInfo($"Loading config: {filePath}");
            if (!File.Exists(filePath))
            {
                using StreamWriter file = new(File.Open(filePath, FileMode.Create));
                file.WriteLine("seed 0 100");
                file.WriteLine();
                file.WriteLine("moon experimentation");
                file.WriteLine();
                file.WriteLine("daystildeadline 1");
                file.WriteLine("dayssurvived 0");
                file.WriteLine();
                file.WriteLine("infestation lootbug");
                file.WriteLine("enemy lootbug > 0");
                file.WriteLine();
                file.WriteLine("#this is a comment");
            }

            //read config
            {
                using StreamReader file = new(File.OpenRead(filePath));
                while (!file.EndOfStream)
                {
                    string[] line = file.ReadLine().Split('#')[0].Trim().Split();
                    if (line.Length == 0 || line.Length == 1 && line[0].Length == 0)
                    {
                        continue;
                    }
                    for (int i = 0; i < line.Length; ++i)
                    {
                        foreach (var item in colloquialNames)
                        {
                            if (IContains(item.Key, line[i]))
                            {
                                LethalSeedCracker3.Logger.LogInfo($"Substituting {line[i]} to {item.Value}");
                                line[i] = item.Value;
                            }
                        }
                    }
                    LethalSeedCracker3.Logger.LogInfo($"Processing line: {string.Join(" ", line)}");
                    string cmd = line[0].ToLower();
                    foreach (var item in commands)
                    {
                        if (item.cmd == cmd)
                        {
                            item.Parse(this, line[1..]);
                            goto PARSED_COMMAND;
                        }
                    }
                    PrintCommands();
                    throw new Exception($"unknown command: {line[0]}");
                PARSED_COMMAND:;
                }
            }

            if (currentLevel is null)
            {
                PrintMoons();
                throw new Exception("No moon set. Set the current moon using \"moon\" e.g. \"moon experimentation\"");
            }
            LethalSeedCracker3.Logger.LogInfo("Successfully loaded config");
        }

        internal bool Filter(FrozenResult result)
        {
            foreach (var item in commands)
            {
                if (item is IConfigFilter cf)
                {
                    if (!cf.Filter(result))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static SelectableLevel ParseMoon(Config _, string name)
        {
            foreach (var level in StartOfRound.Instance.levels)
            {
                if (IContains(level.name, name) || IContains(level.PlanetName, name) || IContains(level.sceneName, name))
                {
                    return level;
                }
            }
            PrintMoons();
            throw new Exception($"Unrecognized moon: {name}");
        }

        private static EnemyType ParseEnemy(Config config, string name)
        {
            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before configuring enemies.");
            }
            foreach (var item in config.currentLevel.Enemies)
            {
                if (IContains(item.enemyType.name, name) || IContains(item.enemyType.enemyName, name))
                {
                    return item.enemyType;
                }
            }
            foreach (var item in config.currentLevel.OutsideEnemies)
            {
                if (IContains(item.enemyType.name, name) || IContains(item.enemyType.enemyName, name))
                {
                    return item.enemyType;
                }
            }
            foreach (var item in config.currentLevel.DaytimeEnemies)
            {
                if (IContains(item.enemyType.name, name) || IContains(item.enemyType.enemyName, name))
                {
                    return item.enemyType;
                }
            }
            PrintEnemies(config);
            throw new Exception($"Unrecognized enemy: {name}");
        }

        private static Item ParseScrap(Config config, string name)
        {
            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before configuring scrap.");
            }
            foreach (var item in config.currentLevel.spawnableScrap)
            {
                if (IContains(item.spawnableItem.name, name) || IContains(item.spawnableItem.itemName, name))
                {
                    return item.spawnableItem;
                }
            }
            PrintScrap(config);
            throw new Exception($"Unrecognized scrap: {name}");
        }

        private static Func<float, float, bool> ParseComparator(Config _, string op)
        {
            if (!comparators.ContainsKey(op))
            {
                PrintComparators();
                throw new Exception($"Unrecognized operator: {op}");
            }
            return comparators[op];
        }

        private static void PrintCommands()
        {
            LethalSeedCracker3.Logger.LogInfo("Commands:");
            foreach (var item in commands)
            {
                LethalSeedCracker3.Logger.LogInfo($"  {item}");
            }
        }

        private static void PrintMoons()
        {
            LethalSeedCracker3.Logger.LogInfo("Moons:");
            foreach (var level in StartOfRound.Instance.levels)
            {
                LethalSeedCracker3.Logger.LogInfo($"  {level.name}, {level.PlanetName}, {level.sceneName}");
            }
        }

        private static void PrintEnemies(Config config)
        {
            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before listing enemies.");
            }
            LethalSeedCracker3.Logger.LogInfo("Enemies:");
            foreach (var item in config.currentLevel.Enemies)
            {
                LethalSeedCracker3.Logger.LogInfo($"  {item.enemyType.name}, {item.enemyType.enemyName}");
            }
            foreach (var item in config.currentLevel.OutsideEnemies)
            {
                LethalSeedCracker3.Logger.LogInfo($"  {item.enemyType.name}, {item.enemyType.enemyName}");
            }
            foreach (var item in config.currentLevel.DaytimeEnemies)
            {
                LethalSeedCracker3.Logger.LogInfo($"  {item.enemyType.name}, {item.enemyType.enemyName}");
            }
        }

        private static void PrintScrap(Config config)
        {
            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before listing scrap.");
            }
            LethalSeedCracker3.Logger.LogInfo("Scrap:");
            foreach (var item in config.currentLevel.spawnableScrap)
            {
                LethalSeedCracker3.Logger.LogInfo($"  {item.spawnableItem.name}, {item.spawnableItem.itemName}");
            }
        }

        private static void PrintComparators()
        {
            LethalSeedCracker3.Logger.LogInfo("Comparators:");
            foreach (var item in comparators)
            {
                LethalSeedCracker3.Logger.LogInfo($"  {item.Key}");
            }
        }
    }
}
