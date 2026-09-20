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

 [MenuItem("Neon Coast/Build Main Menu Scene")]
 public static void Build() {
  if (EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode first.");
  var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
  RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
  RenderSettings.ambientLight = new Color(0.18f, 0.24f, 0.35f);
  RenderSettings.fog = false;

  // Câmera principal focada na vitrine 3D
  var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
  camGO.tag = "MainCamera";
  var cam = camGO.GetComponent<Camera>();
  cam.clearFlags = CameraClearFlags.SolidColor;
  cam.backgroundColor = new Color(0.012f, 0.016f, 0.028f);
  cam.transform.position = new Vector3(0f, 1.25f, -6f);
  cam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);

  // Iluminação de estúdio automotivo
  var keyLightGO = new GameObject("Studio Key Light", typeof(Light));
  var keyL = keyLightGO.GetComponent<Light>();
  keyL.type = LightType.Directional;
  keyL.intensity = 1.4f;
  keyL.color = new Color(0.92f, 0.95f, 1f);
  keyL.transform.rotation = Quaternion.Euler(38f, -25f, 0f);

  var rimLightGO = new GameObject("Studio Rim Light", typeof(Light));
  var rimL = rimLightGO.GetComponent<Light>();
  rimL.type = LightType.Directional;
  rimL.intensity = 0.8f;
  rimL.color = new Color(0.1f, 0.85f, 1f);
  rimL.transform.rotation = Quaternion.Euler(-20f, 150f, 0f);

  // Canvas ScreenSpaceOverlay
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

  // Painel translúcido lateral esquerdo (apenas sob a UI, deixando a vitrine do carro 100% livre à direita)
  var leftPanelGO = new GameObject("Left UI Backdrop", typeof(RectTransform), typeof(Image));
  leftPanelGO.transform.SetParent(canvasGO.transform, false);
  var lpRT = leftPanelGO.GetComponent<RectTransform>();
  lpRT.anchorMin = new Vector2(0f, 0f);
  lpRT.anchorMax = new Vector2(0.48f, 1f);
  lpRT.sizeDelta = Vector2.zero;
  leftPanelGO.GetComponent<Image>().color = new Color(0.010f, 0.015f, 0.026f, 0.70f);

  Color cyan = new Color(0.1f, 0.9f, 1f);
  Color pink = new Color(1f, 0.12f, 0.4f);
  Color cardBg = new Color(0.022f, 0.045f, 0.085f, 0.90f);
  Color textWhite = new Color(0.96f, 0.98f, 1f);
  Color textDim = new Color(0.55f, 0.65f, 0.78f);

  // ==========================================
  // TITLE HEADER (Canto Superior Esquerdo)
  // ==========================================
  var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
  titleGO.transform.SetParent(canvasGO.transform, false);
  var rt = titleGO.GetComponent<RectTransform>();
  rt.anchorMin = rt.anchorMax = new Vector2(0.24f, 0.91f);
  rt.sizeDelta = new Vector2(650, 75);
  var tTitle = titleGO.GetComponent<TextMeshProUGUI>();
  tTitle.text = "NEON COAST";
  tTitle.font = Font();
  tTitle.fontSize = 62;
  tTitle.fontStyle = FontStyles.Bold;
  tTitle.color = cyan;
  tTitle.alignment = TextAlignmentOptions.Center;

  var lineGO = new GameObject("Neon Line", typeof(RectTransform), typeof(Image));
  lineGO.transform.SetParent(canvasGO.transform, false);
  rt = lineGO.GetComponent<RectTransform>();
  rt.anchorMin = rt.anchorMax = new Vector2(0.24f, 0.852f);
  rt.sizeDelta = new Vector2(420, 3);
  lineGO.GetComponent<Image>().color = cyan;

  var subGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
  subGO.transform.SetParent(canvasGO.transform, false);
  rt = subGO.GetComponent<RectTransform>();
  rt.anchorMin = rt.anchorMax = new Vector2(0.24f, 0.822f);
  rt.sizeDelta = new Vector2(450, 30);
  var tSub = subGO.GetComponent<TextMeshProUGUI>();
  tSub.text = "R  A  C  I  N  G";
  tSub.font = Font();
  tSub.fontSize = 20;
  tSub.color = textWhite;
  tSub.alignment = TextAlignmentOptions.Center;

  // ==========================================
  // VEHICLE SELECTION CARD (Lado Esquerdo Superior)
  // ==========================================
  var carCard = Card(canvasGO.transform, "Vehicle Card", new Vector2(0.24f, 0.53f), Vector2.zero, new Vector2(530, 410), cardBg, cyan);

  var cardHeader = Label(carCard.transform, "Card Header", new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(490, 24), 16, cyan, TextAlignmentOptions.Center);
  cardHeader.text = "SELECIONE O VEÍCULO";
  cardHeader.fontStyle = FontStyles.Bold;

  // Vehicle Switcher Row ( [ < ]  ASTER GT  [ > ] )
  var prevBtn = CreateButton(carCard.transform, "Prev Car Btn", "<", new Vector2(0.11f, 0.82f), new Vector2(46, 46), new Color(0.08f, 0.18f, 0.3f, 0.8f), cyan, 28);
  UnityEventTools.AddPersistentListener(prevBtn.GetComponent<Button>().onClick, menu.PrevVehicle);

  var nextBtn = CreateButton(carCard.transform, "Next Car Btn", ">", new Vector2(0.89f, 0.82f), new Vector2(46, 46), new Color(0.08f, 0.18f, 0.3f, 0.8f), cyan, 28);
  UnityEventTools.AddPersistentListener(nextBtn.GetComponent<Button>().onClick, menu.NextVehicle);

  menu.carNameText = Label(carCard.transform, "Car Name", new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(330, 46), 32, textWhite, TextAlignmentOptions.Center);
  menu.carNameText.fontStyle = FontStyles.Bold;

  menu.carTaglineText = Label(carCard.transform, "Car Tagline", new Vector2(0.5f, 0.70f), Vector2.zero, new Vector2(480, 26), 15, cyan, TextAlignmentOptions.Center);

  // Stats Block
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
  var paintLbl = Label(carCard.transform, "Paint Label", new Vector2(0.5f, 0.19f), Vector2.zero, new Vector2(480, 22), 15, textDim, TextAlignmentOptions.Center);
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
   float x = 0.24f + i * 0.17f;
   var swatchGO = new GameObject("Paint_" + i, typeof(RectTransform), typeof(Image), typeof(Button));
   swatchGO.transform.SetParent(carCard.transform, false);
   var srt = swatchGO.GetComponent<RectTransform>();
   srt.anchorMin = srt.anchorMax = new Vector2(x, 0.09f);
   srt.sizeDelta = new Vector2(62, 34);
   swatchGO.GetComponent<Image>().color = paintColors[i];

   var borderGO = new GameObject("SelectedBorder", typeof(RectTransform), typeof(Image));
   borderGO.transform.SetParent(swatchGO.transform, false);
   var bdrt = borderGO.GetComponent<RectTransform>();
   bdrt.anchorMin = Vector2.zero; bdrt.anchorMax = Vector2.one; bdrt.sizeDelta = new Vector2(6, 6);
   borderGO.GetComponent<Image>().color = Color.white;
   borderGO.SetActive(i == 0);

   menu.paintIndicators[i] = swatchGO.GetComponent<Image>();
  }

  // ==========================================
  // ACTIONS ROW / CARD (Lado Esquerdo Inferior)
  // ==========================================
  var actionsCard = Card(canvasGO.transform, "Actions Card", new Vector2(0.24f, 0.19f), Vector2.zero, new Vector2(530, 160), cardBg, pink);

  var actHeader = Label(actionsCard.transform, "Action Header", new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(480, 22), 15, pink, TextAlignmentOptions.Center);
  actHeader.text = "MODO DE JOGO";
  actHeader.fontStyle = FontStyles.Bold;

  var btn1 = CreateButton(actionsCard.transform, "Start Button", "CORRIDA", new Vector2(0.22f, 0.40f), new Vector2(150, 56), new Color(0.1f, 0.9f, 1f, 0.25f), cyan, 24);
  UnityEventTools.AddPersistentListener(btn1.GetComponent<Button>().onClick, menu.StartRace);

  var btnTT = CreateButton(actionsCard.transform, "Time Trial Button", "CONTRA O TEMPO", new Vector2(0.54f, 0.40f), new Vector2(160, 56), new Color(1f, 0.12f, 0.4f, 0.22f), pink, 18);
  UnityEventTools.AddPersistentListener(btnTT.GetComponent<Button>().onClick, menu.StartTimeTrial);

  var btn2 = CreateButton(actionsCard.transform, "Quit Button", "SAIR", new Vector2(0.85f, 0.40f), new Vector2(110, 56), new Color(0.12f, 0.15f, 0.2f, 0.6f), new Color(1f, 0.3f, 0.4f), 20);
  UnityEventTools.AddPersistentListener(btn2.GetComponent<Button>().onClick, menu.QuitGame);

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
  var hintPill = Card(canvasGO.transform, "Hint Card", new Vector2(0.5f, 0.045f), Vector2.zero, new Vector2(1000, 40), new Color(0.02f, 0.04f, 0.08f, 0.88f), cyan);
  var hTxt = Label(hintPill.transform, "Hint Text", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980, 32), 15, textWhite, TextAlignmentOptions.Center);
  hTxt.text = "<b>↑ / ↓</b> Modo   •   <b>← / →</b> Veículo   •   <b>C / 1-4</b> Cor   •   <b>ENTER</b> Iniciar   •   <b>ESC</b> Sair";

  // EventSystem
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
  Debug.Log("[MainMenuBuilder] Main Menu scene successfully built with 3D Showcase setup!");
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
