using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using System.Collections;
namespace NeonCoast {
[RequireComponent(typeof(ArcadeCar))]
public class CarEffects : MonoBehaviour {
 ArcadeCar car; bool isPlayer;
 AudioSource engine, screech, wind, nitroSfx, impactSrc;
 AudioClip engineClip, screechClip, windClip, nitroClip, impactClip;
 ParticleSystem[] exhaust; ParticleSystem sparkSystem;
 Coroutine rumbleRoutine;
 TrailRenderer[] exhaustTrails;
 Light[] exhaustLights;
 static Texture2D softGlowTex;
 static Material nitroMat;

 static Material GetNitroMaterial() {
  if (nitroMat != null) return nitroMat;
  int res = 64;
  softGlowTex = new Texture2D(res, res, TextureFormat.RGBA32, false);
  softGlowTex.name = "SoftGlowTexture";
  softGlowTex.filterMode = FilterMode.Bilinear;
  softGlowTex.wrapMode = TextureWrapMode.Clamp;
  Color[] cols = new Color[res * res];
  float center = (res - 1) * 0.5f;
  for (int y = 0; y < res; y++) {
   for (int x = 0; x < res; x++) {
    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
    float alpha = Mathf.Clamp01(1f - dist);
    alpha = alpha * alpha * (3f - 2f * alpha); // Smoothstep circular falloff
    cols[y * res + x] = new Color(1f, 1f, 1f, alpha);
   }
  }
  softGlowTex.SetPixels(cols);
  softGlowTex.Apply();

  var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Particles/Standard Unlit")
            ?? Shader.Find("Mobile/Particles/Additive")
            ?? Shader.Find("Unlit/Transparent");
  nitroMat = new Material(shader);
  nitroMat.name = "NitroGlowPlasma";
  if (nitroMat.HasProperty("_BaseMap")) nitroMat.SetTexture("_BaseMap", softGlowTex);
  if (nitroMat.HasProperty("_MainTex")) nitroMat.SetTexture("_MainTex", softGlowTex);
  if (nitroMat.HasProperty("_BaseColor")) nitroMat.SetColor("_BaseColor", Color.white);
  if (nitroMat.HasProperty("_Color")) nitroMat.SetColor("_Color", Color.white);
  nitroMat.SetFloat("_Surface", 1); // Transparent
  nitroMat.SetFloat("_Blend", 1);   // Additive
  nitroMat.renderQueue = 3100;
  return nitroMat;
 }

 void Start(){
  car=GetComponent<ArcadeCar>(); isPlayer=!car.automation;
  var mixer=Resources.Load<AudioMixer>("NeonCoastMixer");
  var engineGroup=Group(mixer,"Engine");
  var tiresGroup=Group(mixer,"Tires");
  var windGroup=Group(mixer,"Wind");
  var sfxGroup=Group(mixer,"SFX");

  // Engine — all cars
  engine=Src(.075f,true,.65f,4,65,engineGroup);
  engineClip=SynthEngine(); engine.clip=engineClip; engine.Play();

  var pMat = GetNitroMaterial();

  // Player-only audio + VFX
  if(isPlayer){
   screech=Src(0,true,.7f,3,35,tiresGroup); screechClip=SynthBandNoise(8192,3200,600,77); screech.clip=screechClip; screech.Play();
   wind=Src(0,true,.35f,5,80,windGroup); windClip=SynthBandNoise(8192,350,250,33); wind.clip=windClip; wind.Play();
   nitroSfx=Src(0,true,.5f,3,45,sfxGroup); nitroClip=SynthNitro(); nitroSfx.clip=nitroClip; nitroSfx.Play();
   impactSrc=Src(0,false,.85f,2,30,sfxGroup); impactClip=SynthImpact(); impactSrc.clip=impactClip;

   // Collision sparks particle system (faíscas esticadas, sem blocos)
   var sparkGO=new GameObject("Collision Sparks");sparkGO.transform.SetParent(transform,false);
   sparkSystem=sparkGO.AddComponent<ParticleSystem>();sparkSystem.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var sm=sparkSystem.main;sm.playOnAwake=false;sm.startLifetime=new ParticleSystem.MinMaxCurve(.08f,.35f);sm.startSpeed=new ParticleSystem.MinMaxCurve(6,16);
   sm.startSize=new ParticleSystem.MinMaxCurve(.04f,.10f);sm.startColor=new Color(1f,.85f,.35f);sm.gravityModifier=3.5f;sm.maxParticles=80;sm.simulationSpace=ParticleSystemSimulationSpace.World;
   var se=sparkSystem.emission;se.enabled=false;
   var ss=sparkSystem.shape;ss.shapeType=ParticleSystemShapeType.Hemisphere;ss.radius=.15f;
   var sr=sparkGO.GetComponent<ParticleSystemRenderer>();
   sr.sharedMaterial=pMat;
   sr.renderMode=ParticleSystemRenderMode.Stretch;
   sr.velocityScale=0.06f;
   sr.lengthScale=2.2f;

   // Auto-attach skidmarks
   if(!GetComponent<Skidmarks>()) gameObject.AddComponent<Skidmarks>();
  }

  // Exhaust particles & jet trails — all cars
  exhaust=new ParticleSystem[2];
  exhaustTrails=new TrailRenderer[2];
  exhaustLights=new Light[2];

  for(int i=0;i<2;i++){
   var go=new GameObject("Nitro exhaust " + i);
   go.transform.SetParent(transform,false);
   go.transform.localPosition=new Vector3(i==0?-.58f:.58f,.38f,-2.22f);
   go.transform.localRotation=Quaternion.Euler(0,180,0);

   var ps=go.AddComponent<ParticleSystem>();
   ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=ps.main;
   main.playOnAwake=false;
   main.startLifetime=new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
   main.startSpeed=new ParticleSystem.MinMaxCurve(8f, 15f);
   main.startSize=new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
   main.startColor=new Color(0.2f, 0.85f, 1.0f, 0.9f);
   main.maxParticles=60;
   main.simulationSpace=ParticleSystemSimulationSpace.World;

   var em=ps.emission;
   em.rateOverTime=100;

   var shape=ps.shape;
   shape.shapeType=ParticleSystemShapeType.Cone;
   shape.angle=6f;
   shape.radius=0.035f;

   // Curva de tamanho e opacidade ao longo da vida (suave, sem quads/blocos)
   var sol=ps.sizeOverLifetime;
   sol.enabled=true;
   AnimationCurve sizeCurve=new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.1f));
   sol.size=new ParticleSystem.MinMaxCurve(1f, sizeCurve);

   var col=ps.colorOverLifetime;
   col.enabled=true;
   Gradient grad=new Gradient();
   grad.SetKeys(
    new GradientColorKey[] { new GradientColorKey(new Color(0.8f, 0.95f, 1f), 0f), new GradientColorKey(new Color(0.05f, 0.7f, 1f), 0.5f), new GradientColorKey(new Color(0.3f, 0.1f, 0.9f), 1f) },
    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) }
   );
   col.color=grad;

   var renderer=ps.GetComponent<ParticleSystemRenderer>();
   renderer.sharedMaterial=pMat;
   renderer.renderMode=ParticleSystemRenderMode.Billboard;
   exhaust[i]=ps;

   // Fita de fogo/plasma contínuo (TrailRenderer) no bocal do escapamento
   var trailGO=new GameObject("Exhaust Jet Ribbon");
   trailGO.transform.SetParent(go.transform, false);
   trailGO.transform.localPosition=Vector3.zero;
   var tr=trailGO.AddComponent<TrailRenderer>();
   tr.time=0.07f;
   tr.startWidth=0.14f;
   tr.endWidth=0.02f;
   tr.sharedMaterial=pMat;
   tr.colorGradient=grad;
   tr.emitting=false;
   exhaustTrails[i]=tr;

   // Luz pontual azul ciano no escapamento (ilumina o asfalto e difusor durante nitro)
   var lightGO=new GameObject("Car_NitroGlow_" + (i==0?"L":"R"));
   lightGO.transform.SetParent(go.transform, false);
   lightGO.transform.localPosition=new Vector3(0, 0, 0.2f);
   var l=lightGO.AddComponent<Light>();
   l.type=LightType.Point;
   l.color=new Color(0.1f, 0.8f, 1.0f);
   l.intensity=0f;
   l.range=4.2f;
   l.shadows=LightShadows.None;
   exhaustLights[i]=l;
  }
 }

 AudioSource Src(float vol,bool loop,float spatial,float minDist,float maxDist,AudioMixerGroup group){
  var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=loop;s.volume=vol;s.spatialBlend=spatial;s.minDistance=minDist;s.maxDistance=maxDist;s.outputAudioMixerGroup=group;return s;
 }

 static AudioMixerGroup Group(AudioMixer mixer,string name){
  if(!mixer)return null;
  var groups=mixer.FindMatchingGroups(name);
  return groups.Length>0?groups[0]:null;
 }

 void Update(){
  if(!car)return;
  bool paused=car.Paused;

  float gear=Mathf.Floor(car.SpeedKmh/45);float rpm=(car.SpeedKmh%45)/45;
  engine.pitch=Mathf.Lerp(engine.pitch,.7f+gear*.1f+rpm*.9f+(car.Boosting?.3f:0),Time.deltaTime*6);
  engine.volume=paused?0:.075f+Mathf.Clamp01(car.SpeedKmh/180)*.04f;

  if(isPlayer){
   if(paused){screech.volume=0;wind.volume=0;nitroSfx.volume=0;return;}
   float driftLevel=car.Drifting?Mathf.Clamp01(car.SpeedKmh/80):0;
   screech.volume=Mathf.Lerp(screech.volume,driftLevel*.2f,Time.deltaTime*8);screech.pitch=.8f+driftLevel*.4f;
   float speedFactor=Mathf.Clamp01(car.SpeedKmh/200);
   wind.volume=Mathf.Lerp(wind.volume,speedFactor*.065f,Time.deltaTime*3);wind.pitch=.6f+speedFactor*.8f;
   float nitroTarget=car.Boosting?.15f:0;
   nitroSfx.volume=Mathf.Lerp(nitroSfx.volume,nitroTarget,Time.deltaTime*(car.Boosting?12:5));nitroSfx.pitch=car.Boosting?1.1f+Mathf.Sin(Time.time*6)*.05f:1;
  }

  // Ativação e pulso das labaredas e jatos de plasma de nitro
  bool isBoost=car.Boosting;
  if(exhaust!=null){
   for(int i=0;i<exhaust.Length;i++){
    var ps=exhaust[i];
    if(ps!=null){
     if(isBoost&&!ps.isPlaying)ps.Play();
     else if(!isBoost&&ps.isPlaying)ps.Stop();
    }
    if(exhaustTrails!=null&&i<exhaustTrails.Length&&exhaustTrails[i]!=null){
     exhaustTrails[i].emitting=isBoost;
    }
    if(exhaustLights!=null&&i<exhaustLights.Length&&exhaustLights[i]!=null){
     float targetIntensity=isBoost?2.8f:0f;
     exhaustLights[i].intensity=Mathf.Lerp(exhaustLights[i].intensity,targetIntensity,Time.deltaTime*18f);
    }
   }
  }
 }

 void OnCollisionEnter(Collision col){
  if(!isPlayer)return;
  float force=col.relativeVelocity.magnitude;
  if(force>4){
   if(impactSrc!=null){impactSrc.volume=Mathf.Clamp01(force/30)*.4f;impactSrc.pitch=.7f+Random.Range(0,.4f);impactSrc.PlayOneShot(impactClip);}
   if(sparkSystem!=null&&col.contactCount>0){sparkSystem.transform.position=col.GetContact(0).point;sparkSystem.Emit(Mathf.CeilToInt(force*1.5f));}
   if(GameSettings.VibrationEnabled&&Gamepad.current!=null){
    if(rumbleRoutine!=null)StopCoroutine(rumbleRoutine);
    rumbleRoutine=StartCoroutine(ImpactRumble(Mathf.Clamp01(force/24f)));
   }
  }
 }

 IEnumerator ImpactRumble(float strength){
  var pad=Gamepad.current;
  if(pad==null)yield break;
  pad.SetMotorSpeeds(strength*.45f,strength);
  yield return new WaitForSecondsRealtime(Mathf.Lerp(.06f,.18f,strength));
  pad.SetMotorSpeeds(0f,0f);
  rumbleRoutine=null;
 }

 static AudioClip SynthEngine(){
  const int n=4096;var d=new float[n];
  for(int i=0;i<n;i++){float p=2*Mathf.PI*i/n;d[i]=.42f*Mathf.Sin(p*4)+.2f*Mathf.Sin(p*8)+.14f*Mathf.Sin(p*12)+.08f*Mathf.Sin(p*16)+.06f*Mathf.Sin(p*28)+.03f*Mathf.Sin(p*36);}
  var c=AudioClip.Create("Synth engine v2",n,1,48000,false);c.SetData(d,0);return c;
 }
 static AudioClip SynthBandNoise(int samples,float freq,float bw,int seed){
  var d=new float[samples];const int rate=48000;float w=2*Mathf.PI*freq/rate,r=1-bw/rate,a1=-2*r*Mathf.Cos(w),a2=r*r;
  float y1=0,y2=0;var rng=new System.Random(seed);
  for(int i=0;i<samples;i++){float x=(float)(rng.NextDouble()*2-1);float y=x-a1*y1-a2*y2;d[i]=Mathf.Clamp(y*.25f,-1,1);y2=y1;y1=y;}
  var c=AudioClip.Create("BandNoise_"+freq,samples,1,rate,false);c.SetData(d,0);return c;
 }
 static AudioClip SynthNitro(){
  const int n=4096;const int rate=48000;var d=new float[n];var rng=new System.Random(55);
  for(int i=0;i<n;i++){float t=(float)i/rate;float noise=(float)(rng.NextDouble()*2-1);d[i]=(noise*.15f+Mathf.Sin(2*Mathf.PI*180*t)*.3f+Mathf.Sin(2*Mathf.PI*90*t)*.2f)*.55f;}
  var c=AudioClip.Create("Nitro whoosh",n,1,rate,false);c.SetData(d,0);return c;
 }
 static AudioClip SynthImpact(){
  const int rate=48000;int n=rate/4;var d=new float[n];var rng=new System.Random(99);
  for(int i=0;i<n;i++){float t=(float)i/rate;float env=Mathf.Exp(-t*18);d[i]=(((float)(rng.NextDouble()*2-1))*.4f*env+Mathf.Sin(2*Mathf.PI*65*t)*env)*.7f;}
  var c=AudioClip.Create("Impact thud",n,1,rate,false);c.SetData(d,0);return c;
 }

 void OnDestroy(){
  if(isPlayer&&Gamepad.current!=null)Gamepad.current.SetMotorSpeeds(0f,0f);
  if(engineClip)Destroy(engineClip);if(screechClip)Destroy(screechClip);
  if(windClip)Destroy(windClip);if(nitroClip)Destroy(nitroClip);if(impactClip)Destroy(impactClip);
 }
}
}
