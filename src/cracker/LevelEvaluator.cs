using System.Collections.Generic;
using UnityEngine;

namespace LethalSeedCracker3.src.cracker
{
    internal class LevelEvaluator
    {
        private static readonly int increasedMapHazardSpawnRateIndex = -1;

        internal static void Evaulate(Result result)
        {
            GenerateNewFloor(result);
            SpawnMapObjects(result);
        }

        public static void GenerateNewFloor(Result result)
        {
            result.currentDungeonType = -1;
            if (result.config.currentLevel.dungeonFlowTypes != null && result.config.currentLevel.dungeonFlowTypes.Length != 0)
            {
                List<int> list = [];
                for (int i = 0; i < result.config.currentLevel.dungeonFlowTypes.Length; i++)
                {
                    list.Add(result.config.currentLevel.dungeonFlowTypes[i].rarity);
                }
                int randomWeightedIndex = CrackingRoundManager.GetRandomWeightedIndex([.. list], result.crm.LevelRandom);
                result.currentDungeonType = result.config.currentLevel.dungeonFlowTypes[randomWeightedIndex].id;

                //TODO dungeon gen
                //dungeonGenerator.Generator.DungeonFlow = dungeonFlowTypes[dungeonType].dungeonFlow;
                //currentDungeonType = dungeonType;
                //if (config.currentLevel.dungeonFlowTypes[randomWeightedIndex].overrideLevelAmbience != null)
                //{
                //    SoundManager.Instance.currentLevelAmbience = config.currentLevel.dungeonFlowTypes[randomWeightedIndex].overrideLevelAmbience;
                //}
                //else if (config.currentLevel.levelAmbienceClips != null)
                //{
                //    SoundManager.Instance.currentLevelAmbience = config.currentLevel.levelAmbienceClips;
                //}
            }
            else
            {
                //if (config.currentLevel.levelAmbienceClips != null)
                //{
                //    SoundManager.Instance.currentLevelAmbience = config.currentLevel.levelAmbienceClips;
                //}
                //currentDungeonType = 0;
            }
            //dungeonGenerator.Generator.ShouldRandomizeSeed = false;
            //dungeonGenerator.Generator.Seed
            _ = result.crm.LevelRandom.Next();
            //float num2;
            //if (dungeonType != -1)
            //{
            //    num2 = config.currentLevel.factorySizeMultiplier / dungeonFlowTypes[dungeonType].MapTileSize * mapSizeMultiplier;
            //    num2 = (float)((double)Mathf.Round(num2 * 100f) / 100.0);
            //}
            //else
            //{
            //    num2 = config.currentLevel.factorySizeMultiplier * mapSizeMultiplier;
            //}
            //dungeonGenerator.Generator.LengthMultiplier = num2;
            //dungeonGenerator.Generate();

            System.Random random = new(result.seed + 28);
            int num = 7;
            bool meteorShower;
            result.meteorShower = meteorShower = random.Next(0, 1000) < num;
            result.meteorShowerAtTime = -1f;
            if (meteorShower)
            {
                result.meteorShowerAtTime = random.Next(5, 80) / 100f;
            }

            result.blackout = new System.Random(result.seed + 3).NextDouble() < 0.07999999821186066;

            result.currentCompanyMood = TimeOfDay.Instance.CommonCompanyMoods[new System.Random(result.seed + 164).Next(0, TimeOfDay.Instance.CommonCompanyMoods.Length)];
        }

        public static void SpawnMapObjects(Result result)
        {
            if (result.config.currentLevel.spawnableMapObjects.Length == 0)
            {
                result.traps = [];
                return;
            }
            Dictionary<string, int> ret = [];
            System.Random random = new(result.seed + 587);
            for (int i = 0; i < result.config.currentLevel.spawnableMapObjects.Length; i++)
            {
                int num = (int)result.config.currentLevel.spawnableMapObjects[i].numberToSpawn.Evaluate((float)random.NextDouble());
                if (increasedMapHazardSpawnRateIndex == i)
                {
                    num = Mathf.Min(num * 2, 150);
                }
                if (num <= 0)
                {
                    continue;
                }
                for (int k = 0; k < num; k++)
                {
                    _ = random.Next();
                    EnemyEvaluator.GetRandomNavMeshPositionInBoxPredictable(random);
                    int count = ret.GetValueOrDefault(result.config.currentLevel.spawnableMapObjects[i].prefabToSpawn.name);
                    ret[result.config.currentLevel.spawnableMapObjects[i].prefabToSpawn.name] = count + 1;
                    if (result.config.currentLevel.spawnableMapObjects[i].spawnFacingAwayFromWall)
                    {
                    }
                    else if (result.config.currentLevel.spawnableMapObjects[i].spawnFacingWall)
                    {
                    }
                    else
                    {
                        _ = random.Next(0, 360);
                    }
                }
            }
            result.traps = ret;
        }
    }
}
