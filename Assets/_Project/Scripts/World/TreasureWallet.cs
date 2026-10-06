using UnityEngine;

namespace Badeland.World
{
    /// <summary>How much treasure this machine's player has found. Kept simple for now; the real save system comes later.</summary>
    public static class TreasureWallet
    {
        public static int Count { get; private set; }

        public static void Add(int amount) => Count += amount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Count = 0;
    }
}
