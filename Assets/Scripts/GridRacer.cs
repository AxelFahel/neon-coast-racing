using UnityEngine;
namespace NeonCoast {
public class GridRacer : MonoBehaviour {
 public string driverName="Driver";
 [System.NonSerialized] public int passed;
 [System.NonSerialized] public int next;
 [System.NonSerialized] public float finishTime=-1;
 public ArcadeCar Car {get;private set;}
 void Awake(){Car=GetComponent<ArcadeCar>();}
}
}
