using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using System.Text;
using NeonCoast;

public static class SceneValidatorEditor {
 [MenuItem("Neon Coast/Validate Entire Project")]
 public static void Validate() {
  var sb = new StringBuilder();
  sb.AppendLine("=== NEON COAST RACING: COMPREHENSIVE PROJECT VALIDATION ===");
  sb.AppendLine("Timestamp: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
  sb.AppendLine();

  bool allPassed = true;

  // 1. Check Scene
  var activeScene = EditorSceneManager.GetActiveScene();
  sb.AppendLine($"[SCENE] Active Scene: {activeScene.name} ({activeScene.path})");

  // 2. Camera Verification
  var cam = Camera.main ? Camera.main : Object.FindFirstObjectByType<Camera>();
  if (cam) {
   var data = cam.GetUniversalAdditionalCameraData();
   bool hdrOk = cam.allowHDR;
   bool ppOk = data != null && data.renderPostProcessing;
   bool smaaOk = data != null && data.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing;
   sb.AppendLine($"[CAMERA] HDR: {hdrOk} | PostProcessing: {ppOk} | SMAA AntiAliasing: {smaaOk}");
   if (!hdrOk || !ppOk) allPassed = false;
  } else {
   sb.AppendLine("[CAMERA] ERROR: Main Camera not found!");
   allPassed = false;
  }

  // 3. Post-Processing Volume
  var vol = Object.FindFirstObjectByType<Volume>();
  var profile = vol ? vol.sharedProfile : AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/NeonNight.asset");
  if (profile) {
   bool mbOff = true;
   if (profile.TryGet<MotionBlur>(out var mb)) mbOff = (!mb.active) || mb.intensity.value <= 0.001f;

   bool bloomOk = true;
   if (profile.TryGet<Bloom>(out var bloom)) bloomOk = bloom.threshold.value >= 1.0f && bloom.intensity.value <= 0.5f;

   bool colorOk = true;
   if (profile.TryGet<ColorAdjustments>(out var ca)) colorOk = ca.postExposure.value <= 0.5f && ca.contrast.value >= 10f;

   sb.AppendLine($"[POST-PROCESS] MotionBlur Disabled (no smear): {mbOff} | Bloom Calibrated: {bloomOk} | Contrast/Exposure Calibrated: {colorOk}");
   if (!mbOff || !bloomOk || !colorOk) allPassed = false;
  } else {
   sb.AppendLine("[POST-PROCESS] ERROR: VolumeProfile not found!");
   allPassed = false;
  }

  // 4. In-Game HUD Verification
  var hud = Object.FindFirstObjectByType<RacingHUD>();
  if (hud) {
   var canvas = hud.GetComponent<Canvas>();
   bool isOverlay = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay;
   bool hasCards = hud.speedDigits != null || hud.speed != null;
   bool hasChrono = hud.chronoText != null || hud.timing != null;
   bool hasPosition = hud.positionBadge != null || hud.status != null;
   sb.AppendLine($"[HUD] ScreenSpaceOverlay (Crisp, No Bloom Smear): {isOverlay} | Speedometer: {hasCards} | Chrono: {hasChrono} | Position: {hasPosition}");
   if (!isOverlay) allPassed = false;
  } else {
   sb.AppendLine("[HUD] WARNING: RacingHUD not in active scene (ignore if on MainMenu).");
  }

  // 5. Menu System Verification
  var pause = Object.FindFirstObjectByType<PauseMenu>();
  var results = Object.FindFirstObjectByType<RaceResults>();
  var audio = Object.FindFirstObjectByType<RaceAudio>();
  sb.AppendLine($"[GAMEPLAY SYSTEMS] PauseMenu: {pause != null} | RaceResults: {results != null} | RaceAudio: {audio != null}");

  // 6. Vehicle Registry Verification
  int vCount = VehicleRegistry.Vehicles.Length;
  int pCount = VehicleRegistry.Paints.Length;
  var selCar = VehicleRegistry.GetSelectedVehicle();
  var selPaint = VehicleRegistry.GetSelectedPaint();
  bool carSpecsOk = vCount == 3 && pCount == 4 && selCar != null && selPaint != null;
  sb.AppendLine($"[VEHICLES] Registered: {vCount} cars, {pCount} paints | Active: {selCar?.name} in {selPaint?.name}");
  if (!carSpecsOk) allPassed = false;

  // 7. Player Car Check
  var car = Object.FindFirstObjectByType<ArcadeCar>();
  if (car) {
   var skid = car.GetComponent<Skidmarks>();
   var fx = car.GetComponent<CarEffects>();
   sb.AppendLine($"[PLAYER CAR] TopSpeed: {car.topSpeed} | TorqueMult: {car.torqueMult} | Skidmarks: {skid != null} | CarEffects: {fx != null}");
  }

  // 8. Atmosphere & Fog
  sb.AppendLine($"[ATMOSPHERE] Fog: {RenderSettings.fog} | Density: {RenderSettings.fogDensity} | SkyColor: {RenderSettings.ambientSkyColor}");

  sb.AppendLine();
  sb.AppendLine(allPassed ? ">>> OVERALL STATUS: ALL CHECKS PASSED - PRODUCTION READY! <<<" : ">>> OVERALL STATUS: SOME CHECKS NEED ATTENTION <<<");

  string report = sb.ToString();
  File.WriteAllText("SceneValidationReport.txt", report);
  Debug.Log(report);
 }
}
