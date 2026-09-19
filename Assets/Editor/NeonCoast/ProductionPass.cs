using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
public static class ProductionPass {
 const string Root="Assets/Environment/Production";
 static Transform env;static Material trim,glass,metal;static int meshId;
 [MenuItem("Neon Coast/Apply Racing and Art Pass")]
 public static void Apply(){
  if(GameObject.Find("Production Pass"))throw new Exception("Pass already applied; edit the existing objects.");
  Directory.CreateDirectory(Root);AssetDatabase.Refresh();env=new GameObject("Production Pass").transform;
  trim=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ion cyan.mat");glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Smoked glass.mat");metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Graphite metal.mat");
  var player=UnityEngine.Object.FindFirstObjectByType<NeonCoast.ArcadeCar>();
  var racer=player.gameObject.AddComponent<NeonCoast.GridRacer>();racer.driverName="YOU";
  DetailCar(player.gameObject,false,0);
  var path=new Vector3[440];for(int i=0;i<440;i++)path[i]=NeonCoastBuilder.Point(i/440f);
  var fleet=new GameObject("Racers and Civilian Traffic").transform;
  string[] names={"KAI / Aster R","NOVA / Aster S","REI / Aster X"};Color[] colors={new Color(.95f,.24f,.05f),new Color(.65f,.1f,.7f),new Color(.82f,.86f,.91f)};
  for(int i=0;i<3;i++){
   var go=UnityEngine.Object.Instantiate(player.gameObject);go.name=names[i];go.transform.parent=fleet;
   go.GetComponent<NeonCoast.GridRacer>().driverName=names[i].Split('/')[0].Trim();
   Place(go, -.012f*(i/2+1),i%2==0?-2.7f:2.7f);
   SetPaint(go,colors[i],"Rival "+i);
   var d=go.AddComponent<NeonCoast.PaceDriver>();d.path=path;d.cruiseSpeed=20.5f+i*1.2f;d.laneOffset=i%2==0?-2.7f:2.7f;d.aggression=.45f+i*.25f;
   go.GetComponent<NeonCoast.ArcadeCar>().automation=true;
  }
  for(int i=0;i<7;i++){
   var go=UnityEngine.Object.Instantiate(player.gameObject);go.name=(i%3==0?"Harbor van":i%3==1?"Coastal sedan":"City SUV")+" "+(i+1);go.transform.parent=fleet;
   UnityEngine.Object.DestroyImmediate(go.GetComponent<NeonCoast.GridRacer>());
   foreach(var t in go.GetComponentsInChildren<Transform>())if(t.name=="Rear wing"||t.name=="Wing mount"||t.name=="Paint rim light")UnityEngine.Object.DestroyImmediate(t.gameObject);
   DetailCar(go,true,i%3);
   SetPaint(go,Color.HSVToRGB((i*.147f)%1,.15f,.2f+i*.055f),"Traffic "+i);
   Place(go,.10f+i*.12f,i%2==0?3.4f:-3.4f);
   var d=go.AddComponent<NeonCoast.PaceDriver>();d.path=path;d.cruiseSpeed=10+i%3*1.5f;d.laneOffset=i%2==0?3.4f:-3.4f;d.traffic=true;
   go.GetComponent<NeonCoast.ArcadeCar>().automation=true;
  }
  Facades();RoadEdges();SurfaceDetail();Signs();
  var volume=UnityEngine.Object.FindFirstObjectByType<Volume>();if(volume.sharedProfile.TryGet<Bloom>(out var bloom)){bloom.intensity.value=.55f;bloom.threshold.value=.8f;EditorUtility.SetDirty(bloom);}
  if(volume.sharedProfile.TryGet<ColorAdjustments>(out var grade)){grade.contrast.value=4;grade.postExposure.value=.9f;EditorUtility.SetDirty(grade);}
  RenderSettings.fogDensity=.0024f;RenderSettings.fogColor=new Color(.10f,.12f,.20f);
  foreach(var r in env.GetComponentsInChildren<MeshRenderer>())GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.OccluderStatic);
  PrefabUtility.SaveAsPrefabAsset(player.gameObject,"Assets/Prefabs/PlayerCar.prefab");
  EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();
 }
 static void Place(GameObject go,float t,float lane){var p=NeonCoastBuilder.Point(t);var f=(NeonCoastBuilder.Point(t+.001f)-p).normalized;go.transform.SetPositionAndRotation(p+Vector3.Cross(Vector3.up,f)*lane+Vector3.up*.65f,Quaternion.LookRotation(f));}
 static void SetPaint(GameObject go,Color color,string name){var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Pearl turquoise.mat");var m=new Material(original);m.name=name;m.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(m,"Assets/Materials/"+name+".mat");foreach(var r in go.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial==original)r.sharedMaterial=m;}
 static GameObject Part(string name,Transform parent,Vector3 p,Vector3 scale,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
 static void DetailCar(GameObject go,bool traffic,int variant){
  var car=go.GetComponent<NeonCoast.ArcadeCar>();var coach=car.bodyVisual;
  foreach(var t in go.GetComponentsInChildren<Transform>())if(t.name=="Glass canopy"||t.name=="Sloped windscreen"||t.name=="Roof"||t.name=="Detailed canopy"||t.name=="Cabin cap")UnityEngine.Object.DestroyImmediate(t.gameObject);
  float height=traffic?(variant==0?1.65f:variant==2?1.35f:1.15f):1.13f;
  var cabin=Loft("Detailed canopy",new[]{-1.18f,-.64f,.32f,1.12f},new[]{.81f,.68f,.63f,.72f},new[]{.72f,height,height*.97f,.65f});
  MeshObject("Detailed canopy",cabin,glass,coach);
  var paint=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Pearl turquoise.mat");
  Part("Cabin cap",coach,new Vector3(0,height+.012f,-.17f),new Vector3(1.24f,.035f,.76f),paint);
  for(int s=-1;s<=1;s+=2){
   var sill=Part("Window pillar",coach,new Vector3(s*.7f,.98f,-.42f),new Vector3(.045f,.52f,.06f),metal);sill.transform.localRotation=Quaternion.Euler(0,0,s*13);
   Part("Door handle",coach,new Vector3(s*.952f,.65f,-.27f),new Vector3(.026f,.04f,.19f),metal);
   Part("Mirror stalk",coach,new Vector3(s*.97f,.83f,.62f),new Vector3(.23f,.05f,.06f),metal);
   Part("Mirror",coach,new Vector3(s*1.09f,.87f,.6f),new Vector3(.19f,.12f,.26f),paint);
   Part("Hood vent",coach,new Vector3(s*.51f,.66f,1.40f),new Vector3(.18f,.025f,.38f),metal);
   Part("Exhaust housing",coach,new Vector3(s*.65f,.32f,-2.14f),new Vector3(.25f,.13f,.15f),metal);
  }
  Part("Rear license plate",coach,new Vector3(0,.49f,-2.182f),new Vector3(.40f,.12f,.015f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road markings.mat"));
  if(!traffic){
   var tire=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Performance rubber.mat");
   foreach(var wheel in car.wheelVisuals){
    foreach(Transform child in wheel)child.gameObject.SetActive(false);
    MeshObject("Rounded tire",Torus(.28f,.08f,40,10),tire,wheel);
    MeshObject("Rim lip",Torus(.235f,.016f,40,8),AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Architectural concrete.mat"),wheel);
    for(int i=0;i<10;i++){var spoke=Part("Forged spoke",wheel,Vector3.zero,new Vector3(.18f,.026f,.44f),metal);spoke.transform.localRotation=Quaternion.Euler(i*36,0,0);}
    var disk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);disk.name="Brake disc";disk.transform.SetParent(wheel,false);disk.transform.localRotation=Quaternion.Euler(0,0,90);disk.transform.localScale=new Vector3(.39f,.025f,.39f);UnityEngine.Object.DestroyImmediate(disk.GetComponent<Collider>());disk.GetComponent<Renderer>().sharedMaterial=metal;
   }
  }
  foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=2;
 }
 static Mesh Loft(string name,float[] z,float[] w,float[] top){var v=new List<Vector3>();var tri=new List<int>();for(int j=0;j<z.Length;j++)v.AddRange(new[]{new Vector3(-w[j],.63f,z[j]),new Vector3(-w[j]*.82f,top[j],z[j]),new Vector3(w[j]*.82f,top[j],z[j]),new Vector3(w[j],.63f,z[j])});for(int j=0;j<z.Length-1;j++)for(int k=0;k<4;k++){int a=j*4+k,b=j*4+(k+1)%4;tri.AddRange(new[]{a,a+4,b,b,a+4,b+4});}tri.AddRange(new[]{0,1,2,0,2,3,12,14,13,12,15,14});return SaveMesh(name,v,tri);}
 static Mesh Torus(float radius,float tube,int segments,int sides){var v=new List<Vector3>();var tr=new List<int>();for(int i=0;i<=segments;i++)for(int j=0;j<=sides;j++){float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;float r=radius+tube*Mathf.Cos(b);v.Add(new Vector3(tube*1.75f*Mathf.Sin(b),r*Mathf.Cos(a),r*Mathf.Sin(a)));if(i<segments&&j<sides){int k=i*(sides+1)+j;tr.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});}}return SaveMesh("WheelProfile",v,tr);}
 static Mesh SaveMesh(string name,List<Vector3> v,List<int> tr){var m=new Mesh{name=name};m.SetVertices(v);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Root+"/"+name+(meshId++)+".asset");return m;}
 static GameObject MeshObject(string name,Mesh mesh,Material mat,Transform parent){var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=mat;return g;}
 static void Facades(){
  var tex=new Texture2D(128,256,TextureFormat.RGB24,true);var pixels=new Color[128*256];var rng=new System.Random(330);for(int y=0;y<256;y++)for(int x=0;x<128;x++){int cellX=x/8,cellY=y/12;float seed=Mathf.Repeat(Mathf.Sin(cellX*34.7f+cellY*57.1f)*4321,1);bool lit=x%8>1&&x%8<6&&y%12>2&&y%12<9&&seed>.30f;pixels[y*128+x]=lit?Color.Lerp(new Color(.08f,.32f,.5f),new Color(.8f,.47f,.15f),seed):new Color(.003f,.006f,.015f);}
  tex.SetPixels(pixels);tex.Apply();File.WriteAllBytes(Root+"/OfficeWindows.png",tex.EncodeToPNG());AssetDatabase.ImportAsset(Root+"/OfficeWindows.png");var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/OfficeWindows.png");
  var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name="Architectural glass windows";mat.SetTexture("_BaseMap",texture);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Metallic",.35f);mat.SetFloat("_Smoothness",.7f);mat.SetTexture("_EmissionMap",texture);mat.SetColor("_EmissionColor",Color.white*2);mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;mat.EnableKeyword("_EMISSION");mat.enableInstancing=true;AssetDatabase.CreateAsset(mat,"Assets/Materials/ArchitecturalWindows.mat");
  foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){
   if(r.name=="Window band")r.gameObject.SetActive(false);
   if(!r.name.StartsWith("Skyline tower"))continue;
   var p=r.transform.position;var s=r.transform.localScale;
   for(int side=-1;side<=1;side+=2){Part("Window facade",env,p+Vector3.forward*(s.z/2+.018f)*side,new Vector3(s.x*.94f,s.y*.94f,.025f),mat);Part("Window facade",env,p+Vector3.right*(s.x/2+.018f)*side,new Vector3(.025f,s.y*.94f,s.z*.94f),mat);}
  }
 }
 static void RoadEdges(){
  var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Architectural concrete.mat");var verts=new List<Vector3>();var tri=new List<int>();
  for(int side=-1;side<=1;side+=2){for(int i=0;i<440;i++){
   float t=i/440f,nt=(i+1)/440f;Vector3 p=NeonCoastBuilder.Point(t),n=NeonCoastBuilder.Point(nt);Vector3 r=Vector3.Cross(Vector3.up,(n-p).normalized)*side;
   int k=verts.Count;verts.AddRange(new[]{p+r*8,p+r*8-Vector3.up*.7f,n+r*8,n+r*8-Vector3.up*.7f});if(side==1)tri.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});else tri.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});
   if(p.y<3){var bank=Part("Coastal embankment",env,(p+n)*.5f+r*11-Vector3.up*.6f,new Vector3(6,1.2f,Vector3.Distance(p,n)+.1f),mat);bank.transform.rotation=Quaternion.LookRotation(n-p);}
  }}MeshObject("Viaduct road structure",SaveMesh("RoadFascia",verts,tri),mat,env);
  for(int i=0;i<18;i++){float t=.39f+i*.002f;var p=NeonCoastBuilder.Point(t);var f=(NeonCoastBuilder.Point(t+.001f)-p).normalized;var rib=Part("Tunnel ceiling rib",env,p+Vector3.up*5.45f,new Vector3(17,.2f,.18f),i%3==0?trim:metal);rib.transform.rotation=Quaternion.LookRotation(f);}
 }
 static void SurfaceDetail(){
  var tex=new Texture2D(256,256,TextureFormat.RGBA32,true);var pixels=new Color[256*256];for(int y=0;y<256;y++)for(int x=0;x<256;x++){float a=Mathf.PerlinNoise(x*.055f,y*.055f),b=Mathf.PerlinNoise(x*.055f+.06f,y*.055f),c=Mathf.PerlinNoise(x*.055f,y*.055f+.06f);pixels[y*256+x]=new Color(.5f+(a-b)*2,.5f+(a-c)*2,1,1);}tex.SetPixels(pixels);tex.Apply();File.WriteAllBytes(Root+"/AsphaltNormal.png",tex.EncodeToPNG());AssetDatabase.ImportAsset(Root+"/AsphaltNormal.png");var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/AsphaltNormal.png");importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();var road=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wet graphite asphalt.mat");road.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/AsphaltNormal.png"));road.SetFloat("_BumpScale",.45f);road.EnableKeyword("_NORMALMAP");road.SetFloat("_Smoothness",.8f);EditorUtility.SetDirty(road);
 }
 static void Signs(){
  var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/NeonCoastFont.asset");
  string[] text={"H A R B O R  /  0 1","PACIFIC EXPRESSWAY","ASTRA  //  NIGHT DRIVE","NEON DISTRICT"};
  for(int i=0;i<4;i++){float t=.12f+i*.22f;var p=NeonCoastBuilder.Point(t);var forward=(NeonCoastBuilder.Point(t+.001f)-p).normalized;var sign=new GameObject("District billboard").transform;sign.SetParent(env);sign.position=p+Vector3.Cross(Vector3.up,forward)*12+Vector3.up*6;sign.rotation=Quaternion.LookRotation(forward);
   Part("Sign panel",sign,Vector3.zero,new Vector3(11,2,.4f),metal);Part("Neon underline",sign,new Vector3(0,-.9f,-.24f),new Vector3(11,.09f,.08f),trim);
   var go=new GameObject("District label");go.transform.SetParent(sign,false);go.transform.localPosition=new Vector3(0,0,-.25f);var label=go.AddComponent<TextMeshPro>();label.font=font;label.text=text[i];label.fontSize=5;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.5f,1,1);label.rectTransform.sizeDelta=new Vector2(10,1.8f);
  }
 }
}
