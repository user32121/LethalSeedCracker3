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
            if (result.config.currentLevel.spawnableMapObjects.Length == 0)
            {
                return;
            }
            System.Random random = new(StartOfRound.Instance.randomMapSeed + 587);
            RandomMapObject[] array = Object.FindObjectsOfType<RandomMapObject>();
            EntranceTeleport[] array2 = Object.FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None);
            List<Vector3> list = [];
            List<RandomMapObject> list2 = [];
            for (int i = 0; i < result.config.currentLevel.spawnableMapObjects.Length; i++)
            {
                list2.Clear();
                int num = (int)result.config.currentLevel.spawnableMapObjects[i].numberToSpawn.Evaluate((float)random.NextDouble());
                if (increasedMapHazardSpawnRateIndex == i)
                {
                    num = Mathf.Min(num * 2, 150);
                }
                if (num <= 0)
                {
                    continue;
                }
                for (int j = 0; j < array.Length; j++)
                {
                    if (array[j].spawnablePrefabs.Contains(result.config.currentLevel.spawnableMapObjects[i].prefabToSpawn))
                    {
                        list2.Add(array[j]);
                    }
                }
                if (list2.Count == 0)
                {
                    //Debug.Log("NO SPAWNERS WERE COMPATIBLE WITH THE SPAWNABLE MAP OBJECT: '" + result.config.currentLevel.spawnableMapObjects[i].prefabToSpawn.gameObject.name + "'");
                    continue;
                }
                list.Clear();
                for (int k = 0; k < num; k++)
                {
                    RandomMapObject randomMapObject = list2[random.Next(0, list2.Count)];
                    Vector3 position = randomMapObject.transform.position;
                    position = CrackingRoundManager.GetRandomNavMeshPositionInBoxPredictable(position, randomMapObject.spawnRange, random);
                    if (result.config.currentLevel.spawnableMapObjects[i].disallowSpawningNearEntrances)
                    {
                        for (int l = 0; l < array2.Length; l++)
                        {
                            if (!array2[l].isEntranceToBuilding)
                            {
                                Vector3.Distance(array2[l].entrancePoint.transform.position, position);
                                _ = 5.5f;
                            }
                        }
                    }
                    if (result.config.currentLevel.spawnableMapObjects[i].requireDistanceBetweenSpawns)
                    {
                        bool flag = false;
                        for (int m = 0; m < list.Count; m++)
                        {
                            if (Vector3.Distance(position, list[m]) < 5f)
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (flag)
                        {
                            continue;
                        }
                    }
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
            for (int n = 0; n < array.Length; n++)
            {
                Object.Destroy(array[n].gameObject);
            }
            result.traps = []; //TODO
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
            int numLockedDoors = 0;
            for (int num4 = 0; num4 < 100; num4++)
            {
                if (result.crm.LevelRandom.NextDouble() < (double)num2)
                {
                    float timeToLockPick = Mathf.Clamp(result.crm.LevelRandom.Next(2, 90), 2f, 32f);
                    _ = timeToLockPick;
                    numLockedDoors++;
                }
                num2 /= 1.55f;
            }
            //instantiate keys
            for (int i = 0; i < numLockedDoors; i++)
            {
                _ = result.crm.AnomalyRandom.Next();
                Vector3 randomNavMeshPositionInBoxPredictable = CrackingRoundManager.GetRandomNavMeshPositionInBoxPredictable(Vector3.zero, 8f, result.crm.AnomalyRandom);
                _ = randomNavMeshPositionInBoxPredictable;
            }
        }
    }
}
