using System;

namespace LethalSeedCracker3.src
{
    internal static class Common
    {
        internal static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }
    }
}