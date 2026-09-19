using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using NeonCoast;

public static class GameUIBuilder {
 static TMP_FontAsset font;
 static TMP_FontAsset Font() {
  if (font) return font;
  font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/NeonCoastFont.asset");
  return font;
 }

 [MenuItem("Neon Coast/Add Pause and Results UI")]
 public static void Build() {
  if (EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode first.");
  var race = Object.FindFirstObjectByType<RaceSession>();
  if (!race) throw new System.Exception("Open NeonCoast scene first (RaceSession not found).");
  var car = race.player;

  // Garante que existe EventSystem funcional para mouse e UI
  var existingEventSystem = Object.FindFirstObjectByType<EventSystem>();
  if (!existingEventSystem) {
   var esGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
   var module = esGO.GetComponent<InputSystemUIInputModule>();
   module.actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
  }

  // Limpa instâncias antigas
  var existingPause = Object.FindFirstObjectByType<PauseMenu>();
  if (existingPause) Object.DestroyImmediate(existingPause.gameObject);
  var existingResults = Object.FindFirstObjectByType<RaceResults>();
  if (existingResults) Object.DestroyImmediate(existingResults.gameObject);

  // --- Pause Menu ---
  var pauseCanvas = new GameObject("Pause Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
  var pc = pauseCanvas.GetComponent<Canvas>(); pc.renderMode = RenderMode.ScreenSpaceOverlay; pc.sortingOrder = 10;
  var ps = pauseCanvas.GetComponent<CanvasScaler>(); ps.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; ps.referenceResolution = new Vector2(1920, 1080); ps.matchWidthOrHeight = .5f;

  // Painel de fundo escurecido
  var panel = new GameObject("Pause Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(pauseCanvas.transform, false);
  var prt = panel.GetComponent<RectTransform>(); prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.sizeDelta = Vector2.zero;
  panel.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.04f, 0.88f);

  // Título Pause em Português
  Label(panel.transform, "PAUSADO", new Vector2(.5f, .72f), new Vector2(600, 80), 64, new Color(.1f, .9f, 1));

  // Botões
  var resumeBtn = Btn(panel.transform, "Resume Button", "CONTINUAR", new Vector2(.5f, .55f), new Vector2(340, 68), new Color(.1f, .9f, 1f, .25f), new Color(.1f, .95f, 1), 34);
  var restartBtn = Btn(panel.transform, "Restart Button", "REINICIAR", new Vector2(.5f, .43f), new Vector2(340, 68), new Color(.06f, .12f, .22f, .85f), new Color(.85f, .90f, .96f), 30);
  var quitBtn = Btn(panel.transform, "Quit Button", "MENU PRINCIPAL", new Vector2(.5f, .31f), new Vector2(340, 68), new Color(.06f, .12f, .22f, .85f), new Color(1f, .25f, .35f), 28);

  // Dica de navegação por teclas
  var pauseHint = Label(panel.transform, "Pause Hint", new Vector2(.5f, .18f), new Vector2(700, 36), 18, new Color(.6f, .7f, .85f));
  pauseHint.text = "<b>↑ / ↓</b> Navegar     <b>ENTER</b> Selecionar     <b>ESC</b> Voltar ao Jogo";

  var pauseMenu = pauseCanvas.AddComponent<PauseMenu>();
  pauseMenu.pausePanel = panel;
  pauseMenu.car = car;
  pauseMenu.race = race;

  // Vincula os botões para destaque visual direcional
  pauseMenu.buttonBgs = new Image[] {
   resumeBtn.GetComponent<Image>(),
   restartBtn.GetComponent<Image>(),
   quitBtn.GetComponent<Image>()
  };
  pauseMenu.buttonTexts = new TMP_Text[] {
   resumeBtn.GetComponentInChildren<TMP_Text>(),
   restartBtn.GetComponentInChildren<TMP_Text>(),
   quitBtn.GetComponentInChildren<TMP_Text>()
  };

  UnityEventTools.AddPersistentListener(resumeBtn.GetComponent<Button>().onClick, pauseMenu.Resume);
  UnityEventTools.AddPersistentListener(restartBtn.GetComponent<Button>().onClick, pauseMenu.Restart);
  UnityEventTools.AddPersistentListener(quitBtn.GetComponent<Button>().onClick, pauseMenu.QuitToMenu);
  panel.SetActive(false);

  // --- Race Results ---
  var resultsCanvas = new GameObject("Results Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
  var rc = resultsCanvas.GetComponent<Canvas>(); rc.renderMode = RenderMode.ScreenSpaceOverlay; rc.sortingOrder = 15;
  var rs = resultsCanvas.GetComponent<CanvasScaler>(); rs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; rs.referenceResolution = new Vector2(1920, 1080); rs.matchWidthOrHeight = .5f;

  var rPanel = new GameObject("Results Panel", typeof(RectTransform), typeof(Image)); rPanel.transform.SetParent(resultsCanvas.transform, false);
  var rrt = rPanel.GetComponent<RectTransform>(); rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one; rrt.sizeDelta = Vector2.zero;
  rPanel.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.04f, 0.92f);

  // Título Chegada
  Label(rPanel.transform, "CHEGADA", new Vector2(.5f, .78f), new Vector2(600, 90), 72, new Color(.1f, .9f, 1));
  var line = new GameObject("Neon Line", typeof(RectTransform), typeof(Image)); line.transform.SetParent(rPanel.transform, false);
  var lrt = line.GetComponent<RectTransform>(); lrt.anchorMin = lrt.anchorMax = new Vector2(.5f, .72f); lrt.sizeDelta = new Vector2(500, 3);
  line.GetComponent<Image>().color = new Color(.1f, .9f, 1, .7f);

  // Posição
  var posText = Label(rPanel.transform, "1º LUGAR", new Vector2(.5f, .62f), new Vector2(400, 100), 78, Color.white);
  // Tempo de corrida
  Label(rPanel.transform, "TEMPO TOTAL", new Vector2(.5f, .50f), new Vector2(400, 35), 22, new Color(.5f, .6f, .7f));
  var timeText = Label(rPanel.transform, "00:00.00", new Vector2(.5f, .44f), new Vector2(400, 55), 42, Color.white);
  // Melhor volta
  Label(rPanel.transform, "MELHOR VOLTA", new Vector2(.5f, .36f), new Vector2(400, 35), 22, new Color(.5f, .6f, .7f));
  var bestText = Label(rPanel.transform, "00:00.00", new Vector2(.5f, .30f), new Vector2(400, 55), 42, new Color(.1f, .9f, 1));
  // Atalhos
  var promptText = Label(rPanel.transform, "ENTER  Reiniciar     ESC  Menu Principal", new Vector2(.5f, .15f), new Vector2(750, 44), 24, new Color(.82f, .88f, .96f));

  var results = resultsCanvas.AddComponent<RaceResults>();
  results.resultsPanel = rPanel;
  results.positionText = posText;
  results.timeText = timeText;
  results.bestLapText = bestText;
  results.promptText = promptText;
  results.race = race;
  rPanel.SetActive(false);

  EditorSceneManager.MarkSceneDirty(race.gameObject.scene);
  EditorSceneManager.SaveScene(race.gameObject.scene);
  AssetDatabase.SaveAssets();
  Debug.Log("Neon Coast Racing: Pause Menu and Race Results UI added in Portuguese with full keyboard navigation.");
 }

 static TMP_Text Label(Transform parent, string text, Vector2 anchor, Vector2 size, int fontSize, Color color) {
  var go = new GameObject(text, typeof(RectTransform)); go.transform.SetParent(parent, false);
  var t = go.AddComponent<TextMeshProUGUI>(); t.font = Font(); t.text = text; t.fontSize = fontSize; t.color = color; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
  var r = t.rectTransform; r.anchorMin = r.anchorMax = anchor; r.sizeDelta = size; return t;
 }

 static GameObject Btn(Transform parent, string name, string label, Vector2 anchor, Vector2 size, Color bg, Color textColor, int fontSize) {
  var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
  var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.sizeDelta = size;
  go.GetComponent<Image>().color = bg;
  var txtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); txtGO.transform.SetParent(go.transform, false);
  rt = txtGO.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
  var txt = txtGO.GetComponent<TextMeshProUGUI>(); txt.text = label; txt.font = Font(); txt.fontSize = fontSize; txt.color = textColor; txt.alignment = TextAlignmentOptions.Center;
  return go;
 }
}
