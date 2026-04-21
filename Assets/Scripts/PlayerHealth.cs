using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public Vector2 healthBarOffset = new Vector2(0f, 24f);

    private GameObject createdHudObject;
    private Image healthFillImage;
    private int displayedHealth = int.MinValue;

    public float HealthPercent => maxHealth <= 0 ? 0f : currentHealth / (float)maxHealth;

    void Start()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;

        CreateHealthBar();
        RefreshHealthBar();
    }

    void Update()
    {
        int clampedHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (clampedHealth != currentHealth)
        {
            currentHealth = clampedHealth;
        }

        if (currentHealth != displayedHealth)
        {
            RefreshHealthBar();
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        Debug.Log("Player took damage. Current HP: " + currentHealth);

        RefreshHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void CreateHealthBar()
    {
        if (healthFillImage != null)
        {
            return;
        }

        if (TryCloneStaminaBar())
        {
            return;
        }

        CreateFallbackHealthBar();
    }

    bool TryCloneStaminaBar()
    {
        StaminaUI staminaUi = FindFirstObjectByType<StaminaUI>();
        if (staminaUi == null || staminaUi.fillImage == null)
        {
            return false;
        }

        RectTransform sourceRect = staminaUi.fillImage.rectTransform;
        if (sourceRect.parent == null)
        {
            return false;
        }

        createdHudObject = Instantiate(staminaUi.fillImage.gameObject, sourceRect.parent);
        createdHudObject.name = "HealthMeterBackground";

        RectTransform createdRect = createdHudObject.GetComponent<RectTransform>();
        createdRect.anchorMin = sourceRect.anchorMin;
        createdRect.anchorMax = sourceRect.anchorMax;
        createdRect.pivot = sourceRect.pivot;
        createdRect.sizeDelta = sourceRect.sizeDelta;
        createdRect.anchoredPosition = sourceRect.anchoredPosition + healthBarOffset;
        createdRect.localScale = sourceRect.localScale;

        healthFillImage = createdHudObject.GetComponent<Image>();
        ConfigureFillImage();

        TextMeshProUGUI label = createdHudObject.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = "Health";
            label.name = "HealthMeterText";
        }

        return true;
    }

    void CreateFallbackHealthBar()
    {
        Sprite uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;

        GameObject canvasObject = new GameObject("PlayerHealthHUD");
        createdHudObject = canvasObject;
        Canvas hudCanvas = canvasObject.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();

        GameObject barObject = CreateImageObject(
            "HealthMeter",
            canvasObject.transform,
            uiSprite,
            new Color(0.86f, 0.12f, 0.12f, 1f));

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = Vector2.zero;
        barRect.anchorMax = Vector2.zero;
        barRect.pivot = Vector2.zero;
        barRect.anchoredPosition = new Vector2(0f, 24f);
        barRect.sizeDelta = new Vector2(300f, 20f);

        healthFillImage = barObject.GetComponent<Image>();
        ConfigureFillImage();

        CreateLabel("HealthMeterText", barObject.transform, fontAsset);
    }

    void ConfigureFillImage()
    {
        if (healthFillImage == null)
        {
            return;
        }

        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthFillImage.fillOrigin = 0;
    }

    GameObject CreateImageObject(string objectName, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;

        return imageObject;
    }

    void CreateLabel(string objectName, Transform parent, TMP_FontAsset fontAsset)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(300f, 20f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Health";
        label.color = Color.white;
        label.fontSize = 18f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        if (fontAsset != null)
        {
            label.font = fontAsset;
        }
    }

    void RefreshHealthBar()
    {
        displayedHealth = currentHealth;

        if (healthFillImage == null)
        {
            return;
        }

        float healthPercent = Mathf.Clamp01(HealthPercent);
        healthFillImage.fillAmount = healthPercent;
        healthFillImage.color = Color.Lerp(
            new Color(0.45f, 0.02f, 0.02f, 1f),
            new Color(0.95f, 0.08f, 0.08f, 1f),
            healthPercent);
    }

    void Die()
    {
        Debug.Log("Player died.");
    }

    void OnDestroy()
    {
        if (createdHudObject != null)
        {
            Destroy(createdHudObject);
        }
    }
}
