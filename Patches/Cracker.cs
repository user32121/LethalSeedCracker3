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

        internal enum STATE
        {
            NONE,
            LOAD_SCENE,
            LOADED_SCENE,
            CRACKING,
        };
        internal static STATE curState;
        private static int seedsFound = 0;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static void UpdatePrefix()
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
                    curState = STATE.CRACKING;
                    StartCracking();
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
            for (int curSeed = config.min_seed; curSeed <= config.max_seed; ++curSeed)
            {
                if (curSeed % 1000 == 0)
                {
                    LethalSeedCracker3.Logger.LogInfo($"seed {curSeed}/{config.max_seed}");
                }
                Result curResult = new(curSeed, config);
                LevelEvaluator.EvaluatePreDunGen(curResult);
                LevelEvaluator.EvaluatePostDunGen(curResult);
                ScrapEvaluator.Evaluate(curResult);
                LevelEvaluator.EvaluatePostScrap(curResult);
                EnemyEvaluator.Evaluate(curResult);
                WeatherEvaluator.Evaluate(curResult);
                FrozenResult fr = new(curResult);
                if (config.Filter(fr))
                {
                    LethalSeedCracker3.Logger.LogInfo(fr.ToFormattedString("\n  ", ", "));
                    fr.Save("results3.txt", "seeds3.txt", seedsFound > 0);
                    ++seedsFound;
                }
                curResult.Cleanup();
            }
            LethalSeedCracker3.Logger.LogInfo($"Found {seedsFound} seeds");
        }
    }
}
