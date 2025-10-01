using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace LethalSeedCracker3.src.cracker
{
    internal static class WeatherEvaluator
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static System.Random targetedThunderRandom;
        private static GameObject[] outsideNodes;
        private static System.Random seed;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static Vector3 lastRandomStrikePosition;
        private static float globalTime;
        private static float globalTimeAtEndOfDay;
        private static float currentDayTime;
        private static float lastGlobalTimeUsed;
        private static float randomThunderTime;
        private static float timeAtLastStrike;
        private static NavMeshHit navHit;
        private static float currentWeatherVariable;
        private static RaycastHit rayHit;

        private static float nextUpdateTime;

        internal static void Evaluate(Result result)
        {
            if (result.config.verbose)
            {
                LethalSeedCracker3.Logger.LogInfo("weather evaluate");
            }
            BeginDay(result);
            while (globalTime < globalTimeAtEndOfDay)
            {
                MoveTimeOfDay(result);
            }
            SetPlanetsWeather(result);
        }

        public static void SetPlanetsWeather(Result result)
        {
            for (int i = 0; i < StartOfRound.Instance.levels.Length; i++)
            {
                StartOfRound.Instance.levels[i].currentWeather = LevelWeatherType.None;
                if (StartOfRound.Instance.levels[i].overrideWeather)
                {
                    StartOfRound.Instance.levels[i].currentWeather = StartOfRound.Instance.levels[i].overrideWeatherType;
                }
            }
            System.Random random = new(result.seed + 35);
            List<SelectableLevel> list = [.. StartOfRound.Instance.levels];
            float num = 1f;
            if (result.config.connectedPlayersAmount + 1 > 1 && result.config.daysPlayersSurvivedInARow > 2 && result.config.daysPlayersSurvivedInARow % 3 == 0)
            {
                num = random.Next(15, 25) / 10f;
            }
            float num2 = Mathf.Clamp(StartOfRound.Instance.planetsWeatherRandomCurve.Evaluate((float)random.NextDouble()) * num, 0f, 1f);
            int num3 = Mathf.Clamp((int)(num2 * (StartOfRound.Instance.levels.Length - 2f)), 0, StartOfRound.Instance.levels.Length);
            result.weathers = [];
            for (int j = 0; j < num3; j++)
            {
                SelectableLevel selectableLevel = list[random.Next(0, list.Count)];
                if (selectableLevel.randomWeathers != null && selectableLevel.randomWeathers.Length != 0)
                {
                    selectableLevel.currentWeather = selectableLevel.randomWeathers[random.Next(0, selectableLevel.randomWeathers.Length)].weatherType;
                    result.weathers[selectableLevel] = selectableLevel.currentWeather;
                }
                list.Remove(selectableLevel);
            }
        }

        private static void BeginDay(Result result)
        {
            result.lightningCount = 0;

            globalTime = 100f;
            currentDayTime = CalculatePlanetTime(result.config.currentLevel);
            globalTimeAtEndOfDay = globalTime + (TimeOfDay.Instance.totalTime - currentDayTime) / result.config.currentLevel.DaySpeedMultiplier;

            lastRandomStrikePosition = Vector3.zero;
            targetedThunderRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
            lastGlobalTimeUsed = 0f;
            randomThunderTime = globalTime + 7f;
            timeAtLastStrike = globalTime;
            navHit = default;
            nextUpdateTime = globalTime;
            outsideNodes = [.. from x in GameObject.FindGameObjectsWithTag("OutsideAINode")
                            orderby x.transform.position.x + x.transform.position.z
                            select x];
            seed = new(result.seed);

            foreach (var item in result.config.currentLevel.randomWeathers)
            {
                if (item.weatherType == LevelWeatherType.Stormy)
                {
                    currentWeatherVariable = item.weatherVariable;
                }
            }

            DetermineNextStrikeInterval();
        }

        private static float CalculatePlanetTime(SelectableLevel level)
        {
            return (globalTime + level.OffsetFromGlobalTime) * level.DaySpeedMultiplier % (TimeOfDay.Instance.totalTime + 1f);
        }

        private static void DetermineNextStrikeInterval()
        {
            timeAtLastStrike = randomThunderTime;
            float num = seed.Next(-5, 110);
            randomThunderTime += Mathf.Clamp(num * 0.25f, 0.6f, 110f) / Mathf.Clamp(currentWeatherVariable, 1f, 100f);
            if (randomThunderTime < nextUpdateTime)
            {
                nextUpdateTime = randomThunderTime + 0.001f;
            }
        }

        private static void MoveTimeOfDay(Result result)
        {
            if (globalTime < nextUpdateTime)
            {
                globalTime = nextUpdateTime;
            }
            if (randomThunderTime < globalTime + 10)
            {
                nextUpdateTime = randomThunderTime + 0.001f;
            }
            else
            {
                nextUpdateTime = globalTime + 10;
            }
            OnGlobalTimeSync(result);
            if (globalTime > randomThunderTime)
            {
                LightningStrikeRandom(result);
                DetermineNextStrikeInterval();
            }
        }

        private static void OnGlobalTimeSync(Result result)
        {
            float num = RoundUpToNearestTen(globalTime);
            if (num != lastGlobalTimeUsed)
            {
                lastGlobalTimeUsed = num;
                seed = new((int)num + result.seed);
                timeAtLastStrike = globalTime;
            }
        }

        private static int RoundUpToNearestTen(float x)
        {
            return (int)(x / 10f) * 10;
        }

        private static void LightningStrikeRandom(Result result)
        {
            Vector3 randomNavMeshPositionInBoxPredictable;
            if (seed.Next(0, 100) < 60 && (randomThunderTime - timeAtLastStrike) * currentWeatherVariable < 3f)
            {
                randomNavMeshPositionInBoxPredictable = lastRandomStrikePosition;
            }
            else
            {
                int num = seed.Next(0, outsideNodes.Length);
                if (outsideNodes == null || outsideNodes[num] == null)
                {
                    outsideNodes = [.. from x in GameObject.FindGameObjectsWithTag("OutsideAINode")
                                    orderby x.transform.position.x + x.transform.position.z
                                    select x];
                    num = seed.Next(0, outsideNodes.Length);
                }
                randomNavMeshPositionInBoxPredictable = outsideNodes[num].transform.position;
                randomNavMeshPositionInBoxPredictable = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(randomNavMeshPositionInBoxPredictable, 15f, navHit, seed);
            }
            lastRandomStrikePosition = randomNavMeshPositionInBoxPredictable;
            LightningStrike(result, randomNavMeshPositionInBoxPredictable, useTargetedObject: false);
        }


        private static void LightningStrike(Result result, Vector3 strikePosition, bool useTargetedObject)
        {
            System.Random random;
            if (useTargetedObject)
            {
                random = targetedThunderRandom;
            }
            else
            {
                random = new System.Random(seed.Next(0, 10000));
            }
            bool flag = false;
            Vector3 vector = Vector3.zero;
            for (int i = 0; i < 7; i++)
            {
                if (i == 6)
                {
                    vector = strikePosition + Vector3.up * 80f;
                }
                else
                {
                    float x = random.Next(-32, 32);
                    float z = random.Next(-32, 32);
                    vector = strikePosition + Vector3.up * 80f + new Vector3(x, 0f, z);
                }
                if (!Physics.Linecast(vector, strikePosition + Vector3.up * 0.5f, out rayHit, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                if (!Physics.Raycast(vector, strikePosition - vector, out rayHit, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    return;
                }
                _ = rayHit.point;
            }
            ++result.lightningCount;
        }
    }
}
