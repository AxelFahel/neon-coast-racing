using UnityEngine;

namespace NeonCoast {
[RequireComponent(typeof(Camera))]
public class ChaseCamera : MonoBehaviour {
 public ArcadeCar target;
 Camera cam;
 Rigidbody targetBody;
 Vector3 velocity;
 float yaw;
 float currentRoll;

 public enum CamView { Distante = 0, Dinamica = 1, Capo = 2 }
 public CamView currentView = CamView.Distante; // Inicia na câmera mais distante por padrão

 void Awake() {
  cam = GetComponent<Camera>();
  if (target) { yaw = target.transform.eulerAngles.y; targetBody = target.GetComponent<Rigidbody>(); }
 }

 void LateUpdate() {
  if (!target) return;

  // Tecla C ou Gamepad troca entre as 3 câmeras
  if (GameInput.CameraToggle) {
   currentView = (CamView)(((int)currentView + 1) % 3);
  }

  Transform t = target.transform;
  float speedNorm = Mathf.Clamp01(target.SpeedKmh / 220f);

  // ── Modo 3: Câmera do Capô (Primeira Pessoa) ──
  if (currentView == CamView.Capo) {
   transform.SetPositionAndRotation(t.TransformPoint(new Vector3(0, 1.05f, 1.35f)), t.rotation);
   cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 76f + (target.Boosting ? 12f : 0f), Time.deltaTime * 5f);
   return;
  }

  // Acompanhamento angular suave (yaw)
  yaw = Mathf.LerpAngle(yaw, t.eulerAngles.y, 1f - Mathf.Exp(-6f * Time.deltaTime));
  Quaternion heading = Quaternion.Euler(0, yaw, 0);

  // Parâmetros por modo de câmera:
  // Distante: ~7.6m de distância, ~2.3m de altura (visão completa e ampla do carro e da pista)
  // Dinâmica: ~5.8m de distância, ~1.75m de altura
  float baseDist = (currentView == CamView.Distante) ? 7.6f : 5.8f;
  float baseHeight = (currentView == CamView.Distante) ? 2.3f : 1.75f;
  float lookAhead = (currentView == CamView.Distante) ? 5.5f : 4.2f;

  float camDist = baseDist + speedNorm * 0.9f + (target.Boosting ? 0.8f : 0f);
  float camHeight = baseHeight - speedNorm * 0.12f;

  Vector3 focus = t.position + Vector3.up * 1.05f;
  Vector3 desired = focus + heading * new Vector3(0, camHeight, -camDist);
  Vector3 delta = desired - focus;

  if (Physics.SphereCast(focus, 0.25f, delta.normalized, out RaycastHit hit, delta.magnitude, 1, QueryTriggerInteraction.Ignore)) {
   desired = hit.point + hit.normal * 0.45f;
  }

  transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.11f);

  // Olhar para a frente do veículo, mantendo o horizonte aberto
  Vector3 lookTarget = focus + t.forward * lookAhead + Vector3.up * 0.15f;
  Quaternion lookRot = Quaternion.LookRotation((lookTarget - transform.position).normalized);

  // Inclinação lateral sutil em derrapagens
  if (!targetBody) targetBody = target.GetComponent<Rigidbody>();
  float yawVelocity = targetBody ? Vector3.Dot(targetBody.angularVelocity, Vector3.up) : 0f;
  float targetRoll = target.Drifting ? -Mathf.Sign(yawVelocity) * 2.2f : 0f;
  currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * 4f);
  transform.rotation = Quaternion.Slerp(transform.rotation, lookRot * Quaternion.Euler(0, 0, currentRoll), 1f - Mathf.Exp(-12f * Time.deltaTime));

  // Campo de visão (FOV)
  float baseFov = (currentView == CamView.Distante) ? 65f : 70f;
  float targetFov = baseFov + speedNorm * 14f + (target.Boosting ? 8f : 0f);
  cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * 5f);
 }
}
}
