using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A tiny message board for on-screen hints. Things in the world post a hint each frame ("Press E to read"),
    /// a note to read, or a banner. <see cref="InfoHud"/> draws them.
    /// </summary>
    public static class HudHints
    {
        static string _text = "";
        static int _frame = -10;

        /// <summary>A hint that stays on screen only while something keeps posting it each frame.</summary>
        public static void Show(string text)
        {
            _text = text;
            _frame = Time.frameCount;
        }

        public static string Current => _frame >= Time.frameCount - 1 ? _text : "";

        // A note being read.
        public static string NoteTitle = "";
        public static string NoteBody = "";
        public static float NoteUntil;
        public static bool NoteVisible => Time.time < NoteUntil;

        // A message that stays until it is cleared (for example at the end of an area).
        public static string Banner = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            _text = "";
            _frame = -10;
            NoteUntil = 0f;
            Banner = "";
        }
    }
}
