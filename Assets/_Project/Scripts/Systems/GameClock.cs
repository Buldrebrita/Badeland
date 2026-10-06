using System;
using UnityEngine;

namespace Badeland.Systems
{
    /// <summary>
    /// The clock that anything shared between players (jumping fish, the rotating bar) must follow, so everyone
    /// sees the same thing at the same moment. Offline it is just Unity's time. The networking layer swaps in
    /// the network's shared clock while a game is running.
    /// </summary>
    public static class GameClock
    {
        /// <summary>Set by the networking layer. Null means "use local time".</summary>
        public static Func<double> Provider;

        public static double Now => Provider != null ? Provider() : Time.timeAsDouble;
    }
}
