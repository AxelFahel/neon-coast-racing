using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
[InitializeOnLoad]
public static class NeonPackageSetup {
 static AddAndRemoveRequest request;
 static NeonPackageSetup() { EditorApplication.delayCall += Begin; }
 static void Begin() {
  if (SessionState.GetBool("NeonPackagesRequested", false)) return;
  var manifest=System.IO.File.ReadAllText("Packages/manifest.json"); if(manifest.Contains("com.unity.pipeline") && manifest.Contains("com.unity.cinemachine") && manifest.Contains("com.unity.probuilder")) return;
  if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Begin; return; }
  SessionState.SetBool("NeonPackagesRequested", true);
  request = Client.AddAndRemove(new[]{"com.unity.pipeline", "com.unity.cinemachine", "com.unity.probuilder"});
  EditorApplication.update += Poll;
 }
 static void Poll() {
  if(request == null || !request.IsCompleted) return;
  EditorApplication.update -= Poll;
  if(request.Status == StatusCode.Success) Debug.Log("Neon Coast: official packages installed.");
  else Debug.LogError("Neon Coast package setup: " + request.Error.message);
 }
}

