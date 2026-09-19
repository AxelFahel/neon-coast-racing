using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using TMPro;
public static class NeonPolish {
 public static void Apply(){
  var world=GameObject.Find("Neon Coast Environment").transform;
  var sky=new Material(Shader.Find("NeonCoast/NightSky"));AssetDatabase.CreateAsset(sky,"Assets/Materials/CoastalNightSky.mat");RenderSettings.skybox=sky;Camera.main.clearFlags=CameraClearFlags.Skybox;
  RenderSettings.fogColor=new Color(.095f,.105f,.16f);RenderSettings.fogDensity=.0035f;
  RenderSettings.ambientSkyColor=new Color(.28f,.35f,.48f);RenderSettings.ambientEquatorColor=new Color(.16f,.21f,.3f);RenderSettings.ambientGroundColor=new Color(.08f,.1f,.16f);
  var road=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wet graphite asphalt.mat");road.SetFloat("_Smoothness",.72f);road.SetFloat("_Metallic",.12f);EditorUtility.SetDirty(road);
  var cam=Camera.main;cam.allowHDR=true;
  var cyan=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ion cyan.mat");
  var warm=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Warm light.mat");
  var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Graphite metal.mat");
  var concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Architectural concrete.mat");
  var pink=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Coral neon.mat");
  var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/NeonCoastFont.asset");
  var sign=GameObject.Find("NEON COAST / NIGHT RUN");if(sign){var t=sign.GetComponent<TextMeshPro>();t.font=font;t.text="NEON COAST / NIGHT RUN";t.fontSize=9;t.color=Color.cyan;t.alignment=TextAlignmentOptions.Center;}
  // More subdued facade lights, punctuated by neon roofs.
  var windows=new Material(warm);windows.name="Office window glow";windows.SetColor("_BaseColor",new Color(.26f,.34f,.42f));windows.SetColor("_EmissionColor",new Color(.35f,.5f,.65f)*.9f);AssetDatabase.CreateAsset(windows,"Assets/Materials/OfficeWindows.mat");
  foreach(var r in world.GetComponentsInChildren<MeshRenderer>())if(r.name=="Window band"){r.sharedMaterial=windows;var s=r.transform.localScale;s.y=.38f;r.transform.localScale=s;}
  // Street-level buildings and promenade furniture keep the road visually grounded.
  for(int i=0;i<10;i++){
   float z=-105+i*19;var store=Box("Harbor arcade",new Vector3(-66,2.5f,z),new Vector3(15,6,14),metal,world,true);
   Box("Storefront glass",new Vector3(-73.55f,2.2f,z),new Vector3(.08f,3,11),windows,world,false);
   Box("Shop canopy",new Vector3(-74,4.2f,z),new Vector3(3,.22f,14),concrete,world,false);
   Box("Shop light trim",new Vector3(-75.5f,4.2f,z),new Vector3(.12f,.12f,14),i%3==0?pink:cyan,world,false);
  }
  // A drivable, bounded T junction on the harbor straight.
  Box("Harbor service road",new Vector3(-74,-.06f,-51),new Vector3(42,.1f,21),road,world,true);
  Box("Service end barrier",new Vector3(-53,.5f,-51),new Vector3(.5f,1,22),concrete,world,true);
  for(int s=-1;s<=1;s+=2)Box("Service boundary",new Vector3(-75,.5f,-51+s*10.8f),new Vector3(44,1,.4f),concrete,world,true);
  // Headlights and a restrained body fill remain with the car.
  var car=Object.FindFirstObjectByType<NeonCoast.ArcadeCar>();
  for(int s=-1;s<=1;s+=2){var g=new GameObject("Projector headlight");g.transform.SetParent(car.transform,false);g.transform.localPosition=new Vector3(s*.58f,.55f,2.05f);g.transform.localRotation=Quaternion.Euler(5,0,0);var l=g.AddComponent<Light>();l.type=LightType.Spot;l.range=55;l.spotAngle=52;l.intensity=12;l.color=new Color(.7f,.86f,1);l.shadows=LightShadows.None;g.layer=2;}
  var fill=new GameObject("Paint rim light");fill.transform.SetParent(car.transform,false);fill.transform.localPosition=new Vector3(0,2,-1.5f);var fl=fill.AddComponent<Light>();fl.type=LightType.Point;fl.range=5;fl.intensity=1.4f;fl.color=new Color(.1f,.75f,1);fill.layer=2;
  foreach(var probe in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)){probe.clearFlags=ReflectionProbeClearFlags.Skybox;probe.backgroundColor=RenderSettings.fogColor;}
  // Static render batching reduces the cost of the many small architectural details.
  foreach(var r in world.GetComponentsInChildren<MeshRenderer>()) GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
  PrefabUtility.SaveAsPrefabAsset(car.gameObject,"Assets/Prefabs/PlayerCar.prefab");
  EditorSceneManager.MarkSceneDirty(car.gameObject.scene);EditorSceneManager.SaveScene(car.gameObject.scene);AssetDatabase.SaveAssets();
 }
 static GameObject Box(string name,Vector3 pos,Vector3 size,Material mat,Transform parent,bool collider){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;return g;}
}

