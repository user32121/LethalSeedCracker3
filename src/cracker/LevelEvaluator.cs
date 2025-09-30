using System.Collections.Generic;
using UnityEngine;

namespace LethalSeedCracker3.src.cracker
{
    internal class LevelEvaluator
    {
        private static readonly int increasedMapHazardSpawnRateIndex = -1;

        internal static void EvaluatePreDunGen(Result result)
        {
            if (result.config.verbose)
            {
                LethalSeedCracker3.Logger.LogInfo("level evaluate pre");
            }
            GenerateNewFloor(result);
        }

        internal static void EvaluatePostDunGen(Result result)
        {
            if (result.config.verbose)
            {
                LethalSeedCracker3.Logger.LogInfo("level evaluate post");
            }
            SpawnMapObjects(result);
            BeginDay(result);
        }

        internal static void EvaluatePostScrap(Result result)
        {
            SetLockedDoors(result);
            SetSteamValveTimes(result);
        }

        public static void GenerateNewFloor(Result result)
        {
            int dungeonType = -1;
            if (result.config.currentLevel.dungeonFlowTypes != null && result.config.currentLevel.dungeonFlowTypes.Length != 0)
            {
                List<int> list = [];
                for (int i = 0; i < result.config.currentLevel.dungeonFlowTypes.Length; i++)
                {
                    list.Add(result.config.currentLevel.dungeonFlowTypes[i].rarity);
                }
                int randomWeightedIndex = CrackingRoundManager.GetRandomWeightedIndex([.. list], result.crm.LevelRandom);
                dungeonType = result.config.currentLevel.dungeonFlowTypes[randomWeightedIndex].id;

                result.currentDungeonType = dungeonType;
            }
            else
            {
                result.currentDungeonType = 0;
            }
            _ = result.crm.LevelRandom.Next();
            float num2;
            if (dungeonType != -1)
            {
                num2 = result.config.currentLevel.factorySizeMultiplier / RoundManager.Instance.dungeonFlowTypes[dungeonType].MapTileSize * RoundManager.Instance.mapSizeMultiplier;
                num2 = (float)((double)Mathf.Round(num2 * 100f) / 100.0);
            }
            else
            {
                num2 = result.config.currentLevel.factorySizeMultiplier * RoundManager.Instance.mapSizeMultiplier;
            }
            _ = num2;
        }

        public static void SpawnMapObjects(Result result)
        {
            result.traps = [];
            if (result.config.currentLevel.spawnableMapObjects.Length == 0)
            {
                return;
            }
            System.Random random = new(StartOfRound.Instance.randomMapSeed + 587);
            List<Vector3> list = [];
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
                list.Clear();
                for (int k = 0; k < num; k++)
                {
                    _ = random.Next();
                    _ = CrackingRoundManager.GetRandomNavMeshPositionInBoxPredictable(Vector3.zero, 10, random);
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
                    result.traps[result.config.currentLevel.spawnableMapObjects[i].prefabToSpawn.name] = result.traps.GetValueOrDefault(result.config.currentLevel.spawnableMapObjects[i].prefabToSpawn.name, 0) + 1;
                }
            }
        }

        private static void BeginDay(Result result)
        {
            //meteors
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

        private static void SetLockedDoors(Result result)
        {
            float num2 = 1.1f;
            result.lockedDoors = 0;
            for (int i = 0; i < result.config.doorCount; i++)
            {
                if (result.crm.LevelRandom.NextDouble() < (double)num2)
                {
                    float timeToLockPick = Mathf.Clamp(result.crm.LevelRandom.Next(2, 90), 2f, 32f);
                    _ = timeToLockPick;
                    ++result.lockedDoors;
                }
                num2 /= 1.55f;
            }
            //instantiate keys
            for (int i = 0; i < result.lockedDoors; i++)
            {
                _ = result.crm.AnomalyRandom.Next();
                Vector3 randomNavMeshPositionInBoxPredictable = CrackingRoundManager.GetRandomNavMeshPositionInBoxPredictable(Vector3.zero, 8f, result.crm.AnomalyRandom);
                _ = randomNavMeshPositionInBoxPredictable;
            }
        }

        private static void SetSteamValveTimes(Result result)
        {
            result.burstValves = 0;
            System.Random random = new(result.seed + 513);
            for (int num = 0; num < result.config.valveCount; num++)
            {
                if (random.NextDouble() < 0.75)
                {
                    ++result.burstValves;
                    float valveBurstTime = Mathf.Clamp((float)random.NextDouble(), 0.2f, 1f);
                    _ = valveBurstTime * (float)random.NextDouble();
                    _ = Mathf.Clamp((float)random.NextDouble(), 0.6f, 0.98f);
                }
                else if (random.NextDouble() < 0.25)
                {
                    _ = Mathf.Clamp((float)random.NextDouble(), 0.3f, 0.9f);
                }
            }
        }
    }
}
