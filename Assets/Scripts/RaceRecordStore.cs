using System.Text;
using UnityEngine;

namespace NeonCoast {
/// <summary>Stores personal bests per track, game mode and vehicle.</summary>
public static class RaceRecordStore {
 const string Prefix = "NCR_Record_v2";
 const string MigrationFlag = "NCR_Record_v2_Migrated";

 public static string BuildKey(string metric, string trackId, bool timeTrial, string vehicleId) {
  return string.Join("_", Prefix, Clean(metric), Clean(trackId), timeTrial ? "time_trial" : "circuit", Clean(vehicleId));
 }

 public static float Load(string metric, string trackId, bool timeTrial, string vehicleId) {
  string key = BuildKey(metric, trackId, timeTrial, vehicleId);
  if (PlayerPrefs.HasKey(key)) return PlayerPrefs.GetFloat(key);

  // Preserve one set of records from builds prior to v0.3 without copying it
  // into every vehicle/mode combination.
  if (!PlayerPrefs.HasKey(MigrationFlag)) {
   string legacyKey = metric == "lap" ? "NCR_BestLap" : "NCR_BestRace";
   if (PlayerPrefs.HasKey(legacyKey)) PlayerPrefs.SetFloat(key, PlayerPrefs.GetFloat(legacyKey));
   PlayerPrefs.SetInt(MigrationFlag, 1);
   PlayerPrefs.Save();
   if (PlayerPrefs.HasKey(key)) return PlayerPrefs.GetFloat(key);
  }

  return float.PositiveInfinity;
 }

 public static void Save(string metric, string trackId, bool timeTrial, string vehicleId, float value) {
  if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f) return;
  PlayerPrefs.SetFloat(BuildKey(metric, trackId, timeTrial, vehicleId), value);
 }

 static string Clean(string value) {
  if (string.IsNullOrWhiteSpace(value)) return "unknown";
  var result = new StringBuilder(value.Length);
  foreach (char c in value.ToLowerInvariant()) {
   if (char.IsLetterOrDigit(c)) result.Append(c);
   else if (result.Length > 0 && result[result.Length - 1] != '-') result.Append('-');
  }
  return result.ToString().Trim('-');
 }
}
}
