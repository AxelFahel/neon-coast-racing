using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class CoastalRefinement {
 static Transform root;static Material leaves,bark,stone,concrete;
 [MenuItem("Neon Coast/Refine Coastal Environment")]
 public static void Apply(){
  if(GameObject.Find("Coastal Refinement"))throw new System.Exception("Already refined; preserved.");
  Directory.CreateDirectory("Assets/Environment/CoastalRefinement");AssetDatabase.Refresh();root=new GameObject("Coastal Refinement").transform;
  // Correct the service barriers without closing the junction or touching the race spline.
  foreach(var c in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))if(c.name=="Service boundary"){c.transform.position=new Vector3(-72,c.transform.position.y,c.transform.position.z);c.transform.localScale=new Vector3(36,c.transform.localScale.y,c.transform.localScale.z);}
  var water=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Midnight ocean.mat");water.shader=Shader.Find("NeonCoast/CoastalWater");water.SetColor("_BaseColor",new Color(.018f,.045f,.075f));EditorUtility.SetDirty(water);
  leaves=NewMat("Palm leaves",new Color(.10f,.23f,.15f),.25f);bark=NewMat("Palm bark",new Color(.23f,.17f,.12f),.18f);stone=NewMat("Seawall stone",new Color(.14f,.19f,.24f),.38f);concrete=NewMat("Promenade stone",new Color(.18f,.21f,.25f),.3f);
  foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){if(r.name=="Palm trunk"||r.name=="Palm frond")r.gameObject.SetActive(false);if(r.name=="Coastal foundation"||r.name=="Coastal promenade")r.sharedMaterial=concrete;}
  var treeMesh=PalmMesh();AssetDatabase.CreateAsset(treeMesh,"Assets/Environment/CoastalRefinement/Palm.asset");
  for(int i=0;i<22;i++){
   var tree=new GameObject("Coastal palm",typeof(MeshFilter),typeof(MeshRenderer));tree.transform.SetParent(root,false);tree.transform.position=new Vector3(-137,-.4f,-185+i*17);tree.transform.rotation=Quaternion.Euler(0,i*137.5f,0);float scale=.85f+Mathf.Repeat(i*.37f,.4f);tree.transform.localScale=Vector3.one*scale;tree.GetComponent<MeshFilter>().sharedMesh=treeMesh;tree.GetComponent<MeshRenderer>().sharedMaterials=new[]{bark,leaves};
   for(int j=0;j<2;j++){var bench=Box("Promenade bench",new Vector3(-145,.05f,-185+i*17+j*4),new Vector3(1.1f,.25f,2.5f),bark);Box("Bench support",bench.transform.position+Vector3.down*.3f,new Vector3(.8f,.5f,1.8f),stone);}
  }
  var vs=new List<Vector3>();var ts=new List<int>();for(int i=0;i<=70;i++){float z=-260+i*8;vs.Add(new Vector3(-148,-.3f,z));vs.Add(new Vector3(-162+Mathf.Sin(i*.7f)*1.7f,-1.4f,z));if(i<70){int k=i*2;ts.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}}
  var shore=new Mesh{name="Natural seawall transition"};shore.SetVertices(vs);shore.SetTriangles(ts,0);shore.RecalculateNormals();AssetDatabase.CreateAsset(shore,"Assets/Environment/CoastalRefinement/Shore.asset");var coast=new GameObject("Rock coast",typeof(MeshFilter),typeof(MeshRenderer));coast.transform.parent=root;coast.GetComponent<MeshFilter>().sharedMesh=shore;coast.GetComponent<MeshRenderer>().sharedMaterial=stone;
  // Small warm pools of illumination at ground level, without extra real-time shadows.
  for(int i=0;i<12;i++){float z=-180+i*30;var lamp=Box("Promenade bollard",new Vector3(-146,.25f,z),new Vector3(.18f,1.4f,.18f),stone);var l=new GameObject("Walkway light").AddComponent<Light>();l.transform.parent=root;l.transform.position=lamp.transform.position+Vector3.up*.8f;l.type=LightType.Point;l.color=new Color(1,.57f,.27f);l.intensity=1.6f;l.range=8;l.shadows=LightShadows.None;}
  // Keep the user's sharp camera and HUD; adjust environment tones only.
  var road=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wet graphite asphalt.mat");road.SetFloat("_BumpScale",.12f);road.SetFloat("_Metallic",.07f);road.SetFloat("_Smoothness",.7f);road.SetColor("_BaseColor",new Color(.12f,.14f,.17f));EditorUtility.SetDirty(road);
  foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.name=="Road illumination"){light.color=new Color(1,.78f,.53f);light.intensity=28;light.range=29;}else if(light.name=="Moon cool key"){light.intensity=1.6f;light.color=new Color(.58f,.7f,1);}
  RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.3f,.36f,.48f);var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.12f,.17f,.25f));RenderSettings.ambientProbe=sh;
  foreach(var r in root.GetComponentsInChildren<MeshRenderer>())GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);
  EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);AssetDatabase.SaveAssets();
 }
 static Material NewMat(string name,Color c,float smooth){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Cull",0);m.enableInstancing=true;AssetDatabase.CreateAsset(m,"Assets/Materials/"+name+".mat");return m;}
 static GameObject Box(string name,Vector3 p,Vector3 s,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.position=p;g.transform.localScale=s;Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=m;return g;}
 static Mesh PalmMesh(){
  var v=new List<Vector3>();var barkTris=new List<int>();var leafTris=new List<int>();
  for(int y=0;y<=12;y++)for(int a=0;a<=10;a++){float h=y/12f;float angle=a*Mathf.PI*2/10;float r=Mathf.Lerp(.19f,.085f,h);v.Add(new Vector3(h*h*.75f+Mathf.Cos(angle)*r,h*6.5f,Mathf.Sin(angle)*r));if(y<12&&a<10){int k=y*11+a;barkTris.AddRange(new[]{k,k+11,k+1,k+1,k+11,k+12});}}
  for(int branch=0;branch<9;branch++){float theta=branch*Mathf.PI*2/9;var direction=new Vector3(Mathf.Cos(theta),0,Mathf.Sin(theta));var side=Vector3.Cross(Vector3.up,direction);var crown=new Vector3(.75f,6.5f,0);
   for(int s=0;s<14;s++){float t=s/14f,t1=(s+1)/14f;var a=crown+direction*t*3.8f+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.8f-t*t*1.7f);var b=crown+direction*t1*3.8f+Vector3.up*(Mathf.Sin(t1*Mathf.PI)*.8f-t1*t1*1.7f);float width=Mathf.Sin((t+.08f)*Mathf.PI)*.48f;int k=v.Count;v.AddRange(new[]{a,b,(a+b)*.5f+side*width-direction*.2f,(a+b)*.5f-side*width-direction*.2f});leafTris.AddRange(new[]{k,k+1,k+2,k+1,k,k+3});}
  }
  var mesh=new Mesh{name="Tapered coastal palm"};mesh.SetVertices(v);mesh.subMeshCount=2;mesh.SetTriangles(barkTris,0);mesh.SetTriangles(leafTris,1);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
}
