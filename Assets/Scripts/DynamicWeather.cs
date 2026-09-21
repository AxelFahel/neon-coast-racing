using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

namespace NeonCoast {
/// <summary>Ciclo seco/garoa/chuva com pista molhada e reflexo local atualizado.</summary>
public class DynamicWeather : MonoBehaviour {
    RaceSession race;
    ParticleSystem rain;
    Material rainMaterial;
    AudioSource rainAudio, thunderAudio;
    AudioClip rainClip, thunderClip;
    Material wetRoad;
    Color dryRoadColor;
    float drySmoothness;
    ReflectionProbe reflectionProbe;
    float rainAmount;
    float nextProbeRefresh;
    float nextThunder;
    float baseFogDensity;
    Color baseFogColor;

    void Start() {
        race = GetComponent<RaceSession>();
        baseFogDensity = RenderSettings.fogDensity;
        baseFogColor = RenderSettings.fogColor;
        PrepareWetRoad();
        BuildRain();
        BuildWeatherAudio();
        BuildReflectionProbe();
    }

    void PrepareWetRoad() {
        foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) {
            var source = renderer.sharedMaterial;
            if (!source || !source.name.StartsWith("Wet graphite asphalt")) continue;
            if (!wetRoad) {
                wetRoad = new Material(source) { name = "Runtime wet asphalt" };
                dryRoadColor = wetRoad.HasProperty("_BaseColor") ? wetRoad.GetColor("_BaseColor") : Color.gray;
                drySmoothness = wetRoad.HasProperty("_Smoothness") ? wetRoad.GetFloat("_Smoothness") : .72f;
            }
            renderer.sharedMaterial = wetRoad;
        }
    }

    void BuildRain() {
        var rainObject = new GameObject("Dynamic Rain");
        rainObject.transform.SetParent(transform, false);
        rain = rainObject.AddComponent<ParticleSystem>();
        rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = rain.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.7f, 1.25f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(.018f, .04f);
        main.maxParticles = 2400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = rain.emission;
        emission.rateOverTime = 0f;
        var shape = rain.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(42f, 1f, 42f);
        var velocity = rain.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(-30f, -22f);
        velocity.x = new ParticleSystem.MinMaxCurve(-2.5f, -.5f);

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
        rainMaterial = new Material(shader) { name = "Runtime rain streak" };
        Color rainColor = new Color(.48f, .72f, .92f, .42f);
        if (rainMaterial.HasProperty("_BaseColor")) rainMaterial.SetColor("_BaseColor", rainColor);
        if (rainMaterial.HasProperty("_Color")) rainMaterial.SetColor("_Color", rainColor);
        if (rainMaterial.HasProperty("_Surface")) rainMaterial.SetFloat("_Surface", 1f);
        if (rainMaterial.HasProperty("_ZWrite")) rainMaterial.SetFloat("_ZWrite", 0f);
        rainMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        rainMaterial.renderQueue = 3000;
        var particleRenderer = rain.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = rainMaterial;
        particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        particleRenderer.velocityScale = .04f;
        particleRenderer.lengthScale = 2.8f;
        rain.Play();
    }

    void BuildReflectionProbe() {
        var probeObject = new GameObject("Wet Road Reflection Probe");
        probeObject.transform.SetParent(transform, false);
        reflectionProbe = probeObject.AddComponent<ReflectionProbe>();
        reflectionProbe.mode = ReflectionProbeMode.Realtime;
        reflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        reflectionProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        reflectionProbe.resolution = 128;
        reflectionProbe.hdr = true;
        reflectionProbe.boxProjection = true;
        reflectionProbe.size = new Vector3(90f, 34f, 90f);
        reflectionProbe.nearClipPlane = .3f;
        reflectionProbe.farClipPlane = 120f;
        reflectionProbe.importance = 2;
    }

    void BuildWeatherAudio() {
        AudioMixerGroup ambience = null;
        var mixer = Resources.Load<AudioMixer>("NeonCoastMixer");
        if (mixer) {
            var groups = mixer.FindMatchingGroups("Ambience");
            if (groups.Length > 0) ambience = groups[0];
        }

        rainClip = BuildRainNoise();
        thunderClip = BuildThunder();
        rainAudio = gameObject.AddComponent<AudioSource>();
        rainAudio.clip = rainClip;
        rainAudio.loop = true;
        rainAudio.spatialBlend = 0f;
        rainAudio.volume = 0f;
        rainAudio.outputAudioMixerGroup = ambience;
        rainAudio.Play();
        thunderAudio = gameObject.AddComponent<AudioSource>();
        thunderAudio.spatialBlend = 0f;
        thunderAudio.volume = .22f;
        thunderAudio.outputAudioMixerGroup = ambience;
        nextThunder = 18f;
    }

    static AudioClip BuildRainNoise() {
        const int rate = 24000, seconds = 6;
        int count = rate * seconds;
        var data = new float[count];
        var random = new System.Random(982);
        float hiss = 0f;
        for (int i = 0; i < count; i++) {
            float noise = (float)random.NextDouble() * 2f - 1f;
            hiss = Mathf.Lerp(hiss, noise, .28f);
            data[i] = hiss * .20f + noise * .035f;
        }
        var clip = AudioClip.Create("Procedural rain", count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip BuildThunder() {
        const int rate = 24000;
        int count = rate * 3;
        var data = new float[count];
        var random = new System.Random(1701);
        float low = 0f;
        for (int i = 0; i < count; i++) {
            float t = (float)i / rate;
            float noise = (float)random.NextDouble() * 2f - 1f;
            low = Mathf.Lerp(low, noise, .006f);
            float attack = Mathf.Clamp01(t * 22f);
            float decay = Mathf.Exp(-t * 1.35f);
            data[i] = (low * .78f + Mathf.Sin(t * Mathf.PI * 2f * 43f) * .18f) * attack * decay;
        }
        var clip = AudioClip.Create("Procedural thunder", count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void Update() {
        if (!race || !race.player) return;
        float phase = Mathf.Repeat(Time.time / 110f, 1f);
        float target;
        if (phase < .22f) target = 0f;
        else if (phase < .40f) target = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.22f, .40f, phase));
        else if (phase < .72f) target = 1f;
        else if (phase < .90f) target = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(.72f, .90f, phase));
        else target = 0f;
        rainAmount = Mathf.MoveTowards(rainAmount, target, Time.deltaTime * .16f);
        if (rainAudio) rainAudio.volume = Mathf.Lerp(0f, .16f, rainAmount);
        if (rainAmount > .72f && Time.time >= nextThunder && thunderAudio && !thunderAudio.isPlaying) {
            thunderAudio.PlayOneShot(thunderClip);
            nextThunder = Time.time + Random.Range(18f, 34f);
        }

        Vector3 focus = race.player.transform.position;
        if (rain) {
            rain.transform.position = focus + Vector3.up * 18f;
            var emission = rain.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 1150f, rainAmount);
        }

        if (wetRoad) {
            if (wetRoad.HasProperty("_BaseColor")) wetRoad.SetColor("_BaseColor", Color.Lerp(dryRoadColor, dryRoadColor * .52f, rainAmount));
            if (wetRoad.HasProperty("_Smoothness")) wetRoad.SetFloat("_Smoothness", Mathf.Lerp(drySmoothness, .97f, rainAmount));
            if (wetRoad.HasProperty("_Metallic")) wetRoad.SetFloat("_Metallic", Mathf.Lerp(.20f, .42f, rainAmount));
        }

        RenderSettings.fogDensity = Mathf.Lerp(baseFogDensity, Mathf.Max(baseFogDensity, .0062f), rainAmount);
        RenderSettings.fogColor = Color.Lerp(baseFogColor, new Color(.055f, .075f, .11f), rainAmount);

        if (reflectionProbe) {
            reflectionProbe.transform.position = focus + Vector3.up * 5f;
            reflectionProbe.intensity = Mathf.Lerp(.65f, 1.45f, rainAmount);
            if (rainAmount > .2f && Time.unscaledTime >= nextProbeRefresh) {
                nextProbeRefresh = Time.unscaledTime + 5f;
                reflectionProbe.RenderProbe();
            }
        }
    }

    void OnDestroy() {
        RenderSettings.fogDensity = baseFogDensity;
        RenderSettings.fogColor = baseFogColor;
        if (rainMaterial) Destroy(rainMaterial);
        if (wetRoad) Destroy(wetRoad);
        if (rainClip) Destroy(rainClip);
        if (thunderClip) Destroy(thunderClip);
    }
}
}
