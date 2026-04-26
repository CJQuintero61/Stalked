using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverMenu : MonoBehaviour
{
    public string retrySceneName = "Cellar";
    public string mainMenuSceneName = "MainMenu";
    public bool buildDefaultUiOnStart = true;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureEventSystem();

        if (buildDefaultUiOnStart && FindFirstObjectByType<Canvas>() == null)
        {
            BuildDefaultUi();
        }
    }

    public void RetryCellar()
    {
        SceneManager.LoadScene(retrySceneName);
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Player quit from Game Over.");
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
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject canvasObject = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        GameObject background = CreateImage("Background", canvasRect, new Color(0.015f, 0.01f, 0.008f, 1f));
        StretchToFill(background.GetComponent<RectTransform>());

        GameObject panel = CreateImage("Panel", canvasRect, new Color(0.09f, 0.015f, 0.012f, 0.92f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720f, 460f);
        panelRect.anchoredPosition = Vector2.zero;

        Text title = CreateText("Title", panelRect, "GAME OVER", uiFont, 82, Color.white);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 130f);
        titleRect.sizeDelta = new Vector2(640f, 110f);

        Text subtitle = CreateText("Subtitle", panelRect, "The doll found you in the cellar.", uiFont, 32, new Color(0.95f, 0.74f, 0.65f, 1f));
        RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchoredPosition = new Vector2(0f, 55f);
        subtitleRect.sizeDelta = new Vector2(640f, 60f);

        CreateButton("RetryButton", panelRect, "Retry Cellar", uiFont, new Vector2(0f, -40f), RetryCellar);
        CreateButton("MainMenuButton", panelRect, "Main Menu", uiFont, new Vector2(0f, -125f), ReturnToMainMenu);
    }

    GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;

        return imageObject;
    }

    Text CreateText(string objectName, Transform parent, string message, Font font, int fontSize, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.text = message;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;

        return text;
    }

    void CreateButton(string objectName, Transform parent, string label, Font font, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(360f, 62f);
        buttonRect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.72f, 0.08f, 0.05f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text buttonText = CreateText("Text", buttonRect, label, font, 28, Color.white);
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
