using System.Collections;
using System.IO;
using UnityEngine;
namespace NeonCoast {
public class DrivingValidation : MonoBehaviour {
 IEnumerator Start(){
  var car=FindFirstObjectByType<ArcadeCar>();var rb=car.GetComponent<Rigidbody>();car.automation=true;car.ResetCar();
  yield return new WaitForSeconds(1);
  bool ground=true;foreach(var w in car.wheels)ground &= w.isGrounded;
  car.testThrottle=1;yield return new WaitForSeconds(3);float acceleration=car.SpeedKmh;
  car.testThrottle=-1;yield return new WaitForSeconds(.8f);float braking=car.SpeedKmh;
  yield return new WaitForSeconds(3);float reverse=car.ForwardSpeed;
  car.ResetCar();car.testThrottle=1;car.testSteer=.5f;float yaw=car.transform.eulerAngles.y;
  yield return new WaitForSeconds(2);float steering=Mathf.Abs(Mathf.DeltaAngle(yaw,car.transform.eulerAngles.y));
  car.testSteer=0;car.testThrottle=0;
  rb.position=new Vector3(-100,.2f,-95);rb.rotation=Quaternion.Euler(0,90,0);rb.linearVelocity=Vector3.right*20;
  yield return new WaitForSeconds(1);bool collision=rb.position.x < -91.5f && rb.linearVelocity.x < 4;
  car.ResetCar();car.testThrottle=0;car.testSteer=0;car.automation=false;
  var text="Grounded: "+ground+"\nAcceleration after 3s (km/h): "+acceleration+"\nAfter braking (km/h): "+braking+"\nReverse (m/s): "+reverse+"\nSteering yaw change: "+steering+"\nBarrier held: "+collision+"\nPASS="+(ground&&acceleration>15&&braking<acceleration&&reverse<-.5f&&steering>5&&collision);
  File.WriteAllText(Path.Combine(Application.dataPath,"../DrivingValidation.txt"),text);Debug.Log(text);
  Destroy(this);
 }
}
}
