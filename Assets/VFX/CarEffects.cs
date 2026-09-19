using UnityEngine;
namespace NeonCoast {
[RequireComponent(typeof(ArcadeCar))]
public class CarEffects : MonoBehaviour {
 ArcadeCar car; bool isPlayer;
 AudioSource engine, screech, wind, nitroSfx, impactSrc;
 AudioClip engineClip, screechClip, windClip, nitroClip, impactClip;
 ParticleSystem[] exhaust; ParticleSystem sparkSystem;

 void Start(){
  car=GetComponent<ArcadeCar>(); isPlayer=!car.automation;

  // Engine — all cars
  engine=Src(.075f,true,.65f,4,65);
  engineClip=SynthEngine(); engine.clip=engineClip; engine.Play();

  // Player-only audio + VFX
  if(isPlayer){
   screech=Src(0,true,.7f,3,35); screechClip=SynthBandNoise(8192,3200,600,77); screech.clip=screechClip; screech.Play();
   wind=Src(0,true,.35f,5,80); windClip=SynthBandNoise(8192,350,250,33); wind.clip=windClip; wind.Play();
   nitroSfx=Src(0,true,.5f,3,45); nitroClip=SynthNitro(); nitroSfx.clip=nitroClip; nitroSfx.Play();
   impactSrc=Src(0,false,.85f,2,30); impactClip=SynthImpact(); impactSrc.clip=impactClip;

   // Collision sparks particle system
   var sparkGO=new GameObject("Collision Sparks");sparkGO.transform.SetParent(transform,false);
   sparkSystem=sparkGO.AddComponent<ParticleSystem>();sparkSystem.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var sm=sparkSystem.main;sm.playOnAwake=false;sm.startLifetime=new ParticleSystem.MinMaxCurve(.08f,.35f);sm.startSpeed=new ParticleSystem.MinMaxCurve(5,14);
   sm.startSize=new ParticleSystem.MinMaxCurve(.015f,.05f);sm.startColor=new Color(1,.75f,.25f);sm.gravityModifier=3;sm.maxParticles=60;sm.simulationSpace=ParticleSystemSimulationSpace.World;
   var se=sparkSystem.emission;se.enabled=false;
   var ss=sparkSystem.shape;ss.shapeType=ParticleSystemShapeType.Hemisphere;ss.radius=.15f;
   sparkGO.GetComponent<ParticleSystemRenderer>().sharedMaterial=Resources.Load<Material>("NitroParticle");

   // Auto-attach skidmarks
   if(!GetComponent<Skidmarks>()) gameObject.AddComponent<Skidmarks>();
  }

  // Exhaust particles — all cars
  exhaust=new ParticleSystem[2];
  for(int i=0;i<2;i++){
   var go=new GameObject("Nitro exhaust");go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(i==0?-.65f:.65f,.35f,-2.18f);go.transform.localRotation=Quaternion.Euler(0,180,0);
   var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=ps.main;main.playOnAwake=false;main.startLifetime=.12f;main.startSpeed=5;main.startSize=.17f;main.startColor=new Color(.1f,.8f,1);main.maxParticles=40;main.simulationSpace=ParticleSystemSimulationSpace.World;
   var em=ps.emission;em.rateOverTime=80;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=8;shape.radius=.04f;
   var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Resources.Load<Material>("NitroParticle");exhaust[i]=ps;
  }
 }

 AudioSource Src(float vol,bool loop,float spatial,float minDist,float maxDist){
  var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=loop;s.volume=vol;s.spatialBlend=spatial;s.minDistance=minDist;s.maxDistance=maxDist;return s;
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
  foreach(var ps in exhaust){if(car.Boosting&&!ps.isPlaying)ps.Play();else if(!car.Boosting&&ps.isPlaying)ps.Stop();}
 }

 void OnCollisionEnter(Collision col){
  if(!isPlayer)return;
  float force=col.relativeVelocity.magnitude;
  if(force>4){
   if(impactSrc!=null){impactSrc.volume=Mathf.Clamp01(force/30)*.4f;impactSrc.pitch=.7f+Random.Range(0,.4f);impactSrc.PlayOneShot(impactClip);}
   if(sparkSystem!=null&&col.contactCount>0){sparkSystem.transform.position=col.GetContact(0).point;sparkSystem.Emit(Mathf.CeilToInt(force*1.5f));}
  }
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
  if(engineClip)Destroy(engineClip);if(screechClip)Destroy(screechClip);
  if(windClip)Destroy(windClip);if(nitroClip)Destroy(nitroClip);if(impactClip)Destroy(impactClip);
 }
}
}
