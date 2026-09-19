using UnityEngine;
namespace NeonCoast {
[RequireComponent(typeof(ArcadeCar))]
public class Skidmarks : MonoBehaviour {
 ArcadeCar car; TrailRenderer[] trails;
 void Start(){
  car=GetComponent<ArcadeCar>();
  if(car.automation){Destroy(this);return;}
  trails=new TrailRenderer[2];
  var mat=new Material(Shader.Find("Sprites/Default"));
  for(int i=0;i<2;i++){
   var go=new GameObject("Skid "+i);go.transform.SetParent(transform,false);
   var t=go.AddComponent<TrailRenderer>();
   t.time=6;t.widthMultiplier=.2f;t.minVertexDistance=.25f;
   t.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;t.receiveShadows=false;
   t.emitting=false;t.material=mat;t.numCornerVertices=2;t.numCapVertices=2;
   t.startColor=new Color(.02f,.025f,.03f,.7f);t.endColor=new Color(.02f,.025f,.03f,.05f);
   trails[i]=t;
  }
 }
 void LateUpdate(){
  if(!car||trails==null)return;
  bool skid=car.Drifting&&car.SpeedKmh>12;
  for(int i=0;i<2;i++){
   trails[i].emitting=skid;
   if(car.wheels[i+2].GetGroundHit(out WheelHit hit)) trails[i].transform.position=hit.point+Vector3.up*.015f;
  }
 }
 void OnDestroy(){if(trails!=null)foreach(var t in trails)if(t&&t.material)Destroy(t.material);}
}
}
