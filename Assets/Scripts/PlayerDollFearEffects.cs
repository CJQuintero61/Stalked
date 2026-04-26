using UnityEngine;

[DefaultExecutionOrder(1000)]
public class PlayerDollFearEffects : MonoBehaviour
{
    [Header("References")]
    public Transform playerRoot;
    public Transform cameraTarget;
    public Transform pressureSource;

    [Header("Debug")]
    public bool effectsEnabled = false;

    [Header("General")]
    public float presenceSmoothing = 2.5f;

    [Header("Peek Effects")]
    public float peekFovOffset = 0.4f;
    public float peekPitch = 0.12f;
    public float peekRoll = 0.18f;

    [Header("Threat Effects")]
    public float threatFovOffset = 1.25f;
    public float threatPitch = 0.3f;
    public float threatRoll = 0.45f;
    public float threatYawPullSpeed = 0f;

    [Header("Lunge Effects")]
    public float lungeFovOffset = 2.4f;
    public float lungePitch = 0.9f;
    public float lungeRoll = 0.65f;
    public float lungeYawPullSpeed = 0f;

    [Header("Scare Effects")]
    public float scareFovOffset = 3.6f;
    public float scarePitchOffset = 1.6f;
    public float scareRollOffset = 0.9f;
    public float scareYawPullSpeed = 0f;

    [Header("Flash Feedback")]
    public float flashResponseDuration = 0.2f;
    public float flashFovKick = -0.6f;
    public float flashRollKick = 0.4f;

    private Camera cachedCamera;
    private float baseFieldOfView;
    private float desiredPeek;
    private float desiredThreat;
    private float desiredLunge;
    private float currentPeek;
    private float currentThreat;
    private float currentLunge;
    private bool presenceTouchedThisFrame;
    private float scareTimer;
    private float scareDuration;
    private float scareIntensity;
    private float flashTimer;
    private Quaternion lastTargetOffset = Quaternion.identity;
    private Quaternion lastCameraRoll = Quaternion.identity;
    private bool disabledCleanupApplied;

    void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        if (cachedCamera != null)
        {
            baseFieldOfView = cachedCamera.fieldOfView;
        }
    }

    void Start()
    {
        if (playerRoot == null && transform.parent != null)
        {
            playerRoot = transform.parent;
        }
    }

    void OnDisable()
    {
        RestoreDefaults();
    }

    public void SetPressureSource(Transform source)
    {
        pressureSource = source;
    }

    public void SetPeekPresence(float intensity)
    {
        if (!effectsEnabled)
        {
            return;
        }

        presenceTouchedThisFrame = true;
        desiredPeek = Mathf.Max(desiredPeek, Mathf.Clamp01(intensity));
    }

    public void SetThreatPresence(float intensity)
    {
        if (!effectsEnabled)
        {
            return;
        }

        presenceTouchedThisFrame = true;
        desiredThreat = Mathf.Max(desiredThreat, Mathf.Clamp01(intensity));
    }

    public void SetLungePresence(float intensity)
    {
        if (!effectsEnabled)
        {
            return;
        }

        presenceTouchedThisFrame = true;
        desiredLunge = Mathf.Max(desiredLunge, Mathf.Clamp01(intensity));
    }

    public void ClearEncounterPresence()
    {
        if (!effectsEnabled)
        {
            return;
        }

        presenceTouchedThisFrame = true;
        desiredPeek = 0f;
        desiredThreat = 0f;
        desiredLunge = 0f;
    }

    public void SetDollPressure(float intensity)
    {
        SetThreatPresence(intensity);
    }

    public void ClearDollPressure()
    {
        ClearEncounterPresence();
    }

    public void PlayScareStagger(float duration, float intensity = 1f)
    {
        if (!effectsEnabled)
        {
            return;
        }

        scareDuration = Mathf.Max(0.05f, duration);
        scareTimer = scareDuration;
        scareIntensity = Mathf.Clamp01(intensity);
        presenceTouchedThisFrame = true;
    }

    public void PlayFlashRepel()
    {
        if (!effectsEnabled)
        {
            return;
        }

        flashTimer = Mathf.Max(0.05f, flashResponseDuration);
        presenceTouchedThisFrame = true;
    }

    public void PlayDollGrab(float duration)
    {
        SetLungePresence(1f);
        PlayScareStagger(duration, 1f);
    }

    void LateUpdate()
    {
        if (!effectsEnabled)
        {
            if (!disabledCleanupApplied)
            {
                RestoreDefaults();
                disabledCleanupApplied = true;
            }
            return;
        }

        disabledCleanupApplied = false;

        if (scareTimer > 0f)
        {
            scareTimer = Mathf.Max(0f, scareTimer - Time.deltaTime);
            presenceTouchedThisFrame = true;
        }

        if (flashTimer > 0f)
        {
            flashTimer = Mathf.Max(0f, flashTimer - Time.deltaTime);
            presenceTouchedThisFrame = true;
        }

        if (!presenceTouchedThisFrame)
        {
            desiredPeek = 0f;
            desiredThreat = 0f;
            desiredLunge = 0f;
        }

        currentPeek = Mathf.MoveTowards(currentPeek, desiredPeek, Time.deltaTime * presenceSmoothing);
        currentThreat = Mathf.MoveTowards(currentThreat, desiredThreat, Time.deltaTime * presenceSmoothing);
        currentLunge = Mathf.MoveTowards(currentLunge, desiredLunge, Time.deltaTime * presenceSmoothing);

        ApplyEffects();

        presenceTouchedThisFrame = false;
        desiredPeek = Mathf.Clamp01(desiredPeek);
        desiredThreat = Mathf.Clamp01(desiredThreat);
        desiredLunge = Mathf.Clamp01(desiredLunge);
    }

    void ApplyEffects()
    {
        if (cameraTarget == null)
        {
            return;
        }

        cameraTarget.localRotation = cameraTarget.localRotation * Quaternion.Inverse(lastTargetOffset);
        transform.localRotation = transform.localRotation * Quaternion.Inverse(lastCameraRoll);

        float scareStrength = scareDuration > 0f ? Mathf.Clamp01(scareTimer / scareDuration) * scareIntensity : 0f;
        float flashStrength = flashResponseDuration > 0f ? Mathf.Clamp01(flashTimer / flashResponseDuration) : 0f;
        float motionTime = Time.time * (1.2f + currentThreat + currentLunge + scareStrength);

        float pitchOffset =
            Mathf.Sin(motionTime * 1.8f) * (peekPitch * currentPeek + threatPitch * currentThreat + lungePitch * currentLunge) +
            scarePitchOffset * scareStrength;
        float rollOffset =
            Mathf.Sin(motionTime * 2.7f) * (peekRoll * currentPeek + threatRoll * currentThreat + lungeRoll * currentLunge) +
            scareRollOffset * scareStrength +
            flashRollKick * flashStrength;

        lastTargetOffset = Quaternion.Euler(pitchOffset, 0f, 0f);
        lastCameraRoll = Quaternion.Euler(0f, 0f, rollOffset);

        cameraTarget.localRotation = cameraTarget.localRotation * lastTargetOffset;
        transform.localRotation = transform.localRotation * lastCameraRoll;

        if (cachedCamera != null)
        {
            float pulse = Mathf.Sin(motionTime * 2.1f) * 0.45f * (currentThreat + currentLunge + scareStrength);
            cachedCamera.fieldOfView =
                baseFieldOfView +
                peekFovOffset * currentPeek +
                threatFovOffset * currentThreat +
                lungeFovOffset * currentLunge +
                scareFovOffset * scareStrength +
                flashFovKick * flashStrength +
                pulse;
        }

        if (currentPeek <= 0.001f &&
            currentThreat <= 0.001f &&
            currentLunge <= 0.001f &&
            scareStrength <= 0.001f &&
            flashStrength <= 0.001f)
        {
            lastTargetOffset = Quaternion.identity;
            lastCameraRoll = Quaternion.identity;
            transform.localRotation = Quaternion.identity;
            if (cachedCamera != null)
            {
                cachedCamera.fieldOfView = baseFieldOfView;
            }
        }
    }

    void RestoreDefaults()
    {
        currentPeek = 0f;
        currentThreat = 0f;
        currentLunge = 0f;
        desiredPeek = 0f;
        desiredThreat = 0f;
        desiredLunge = 0f;
        scareTimer = 0f;
        flashTimer = 0f;
        scareIntensity = 0f;
        presenceTouchedThisFrame = false;
        disabledCleanupApplied = false;

        if (transform != null)
        {
            transform.localRotation = transform.localRotation * Quaternion.Inverse(lastCameraRoll);
            lastCameraRoll = Quaternion.identity;
            transform.localRotation = Quaternion.identity;
        }

        if (cameraTarget != null)
        {
            cameraTarget.localRotation = cameraTarget.localRotation * Quaternion.Inverse(lastTargetOffset);
            lastTargetOffset = Quaternion.identity;
        }

        if (cachedCamera != null)
        {
            cachedCamera.fieldOfView = baseFieldOfView;
        }
    }
}
