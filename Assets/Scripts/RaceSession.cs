using UnityEngine;
namespace NeonCoast {
public class RaceSession : MonoBehaviour {
 public ArcadeCar player;
 public Transform[] checkpoints;
 public int totalLaps = 3;
 public GridRacer[] racers;
 public float Countdown { get; private set; } = 3.5f;
 public int Position { get; private set; } = 1;
 public int Lap { get; private set; } = 1;
 public int Next { get; private set; }
 public float Elapsed { get; private set; }
 public float BestLap { get; private set; } = float.PositiveInfinity;
 public bool Finished { get; private set; }
 public float RecordLap { get; private set; } = float.PositiveInfinity;
 public float RecordRace { get; private set; } = float.PositiveInfinity;
 public bool NewLapRecord { get; private set; }
 public bool NewRaceRecord { get; private set; }

 float lapStart, raceClock;
 GridRacer playerRacer;

 void Awake() {
  racers = FindObjectsByType<GridRacer>(FindObjectsSortMode.None);
  playerRacer = player ? player.GetComponent<GridRacer>() : null;

  foreach (var r in racers) {
   if (r) {
    var car = r.GetComponent<ArcadeCar>();
    if (car) car.controlsEnabled = false;
    r.passed = 0;
    r.next = 0;
    r.finishTime = -1f;
   }
  }

  RecordLap = PlayerPrefs.GetFloat("NCR_BestLap", float.PositiveInfinity);
  RecordRace = PlayerPrefs.GetFloat("NCR_BestRace", float.PositiveInfinity);

  if (GameMode.TimeTrial) {
   foreach (var d in FindObjectsByType<PaceDriver>(FindObjectsSortMode.None)) d.gameObject.SetActive(false);
   racers = new GridRacer[] { playerRacer };
  } else {
   RandomizeStartingGrid();
  }

  // Garante que o público de pedestres e torcida esteja presente no circuito
  if (!FindFirstObjectByType<TrackSpectators>()) {
   new GameObject("Track Spectators").AddComponent<TrackSpectators>();
  }
 }

 struct GridSlot {
  public Vector3 pos;
  public Quaternion rot;
  public GridSlot(Vector3 p, Quaternion r) { pos = p; rot = r; }
 }

 void RandomizeStartingGrid() {
  if (racers == null || racers.Length <= 1) return;

  var slots = new System.Collections.Generic.List<GridSlot>();
  foreach (var r in racers) {
   if (r) slots.Add(new GridSlot(r.transform.position, r.transform.rotation));
  }

  // Ordena os slots do 1º ao último baseado na proximidade da linha de largada
  if (checkpoints != null && checkpoints.Length > 0 && checkpoints[0] != null) {
   Vector3 cp0 = checkpoints[0].position;
   slots.Sort((a, b) => (a.pos - cp0).sqrMagnitude.CompareTo((b.pos - cp0).sqrMagnitude));
  }

  // Embaralha competidores (Fisher-Yates)
  var shuffled = new System.Collections.Generic.List<GridRacer>(racers);
  for (int i = shuffled.Count - 1; i > 0; i--) {
   int rnd = Random.Range(0, i + 1);
   var temp = shuffled[i];
   shuffled[i] = shuffled[rnd];
   shuffled[rnd] = temp;
  }

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

  int playerSlotIdx = shuffled.IndexOf(playerRacer);
  if (playerSlotIdx >= 0) Position = playerSlotIdx + 1;

  var cam = FindFirstObjectByType<ChaseCamera>();
  if (cam && player) {
   cam.transform.position = player.transform.position - player.transform.forward * 7.6f + Vector3.up * 2.3f;
   cam.transform.rotation = player.transform.rotation;
  }
 }

 void Update() {
  if (Countdown > 0) {
   Countdown -= Time.deltaTime;
   if (Countdown <= 0) {
    foreach (var r in racers) {
     if (r) {
      var car = r.GetComponent<ArcadeCar>();
      if (car) car.controlsEnabled = true;
     }
    }
   }
   return;
  }

  raceClock += Time.deltaTime;
  if (!Finished) Elapsed = raceClock;

  // ── Cálculo da posição em tempo real baseado exclusivamente no progresso na pista ──
  if (playerRacer != null && racers != null && racers.Length > 1) {
   int currentPos = 1;
   foreach (var r in racers) {
    if (!r || r == playerRacer) continue;

    // 1. Rival já cruzou a linha de chegada final (3 voltas) e jogador ainda não
    if (r.finishTime >= 0 && playerRacer.finishTime < 0) {
     currentPos++;
    }
    // 2. Ambos ainda disputando na pista
    else if (playerRacer.finishTime < 0 && r.finishTime < 0) {
     // Rival passou por mais checkpoints (está à frente em voltas ou setores)
     if (r.passed > playerRacer.passed) {
      currentPos++;
     }
     // No mesmo setor/checkpoint: compara proximidade ao próximo checkpoint
     else if (r.passed == playerRacer.passed && DistanceToNextCheckpoint(r) < DistanceToNextCheckpoint(playerRacer)) {
      currentPos++;
     }
    }
    // 3. Ambos já finalizaram: compara tempo final oficial
    else if (playerRacer.finishTime >= 0 && r.finishTime >= 0 && r.finishTime < playerRacer.finishTime) {
     currentPos++;
    }
   }
   Position = Mathf.Clamp(currentPos, 1, racers.Length);
  }
 }

 float DistanceToNextCheckpoint(GridRacer r) {
  if (checkpoints == null || checkpoints.Length == 0 || r == null) return 0f;
  int nextIdx = Mathf.Clamp(r.next, 0, checkpoints.Length - 1);
  if (checkpoints[nextIdx] == null) return 0f;
  return (r.transform.position - checkpoints[nextIdx].position).sqrMagnitude;
 }

 public void Cross(int index, ArcadeCar car) {
  if (!car) return;
  var racer = car.GetComponent<GridRacer>();

  // Validação rigorosa individual por piloto:
  // 1. Deve ser um competidor registrado
  // 2. A largada já deve ter ocorrido
  // 3. O piloto ainda não finalizou a prova
  // 4. O checkpoint cruzado DEVE ser exatamente o esperado (racer.next)
  if (!racer || Countdown > 0 || racer.finishTime >= 0 || index != racer.next) return;

  var p = checkpoints[index];
  if (!p) return;

  // 5. Validação de vetor direcional (evita corte em sentido contrário)
  if (Vector3.Dot(car.transform.forward, p.forward) < 0.10f) return;

  // 6. Validação de proximidade física (anti-teleporte / anti-exploit)
  if (Vector3.Distance(car.transform.position, p.position) > 22f) return;

  // Registra o spawn de recuperação oficial deste carro
  car.SetSpawn(p.position + p.forward * 4.5f - Vector3.up * 0.8f, p.rotation);

  // Avança o piloto individual
  racer.passed++;
  racer.next = (racer.next + 1) % checkpoints.Length;

  int finishIndex = checkpoints.Length - 1; // Checkpoint 7 = Linha de Chegada

  // Se o piloto completou todas as voltas estipuladas
  if (racer.passed >= totalLaps * checkpoints.Length) {
   racer.finishTime = raceClock;
  }

  // ── Atualização isolada para o Jogador ─────────────────────────────
  if (car != player) return;

  Next = racer.next;
  int completedLaps = racer.passed / checkpoints.Length;
  Lap = Mathf.Clamp(1 + completedLaps, 1, totalLaps);

  // Computa tempo de volta quando cruza a linha de chegada (Checkpoint final)
  if (index == finishIndex && completedLaps > 0) {
   float currentLapTime = Elapsed - lapStart;
   if (currentLapTime > 5f) { // Evita falsos registros iniciais
    BestLap = Mathf.Min(BestLap, currentLapTime);
   }
   lapStart = Elapsed;
  }

  Finished = racer.finishTime >= 0;
  if (Finished) {
   if (BestLap < RecordLap) {
    RecordLap = BestLap;
    NewLapRecord = true;
    PlayerPrefs.SetFloat("NCR_BestLap", BestLap);
   }
   if (Elapsed < RecordRace) {
    RecordRace = Elapsed;
    NewRaceRecord = true;
    PlayerPrefs.SetFloat("NCR_BestRace", Elapsed);
   }
   PlayerPrefs.Save();
  }
 }
}
}
