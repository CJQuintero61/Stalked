using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverMenu : MonoBehaviour
{
    public string retrySceneName = "Cellar";
    public string mainMenuSceneName = "MainMenu";
    public bool buildDefaultUiOnStart = true;

    private DeathSceneInfo deathSceneInfo;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        deathSceneInfo = DeathSceneState.GetOrDefault(retrySceneName);
        retrySceneName = deathSceneInfo.RetrySceneName;

        EnsureEventSystem();

        if (buildDefaultUiOnStart && FindFirstObjectByType<Canvas>() == null)
        {
            BuildDefaultUi();
        }
    }

    public void Restart()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        string restartSceneName = DeathSceneActionUtility.ResolveRestartSceneName(activeSceneName, retrySceneName);

        DeathSceneActionUtility.PrepareForRestart();
        SceneManager.LoadScene(restartSceneName);
    }

    public void QuitGame()
    {
        DeathSceneActionUtility.QuitGame();
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    void BuildDefaultUi()
    {
        GameObject canvasObject = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        TMP_FontAsset tmpFont = TMP_Settings.defaultFontAsset;

        GameObject background = CreateImage("Background", canvasRect, Color.black);
        StretchToFill(background.GetComponent<RectTransform>());

        TextMeshProUGUI title = CreateText("YouDiedText", canvasRect, deathSceneInfo.PanelTitle, tmpFont, 64f, new Color(0.5849056f, 0f, 0f, 1f));
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 129f);
        titleRect.sizeDelta = new Vector2(600f, 100f);

        if (!string.IsNullOrWhiteSpace(deathSceneInfo.Subtitle))
        {
            TextMeshProUGUI subtitle = CreateText("Subtitle", canvasRect, deathSceneInfo.Subtitle, tmpFont, 32f, new Color(0.95f, 0.74f, 0.65f, 1f));
            RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRect.pivot = new Vector2(0.5f, 0.5f);
            subtitleRect.anchoredPosition = new Vector2(0f, 55f);
            subtitleRect.sizeDelta = new Vector2(640f, 60f);
        }

        CreateButton("RestartButton", canvasRect, deathSceneInfo.RetryButtonLabel, tmpFont, new Vector2(0f, -60f), Restart);
        CreateButton("QuitButton", canvasRect, deathSceneInfo.QuitButtonLabel, tmpFont, new Vector2(0f, -135f), QuitGame);
    }

    GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;

        return imageObject;
    }

    TextMeshProUGUI CreateText(string objectName, Transform parent, string message, TMP_FontAsset font, float fontSize, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = message;
        if (font != null)
        {
            text.font = font;
        }
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;

        return text;
    }

    void CreateButton(string objectName, Transform parent, string label, TMP_FontAsset font, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(300f, 75f);
        buttonRect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        TextMeshProUGUI buttonText = CreateText(objectName + "Text", buttonRect, label, font, 48f, new Color(0.58431375f, 0f, 0f, 1f));
        StretchToFill(buttonText.GetComponent<RectTransform>());
    }

    void StretchToFill(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
