using UnityEngine;
namespace NeonCoast {
public class Checkpoint : MonoBehaviour {
 public RaceSession race; public int index;
 void OnTriggerEnter(Collider other){var car=other.GetComponentInParent<ArcadeCar>();if(car)race.Cross(index,car);}
}
}
