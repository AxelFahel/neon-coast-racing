using UnityEngine;
namespace NeonCoast {
[DefaultExecutionOrder(-20)]
public class PaceDriver : MonoBehaviour {
 public Vector3[] path;
 public float cruiseSpeed = 21f;
 public float laneOffset;
 public bool traffic;
 public float aggression = .5f;

 public int Recoveries { get; private set; }

 ArcadeCar   car;
 ArcadeCar[] vehicles;
 float lane, stuckTime;
 int   closest;

 // How long the car must be nearly stopped before a recovery is triggered
 const float StuckTimeThreshold = 8f;
 // Lookahead base distance (metres) used when projecting the target point
 const float LookaheadBase      = 7f;
 // Width of the collision avoidance tunnel (half-width each side)
 const float TunnelHalfWidth    = 2.6f;
 const float TunnelHalfHeight   = 3f;
 const float TunnelLength       = 22f;

 void Awake() {
  car = GetComponent<ArcadeCar>();
  car.automation = true;
  lane = laneOffset;
 }

 void Start() {
  RefreshVehicleList();
 }

 /// <summary>Re-scans the scene for ArcadeCar instances.
 /// Call this whenever cars are spawned or destroyed at runtime.</summary>
 public void RefreshVehicleList() {
  vehicles = FindObjectsByType<ArcadeCar>(FindObjectsSortMode.None);
 }

 void Update() {
  if (path == null || path.Length < 3) return;

  // ── 1. Find the closest path node ────────────────────────────────────
  float best = float.MaxValue;
  for (int i = 0; i < path.Length; i++) {
   float d = (path[i] - transform.position).sqrMagnitude;
   if (d < best) { best = d; closest = i; }
  }

  // ── 2. Obstacle / lane-change logic ──────────────────────────────────
  float desiredLane   = laneOffset;
  float obstacleLimit = cruiseSpeed;

  foreach (var other in vehicles) {
   if (!other || other == car) continue;
   var localOther = transform.InverseTransformPoint(other.transform.position);

   bool inTunnel = localOther.z > 0 && localOther.z < TunnelLength &&
                   Mathf.Abs(localOther.x) < TunnelHalfWidth &&
                   Mathf.Abs(localOther.y) < TunnelHalfHeight;
   if (!inTunnel) continue;

   // Slow to match the vehicle ahead, with some spacing
   obstacleLimit = Mathf.Min(obstacleLimit,
    Mathf.Max(0, other.ForwardSpeed) + (localOther.z - 8) * .65f);

   // Attempt a lane change if not in traffic mode and there's enough gap
   if (!traffic && localOther.z > 6) {
    float candidate = laneOffset > 0 ? -3.1f : 3.1f;
    if (IsLaneFree(candidate)) desiredLane = candidate;
   }
  }

  // Gradually drift toward the desired lane
  lane = Mathf.MoveTowards(lane, desiredLane, Time.deltaTime * (traffic ? .7f : 1.6f));

  // ── 3. Lookahead target point ─────────────────────────────────────────
  int next      = (closest + 1) % path.Length;
  float remaining = LookaheadBase + Mathf.Abs(car.ForwardSpeed) * .65f;
  for (int step = 0; step < path.Length; step++) {
   remaining -= Vector3.Distance(path[next], path[(next + 1) % path.Length]);
   if (remaining <= 0) break;
   next = (next + 1) % path.Length;
  }
  var tangent = (path[(next + 1) % path.Length] - path[(next + path.Length - 1) % path.Length]).normalized;
  Vector3 aim   = path[next] + Vector3.Cross(Vector3.up, tangent).normalized * lane;
  Vector3 local = transform.InverseTransformPoint(aim);

  // ── 4. Steering ───────────────────────────────────────────────────────
  // Pure-pursuit lateral error → heading angle
  float pursuitAngle = Mathf.Atan2(2f * 2.63f * local.x,
                        Mathf.Max(1f, local.x * local.x + local.z * local.z)) * Mathf.Rad2Deg;
  float maxSteer = Mathf.Lerp(car.steerAngleMax, car.steerAngleMin,
                    Mathf.Clamp01(Mathf.Abs(car.ForwardSpeed) / car.topSpeed));
  car.testSteer = Mathf.Clamp(pursuitAngle / maxSteer, -1, 1);

  // ── 5. Speed control ──────────────────────────────────────────────────
  float target = cruiseSpeed * Mathf.Lerp(1f, .55f, Mathf.Clamp01(Mathf.Abs(pursuitAngle) / 20f));
  if (Mathf.Abs(lane - desiredLane) > 1f || traffic) target = Mathf.Min(target, obstacleLimit);
  if (obstacleLimit < 2f) target = 0f;

  car.testThrottle = car.ForwardSpeed > target + .5f ? -1 :
                     car.ForwardSpeed < target       ?  1 : 0;

  // Nitro only when straight, fast, and aggressive
  car.aiBoost = !traffic &&
                Mathf.Abs(pursuitAngle) < 2.5f &&
                car.SpeedKmh > 55f &&
                car.Nitro > .45f &&
                obstacleLimit >= cruiseSpeed &&
                aggression > .6f;

  // ── 6. Stuck detection & recovery ────────────────────────────────────
  if (car.controlsEnabled && car.SpeedKmh < 2f)
   stuckTime += Time.deltaTime;
  else
   stuckTime = 0;

  bool fellOffTrack = transform.position.y < path[closest].y - 5f;
  if (stuckTime > StuckTimeThreshold || fellOffTrack) {
   var forward = (path[(closest + 1) % path.Length] - path[closest]).normalized;
   car.SetSpawn(
    path[closest] + Vector3.Cross(Vector3.up, forward) * laneOffset + Vector3.up * .7f,
    Quaternion.LookRotation(forward));
   car.ResetCar();
   stuckTime = 0;
   Recoveries++;
  }
 }

 /// <summary>Returns true when the given lane offset is clear of other vehicles
 /// within the look-around radius.</summary>
 bool IsLaneFree(float candidateLane) {
  foreach (var v in vehicles) {
   if (!v || v == car) continue;
   var p = transform.InverseTransformPoint(v.transform.position);
   if (Mathf.Abs(p.z) < 13f && Mathf.Abs(p.x - (candidateLane - lane)) < TunnelHalfWidth)
    return false;
  }
  return true;
 }

 void OnDisable() {
  if (!car) return;
  car.automation   = false;
  car.testThrottle = 0;
  car.testSteer    = 0;
  car.aiBoost      = false;
 }
}
}
