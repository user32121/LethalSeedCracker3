using HarmonyLib;
using LethalSeedCracker3.src.config;

namespace LethalSeedCracker3.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class Cracker
    {
        private static readonly Config config = new("config3.txt");

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        private static void StartPrefix()
        {
            LethalSeedCracker3.Logger.LogInfo(config);
        }
    }
}
