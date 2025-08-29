using DunGen;
using LethalSeedCracker3.Patches;
using System.Collections.Generic;
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
            //RoundManager.Instance.SpawnSyncedProps();
            SpawnMapObjects(result);
            BeginDay(result);
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
    }
}
