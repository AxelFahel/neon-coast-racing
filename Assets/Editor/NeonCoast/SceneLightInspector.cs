using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SceneLightInspector {
    [MenuItem("Neon Coast/Inspect Lights")]
    public static void InspectAndClean() {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/NeonCoast.unity");
        Debug.Log("=== INSPECTING ALL LIGHTS IN NEONCOAST.UNITY ===");
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights) {
            string path = l.name;
            Transform p = l.transform.parent;
            while (p != null) {
                path = p.name + "/" + path;
                p = p.parent;
            }
            Debug.Log($"[SCENE_LIGHT] Path: '{path}' | Type: {l.type} | Intensity: {l.intensity} | Range: {l.range} | Color: {l.color} | LocalPos: {l.transform.localPosition}");
        }

        // Specifically inspect all ArcadeCars
        var cars = Object.FindObjectsByType<NeonCoast.ArcadeCar>(FindObjectsSortMode.None);
        Debug.Log($"=== FOUND {cars.Length} ARCADE CARS IN SCENE ===");
        foreach (var car in cars) {
            Debug.Log($"Car: {car.name} (automation={car.automation})");
            // List all children
            foreach (Transform child in car.transform) {
                var childLights = child.GetComponentsInChildren<Light>(true);
                Debug.Log($"  Child: {child.name} (has {childLights.Length} lights)");
                foreach (var cl in childLights) {
                    Debug.Log($"    -> Light on '{cl.name}': Type={cl.type}, Intensity={cl.intensity}, Range={cl.range}, Color={cl.color}");
                }
            }
        }
    }
}
