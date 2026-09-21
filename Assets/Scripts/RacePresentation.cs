using UnityEngine;

namespace NeonCoast {
/// <summary>Largada com semáforo físico e travelling cinematográfico curto.</summary>
public class RacePresentation : MonoBehaviour {
    RaceSession race;
    Camera raceCamera;
    ChaseCamera chase;
    Renderer[] bulbs;
    Material bulbMaterial;
    bool released;
    float greenTimer;

    void Start() {
        race = GetComponent<RaceSession>();
        raceCamera = Camera.main;
        chase = raceCamera ? raceCamera.GetComponent<ChaseCamera>() : null;
        BuildStartLights();
    }

    void BuildStartLights() {
        if (!race || race.checkpoints == null || race.checkpoints.Length == 0) return;
        Transform line = race.checkpoints[race.checkpoints.Length - 1];
        if (!line) return;

        var rig = new GameObject("Race Start Lights");
        rig.transform.SetParent(transform, false);
        rig.transform.SetPositionAndRotation(line.position + Vector3.up * 6.15f - line.forward * .35f, line.rotation);

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        bulbMaterial = new Material(shader) { name = "Runtime start bulb" };
        if (bulbMaterial.HasProperty("_BaseColor")) bulbMaterial.SetColor("_BaseColor", new Color(.025f, .025f, .03f));
        bulbMaterial.EnableKeyword("_EMISSION");
        if (bulbMaterial.HasProperty("_EmissionColor")) bulbMaterial.SetColor("_EmissionColor", Color.black);
        bulbs = new Renderer[5];
        for (int i = 0; i < bulbs.Length; i++) {
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Start bulb " + (i + 1);
            bulb.transform.SetParent(rig.transform, false);
            bulb.transform.localPosition = new Vector3((i - 2) * 1.05f, 0, 0);
            bulb.transform.localScale = Vector3.one * .62f;
            Destroy(bulb.GetComponent<Collider>());
            bulbs[i] = bulb.GetComponent<Renderer>();
            bulbs[i].sharedMaterial = bulbMaterial;
        }
    }

    void LateUpdate() {
        if (!race) return;
        UpdateLights();

        if (race.Countdown > 0f && raceCamera && race.player) {
            if (chase) chase.enabled = false;
            float t = Mathf.Clamp01(1f - race.Countdown / 3.5f);
            float eased = t * t * (3f - 2f * t);
            Transform car = race.player.transform;
            Vector3 establishing = car.position + car.right * 8.5f + car.forward * 3.5f + Vector3.up * 2.2f;
            Vector3 gridView = car.position - car.forward * 8.4f + Vector3.up * 2.65f;
            raceCamera.transform.position = Vector3.Lerp(establishing, gridView, eased);
            Vector3 focus = car.position + Vector3.up * 1.05f + car.forward * Mathf.Lerp(0f, 4f, eased);
            raceCamera.transform.rotation = Quaternion.LookRotation((focus - raceCamera.transform.position).normalized, Vector3.up);
            raceCamera.fieldOfView = Mathf.Lerp(48f, 64f, eased);
        } else if (!released) {
            released = true;
            greenTimer = 1.1f;
            if (chase) chase.enabled = true;
        }
    }

    void UpdateLights() {
        if (bulbs == null) return;
        if (race.Countdown > 0f) {
            int lit = Mathf.Clamp(Mathf.FloorToInt((3.5f - race.Countdown) / .58f) + 1, 0, bulbs.Length);
            for (int i = 0; i < bulbs.Length; i++) SetBulb(bulbs[i], i < lit ? new Color(1f, .025f, .015f) : new Color(.025f, .025f, .03f), i < lit);
        } else if (greenTimer > 0f) {
            greenTimer -= Time.unscaledDeltaTime;
            for (int i = 0; i < bulbs.Length; i++) SetBulb(bulbs[i], new Color(.02f, 1f, .22f), true);
        } else {
            for (int i = 0; i < bulbs.Length; i++) SetBulb(bulbs[i], new Color(.025f, .025f, .03f), false);
        }
    }

    static void SetBulb(Renderer renderer, Color color, bool emissive) {
        if (!renderer) return;
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        block.SetColor("_EmissionColor", emissive ? color * 5f : Color.black);
        renderer.SetPropertyBlock(block);
    }

    void OnDestroy() { if (bulbMaterial) Destroy(bulbMaterial); }
}
}
