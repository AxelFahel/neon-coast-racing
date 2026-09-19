using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Events;
using TMPro;
using System.IO;
using System.Collections.Generic;
using NeonCoast;

public static class MainMenuBuilder {
 const string ScenePath = "Assets/Scenes/MainMenu.unity";
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
  return whiteSprite;
 }

 [MenuItem("Neon Coast/Build Main Menu Scene")]
 public static void Build() {
  if (EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode first.");
  var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
  RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
  RenderSettings.ambientLight = Color.black;
  RenderSettings.fog = false;

  // Camera
  var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
  camGO.tag = "MainCamera";
  var cam = camGO.GetComponent<Camera>();
  cam.clearFlags = CameraClearFlags.SolidColor;
  cam.backgroundColor = new Color(0.015f, 0.02f, 0.04f);

  // Subtle directional light
  var lightGO = new GameObject("Ambient Light", typeof(Light));
  var l = lightGO.GetComponent<Light>();
  l.type = LightType.Directional;
  l.intensity = 0.5f;
  l.color = new Color(0.4f, 0.6f, 1f);
  l.transform.rotation = Quaternion.Euler(40, -30, 0);

  // Canvas
  var canvasGO = new GameObject("Main Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(MainMenu));
  var canvas = canvasGO.GetComponent<Canvas>();
  canvas.renderMode = RenderMode.ScreenSpaceOverlay;
  var scaler = canvasGO.GetComponent<CanvasScaler>();
  scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
  scaler.referenceResolution = new Vector2(1920, 1080);
  scaler.matchWidthOrHeight = 0.5f;

  var cg = canvasGO.GetComponent<CanvasGroup>();
  var menu = canvasGO.GetComponent<MainMenu>();
  menu.canvasGroup = cg;

  // Background gradient/tint panel
  var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
  bgGO.transform.SetParent(canvasGO.transform, false);
  var bgRT = bgGO.GetComponent<RectTransform>();
  bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.sizeDelta = Vector2.zero;
  bgGO.GetComponent<Image>().color = new Color(0.012f, 0.018f, 0.035f, 1f);

  Color cyan = new Color(0.1f, 0.9f, 1f);
  Color pink = new Color(1f, 0.12f, 0.4f);
  Color cardBg = new Color(0.025f, 0.05f, 0.09f, 0.92f);
  Color textWhite = new Color(0.96f, 0.98f, 1f);
  Color textDim = new Color(0.55f, 0.65f, 0.78f);

  // ==========================================
  // TITLE HEADER
  // ==========================================
  var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
  titleGO.transform.SetParent(canvasGO.transform, false);
  var rt = titleGO.GetComponent<RectTransform>();
  rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.88f);
  rt.sizeDelta = new Vector2(900, 110);
  var tTitle = titleGO.GetComponent<TextMeshProUGUI>();
  tTitle.text = "NEON COAST";
  tTitle.font = Font();
  tTitle.fontSize = 86;
  tTitle.fontStyle = FontStyles.Bold;
  tTitle.color = cyan;
  tTitle.alignment = TextAlignmentOptions.Center;

  var lineGO = new GameObject("Neon Line", typeof(RectTransform), typeof(Image));
  lineGO.transform.SetParent(canvasGO.transform, false);
  rt = lineGO.GetComponent<RectTransform>();
  rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.825f);
  rt.sizeDelta = new Vector2(580, 4);
  lineGO.GetComponent<Image>().color = cyan;

  var subGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
  subGO.transform.SetParent(canvasGO.transform, false);
  rt = subGO.GetComponent<RectTransform>();
  rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.785f);
  rt.sizeDelta = new Vector2(600, 40);
  var tSub = subGO.GetComponent<TextMeshProUGUI>();
  tSub.text = "R  A  C  I  N  G";
  tSub.font = Font();
  tSub.fontSize = 28;
  tSub.color = textWhite;
  tSub.alignment = TextAlignmentOptions.Center;

  // ==========================================
  // VEHICLE SELECTION CARD (Left Center)
  // ==========================================
  var carCard = Card(canvasGO.transform, "Vehicle Card", new Vector2(0.5f, 0.5f), new Vector2(-220, -20), new Vector2(540, 410), cardBg, cyan);

  var cardHeader = Label(carCard.transform, "Card Header", new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(500, 24), 16, cyan, TextAlignmentOptions.Center);
  cardHeader.text = "SELECIONE O VEÍCULO";
  cardHeader.fontStyle = FontStyles.Bold;

  // Vehicle Switcher Row ( [ < ]  ASTER GT  [ > ] )
  var prevBtn = CreateButton(carCard.transform, "Prev Car Btn", "<", new Vector2(0.12f, 0.81f), new Vector2(46, 46), new Color(0.08f, 0.18f, 0.3f, 0.8f), cyan, 28);
  UnityEventTools.AddPersistentListener(prevBtn.GetComponent<Button>().onClick, menu.PrevVehicle);

  var nextBtn = CreateButton(carCard.transform, "Next Car Btn", ">", new Vector2(0.88f, 0.81f), new Vector2(46, 46), new Color(0.08f, 0.18f, 0.3f, 0.8f), cyan, 28);
  UnityEventTools.AddPersistentListener(nextBtn.GetComponent<Button>().onClick, menu.NextVehicle);

  menu.carNameText = Label(carCard.transform, "Car Name", new Vector2(0.5f, 0.81f), Vector2.zero, new Vector2(340, 46), 34, textWhite, TextAlignmentOptions.Center);
  menu.carNameText.fontStyle = FontStyles.Bold;

  menu.carTaglineText = Label(carCard.transform, "Car Tagline", new Vector2(0.5f, 0.70f), Vector2.zero, new Vector2(480, 26), 16, cyan, TextAlignmentOptions.Center);

  // Stats Block (Em Português)
  string[] statNames = { "VELOCIDADE", "ACELERAÇÃO", "DIRIGIBILIDADE", "NITRO REGEN" };
  TMP_Text[] statTexts = new TMP_Text[4];
  for (int i = 0; i < 4; i++) {
   float y = 0.58f - i * 0.082f;
   var lbl = Label(carCard.transform, "StatLabel_" + i, new Vector2(0.08f, y), Vector2.zero, new Vector2(160, 24), 15, textDim, TextAlignmentOptions.Left);
   lbl.text = statNames[i];
   lbl.fontStyle = FontStyles.Bold;

   var val = Label(carCard.transform, "StatVal_" + i, new Vector2(0.92f, y), Vector2.zero, new Vector2(240, 24), 15, textWhite, TextAlignmentOptions.Right);
   statTexts[i] = val;
  }
  menu.speedStatText = statTexts[0];
  menu.accelStatText = statTexts[1];
  menu.handlingStatText = statTexts[2];
  menu.nitroStatText = statTexts[3];

  // Paint Swatches Row
  var paintLbl = Label(carCard.transform, "Paint Label", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(480, 22), 15, textDim, TextAlignmentOptions.Center);
  paintLbl.text = "ESCOLHA A COR";
  paintLbl.fontStyle = FontStyles.Bold;

  menu.paintIndicators = new Image[4];
  Color[] paintColors = {
   new Color(0.05f, 0.85f, 1f),
   new Color(1f, 0.12f, 0.35f),
   new Color(1f, 0.7f, 0.05f),
   new Color(0.65f, 0.2f, 1f)
  };

  for (int i = 0; i < 4; i++) {
   int pIndex = i;
   float x = 0.24f + i * 0.17f;
   var swatchGO = new GameObject("Paint_" + i, typeof(RectTransform), typeof(Image), typeof(Button));
   swatchGO.transform.SetParent(carCard.transform, false);
   var srt = swatchGO.GetComponent<RectTransform>();
   srt.anchorMin = srt.anchorMax = new Vector2(x, 0.09f);
   srt.sizeDelta = new Vector2(60, 32);
   swatchGO.GetComponent<Image>().color = paintColors[i];

   var borderGO = new GameObject("SelectedBorder", typeof(RectTransform), typeof(Image));
   borderGO.transform.SetParent(swatchGO.transform, false);
   var bdrt = borderGO.GetComponent<RectTransform>();
   bdrt.anchorMin = Vector2.zero; bdrt.anchorMax = Vector2.one; bdrt.sizeDelta = new Vector2(6, 6);
   borderGO.GetComponent<Image>().color = Color.white;
   borderGO.SetActive(i == 0);

   menu.paintIndicators[i] = swatchGO.GetComponent<Image>();

   var sBtn = swatchGO.GetComponent<Button>();
   sBtn.onClick.AddListener(() => menu.SelectPaint(pIndex));
  }

  // ==========================================
  // ACTIONS COLUMN (Right Center)
  // ==========================================
  var actionsCard = Card(canvasGO.transform, "Actions Card", new Vector2(0.5f, 0.5f), new Vector2(280, -20), new Vector2(360, 410), cardBg, pink);

  var actHeader = Label(actionsCard.transform, "Action Header", new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(320, 24), 16, pink, TextAlignmentOptions.Center);
  actHeader.text = "MODO DE JOGO";
  actHeader.fontStyle = FontStyles.Bold;

  // Botão Corrida
  var btn1 = CreateButton(actionsCard.transform, "Start Button", "CORRIDA", new Vector2(0.5f, 0.72f), new Vector2(300, 70), new Color(0.1f, 0.9f, 1f, 0.25f), cyan, 34);
  UnityEventTools.AddPersistentListener(btn1.GetComponent<Button>().onClick, menu.StartRace);

  // Botão Contra o Tempo
  var btnTT = CreateButton(actionsCard.transform, "Time Trial Button", "CONTRA O TEMPO", new Vector2(0.5f, 0.48f), new Vector2(300, 70), new Color(1f, 0.12f, 0.4f, 0.22f), pink, 28);
  UnityEventTools.AddPersistentListener(btnTT.GetComponent<Button>().onClick, menu.StartTimeTrial);

  // Botão Sair
  var btn2 = CreateButton(actionsCard.transform, "Quit Button", "SAIR DO JOGO", new Vector2(0.5f, 0.24f), new Vector2(300, 55), new Color(0.12f, 0.15f, 0.2f, 0.6f), new Color(1f, 0.3f, 0.4f), 24);
  UnityEventTools.AddPersistentListener(btn2.GetComponent<Button>().onClick, menu.QuitGame);

  // Vincula botões de ação ao MainMenu para navegação direcional
  menu.actionButtonBgs = new Image[] {
   btn1.GetComponent<Image>(),
   btnTT.GetComponent<Image>(),
   btn2.GetComponent<Image>()
  };
  menu.actionButtonTexts = new TMP_Text[] {
   btn1.GetComponentInChildren<TMP_Text>(),
   btnTT.GetComponentInChildren<TMP_Text>(),
   btn2.GetComponentInChildren<TMP_Text>()
  };

  // ==========================================
  // BOTTOM CONTROLS HINT
  // ==========================================
  var hintPill = Card(canvasGO.transform, "Hint Card", new Vector2(0.5f, 0.05f), Vector2.zero, new Vector2(1000, 44), new Color(0.02f, 0.04f, 0.08f, 0.88f), cyan);
  var hTxt = Label(hintPill.transform, "Hint Text", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980, 36), 16, textWhite, TextAlignmentOptions.Center);
  hTxt.text = "<b>↑ / ↓</b> Modo   •   <b>← / →</b> Veículo   •   <b>C / 1-4</b> Cor   •   <b>ENTER</b> Iniciar   •   <b>ESC</b> Sair";

  var esGO = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
  var module = esGO.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
  module.actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");

  if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
  EditorSceneManager.SaveScene(scene, ScenePath);

  var scenes = EditorBuildSettings.scenes;
  var newScenes = new List<EditorBuildSettingsScene>();
  newScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
  foreach (var s in scenes) if (s.path != ScenePath) newScenes.Add(s);
  EditorBuildSettings.scenes = newScenes.ToArray();

  AssetDatabase.SaveAssets();
  Debug.Log("Neon Coast Racing: Main Menu scene rebuilt with Vehicle Selection Showcase in Portuguese!");
 }

 static GameObject Card(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color bgColor, Color borderColor) {
  var card = new GameObject(name, typeof(RectTransform), typeof(Image));
  card.transform.SetParent(parent, false);
  var rt = card.GetComponent<RectTransform>();
  rt.anchorMin = rt.anchorMax = anchor;
  rt.pivot = anchor;
  rt.anchoredPosition = pos;
  rt.sizeDelta = size;
  card.GetComponent<Image>().color = bgColor;

  // Neon accent line
  var line = new GameObject("Accent Line", typeof(RectTransform), typeof(Image));
  line.transform.SetParent(card.transform, false);
  var lrt = line.GetComponent<RectTransform>();
  lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(1, 1);
  lrt.pivot = new Vector2(0.5f, 1);
  lrt.sizeDelta = new Vector2(0, 3);
  lrt.anchoredPosition = Vector2.zero;
  line.GetComponent<Image>().color = borderColor;

  return card;
 }

 static GameObject CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size, Color bgColor, Color textColor, int fontSize) {
  var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
  go.transform.SetParent(parent, false);
  var rt = go.GetComponent<RectTransform>();
  rt.anchorMin = rt.anchorMax = anchor;
  rt.sizeDelta = size;
  go.GetComponent<Image>().color = bgColor;

  var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
  border.transform.SetParent(go.transform, false);
  var brt = border.GetComponent<RectTransform>();
  brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
  brt.sizeDelta = new Vector2(0, 2);
  brt.anchoredPosition = Vector2.zero;
  border.GetComponent<Image>().color = textColor;

  var txtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
  txtGO.transform.SetParent(go.transform, false);
  rt = txtGO.GetComponent<RectTransform>();
  rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
  var txt = txtGO.GetComponent<TextMeshProUGUI>();
  txt.text = label;
  txt.font = Font();
  txt.fontSize = fontSize;
  txt.fontStyle = FontStyles.Bold;
  txt.color = textColor;
  txt.alignment = TextAlignmentOptions.Center;
  return go;
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
  r.anchorMin = r.anchorMax = anchor;
  r.pivot = anchor;
  r.anchoredPosition = pos;
  r.sizeDelta = size;
  return t;
 }
}
