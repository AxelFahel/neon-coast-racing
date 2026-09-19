using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using NeonCoast;
public static class NeonCoastBuilder {
 const string ScenePath="Assets/Scenes/NeonCoast.unity";
 static Material asphalt,concrete,metal,cyan,pink,white,glass,paint,grass,marking;
 static Transform world;
 static readonly Vector3[] knots={new Vector3(-100,0,-120),new Vector3(-100,0,-40),new Vector3(-95,0,65),new Vector3(-45,2,125),new Vector3(55,7,125),new Vector3(115,12,65),new Vector3(115,12,-10),new Vector3(85,7,-65),new Vector3(105,2,-130),new Vector3(45,0,-185),new Vector3(-45,0,-185)};
 public static Vector3 Point(float t){float f=Mathf.Repeat(t,1)*knots.Length;int i=Mathf.FloorToInt(f);float u=f-i;Vector3 a=knots[(i+knots.Length-1)%knots.Length],b=knots[i%knots.Length],c=knots[(i+1)%knots.Length],d=knots[(i+2)%knots.Length];return .5f*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u);}
 static Vector3 Forward(float t){return (Point(t+.0005f)-Point(t-.0005f)).normalized;}
 static Vector3 Right(float t){return Vector3.Cross(Vector3.up,Forward(t)).normalized;}
 [MenuItem("Neon Coast/Build Initial Racing Scene")]
 public static void Build(){
  if(File.Exists(ScenePath))throw new Exception("NeonCoast scene already exists; preserved.");
  if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first.");
  foreach(string folder in new[]{"Scenes","Scripts","Vehicles","Environment","Materials","Prefabs","UI","Audio","VFX","Shaders","Settings"})Directory.CreateDirectory("Assets/"+folder);
  AssetDatabase.Refresh();
  bool resume=GameObject.Find("Neon Coast Environment")!=null; var scene=resume?SceneManager.GetActiveScene():EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  world=resume?GameObject.Find("Neon Coast Environment").transform:new GameObject("Neon Coast Environment").transform;
  asphalt=Mat("Wet graphite asphalt",new Color(.075f,.085f,.10f),.3f,.88f);
  concrete=Mat("Architectural concrete",new Color(.24f,.28f,.34f),.05f,.38f);
  metal=Mat("Graphite metal",new Color(.08f,.10f,.15f),.75f,.7f);
  cyan=Mat("Ion cyan",new Color(.02f,.7f,.8f),.15f,.7f,new Color(.04f,.9f,1)*3);
  pink=Mat("Coral neon",new Color(.9f,.08f,.3f),.1f,.6f,new Color(1,.055f,.2f)*3);
  white=Mat("Warm light",new Color(.85f,.8f,.65f),0,.6f,new Color(1,.76f,.45f)*2);
  marking=Mat("Road markings",new Color(.8f,.83f,.86f),0,.35f);
  glass=Mat("Smoked glass",new Color(.025f,.07f,.11f),.85f,.97f);
  paint=Mat("Pearl turquoise",new Color(.015f,.48f,.52f),.8f,.85f);
  grass=Mat("Coastal vegetation",new Color(.035f,.11f,.095f),0,.25f);
  if(!resume)CreateAsphaltTexture();
  if(!resume)Cube("Coastal foundation",new Vector3(20,-2,0),new Vector3(355,3,490),concrete,true);
  if(!resume)Cube("Pacific ocean",new Vector3(-500,-1.7f,0),new Vector3(680,.25f,1800),Mat("Midnight ocean",new Color(.016f,.05f,.09f),.65f,.93f),false);
  if(!resume)Road();City();
  ArcadeCar car=Car();
  var cameraGO=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(ChaseCamera));cameraGO.tag="MainCamera";
  var cam=cameraGO.GetComponent<Camera>();cam.nearClipPlane=.1f;cam.farClipPlane=1100;cam.allowHDR=true;
  cameraGO.transform.position=car.transform.position-car.transform.forward*8+Vector3.up*4;cameraGO.transform.LookAt(car.transform.position+Vector3.up);
  cameraGO.GetComponent<ChaseCamera>().target=car;Lighting(cam);
  var race=new GameObject("Race Director").AddComponent<RaceSession>();race.player=car;race.checkpoints=new Transform[8];
  for(int i=0;i<8;i++){
   float t=(i+1)/8f;var go=new GameObject("Checkpoint "+(i+1));go.transform.position=Point(t)+Vector3.up*1.5f;go.transform.rotation=Quaternion.LookRotation(Forward(t));
   var col=go.AddComponent<BoxCollider>();col.isTrigger=true;col.size=new Vector3(17,5,2);
   var cp=go.AddComponent<Checkpoint>();cp.race=race;cp.index=i;race.checkpoints[i]=go.transform;
   for(int s=-1;s<=1;s+=2)Cube("Checkpoint light",Point(t)+Right(t)*8*s+Vector3.up*2.5f,new Vector3(.18f,5,.18f),i==7?pink:cyan,false);
  }
  HUD(cam,car,race);PrefabUtility.SaveAsPrefabAsset(car.gameObject,"Assets/Prefabs/PlayerCar.prefab");
  EditorSceneManager.SaveScene(scene,ScenePath);
  var builds=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(ScenePath,true)};
  foreach(var b in EditorBuildSettings.scenes)if(b.path!=ScenePath)builds.Add(b);EditorBuildSettings.scenes=builds.ToArray();
  AssetDatabase.SaveAssets();Selection.activeGameObject=car.gameObject;
  Debug.Log("Neon Coast Racing: scene generated and saved. Ready for Play Mode validation.");
 }
 static Material Mat(string name,Color c,float metallic,float smooth,Color emission=default){
  string path="Assets/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
  m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);m.enableInstancing=true;
  if(emission.maxColorComponent>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission);}AssetDatabase.CreateAsset(m,path);return m;
 }
 static GameObject Cube(string name,Vector3 p,Vector3 scale,Material mat,bool collide,Transform parent=null){
  var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent?parent:world,false);go.transform.position=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;
  if(!collide)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.isStatic=parent==null;return go;
 }
 static void CreateAsphaltTexture(){
  var tex=new Texture2D(256,256,TextureFormat.RGB24,true);var random=new System.Random(84);var colors=new Color[256*256];
  for(int i=0;i<colors.Length;i++){float n=.7f+(float)random.NextDouble()*.3f;colors[i]=new Color(n,n,n);}
  tex.SetPixels(colors);tex.Apply();File.WriteAllBytes("Assets/Environment/AsphaltGrain.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
  AssetDatabase.ImportAsset("Assets/Environment/AsphaltGrain.png");asphalt.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/AsphaltGrain.png"));asphalt.SetTextureScale("_BaseMap",new Vector2(3,3));
 }
 static void Road(){
  int n=440;var v=new Vector3[(n+1)*2];var uv=new Vector2[v.Length];var tris=new int[n*6];float distance=0;
  for(int i=0;i<=n;i++){float t=(float)i/n;Vector3 p=Point(t),r=Right(t);if(i>0)distance+=Vector3.Distance(p,Point((float)(i-1)/n));v[i*2]=p-r*8;v[i*2+1]=p+r*8;uv[i*2]=new Vector2(0,distance/16);uv[i*2+1]=new Vector2(1,distance/16);
   if(i<n){int j=i*6,k=i*2;tris[j]=k;tris[j+1]=k+2;tris[j+2]=k+1;tris[j+3]=k+1;tris[j+4]=k+2;tris[j+5]=k+3;}}
  var mesh=new Mesh{name="Continuous coastal circuit"};mesh.vertices=v;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,"Assets/Environment/CoastalRoad.asset");
  var go=new GameObject("Coastal Circuit",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.parent=world;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=asphalt;go.GetComponent<MeshCollider>().sharedMesh=mesh;go.isStatic=true;
  for(int i=0;i<n;i++){
   float t=(float)i/n,t1=(float)(i+1)/n;Vector3 p=Point(t),p1=Point(t1),mid=(p+p1)*.5f,r=Right((t+t1)/2);float length=Vector3.Distance(p,p1)+.05f;var q=Quaternion.LookRotation(p1-p);
   for(int s=-1;s<=1;s+=2){
    // Open the inside boundary at the harbor service intersection.
    if(!(s==1 && i>=31 && i<=40)){var barrier=Cube("Safety barrier",mid+r*8.6f*s+Vector3.up*.55f,new Vector3(.5f,1.1f,length),concrete,true);barrier.transform.rotation=q;}
    var edge=Cube("Edge strip",mid+r*7.5f*s+Vector3.up*.025f,new Vector3(.13f,.025f,length),marking,false);edge.transform.rotation=q;
    if(i%4==0){var strip=Cube("Barrier reflector",mid+r*8.29f*s+Vector3.up*.8f,new Vector3(.04f,.12f,.65f),i%8==0?cyan:pink,false);strip.transform.rotation=q;}
   }
   if(i%3==0){var lane=Cube("Center dash",mid+Vector3.up*.025f,new Vector3(.16f,.025f,length*.8f),marking,false);lane.transform.rotation=q;}
   if(i%18==0){Streetlight(p+r*10,q);if(p.y>3)Cube("Viaduct pier",p+Vector3.down*(p.y+1)/2,new Vector3(3,p.y+1,3),concrete,true);}
   if(i>=170&&i<=205){for(int s=-1;s<=1;s+=2){var wall=Cube("Tunnel wall",mid+r*9*s+Vector3.up*3,new Vector3(.5f,6,length),concrete,true);wall.transform.rotation=q;}var roof=Cube("Tunnel canopy",mid+Vector3.up*6,new Vector3(18.5f,.6f,length),metal,true);roof.transform.rotation=q;if(i%5==0){var bar=Cube("Tunnel light ribbon",mid+Vector3.up*5.6f,new Vector3(15,.12f,.22f),cyan,false);bar.transform.rotation=q;}}
  }
  Vector3 start=Point(0);for(int x=0;x<16;x++)for(int z=0;z<2;z++){if((x+z)%2==0)Cube("Start checker",start+new Vector3(x-7.5f,.035f,z-1),new Vector3(.95f,.035f,.95f),marking,false);}
  for(int s=-1;s<=1;s+=2)Cube("Start gantry leg",start+Vector3.right*10*s+Vector3.up*4,new Vector3(.7f,8,.7f),metal,true);
  Cube("Start gantry",start+Vector3.up*8,new Vector3(21,1.6f,.7f),metal,true);
  WorldText("NEON COAST / NIGHT RUN",start+new Vector3(0,8,-.4f),Quaternion.identity,1,Color.cyan);
  Cube("Harbor service road",new Vector3(-74,-.06f,-51),new Vector3(42,.1f,21),asphalt,true);
  Cube("Service road end barrier",new Vector3(-53,.5f,-51),new Vector3(.5f,1,22),concrete,true);
  for(int s=-1;s<=1;s+=2)Cube("Service boundary",new Vector3(-75,.5f,-51+s*10.8f),new Vector3(44,1,.4f),concrete,true);
 }
 static void Streetlight(Vector3 p,Quaternion q){
  Cube("Lamp column",p+Vector3.up*4,new Vector3(.16f,8,.16f),metal,true);
  var arm=Cube("Lamp arm",p+Vector3.up*7.9f-q*Vector3.right*1.4f,new Vector3(3,.12f,.14f),metal,false);arm.transform.rotation=q;
  Vector3 lp=p+Vector3.up*7.8f-q*Vector3.right*2.7f;Cube("Lamp luminaire",lp,new Vector3(.65f,.12f,1.1f),white,false);
  var l=new GameObject("Road illumination").AddComponent<Light>();l.transform.parent=world;l.transform.position=lp;l.transform.rotation=Quaternion.Euler(90,0,0);l.type=LightType.Spot;l.spotAngle=110;l.range=27;l.intensity=22;l.color=new Color(.66f,.83f,1);l.shadows=LightShadows.None;
 }
 static void City(){
  var rng=new System.Random(57);
  for(int i=0;i<58;i++){
   float x=-43+(float)rng.NextDouble()*90,z=-120+(float)rng.NextDouble()*210,h=14+(float)rng.NextDouble()*70,w=8+(float)rng.NextDouble()*10,d=8+(float)rng.NextDouble()*12;
   Vector3 p=new Vector3(x,h/2-1,z);Cube("Skyline tower "+i,p,new Vector3(w,h,d),i%3==0?glass:metal,true);
   Cube("Rooftop rim",p+Vector3.up*(h/2),new Vector3(w+.2f,.16f,d+.2f),i%3==0?pink:cyan,false);
   for(int floor=3;floor<h;floor+=4)for(int side=-1;side<=1;side+=2)Cube("Window band",new Vector3(x,floor-1,z+side*(d/2+.025f)),new Vector3(w*.86f,.65f,.035f),i%4==0?white:cyan,false);
  }
  for(int i=0;i<22;i++){
   Vector3 p=new Vector3(-137,-.5f,-185+i*17);Cube("Coastal promenade",p,new Vector3(20,.4f,16.9f),concrete,true);
   Cube("Palm trunk",p+Vector3.up*3,new Vector3(.25f,6,.25f),metal,false);
   for(int leaf=0;leaf<6;leaf++){var go=Cube("Palm frond",p+Vector3.up*6,new Vector3(.65f,.13f,5),grass,false);go.transform.rotation=Quaternion.Euler(20,leaf*60,0);}
   if(i%3==0)Cube("Promenade neon",p+Vector3.left*8+Vector3.up*.25f,new Vector3(.15f,.15f,15),cyan,false);
  }
  for(int i=0;i<8;i++){float t=.24f+i*.075f;Vector3 p=Point(t)+Right(t)*11+Vector3.up*3;var sign=Cube("Direction sign",p,new Vector3(2.5f,1.3f,.12f),metal,false);sign.transform.rotation=Quaternion.LookRotation(Forward(t));WorldText("> > >",p-Forward(t)*.08f,Quaternion.LookRotation(Forward(t)),.4f,Color.white);}
 }
 static ArcadeCar Car(){
  var root=new GameObject("Player Aster GT");root.layer=2;root.transform.position=Point(0)+Vector3.up*.8f;root.transform.rotation=Quaternion.LookRotation(Forward(0));
  root.AddComponent<Rigidbody>();var col=root.AddComponent<BoxCollider>();col.center=new Vector3(0,.58f,0);col.size=new Vector3(1.86f,.7f,4.15f);
  var car=root.AddComponent<ArcadeCar>();car.wheels=new WheelCollider[4];car.wheelVisuals=new Transform[4];
  var visual=new GameObject("Aster GT Coachwork").transform;visual.SetParent(root.transform,false);car.bodyVisual=visual;
  float[] zs={-2.18f,-1.8f,-.9f,.55f,1.5f,2.12f};float[] widths={.77f,1.0f,.94f,.92f,.97f,.78f};float[] heights={.55f,.72f,.78f,.70f,.60f,.42f};
  var verts=new List<Vector3>();var triangles=new List<int>();
  for(int j=0;j<zs.Length;j++){float w=widths[j],h=heights[j];verts.AddRange(new[]{new Vector3(-w,.28f,zs[j]),new Vector3(-w,h*.82f,zs[j]),new Vector3(-w*.74f,h,zs[j]),new Vector3(w*.74f,h,zs[j]),new Vector3(w,h*.82f,zs[j]),new Vector3(w,.28f,zs[j])});}
  for(int j=0;j<zs.Length-1;j++)for(int k=0;k<6;k++){int a=j*6+k,b=j*6+(k+1)%6,c=a+6,d=b+6;triangles.AddRange(new[]{a,c,b,b,c,d});}
  for(int k=1;k<5;k++){triangles.AddRange(new[]{0,k,k+1});int a=(zs.Length-1)*6;triangles.AddRange(new[]{a,a+k+1,a+k});}
  var mesh=new Mesh{name="Aster GT sculpted body"};mesh.SetVertices(verts);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,"Assets/Vehicles/AsterGTBody.asset");
  var shell=new GameObject("Sculpted body",typeof(MeshFilter),typeof(MeshRenderer));shell.transform.SetParent(visual,false);shell.GetComponent<MeshFilter>().sharedMesh=mesh;shell.GetComponent<MeshRenderer>().sharedMaterial=paint;
  CarPart("Glass canopy",new Vector3(0,.92f,-.25f),new Vector3(1.48f,.64f,1.72f),glass,visual);
  var windshield=CarPart("Sloped windscreen",new Vector3(0,1,.6f),new Vector3(1.42f,.06f,.95f),glass,visual);windshield.transform.localRotation=Quaternion.Euler(38,0,0);
  CarPart("Roof",new Vector3(0,1.27f,-.3f),new Vector3(1.4f,.08f,.9f),paint,visual);
  CarPart("Front splitter",new Vector3(0,.25f,1.94f),new Vector3(1.8f,.12f,.35f),metal,visual);
  CarPart("Rear diffuser",new Vector3(0,.27f,-2),new Vector3(1.65f,.16f,.35f),metal,visual);
  CarPart("Rear wing",new Vector3(0,1.02f,-1.85f),new Vector3(2.02f,.09f,.36f),metal,visual);
  for(int s=-1;s<=1;s+=2){CarPart("Wing mount",new Vector3(s*.62f,.84f,-1.85f),new Vector3(.07f,.4f,.16f),metal,visual);CarPart("Headlight",new Vector3(s*.59f,.52f,1.96f),new Vector3(.44f,.075f,.08f),white,visual);CarPart("Tail light",new Vector3(s*.56f,.61f,-2.03f),new Vector3(.48f,.065f,.08f),pink,visual);CarPart("Side skirt",new Vector3(s*.94f,.29f,0),new Vector3(.09f,.13f,2.1f),metal,visual);}
  var rubber=Mat("Performance rubber",new Color(.02f,.023f,.028f),0,.22f);
  for(int i=0;i<4;i++){
   Vector3 pos=new Vector3(i%2==0?-.96f:.96f,.38f,i<2?1.31f:-1.32f);
   var wgo=new GameObject("Suspension "+i);wgo.layer=2;wgo.transform.SetParent(root.transform,false);wgo.transform.localPosition=pos;
   var wc=wgo.AddComponent<WheelCollider>();wc.radius=.36f;wc.mass=25;wc.suspensionDistance=.22f;wc.forceAppPointDistance=.18f;var spring=wc.suspensionSpring;spring.spring=42000;spring.damper=5200;spring.targetPosition=.5f;wc.suspensionSpring=spring;car.wheels[i]=wc;
   var wheel=new GameObject("Wheel "+i).transform;wheel.SetParent(root.transform,false);wheel.localPosition=pos;car.wheelVisuals[i]=wheel;
   Cylinder("Tire",wheel,new Vector3(.72f,.16f,.72f),rubber);Cylinder("Alloy rim",wheel,new Vector3(.50f,.166f,.50f),concrete);Cylinder("Hub",wheel,new Vector3(.14f,.17f,.14f),metal);
   for(int s=0;s<5;s++){var spoke=CarPart("Rim spoke",Vector3.zero,new Vector3(.34f,.045f,.5f),metal,wheel);spoke.transform.localRotation=Quaternion.Euler(s*72,0,0);}
  }
  foreach(var tr in root.GetComponentsInChildren<Transform>())tr.gameObject.layer=2;return car;
 }
 static GameObject CarPart(string name,Vector3 p,Vector3 size,Material m,Transform parent){var g=Cube(name,Vector3.zero,size,m,false,parent);g.transform.localPosition=p;return g;}
 static void Cylinder(string name,Transform parent,Vector3 scale,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);g.transform.localRotation=Quaternion.Euler(0,0,90);g.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;}
 static TMP_FontAsset font;
 static TMP_FontAsset Font(){if(font)return font;font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/NeonCoastFont.asset");if(font)return font;var f=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");font=TMP_FontAsset.CreateFontAsset(f);font.name="Neon Coast UI Font";AssetDatabase.CreateAsset(font,"Assets/UI/NeonCoastFont.asset");foreach(var atlas in font.atlasTextures)if(atlas)AssetDatabase.AddObjectToAsset(atlas,font);if(font.material)AssetDatabase.AddObjectToAsset(font.material,font);return font;}
 static void WorldText(string text,Vector3 p,Quaternion rot,float size,Color color){var go=new GameObject(text);go.transform.parent=world;go.transform.SetPositionAndRotation(p,rot);var tmp=go.AddComponent<TextMeshPro>();tmp.font=Font();tmp.text=text;tmp.fontSize=size*10;tmp.color=color;tmp.alignment=TextAlignmentOptions.Center;tmp.rectTransform.sizeDelta=new Vector2(20,2);}
 static void Lighting(Camera cam){
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.10f,.16f,.27f);RenderSettings.ambientEquatorColor=new Color(.075f,.10f,.16f);RenderSettings.ambientGroundColor=new Color(.025f,.035f,.06f);
  RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.025f,.045f,.085f);RenderSettings.fogDensity=.0027f;
  cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=RenderSettings.fogColor;
  var moon=new GameObject("Moon cool key").AddComponent<Light>();moon.type=LightType.Directional;moon.color=new Color(.45f,.65f,1);moon.intensity=1.1f;moon.shadows=LightShadows.Soft;moon.transform.rotation=Quaternion.Euler(35,-35,0);RenderSettings.sun=moon;
  var data=cam.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.volumeLayerMask=1;
  var asset=UniversalRenderPipeline.asset;if(asset){asset.supportsHDR=true;asset.shadowDistance=100;EditorUtility.SetDirty(asset);}
  var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,"Assets/Settings/NeonNight.asset");
  var bloom=profile.Add<Bloom>(true);bloom.threshold.value=1;bloom.intensity.value=.3f;bloom.scatter.value=.65f;
  profile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;
  var color=profile.Add<ColorAdjustments>(true);color.postExposure.value=.6f;color.contrast.value=12;color.saturation.value=-8;
  profile.Add<Vignette>(true).intensity.value=.18f;profile.Add<MotionBlur>(true).intensity.value=.12f;
  foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);
  var vol=new GameObject("Night Grade Global Volume").AddComponent<Volume>();vol.isGlobal=true;vol.sharedProfile=profile;
  for(int i=0;i<3;i++){var probe=new GameObject("Reflection district "+i).AddComponent<ReflectionProbe>();probe.transform.position=Point(i/3f)+Vector3.up*4;probe.size=new Vector3(180,70,180);probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.resolution=128;probe.boxProjection=true;probe.cullingMask=1;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=RenderSettings.fogColor;}
 }
 static TMP_Text Label(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,int fontSize,Color color){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var t=go.AddComponent<TextMeshProUGUI>();t.font=Font();t.fontSize=fontSize;t.color=color;t.raycastTarget=false;var r=t.rectTransform;r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;return t;}
 static void HUD(Camera cam,ArcadeCar car,RaceSession race){
  HUDOverhaul.RebuildHUD();
 }
}
