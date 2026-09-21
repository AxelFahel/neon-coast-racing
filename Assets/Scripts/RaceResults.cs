using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace NeonCoast {
public class RaceResults : MonoBehaviour {
 public GameObject resultsPanel;
 public TMP_Text positionText, timeText, bestLapText, promptText;
 public RaceSession race;
 bool shown;

 void Start() {
  if (resultsPanel) resultsPanel.SetActive(false);
 }

 void Update() {
  if (!race || !race.Finished) return;
  if (!race.FinishPresentationComplete) return;
  if (!shown) { Show(); shown = true; }
  if (GameInput.Confirm) {
   Time.timeScale = 1;
   AudioListener.pause = false;
   SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
  }
  if (GameInput.Back) {
   Time.timeScale = 1;
   AudioListener.pause = false;
   SceneManager.LoadScene(0);
  }
 }

 void Show() {
  if (resultsPanel) resultsPanel.SetActive(true);
  if (race.player) race.player.controlsEnabled = false;
  Cursor.visible = true;
  Cursor.lockState = CursorLockMode.None;

  if (positionText) {
   if (GameMode.TimeTrial) {
    positionText.text = (race.NewRaceRecord || race.NewLapRecord) ? "NOVO RECORDE!" : "TREINO CONCLUÍDO";
   } else {
    positionText.text = race.Position + "º LUGAR";
   }
  }

  if (timeText) {
   timeText.text = System.TimeSpan.FromSeconds(race.Elapsed).ToString(@"mm\:ss\.ff") +
    (race.NewRaceRecord ? " <size=24><color=#19E6FF>[MELHOR]</color></size>" : "");
  }

  if (bestLapText) {
   bestLapText.text = (race.BestLap < float.PositiveInfinity
    ? System.TimeSpan.FromSeconds(race.BestLap).ToString(@"mm\:ss\.ff")
    : "--:--.--") + (race.NewLapRecord ? " <size=24><color=#19E6FF>[MELHOR]</color></size>" : "");
  }

  if (promptText) {
   promptText.text = GameInput.GamepadConnected
    ? "(A)  Reiniciar     (B)  Menu Principal"
    : "ENTER  Reiniciar     ESC  Menu Principal";
  }
 }
}
}
