using DunGen;
using LethalSeedCracker3.src.common;
using LethalSeedCracker3.src.config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LethalSeedCracker3.src.cracker
{
    internal class Result(int seed, Config config)
    {
        internal int seed = seed;
        internal Config config = config;
        //intermediate values
        internal CrackingRoundManager crm = new(seed);
        //enemies
        internal EnemyType? infestation;
        internal Dictionary<EnemyType, int> enemyCounts = [];
        //level
        internal bool? indoorFog;
        internal int? currentDungeonType;
        internal bool? meteorShower;
        internal float? meteorShowerAtTime;
        internal bool? blackout;
        internal Dictionary<string, int>? traps;
        //scrap
        internal int? numScrap;
        internal Item? singleItemDay;
        internal Dictionary<Item, int>? scrapCounts;
        //misc
        internal CompanyMood? currentCompanyMood;
        internal Dictionary<SelectableLevel, LevelWeatherType>? weathers;

        internal void Cleanup()
        {
            RuntimeDungeon dungeonGenerator = UnityEngine.Object.FindObjectOfType<RuntimeDungeon>(includeInactive: false);
            dungeonGenerator.Generator.Clear(true);
            RoundManager.Instance.UnloadSceneObjectsEarly();
            RoundManager.Instance.DespawnPropsAtEndOfRound();
        }
    }

    internal class FrozenResult(Result result)
    {
        internal readonly int seed = result.seed;
        internal readonly Config config = result.config;
        //enemies
        internal readonly EnemyType? infestation = result.infestation;
        internal readonly Dictionary<EnemyType, int> enemies = result.enemyCounts;
        //level
        internal readonly bool indoorFog = Util.NonNull(result.indoorFog, nameof(result.indoorFog));
        internal readonly int currentDungeonType = Util.NonNull(result.currentDungeonType, nameof(result.currentDungeonType));
        internal readonly bool meteorShower = Util.NonNull(result.meteorShower, nameof(result.meteorShower));
        internal readonly float meteorShowerAtTime = Util.NonNull(result.meteorShowerAtTime, nameof(result.meteorShowerAtTime));
        internal readonly bool blackout = Util.NonNull(result.blackout, nameof(result.blackout));
        internal readonly Dictionary<string, int> traps = Util.NonNull(result.traps, nameof(result.traps));
        //scrap
        internal readonly int numScrap = Util.NonNull(result.numScrap, nameof(result.numScrap));
        internal readonly Item? singleItemDay = result.singleItemDay;
        internal readonly Dictionary<Item, int> scrapCounts = Util.NonNull(result.scrapCounts, nameof(result.scrapCounts));
        //misc
        internal readonly CompanyMood currentCompanyMood = Util.NonNull(result.currentCompanyMood, nameof(result.currentCompanyMood));
        internal readonly Dictionary<SelectableLevel, LevelWeatherType> weathers = Util.NonNull(result.weathers, nameof(result.weathers));

        public string ToFormattedString(string majorSeparator, string minorSeparator)
        {
            string maj = majorSeparator;
            string min = minorSeparator;
            string enemyList = string.Join(min, [.. from item in enemies select $"{item.Key.name}: {item.Value}"]);
            string trapList = string.Join(min, [.. from item in traps select $"{item.Key}: {item.Value}"]);
            string scrapList = string.Join(min, [.. from item in scrapCounts select $"{item.Key.name}: {item.Value}"]);
            string dungeon = currentDungeonType switch
            {
                -1 => "none",
                0 => "factory",
                1 => "mansion",
                2 => "factory",
                3 => "factory",
                4 => "mineshaft",
                _ => throw new NotImplementedException($"dungeon type: {currentDungeonType}")
            };
            return $"seed: {seed}" +
                $"{maj}moon: {config.currentLevel.name}{min}eclipsed: {config.eclipsed}" +
                $"{maj}daystildeadline: {config.daysUntilDeadline}{min}dayssurvived: {config.daysPlayersSurvivedInARow}{min}players: {config.connectedPlayersAmount}{min}anniversary: {config.isAnniversary}" +
                $"{maj}dungeon: {dungeon}{min}indoorfog: {indoorFog}{min}blackout: {blackout}{min}meteorshower: {meteorShower}{min}meteorshowertime: {meteorShowerAtTime}{min}companymood: {currentCompanyMood.name}" +
                $"{maj}infestation: {infestation?.name}{min}enemies: [{enemyList}]" +
                $"{maj}numscrap: {numScrap}{min}singleitemday: {singleItemDay}{min}scrap: [{scrapList}]" +
                $"{maj}traps: [{trapList}]";
        }

        internal void Save(string fileName, string compressedFileName, bool append)
        {
            string folderPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "32121", "LethalSeedCracker");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string filePath = Path.Join(folderPath, fileName);
            LethalSeedCracker3.Logger.LogInfo($"writing seed result to: {filePath}");
            using StreamWriter file = new(File.Open(filePath, append ? FileMode.Append : FileMode.Create));
            file.WriteLine(ToFormattedString("\n  ", ", ") + "\n");

            filePath = Path.Join(folderPath, compressedFileName);
            LethalSeedCracker3.Logger.LogInfo($"writing seed to: {filePath}");
            using StreamWriter compressedFile = new(File.Open(filePath, append ? FileMode.Append : FileMode.Create));
            compressedFile.WriteLine("seeds " + seed);
        }
    }
}
