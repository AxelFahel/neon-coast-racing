using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace NeonCoast {
public class MainMenu : MonoBehaviour {
 public CanvasGroup canvasGroup;
 public float fadeTime = 1f;

 [Header("Vehicle Selection UI")]
 public TMP_Text carNameText;
 public TMP_Text carTaglineText;
 public TMP_Text speedStatText;
 public TMP_Text accelStatText;
 public TMP_Text handlingStatText;
 public TMP_Text nitroStatText;
 public Image[] paintIndicators;

 [Header("Action Buttons (0=Corrida, 1=Contra o Tempo, 2=Sair)")]
 public Image[] actionButtonBgs;
 public TMP_Text[] actionButtonTexts;

 // Vitrine 3D do Supercarro em tempo real no menu
 GameObject showcaseRoot;
 GameObject showcaseTurntable;
 GameObject showcaseCarGO;
 ArcadeCar  showcaseArcadeCar;
 Renderer   turntableRingRenderer;
 Camera     menuCamera;

 int selectedActionIndex = 0;
 float navCooldown = 0f;

 readonly Color normalActionBg       = new Color(0.06f, 0.12f, 0.22f, 0.85f);
 readonly Color selectedActionBg     = new Color(0.1f, 0.9f, 1f, 0.35f);
 readonly Color normalActionTextColor   = new Color(0.85f, 0.90f, 0.96f);
 readonly Color selectedActionTextColor = new Color(0.1f, 0.95f, 1f);

 void OnEnable() {
  Time.timeScale = 1;
  Cursor.visible = true;
  Cursor.lockState = CursorLockMode.None;
 }

 void Start() {
  if (canvasGroup) {
   canvasGroup.alpha = 0;
   StartCoroutine(FadeIn());
  }

  menuCamera = Camera.main;

  // Garante que os botões de seleção de cor na UI recebam os cliques do mouse em runtime
  if (paintIndicators != null) {
   for (int i = 0; i < paintIndicators.Length; i++) {
    int pIdx = i;
    var btn = paintIndicators[i].GetComponent<Button>();
    if (!btn) btn = paintIndicators[i].gameObject.AddComponent<Button>();
    btn.onClick.RemoveAllListeners();
    btn.onClick.AddListener(() => SelectPaint(pIdx));
   }
  }

  BuildShowcase();
  UpdateVehicleDisplay();
  UpdateActionSelection();
 }

 IEnumerator FadeIn() {
  float t = 0;
  while (t < fadeTime) {
   canvasGroup.alpha = t / fadeTime;
   t += Time.unscaledDeltaTime;
   yield return null;
  }
  canvasGroup.alpha = 1;
 }

 // ── Vitrine 3D com Plataforma Giratória Neon ──────────────────────────────
 void BuildShowcase() {
  if (showcaseRoot) Destroy(showcaseRoot);

  showcaseRoot = new GameObject("Showcase_Root");

  // Posicionamento no canto direito da tela diante da câmera
  Vector3 camPos = menuCamera ? menuCamera.transform.position : new Vector3(0, 2f, -10f);
  Vector3 camFwd = menuCamera ? menuCamera.transform.forward : Vector3.forward;
  Vector3 camRight = menuCamera ? menuCamera.transform.right : Vector3.right;
  Vector3 camUp = menuCamera ? menuCamera.transform.up : Vector3.up;

  Vector3 basePos = camPos + camFwd * 5.8f + camRight * 2.1f - camUp * 0.9f;
  showcaseRoot.transform.position = basePos;
  showcaseRoot.transform.rotation = Quaternion.Euler(0f, -22f, 0f);

  // 1. Plataforma giratória escura
  showcaseTurntable = new GameObject("Showcase_Turntable");
  showcaseTurntable.transform.SetParent(showcaseRoot.transform, false);

  var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

  var discMat = new Material(litShader);
  discMat.SetColor("_BaseColor", new Color(0.04f, 0.05f, 0.07f));
  discMat.SetFloat("_Smoothness", 0.75f);
  discMat.SetFloat("_Metallic", 0.35f);

  var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
  disc.name = "Plataform_Disc";
  disc.transform.SetParent(showcaseTurntable.transform, false);
  disc.transform.localPosition = new Vector3(0f, -0.10f, 0f);
  disc.transform.localScale = new Vector3(4.8f, 0.08f, 4.8f);
  Destroy(disc.GetComponent<Collider>());
  disc.GetComponent<Renderer>().sharedMaterial = discMat;

  // 2. Anel de neon brilhante na borda da plataforma giratória
  var ringMat = new Material(litShader);
  ringMat.name = "Showcase_TurntableNeonRing";
  ringMat.EnableKeyword("_EMISSION");
  var curPaint = VehicleRegistry.GetSelectedPaint();
  ringMat.SetColor("_BaseColor", curPaint.color);
  ringMat.SetColor("_EmissionColor", curPaint.color * 2.2f);

  var ring = new GameObject("Plataform_NeonRing", typeof(MeshFilter), typeof(MeshRenderer));
  ring.name = "Plataform_NeonRing";
  ring.transform.SetParent(showcaseTurntable.transform, false);
  ring.transform.localPosition = new Vector3(0f, -0.05f, 0f);
  // A narrow annulus leaves the dark platform visible beneath the car.
  const int ringSegments = 96;
  var ringVertices = new Vector3[(ringSegments + 1) * 2];
  var ringTriangles = new int[ringSegments * 6];
  for (int i = 0; i <= ringSegments; i++) {
   float angle = i * Mathf.PI * 2f / ringSegments;
   var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
   ringVertices[i * 2] = radial * 2.34f;
   ringVertices[i * 2 + 1] = radial * 2.40f;
   if (i == ringSegments) continue;
   int v = i * 2, t = i * 6;
   ringTriangles[t] = v; ringTriangles[t + 1] = v + 2; ringTriangles[t + 2] = v + 1;
   ringTriangles[t + 3] = v + 1; ringTriangles[t + 4] = v + 2; ringTriangles[t + 5] = v + 3;
  }
  var ringMesh = new Mesh { name = "Showcase Neon Annulus", vertices = ringVertices, triangles = ringTriangles };
  ringMesh.RecalculateNormals();
  ringMesh.RecalculateBounds();
  ring.GetComponent<MeshFilter>().sharedMesh = ringMesh;
  turntableRingRenderer = ring.GetComponent<Renderer>();
  turntableRingRenderer.sharedMaterial = ringMat;

  // 3. Luz pontual suave de vitrine
  var lightGO = new GameObject("Showcase_Spot");
  lightGO.transform.SetParent(showcaseRoot.transform, false);
  lightGO.transform.localPosition = new Vector3(0f, 3.2f, 0.5f);
  lightGO.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, -3.2f, -0.5f));
  var lightComp = lightGO.AddComponent<Light>();
  lightComp.type = LightType.Spot;
  lightComp.range = 8.5f;
  lightComp.spotAngle = 72f;
  lightComp.intensity = 4.5f;
  lightComp.color = new Color(0.92f, 0.95f, 1.0f);
  lightComp.shadows = LightShadows.None;

  // 4. Carro 3D montado sobre a plataforma
  showcaseCarGO = new GameObject("Showcase_Car");
  showcaseCarGO.transform.SetParent(showcaseTurntable.transform, false);
  showcaseCarGO.transform.localPosition = new Vector3(0f, 0.0f, 0f);

  // Componente ArcadeCar dummy apenas para portar a referência de bodyVisual
  // Desabilitado para não rodar FixedUpdate nem física
  showcaseArcadeCar = showcaseCarGO.AddComponent<ArcadeCar>();
  showcaseArcadeCar.enabled = false;
  showcaseArcadeCar.automation = true;
  // Disabling the controller does not disable the Rigidbody added by RequireComponent.
  var showcaseBody = showcaseCarGO.GetComponent<Rigidbody>();
  showcaseBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
  showcaseBody.useGravity = false;
  showcaseBody.isKinematic = true;
  showcaseBody.interpolation = RigidbodyInterpolation.None;

  var coachGO = new GameObject("Aster GT Coachwork");
  coachGO.transform.SetParent(showcaseCarGO.transform, false);
  showcaseArcadeCar.bodyVisual = coachGO.transform;

  RefreshShowcaseCar();
 }

 void RefreshShowcaseCar() {
  if (!showcaseCarGO || !showcaseArcadeCar) { BuildShowcase(); return; }

  var v = VehicleRegistry.GetSelectedVehicle();
  var p = VehicleRegistry.GetSelectedPaint();

  // Reconstrói a carroceria completa (com aerofólios e rodas de vitrine)
  CarVisualsOverhaul.RebuildCarVisuals(showcaseArcadeCar, v, p);

  // Atualiza a cor do anel de neon da plataforma
  if (turntableRingRenderer && turntableRingRenderer.sharedMaterial) {
   turntableRingRenderer.sharedMaterial.SetColor("_BaseColor", p.color);
   turntableRingRenderer.sharedMaterial.SetColor("_EmissionColor", p.color * 2.2f);
  }
 }

 void Update() {
  // Rotação contínua e suave da vitrine 3D
  if (showcaseTurntable) {
   showcaseTurntable.transform.Rotate(0f, 16f * Time.unscaledDeltaTime, 0f, Space.Self);
  }

  if (navCooldown > 0) navCooldown -= Time.unscaledDeltaTime;

  var k = UnityEngine.InputSystem.Keyboard.current;
  var g = UnityEngine.InputSystem.Gamepad.current;

  // Navegação de Modos (Cima / Baixo)
  bool up   = (k != null && (k.upArrowKey.wasPressedThisFrame   || k.wKey.wasPressedThisFrame)) ||
              (g != null && (g.dpad.up.wasPressedThisFrame       || (navCooldown <= 0 && g.leftStick.y.ReadValue() >  0.5f)));
  bool down = (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) ||
              (g != null && (g.dpad.down.wasPressedThisFrame     || (navCooldown <= 0 && g.leftStick.y.ReadValue() < -0.5f)));

  if (up)   { selectedActionIndex = (selectedActionIndex - 1 + 3) % 3; navCooldown = 0.20f; UpdateActionSelection(); }
  else if (down) { selectedActionIndex = (selectedActionIndex + 1) % 3; navCooldown = 0.20f; UpdateActionSelection(); }

  // Alternância de Veículos (Esquerda / Direita)
  bool left  = (k != null && (k.leftArrowKey.wasPressedThisFrame  || k.aKey.wasPressedThisFrame)) ||
               (g != null && (g.dpad.left.wasPressedThisFrame      || g.leftShoulder.wasPressedThisFrame  || (navCooldown <= 0 && g.leftStick.x.ReadValue() < -0.5f)));
  bool right = (k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame)) ||
               (g != null && (g.dpad.right.wasPressedThisFrame     || g.rightShoulder.wasPressedThisFrame || (navCooldown <= 0 && g.leftStick.x.ReadValue() >  0.5f)));

  if (left)  { PrevVehicle(); navCooldown = 0.22f; }
  else if (right) { NextVehicle(); navCooldown = 0.22f; }

  // Tecla C ou Gamepad Y / Triângulo para alternar cores
  bool cycleColor = (k != null && k.cKey.wasPressedThisFrame) || (g != null && g.buttonNorth.wasPressedThisFrame);
  if (cycleColor) SelectPaint((VehicleRegistry.SelectedPaintIndex + 1) % VehicleRegistry.Paints.Length);

  // Teclas 1 a 4 para escolha direta de cor
  if (k != null) {
   if (k.digit1Key.wasPressedThisFrame) SelectPaint(0);
   if (k.digit2Key.wasPressedThisFrame) SelectPaint(1);
   if (k.digit3Key.wasPressedThisFrame) SelectPaint(2);
   if (k.digit4Key.wasPressedThisFrame) SelectPaint(3);
  }

  // Confirmar com Enter / Espaço / Gamepad A
  bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) ||
                 (g != null && g.buttonSouth.wasPressedThisFrame);
  if (confirm) ExecuteSelectedAction();

  if ((k != null && k.tKey.wasPressedThisFrame) || (g != null && g.buttonWest.wasPressedThisFrame)) StartTimeTrial();
  if (k != null && k.escapeKey.wasPressedThisFrame) QuitGame();
 }

 void UpdateActionSelection() {
  if (actionButtonBgs == null || actionButtonTexts == null) return;
  for (int i = 0; i < 3; i++) {
   bool sel = (i == selectedActionIndex);
   if (i < actionButtonBgs.Length && actionButtonBgs[i] != null) {
    actionButtonBgs[i].color = sel ? selectedActionBg : normalActionBg;
    actionButtonBgs[i].transform.localScale = sel ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;
   }
   if (i < actionButtonTexts.Length && actionButtonTexts[i] != null) {
    actionButtonTexts[i].color = sel ? selectedActionTextColor : normalActionTextColor;
    actionButtonTexts[i].fontStyle = sel ? FontStyles.Bold : FontStyles.Normal;
   }
  }
 }

 public void SelectAction(int index) {
  selectedActionIndex = Mathf.Clamp(index, 0, 2);
  UpdateActionSelection();
 }

 public void ExecuteSelectedAction() {
  switch (selectedActionIndex) {
   case 0: StartRace(); break;
   case 1: StartTimeTrial(); break;
   case 2: QuitGame(); break;
  }
 }

 public void NextVehicle() {
  VehicleRegistry.SelectedVehicleIndex = (VehicleRegistry.SelectedVehicleIndex + 1) % VehicleRegistry.Vehicles.Length;
  UpdateVehicleDisplay();
  RefreshShowcaseCar();
 }

 public void PrevVehicle() {
  VehicleRegistry.SelectedVehicleIndex = (VehicleRegistry.SelectedVehicleIndex - 1 + VehicleRegistry.Vehicles.Length) % VehicleRegistry.Vehicles.Length;
  UpdateVehicleDisplay();
  RefreshShowcaseCar();
 }

 public void SelectPaint(int index) {
  VehicleRegistry.SelectedPaintIndex = index;
  UpdateVehicleDisplay();
  RefreshShowcaseCar();
 }

 public void UpdateVehicleDisplay() {
  var v = VehicleRegistry.GetSelectedVehicle();
  var p = VehicleRegistry.GetSelectedPaint();

  if (carNameText) carNameText.text = v.name;
  if (carTaglineText) carTaglineText.text = v.tagline + "  //  " + p.name.ToUpper();

  if (speedStatText)    speedStatText.text    = Bar(v.statSpeed) + " <color=#8DA4BF><size=14>" + Mathf.RoundToInt(v.topSpeed * 3.6f) + " KM/H</size></color>";
  if (accelStatText)    accelStatText.text    = Bar(v.statAccel);
  if (handlingStatText) handlingStatText.text = Bar(v.statHandling);
  if (nitroStatText)    nitroStatText.text    = Bar(v.statNitro);

  if (paintIndicators != null) {
   int curPaint = VehicleRegistry.SelectedPaintIndex;
   for (int i = 0; i < paintIndicators.Length; i++) {
    if (paintIndicators[i] != null) {
     var outline = paintIndicators[i].transform.Find("SelectedBorder");
     if (outline) outline.gameObject.SetActive(i == curPaint);
    }
   }
  }
 }

 static string Bar(int val) {
  string s = "";
  for (int i = 1; i <= 10; i++) s += (i <= val) ? "<color=#19E6FF>■ </color>" : "<color=#1A2E44>■ </color>";
  return s;
 }

 public void StartRace() {
  PlayerPrefs.Save();
  GameMode.TimeTrial = false;
  SceneManager.LoadScene("NeonCoast");
 }

 public void StartTimeTrial() {
  PlayerPrefs.Save();
  GameMode.TimeTrial = true;
  SceneManager.LoadScene("NeonCoast");
 }

 public void QuitGame() {
  PlayerPrefs.Save();
  Application.Quit();
#if UNITY_EDITOR
  UnityEditor.EditorApplication.isPlaying = false;
#endif
 }

 void OnDestroy() {
  if (showcaseRoot) Destroy(showcaseRoot);
 }
}
}
