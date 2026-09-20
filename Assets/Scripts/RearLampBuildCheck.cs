using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace NeonCoast {
// Opt-in standalone smoke test; inactive during ordinary play.
public class RearLampBuildCheck : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Install() {
  if (Array.IndexOf(Environment.GetCommandLineArgs(), "--ncr-check-lamps") < 0) return;
  var go=new GameObject("Rear lamp build verification");DontDestroyOnLoad(go);go.AddComponent<RearLampBuildCheck>();
 }
 static void Capture(string path) {
  var cam=Camera.main;var previous=cam.targetTexture;var active=RenderTexture.active;
  var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
  var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
  try { cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG()); }
  finally {cam.targetTexture=previous;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(image);}
 }
 IEnumerator Start() {
  Application.runInBackground=true;
  SceneManager.LoadScene("NeonCoast");
  yield return null;
  yield return new WaitForSeconds(5);
  var session=FindFirstObjectByType<RaceSession>();
  var car=session.player;
  car.automation=true;car.testThrottle=0;car.controlsEnabled=true;
  string dir=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Validation");Directory.CreateDirectory(dir);
  yield return new WaitForSeconds(1);
  yield return new WaitForEndOfFrame();
  Capture(Path.Combine(dir,"rear-running.png"));
  yield return new WaitForSeconds(1);
  var lamp=car.bodyVisual.Find("Center stop lamp").GetComponent<Renderer>();
  bool idleOff=!lamp.enabled;
  car.controlsEnabled=false;
  yield return new WaitForSeconds(1);
  yield return new WaitForEndOfFrame();
  Capture(Path.Combine(dir,"rear-braking.png"));
  var led=car.bodyVisual.Find("Tail light bar").GetComponent<Renderer>();
  bool pass=idleOff && lamp.enabled && led.sharedMaterial.shader.name=="NeonCoast/RearLamp" && led.sharedMaterial.shader.isSupported;
  File.WriteAllText(Path.Combine(dir,"result.txt"),"PASS="+pass+"; idleOff="+idleOff+"; brakeOn="+lamp.enabled+"; shader="+led.sharedMaterial.shader.name);
  yield return new WaitForSeconds(2);
  Application.Quit(pass?0:1);
 }
}
}
