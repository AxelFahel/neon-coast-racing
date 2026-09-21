using UnityEngine;
using UnityEngine.Audio;

namespace NeonCoast {
/// <summary>Camadas procedurais de cidade, costa e público, sem arquivos de áudio externos.</summary>
public class RaceAmbience : MonoBehaviour {
    RaceSession race;
    AudioSource city, coast, crowd;
    AudioClip cityClip, coastClip, crowdClip;

    void Start() {
        race = GetComponent<RaceSession>();
        var mixer = Resources.Load<AudioMixer>("NeonCoastMixer");
        AudioMixerGroup ambience = null;
        if (mixer) {
            var groups = mixer.FindMatchingGroups("Ambience");
            if (groups.Length > 0) ambience = groups[0];
        }

        cityClip = BuildCity();
        coastClip = BuildCoast();
        crowdClip = BuildCrowd();
        city = Source("City ambience", cityClip, .055f, ambience);
        coast = Source("Coastal ambience", coastClip, .045f, ambience);
        crowd = Source("Crowd ambience", crowdClip, .10f, ambience);
    }

    AudioSource Source(string sourceName, AudioClip clip, float volume, AudioMixerGroup group) {
        var sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        var source = sourceObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.outputAudioMixerGroup = group;
        source.Play();
        return source;
    }

    void Update() {
        if (!race || !crowd) return;
        float speed = race.player ? Mathf.Clamp01(race.player.SpeedKmh / 180f) : 0f;
        float countdownEnergy = race.Countdown > 0f ? Mathf.Clamp01(1f - race.Countdown / 3.5f) : 0f;
        float targetCrowd = race.Finished ? .24f : Mathf.Lerp(.075f, .14f, Mathf.Max(speed, countdownEnergy));
        crowd.volume = Mathf.MoveTowards(crowd.volume, targetCrowd, Time.unscaledDeltaTime * .10f);
        city.volume = Mathf.Lerp(.065f, .035f, speed);
        coast.volume = Mathf.Lerp(.035f, .06f, speed);
    }

    static AudioClip BuildCity() {
        const int rate = 24000, seconds = 8;
        int count = rate * seconds;
        var data = new float[count];
        var random = new System.Random(812);
        float filtered = 0f;
        for (int i = 0; i < count; i++) {
            float t = (float)i / rate;
            float noise = (float)random.NextDouble() * 2f - 1f;
            filtered = Mathf.Lerp(filtered, noise, .025f);
            float hum = Mathf.Sin(t * Mathf.PI * 2f * 54f) * .06f + Mathf.Sin(t * Mathf.PI * 2f * 91f) * .025f;
            float distantSiren = Mathf.Sin(t * Mathf.PI * 2f * (420f + Mathf.Sin(t * .55f) * 85f)) * .018f;
            data[i] = filtered * .09f + hum + distantSiren;
        }
        var clip = AudioClip.Create("Procedural city night", count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip BuildCoast() {
        const int rate = 24000, seconds = 8;
        int count = rate * seconds;
        var data = new float[count];
        var random = new System.Random(119);
        float low = 0f;
        for (int i = 0; i < count; i++) {
            float t = (float)i / rate;
            low = Mathf.Lerp(low, (float)random.NextDouble() * 2f - 1f, .008f);
            float wave = .45f + .55f * Mathf.Sin(t * Mathf.PI * .52f);
            data[i] = low * wave * .16f;
        }
        var clip = AudioClip.Create("Procedural coastal waves", count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip BuildCrowd() {
        const int rate = 24000, seconds = 8;
        int count = rate * seconds;
        var data = new float[count];
        var random = new System.Random(431);
        float murmur = 0f;
        for (int i = 0; i < count; i++) {
            float t = (float)i / rate;
            float noise = (float)random.NextDouble() * 2f - 1f;
            murmur = Mathf.Lerp(murmur, noise, .06f);
            float voices = Mathf.Sin(t * 31f) * Mathf.Sin(t * 2.7f) + Mathf.Sin(t * 47f + 1.3f) * .6f;
            data[i] = murmur * .11f + voices * .018f;
        }
        var clip = AudioClip.Create("Procedural crowd", count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void OnDestroy() {
        if (cityClip) Destroy(cityClip);
        if (coastClip) Destroy(coastClip);
        if (crowdClip) Destroy(crowdClip);
    }
}
}
