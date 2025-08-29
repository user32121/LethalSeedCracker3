using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace LethalSeedCracker3.src.cracker
{
    internal class ScrapEvaluator
    {
        internal static void Evaluate(Result result)
        {
            if (result.config.verbose)
            {
                LethalSeedCracker3.Logger.LogInfo("scrap evaluate");
            }
            SpawnScrapInLevel(result);
        }

        public static void SpawnScrapInLevel(Result result)
        {
            result.numScrap = (int)(result.crm.AnomalyRandom.Next(result.config.currentLevel.minScrap, result.config.currentLevel.maxScrap) * RoundManager.Instance.scrapAmountMultiplier);
            if (result.currentDungeonType == 4)
            {
                result.numScrap += 6;
            }
            if (StartOfRound.Instance.isChallengeFile)
            {
                int num2 = result.crm.AnomalyRandom.Next(10, 30);
                result.numScrap += num2;
            }
            int singleItemIndex = -1;
            if (result.crm.AnomalyRandom.Next(0, 500) <= 20)
            {
                singleItemIndex = result.crm.AnomalyRandom.Next(0, result.config.currentLevel.spawnableScrap.Count);
                if (singleItemIndex < 0 || singleItemIndex >= result.config.currentLevel.spawnableScrap.Count)
                {
                    singleItemIndex = -1;
                }
                else
                {
                    bool flag = false;
                    for (int i = 0; i < 2; i++)
                    {
                        if (result.config.currentLevel.spawnableScrap[singleItemIndex].rarity < 5 || result.config.currentLevel.spawnableScrap[singleItemIndex].spawnableItem.twoHanded)
                        {
                            singleItemIndex = result.crm.AnomalyRandom.Next(0, result.config.currentLevel.spawnableScrap.Count);
                            continue;
                        }
                        flag = true;
                        break;
                    }
                    if (!flag && result.crm.AnomalyRandom.Next(0, 100) < 60)
                    {
                        singleItemIndex = -1;
                    }
                }
            }
            List<Item> ScrapToSpawn = [];
            int num4 = 0;
            List<int> list2 = new(result.config.currentLevel.spawnableScrap.Count);
            for (int j = 0; j < result.config.currentLevel.spawnableScrap.Count; j++)
            {
                if (result.config.currentLevel.spawnableScrap[j].spawnableItem.itemId == 152767)
                {
                    list2.Add(Mathf.Min(result.config.currentLevel.spawnableScrap[j].rarity + 30, 99));
                }
                else
                {
                    list2.Add(result.config.currentLevel.spawnableScrap[j].rarity);
                }
            }
            int[] weights = [.. list2];
            if (result.config.currentLevel.spawnableScrap.Count > 0)
            {
                for (int k = 0; k < result.numScrap; k++)
                {
                    if (singleItemIndex != -1)
                    {
                        ScrapToSpawn.Add(result.config.currentLevel.spawnableScrap[singleItemIndex].spawnableItem);
                    }
                    else
                    {
                        ScrapToSpawn.Add(result.config.currentLevel.spawnableScrap[CrackingRoundManager.GetRandomWeightedIndex(weights, result.crm.AnomalyRandom)].spawnableItem);
                    }
                }
            }
            //scrap spawn
            List<int> list = [];
            RandomScrapSpawn? randomScrapSpawn = null;
            RandomScrapSpawn[] source = Object.FindObjectsOfType<RandomScrapSpawn>();
            if (result.config.verbose)
            {
                LethalSeedCracker3.Logger.LogInfo($"source {source.Length}");
                int l2 = 0;
                List<RandomScrapSpawn> list4 = (ScrapToSpawn[l2].spawnPositionTypes != null && ScrapToSpawn[l2].spawnPositionTypes.Count != 0 && singleItemIndex == -1) ? [.. source.Where((RandomScrapSpawn x) => ScrapToSpawn[l2].spawnPositionTypes.Contains(x.spawnableItems) && !x.spawnUsed)] : [.. source];
                LethalSeedCracker3.Logger.LogInfo($"list4[0] {list4.Count}");
                foreach (var item in source)
                {
                    LethalSeedCracker3.Logger.LogInfo($"{item} {item.name} {item.transform.position} {item.spawnedItemsCopyPosition}");
                }
            }
            //List<NetworkObjectReference> list3 = [];
            List<RandomScrapSpawn> usedSpawns = [];
            int l;
            for (l = 0; l < ScrapToSpawn.Count; l++)
            {
                if (ScrapToSpawn[l] == null)
                {
                    continue;
                }
                List<RandomScrapSpawn> list4 = (ScrapToSpawn[l].spawnPositionTypes != null && ScrapToSpawn[l].spawnPositionTypes.Count != 0 && singleItemIndex == -1) ? [.. source.Where((RandomScrapSpawn x) => ScrapToSpawn[l].spawnPositionTypes.Contains(x.spawnableItems) && !x.spawnUsed)] : [.. source];
                if (list4.Count <= 0)
                {
                    continue;
                }
                if (usedSpawns.Count > 0 && randomScrapSpawn != null && list4.Contains(randomScrapSpawn))
                {
                    list4.RemoveAll(usedSpawns.Contains);
                    if (list4.Count <= 0)
                    {
                        usedSpawns.Clear();
                        l--;
                        continue;
                    }
                }
                randomScrapSpawn = list4[result.crm.AnomalyRandom.Next(0, list4.Count)];
                usedSpawns.Add(randomScrapSpawn);
                Vector3 position;
                if (randomScrapSpawn.spawnedItemsCopyPosition)
                {
                    randomScrapSpawn.spawnUsed = true;
                    position = (!randomScrapSpawn.spawnWithParent) ? randomScrapSpawn.transform.position : randomScrapSpawn.spawnWithParent.transform.position;
                }
                else
                {
                    position = GetRandomNavMeshPositionInBoxPredictable(randomScrapSpawn.transform.position, randomScrapSpawn.itemSpawnRange, result.crm.AnomalyRandom) + Vector3.up * ScrapToSpawn[l].verticalOffset;
                }
                //GameObject obj = Object.Instantiate(ScrapToSpawn[l].spawnPrefab, position, Quaternion.identity, null);
                //GrabbableObject component = obj.GetComponent<GrabbableObject>();
                //component.transform.rotation = Quaternion.Euler(component.itemProperties.restingRotation);
                //component.fallTime = 0f;
                if (singleItemIndex != -1)
                {
                    list.Add(Mathf.Clamp((int)(result.crm.AnomalyRandom.Next(ScrapToSpawn[l].minValue, ScrapToSpawn[l].maxValue) * RoundManager.Instance.scrapValueMultiplier), 50, 170));
                }
                else
                {
                    list.Add((int)(result.crm.AnomalyRandom.Next(ScrapToSpawn[l].minValue, ScrapToSpawn[l].maxValue) * RoundManager.Instance.scrapValueMultiplier));
                }
                num4 += list[^1];
                //component.scrapValue = list[^1];
                //NetworkObject component2 = obj.GetComponent<NetworkObject>();
                //component2.Spawn();
                //list3.Add(component2);
            }
            if (singleItemIndex != -1)
            {
                float num5 = 600f;
                if (result.config.currentLevel.spawnableScrap[singleItemIndex].spawnableItem.twoHanded)
                {
                    num5 = 1500f;
                }
                if (num4 > 4500)
                {
                    num4 = 0;
                    for (int num6 = 0; num6 < list.Count; num6++)
                    {
                        list[num6] = (int)(list[num6] * 0.7f);
                        num4 += list[num6];
                    }
                }
                else if (num4 < num5)
                {
                    num4 = 0;
                    for (int num7 = 0; num7 < list.Count; num7++)
                    {
                        list[num7] = (int)(list[num7] * 1.4f);
                        num4 += list[num7];
                    }
                }
            }

            result.scrapCounts = [];
            foreach (var item in ScrapToSpawn)
            {
                int count = result.scrapCounts.GetValueOrDefault(item, 0) + 1;
                result.scrapCounts[item] = count;
            }
            result.singleItemDay = singleItemIndex == -1 ? null : result.config.currentLevel.spawnableScrap[singleItemIndex].spawnableItem;
        }

        public static Vector3 GetRandomNavMeshPositionInBoxPredictable(Vector3 pos, float radius = 10f, System.Random? randomSeed = null, int layerMask = -1, float verticalScale = 1f)
        {
            if (randomSeed is null)
            {
                throw new System.ArgumentNullException(nameof(randomSeed));
            }

            float y = pos.y;
            float x = RandomNumberInRadius(radius, randomSeed);
            float y2 = RandomNumberInRadius(radius * verticalScale, randomSeed);
            float z = RandomNumberInRadius(radius, randomSeed);
            Vector3 vector = new Vector3(x, y2, z) + pos;
            vector.y = y;
            float num = Vector3.Distance(pos, vector);
            if (NavMesh.SamplePosition(vector, out NavMeshHit navHit, num + 2f, layerMask))
            {
                return navHit.position;
            }
            return pos;
        }

        private static float RandomNumberInRadius(float radius, System.Random randomSeed)
        {
            return ((float)randomSeed.NextDouble() - 0.5f) * radius;
        }
    }
}
