using LethalSeedCracker3.src.config;
using System.Collections.Generic;
using UnityEngine;

namespace LethalSeedCracker3.src.cracker
{
    internal static class EnemyEvaluator
    {
        private static int currentHour;
        private static bool cannotSpawnMoreInsideEnemies;
        private static int minEnemiesToSpawn;
        private static int minOutsideEnemiesToSpawn;
        private static float currentDaytimeEnemyPower;
        private static readonly List<int> SpawnProbabilities = [];
        private static bool firstTimeSpawningDaytimeEnemies;
        private static int enemyRushIndex;
        private static float currentMaxInsidePower;
        private static bool firstTimeSpawningEnemies;
        private static bool firstTimeSpawningOutsideEnemies;
        private static float currentMaxOutsidePower;
        private static float currentOutsideEnemyPower;
        private static float currentEnemyPower;

        internal static void Evaluate(Result result)
        {
            if (result.config.verbose)
            {
                LethalSeedCracker3.Logger.LogInfo("enemy evaluate");
            }
            ResetEnemySpawningVariables(result.config);
            RefreshEnemiesList(result);
            while (currentHour < TimeOfDay.Instance.numberOfHours)
            {
                AdvanceHourAndSpawnNewBatchOfEnemies(result);
            }
        }

        private static void RefreshEnemiesList(Result result)
        {
            firstTimeSpawningEnemies = true;
            firstTimeSpawningOutsideEnemies = true;
            firstTimeSpawningDaytimeEnemies = true;
            enemyRushIndex = -1;
            currentMaxInsidePower = result.config.currentLevel.maxEnemyPowerCount;
            bool num = result.config.isAnniversary;
            System.Random random2 = new(result.seed + 5781);
            if ((!num && random2.Next(0, 210) < 4) || random2.Next(0, 1000) < 7)
            {
                result.indoorFog = random2.Next(0, 100) < 20;
                if (random2.Next(0, 100) < 25)
                {
                    for (int i = 0; i < result.config.currentLevel.Enemies.Count; i++)
                    {
                        if (result.config.currentLevel.Enemies[i].enemyType.enemyName == "Nutcracker")
                        {
                            enemyRushIndex = i;
                            currentMaxInsidePower = 20f;
                            result.infestation = result.config.currentLevel.Enemies[i].enemyType;
                            break;
                        }
                    }
                    if (enemyRushIndex == -1)
                    {
                        for (int j = 0; j < result.config.currentLevel.Enemies.Count; j++)
                        {
                            if (result.config.currentLevel.Enemies[j].enemyType.enemyName == "Hoarding bug")
                            {
                                enemyRushIndex = j;
                                currentMaxInsidePower = 30f;
                                result.infestation = result.config.currentLevel.Enemies[j].enemyType;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    for (int k = 0; k < result.config.currentLevel.Enemies.Count; k++)
                    {
                        if (result.config.currentLevel.Enemies[k].enemyType.enemyName == "Hoarding bug")
                        {
                            enemyRushIndex = k;
                            currentMaxInsidePower = 30f;
                            result.infestation = result.config.currentLevel.Enemies[k].enemyType;
                            break;
                        }
                    }
                }
            }
            else
            {
                result.indoorFog = random2.Next(0, 150) < 3;
            }
            currentMaxOutsidePower = result.config.currentLevel.maxOutsideEnemyPowerCount;
        }

        private static void ResetEnemySpawningVariables(Config config)
        {
            currentDaytimeEnemyPower = 0;
            currentOutsideEnemyPower = 0;
            currentEnemyPower = 0;
            currentHour = 0;
            cannotSpawnMoreInsideEnemies = false;
            minEnemiesToSpawn = 0;
            minOutsideEnemiesToSpawn = 0;
            if (config.eclipsed)
            {
                foreach (var item in config.currentLevel.randomWeathers)
                {
                    if (item.weatherType == LevelWeatherType.Eclipsed)
                    {
                        minEnemiesToSpawn = item.weatherVariable;
                        minOutsideEnemiesToSpawn = item.weatherVariable;
                    }
                }
            }
            for (int i = 0; i < config.currentLevel.OutsideEnemies.Count; i++)
            {
                config.currentLevel.OutsideEnemies[i].enemyType.nestsSpawned = 0;
            }
        }

        private static void AdvanceHourAndSpawnNewBatchOfEnemies(Result result)
        {
            currentHour += RoundManager.Instance.hourTimeBetweenEnemySpawnBatches;
            SpawnDaytimeEnemiesOutside(result);
            SpawnEnemiesOutside(result);
            if (!cannotSpawnMoreInsideEnemies)
            {
                if (result.config.connectedPlayersAmount + 1 > 0 && result.config.daysUntilDeadline <= 2 && (result.config.daysPlayersSurvivedInARow >= 5) && minEnemiesToSpawn == 0)
                {
                    minEnemiesToSpawn = 1;
                }
                PlotOutEnemiesForNextHour(result);
            }
        }

        public static void SpawnDaytimeEnemiesOutside(Result result)
        {
            if (result.config.currentLevel.DaytimeEnemies == null || result.config.currentLevel.DaytimeEnemies.Count <= 0 || currentDaytimeEnemyPower > result.config.currentLevel.maxDaytimeEnemyPowerCount)
            {
                return;
            }
            float num = TimeOfDay.Instance.lengthOfHours * currentHour;
            float num2 = result.config.currentLevel.daytimeEnemySpawnChanceThroughDay.Evaluate(num / TimeOfDay.Instance.totalTime);
            int num3 = Mathf.Clamp(result.crm.AnomalyRandom.Next((int)(num2 - result.config.currentLevel.daytimeEnemiesProbabilityRange), (int)(num2 + result.config.currentLevel.daytimeEnemiesProbabilityRange)), 0, 20);
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
            for (int i = 0; i < num3; i++)
            {
                if (!SpawnRandomDaytimeEnemy(spawnPoints, num, result))
                {
                    break;
                }
            }
        }

        private static bool SpawnRandomDaytimeEnemy(GameObject[] spawnPoints, float timeUpToCurrentHour, Result result)
        {
            SpawnProbabilities.Clear();
            int num = 0;
            for (int i = 0; i < result.config.currentLevel.DaytimeEnemies.Count; i++)
            {
                EnemyType enemyType = result.config.currentLevel.DaytimeEnemies[i].enemyType;
                if (firstTimeSpawningDaytimeEnemies)
                {
                    enemyType.numberSpawned = 0;
                }
                if (enemyType.PowerLevel > result.config.currentLevel.maxDaytimeEnemyPowerCount - currentDaytimeEnemyPower || enemyType.numberSpawned >= result.config.currentLevel.DaytimeEnemies[i].enemyType.MaxCount || enemyType.normalizedTimeInDayToLeave < TimeOfDay.Instance.normalizedTimeOfDay || enemyType.spawningDisabled)
                {
                    SpawnProbabilities.Add(0);
                    continue;
                }
                int num2 = (int)(result.config.currentLevel.DaytimeEnemies[i].rarity * enemyType.probabilityCurve.Evaluate(timeUpToCurrentHour / TimeOfDay.Instance.totalTime));
                SpawnProbabilities.Add(num2);
                num += num2;
            }
            firstTimeSpawningDaytimeEnemies = false;
            if (num <= 0)
            {
                _ = currentDaytimeEnemyPower;
                _ = (float)result.config.currentLevel.maxDaytimeEnemyPowerCount;
                return false;
            }
            int randomWeightedIndex = CrackingRoundManager.GetRandomWeightedIndex([.. SpawnProbabilities], result.crm.EnemySpawnRandom);
            EnemyType enemyType2 = result.config.currentLevel.DaytimeEnemies[randomWeightedIndex].enemyType;
            bool res = false;
            float num3 = Mathf.Max(enemyType2.spawnInGroupsOf, 1);
            for (int j = 0; j < num3; j++)
            {
                if (enemyType2.PowerLevel > result.config.currentLevel.maxDaytimeEnemyPowerCount - currentDaytimeEnemyPower)
                {
                    break;
                }
                currentDaytimeEnemyPower += result.config.currentLevel.DaytimeEnemies[randomWeightedIndex].enemyType.PowerLevel;
                Vector3 position = spawnPoints[result.crm.AnomalyRandom.Next(0, spawnPoints.Length)].transform.position;
                position = CrackingRoundManager.GetRandomNavMeshPositionInBoxPredictable(position, 10f, result.crm.EnemySpawnRandom, CrackingRoundManager.GetLayermaskForEnemySizeLimit(enemyType2));
                _ = CrackingRoundManager.PositionWithDenialPointsChecked(result, position, spawnPoints, enemyType2);
                enemyType2.numberSpawned++;
                res = true;
                int count = result.enemyCounts.GetValueOrDefault(enemyType2, 0) + 1;
                result.enemyCounts[enemyType2] = count;
            }
            return res;
        }

        public static void SpawnEnemiesOutside(Result result)
        {
            if (currentOutsideEnemyPower > currentMaxOutsidePower)
            {
                return;
            }
            float num = TimeOfDay.Instance.lengthOfHours * currentHour;
            float num2 = (int)(result.config.currentLevel.outsideEnemySpawnChanceThroughDay.Evaluate(num / TimeOfDay.Instance.totalTime) * 100f) / 100f;
            float num3 = num2 + Mathf.Abs(TimeOfDay.Instance.daysUntilDeadline - 3) / 1.6f;
            int num4 = Mathf.Clamp(result.crm.OutsideEnemySpawnRandom.Next((int)(num3 - 3f), (int)(num2 + 3f)), minOutsideEnemiesToSpawn, 20);
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
            for (int i = 0; i < num4; i++)
            {
                if (!SpawnRandomOutsideEnemy(spawnPoints, num, result))
                {
                    break;
                }
            }
        }

        private static bool SpawnRandomOutsideEnemy(GameObject[] spawnPoints, float timeUpToCurrentHour, Result result)
        {
            SpawnProbabilities.Clear();
            int num = 0;
            for (int i = 0; i < result.config.currentLevel.OutsideEnemies.Count; i++)
            {
                EnemyType enemyType = result.config.currentLevel.OutsideEnemies[i].enemyType;
                if (firstTimeSpawningOutsideEnemies)
                {
                    enemyType.numberSpawned = 0;
                }
                if (enemyType.PowerLevel > currentMaxOutsidePower - currentOutsideEnemyPower || enemyType.numberSpawned >= enemyType.MaxCount || enemyType.spawningDisabled)
                {
                    SpawnProbabilities.Add(0);
                    continue;
                }
                int num3 = (!enemyType.useNumberSpawnedFalloff) ? ((int)(result.config.currentLevel.OutsideEnemies[i].rarity * enemyType.probabilityCurve.Evaluate(timeUpToCurrentHour / TimeOfDay.Instance.totalTime))) : ((int)(result.config.currentLevel.OutsideEnemies[i].rarity * (enemyType.probabilityCurve.Evaluate(timeUpToCurrentHour / TimeOfDay.Instance.totalTime) * enemyType.numberSpawnedFalloff.Evaluate(enemyType.numberSpawned / 10f))));
                SpawnProbabilities.Add(num3);
                num += num3;
            }
            firstTimeSpawningOutsideEnemies = false;
            if (num <= 0)
            {
                _ = currentOutsideEnemyPower;
                _ = currentMaxOutsidePower;
                return false;
            }
            bool res = false;
            int randomWeightedIndex = CrackingRoundManager.GetRandomWeightedIndex([.. SpawnProbabilities], result.crm.OutsideEnemySpawnRandom);
            EnemyType enemyType2 = result.config.currentLevel.OutsideEnemies[randomWeightedIndex].enemyType;
            if (enemyType2.requireNestObjectsToSpawn)
            {
                bool flag = false;
                EnemyAINestSpawnObject[] array = Object.FindObjectsByType<EnemyAINestSpawnObject>(FindObjectsSortMode.None);
                for (int j = 0; j < array.Length; j++)
                {
                    if (array[j].enemyType == enemyType2)
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    return false;
                }
            }
            float num4 = Mathf.Max(enemyType2.spawnInGroupsOf, 1);
            for (int k = 0; k < num4; k++)
            {
                if (enemyType2.PowerLevel > currentMaxOutsidePower - currentOutsideEnemyPower)
                {
                    break;
                }
                currentOutsideEnemyPower += result.config.currentLevel.OutsideEnemies[randomWeightedIndex].enemyType.PowerLevel;
                Vector3 position = spawnPoints[result.crm.AnomalyRandom.Next(0, spawnPoints.Length)].transform.position;
                position = CrackingRoundManager.GetRandomNavMeshPositionInBoxPredictable(position, 10f, result.crm.AnomalyRandom, CrackingRoundManager.GetLayermaskForEnemySizeLimit(enemyType2));
                _ = CrackingRoundManager.PositionWithDenialPointsChecked(result, position, spawnPoints, enemyType2);
                enemyType2.numberSpawned++;
                res = true;
                int count = result.enemyCounts.GetValueOrDefault(enemyType2, 0) + 1;
                result.enemyCounts[enemyType2] = count;
            }
            return res;
        }

        public static void PlotOutEnemiesForNextHour(Result result)
        {
            float currentDayTime = currentHour * TimeOfDay.Instance.lengthOfHours;
            float num = result.config.currentLevel.enemySpawnChanceThroughoutDay.Evaluate(currentDayTime / TimeOfDay.Instance.totalTime);
            if (StartOfRound.Instance.isChallengeFile)
            {
                num += 1f;
            }
            float num2 = num + Mathf.Abs(TimeOfDay.Instance.daysUntilDeadline - 3) / 1.6f;
            int num3 = Mathf.Clamp(result.crm.AnomalyRandom.Next((int)(num2 - result.config.currentLevel.spawnProbabilityRange), (int)(num + result.config.currentLevel.spawnProbabilityRange)), minEnemiesToSpawn, 20);
            if (enemyRushIndex != -1)
            {
                num3 += 2;
            }
            //num3 = Mathf.Clamp(num3, 0, list.Count);
            if (currentEnemyPower >= currentMaxInsidePower)
            {
                cannotSpawnMoreInsideEnemies = true;
                return;
            }
            float num4 = TimeOfDay.Instance.lengthOfHours * currentHour;
            for (int j = 0; j < num3; j++)
            {
                result.crm.AnomalyRandom.Next((int)(10f + num4), (int)(TimeOfDay.Instance.lengthOfHours * RoundManager.Instance.hourTimeBetweenEnemySpawnBatches + num4));
                result.crm.AnomalyRandom.Next();
                if (!AssignRandomEnemyToVent(result))
                {
                    break;
                }
            }
        }

        private static bool AssignRandomEnemyToVent(Result result)
        {
            float currentDayTime = currentHour * TimeOfDay.Instance.lengthOfHours;
            float normalizedTimeOfDay = currentDayTime / TimeOfDay.Instance.totalTime;
            SpawnProbabilities.Clear();
            int num = 0;
            for (int i = 0; i < result.config.currentLevel.Enemies.Count; i++)
            {
                EnemyType enemyType = result.config.currentLevel.Enemies[i].enemyType;
                if (firstTimeSpawningEnemies)
                {
                    enemyType.numberSpawned = 0;
                }
                if (EnemyCannotBeSpawned(i, result.config))
                {
                    SpawnProbabilities.Add(0);
                    continue;
                }
                int num2 = (enemyRushIndex != -1) ? ((enemyRushIndex != i) ? 1 : 100) : (false ? 100 : ((!enemyType.useNumberSpawnedFalloff) ? ((int)(result.config.currentLevel.Enemies[i].rarity * enemyType.probabilityCurve.Evaluate(normalizedTimeOfDay))) : ((int)(result.config.currentLevel.Enemies[i].rarity * (enemyType.probabilityCurve.Evaluate(normalizedTimeOfDay) * enemyType.numberSpawnedFalloff.Evaluate(enemyType.numberSpawned / 10f))))));
                if (enemyType.increasedChanceInterior != -1 && result.currentDungeonType == enemyType.increasedChanceInterior)
                {
                    num2 = (int)Mathf.Min(num2 * 1.7f, 100f);
                }
                SpawnProbabilities.Add(num2);
                num += num2;
            }
            firstTimeSpawningEnemies = false;
            if (num <= 0)
            {
                if (currentEnemyPower >= currentMaxInsidePower)
                {
                    cannotSpawnMoreInsideEnemies = true;
                }
                return false;
            }
            int randomWeightedIndex = CrackingRoundManager.GetRandomWeightedIndex([.. SpawnProbabilities], result.crm.EnemySpawnRandom);
            currentEnemyPower += result.config.currentLevel.Enemies[randomWeightedIndex].enemyType.PowerLevel;
            //vent.enemyType = config.currentLevel.Enemies[randomWeightedIndex].enemyType;
            //vent.enemyTypeIndex = randomWeightedIndex;
            //vent.occupied = true;
            //vent.spawnTime = spawnTime;
            //if (timeScript.hour - currentHour > 0)
            //{
            //    CrackingRoundManager.Log("RoundManager is catching up to current time! Not syncing vent SFX with clients since enemy will spawn from vent almost immediately.");
            //}
            //else
            //{
            //    vent.SyncVentSpawnTimeClientRpc((int)spawnTime, randomWeightedIndex);
            //}
            result.config.currentLevel.Enemies[randomWeightedIndex].enemyType.numberSpawned++;
            EnemyType enemyType2 = result.config.currentLevel.Enemies[randomWeightedIndex].enemyType;
            int count = result.enemyCounts.GetValueOrDefault(enemyType2, 0) + 1;
            result.enemyCounts[enemyType2] = count;
            return true;
        }

        private static bool EnemyCannotBeSpawned(int enemyIndex, Config config)
        {
            if (!config.currentLevel.Enemies[enemyIndex].enemyType.spawningDisabled)
            {
                if (!(config.currentLevel.Enemies[enemyIndex].enemyType.PowerLevel > currentMaxInsidePower - currentEnemyPower))
                {
                    return config.currentLevel.Enemies[enemyIndex].enemyType.numberSpawned >= config.currentLevel.Enemies[enemyIndex].enemyType.MaxCount;
                }
                return true;
            }
            return true;
        }
    }
}
