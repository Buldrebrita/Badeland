using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A tiny message board for on-screen text. Things in the world post a hint each frame ("Press E to read"), open a
    /// note to read, or set a banner. <see cref="InfoHud"/> draws them. While a note is open its reader cannot move
    /// and cannot be hurt, so they can read in peace.
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
        public static string NoteTitle { get; private set; } = "";
        public static string NoteBody { get; private set; } = "";
        /// <summary>Goes up every time a note is opened, so the screen knows to scroll back to the top.</summary>
        public static int NoteSerial { get; private set; }
        public static object NoteSource { get; private set; }
        public static bool NoteVisible => _reader != null;

        static PlayerController _reader;

        // A message that stays until it is cleared (for example at the end of an area).
        public static string Banner = "";

        public static bool IsReader(PlayerController player) => _reader != null && _reader == player;

        public static void OpenNote(PlayerController reader, object source, string title, string body)
        {
            CloseNote();
            _reader = reader;
            NoteSource = source;
            NoteTitle = title;
            NoteBody = body;
            NoteSerial++;

            // The reader stands still and nothing can touch them while they read.
            reader.InputLocked = true;
            reader.Invulnerable = true;
        }

        public static void CloseNote()
        {
            if (_reader != null)
            {
                _reader.InputLocked = false;
                _reader.Invulnerable = false;
            }
            _reader = null;
            NoteSource = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            _text = "";
            _frame = -10;
            _reader = null;
            NoteSource = null;
            Banner = "";
        }
    }
}
