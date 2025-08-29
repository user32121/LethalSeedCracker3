using HarmonyLib;
using LethalSeedCracker3.src.common;
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
            CRACKING_PRE_DUNGEN,
            CRACKING_GEN_DUNGEN,
            CRACKING_POST_DUNGEN,
            DONE_CRACKING,
        };
        internal static STATE curState;
        private static int seedsFound = 0;
        private static int curSeed;
        private static Result? curResult;

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
                    curState = STATE.CRACKING_PRE_DUNGEN;
                    break;
                case STATE.CRACKING_PRE_DUNGEN:
                    ContinueCrackingPreDunGen();
                    break;
                case STATE.CRACKING_GEN_DUNGEN:
                    break;
                case STATE.CRACKING_POST_DUNGEN:
                    ContinueCrackingPostDunGen();
                    break;
                case STATE.DONE_CRACKING:
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
            curSeed = config.min_seed;
        }

        private static void ContinueCrackingPreDunGen()
        {
            if (curSeed % 1000 == 0)
            {
                LethalSeedCracker3.Logger.LogInfo($"seed {curSeed}/{config.max_seed}");
            }
            curResult = new(curSeed, config);
            LevelEvaluator.EvaluatePreDunGen(curResult);
        }

        private static void ContinueCrackingPostDunGen()
        {
            curResult = Util.NonNull(curResult, nameof(curResult));
            LevelEvaluator.EvaluatePostDunGen(curResult);
            ScrapEvaluator.Evaluate(curResult);
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
            ++curSeed;
            if (curSeed >= config.max_seed)
            {
                LethalSeedCracker3.Logger.LogInfo($"Found {seedsFound} seeds");
                curState = STATE.DONE_CRACKING;
            }
            else
            {
                curState = STATE.CRACKING_PRE_DUNGEN;
            }
        }
    }
}
