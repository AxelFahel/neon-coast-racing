using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonCoast {
/// <summary>Replay dos últimos segundos, câmera lateral e captura da foto de chegada.</summary>
public class FinishReplay : MonoBehaviour {
    struct Frame {
        public Vector3 position;
        public Quaternion rotation;
        public Frame(Vector3 p, Quaternion r) { position = p; rotation = r; }
    }

    readonly List<Frame> frames = new List<Frame>(480);
    RaceSession race;
    Camera raceCamera;
    ChaseCamera chase;
    bool replayStarted;
    GameObject ghost;
    Renderer[] playerRenderers;
    GameObject flashQuad;
    Material flashMaterial;

    void Start() {
        race = GetComponent<RaceSession>();
        raceCamera = Camera.main;
        chase = raceCamera ? raceCamera.GetComponent<ChaseCamera>() : null;
        if (race) race.FinishPresentationComplete = false;
    }

    void FixedUpdate() {
        if (!race || !race.player || race.Finished) return;
        frames.Add(new Frame(race.player.transform.position, race.player.transform.rotation));
        if (frames.Count > 400) frames.RemoveAt(0);
    }

    void Update() {
        if (race && race.Finished && !replayStarted) {
            replayStarted = true;
            StartCoroutine(PlayFinishReplay());
        }
    }

    IEnumerator PlayFinishReplay() {
        if (!race.player || !raceCamera || frames.Count < 8) {
            race.FinishPresentationComplete = true;
            yield break;
        }

        race.player.controlsEnabled = false;
        var body = race.player.GetComponent<Rigidbody>();
        if (body) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        if (chase) chase.enabled = false;

        playerRenderers = race.player.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in playerRenderers) renderer.enabled = false;
        ghost = BuildVisualGhost(race.player);
        BuildFlash();

        Time.timeScale = .35f;
        const float replayDuration = 5.2f;
        float elapsed = 0f;
        while (elapsed < replayDuration) {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / replayDuration);
            float sample = t * (frames.Count - 1);
            int a = Mathf.Clamp(Mathf.FloorToInt(sample), 0, frames.Count - 1);
            int b = Mathf.Min(a + 1, frames.Count - 1);
            float blend = sample - a;
            ghost.transform.SetPositionAndRotation(
                Vector3.Lerp(frames[a].position, frames[b].position, blend),
                Quaternion.Slerp(frames[a].rotation, frames[b].rotation, blend));

            Transform car = ghost.transform;
            float sweep = Mathf.SmoothStep(-1f, 1f, t);
            Vector3 cameraPos = car.position + car.right * Mathf.Lerp(8f, -6f, (sweep + 1f) * .5f) - car.forward * 5f + Vector3.up * 2.3f;
            raceCamera.transform.position = cameraPos;
            raceCamera.transform.rotation = Quaternion.LookRotation((car.position + Vector3.up * .9f - cameraPos).normalized, Vector3.up);
            raceCamera.fieldOfView = Mathf.Lerp(58f, 46f, t);
            yield return null;
        }

        string photoPath = Path.Combine(Application.persistentDataPath, "NeonCoast_PhotoFinish.png");
        ScreenCapture.CaptureScreenshot(photoPath, 1);
        if (flashQuad) flashQuad.SetActive(true);
        yield return new WaitForSecondsRealtime(.085f);
        if (flashQuad) flashQuad.SetActive(false);
        yield return new WaitForSecondsRealtime(.55f);

        foreach (var renderer in playerRenderers) if (renderer) renderer.enabled = true;
        if (ghost) Destroy(ghost);
        if (flashQuad) Destroy(flashQuad);
        Time.timeScale = 1f;
        if (chase) chase.enabled = true;
        race.FinishPresentationComplete = true;
    }

    GameObject BuildVisualGhost(ArcadeCar source) {
        var result = new GameObject("Finish Replay Car");
        if (source.bodyVisual) {
            var body = Instantiate(source.bodyVisual.gameObject, result.transform);
            body.name = "Replay body";
            body.transform.localPosition = source.bodyVisual.localPosition;
            body.transform.localRotation = source.bodyVisual.localRotation;
            body.transform.localScale = source.bodyVisual.localScale;
        }
        if (source.wheelVisuals != null) {
            foreach (var wheel in source.wheelVisuals) {
                if (!wheel || (source.bodyVisual && wheel.IsChildOf(source.bodyVisual))) continue;
                var copy = Instantiate(wheel.gameObject, result.transform);
                copy.name = "Replay wheel";
                copy.transform.localPosition = source.transform.InverseTransformPoint(wheel.position);
                copy.transform.localRotation = Quaternion.Inverse(source.transform.rotation) * wheel.rotation;
                copy.transform.localScale = wheel.lossyScale;
            }
        }
        return result;
    }

    void BuildFlash() {
        if (!raceCamera) return;
        flashQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        flashQuad.name = "Photo Finish Flash";
        Destroy(flashQuad.GetComponent<Collider>());
        flashQuad.transform.SetParent(raceCamera.transform, false);
        flashQuad.transform.localPosition = new Vector3(0, 0, .32f);
        flashQuad.transform.localRotation = Quaternion.identity;
        flashQuad.transform.localScale = new Vector3(1.25f, .75f, 1f);
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        flashMaterial = new Material(shader) { name = "Photo finish white flash", renderQueue = 5000 };
        if (flashMaterial.HasProperty("_BaseColor")) flashMaterial.SetColor("_BaseColor", Color.white);
        if (flashMaterial.HasProperty("_Color")) flashMaterial.SetColor("_Color", Color.white);
        flashQuad.GetComponent<Renderer>().sharedMaterial = flashMaterial;
        flashQuad.SetActive(false);
    }

    void OnDestroy() {
        Time.timeScale = 1f;
        if (flashMaterial) Destroy(flashMaterial);
    }
}
}
