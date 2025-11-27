using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;

namespace LethalSeedCracker3.src.cracker
{
    internal class LevelEvaluator
    {
        private static readonly int increasedMapHazardSpawnRateIndex = -1;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static GameObject[] outsideAINodes;
        private static GameObject[] spawnDenialPoints;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

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
            SpawnOutsideHazards(result);
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
            System.Random random = new(result.seed + 587);
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

        private static void SpawnOutsideHazards(Result result)
        {
            if (result.config.verbose)
            {
                LethalSeedCracker3.Logger.LogInfo("spawn outside hazards");
                foreach (var item in Object.FindObjectsOfType<NavMeshSurface>())
                {
                    LethalSeedCracker3.Logger.LogInfo($"navmesh {item} {item.gameObject.scene.name}");
                }
            }
            result.outsideObjects = [];

            System.Random random = new(result.seed + 2);
            outsideAINodes = [.. from x in GameObject.FindGameObjectsWithTag("OutsideAINode")
                              orderby Vector3.Distance(x.transform.position, Vector3.zero)
                              select x];
            int num = 0;
            if (result.config.rainy)
            {
                num = random.Next(5, 15);
                if (random.Next(0, 100) < 7)
                {
                    num = random.Next(5, 30);
                }
                for (int num2 = 0; num2 < num; num2++)
                {
                    Vector3 position = outsideAINodes[random.Next(0, outsideAINodes.Length)].transform.position;
                    Vector3 position2 = CrackingRoundManager.GetRandomNavMeshPositionInBoxPredictable(position, 30f, random) + Vector3.up;
                    result.outsideObjects[RoundManager.Instance.quicksandPrefab.name] = result.outsideObjects.GetValueOrDefault(RoundManager.Instance.quicksandPrefab.name, 0) + 1;
                }
            }
            int num3 = 0;
            List<Vector3> list = [];
            spawnDenialPoints = GameObject.FindGameObjectsWithTag("SpawnDenialPoint");
            if (result.config.currentLevel.spawnableOutsideObjects != null)
            {
                for (int num4 = 0; num4 < result.config.currentLevel.spawnableOutsideObjects.Length; num4++)
                {
                    double num5 = random.NextDouble();
                    num = (int)result.config.currentLevel.spawnableOutsideObjects[num4].randomAmount.Evaluate((float)num5);
                    if (random.Next(0, 100) < 20f)
                    {
                        num *= 2;
                    }
                    for (int num6 = 0; num6 < num; num6++)
                    {
                        int num7 = random.Next(0, outsideAINodes.Length);
                        Vector3 position2 = CrackingRoundManager.GetRandomNavMeshPositionInBoxPredictable(outsideAINodes[num7].transform.position, 30f, random);
                        if (result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.spawnableFloorTags != null)
                        {
                            bool flag = false;
                            if (Physics.Raycast(position2 + Vector3.up, Vector3.down, out var hitInfo, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                            {
                                for (int num8 = 0; num8 < result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.spawnableFloorTags.Length; num8++)
                                {
                                    if (hitInfo.collider.transform.CompareTag(result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.spawnableFloorTags[num8]))
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                            if (!flag)
                            {
                                continue;
                            }
                        }
                        if (result.config.verbose)
                        {
                            LethalSeedCracker3.Logger.LogInfo($"position edge check ({position2}, {result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.objectWidth})");
                        }
                        position2 = CrackingRoundManager.PositionEdgeCheck(position2, result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.objectWidth);
                        if (result.config.verbose)
                        {
                            LethalSeedCracker3.Logger.LogInfo($"=> {position2}");
                        }
                        if (position2 == Vector3.zero)
                        {
                            continue;
                        }
                        if (result.config.verbose)
                        {
                            LethalSeedCracker3.Logger.LogInfo("ship spawn point check");
                        }
                        bool flag2 = false;
                        for (int num9 = 0; num9 < RoundManager.Instance.shipSpawnPathPoints.Length; num9++)
                        {
                            if (Vector3.Distance(RoundManager.Instance.shipSpawnPathPoints[num9].transform.position, position2) < result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.objectWidth + 6f)
                            {
                                flag2 = true;
                                break;
                            }
                        }
                        if (flag2)
                        {
                            continue;
                        }
                        if (result.config.verbose)
                        {
                            LethalSeedCracker3.Logger.LogInfo("spawn denial point check");
                        }
                        for (int num10 = 0; num10 < spawnDenialPoints.Length; num10++)
                        {
                            if (Vector3.Distance(spawnDenialPoints[num10].transform.position, position2) < result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.objectWidth + 6f)
                            {
                                flag2 = true;
                                break;
                            }
                        }
                        if (flag2)
                        {
                            continue;
                        }
                        if (result.config.verbose)
                        {
                            LethalSeedCracker3.Logger.LogInfo("ship landing node check");
                        }
                        if (Vector3.Distance(GameObject.FindGameObjectWithTag("ItemShipLandingNode").transform.position, position2) < result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.objectWidth + 4f)
                        {
                            flag2 = true;
                            break;
                        }
                        if (flag2)
                        {
                            continue;
                        }
                        if (result.config.verbose)
                        {
                            LethalSeedCracker3.Logger.LogInfo("outside hazards check");
                        }
                        if (result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.objectWidth > 4)
                        {
                            flag2 = false;
                            for (int num11 = 0; num11 < list.Count; num11++)
                            {
                                if (Vector3.Distance(position2, list[num11]) < result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.objectWidth)
                                {
                                    flag2 = true;
                                    break;
                                }
                            }
                            if (flag2)
                            {
                                continue;
                            }
                        }
                        list.Add(position2);
                        result.outsideObjects[result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.prefabToSpawn.name] = result.outsideObjects.GetValueOrDefault(result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.prefabToSpawn.name, 0) + 1;
                        num3++;
                        if (result.config.currentLevel.spawnableOutsideObjects[num4].spawnableObject.spawnFacingAwayFromWall)
                        {
                            _ = new Vector3(0f, CrackingRoundManager.YRotationThatFacesTheFarthestFromPosition(position2 + Vector3.up * 0.2f), 0f);
                        }
                        else
                        {
                            int num12 = random.Next(0, 360);
                            _ = new Vector3(0, num12, 0);
                        }
                    }
                }
            }
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
