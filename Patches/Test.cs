using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalSeedCracker3.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class Test
    {
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
        private static void UpdatePostfix(RoundManager __instance)
        {
            switch (curState)
            {
                case STATE.NONE:
                    StartOfRound.Instance.NetworkManager.SceneManager.OnLoadComplete += OnLoadComplete;
                    StartOfRound.Instance.NetworkManager.SceneManager.LoadScene(StartOfRound.Instance.levels[4].sceneName, LoadSceneMode.Additive);
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
            LethalSeedCracker3.Logger.LogInfo("qqq");
            var denialPoints = GameObject.FindGameObjectsWithTag("SpawnDenialPoint");
            foreach (var item in denialPoints)
            {
                LethalSeedCracker3.Logger.LogInfo($"{item} {item.transform.position}");
            }
            LethalSeedCracker3.Logger.LogInfo("www");
            var spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
            foreach (var item in spawnPoints)
            {
                LethalSeedCracker3.Logger.LogInfo($"{item} {item.transform.position}");
            }
        }
    }
}
