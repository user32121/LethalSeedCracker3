using System.Collections.Generic;
using UnityEngine;

namespace LethalSeedCracker3.src.cracker
{
    internal class WeatherEvaluator
    {
        internal static void Evaluate(Result result)
        {
            if (result.config.verbose)
            {
                LethalSeedCracker3.Logger.LogInfo("weather evaluate");
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
    }
}
