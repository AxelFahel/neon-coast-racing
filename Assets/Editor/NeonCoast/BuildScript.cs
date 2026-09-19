using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class NCRBuildScript {

 [MenuItem("Neon Coast/Build Windows Standalone")]
 public static void BuildWindows() {
  const string outputDir = @"C:\Users\Aksel\Documents\Neon Coast Racing - Build";
  string exePath = Path.Combine(outputDir, "Neon Coast Racing.exe");

  Debug.Log("[NCRBuildScript] Ensuring output directory exists: " + outputDir);
  Directory.CreateDirectory(outputDir);

  // 1. Regenera a cena NeonCoast com o novo HUD (sem sobreposição) e Pause Menu com navegação direcional
  try {
   Debug.Log("[NCRBuildScript] Rebuilding NeonCoast scene HUD and UI...");
   EditorSceneManager.OpenScene("Assets/Scenes/NeonCoast.unity");
   HUDOverhaul.RebuildHUD();
   GameUIBuilder.Build();

   // Remove luzes espúrias que causavam clarão/ofuscamento da câmera
   var allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
   foreach (var l in allLights) {
    if (l != null && (l.name == "Paint rim light" || l.name == "Projector headlight" || l.name.Contains("Rim light") || l.name.Contains("Flare"))) {
     Debug.Log("[NCRBuildScript] Removing rogue light: " + l.name);
     Object.DestroyImmediate(l.gameObject);
    }
   }
   var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
   foreach (var t in allTransforms) {
    if (t != null && (t.name == "Paint rim light" || t.name == "Projector headlight")) {
     Debug.Log("[NCRBuildScript] Destroying rogue scene object: " + t.name);
     Object.DestroyImmediate(t.gameObject);
    }
   }

   // Atualiza os visuais 3D do carro na cena NeonCoast
   var allCars = Object.FindObjectsByType<NeonCoast.ArcadeCar>(FindObjectsSortMode.None);
   foreach (var car in allCars) {
    car.CleanExcessiveLights();
    if (!car.automation) {
     var v = NeonCoast.VehicleRegistry.GetSelectedVehicle();
     var p = NeonCoast.VehicleRegistry.GetSelectedPaint();
     NeonCoast.CarVisualsOverhaul.RebuildCarVisuals(car, v, p);
     EditorUtility.SetDirty(car.gameObject);
    }
   }
   EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
   EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
  } catch (System.Exception e) {
   Debug.LogWarning("[NCRBuildScript] Note on NeonCoast UI rebuild: " + e.Message);
  }

  // 2. Regenera o Menu Principal em português com suporte a navegação direcional
  try {
   Debug.Log("[NCRBuildScript] Rebuilding MainMenu scene in Portuguese...");
   MainMenuBuilder.Build();
  } catch (System.Exception e) {
   Debug.LogWarning("[NCRBuildScript] Note on MainMenu rebuild: " + e.Message);
  }

  Debug.Log("[NCRBuildScript] Starting Windows x64 build -> " + exePath);

  string[] scenes = new string[] {
   "Assets/Scenes/MainMenu.unity",
   "Assets/Scenes/NeonCoast.unity"
  };

  BuildPlayerOptions opts = new BuildPlayerOptions {
   scenes           = scenes,
   locationPathName = exePath,
   target           = BuildTarget.StandaloneWindows64,
   options          = BuildOptions.None
  };

  BuildReport report = BuildPipeline.BuildPlayer(opts);
  BuildSummary summary = report.summary;

  if (summary.result == BuildResult.Succeeded) {
   Debug.Log("[NCRBuildScript] BUILD SUCCEEDED! Total size: " + (summary.totalSize / (1024 * 1024)) + " MB");
   EditorApplication.Exit(0);
  } else {
   Debug.LogError("[NCRBuildScript] BUILD FAILED: " + summary.result + " | Total errors: " + summary.totalErrors);
   foreach (var step in report.steps) {
    foreach (var msg in step.messages) {
     if (msg.type == LogType.Error || msg.type == LogType.Exception) {
      Debug.LogError("[BuildStepError] " + msg.content);
     }
    }
   }
   EditorApplication.Exit(1);
  }
 }
}
