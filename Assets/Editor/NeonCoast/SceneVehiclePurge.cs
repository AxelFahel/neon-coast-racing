using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace NeonCoast {
[InitializeOnLoad]
public static class SceneVehiclePurge {
    [InitializeOnLoadMethod]
    static void AutoRun() {
        EditorApplication.delayCall += PurgeAndSave;
    }

    [MenuItem("Neon Coast/Purge Excess Lights and Fix Tail LEDs")]
    public static void PurgeAndSave() {
        if (EditorApplication.isPlaying) return;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "NeonCoast") {
            if (File.Exists("Assets/Scenes/NeonCoast.unity")) {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/NeonCoast.unity", OpenSceneMode.Single);
            }
        }

        Debug.Log("[SceneVehiclePurge] Iniciando limpeza de luzes espúrias e clarões em todos os veículos...");

        // 1. Destrói luzes de preenchimento soltas na cena que causavam clarão no carro
        var allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        int destroyedLights = 0;
        foreach (var l in allLights) {
            if (!l) continue;
            string n = l.name;
            if (n == "Paint rim light" || n == "Projector headlight" || n.Contains("Rim light") || n.Contains("Flare")) {
                Object.DestroyImmediate(l.gameObject);
                destroyedLights++;
            }
        }

        // 2. Itera em todos os ArcadeCars na cena
        var cars = Object.FindObjectsByType<ArcadeCar>(FindObjectsSortMode.None);
        foreach (var car in cars) {
            if (!car) continue;

            // Remove GameObjects antigos em toda a hierarquia
            var oldNames = new HashSet<string> {
                "Paint rim light", "Projector headlight", "Car_TailLight_L", "Car_TailLight_R",
                "Car_RoofFill"
            };

            var toRemove = new List<GameObject>();
            foreach (var t in car.GetComponentsInChildren<Transform>(true)) {
                if (!t || t == car.transform) continue;
                if (oldNames.Contains(t.name)) {
                    toRemove.Add(t.gameObject);
                }
            }

            // Remove luzes que não sejam faróis dianteiros ou underglow
            foreach (var l in car.GetComponentsInChildren<Light>(true)) {
                if (!l) continue;
                if (l.name != "Car_Headlight_L" && l.name != "Car_Headlight_R" && l.name != "Car_Underglow") {
                    toRemove.Add(l.gameObject);
                }
            }

            foreach (var g in toRemove) {
                if (g) Object.DestroyImmediate(g);
            }

            // Se for o jogador ou rival, reconstrói o modelo moderno sem clarão
            if (!car.automation) {
                var v = VehicleRegistry.GetSelectedVehicle();
                var p = VehicleRegistry.GetSelectedPaint();
                CarVisualsOverhaul.RebuildCarVisuals(car, v, p);
            }

            EditorUtility.SetDirty(car.gameObject);
        }

        // 3. Ajusta o material Coral neon para não ter emissão absurda que queime o bloom
        var coralMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Coral neon.mat");
        if (coralMat != null) {
            coralMat.SetColor("_EmissionColor", new Color(0.9f, 0.08f, 0.25f) * 0.9f);
            EditorUtility.SetDirty(coralMat);
        }

        // 4. Salva alterações no disco
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SceneVehiclePurge] Concluído com sucesso! {destroyedLights} luzes espúrias removidas e lanternas traseiras calibradas para vermelho rubi puro.");
    }
}
}
