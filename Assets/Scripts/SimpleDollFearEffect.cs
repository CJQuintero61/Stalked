using UnityEngine;

public class SimpleDollFearEffect : MonoBehaviour
{
    [Header("Fear Settings")]
    public float smallPulseDuration = 0.25f;
    public float strongPulseDuration = 0.45f;

    public float smallFovKick = 2f;
    public float strongFovKick = 5f;

    public float smallShakeAmount = 0.5f;
    public float strongShakeAmount = 1.4f;

    private Camera cam;
    private float baseFov;
    private Quaternion baseRotation;

    private float pulseTimer;
    private float pulseDuration;
    private float currentFovKick;
    private float currentShakeAmount;

    void Awake()
    {
        cam = GetComponent<Camera>();

        if (cam != null)
        {
            baseFov = cam.fieldOfView;
        }

        baseRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        if (pulseTimer <= 0f)
        {
            RestoreCamera();
            return;
        }

        pulseTimer -= Time.deltaTime;

        float strength = pulseTimer / pulseDuration;

        if (cam != null)
        {
            cam.fieldOfView = baseFov + currentFovKick * strength;
        }

        float pitch = Random.Range(-currentShakeAmount, currentShakeAmount) * strength;
        float yaw = Random.Range(-currentShakeAmount, currentShakeAmount) * strength;
        float roll = Random.Range(-currentShakeAmount, currentShakeAmount) * strength;

        transform.localRotation = baseRotation * Quaternion.Euler(pitch, yaw, roll);
    }

    public void PlaySmallFearPulse()
    {
        StartPulse(smallPulseDuration, smallFovKick, smallShakeAmount);
    }

    public void PlayFearPulse()
    {
        StartPulse(strongPulseDuration, strongFovKick, strongShakeAmount);
    }

    void StartPulse(float duration, float fovKick, float shakeAmount)
    {
        pulseDuration = Mathf.Max(0.05f, duration);
        pulseTimer = pulseDuration;
        currentFovKick = fovKick;
        currentShakeAmount = shakeAmount;
    }

    void RestoreCamera()
    {
        if (cam != null)
        {
            cam.fieldOfView = baseFov;
        }

        transform.localRotation = baseRotation;
    }
}