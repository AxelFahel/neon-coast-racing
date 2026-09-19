using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using NeonCoast;

public static class AudioBuilder {
 [MenuItem("Neon Coast/Add Race Audio")]
 public static void Build(){
  if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode first.");
  var race=Object.FindFirstObjectByType<RaceSession>();
  if(!race)throw new System.Exception("Open NeonCoast scene first (RaceSession not found).");
  var oldAudio = Object.FindFirstObjectByType<RaceAudio>();
  if(oldAudio) Object.DestroyImmediate(oldAudio.gameObject);

  var go=new GameObject("Race Audio");
  var ra=go.AddComponent<RaceAudio>();
  ra.race=race;

  EditorSceneManager.MarkSceneDirty(race.gameObject.scene);
  EditorSceneManager.SaveScene(race.gameObject.scene);
  AssetDatabase.SaveAssets();
  Debug.Log("Neon Coast Racing: Race Audio system added (countdown beeps + finish fanfare).");
 }
}
