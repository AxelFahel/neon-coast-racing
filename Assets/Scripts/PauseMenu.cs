using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace NeonCoast {
public class PauseMenu : MonoBehaviour {
 public GameObject pausePanel;
 public ArcadeCar car;
 public RaceSession race;

 [Header("Menu Buttons (Ordem: 0=Continuar, 1=Reiniciar, 2=Menu)")]
 public Image[] buttonBgs;
 public TMP_Text[] buttonTexts;

 public bool IsPaused { get; private set; }
 int selectedIndex = 0;
 float navCooldown = 0f;

 readonly Color normalBg = new Color(0.06f, 0.12f, 0.22f, 0.85f);
 readonly Color selectedBg = new Color(0.1f, 0.9f, 1f, 0.35f);
 readonly Color normalTextColor = new Color(0.85f, 0.90f, 0.96f);
 readonly Color selectedTextColor = new Color(0.1f, 0.95f, 1f);

 void Start() {
  if (pausePanel) pausePanel.SetActive(false);
  UpdateVisualSelection();
 }

 void Update() {
  if (race != null && (race.Countdown > 0 || race.Finished)) return;

  if (navCooldown > 0) navCooldown -= Time.unscaledDeltaTime;

  var k = UnityEngine.InputSystem.Keyboard.current;
  var g = UnityEngine.InputSystem.Gamepad.current;

  // Toggle de Pause com ESC / Start
  if (GameInput.Pause) {
   if (IsPaused) Resume();
   else Pause();
   return;
  }

  if (!IsPaused) return;

  // Navegação direcional para CIMA
  bool upPressed = (k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) ||
                   (g != null && (g.dpad.up.wasPressedThisFrame || (navCooldown <= 0 && g.leftStick.y.ReadValue() > 0.5f)));

  // Navegação direcional para BAIXO
  bool downPressed = (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) ||
                     (g != null && (g.dpad.down.wasPressedThisFrame || (navCooldown <= 0 && g.leftStick.y.ReadValue() < -0.5f)));

  if (upPressed) {
   selectedIndex = (selectedIndex - 1 + 3) % 3;
   navCooldown = 0.22f;
   UpdateVisualSelection();
  } else if (downPressed) {
   selectedIndex = (selectedIndex + 1) % 3;
   navCooldown = 0.22f;
   UpdateVisualSelection();
  }

  // Confirmar com Enter / Espaço / Gamepad A
  bool confirmPressed = (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) ||
                        (g != null && g.buttonSouth.wasPressedThisFrame);

  if (confirmPressed) {
   ExecuteSelectedOption();
  }

  // Voltar / Cancelar com B / Círculo no controle
  if (g != null && g.buttonEast.wasPressedThisFrame) {
   Resume();
  }
 }

 void UpdateVisualSelection() {
  if (buttonBgs == null || buttonTexts == null) return;
  for (int i = 0; i < 3; i++) {
   bool sel = (i == selectedIndex);
   if (i < buttonBgs.Length && buttonBgs[i] != null) {
    buttonBgs[i].color = sel ? selectedBg : normalBg;
    buttonBgs[i].transform.localScale = sel ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;
   }
   if (i < buttonTexts.Length && buttonTexts[i] != null) {
    buttonTexts[i].color = sel ? selectedTextColor : normalTextColor;
    buttonTexts[i].fontStyle = sel ? FontStyles.Bold : FontStyles.Normal;
   }
  }
 }

 public void SelectIndex(int index) {
  selectedIndex = Mathf.Clamp(index, 0, 2);
  UpdateVisualSelection();
 }

 public void ExecuteSelectedOption() {
  switch (selectedIndex) {
   case 0: Resume(); break;
   case 1: Restart(); break;
   case 2: QuitToMenu(); break;
  }
 }

 public void Pause() {
  IsPaused = true;
  selectedIndex = 0; // Sempre foca em "Continuar" ao abrir
  if (car != null) car.Paused = true;
  if (pausePanel != null) pausePanel.SetActive(true);
  UpdateVisualSelection();
  Time.timeScale = 0;
  AudioListener.pause = true;
  Cursor.visible = true;
  Cursor.lockState = CursorLockMode.None;
 }

 public void Resume() {
  IsPaused = false;
  if (car != null) car.Paused = false;
  if (pausePanel != null) pausePanel.SetActive(false);
  Time.timeScale = 1;
  AudioListener.pause = false;
  Cursor.visible = false;
  Cursor.lockState = CursorLockMode.Locked;
 }

 public void Restart() {
  Time.timeScale = 1;
  AudioListener.pause = false;
  Cursor.visible = false;
  Cursor.lockState = CursorLockMode.Locked;
  SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
 }

 public void QuitToMenu() {
  Time.timeScale = 1;
  AudioListener.pause = false;
  Cursor.visible = true;
  Cursor.lockState = CursorLockMode.None;
  SceneManager.LoadScene(0);
 }

 void OnDisable() {
  Time.timeScale = 1;
  AudioListener.pause = false;
 }
}
}
