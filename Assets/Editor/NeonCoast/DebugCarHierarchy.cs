using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;

public static class DebugCarHierarchy {
    [MenuItem("Neon Coast/Debug Car Hierarchy")]
    public static void Dump() {
        var sb = new StringBuilder();
        sb.AppendLine("=== STARTING CAR HIERARCHY DUMP ===");
        EditorSceneManager.OpenScene("Assets/Scenes/NeonCoast.unity");

        var cars = Object.FindObjectsByType<NeonCoast.ArcadeCar>(FindObjectsSortMode.None);
        sb.AppendLine($"Total ArcadeCars found: {cars.Length}");

        foreach (var car in cars) {
            sb.AppendLine($"\n==========================================");
            sb.AppendLine($"CAR: '{car.name}' (Active={car.gameObject.activeSelf}, Pos={car.transform.position})");
            DumpTransform(car.transform, "", sb);
        }

        File.WriteAllText(@"C:\Users\Aksel\car_hierarchy_dump.txt", sb.ToString());
        Debug.Log("[DebugCarHierarchy] Dump written to C:\\Users\\Aksel\\car_hierarchy_dump.txt");
    }

    static void DumpTransform(Transform t, string indent, StringBuilder sb) {
        sb.AppendLine($"{indent}+-- '{t.name}' (Pos={t.localPosition}, Rot={t.localEulerAngles}, Layer={t.gameObject.layer}, Active={t.gameObject.activeSelf})");

        var comps = t.GetComponents<Component>();
        foreach (var c in comps) {
            if (c == null || c is Transform) continue;
            if (c is Light l) {
                sb.AppendLine($"{indent}    [LIGHT] Type={l.type}, Intensity={l.intensity}, Range={l.range}, Color={l.color}, SpotAngle={l.spotAngle}, Shadows={l.shadows}");
            } else if (c is MeshRenderer mr) {
                string matInfo = "";
                if (mr.sharedMaterials != null) {
                    foreach (var m in mr.sharedMaterials) {
                        if (m == null) { matInfo += "null; "; continue; }
                        string em = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor").ToString() : "no-em";
                        string bc = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : (m.HasProperty("_Color") ? m.GetColor("_Color").ToString() : "no-col");
                        matInfo += $"{m.name}(Shader={m.shader.name}, Base={bc}, Em={em}); ";
                    }
                }
                sb.AppendLine($"{indent}    [MESH_RENDERER] Mats={matInfo}");
            } else if (c is ParticleSystem ps) {
                sb.AppendLine($"{indent}    [PARTICLE_SYSTEM] Playing={ps.isPlaying}");
            } else {
                sb.AppendLine($"{indent}    [{c.GetType().Name}]");
            }
        }

        foreach (Transform child in t) {
            DumpTransform(child, indent + "  ", sb);
        }
    }
}
