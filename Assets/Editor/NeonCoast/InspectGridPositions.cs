using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NeonCoast;

public static class InspectGridPositions {
    [MenuItem("Neon Coast/Inspect Grid")]
    public static void Inspect() {
        EditorSceneManager.OpenScene("Assets/Scenes/NeonCoast.unity");
        var racers = Object.FindObjectsByType<GridRacer>(FindObjectsSortMode.None);
        Debug.Log("Found " + racers.Length + " racers on grid:");
        foreach (var r in racers) {
            Debug.Log("Racer: " + r.driverName + " | Pos: " + r.transform.position + " | Rot: " + r.transform.eulerAngles);
        }
    }
}
