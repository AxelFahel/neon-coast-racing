using UnityEngine;
namespace NeonCoast {
[RequireComponent(typeof(Rigidbody))]
public class ArcadeCar : MonoBehaviour {
 public WheelCollider[] wheels;
 public Transform[] wheelVisuals;
 public Transform bodyVisual;
 public float topSpeed = 60f;

 public bool automation;
 public bool controlsEnabled = true, aiBoost, aiHandbrake;
 public float testThrottle, testSteer;
 public float SpeedKmh    => rb ? rb.linearVelocity.magnitude * 3.6f : 0;
 public float ForwardSpeed => rb ? Vector3.Dot(rb.linearVelocity, transform.forward) : 0;
 public float Nitro   { get; private set; } = 1;
 public bool  Boosting { get; private set; }
 public bool  Drifting { get; private set; }
 public bool  Paused   { get; set; }
 public float torqueMult    = 1.0f;
 public float steerAngleMax = 31f, steerAngleMin = 9f;
 public float driftNitroMult = 1.0f, handbrakeGrip = 0.65f;
 public string vehicleName = "ASTER GT";
 public bool Slipstreaming { get; set; }

 static readonly System.Collections.Generic.HashSet<ArcadeCar> activeCars = new System.Collections.Generic.HashSet<ArcadeCar>();
 Renderer centerStopLamp;
 Light tailGlow;
 void OnEnable() { activeCars.Add(this); }

 Rigidbody rb;
 float throttle, steer, smoothedSteer;
 bool handbrake, boost;
 Vector3 spawn; Quaternion spawnRotation;
 readonly System.Collections.Generic.List<Material> tailLightMats = new System.Collections.Generic.List<Material>();
 Material playerPaint;

 // ── Physics constants calibradas para pilotagem esportiva arcade ───────────
 const float RbMass           = 1350f;
 const float DownforceMult    = 1.35f;  // Downforce alto = estabilidade em alta velocidade
 const float DragMult         = 0.30f;
 const float MotorTorqueBase  = 1400f;
 const float MotorTorqueBoost = 2100f;
 const float BrakeTorque      = 3500f;
 const float HandbrakeTorque  = 2400f;
 const float IdleBrakeTorque  = 55f;
 const float SidewaysFriction = 2.6f;   // Alta aderência lateral padrão
 const float ForwardFriction  = 1.9f;   // Alta tração longitudinal
 const float SteerSmoothing   = 4.5f;   // Resposta de volante ágil e suave
 const float BodyTiltSpeed    = 5.5f;

 public void SetSpawn(Vector3 p, Quaternion q) { spawn = p; spawnRotation = q; }

 void Awake() {
  rb = GetComponent<Rigidbody>();
  rb.mass = RbMass;
  rb.centerOfMass = new Vector3(0, 0.08f, 0); // Centro de gravidade baixo = não capota e não desliza
  rb.interpolation = RigidbodyInterpolation.Interpolate;
  rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
  spawn = transform.position;
  spawnRotation = transform.rotation;
  CleanExcessiveLights();
 }

 void Start() {
  CleanExcessiveLights();
  if (!automation) ApplyVehicleSpecs();
  else if (GetComponent<GridRacer>() != null) ApplyRivalSpecs();
  else ApplyTrafficSpecs();

  foreach (var renderer in GetComponentsInChildren<Renderer>(true))
   if (renderer.name == "Center stop lamp") centerStopLamp = renderer;
  foreach (var light in GetComponentsInChildren<Light>(true))
   if (light.name == "Car_TailGlow") tailGlow = light;

  ApplyWheelFriction();
 }

 void ApplyWheelFriction() {
  if (wheels == null || wheels.Length == 0) return;
  foreach (var w in wheels) {
   if (!w) continue;
   var sf = w.sidewaysFriction;
   sf.extremumSlip   = 0.16f;
   sf.extremumValue  = 1.0f;
   sf.asymptoteSlip  = 0.32f;
   sf.asymptoteValue = 0.85f;
   sf.stiffness      = SidewaysFriction;
   w.sidewaysFriction = sf;

   var ff = w.forwardFriction;
   ff.extremumSlip   = 0.11f;
   ff.extremumValue  = 1.0f;
   ff.asymptoteSlip  = 0.26f;
   ff.asymptoteValue = 0.80f;
   ff.stiffness      = ForwardFriction;
   w.forwardFriction = ff;
  }
 }

 public void ApplyVehicleSpecs() {
  var v = VehicleRegistry.GetSelectedVehicle();
  var p = VehicleRegistry.GetSelectedPaint();
  vehicleName    = v.name;
  topSpeed       = v.topSpeed;
  torqueMult     = v.torqueMult;
  steerAngleMax  = v.steerAngleMax;
  steerAngleMin  = v.steerAngleMin;
  driftNitroMult = v.driftNitroMult;
  handbrakeGrip  = v.handbrakeGrip;

  // Reconstrói a carroceria exclusiva do modelo escolhido com a pintura selecionada
  CarVisualsOverhaul.RebuildCarVisuals(this, v, p);

  // Localiza materiais de lanterna para modulação de freio
  tailLightMats.Clear();
  foreach (var mr in GetComponentsInChildren<MeshRenderer>(true)) {
   if (mr.name == "Tail light bar") {
    var tailLightMat = new Material(mr.sharedMaterial);
    tailLightMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
    tailLightMat.EnableKeyword("_EMISSION");
    tailLightMats.Add(tailLightMat);
    mr.material = tailLightMat;
   }
  }

  CleanExcessiveLights();

  // Faróis direcionais para a pista
  if (transform.Find("Car_Headlight_L") == null) {
   CreateHeadlight("Car_Headlight_L", new Vector3(-0.68f, 0.68f, 2.18f));
   CreateHeadlight("Car_Headlight_R", new Vector3( 0.68f, 0.68f, 2.18f));
  }

  // Neon de chassi (Underglow na cor da pintura selecionada)
  var underglow = transform.Find("Car_Underglow");
  if (!underglow) {
   var underglowGO = new GameObject("Car_Underglow");
   underglowGO.transform.SetParent(transform, false);
   underglowGO.transform.localPosition = new Vector3(0, 0.15f, 0);
   var underLight = underglowGO.AddComponent<Light>();
   underLight.type = LightType.Point;
   underLight.range = 3.6f;
   underLight.intensity = 0.85f;
   underLight.color = p.color;
   underLight.shadows = LightShadows.None;
  } else {
   var underLight = underglow.GetComponent<Light>();
   if (underLight) underLight.color = p.color;
  }
 }

 void ApplyRivalSpecs() {
  var racer = GetComponent<GridRacer>();
  string rName = racer ? racer.driverName : "Rival";
  Color rivalColor = Color.white;
  string vehId = "aster_gt";

  if (rName.Contains("KAI")) {
   rivalColor = new Color(0.98f, 0.32f, 0.05f); // Laranja Neon Drift
   vehId = "shinobi_rspec";
  } else if (rName.Contains("NOVA")) {
   rivalColor = new Color(0.72f, 0.12f, 0.98f); // Violeta Hyper
   vehId = "valkyrie_apex";
  } else {
   rivalColor = new Color(0.04f, 0.92f, 0.62f); // Esmeralda Ciano
   vehId = "aster_gt";
  }

  VehicleData vData = null;
  foreach (var v in VehicleRegistry.Vehicles) {
   if (v.id == vehId) { vData = v; break; }
  }
  if (vData == null) vData = VehicleRegistry.Vehicles[0];

  var pData = new PaintData {
   name = rName,
   color = rivalColor,
   emission = Color.black
  };

  CarVisualsOverhaul.RebuildCarVisuals(this, vData, pData);

  tailLightMats.Clear();
  foreach (var mr in GetComponentsInChildren<MeshRenderer>(true)) {
   if (mr.name == "Tail light bar") {
    var tailLightMat = new Material(mr.sharedMaterial);
    tailLightMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
    if (tailLightMat.HasProperty("_EmissionColor")) tailLightMat.EnableKeyword("_EMISSION");
    tailLightMats.Add(tailLightMat);
    mr.material = tailLightMat;
   }
  }

  CleanExcessiveLights();

  if (transform.Find("Car_Headlight_L") == null) {
   CreateHeadlight("Car_Headlight_L", new Vector3(-0.68f, 0.68f, 2.18f));
   CreateHeadlight("Car_Headlight_R", new Vector3( 0.68f, 0.68f, 2.18f));
  }

  if (transform.Find("Car_Underglow") == null) {
   var underglowGO = new GameObject("Car_Underglow");
   underglowGO.transform.SetParent(transform, false);
   underglowGO.transform.localPosition = new Vector3(0, 0.15f, 0);
   var underLight = underglowGO.AddComponent<Light>();
   underLight.type = LightType.Point;
   underLight.range = 3.2f;
   underLight.intensity = 0.7f;
   underLight.color = rivalColor;
   underLight.shadows = LightShadows.None;
  }
 }

 void ApplyTrafficSpecs() {
  CleanExcessiveLights();
  tailLightMats.Clear();

  var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
  var trafficTailMat = new Material(Resources.Load<Shader>("VehicleRearLamp"));
  trafficTailMat.name = "TrafficTailLED";
  trafficTailMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
  Color red = new Color(0.85f, 0.02f, 0.04f);
  trafficTailMat.SetColor("_BaseColor", red);
  if (trafficTailMat.HasProperty("_EmissionColor")) {
   trafficTailMat.EnableKeyword("_EMISSION");
   trafficTailMat.SetColor("_EmissionColor", red * 0.85f);
  }
  foreach (var mr in GetComponentsInChildren<MeshRenderer>(true)) {
   if (mr.name.Contains("Tail") || mr.name.Contains("tail")) {
    mr.material = trafficTailMat;
    if (!tailLightMats.Contains(trafficTailMat)) tailLightMats.Add(trafficTailMat);
   }
  }

  if (transform.Find("Car_Headlight_L") == null) {
   CreateHeadlight("Car_Headlight_L", new Vector3(-0.6f, 0.65f, 2.0f));
   CreateHeadlight("Car_Headlight_R", new Vector3( 0.6f, 0.65f, 2.0f));
  }
 }

 public void CleanExcessiveLights() {
  var lights = GetComponentsInChildren<Light>(true);
  var toDestroy = new System.Collections.Generic.List<GameObject>();
  foreach (var l in lights) {
   if (!l) continue;
   if (l.name != "Car_Headlight_L" && l.name != "Car_Headlight_R"
       && l.name != "Car_Underglow" && l.name != "Car_TailGlow") {
    toDestroy.Add(l.gameObject);
   }
  }
  foreach (var g in toDestroy) {
   if (g != null && g != gameObject) {
    if (Application.isPlaying) { g.SetActive(false); Destroy(g); }
    else DestroyImmediate(g);
   }
  }

  foreach (var n in new string[] { "Paint rim light", "Projector headlight", "Car_TailLight_L", "Car_TailLight_R", "Car_RoofFill" }) {
   var t = transform.Find(n);
   if (t) {
    if (Application.isPlaying) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
    else DestroyImmediate(t.gameObject);
   }
  }

  if (transform.Find("Aster GT Coachwork/Tail light bar") != null || GetComponent<GridRacer>() != null || !automation) {
   foreach (var t in GetComponentsInChildren<Transform>(true)) {
    if (t && t != transform && (t.name == "Tail light" || t.name == "Headlight")) {
     if (Application.isPlaying) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
     else DestroyImmediate(t.gameObject);
    }
   }
  }

  var fL = transform.Find("Car_Headlight_L/Car_Headlight_L_Flare");
  if (fL) Destroy(fL.gameObject);
  var fR = transform.Find("Car_Headlight_R/Car_Headlight_R_Flare");
  if (fR) Destroy(fR.gameObject);
 }

 void CreateHeadlight(string name, Vector3 localPos) {
  var old = transform.Find(name);
  if (old) Destroy(old.gameObject);

  var hlGO = new GameObject(name);
  hlGO.transform.SetParent(transform, false);
  hlGO.transform.localPosition = localPos;
  hlGO.transform.localRotation = Quaternion.Euler(2.5f, (localPos.x > 0 ? 1.5f : -1.5f), 0f);
  var l = hlGO.AddComponent<Light>();
  l.type = LightType.Spot;
  l.range = 55f;
  l.spotAngle = 42f;
  l.innerSpotAngle = 18f;
  l.intensity = 3.5f;
  l.color = new Color(0.90f, 0.95f, 1.0f);
  l.shadows = LightShadows.None;
 }

 void Update() {
  if (!automation && GameInput.Reset) ResetCar();
  throttle  = automation ? testThrottle : GameInput.Throttle;
  steer     = automation ? testSteer    : GameInput.Steer;
  handbrake = automation ? aiHandbrake  : GameInput.Handbrake;
  boost     = automation ? aiBoost      : GameInput.Boost;

  if (transform.position.y < -12) ResetCar();

  if (wheels != null && wheelVisuals != null) {
   int count = Mathf.Min(wheels.Length, wheelVisuals.Length);
   for (int i = 0; i < count; i++) {
    if (wheelVisuals[i] == null || wheels[i] == null) continue;
    wheels[i].GetWorldPose(out Vector3 pos, out Quaternion q);
    wheelVisuals[i].SetPositionAndRotation(pos, q);
   }
  }

  if (bodyVisual)
   bodyVisual.localRotation = Quaternion.Slerp(
    bodyVisual.localRotation,
    Quaternion.Euler(throttle * -1.1f, 0, -smoothedSteer * Mathf.Clamp(SpeedKmh / 45f, 0, 2)),
    Time.deltaTime * BodyTiltSpeed);

  // Dynamic brake lights
  {
   bool isBraking = !controlsEnabled || handbrake || (Mathf.Abs(ForwardSpeed) > .35f && throttle * ForwardSpeed < -.15f);
   Color pureRed = new Color(0.95f, 0.02f, 0.04f, 1f);
   Color normalEmission  = new Color(.65f, .003f, .006f);
   Color brakingEmission = new Color(3.2f, .008f, .015f);
   Color targetEmission  = isBraking ? brakingEmission : normalEmission;

   foreach (var mat in tailLightMats) {
    if (!mat) continue;
    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", pureRed);
    if (mat.HasProperty("_Color")) mat.SetColor("_Color", pureRed);
    if (mat.HasProperty("_EmissionColor")) {
     mat.SetColor("_EmissionColor", Color.Lerp(mat.GetColor("_EmissionColor"), targetEmission, Time.deltaTime * 16f));
    }
   }

   if (centerStopLamp) centerStopLamp.enabled = isBraking;
   if (tailGlow) tailGlow.intensity = Mathf.Lerp(tailGlow.intensity, isBraking ? 1.5f : .25f, Time.deltaTime * 16f);
  }

  // Slipstream
  Slipstreaming = false;
  if (ForwardSpeed > 18f) {
   foreach (var c in activeCars) {
    if (!c || c == this) continue;
    Vector3 localOther = transform.InverseTransformPoint(c.transform.position);
    if (localOther.z > 3.5f && localOther.z < 22f && Mathf.Abs(localOther.x) < 2.3f && Mathf.Abs(localOther.y) < 2.0f) {
     Slipstreaming = true;
     break;
    }
   }
  }
 }

 void FixedUpdate() {
  if (wheels == null || wheels.Length == 0) return;

  if (!controlsEnabled) {
   foreach (var w in wheels) { if (w) { w.motorTorque = 0; w.brakeTorque = BrakeTorque; } }
   return;
  }

  float speed   = ForwardSpeed;
  smoothedSteer = Mathf.MoveTowards(smoothedSteer, steer, Time.fixedDeltaTime * SteerSmoothing);
  float steerDeg = smoothedSteer * Mathf.Lerp(steerAngleMax, steerAngleMin,
                    Mathf.Clamp01(Mathf.Abs(speed) / topSpeed));

  bool grounded = false;
  foreach (var w in wheels) if (w) grounded |= w.isGrounded;

  Boosting = boost && throttle > 0 && speed > 4 && Nitro > .01f && grounded;
  Drifting = handbrake && Mathf.Abs(speed) > 7 && grounded;

  float nitroDelta = Boosting ? -.22f : Drifting ? .14f * driftNitroMult : .035f;
  Nitro = Mathf.Clamp01(Nitro + Time.fixedDeltaTime * nitroDelta);

  bool  braking  = throttle * speed < -1.5f;
  float speedCap = throttle < 0 ? 10f : topSpeed * (Boosting ? 1.25f : Slipstreaming ? 1.10f : 1f);
  float torque   = braking ? 0f
                   : throttle * (Boosting ? MotorTorqueBoost : Slipstreaming ? MotorTorqueBase * 1.25f : MotorTorqueBase)
                     * torqueMult * Mathf.Clamp01(1f - Mathf.Abs(speed) / speedCap);

  for (int i = 0; i < wheels.Length; i++) {
   var  w = wheels[i];
   if (!w) continue;
   bool isFront = i < 2;

   w.steerAngle  = isFront ? steerDeg : 0;
   w.motorTorque = isFront ? 0 : torque;

   if (braking)
    w.brakeTorque = BrakeTorque;
   else if (handbrake && !isFront)
    w.brakeTorque = HandbrakeTorque;
   else if (Mathf.Abs(throttle) < .01f)
    w.brakeTorque = IdleBrakeTorque;
   else
    w.brakeTorque = 0;

   // Aderência lateral: solta as rodas traseiras para drift quando freio de mão está acionado
   var f = w.sidewaysFriction;
   float targetStiff = (handbrake && !isFront) ? handbrakeGrip : SidewaysFriction;
   f.stiffness = Mathf.MoveTowards(f.stiffness, targetStiff, Time.fixedDeltaTime * 10f);
   w.sidewaysFriction = f;
  }

  if (grounded) {
   rb.AddForce(-transform.up * rb.linearVelocity.sqrMagnitude * DownforceMult);
   rb.AddForce(-rb.linearVelocity * rb.linearVelocity.magnitude * DragMult);

   // ── Assistência de Aderência Lateral (Rock-Solid Grip) ─────────────
   // Mantém o carro firme e guiado nas curvas normais, sem escorregar como sabão.
   // Quando o freio de mão é acionado, permite derrapagem com total controle.
   if (Mathf.Abs(speed) > 1.8f) {
    Vector3 rightVec = transform.right;
    float latVel = Vector3.Dot(rb.linearVelocity, rightVec);
    // Em curva normal: cancela 88% do deslize lateral -> carro colado no traçado
    // No drift de freio de mão: reduz cancelamento para 28% -> drift fluído e previsível
    float cancelRate = handbrake ? (1f - handbrakeGrip) : 0.88f;
    Vector3 correction = -rightVec * latVel * cancelRate;
    rb.AddForce(correction * Mathf.Clamp01(Time.fixedDeltaTime * 28f), ForceMode.VelocityChange);
   }
  }
 }

 public void ResetCar() {
  rb.linearVelocity  = Vector3.zero;
  rb.angularVelocity = Vector3.zero;
  rb.position        = spawn;
  rb.rotation        = spawnRotation;
 }

 void OnDisable() {
  activeCars.Remove(this);
  if (wheels == null) return;
  foreach (var w in wheels) { if (w) { w.motorTorque = 0; w.brakeTorque = BrakeTorque; } }
 }

 void OnDestroy() {
  foreach (var m in tailLightMats) if (m) Destroy(m);
  if (playerPaint) Destroy(playerPaint);
 }
}
}
