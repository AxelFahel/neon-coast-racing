using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using NeonCoast;

public static class VisualFidelityRestorer {
 [MenuItem("Neon Coast/Restore Ultra-Crisp Visuals")]
 public static void Apply() {
  if (EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode first.");

  // 1. Camera calibration
  var cam = Camera.main;
  if (!cam) cam = Object.FindFirstObjectByType<Camera>();
  if (cam) {
   cam.allowHDR = true;
   cam.allowMSAA = false; // Deferred uses post-AA
   var data = cam.GetUniversalAdditionalCameraData();
   if (data) {
    data.renderPostProcessing = true;
    data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
    data.antialiasingQuality = AntialiasingQuality.High;
    data.volumeLayerMask = ~0; // All layers
   }
  }

  // 2. Volume Profile (NeonNight.asset) calibration
  var vol = Object.FindFirstObjectByType<Volume>();
  VolumeProfile profile = vol ? vol.sharedProfile : AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/NeonNight.asset");
  if (profile) {
   // Bloom: crisp neon only, no hazy glow on dark/matte surfaces
   if (profile.TryGet<Bloom>(out var bloom)) {
    bloom.active = true;
    bloom.threshold.value = 1.1f;
    bloom.intensity.value = 0.30f;
    bloom.scatter.value = 0.55f;
    EditorUtility.SetDirty(bloom);
   }

   // MotionBlur: DISABLED to eliminate full-screen camera smearing
   if (profile.TryGet<MotionBlur>(out var mb)) {
    mb.active = false;
    mb.intensity.value = 0f;
    EditorUtility.SetDirty(mb);
   }

   // Color Adjustments: rich deep contrast, remove milky overexposure
   if (profile.TryGet<ColorAdjustments>(out var grade)) {
    grade.active = true;
    grade.postExposure.value = 0.25f;
    grade.contrast.value = 16f;
    grade.saturation.value = 12f;
    EditorUtility.SetDirty(grade);
   }

   // Tonemapping: ACES cinematic film curve
   if (profile.TryGet<Tonemapping>(out var tm)) {
    tm.active = true;
    tm.mode.value = TonemappingMode.ACES;
    EditorUtility.SetDirty(tm);
   }

   EditorUtility.SetDirty(profile);
  }

  // 3. Atmosphere & Fog: clear air so city skyline and road are sharp
  RenderSettings.fog = true;
  RenderSettings.fogMode = FogMode.ExponentialSquared;
  RenderSettings.fogDensity = 0.0018f;
  RenderSettings.fogColor = new Color(0.04f, 0.06f, 0.12f);
  RenderSettings.ambientMode = AmbientMode.Trilight;
  RenderSettings.ambientSkyColor = new Color(0.20f, 0.26f, 0.40f);
  RenderSettings.ambientEquatorColor = new Color(0.10f, 0.14f, 0.22f);
  RenderSettings.ambientGroundColor = new Color(0.04f, 0.06f, 0.10f);

  // 4. Road Material: crisp specular reflections
  var road = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wet graphite asphalt.mat");
  if (road) {
   road.SetFloat("_Smoothness", 0.74f);
   road.SetFloat("_Metallic", 0.18f);
   road.enableInstancing = true;
   EditorUtility.SetDirty(road);
  }

  // 5. Rebuild HUD to ensure razor-sharp ScreenSpaceOverlay UI
  var race = Object.FindFirstObjectByType<RaceSession>();
  if (race) {
   HUDOverhaul.RebuildHUD();
  }

  if (cam) EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
  AssetDatabase.SaveAssets();
  Debug.Log("Neon Coast Racing: Ultra-Crisp Visuals successfully restored! Image clarity, deep contrast, and razor-sharp UI active.");
 }
}
