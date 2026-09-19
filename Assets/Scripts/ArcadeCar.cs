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

 Rigidbody rb;
 float throttle, steer, smoothedSteer;
 bool handbrake, boost;
 Vector3 spawn; Quaternion spawnRotation;
 readonly System.Collections.Generic.List<Material> tailLightMats = new System.Collections.Generic.List<Material>();
 Material playerPaint;
 Color tailNormal  = new Color(0.85f, 0.02f, 0.04f);
 Color tailBraking = new Color(1.00f, 0.04f, 0.06f);

 // ── Physics constants ─────────────────────────────────────────────────────
 const float RbMass           = 1350f;
 const float DownforceMult    = 0.8f;
 const float DragMult          = 0.32f;
 const float MotorTorqueBase   = 1350f;
 const float MotorTorqueBoost  = 2000f;
 const float BrakeTorque       = 3200f;
 const float HandbrakeTorque   = 2200f;
 const float IdleBrakeTorque   = 55f;
 const float SidewaysFriction  = 1.6f;
 const float SteerSmoothing    = 2.8f;
 const float BodyTiltSpeed     = 5f;

 public void SetSpawn(Vector3 p, Quaternion q) { spawn = p; spawnRotation = q; }

 void Awake() {
  rb = GetComponent<Rigidbody>();
  rb.mass = RbMass;
  rb.centerOfMass = new Vector3(0, .15f, 0);
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
 }

 void ApplyVehicleSpecs() {
  var v = VehicleRegistry.GetSelectedVehicle();
  var p = VehicleRegistry.GetSelectedPaint();
  vehicleName    = v.name;
  topSpeed       = v.topSpeed;
  torqueMult     = v.torqueMult;
  steerAngleMax  = v.steerAngleMax;
  steerAngleMin  = v.steerAngleMin;
  driftNitroMult = v.driftNitroMult;
  handbrakeGrip  = v.handbrakeGrip;

  // Reconstrói a carroceria esculpida, aerodinâmica e estilosa do supercarro
  CarVisualsOverhaul.RebuildCarVisuals(this, v, p);

  // Localiza materiais de lanterna para modulação dinâmica de freio (apenas a barra LED)
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

  // Limpeza rigorosa de luzes pontuais que causavam clarão/ofuscamento da câmera
  CleanExcessiveLights();

  // Faróis focados para frente na pista (spots direcionais, sem ofuscamento omnidirecional)
  if (transform.Find("Car_Headlight_L") == null) {
   CreateHeadlight("Car_Headlight_L", new Vector3(-0.68f, 0.68f, 2.18f));
   CreateHeadlight("Car_Headlight_R", new Vector3( 0.68f, 0.68f, 2.18f));
  }

  // Luz neon de chassi suave (Underglow sutil no asfalto)
  if (transform.Find("Car_Underglow") == null) {
   var underglowGO = new GameObject("Car_Underglow");
   underglowGO.transform.SetParent(transform, false);
   underglowGO.transform.localPosition = new Vector3(0, 0.15f, 0);
   var underLight = underglowGO.AddComponent<Light>();
   underLight.type = LightType.Point;
   underLight.range = 3.2f;
   underLight.intensity = 0.5f;
   underLight.color = p.color;
   underLight.shadows = LightShadows.None;
  }
 }

 void ApplyRivalSpecs() {
  var racer = GetComponent<GridRacer>();
  string rName = racer ? racer.driverName : "Rival";
  Color rivalColor = Color.white;
  string vehId = "aster_gt";

  if (rName.Contains("KAI")) {
   rivalColor = new Color(0.98f, 0.32f, 0.05f); // Neon Coral / Laranja Hyper
   vehId = "shinobi_rspec";
  } else if (rName.Contains("NOVA")) {
   rivalColor = new Color(0.72f, 0.12f, 0.98f); // Violeta / Roxo Cyberpunk
   vehId = "valkyrie_apex";
  } else {
   rivalColor = new Color(0.04f, 0.92f, 0.62f); // Verde Esmeralda / Ciano Elétrico
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

  // Localiza materiais de lanterna para modulação dinâmica de freio dos rivais
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

  // Limpeza rigorosa de luzes pontuais
  CleanExcessiveLights();

  // Faróis potentes dianteiros
  if (transform.Find("Car_Headlight_L") == null) {
   CreateHeadlight("Car_Headlight_L", new Vector3(-0.68f, 0.68f, 2.18f));
   CreateHeadlight("Car_Headlight_R", new Vector3( 0.68f, 0.68f, 2.18f));
  }

  // Luz neon sob o chassi (Underglow suave na cor do oponente)
  if (transform.Find("Car_Underglow") == null) {
   var underglowGO = new GameObject("Car_Underglow");
   underglowGO.transform.SetParent(transform, false);
   underglowGO.transform.localPosition = new Vector3(0, 0.15f, 0);
   var underLight = underglowGO.AddComponent<Light>();
   underLight.type = LightType.Point;
   underLight.range = 3.0f;
   underLight.intensity = 0.5f;
   underLight.color = rivalColor;
   underLight.shadows = LightShadows.None;
  }
 }

 void ApplyTrafficSpecs() {
  CleanExcessiveLights();
  tailLightMats.Clear();

  // Substitui os cubos antigos de lanterna por material LED vermelho escuro/saturado sem clarão branco
  var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
  var trafficTailMat = new Material(litShader);
  trafficTailMat.name = "TrafficTailLED";
  Color red = new Color(0.85f, 0.02f, 0.04f);
  trafficTailMat.SetColor("_BaseColor", red);
  if (trafficTailMat.HasProperty("_EmissionColor")) {
   trafficTailMat.EnableKeyword("_EMISSION");
   trafficTailMat.SetColor("_EmissionColor", red * 0.85f);
  }
  foreach (var mr in GetComponentsInChildren<MeshRenderer>(true)) {
   if (mr.name.Contains("Tail") || mr.name.Contains("tail")) {
    mr.material = trafficTailMat;
    tailLightMats.Add(trafficTailMat);
   }
  }

  // Faróis dianteiros focados para o tráfego civil
  if (transform.Find("Car_Headlight_L") == null) {
   CreateHeadlight("Car_Headlight_L", new Vector3(-0.6f, 0.65f, 2.0f));
   CreateHeadlight("Car_Headlight_R", new Vector3( 0.6f, 0.65f, 2.0f));
  }
 }

 public void CleanExcessiveLights() {
  // Destrói qualquer luz da cena antiga ou rogue que cause clarão branco ofuscante
  var lights = GetComponentsInChildren<Light>(true);
  var toDestroy = new System.Collections.Generic.List<GameObject>();
  foreach (var l in lights) {
   if (!l) continue;
   // Preserva apenas as luzes oficiais do carro
   if (l.name != "Car_Headlight_L" && l.name != "Car_Headlight_R"
       && l.name != "Car_Underglow" && l.name != "Car_TailGlow") {
    toDestroy.Add(l.gameObject);
   }
  }
  foreach (var g in toDestroy) {
   if (g != null && g != gameObject) Destroy(g);
  }

  // Purga GameObjects residuais específicos
  foreach (var n in new string[] { "Paint rim light", "Projector headlight", "Car_TailLight_L", "Car_TailLight_R", "Car_RoofFill" }) {
   var t = transform.Find(n);
   if (t) Destroy(t.gameObject);
  }

  // Remove cubos antigos "Tail light" e "Headlight" se o novo modelo estilizado estiver ativo
  if (transform.Find("Aster GT Coachwork/Tail light bar") != null || GetComponent<GridRacer>() != null || !automation) {
   foreach (var t in GetComponentsInChildren<Transform>(true)) {
    if (t && t != transform && (t.name == "Tail light" || t.name == "Headlight")) {
     Destroy(t.gameObject);
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
  l.spotAngle = 42f;        // Cone mais fechado = feixe mais definido na pista
  l.innerSpotAngle = 18f;
  l.intensity = 3.5f;       // Iluminação real na pista sem bloom excessivo
  l.color = new Color(0.90f, 0.95f, 1.0f);  // Branco levemente frio (xenônio/LED)
  l.shadows = LightShadows.None;
 }

 void Update() {
  if (!automation && GameInput.Reset) ResetCar();
  throttle  = automation ? testThrottle : GameInput.Throttle;
  steer     = automation ? testSteer    : GameInput.Steer;
  handbrake = automation ? aiHandbrake  : GameInput.Handbrake;
  boost     = automation ? aiBoost      : GameInput.Boost;

  if (transform.position.y < -12) ResetCar();

  // Sync wheel visuals — guard against inspector mis-configuration
  if (wheels != null && wheelVisuals != null) {
   int count = Mathf.Min(wheels.Length, wheelVisuals.Length);
   for (int i = 0; i < count; i++) {
    if (wheelVisuals[i] == null) continue;
    wheels[i].GetWorldPose(out Vector3 pos, out Quaternion q);
    wheelVisuals[i].SetPositionAndRotation(pos, q);
   }
  }

  if (bodyVisual)
   bodyVisual.localRotation = Quaternion.Slerp(
    bodyVisual.localRotation,
    Quaternion.Euler(throttle * -1.1f, 0, -smoothedSteer * Mathf.Clamp(SpeedKmh / 45f, 0, 2)),
    Time.deltaTime * BodyTiltSpeed);

   // Dynamic brake lights — modula emissão de LED e luz de glow no asfalto
   {
    bool isBraking = (throttle * ForwardSpeed < -1.2f) || handbrake;
    Color pureRed = new Color(0.95f, 0.02f, 0.04f, 1f);
    // Emissão em HDR para bloom neon: normal = 1.9 (vermelho vivo), freio = 4.2 (LED hiper-brilhante)
    Color normalEmission  = new Color(1.9f, 0.02f, 0.03f);
    Color brakingEmission = new Color(4.2f, 0.03f, 0.05f);
    Color targetEmission  = isBraking ? brakingEmission : normalEmission;

    foreach (var mat in tailLightMats) {
     if (!mat) continue;
     // BaseColor NUNCA ultrapassa 1.0 para nunca desbotar ou ficar branco
     if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", pureRed);
     if (mat.HasProperty("_Color")) mat.SetColor("_Color", pureRed);
     if (mat.HasProperty("_EmissionColor")) {
      mat.SetColor("_EmissionColor", Color.Lerp(mat.GetColor("_EmissionColor"), targetEmission, Time.deltaTime * 16f));
     }
    }

    // Modula a intensidade da luz pontual de glow traseiro no asfalto
    var tailGlowT = bodyVisual ? bodyVisual.Find("Car_TailGlow") : transform.Find("Car_TailGlow");
    if (tailGlowT) {
     var tgl = tailGlowT.GetComponent<Light>();
     if (tgl) tgl.intensity = Mathf.Lerp(tgl.intensity, isBraking ? 3.0f : 1.2f, Time.deltaTime * 16f);
    }
   }

  // Vácuo aerodinâmico (Slipstream / Drafting) atrás de outros carros
  Slipstreaming = false;
  if (ForwardSpeed > 18f) {
   var allCars = FindObjectsByType<ArcadeCar>(FindObjectsSortMode.None);
   foreach (var c in allCars) {
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
  if (!controlsEnabled) {
   // Kill drive and hold brakes on all four wheels
   foreach (var w in wheels) { w.motorTorque = 0; w.brakeTorque = BrakeTorque; }
   return;
  }

  float speed   = ForwardSpeed;
  smoothedSteer = Mathf.MoveTowards(smoothedSteer, steer, Time.fixedDeltaTime * SteerSmoothing);
  float steerDeg = smoothedSteer * Mathf.Lerp(steerAngleMax, steerAngleMin,
                    Mathf.Clamp01(Mathf.Abs(speed) / topSpeed));

  bool grounded = false;
  foreach (var w in wheels) grounded |= w.isGrounded;

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
   var  w       = wheels[i];
   bool isFront = i < 2;

   w.steerAngle  = isFront ? steerDeg : 0;
   w.motorTorque = isFront ? 0 : torque;  // rear-wheel drive

   if (braking)
    w.brakeTorque = BrakeTorque;
   else if (handbrake && !isFront)
    w.brakeTorque = HandbrakeTorque;
   else if (Mathf.Abs(throttle) < .01f)
    w.brakeTorque = IdleBrakeTorque;
   else
    w.brakeTorque = 0;

   var f = w.sidewaysFriction;
   f.stiffness = handbrake && !isFront ? handbrakeGrip : SidewaysFriction;
   w.sidewaysFriction = f;
  }

  if (grounded) {
   rb.AddForce(-transform.up * rb.linearVelocity.sqrMagnitude * DownforceMult);
   rb.AddForce(-rb.linearVelocity * rb.linearVelocity.magnitude * DragMult);
  }
 }

 public void ResetCar() {
  rb.linearVelocity  = Vector3.zero;
  rb.angularVelocity = Vector3.zero;
  rb.position        = spawn;
  rb.rotation        = spawnRotation;
 }

 void OnDisable() {
  // Bring the car to a safe halt whenever the component is toggled off
  if (wheels == null) return;
  foreach (var w in wheels) { w.motorTorque = 0; w.brakeTorque = BrakeTorque; }
 }

 void OnDestroy() {
  foreach (var m in tailLightMats) if (m) Destroy(m);
  if (playerPaint) Destroy(playerPaint);
 }
}
}
