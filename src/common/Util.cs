using System;

namespace LethalSeedCracker3.src.common
{
    internal static class Util
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