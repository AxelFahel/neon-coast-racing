using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonCoast.EditorTools {
public static class ProductionValidation {
 const string GameplayScene = "Assets/Scenes/NeonCoast.unity";
 const string MenuScene = "Assets/Scenes/MainMenu.unity";

 public static void Run() {
  RunInternal(true);
 }

 public static void RunLive() {
  RunInternal(false);
 }

 static void RunInternal(bool exitWhenFinished) {
  SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
  int exitCode = 0;
  try {
   AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
   ValidateRecordKeys();
   ValidateScene(MenuScene, false);
   ValidateScene(GameplayScene, true);
   ValidateBuildSettings();
   Debug.Log("[NCR Validation] PASS — records, scenes, UI and build settings are valid.");
  } catch (Exception ex) {
   exitCode = 1;
   Debug.LogError("[NCR Validation] FAIL — " + ex);
  } finally {
   if (!exitWhenFinished) EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
  }
  if (exitWhenFinished) EditorApplication.Exit(exitCode);
 }

 static void ValidateRecordKeys() {
  string a = RaceRecordStore.BuildKey("lap", "Neon Coast", true, "aster_gt");
  string b = RaceRecordStore.BuildKey("lap", "Neon Coast", false, "aster_gt");
  string c = RaceRecordStore.BuildKey("lap", "Neon Coast", true, "valkyrie_apex");
  if (a == b || a == c || !a.Contains("time_trial") || a.Contains(" "))
   throw new InvalidOperationException("Record keys are not isolated by mode/vehicle.");
 }

 static void ValidateScene(string path, bool gameplay) {
  if (!File.Exists(path)) throw new FileNotFoundException(path);
  Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
  var roots = scene.GetRootGameObjects();
  var canvases = roots.SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).ToArray();
  var eventSystems = roots.SelectMany(r => r.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).ToArray();
  if (canvases.Length == 0) throw new InvalidOperationException(path + " has no Canvas.");
  if (eventSystems.Length != 1) throw new InvalidOperationException(path + " must contain exactly one EventSystem.");

  foreach (var canvas in canvases) {
   var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
   if (!scaler) continue;
   if (scaler.uiScaleMode != UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
    throw new InvalidOperationException(canvas.name + " does not use Scale With Screen Size.");
   if (scaler.referenceResolution.x < 1280 || scaler.referenceResolution.y < 720)
    throw new InvalidOperationException(canvas.name + " reference resolution is too small.");
  }

  if (gameplay) {
   if (roots.SelectMany(r => r.GetComponentsInChildren<RaceSession>(true)).Count() != 1)
    throw new InvalidOperationException("Gameplay scene must contain exactly one RaceSession.");
   if (!roots.SelectMany(r => r.GetComponentsInChildren<RacingHUD>(true)).Any())
    throw new InvalidOperationException("Gameplay scene has no RacingHUD.");
  }
 }

 static void ValidateBuildSettings() {
  var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
  if (enabled.Length < 2 || enabled[0] != MenuScene || !enabled.Contains(GameplayScene))
   throw new InvalidOperationException("Build scene order must start with MainMenu and include NeonCoast.");
 }
}
}
