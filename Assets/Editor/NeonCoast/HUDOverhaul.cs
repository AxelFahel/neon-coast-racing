using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using NeonCoast;

public static class HUDOverhaul {
 static TMP_FontAsset font;
 static TMP_FontAsset Font() {
  if (font) return font;
  font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/NeonCoastFont.asset");
  if (!font) {
   var f = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
   if (f) {
    font = TMP_FontAsset.CreateFontAsset(f);
    font.name = "Neon Coast UI Font";
    AssetDatabase.CreateAsset(font, "Assets/UI/NeonCoastFont.asset");
   }
  }
  return font;
 }

 static Sprite whiteSprite;
 static Sprite WhiteSprite() {
  if (whiteSprite) return whiteSprite;
  whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/NitroSprite.asset");
  if (!whiteSprite) {
   var tex = new Texture2D(4, 4);
   var cols = new Color[16];
   for (int i = 0; i < 16; i++) cols[i] = Color.white;
   tex.SetPixels(cols); tex.Apply();
   AssetDatabase.CreateAsset(tex, "Assets/UI/WhitePixelTex.asset");
   whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
   AssetDatabase.CreateAsset(whiteSprite, "Assets/UI/WhitePixelSprite.asset");
  }
  return whiteSprite;
 }

 [MenuItem("Neon Coast/Rebuild High-Visibility HUD")]
 public static void RebuildHUD() {
  if (EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode first.");
  var race = Object.FindFirstObjectByType<RaceSession>();
  if (!race) throw new System.Exception("Open NeonCoast scene first (RaceSession not found).");
  var car = race.player;
  if (!car) car = Object.FindFirstObjectByType<ArcadeCar>();

  // Remove HUD anterior se presente
  var oldHUD = GameObject.Find("Race HUD");
  if (oldHUD) Object.DestroyImmediate(oldHUD);

  // Canvas ScreenSpaceOverlay
  var canvasGO = new GameObject("Race HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(RacingHUD));
  var canvas = canvasGO.GetComponent<Canvas>();
  canvas.renderMode = RenderMode.ScreenSpaceOverlay;
  canvas.sortingOrder = 5;

  var scaler = canvasGO.GetComponent<CanvasScaler>();
  scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
  scaler.referenceResolution = new Vector2(1920, 1080);
  scaler.matchWidthOrHeight = 0.5f;

  var hud = canvasGO.GetComponent<RacingHUD>();
  hud.car = car;
  hud.race = race;

  // Paleta moderna de UI translúcida (Glassmorphism + Neon Synthwave)
  Color glassBg = new Color(0.012f, 0.025f, 0.05f, 0.52f);
  Color cyanAccent = new Color(0.1f, 0.9f, 1f, 0.95f);
  Color pinkAccent = new Color(1f, 0.1f, 0.35f, 0.95f);
  Color amberAccent = new Color(1f, 0.75f, 0.1f, 0.95f);
  Color textWhite = new Color(0.96f, 0.98f, 1f, 1f);
  Color textDim = new Color(0.60f, 0.72f, 0.85f, 1f);
  Color pillBg = new Color(0.04f, 0.09f, 0.17f, 0.75f);

  // =========================================================================
  // TOP-LEFT: Telemetria de Corrida Esportiva (Posição, Voltas e Checkpoints)
  // =========================================================================
  var statusCard = Card(canvasGO.transform, "Status Card", new Vector2(0, 1), new Vector2(24, -20), new Vector2(360, 96), glassBg, cyanAccent);

  hud.carNameText = Label(statusCard.transform, "Brand", new Vector2(0, 1), new Vector2(14, -10), new Vector2(332, 20), 13, cyanAccent, TextAlignmentOptions.Left);
  hud.carNameText.fontStyle = FontStyles.Bold;

  var badgesRow = new GameObject("Badges Row", typeof(RectTransform));
  badgesRow.transform.SetParent(statusCard.transform, false);
  var brt = badgesRow.GetComponent<RectTransform>();
  brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0, 1);
  brt.anchoredPosition = new Vector2(14, -36);
  brt.sizeDelta = new Vector2(332, 48);

  // Posição
  var posPill = Pill(badgesRow.transform, "Position Pill", new Vector2(0, 0), new Vector2(115, 48), pillBg, cyanAccent);
  hud.positionBadge = Label(posPill.transform, "Pos Text", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(105, 42), 24, textWhite, TextAlignmentOptions.Center);
  hud.positionBadge.fontStyle = FontStyles.Bold;

  // Voltas
  var lapPill = Pill(badgesRow.transform, "Lap Pill", new Vector2(122, 0), new Vector2(104, 48), pillBg, new Color(0.2f, 0.4f, 0.6f, 0.5f));
  hud.lapBadge = Label(lapPill.transform, "Lap Text", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 42), 20, textWhite, TextAlignmentOptions.Center);
  hud.lapBadge.fontStyle = FontStyles.Bold;

  // Checkpoints
  var gatePill = Pill(badgesRow.transform, "Gate Pill", new Vector2(232, 0), new Vector2(98, 48), pillBg, new Color(0.2f, 0.4f, 0.6f, 0.5f));
  hud.gateBadge = Label(gatePill.transform, "Gate Text", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 42), 19, textDim, TextAlignmentOptions.Center);

  // =========================================================================
  // TOP-RIGHT: Cronômetro Digital e Melhor Volta
  // =========================================================================
  var timingCard = Card(canvasGO.transform, "Timing Card", new Vector2(1, 1), new Vector2(-24, -20), new Vector2(300, 96), glassBg, cyanAccent);

  hud.chronoText = Label(timingCard.transform, "Chrono", new Vector2(1, 1), new Vector2(-16, -10), new Vector2(268, 48), 44, textWhite, TextAlignmentOptions.Right);
  hud.chronoText.fontStyle = FontStyles.Bold | FontStyles.Italic;

  hud.bestLapDisplay = Label(timingCard.transform, "Best Lap", new Vector2(1, 1), new Vector2(-16, -60), new Vector2(268, 22), 16, cyanAccent, TextAlignmentOptions.Right);

  // Tags Nitro / Drift dinâmicas
  var tagsRow = new GameObject("Tags Row", typeof(RectTransform));
  tagsRow.transform.SetParent(timingCard.transform, false);
  var trt = tagsRow.GetComponent<RectTransform>();
  trt.anchorMin = trt.anchorMax = new Vector2(1, 0);
  trt.anchoredPosition = new Vector2(-16, -18);
  trt.sizeDelta = new Vector2(200, 22);

  var boostGO = Pill(tagsRow.transform, "Boost Tag", new Vector2(-105, 0), new Vector2(90, 22), new Color(0.05f, 0.7f, 0.85f, 0.95f), cyanAccent);
  var bTxt = Label(boostGO.transform, "Text", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 18), 14, Color.black, TextAlignmentOptions.Center);
  bTxt.text = "NITRO"; bTxt.fontStyle = FontStyles.Bold;
  hud.boostTag = boostGO;
  boostGO.SetActive(false);

  var driftGO = Pill(tagsRow.transform, "Drift Tag", new Vector2(-5, 0), new Vector2(90, 22), new Color(1f, 0.1f, 0.35f, 0.95f), pinkAccent);
  var dTxt = Label(driftGO.transform, "Text", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 18), 14, Color.white, TextAlignmentOptions.Center);
  dTxt.text = "DRIFT"; dTxt.fontStyle = FontStyles.Bold;
  hud.driftTag = driftGO;
  driftGO.SetActive(false);

  // =========================================================================
  // BOTTOM-RIGHT: Cockpit Cyberpunk (Velocímetro, Marcha, Tacômetro e Nitro)
  // =========================================================================
  var dashCard = Card(canvasGO.transform, "Dashboard Card", new Vector2(1, 0), new Vector2(-24, 20), new Vector2(330, 160), glassBg, pinkAccent);

  // Dígitos da Velocidade
  hud.speedDigits = Label(dashCard.transform, "Speed Digits", new Vector2(1, 1), new Vector2(-105, -8), new Vector2(200, 78), 78, textWhite, TextAlignmentOptions.Right);
  hud.speedDigits.fontStyle = FontStyles.Bold | FontStyles.Italic;

  // Unidade KM/H
  var kmhLabel = Label(dashCard.transform, "KMH Unit", new Vector2(1, 1), new Vector2(-38, -20), new Vector2(65, 24), 18, cyanAccent, TextAlignmentOptions.Left);
  kmhLabel.text = "KM/H"; kmhLabel.fontStyle = FontStyles.Bold;

  // Cápsula de Marcha (Gear Indicator)
  var gearPill = Pill(dashCard.transform, "Gear Pill", new Vector2(18, 92), new Vector2(44, 48), pillBg, amberAccent);
  hud.gearText = Label(gearPill.transform, "Gear Text", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 42), 26, amberAccent, TextAlignmentOptions.Center);
  hud.gearText.fontStyle = FontStyles.Bold;

  // Barra de Tacômetro / Rotação
  var speedTrack = new GameObject("Speed Bar Track", typeof(RectTransform), typeof(Image));
  speedTrack.transform.SetParent(dashCard.transform, false);
  var strt = speedTrack.GetComponent<RectTransform>();
  strt.anchorMin = strt.anchorMax = new Vector2(0.5f, 0);
  strt.anchoredPosition = new Vector2(0, 68);
  strt.sizeDelta = new Vector2(294, 7);
  speedTrack.GetComponent<Image>().color = new Color(0.08f, 0.14f, 0.22f, 0.85f);

  var sBarGO = new GameObject("Speed Bar Fill", typeof(RectTransform), typeof(Image));
  sBarGO.transform.SetParent(speedTrack.transform, false);
  var sbrt = sBarGO.GetComponent<RectTransform>();
  sbrt.anchorMin = Vector2.zero; sbrt.anchorMax = Vector2.one; sbrt.sizeDelta = Vector2.zero;
  var sImg = sBarGO.GetComponent<Image>();
  sImg.sprite = WhiteSprite();
  sImg.type = Image.Type.Filled;
  sImg.fillMethod = Image.FillMethod.Horizontal;
  sImg.color = cyanAccent;
  hud.speedBar = sImg;

  // Rótulo Turbo Nitro
  var nitroLbl = Label(dashCard.transform, "Nitro Label", new Vector2(0, 0), new Vector2(18, 42), new Vector2(140, 18), 13, cyanAccent, TextAlignmentOptions.Left);
  nitroLbl.text = "TURBO NITRO"; nitroLbl.fontStyle = FontStyles.Bold;

  // Barra de Célula de Energia de Nitro
  var nitroFrame = new GameObject("Nitro Frame", typeof(RectTransform), typeof(Image));
  nitroFrame.transform.SetParent(dashCard.transform, false);
  var nfrt = nitroFrame.GetComponent<RectTransform>();
  nfrt.anchorMin = nfrt.anchorMax = new Vector2(0.5f, 0);
  nfrt.anchoredPosition = new Vector2(0, 22);
  nfrt.sizeDelta = new Vector2(294, 16);
  nitroFrame.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.16f, 0.9f);

  var nitroBarGO = new GameObject("Nitro Bar Fill", typeof(RectTransform), typeof(Image));
  nitroBarGO.transform.SetParent(nitroFrame.transform, false);
  var nbrt = nitroBarGO.GetComponent<RectTransform>();
  nbrt.anchorMin = Vector2.zero; nbrt.anchorMax = Vector2.one; nbrt.sizeDelta = Vector2.zero;
  var nImg = nitroBarGO.GetComponent<Image>();
  nImg.sprite = WhiteSprite();
  nImg.type = Image.Type.Filled;
  nImg.fillMethod = Image.FillMethod.Horizontal;
  nImg.color = new Color(0.05f, 0.95f, 1f);
  hud.nitroBar = nImg;
  hud.nitro = nImg;

  // Glow de Nitro Boost
  var glowGO = new GameObject("Nitro Glow Overlay", typeof(RectTransform), typeof(Image));
  glowGO.transform.SetParent(nitroFrame.transform, false);
  var gwrt = glowGO.GetComponent<RectTransform>();
  gwrt.anchorMin = Vector2.zero; gwrt.anchorMax = Vector2.one; gwrt.sizeDelta = new Vector2(4, 4);
  var gwImg = glowGO.GetComponent<Image>();
  gwImg.color = new Color(0.2f, 1f, 1f, 0.40f);
  hud.nitroGlow = glowGO;
  glowGO.SetActive(false);

  // =========================================================================
  // BOTTOM-LEFT: Tira Minimalista de Controles com Fade-out Automático
  // Fica visível no início da corrida e desaparece para deixar a visão limpa!
  // =========================================================================
  var ctrlPanel = new GameObject("Controls Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
  ctrlPanel.transform.SetParent(canvasGO.transform, false);
  var cprt = ctrlPanel.GetComponent<RectTransform>();
  cprt.anchorMin = cprt.anchorMax = cprt.pivot = new Vector2(0, 0);
  cprt.anchoredPosition = new Vector2(24, 20);
  cprt.sizeDelta = new Vector2(650, 56);
  ctrlPanel.GetComponent<Image>().color = glassBg;

  var ctrlAccent = new GameObject("Left Accent", typeof(RectTransform), typeof(Image));
  ctrlAccent.transform.SetParent(ctrlPanel.transform, false);
  var caBrt = ctrlAccent.GetComponent<RectTransform>();
  caBrt.anchorMin = new Vector2(0, 0); caBrt.anchorMax = new Vector2(0, 1);
  caBrt.sizeDelta = new Vector2(3, 0); caBrt.anchoredPosition = Vector2.zero;
  ctrlAccent.GetComponent<Image>().color = cyanAccent;

  hud.controlsGroup = ctrlPanel.GetComponent<CanvasGroup>();

  var ctrlTxt = Label(ctrlPanel.transform, "Controls Hint", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 42), 16, textWhite, TextAlignmentOptions.Left);
  ctrlTxt.text = "<color=#19E6FF><b>W/RT</b></color> Acelerar   <color=#19E6FF><b>S/LT</b></color> Freio   <color=#19E6FF><b>A-D</b></color> Direção   <color=#FF1A59><b>ESPAÇO</b></color> Drift   <color=#19E6FF><b>SHIFT</b></color> Nitro   <color=#8DA4BF><b>ESC</b></color> Pause";

  // =========================================================================
  // ALERTA CENTRAL DINÂMICO
  // =========================================================================
  var bannerGO = new GameObject("Alert Banner", typeof(RectTransform), typeof(Image));
  bannerGO.transform.SetParent(canvasGO.transform, false);
  var bnrt = bannerGO.GetComponent<RectTransform>();
  bnrt.anchorMin = bnrt.anchorMax = new Vector2(0.5f, 0.68f);
  bnrt.sizeDelta = new Vector2(500, 75);
  bannerGO.GetComponent<Image>().color = new Color(0.015f, 0.035f, 0.07f, 0.90f);

  var bBorderTop = new GameObject("Top Border", typeof(RectTransform), typeof(Image));
  bBorderTop.transform.SetParent(bannerGO.transform, false);
  var bt = bBorderTop.GetComponent<RectTransform>();
  bt.anchorMin = new Vector2(0, 1); bt.anchorMax = new Vector2(1, 1);
  bt.sizeDelta = new Vector2(0, 3); bt.anchoredPosition = Vector2.zero;
  bBorderTop.GetComponent<Image>().color = cyanAccent;

  hud.alertBannerText = Label(bannerGO.transform, "Banner Text", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480, 65), 48, textWhite, TextAlignmentOptions.Center);
  hud.alertBannerText.fontStyle = FontStyles.Bold;
  hud.alertBanner = bannerGO;
  bannerGO.SetActive(false);

  EditorSceneManager.MarkSceneDirty(race.gameObject.scene);
  EditorSceneManager.SaveScene(race.gameObject.scene);
  AssetDatabase.SaveAssets();
  Debug.Log("Neon Coast Racing: Modern Game Designer HUD successfully built with glassmorphism, dynamic tachometer, and auto-fading controls!");
 }

 static GameObject Card(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color bgColor, Color borderColor) {
  var card = new GameObject(name, typeof(RectTransform), typeof(Image));
  card.transform.SetParent(parent, false);
  var rt = card.GetComponent<RectTransform>();
  rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
  rt.anchoredPosition = pos;
  rt.sizeDelta = size;
  card.GetComponent<Image>().color = bgColor;

  // Linha de acento neon
  var line = new GameObject("Accent Line", typeof(RectTransform), typeof(Image));
  line.transform.SetParent(card.transform, false);
  var lrt = line.GetComponent<RectTransform>();
  lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(1, 1);
  lrt.pivot = new Vector2(0.5f, 1);
  lrt.sizeDelta = new Vector2(0, 2);
  lrt.anchoredPosition = Vector2.zero;
  line.GetComponent<Image>().color = borderColor;

  return card;
 }

 static GameObject Pill(Transform parent, string name, Vector2 pos, Vector2 size, Color bgColor, Color outlineColor) {
  var pill = new GameObject(name, typeof(RectTransform), typeof(Image));
  pill.transform.SetParent(parent, false);
  var rt = pill.GetComponent<RectTransform>();
  rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0);
  rt.anchoredPosition = pos;
  rt.sizeDelta = size;
  pill.GetComponent<Image>().color = bgColor;

  var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
  border.transform.SetParent(pill.transform, false);
  var brt = border.GetComponent<RectTransform>();
  brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
  brt.sizeDelta = new Vector2(0, 2);
  brt.anchoredPosition = Vector2.zero;
  border.GetComponent<Image>().color = outlineColor;

  return pill;
 }

 static TMP_Text Label(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, Color color, TextAlignmentOptions align) {
  var go = new GameObject(name, typeof(RectTransform));
  go.transform.SetParent(parent, false);
  var t = go.AddComponent<TextMeshProUGUI>();
  t.font = Font();
  t.fontSize = fontSize;
  t.color = color;
  t.alignment = align;
  t.raycastTarget = false;

  var r = t.rectTransform;
  r.anchorMin = r.anchorMax = r.pivot = anchor;
  r.anchoredPosition = pos;
  r.sizeDelta = size;
  return t;
 }
}
