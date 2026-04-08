using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    public StaminaManager staminaManager;
    public Image fillImage;
    public Color fullColor = Color.yellow;
    public Color emptyColor = Color.red;

    void Update()
    {
        if (staminaManager == null || fillImage == null) return;

        float pct = staminaManager.StaminaPercent;
        fillImage.fillAmount = pct; // 1 = full, 0 = empty
        fillImage.color = Color.Lerp(emptyColor, fullColor, pct);
    }
}