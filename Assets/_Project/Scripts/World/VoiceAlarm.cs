using System;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// Speaks the danger warning. If a recorded voice clip is given, it plays that. Otherwise, on Windows, it uses
    /// the computer's built-in voice as a placeholder (so you hear "Danger! Do not go in the water!" now), and on
    /// other systems it stays silent (the on-screen text still shows). Replace with a real recording later.
    /// </summary>
    public static class VoiceAlarm
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        static System.Diagnostics.Process _speaking;
#endif

        public static void Speak(string text, AudioClip clip, AudioSource source)
        {
            if (clip != null && source != null)
            {
                source.PlayOneShot(clip);
                return;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                if (_speaking != null && !_speaking.HasExited) return; // still talking

                string safe = text.Replace("'", "").Replace("\"", "");
                var info = new System.Diagnostics.ProcessStartInfo("powershell.exe",
                    "-NoProfile -NonInteractive -Command \"Add-Type -AssemblyName System.Speech; " +
                    "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer; $s.Rate = 1; $s.Speak('" + safe + "')\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                };
                _speaking = System.Diagnostics.Process.Start(info);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Badeland: could not speak the warning (" + e.Message + "). The on-screen text still shows.");
            }
#endif
        }
    }
}
