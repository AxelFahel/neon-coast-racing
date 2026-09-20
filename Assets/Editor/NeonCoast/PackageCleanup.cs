using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace NeonCoast.EditorTools {
public static class PackageCleanup {
 static readonly string[] PackagesToRemove = {
  "com.unity.sentis",
  "com.unity.visualscripting",
  "com.unity.multiplayer.center",
  "com.unity.ai.navigation",
  "com.unity.collab-proxy"
 };

 static AddAndRemoveRequest request;
 static double deadline;
 static bool exitWhenFinished;

 public static void Run() {
  Start(true);
 }

 public static void RunLive() {
  Start(false);
 }

 static void Start(bool shouldExit) {
  Debug.Log("[NCR Package Cleanup] Removing unused packages: " + string.Join(", ", PackagesToRemove));
  exitWhenFinished = shouldExit;
  request = Client.AddAndRemove(new string[0], PackagesToRemove);
  deadline = EditorApplication.timeSinceStartup + 600d;
  EditorApplication.update += Poll;
 }

 static void Poll() {
  if (request == null) return;
  if (!request.IsCompleted) {
   if (EditorApplication.timeSinceStartup <= deadline) return;
   EditorApplication.update -= Poll;
   Debug.LogError("[NCR Package Cleanup] Timed out waiting for UPM.");
   if (exitWhenFinished) EditorApplication.Exit(2);
   return;
  }

  EditorApplication.update -= Poll;
  if (request.Status == StatusCode.Success) {
   Debug.Log("[NCR Package Cleanup] Resolved packages: " + string.Join(", ", request.Result.Select(p => p.name + "@" + p.version)));
   if (exitWhenFinished) EditorApplication.Exit(0);
  } else {
   Debug.LogError("[NCR Package Cleanup] Failed: " + request.Error?.message);
   if (exitWhenFinished) EditorApplication.Exit(1);
  }
 }
}
}
