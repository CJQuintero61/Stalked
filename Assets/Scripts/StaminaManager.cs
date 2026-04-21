using UnityEngine;

public class StaminaManager : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 10f;
    public float staminaRegenRate = 20f;
    public float regenDelay = 1.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip outOfBreathSound;

    private float currentStamina;
    private float regenTimer;
    private bool isSprinting;
    private bool hasPlayedBreath = false; // Prevents the sound from looping infinitely

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

            // CHECK: Did we just hit zero?
            if (currentStamina <= 0f && !hasPlayedBreath)
            {
                PlayBreathSound();
            }
        }
        else
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= regenDelay)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

                // Reset the gatekeeper once they have some breath back (e.g., 20%)
                if (hasPlayedBreath && StaminaPercent > 0.2f)
                {
                    hasPlayedBreath = false;
                }
            }
        }
    }

    void PlayBreathSound()
    {
        if (audioSource != null && outOfBreathSound != null)
        {
            audioSource.PlayOneShot(outOfBreathSound);
            hasPlayedBreath = true; 
        }
    }

    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting && CanSprint;
    }
}