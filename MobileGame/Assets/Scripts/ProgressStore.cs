using System;
using UnityEngine;
namespace Ashlight {
    public static class ProgressStore {
        const string Key = "Ashlight.Progress.v1";
        const string Backup = "Ashlight.Progress.v1.backup";
        static bool Valid(string json) {
            try { return new Battle(true).RestoreProgress(JsonUtility.FromJson<ProgressData>(json)); }
            catch (Exception) { return false; }
        }
        public static bool Load(Battle battle, out ProgressData loaded) {
            loaded = null;
            foreach (var key in new[] { Key, Backup }) {
                if (!PlayerPrefs.HasKey(key)) continue;
                try {
                    var candidate = JsonUtility.FromJson<ProgressData>(PlayerPrefs.GetString(key));
                    if (battle.RestoreProgress(candidate)) { loaded = candidate; return true; }
                    Debug.LogWarning("Saved progress failed validation; trying the backup if available.");
                } catch (Exception exception) { Debug.LogWarning("Could not read saved progress: " + exception.Message); }
            }
            return false;
        }
        public static bool Save(ProgressData data) {
            try {
                string json = JsonUtility.ToJson(data);
                if (!Valid(json)) { Debug.LogWarning("Progress snapshot is invalid; previous save preserved."); return false; }
                if (PlayerPrefs.HasKey(Key)) {
                    string previous = PlayerPrefs.GetString(Key);
                    if (Valid(previous)) PlayerPrefs.SetString(Backup, previous);
                }
                PlayerPrefs.SetString(Key, json); PlayerPrefs.Save(); return true;
            } catch (Exception exception) { Debug.LogWarning("Could not save progress: " + exception.Message); return false; }
        }
    }
}
