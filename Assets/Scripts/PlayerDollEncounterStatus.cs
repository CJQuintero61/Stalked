using UnityEngine;

public class PlayerDollEncounterStatus : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;

    [Header("Runtime")]
    [SerializeField] private float vulnerabilityTimer;

    public bool IsVulnerable => vulnerabilityTimer > 0f;
    public float VulnerabilityTimeRemaining => Mathf.Max(0f, vulnerabilityTimer);

    void Awake()
    {
        ResolveReferences();
    }

    void Update()
    {
        if (vulnerabilityTimer > 0f)
        {
            vulnerabilityTimer = Mathf.Max(0f, vulnerabilityTimer - Time.deltaTime);
        }
    }

    public bool ResolveDollPunish(int followUpDamage, float vulnerabilityDuration, out bool dealtDamage)
    {
        ResolveReferences();
        dealtDamage = false;

        if (IsVulnerable)
        {
            dealtDamage = true;
            vulnerabilityTimer = 0f;

            if (playerHealth != null && followUpDamage > 0)
            {
                playerHealth.TakeDamage(followUpDamage);
            }

            return true;
        }

        vulnerabilityTimer = Mathf.Max(0.05f, vulnerabilityDuration);
        return true;
    }

    public void ClearVulnerability()
    {
        vulnerabilityTimer = 0f;
    }

    void ResolveReferences()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>() ?? GetComponentInChildren<PlayerHealth>();
        }
    }
}
