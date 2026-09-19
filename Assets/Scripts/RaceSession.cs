using UnityEngine;
namespace NeonCoast {
public class RaceSession : MonoBehaviour {
 public ArcadeCar player;
 public Transform[] checkpoints;
 public int totalLaps=3;
 public GridRacer[] racers;
 public float Countdown {get;private set;}=3.5f;
 public int Position {get;private set;}=1;
 public int Lap {get;private set;}=1;
 public int Next {get;private set;}
 public float Elapsed {get;private set;}
 public float BestLap {get;private set;}=float.PositiveInfinity;
 public bool Finished {get;private set;}
 public float RecordLap {get;private set;}=float.PositiveInfinity;
 public float RecordRace {get;private set;}=float.PositiveInfinity;
 public bool NewLapRecord {get;private set;}
 public bool NewRaceRecord {get;private set;}
 float lapStart, raceClock; GridRacer playerRacer;
 void Awake(){
  racers=FindObjectsByType<GridRacer>(FindObjectsSortMode.None);playerRacer=player.GetComponent<GridRacer>();
  foreach(var r in racers)r.GetComponent<ArcadeCar>().controlsEnabled=false;
  RecordLap=PlayerPrefs.GetFloat("NCR_BestLap",float.PositiveInfinity);RecordRace=PlayerPrefs.GetFloat("NCR_BestRace",float.PositiveInfinity);
  if(GameMode.TimeTrial){foreach(var d in FindObjectsByType<PaceDriver>(FindObjectsSortMode.None))d.gameObject.SetActive(false);racers=new GridRacer[]{playerRacer};}
  else { RandomizeStartingGrid(); }
 }

 struct GridSlot {
  public Vector3 pos; public Quaternion rot;
  public GridSlot(Vector3 p, Quaternion r) { pos = p; rot = r; }
 }

 void RandomizeStartingGrid() {
  if (racers == null || racers.Length <= 1) return;

  // 1. Registra os slots físicos de largada existentes
  var slots = new System.Collections.Generic.List<GridSlot>();
  foreach (var r in racers) {
   if (r) slots.Add(new GridSlot(r.transform.position, r.transform.rotation));
  }

  // 2. Ordena os slots do 1º (pole) ao último baseado na distância da largada
  if (checkpoints != null && checkpoints.Length > 0 && checkpoints[0] != null) {
   Vector3 cp0 = checkpoints[0].position;
   slots.Sort((a, b) => (a.pos - cp0).sqrMagnitude.CompareTo((b.pos - cp0).sqrMagnitude));
  }

  // 3. Embaralha os pilotos aleatoriamente (Fisher-Yates)
  var shuffled = new System.Collections.Generic.List<GridRacer>(racers);
  for (int i = shuffled.Count - 1; i > 0; i--) {
   int rnd = Random.Range(0, i + 1);
   var temp = shuffled[i];
   shuffled[i] = shuffled[rnd];
   shuffled[rnd] = temp;
  }

  // 4. Aplica os slots sorteados para cada competidor
  for (int i = 0; i < shuffled.Count && i < slots.Count; i++) {
   var r = shuffled[i];
   var slot = slots[i];
   if (!r) continue;

   r.transform.SetPositionAndRotation(slot.pos, slot.rot);

   var car = r.GetComponent<ArcadeCar>();
   if (car) car.SetSpawn(slot.pos, slot.rot);

   var rb = r.GetComponent<Rigidbody>();
   if (rb) {
    rb.position = slot.pos;
    rb.rotation = slot.rot;
    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;
   }
  }

  // 5. Define a posição inicial no placar (P1, P2, P3, P4)
  int playerSlotIdx = shuffled.IndexOf(playerRacer);
  if (playerSlotIdx >= 0) Position = playerSlotIdx + 1;

  // 6. Alinha a câmera atrás do carro do jogador em sua nova posição
  var cam = FindFirstObjectByType<ChaseCamera>();
  if (cam && player) {
   cam.transform.position = player.transform.position - player.transform.forward * 7.6f + Vector3.up * 2.3f;
   cam.transform.rotation = player.transform.rotation;
  }
 }
 void Update(){
  if(Countdown>0){Countdown-=Time.deltaTime;if(Countdown<=0)foreach(var r in racers)r.GetComponent<ArcadeCar>().controlsEnabled=true;return;}
  raceClock+=Time.deltaTime;if(!Finished)Elapsed=raceClock;
  Position=1;foreach(var r in racers){if(r==playerRacer)continue;
   if(r.finishTime>=0){if(playerRacer.finishTime<0||r.finishTime<playerRacer.finishTime)Position++;}
   else if(playerRacer.finishTime<0&&(r.passed>playerRacer.passed||(r.passed==playerRacer.passed&&Distance(r)<Distance(playerRacer))))Position++;
  }
 }
 float Distance(GridRacer r){return (r.transform.position-checkpoints[r.next].position).sqrMagnitude;}
 public void Cross(int index,ArcadeCar car){
  var racer=car.GetComponent<GridRacer>();
  if(!racer||Countdown>0||racer.finishTime>=0||index!=racer.next)return;
  var p=checkpoints[index];if(Vector3.Dot(car.transform.forward,p.forward)<.2f)return;
  car.SetSpawn(p.position+p.forward*5-Vector3.up*.8f,p.rotation);
  racer.passed++;racer.next=(racer.next+1)%checkpoints.Length;
  if(racer.passed>=totalLaps*checkpoints.Length)racer.finishTime=raceClock;
  if(car!=player)return;
  Next=racer.next;Lap=Mathf.Min(totalLaps,1+racer.passed/checkpoints.Length);
  if(Next==0){BestLap=Mathf.Min(BestLap,Elapsed-lapStart);lapStart=Elapsed;}
  Finished=racer.finishTime>=0;
  if(Finished){if(BestLap<RecordLap){RecordLap=BestLap;NewLapRecord=true;PlayerPrefs.SetFloat("NCR_BestLap",BestLap);}if(Elapsed<RecordRace){RecordRace=Elapsed;NewRaceRecord=true;PlayerPrefs.SetFloat("NCR_BestRace",Elapsed);}PlayerPrefs.Save();}
 }
}
}

