using UnityEngine;
using TMPro;

namespace NeonCoast {
public class RacingHUD : MonoBehaviour {
 public ArcadeCar car;
 public RaceSession race;

 // Legacy / fallback bindings
 public TMP_Text speed, status, timing;
 public UnityEngine.UI.Image nitro;

 // Rich dashboard bindings
 public TMP_Text carNameText;
 public TMP_Text positionBadge;
 public TMP_Text lapBadge;
 public TMP_Text gateBadge;
 public TMP_Text speedDigits;
 public TMP_Text gearText;
 public TMP_Text chronoText;
 public TMP_Text bestLapDisplay;
 public TMP_Text alertBannerText;
 public UnityEngine.UI.Image speedBar;
 public UnityEngine.UI.Image nitroBar;
 public GameObject nitroGlow;
 public GameObject boostTag;
 public GameObject driftTag;
 public GameObject alertBanner;
 public CanvasGroup controlsGroup;

 float alertTimer;
 int lastLapAnnounced = 1;
 int lastScreenWidth, lastScreenHeight;
 Rect lastSafeArea;

 void Start() {
  ApplyResponsiveLayout(true);
  if (carNameText != null && car != null) {
   var v = VehicleRegistry.GetSelectedVehicle();
   carNameText.text = v.name + "  //  " + v.tagline;
  }
 }

 void Update() {
  ApplyResponsiveLayout(false);
  if (!car || !race) return;

  float spd = car.SpeedKmh;
  int speedInt = Mathf.RoundToInt(spd);

  // --- Velocímetro Dinâmico e Marcha ---
  if (speedDigits) speedDigits.text = speedInt.ToString("000");
  else if (speed) speed.text = speedInt.ToString("000") + "<size=24> KM/H</size>";

  int gear = Mathf.Clamp(Mathf.FloorToInt(spd / 40f) + 1, 1, 6);
  if (gearText) gearText.text = spd < 1 ? "N" : gear.ToString();

  // Barra de rotação / tacômetro com transição de cor (Ciano -> Âmbar -> Vermelho corte de giro)
  if (speedBar) {
   float maxSpd = Mathf.Max(120f, car.topSpeed * 3.6f * 1.25f);
   float ratio = Mathf.Clamp01(spd / maxSpd);
   speedBar.fillAmount = ratio;
   speedBar.color = ratio < 0.60f
    ? Color.Lerp(new Color(0.1f, 0.9f, 1f), new Color(1f, 0.82f, 0.1f), ratio / 0.60f)
    : Color.Lerp(new Color(1f, 0.82f, 0.1f), new Color(1f, 0.1f, 0.35f), (ratio - 0.60f) / 0.40f);
  }

  // --- Medidor de Nitro ---
  float nitroVal = car.Nitro;
  if (nitroBar) {
   nitroBar.fillAmount = nitroVal;
   // Pulsação sutil no nitro quando ativo
   if (car.Boosting) {
    float pulse = 0.85f + Mathf.PingPong(Time.time * 6f, 0.3f);
    nitroBar.color = new Color(0.1f * pulse, 0.95f * pulse, 1f * pulse);
   } else {
    nitroBar.color = new Color(0.05f, 0.95f, 1f);
   }
  }
  if (nitro) nitro.fillAmount = nitroVal;
  if (nitroGlow) nitroGlow.SetActive(car.Boosting);

  // --- Posição na Corrida e Voltas ---
  if (positionBadge) {
   if (GameMode.TimeTrial) positionBadge.text = "TREINO";
   else positionBadge.text = "<size=36><b>" + race.Position + "º</b></size> <size=20><color=#8DA4BF>/ " + race.racers.Length + "</color></size>";
  }
  if (lapBadge) lapBadge.text = "VOLTA " + race.Lap + "<size=18><color=#8DA4BF>/" + race.totalLaps + "</color></size>";
  if (gateBadge) gateBadge.text = "CHECK " + (race.Next + 1) + "<size=18><color=#8DA4BF>/" + race.checkpoints.Length + "</color></size>";

  // --- Cronômetro e Melhor Volta ---
  string timeFormatted = System.TimeSpan.FromSeconds(race.Elapsed).ToString(@"mm\:ss\.ff");
  if (chronoText) chronoText.text = timeFormatted;
  else if (timing) timing.text = timeFormatted + (car.Boosting ? "   NITRO" : car.Drifting ? "   DRIFT" : "");

  if (bestLapDisplay) {
   float best = GameMode.TimeTrial ? race.RecordLap : race.BestLap;
   bestLapDisplay.text = (best < float.PositiveInfinity) ? ("MELHOR " + System.TimeSpan.FromSeconds(best).ToString(@"mm\:ss\.ff")) : "MELHOR --:--.--";
  }

  // Tags Nitro / Drift
  if (boostTag) boostTag.SetActive(car.Boosting);
  if (driftTag) driftTag.SetActive(car.Drifting);

  // --- Fade-out automático elegante da legenda de controles ---
  // Some suavemente após 5s de corrida para deixar a tela limpa e cinematográfica
  if (controlsGroup) {
   bool showControls = car.Paused || (race.Countdown > 0) || (race.Elapsed < 5.5f && car.SpeedKmh < 20f);
   controlsGroup.alpha = Mathf.MoveTowards(controlsGroup.alpha, showControls ? 1f : 0f, Time.unscaledDeltaTime * 1.5f);
  }

  // --- Alertas Centrais ---
  UpdateAlerts();

  // Fallback para binds antigos
  if (status && !positionBadge) {
   string prefix = GameMode.TimeTrial ? "CONTRA O TEMPO     " : ("POS " + race.Position + " / " + race.racers.Length + "     ");
   string rec = (GameMode.TimeTrial && race.RecordLap < float.PositiveInfinity) ? ("     MELHOR: " + System.TimeSpan.FromSeconds(race.RecordLap).ToString(@"mm\:ss\.ff")) : "";
   status.text = car.Paused ? "PAUSADO  /  ESC PARA CONTINUAR" : race.Countdown > 0 ? "LARGADA EM " + Mathf.CeilToInt(race.Countdown) : race.Finished ? "" : prefix + "VOLTA " + race.Lap + " / " + race.totalLaps + "     CHECKPOINT " + (race.Next + 1) + " / " + race.checkpoints.Length + rec;
  }
 }

 void ApplyResponsiveLayout(bool force) {
  Rect safe = Screen.safeArea;
  if (!force && lastScreenWidth == Screen.width && lastScreenHeight == Screen.height && lastSafeArea == safe) return;
  lastScreenWidth = Screen.width;
  lastScreenHeight = Screen.height;
  lastSafeArea = safe;

  var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
  if (!canvas || Screen.width <= 0 || Screen.height <= 0) return;
  float scale = Mathf.Max(.01f, canvas.scaleFactor);
  float left = safe.xMin / scale + 24f;
  float right = (Screen.width - safe.xMax) / scale + 24f;
  float bottom = safe.yMin / scale + 20f;
  float top = (Screen.height - safe.yMax) / scale + 20f;

  Place("Status Card", new Vector2(0f, 1f), new Vector2(left, -top));
  Place("Timing Card", new Vector2(1f, 1f), new Vector2(-right, -top));
  Place("Dashboard Card", new Vector2(1f, 0f), new Vector2(-right, bottom));
  Place("Controls Panel", new Vector2(0f, 0f), new Vector2(left, bottom));

  var controls = transform.Find("Controls Panel");
  if (controls) {
   var rt = controls as RectTransform;
   if (rt) rt.sizeDelta = new Vector2(650f, 56f);
   var label = controls.Find("Controls Hint")?.GetComponent<TMP_Text>();
   if (label) { label.fontSize = Mathf.Max(label.fontSize, 16f); label.rectTransform.sizeDelta = new Vector2(620f, 42f); }
  }
 }

 void Place(string childName, Vector2 anchor, Vector2 position) {
  var child = transform.Find(childName) as RectTransform;
  if (!child) return;
  child.anchorMin = child.anchorMax = child.pivot = anchor;
  child.anchoredPosition = position;
 }

 void UpdateAlerts() {
  if (!alertBanner || !alertBannerText) return;

  if (race.Countdown > 0) {
   alertBanner.SetActive(true);
   int cnt = Mathf.CeilToInt(race.Countdown);
   alertBannerText.text = cnt > 0 ? cnt.ToString() : "VAI!";
   alertBannerText.color = cnt > 0 ? new Color(1f, 0.85f, 0.2f) : new Color(0.1f, 0.95f, 1f);
   return;
  }

  // Alerta de contramão
  if (race.checkpoints != null && race.Next < race.checkpoints.Length && car.ForwardSpeed > 6f) {
   var cp = race.checkpoints[race.Next];
   if (Vector3.Dot(car.transform.forward, cp.forward) < -0.45f) {
    alertBanner.SetActive(true);
    alertBannerText.text = "CONTRAMÃO!";
    alertBannerText.color = new Color(1f, 0.1f, 0.2f);
    return;
   }
  }

  // Vácuo aerodinâmico (Slipstream / Drafting)
  if (car.Slipstreaming && !GameMode.TimeTrial) {
   alertBanner.SetActive(true);
   alertBannerText.text = "VÁCUO ATIVO  //  +15 KM/H";
   alertBannerText.color = new Color(0.2f, 0.95f, 1f);
   return;
  }

  // Anúncio de última volta
  if (race.Lap == race.totalLaps && lastLapAnnounced < race.totalLaps) {
   lastLapAnnounced = race.totalLaps;
   alertTimer = 3f;
  }

  if (alertTimer > 0) {
   alertTimer -= Time.deltaTime;
   alertBanner.SetActive(true);
   alertBannerText.text = "ÚLTIMA VOLTA!";
   alertBannerText.color = new Color(1f, 0.85f, 0.1f);
   return;
  }

  alertBanner.SetActive(false);
 }
}
}
