using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace NeonCoast {
[InitializeOnLoad]
public static class AutoExecutor {
 const string FlagPath = "Temp/AutoExecutor_Done.txt";

 [InitializeOnLoadMethod]
 static void RunOnLoad() {
  EditorApplication.delayCall += Execute;
 }

 static void Execute() {
  if (File.Exists(FlagPath)) return;
  // Existing authored scenes must not be rebuilt automatically on editor restart.
  if (File.Exists("Assets/Scenes/MainMenu.unity") && File.Exists("Assets/Scenes/NeonCoast.unity")) return;
  if (EditorApplication.isPlayingOrWillChangePlaymode) return;

  try {
   Debug.Log("=== AUTO EXECUTOR STARTED ===");

   // 1. Open NeonCoast scene
   var neonScene = EditorSceneManager.OpenScene("Assets/Scenes/NeonCoast.unity", OpenSceneMode.Single);

   // 2. Apply Visual Fidelity Restorer (crisp camera, bloom, contrast, fog, road)
   VisualFidelityRestorer.Apply();

   // 3. Rebuild High-Visibility HUD
   HUDOverhaul.RebuildHUD();

   // 4. Build Pause Menu & Race Results UI
   GameUIBuilder.Build();

   // 5. Build Race Audio
   AudioBuilder.Build();

   // Save NeonCoast scene
   EditorSceneManager.MarkSceneDirty(neonScene);
   EditorSceneManager.SaveScene(neonScene);

   // 6. Build Main Menu scene with Vehicle Selection
   MainMenuBuilder.Build();

   // 7. Re-open NeonCoast scene so user is ready on the racing track
   EditorSceneManager.OpenScene("Assets/Scenes/NeonCoast.unity", OpenSceneMode.Single);

   // 8. Run comprehensive validation
   SceneValidatorEditor.Validate();

   File.WriteAllText(FlagPath, "Executed at " + System.DateTime.Now);
   AssetDatabase.SaveAssets();

   Debug.Log("### AUTO_EXECUTOR_COMPLETED_SUCCESSFULLY ###");
  } catch (System.Exception ex) {
   Debug.LogError("AUTO_EXECUTOR_FAILED: " + ex);
  }
 }
}
}

