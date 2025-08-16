using System;
using System.Collections.Generic;
using UnityEngine;

public static class SaveService
{
    private const string KEY = "BEERANGED_SAVE_V2";

    [Serializable]
    public class LeaderboardEntry
    {
        public float seconds;      // run length in seconds
        public string date;        // "YYYY-MM-DD" local date
        public string initials;    // up to 5 letters, uppercased
    }

    [Serializable]
    public class SaveData
    {
        public int version = 2;

        // Legacy single "best" for compatibility
        public float bestTimeSeconds = 0f;

        // NEW in v2: structured leaderboard entries (Top 10 kept after normalize)
        public List<LeaderboardEntry> leaderboard = new List<LeaderboardEntry>();

        // Optional: remember last used initials to prefill next time
        public string lastInitials = "BRAZA";

        // Settings (expand as needed)
        public float musicVolume = 1f;
        public float sfxVolume = 1f;

        // --- v1 legacy (for migration) ---
        public List<float> topTimes = new List<float>(); // old float-only list
    }

    private static SaveData _cache;

    public static SaveData Data
    {
        get
        {
            if (_cache == null) _cache = Load();
            return _cache;
        }
    }

    public static SaveData Load()
    {
        if (!PlayerPrefs.HasKey(KEY))
        {
            // Attempt to migrate from older key if present
            var data = new SaveData();
            TryMigrateFromV1(ref data);
            return data;
        }

        string json = PlayerPrefs.GetString(KEY, "{}");
        var loaded = JsonUtility.FromJson<SaveData>(json);
        if (loaded == null) loaded = new SaveData();

        // Normalize after load (sort/trim and sync best)
        NormalizeAndClamp(loaded);
        return loaded;
    }

    public static void Save()
    {
        if (_cache == null) _cache = new SaveData();
        NormalizeAndClamp(_cache);
        string json = JsonUtility.ToJson(_cache);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }

    // --- Public API ---

    /// <summary>Record a completed run. Higher seconds = better. Initials are clamped to 5 letters and uppercased. Date uses local time.</summary>
    public static void RecordRun(float seconds, string initials = "BRAZA", DateTime? dateLocal = null)
    {
        if (seconds <= 0f) return;

        var entry = new LeaderboardEntry
        {
            seconds = seconds,
            initials = SanitizeInitials(initials),
            date = (dateLocal ?? DateTime.Now).ToString("yyyy-MM-dd")
        };

        Data.leaderboard.Add(entry);

        // Keep legacy best in sync
        if (seconds > Data.bestTimeSeconds)
            Data.bestTimeSeconds = seconds;

        // Remember last initials
        Data.lastInitials = entry.initials;

        Save();
    }

    /// <summary>Convenience: updates best if improved, and records the run (with initials).</summary>
    public static bool TryUpdateBestTime(float seconds, string initials = "BRAZA")
    {
        bool improved = seconds > Data.bestTimeSeconds;
        RecordRun(seconds, initials);
        return improved;
    }

    /// <summary>Get a copy of the Top 10 entries, sorted descending by time.</summary>
    public static List<LeaderboardEntry> GetTopEntries()
    {
        var list = new List<LeaderboardEntry>(Data.leaderboard);
        list.RemoveAll(e => e == null || e.seconds <= 0f || float.IsNaN(e.seconds) || float.IsInfinity(e.seconds));
        list.Sort((a, b) => b.seconds.CompareTo(a.seconds)); // desc
        if (list.Count > 10) list = list.GetRange(0, 10);
        return list;
    }

    public static void ResetLeaderboard()
    {
        Data.leaderboard = new List<LeaderboardEntry>();
        Data.bestTimeSeconds = 0f;
        Save();
    }

    public static void SetMusicVolume(float v) { Data.musicVolume = Mathf.Clamp01(v); Save(); }
    public static void SetSfxVolume(float v) { Data.sfxVolume = Mathf.Clamp01(v); Save(); }

    public static string SanitizeInitials(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "BRAZA";
        // Keep A-Z only, up to 5, uppercased
        char[] buf = new char[Mathf.Min(5, raw.Length)];
        int n = 0;
        for (int i = 0; i < raw.Length && n < 5; i++)
        {
            char c = raw[i];
            if (char.IsLetter(c))
                buf[n++] = char.ToUpperInvariant(c);
        }
        if (n == 0) return "BRAZA";
        return new string(buf, 0, n);
    }

    // --- Helpers ---

    private static void NormalizeAndClamp(SaveData data)
    {
        if (data.leaderboard == null) data.leaderboard = new List<LeaderboardEntry>();

        // Clean + sort
        data.leaderboard.RemoveAll(e => e == null || e.seconds <= 0f || float.IsNaN(e.seconds) || float.IsInfinity(e.seconds));
        data.leaderboard.Sort((a, b) => b.seconds.CompareTo(a.seconds));
        if (data.leaderboard.Count > 10)
            data.leaderboard = data.leaderboard.GetRange(0, 10);

        // Sync legacy best
        float best = 0f;
        if (data.leaderboard.Count > 0) best = Mathf.Max(best, data.leaderboard[0].seconds);
        data.bestTimeSeconds = Mathf.Max(data.bestTimeSeconds, best);

        // Ensure lastInitials has something sane
        data.lastInitials = SanitizeInitials(string.IsNullOrEmpty(data.lastInitials) ? "BRAZA" : data.lastInitials);
    }

    private static void TryMigrateFromV1(ref SaveData data)
    {
        // Old key (from earlier step) might exist:
        const string OLD_KEY = "BEERANGED_SAVE_V1";
        if (!PlayerPrefs.HasKey(OLD_KEY)) return;

        string json = PlayerPrefs.GetString(OLD_KEY, "{}");
        var old = JsonUtility.FromJson<SaveData>(json);
        if (old == null) return;

        // Move best
        data.bestTimeSeconds = old.bestTimeSeconds;

        // Convert old topTimes (floats) into entries
        if (old.topTimes != null)
        {
            foreach (var t in old.topTimes)
            {
                if (t <= 0f) continue;
                data.leaderboard.Add(new LeaderboardEntry
                {
                    seconds = t,
                    initials = "BRAZA",
                    date = DateTime.Now.ToString("yyyy-MM-dd")
                });
            }
        }
        NormalizeAndClamp(data);
        Save();
    }
}
