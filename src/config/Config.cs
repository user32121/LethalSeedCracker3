using LethalSeedCracker3.src.cracker;
using System;
using System.Collections.Generic;
using System.IO;

namespace LethalSeedCracker3.src.config
{
    internal class Config
    {
        internal bool verbose = false;
        internal int min_seed = 0;
        internal int max_seed = 100;
        internal SelectableLevel currentLevel;
        internal int daysUntilDeadline = 3;
        internal int daysPlayersSurvivedInARow = 0;
        internal int connectedPlayersAmount = 1;
        internal bool isAnniversary = false;
        internal bool eclipsed = false;
        internal int doorCount = 0;

        private static readonly Func<Config, string, int> ParseInt = (config, s) => int.Parse(s);
        private static readonly Func<Config, string, float> ParseFloat = (config, s) => float.Parse(s);
        private static readonly List<BaseConfigCommand> commands =
        [
            new ConfigParameter("verbose", config => config.verbose= true),
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
            new ConfigParameter<int>("setdoorcount", ParseInt, "num", (config, num) => config.doorCount = num),

            new ConfigFilter<EnemyType?>("infestation", ParseEnemy, null, "enemy", (result, enemy) => enemy == null || enemy == result.infestation),
            new ConfigFilters<EnemyType, Func<float, float, bool>, int>("enemy", ParseEnemy, "enemy", ParseComparator, "comparator", ParseInt, "num", (result, enemies, ops, nums) => {
                for (int i = 0; i < enemies.Count; ++i) {
                    if (!ops[i](result.enemies.GetValueOrDefault(enemies[i], 0), nums[i])) {
                        return false;
                    }
                }
                return true;
            }, validation: CheckEnemyPower),
            new ConfigFilter<DungeonType?>("dungeon", (config, s) => ParseEnum<DungeonType>(config, s), null, "dungeon", (result, dungeon) => dungeon == null || result.currentDungeonType == dungeon),
            new ConfigFilter("indoorfog", (result, active) => !active || result.indoorFog),
            new ConfigFilters<Item, Func<float, float, bool>, int>("item", ParseScrap, "scrap", ParseComparator, "comparator", ParseInt, "num", (result, scraps, ops, nums) => {
                for (int i = 0; i < scraps.Count; ++i) {
                    if (!ops[i](result.scrapCounts.GetValueOrDefault(scraps[i], 0), nums[i])) {
                        return false;
                    }
                }
                return true;
            }),
            new ConfigFilter<Item?>("singleitemday", ParseScrap, null, "scrap", (result, scrap) => scrap == null || scrap == result.singleItemDay),
            new ConfigFilters<LevelWeatherType,SelectableLevel>("weather", ParseEnum<LevelWeatherType>, "weather", ParseMoon, "moon", (result, weathers, moons) => {
                for (int i = 0; i < weathers.Count; ++i) {
                    if (result.weathers.GetValueOrDefault(moons[i], LevelWeatherType.None) != weathers[i]) {
                        return false;
                    }
                }
                return true;
            }),
            new ConfigFilters<SelectableLevel>("anyweather", ParseMoon, "moon", (result, moons) => {
                for (int i = 0; i < moons.Count; ++i) {
                    if (!result.weathers.ContainsKey(moons[i])) {
                        return false;
                    }
                }
                return true;
            }),
            new ConfigFilter("meteor", (result, active) => !active || result.meteorShower),
            new ConfigFilter<Func<float, float, bool>?, float>("meteortime", ParseComparator, null, "comparator", ParseFloat, 0, "time", (result, op, time) => op == null || op(result.meteorShowerAtTime, time)),
            new ConfigFilter("blackout", (result, active) => !active || result.blackout),
            new ConfigFilters<string, Func<float, float, bool>, int>("trap", ParseTrap, "trap", ParseComparator, "comparator", ParseInt, "num", (result, traps, ops, nums) => {
                for (int i = 0; i < traps.Count; ++i) {
                    if (!ops[i](result.traps.GetValueOrDefault(traps[i], 0), nums[i])) {
                        return false;
                    }
                }
                return true;
            }),
            new ConfigFilter<CompanyMood?>("companymood", ParseCompanyMood, null, "mood", (result, mood) => mood == null || mood == result.currentCompanyMood),
            new ConfigFilter<Func<float, float, bool>?, int>("lightning", ParseComparator, null, "comparator", ParseInt, 0, "num", (result, op, num) => op == null || op(result.lightningCount, num)),
            new ConfigFilter<Func<float, float, bool>?, int>("lockeddoors", ParseComparator, null, "comparator", ParseInt, 0, "num", (result, op, num) => op == null || op(result.lockedDoors, num)),
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

        private static T ParseEnum<T>(Config _, string name) where T : Enum
        {
            foreach (var item in (T[])Enum.GetValues(typeof(T)))
            {
                if (IContains(item.ToString(), name))
                {
                    return item;
                }
            }
            PrintEnums<T>();
            throw new Exception($"Unrecognized {typeof(T).Name}: {name}");
        }

        private static CompanyMood ParseCompanyMood(Config _, string name)
        {
            foreach (var item in TimeOfDay.Instance.CommonCompanyMoods)
            {
                if (IContains(item.name, name))
                {
                    return item;
                }
            }
            PrintCompanyMoods();
            throw new Exception($"Unrecognized company mood: {name}");
        }

        private static string ParseTrap(Config config, string name)
        {
            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before configuring traps.");
            }
            foreach (var item in config.currentLevel.spawnableMapObjects)
            {
                if (IContains(item.prefabToSpawn.name, name))
                {
                    return item.prefabToSpawn.name;
                }
            }
            PrintTraps(config);
            throw new Exception($"Unrecognized trap: {name}");
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

        private static void PrintEnums<T>() where T : Enum
        {
            LethalSeedCracker3.Logger.LogInfo($"{typeof(T).Name}s:");
            foreach (var item in (T[])Enum.GetValues(typeof(T)))
            {
                LethalSeedCracker3.Logger.LogInfo($"  {item}");
            }
        }

        private static void PrintCompanyMoods()
        {
            LethalSeedCracker3.Logger.LogInfo("Company moods:");
            foreach (var item in TimeOfDay.Instance.CommonCompanyMoods)
            {
                LethalSeedCracker3.Logger.LogInfo($"{item}: timeToWaitBeforeGrabbingItem: {item.timeToWaitBeforeGrabbingItem}, irritability: {item.irritability}, judgementSpeed: {item.judgementSpeed}, startingPatience: {item.startingPatience}, mustBeWokenUp: {item.mustBeWokenUp}, maximumItemsToAnger: {item.maximumItemsToAnger}, sensitivity: {item.sensitivity}, manifestation: {item.manifestation}");
            }
        }

        private static void PrintTraps(Config config)
        {
            LethalSeedCracker3.Logger.LogInfo("Trap:");
            foreach (var item in config.currentLevel.spawnableScrap)
            {
                LethalSeedCracker3.Logger.LogInfo($"{item.spawnableItem.name}, {item.spawnableItem.itemName}");
            }
        }

        //Find the smallest int that satisfies f
        private static int MinNum(Func<float, bool> f)
        {
            if (f(0))
            {
                return 0;
            }
            int l = 0;
            int r = 1;
            while (!f(r))
            {
                l = r;
                r *= 2;
            }
            while (l < r)
            {
                int m = (l + r) / 2;
                if (f(m))
                {
                    r = m - 1;
                }
                else
                {
                    l = m + 1;
                }
            }
            if (f(l))
            {
                return l;
            }
            else
            {
                return l - 1;
            }
        }

        private static void CheckEnemyPower(Config config, List<EnemyType> enemies, List<Func<float, float, bool>> ops, List<int> nums)
        {
            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before checking enemies.");
            }
            float insidePower = 0;
            float outsidePower = 0;
            float daytimePower = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                int count = MinNum(x => ops[i](x, nums[i]));
                EnemyType enemy = enemies[i];
                if (count > enemy.MaxCount)
                {
                    throw new Exception($"Requested at least {count} {enemy.name}, but {enemy.MaxCount} is the maximum.");
                }

                foreach (var item in config.currentLevel.Enemies)
                {
                    if (item.enemyType == enemy)
                    {
                        insidePower += item.enemyType.PowerLevel * count;
                    }
                }
                foreach (var item in config.currentLevel.OutsideEnemies)
                {
                    if (item.enemyType == enemy)
                    {
                        outsidePower += item.enemyType.PowerLevel * count;
                    }
                }
                foreach (var item in config.currentLevel.DaytimeEnemies)
                {
                    if (item.enemyType == enemy)
                    {
                        daytimePower += item.enemyType.PowerLevel * count;
                    }
                }
            }
            float maxInsidePower = 30f;
            float maxOutsidePower = config.currentLevel.maxOutsideEnemyPowerCount;
            float maxDaytimePower = config.currentLevel.maxDaytimeEnemyPowerCount;
            if (insidePower > maxInsidePower)
            {
                throw new Exception($"Inside power required ({insidePower}) exceeds max possible limit ({maxInsidePower}).");
            }
            if (outsidePower > maxOutsidePower)
            {
                throw new Exception($"Outside power required ({outsidePower}) exceeds max possible limit ({maxOutsidePower}).");
            }
            if (daytimePower > maxDaytimePower)
            {
                throw new Exception($"Daytime power required ({daytimePower}) exceeds max possible limit ({maxDaytimePower}).");
            }
        }
    }
}
