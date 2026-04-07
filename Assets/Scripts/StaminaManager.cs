using UnityEngine;

public class StaminaManager : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 10f;    // per second while sprinting
    public float staminaRegenRate = 20f;     // per second while not sprinting
    public float regenDelay = 1.5f;          // seconds before regen starts

    private float currentStamina;
    private float regenTimer;
    private bool isSprinting;

    public float StaminaPercent => currentStamina / maxStamina;
    public bool CanSprint => currentStamina > 0f;

    void Start()
    {
        currentStamina = maxStamina;
    }

    void Update()
    {
        if (isSprinting && currentStamina > 0f)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            regenTimer = 0f;
        }
        else
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= regenDelay)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            }
        }
    }

    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting && CanSprint;
    }
}