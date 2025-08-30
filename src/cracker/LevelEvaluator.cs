using DunGen;
using LethalSeedCracker3.Patches;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalSeedCracker3.src.cracker
{
    internal class LevelEvaluator
    {
        private static readonly int increasedMapHazardSpawnRateIndex = -1;

        private static bool dungeonCompletedGenerating = false;

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
            RoundManager.Instance.FinishGeneratingLevel();
            RoundManager.Instance.SpawnSyncedProps();
            SpawnMapObjects(result);
            BeginDay(result);
        }

        internal static void EvaluatePostScrap(Result result)
        {
            Vector3 mainEntrancePosition = RoundManager.FindMainEntrancePosition();
            SetLockedDoors(mainEntrancePosition, result);
        }

        public static void GenerateNewFloor(Result result)
        {
            dungeonCompletedGenerating = false;

            RuntimeDungeon dungeonGenerator = Object.FindObjectOfType<RuntimeDungeon>(includeInactive: false);
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

                //dungeon gen
                dungeonGenerator.Generator.DungeonFlow = RoundManager.Instance.dungeonFlowTypes[dungeonType].dungeonFlow;
                result.currentDungeonType = dungeonType;
            }
            else
            {
                result.currentDungeonType = 0;
            }
            dungeonGenerator.Generator.ShouldRandomizeSeed = false;
            dungeonGenerator.Generator.Seed = result.crm.LevelRandom.Next();
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
            dungeonGenerator.Generator.LengthMultiplier = num2;
            dungeonGenerator.Generator.OnGenerationStatusChanged += Generator_OnGenerationStatusChanged;
            Cracker.curState = Cracker.STATE.CRACKING_GEN_DUNGEN;
            dungeonGenerator.Generate();
            if (dungeonGenerator.Generator.Status == GenerationStatus.Complete)
            {
                Cracker.curState = Cracker.STATE.CRACKING_POST_DUNGEN;
            }
        }

        private static void Generator_OnGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
        {
            if (status == GenerationStatus.Complete && !dungeonCompletedGenerating)
            {
                dungeonCompletedGenerating = true;
                Cracker.curState = Cracker.STATE.CRACKING_POST_DUNGEN;
            }
            generator.OnGenerationStatusChanged -= Generator_OnGenerationStatusChanged;
        }

        public static void SpawnMapObjects(Result result)
        {
            if (result.config.currentLevel.spawnableMapObjects.Length == 0)
            {
                return;
            }
            System.Random random = new(StartOfRound.Instance.randomMapSeed + 587);
            GameObject mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer");
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
                    Debug.Log("NO SPAWNERS WERE COMPATIBLE WITH THE SPAWNABLE MAP OBJECT: '" + result.config.currentLevel.spawnableMapObjects[i].prefabToSpawn.gameObject.name + "'");
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
                    //GameObject gameObject = Object.Instantiate(result.config.currentLevel.spawnableMapObjects[i].prefabToSpawn, position, Quaternion.identity, mapPropsContainer.transform);
                    if (result.config.currentLevel.spawnableMapObjects[i].spawnFacingAwayFromWall)
                    {
                        //gameObject.transform.eulerAngles = new Vector3(0f, CrackingRoundManager.YRotationThatFacesTheFarthestFromPosition(position + Vector3.up * 0.2f), 0f);
                    }
                    else if (result.config.currentLevel.spawnableMapObjects[i].spawnFacingWall)
                    {
                        //gameObject.transform.eulerAngles = new Vector3(0f, CrackingRoundManager.YRotationThatFacesTheNearestFromPosition(position + Vector3.up * 0.2f), 0f);
                    }
                    else
                    {
                        //gameObject.transform.eulerAngles = new Vector3(gameObject.transform.eulerAngles.x, random.Next(0, 360), gameObject.transform.eulerAngles.z);
                        _ = random.Next(0, 360);
                    }
                    //if (result.config.currentLevel.spawnableMapObjects[i].spawnWithBackToWall && Physics.Raycast(gameObject.transform.position, -gameObject.transform.forward, out var hitInfo, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                    //{
                    //    gameObject.transform.position = hitInfo.point;
                    //    if (result.config.currentLevel.spawnableMapObjects[i].spawnWithBackFlushAgainstWall)
                    //    {
                    //        gameObject.transform.forward = hitInfo.normal;
                    //        gameObject.transform.eulerAngles = new Vector3(0f, gameObject.transform.eulerAngles.y, 0f);
                    //    }
                    //}
                    //gameObject.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
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

        private static void SetLockedDoors(Vector3 mainEntrancePosition, Result result)
        {
            if (mainEntrancePosition == Vector3.zero)
            {
                Debug.Log("Main entrance teleport was not spawned on local client within 12 seconds. Locking doors based on origin instead.");
            }
            List<DoorLock> list = Object.FindObjectsOfType<DoorLock>().ToList();
            for (int num = list.Count - 1; num >= 0; num--)
            {
                //not accessible by visual studio for some reason; workaround is to use reflection
                bool canBeLocked = (bool)typeof(DoorLock).GetField("canBeLocked").GetValue(list[num]);
                if (list[num].transform.position.y > -160f || !canBeLocked)
                {
                    list.RemoveAt(num);
                }
            }
            list = list.OrderByDescending((DoorLock x) => (mainEntrancePosition - x.transform.position).sqrMagnitude).ToList();
            float num2 = 1.1f;
            int num3 = 0;
            for (int num4 = 0; num4 < list.Count; num4++)
            {
                if (result.crm.LevelRandom.NextDouble() < (double)num2)
                {
                    float timeToLockPick = Mathf.Clamp(result.crm.LevelRandom.Next(2, 90), 2f, 32f);
                    //list[num4].LockDoor(timeToLockPick);
                    num3++;
                }
                num2 /= 1.55f;
            }
            GameObject[] array;
            int maxValue;
            GameObject[] insideAINodes = GameObject.FindGameObjectsWithTag("AINode");
            if (result.currentDungeonType != 4)
            {
                array = insideAINodes;
                maxValue = insideAINodes.Length;
            }
            else
            {
                array = insideAINodes.OrderBy((GameObject x) => Vector3.Distance(x.transform.position, mainEntrancePosition)).ToArray();
                maxValue = array.Length / 3;
            }
            for (int num5 = 0; num5 < num3; num5++)
            {
                int num6 = result.crm.AnomalyRandom.Next(0, maxValue);
                Vector3 randomNavMeshPositionInBoxPredictable = CrackingRoundManager.GetRandomNavMeshPositionInBoxPredictable(array[num6].transform.position, 8f, result.crm.AnomalyRandom);
                //Object.Instantiate(keyPrefab, randomNavMeshPositionInBoxPredictable, Quaternion.identity, spawnedScrapContainer).GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
