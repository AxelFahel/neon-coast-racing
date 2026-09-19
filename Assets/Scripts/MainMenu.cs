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

 int selectedActionIndex = 0;
 float navCooldown = 0f;

 readonly Color normalActionBg = new Color(0.06f, 0.12f, 0.22f, 0.85f);
 readonly Color selectedActionBg = new Color(0.1f, 0.9f, 1f, 0.35f);
 readonly Color normalActionTextColor = new Color(0.85f, 0.90f, 0.96f);
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

 void Update() {
  if (navCooldown > 0) navCooldown -= Time.unscaledDeltaTime;

  var k = UnityEngine.InputSystem.Keyboard.current;
  var g = UnityEngine.InputSystem.Gamepad.current;

  // Navegação Vertical (Cima / Baixo) entre as ações do menu
  bool up = (k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) ||
            (g != null && (g.dpad.up.wasPressedThisFrame || (navCooldown <= 0 && g.leftStick.y.ReadValue() > 0.5f)));

  bool down = (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) ||
              (g != null && (g.dpad.down.wasPressedThisFrame || (navCooldown <= 0 && g.leftStick.y.ReadValue() < -0.5f)));

  if (up) {
   selectedActionIndex = (selectedActionIndex - 1 + 3) % 3;
   navCooldown = 0.22f;
   UpdateActionSelection();
  } else if (down) {
   selectedActionIndex = (selectedActionIndex + 1) % 3;
   navCooldown = 0.22f;
   UpdateActionSelection();
  }

  // Navegação Horizontal (Esquerda / Direita) para alternar veículos
  bool left = (k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame)) ||
              (g != null && (g.dpad.left.wasPressedThisFrame || g.leftShoulder.wasPressedThisFrame || (navCooldown <= 0 && g.leftStick.x.ReadValue() < -0.5f)));

  bool right = (k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame)) ||
               (g != null && (g.dpad.right.wasPressedThisFrame || g.rightShoulder.wasPressedThisFrame || (navCooldown <= 0 && g.leftStick.x.ReadValue() > 0.5f)));

  if (left) {
   PrevVehicle();
   navCooldown = 0.25f;
  } else if (right) {
   NextVehicle();
   navCooldown = 0.25f;
  }

  // Tecla C ou Gamepad Y / Triângulo para alternar cores
  bool cycleColor = (k != null && k.cKey.wasPressedThisFrame) ||
                    (g != null && g.buttonNorth.wasPressedThisFrame);
  if (cycleColor) {
   int nextPaint = (VehicleRegistry.SelectedPaintIndex + 1) % VehicleRegistry.Paints.Length;
   SelectPaint(nextPaint);
  }

  // Números 1 a 4 para escolha direta de cor
  if (k != null) {
   if (k.digit1Key.wasPressedThisFrame) SelectPaint(0);
   if (k.digit2Key.wasPressedThisFrame) SelectPaint(1);
   if (k.digit3Key.wasPressedThisFrame) SelectPaint(2);
   if (k.digit4Key.wasPressedThisFrame) SelectPaint(3);
  }

  // Confirmar ação selecionada com ENTER / Espaço / Gamepad A
  bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) ||
                 (g != null && g.buttonSouth.wasPressedThisFrame);
  if (confirm) {
   ExecuteSelectedAction();
  }

  // Atalho direto de Contra o Tempo (T ou X no gamepad)
  if ((k != null && k.tKey.wasPressedThisFrame) || (g != null && g.buttonWest.wasPressedThisFrame)) {
   StartTimeTrial();
  }

  // Sair com ESC
  if (k != null && k.escapeKey.wasPressedThisFrame) {
   QuitGame();
  }
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
 }

 public void PrevVehicle() {
  VehicleRegistry.SelectedVehicleIndex = (VehicleRegistry.SelectedVehicleIndex - 1 + VehicleRegistry.Vehicles.Length) % VehicleRegistry.Vehicles.Length;
  UpdateVehicleDisplay();
 }

 public void SelectPaint(int index) {
  VehicleRegistry.SelectedPaintIndex = index;
  UpdateVehicleDisplay();
 }

 public void UpdateVehicleDisplay() {
  var v = VehicleRegistry.GetSelectedVehicle();
  var p = VehicleRegistry.GetSelectedPaint();

  if (carNameText) carNameText.text = v.name;
  if (carTaglineText) carTaglineText.text = v.tagline + "  //  " + p.name.ToUpper();

  if (speedStatText) speedStatText.text = Bar(v.statSpeed) + " <color=#8DA4BF><size=14>" + Mathf.RoundToInt(v.topSpeed * 3.6f) + " KM/H</size></color>";
  if (accelStatText) accelStatText.text = Bar(v.statAccel);
  if (handlingStatText) handlingStatText.text = Bar(v.statHandling);
  if (nitroStatText) nitroStatText.text = Bar(v.statNitro);

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
  for (int i = 1; i <= 10; i++) {
   s += (i <= val) ? "<color=#19E6FF>■ </color>" : "<color=#1A2E44>■ </color>";
  }
  return s;
 }

 public void StartRace() {
  GameMode.TimeTrial = false;
  SceneManager.LoadScene("NeonCoast");
 }

 public void StartTimeTrial() {
  GameMode.TimeTrial = true;
  SceneManager.LoadScene("NeonCoast");
 }

 public void QuitGame() {
  Application.Quit();
#if UNITY_EDITOR
  UnityEditor.EditorApplication.isPlaying = false;
#endif
 }
}
}
