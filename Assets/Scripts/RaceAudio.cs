using UnityEngine;
using UnityEngine.Audio;
namespace NeonCoast {
public class RaceAudio : MonoBehaviour {
 public RaceSession race;
 AudioSource beepSrc;
 AudioClip beepClip, goClip, finishClip;
 int lastBeep=4;
 bool finishPlayed;

 void Awake(){
  if(!race) race=FindFirstObjectByType<RaceSession>();
  var mixer=Resources.Load<AudioMixer>("NeonCoastMixer");
  var groups=mixer?mixer.FindMatchingGroups("UI"):null;
  beepSrc=gameObject.AddComponent<AudioSource>();beepSrc.playOnAwake=false;beepSrc.spatialBlend=0;beepSrc.volume=.4f;beepSrc.outputAudioMixerGroup=groups!=null&&groups.Length>0?groups[0]:null;
  beepClip=SynthTone(880,.12f,.45f);   // A5 — countdown tick
  goClip=SynthTone(1760,.25f,.5f);     // A6 — GO!
  finishClip=SynthFanfare();            // finish jingle
 }

 void Update(){
  if(!race)return;
  // Countdown beeps: 3, 2, 1
  if(race.Countdown>0){
   int sec=Mathf.CeilToInt(race.Countdown);
   if(sec<lastBeep&&sec>0&&sec<=3){beepSrc.PlayOneShot(beepClip,.4f);lastBeep=sec;}
  }
  // GO!
  else if(lastBeep>0){beepSrc.PlayOneShot(goClip,.5f);lastBeep=0;}
  // Finish fanfare
  if(race.Finished&&!finishPlayed){beepSrc.PlayOneShot(finishClip,.45f);finishPlayed=true;}
 }

 static AudioClip SynthTone(float freq,float duration,float vol){
  const int rate=48000;int n=(int)(rate*duration);var d=new float[n];
  for(int i=0;i<n;i++){float t=(float)i/rate;float env=Mathf.Clamp01(1-t/duration)*Mathf.Clamp01(t*120);d[i]=Mathf.Sin(2*Mathf.PI*freq*t)*env*vol;}
  var c=AudioClip.Create("Tone_"+freq,n,1,rate,false);c.SetData(d,0);return c;
 }

 static AudioClip SynthFanfare(){
  const int rate=48000;float dur=.9f;int n=(int)(rate*dur);var d=new float[n];
  // Three ascending notes: C6 E6 G6 played sequentially
  float[] freqs={1047,1319,1568};float noteLen=dur/3;
  for(int i=0;i<n;i++){float t=(float)i/rate;int note=Mathf.Min(2,Mathf.FloorToInt(t/noteLen));float nt=t-note*noteLen;
   float env=Mathf.Clamp01(1-nt/noteLen)*Mathf.Clamp01(nt*60);
   d[i]=(Mathf.Sin(2*Mathf.PI*freqs[note]*t)*.35f+Mathf.Sin(2*Mathf.PI*freqs[note]*2*t)*.12f)*env;}
  var c=AudioClip.Create("Finish fanfare",n,1,rate,false);c.SetData(d,0);return c;
 }

 void OnDestroy(){if(beepClip)Destroy(beepClip);if(goClip)Destroy(goClip);if(finishClip)Destroy(finishClip);}
}
}
