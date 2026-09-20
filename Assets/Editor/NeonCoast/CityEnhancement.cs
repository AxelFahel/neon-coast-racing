using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace NeonCoast {
public static class CityEnhancement {

    // Migrate the generated city without rebuilding authored scenery or spectators.
    [InitializeOnLoadMethod]
    static void RegisterScaleRepair() {
        EditorApplication.delayCall += RepairLoadedCity;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode) {
        RepairLoadedCity();
    }

    [MenuItem("Neon Coast/Repair City Geometry Scale")]
    public static void RepairLoadedCity() {
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || scene.path != "Assets/Scenes/NeonCoast.unity") continue;
            bool wasDirty = scene.isDirty;
            int repaired = 0;
            foreach (var root in scene.GetRootGameObjects()) {
                if (root.name != "City_Enhanced_Root") continue;
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) {
                    if (filter.name.EndsWith("_Mesh")) continue;
                    var renderer = filter.GetComponent<MeshRenderer>();
                    if (!renderer) continue;
                    var pivot = filter.transform;
                    var geometry = new GameObject(pivot.name + "_Mesh", typeof(MeshFilter), typeof(MeshRenderer));
                    Undo.RegisterCreatedObjectUndo(geometry, "Repair city scale");
                    geometry.transform.SetParent(pivot, false);
                    geometry.transform.localScale = pivot.localScale;
                    geometry.layer = pivot.gameObject.layer;
                    GameObjectUtility.SetStaticEditorFlags(geometry, GameObjectUtility.GetStaticEditorFlags(pivot.gameObject));
                    geometry.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    EditorUtility.CopySerialized(renderer, geometry.GetComponent<MeshRenderer>());
                    Undo.RecordObject(pivot, "Repair city scale");
                    pivot.localScale = Vector3.one;
                    Undo.DestroyObjectImmediate(renderer);
                    Undo.DestroyObjectImmediate(filter);
                    repaired++;
                }
            }
            if (repaired == 0) continue;
            EditorSceneManager.MarkSceneDirty(scene);
            // Never silently save other edits that were already pending.
            if (!wasDirty) EditorSceneManager.SaveScene(scene);
            Debug.Log("[CityEnhancement] Repaired " + repaired + " scaled city meshes." +
                (wasDirty ? " Save the scene to keep this repair and your pending edits." : " Scene saved."));
        }
    }

    [MenuItem("Neon Coast/Enhance City and Scenery")]
    public static void Apply() {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.path.Contains("NeonCoast")) {
            EditorSceneManager.OpenScene("Assets/Scenes/NeonCoast.unity");
        }

        var oldCity = GameObject.Find("City_Enhanced_Root");
        if (oldCity) Object.DestroyImmediate(oldCity);

        var cityRoot = new GameObject("City_Enhanced_Root");
        cityRoot.transform.position = Vector3.zero;

        var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // Materiais Urbanos
        var darkMetal = CreateMat(litShader, "City_DarkMetal", new Color(0.08f, 0.09f, 0.11f), 0.7f, 0.45f);
        var glassTower = CreateMat(litShader, "City_GlassTower", new Color(0.04f, 0.08f, 0.14f), 0.1f, 0.95f);
        var concrete = CreateMat(litShader, "City_Concrete", new Color(0.20f, 0.22f, 0.25f), 0.0f, 0.35f);
        var warmWindows = CreateMat(litShader, "City_WarmWindows", new Color(0.85f, 0.65f, 0.30f), 0.0f, 0.2f, new Color(0.95f, 0.70f, 0.35f) * 1.4f);
        var cyanNeon = CreateMat(litShader, "City_CyanNeon", new Color(0.1f, 0.9f, 1f), 0.0f, 0.5f, new Color(0.1f, 0.9f, 1f) * 2.5f);
        var pinkNeon = CreateMat(litShader, "City_PinkNeon", new Color(1f, 0.12f, 0.45f), 0.0f, 0.5f, new Color(1f, 0.12f, 0.45f) * 2.5f);
        var amberNeon = CreateMat(litShader, "City_AmberNeon", new Color(1f, 0.70f, 0.05f), 0.0f, 0.5f, new Color(1f, 0.70f, 0.05f) * 2.2f);

        var rng = new System.Random(88);

        // ── 1. SKYLINE E ARRANHA-CÉUS COMPLEXOS ─────────────────────────────────
        for (int i = 0; i < 48; i++) {
            float x = -60f + (float)rng.NextDouble() * 140f;
            float z = -140f + (float)rng.NextDouble() * 240f;

            // Evita colocar prédios no meio da pista de corrida
            if (x > -35f && x < 25f && z > -110f && z < 50f) continue;

            float h = 28f + (float)rng.NextDouble() * 85f;
            float w = 12f + (float)rng.NextDouble() * 14f;
            float d = 12f + (float)rng.NextDouble() * 16f;

            Vector3 basePos = new Vector3(x, h * 0.5f - 1f, z);

            // Bloco principal da torre
            var tower = Box("Skyscraper_" + i, cityRoot.transform, basePos, new Vector3(w, h, d), i % 3 == 0 ? glassTower : darkMetal);

            // Setback / Bloco de cobertura superior escalonado
            if (h > 45f) {
                float topH = 8f + (float)rng.NextDouble() * 14f;
                float topW = w * 0.65f;
                float topD = d * 0.65f;
                Box("Tower_Top_" + i, tower.transform, new Vector3(0f, (h + topH) * 0.5f, 0f), new Vector3(topW, topH, topD), concrete);

                // Antena de transmissão com luz de sinalização vermelha no topo
                var antenna = Box("Antenna_" + i, tower.transform, new Vector3(0f, h * 0.5f + topH + 5f, 0f), new Vector3(0.25f, 10f, 0.25f), darkMetal);
                var blinkLight = Box("BeaconLight", antenna.transform, new Vector3(0f, 5.1f, 0f), new Vector3(0.45f, 0.45f, 0.45f), pinkNeon);
            }

            // Friso neon de topo do prédio
            Material rimNeon = (i % 2 == 0) ? cyanNeon : pinkNeon;
            Box("Rooftop_Neon_" + i, tower.transform, new Vector3(0f, h * 0.5f + 0.05f, 0f), new Vector3(w + 0.3f, 0.25f, d + 0.3f), rimNeon);

            // Faixas iluminadas de escritórios por andar
            for (float fl = 6f; fl < h - 4f; fl += 6.5f) {
                for (int s = -1; s <= 1; s += 2) {
                    Box("OfficeBand_Z_" + i + "_" + fl, tower.transform, new Vector3(0f, fl - h * 0.5f, s * (d * 0.5f + 0.02f)), new Vector3(w * 0.88f, 1.2f, 0.04f), (i + (int)fl) % 4 == 0 ? cyanNeon : warmWindows);
                }
            }

            // Outdoors e telões neon comerciais nas torres próximas da visão da pista
            if (i % 4 == 0) {
                var billboard = Box("Billboard_" + i, tower.transform, new Vector3(0f, h * 0.25f, d * 0.5f + 0.25f), new Vector3(w * 0.75f, 4.5f, 0.35f), darkMetal);
                var adScreen = Box("Ad_Screen", billboard.transform, new Vector3(0f, 0f, 0.20f), new Vector3(w * 0.72f, 4.2f, 0.05f), (i % 8 == 0) ? cyanNeon : (i % 8 == 4) ? pinkNeon : amberNeon);
            }
        }

        // ── 2. PÓRTICOS DE CORRIDA ILUMINADOS (RACE GANTRIES) ───────────────────
        for (int g = 0; g < 4; g++) {
            float t = 0.08f + g * 0.24f;
            Vector3 p = TrackSpectators.GetTrackPoint(t);
            Vector3 fwd = TrackSpectators.GetTrackForward(t);
            Vector3 right = TrackSpectators.GetTrackRight(t);

            var gantryGO = new GameObject("Overhead_Gantry_" + g);
            gantryGO.transform.SetParent(cityRoot.transform, false);
            gantryGO.transform.position = p + Vector3.up * 6.8f;
            gantryGO.transform.rotation = Quaternion.LookRotation(fwd);

            // Estrutura em treliça sobre a pista
            Box("Truss_Beam", gantryGO.transform, Vector3.zero, new Vector3(20f, 0.6f, 0.8f), darkMetal);
            Box("Truss_Pillar_L", gantryGO.transform, new Vector3(-9.6f, -3.4f, 0f), new Vector3(0.5f, 7.2f, 0.5f), darkMetal);
            Box("Truss_Pillar_R", gantryGO.transform, new Vector3( 9.6f, -3.4f, 0f), new Vector3(0.5f, 7.2f, 0.5f), darkMetal);

            // Telão LED do pórtico
            Box("Gantry_Display", gantryGO.transform, new Vector3(0f, 0f, -0.42f), new Vector3(14f, 2.2f, 0.15f), cyanNeon);

            // Refletores de pista apontados para o asfalto
            for (int rfl = -3; rfl <= 3; rfl += 2) {
                var spotGO = new GameObject("Gantry_Spot_" + rfl);
                spotGO.transform.SetParent(gantryGO.transform, false);
                spotGO.transform.localPosition = new Vector3(rfl * 2.2f, -0.4f, 0f);
                spotGO.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
                var spot = spotGO.AddComponent<Light>();
                spot.type = LightType.Spot;
                spot.range = 22f;
                spot.spotAngle = 65f;
                spot.intensity = 18f;
                spot.color = new Color(0.85f, 0.92f, 1f);
                spot.shadows = LightShadows.None;
            }
        }

        // ── 3. QUIOSQUES, RESTAURANTES E PALMEIRAS NO CALÇADÃO ───────────────────
        for (int k = 0; k < 14; k++) {
            float z = -175f + k * 26f;
            Vector3 kPos = new Vector3(-132f, 0f, z);

            // Quiosque à beira-mar com telhado neon
            var kiosk = Box("Beach_Kiosk_" + k, cityRoot.transform, kPos + Vector3.up * 1.5f, new Vector3(5f, 3f, 5f), concrete);
            Box("Kiosk_Roof", kiosk.transform, new Vector3(0f, 1.6f, 0f), new Vector3(5.8f, 0.35f, 5.8f), (k % 2 == 0) ? cyanNeon : pinkNeon);

            // Iluminação interna acolhedora do bar
            var barLightGO = new GameObject("Kiosk_Light_" + k);
            barLightGO.transform.SetParent(kiosk.transform, false);
            barLightGO.transform.localPosition = new Vector3(0f, 0f, 0f);
            var barLight = barLightGO.AddComponent<Light>();
            barLight.type = LightType.Point;
            barLight.range = 7f;
            barLight.intensity = 8f;
            barLight.color = new Color(1f, 0.72f, 0.32f);
            barLight.shadows = LightShadows.None;
        }

        // ── 4. ATUALIZAÇÃO DOS ESPECTADORES ─────────────────────────────────────
        var spectators = Object.FindFirstObjectByType<TrackSpectators>();
        if (!spectators) {
            var specGO = new GameObject("Track Spectators");
            spectators = specGO.AddComponent<TrackSpectators>();
        }
        spectators.SpawnAllSpectators();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CityEnhancement] City, Skyline, Gantries, and Promenade successfully upgraded!");
    }

    static Material CreateMat(Shader shader, string name, Color color, float metallic, float smooth, Color emission = default) {
        var m = new Material(shader) { name = name };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (emission.maxColorComponent > 0) {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission);
        }
        m.enableInstancing = true;
        return m;
    }

    static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat) {
        // Keep the assembly pivot at unit scale: child dimensions are in metres.
        var pivot = new GameObject(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPos;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name + "_Mesh";
        go.transform.SetParent(pivot.transform, false);
        go.transform.localScale = scale;
        if (Application.isPlaying) Object.Destroy(go.GetComponent<Collider>());
        else Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return pivot;
    }
}
}
