using HarmonyLib;
using LethalSeedCracker3.src.config;
using LethalSeedCracker3.src.cracker;
using UnityEngine.SceneManagement;

namespace LethalSeedCracker3.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class Cracker
    {
        private static readonly Config config = new("config3.txt");

        private enum STATE
        {
            NONE,
            LOAD_SCENE,
            LOADED_SCENE,
            CRACKING,
        };
        private static STATE curState;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static void UpdatePostfix()
        {
            switch (curState)
            {
                case STATE.NONE:
                    StartOfRound.Instance.NetworkManager.SceneManager.OnLoadComplete += OnLoadComplete;
                    StartOfRound.Instance.NetworkManager.SceneManager.LoadScene(config.currentLevel.sceneName, LoadSceneMode.Additive);
                    curState = STATE.LOAD_SCENE;
                    break;
                case STATE.LOAD_SCENE:
                    break;
                case STATE.LOADED_SCENE:
                    StartCracking();
                    curState = STATE.CRACKING;
                    break;
                case STATE.CRACKING:
                    break;
                default:
                    throw new System.Exception($"Not implemented state: {curState}");
            }
        }

        private static void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            curState = STATE.LOADED_SCENE;
        }

        private static void StartCracking()
        {
            int seedsFound = 0;
            for (int seed = config.min_seed; seed <= config.max_seed; ++seed)
            {
                if (seed % 1000 == 0)
                {
                    LethalSeedCracker3.Logger.LogInfo($"seed {seed}/{config.max_seed}");
                }
                Result result = new(seed, config);
                LevelEvaluator.Evaulate(result);
                ScrapEvaluator.Evaluate(result);
                EnemyEvaluator.Evaluate(result);
                WeatherEvaluator.Evaluate(result);
                FrozenResult fr = new(result);
                if (config.Filter(fr))
                {
                    LethalSeedCracker3.Logger.LogInfo(fr.ToFormattedString("\n  ", ", "));
                    fr.Save("results3.txt", "seeds3.txt", seedsFound > 0);
                    ++seedsFound;
                }
                fr.Cleanup();
            }
            LethalSeedCracker3.Logger.LogInfo($"Found {seedsFound} seeds");
        }
    }
}
