using System;
using System.Collections.Generic;

namespace KineticRank.Logic
{
    // Pure track rotation for the MusicPlayer. Holds clip names only; the MonoBehaviour
    // decides whether a name can actually be loaded via the isAvailable callback.
    public sealed class MusicPlaylist
    {
        public static readonly string[] MenuTracks = { "menu_1", "menu_2" };
        public static readonly string[] MatchTracks =
            { "gameplay_1", "gameplay_2", "gameplay_3", "gameplay_4", "gameplay_5", "gameplay_6" };

        readonly string[] tracks;
        int index = -1;

        public MusicPlaylist(IList<string> trackNames)
        {
            tracks = trackNames == null ? new string[0] : new List<string>(trackNames).ToArray();
        }

        public int Count => tracks.Length;

        // Null until Next has returned a track.
        public string Current => index >= 0 && index < tracks.Length ? tracks[index] : null;

        // Advances in order (wrapping) to the next track that isAvailable accepts.
        // Tries each track at most once; returns null if none are available.
        public string Next(Func<string, bool> isAvailable = null)
        {
            for (int tried = 0; tried < tracks.Length; tried++)
            {
                int candidate = (index + 1 + tried) % tracks.Length;
                string name = tracks[candidate];
                if (string.IsNullOrEmpty(name)) continue;
                if (isAvailable != null && !isAvailable(name)) continue;

                index = candidate;
                return name;
            }
            return null;
        }

        public void Reset()
        {
            index = -1;
        }
    }

    // Linear crossfade between the outgoing and incoming music sources.
    public static class MusicFade
    {
        public static void Volumes(float elapsed, float duration, float targetVolume, out float outgoing, out float incoming)
        {
            float t = duration <= 0f ? 1f : elapsed / duration;
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;

            outgoing = targetVolume * (1f - t);
            incoming = targetVolume * t;
        }
    }
}
