using UnityEngine;

namespace NeonCoast {
/// <summary>Small, persistent accessibility and performance settings surface.</summary>
public static class GameSettings {
 const string SteeringSensitivityKey = "NCR_SteeringSensitivity";
 const string SteeringDeadzoneKey = "NCR_SteeringDeadzone";
 const string VibrationKey = "NCR_Vibration";
 const string FrameRateKey = "NCR_FrameRate";

 public static float SteeringSensitivity {
  get => PlayerPrefs.GetFloat(SteeringSensitivityKey, 1f);
  set => PlayerPrefs.SetFloat(SteeringSensitivityKey, Mathf.Clamp(value, .5f, 1.5f));
 }

 public static float SteeringDeadzone {
  get => PlayerPrefs.GetFloat(SteeringDeadzoneKey, .12f);
  set => PlayerPrefs.SetFloat(SteeringDeadzoneKey, Mathf.Clamp(value, .05f, .35f));
 }

 public static bool VibrationEnabled {
  get => PlayerPrefs.GetInt(VibrationKey, 1) != 0;
  set => PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
 }

 public static int TargetFrameRate {
  get => PlayerPrefs.GetInt(FrameRateKey, 120);
  set => PlayerPrefs.SetInt(FrameRateKey, Mathf.Clamp(value, 30, 240));
 }

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
 static void ApplyRuntimeDefaults() {
  Application.targetFrameRate = TargetFrameRate;
 }
}
}
