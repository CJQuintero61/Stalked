using StarterAssets;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class PlayerDollFearEffects : MonoBehaviour
{
    [Header("References")]
    public Transform playerRoot;
    public Transform cameraTarget;
    public FirstPersonController firstPersonController;
    public Transform pressureSource;

    [Header("Pressure Effects")]
    public float pressureSmoothing = 5f;
    public float maxFovOffset = 5f;
    public float swayPitch = 1.4f;
    public float swayRoll = 2f;
    public float yawPullSpeed = 18f;
    [Range(0f, 1f)]
    public float lookDampening = 0.35f;

    [Header("Grab Effects")]
    public float grabFovOffset = 10f;
    public float grabPitchOffset = 8f;
    public float grabYawPullSpeed = 110f;

    private Camera cachedCamera;
    private float baseFieldOfView;
    private float currentPressure;
    private float desiredPressure;
    private float grabTimer;
    private float grabDuration;
    private bool pressureTouchedThisFrame;
    private bool controllerWasEnabled;
    private Quaternion lastTargetOffset = Quaternion.identity;
    private Quaternion lastCameraRoll = Quaternion.identity;

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

        if (firstPersonController == null && playerRoot != null)
        {
            firstPersonController = playerRoot.GetComponentInChildren<FirstPersonController>();
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

    public void SetDollPressure(float intensity)
    {
        pressureTouchedThisFrame = true;
        desiredPressure = Mathf.Max(desiredPressure, Mathf.Clamp01(intensity));
    }

    public void ClearDollPressure()
    {
        pressureTouchedThisFrame = true;
        desiredPressure = 0f;
    }

    public void PlayDollGrab(float duration)
    {
        grabDuration = Mathf.Max(0.05f, duration);
        grabTimer = grabDuration;
        pressureTouchedThisFrame = true;
        desiredPressure = 1f;

        if (firstPersonController != null)
        {
            controllerWasEnabled = firstPersonController.enabled;
            firstPersonController.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (grabTimer > 0f)
        {
            grabTimer -= Time.deltaTime;
            pressureTouchedThisFrame = true;
            desiredPressure = 1f;

            if (grabTimer <= 0f && firstPersonController != null)
            {
                firstPersonController.enabled = controllerWasEnabled;
            }
        }

        if (!pressureTouchedThisFrame)
        {
            desiredPressure = 0f;
        }

        currentPressure = Mathf.MoveTowards(currentPressure, desiredPressure, Time.deltaTime * pressureSmoothing);
        ApplyEffects();

        pressureTouchedThisFrame = false;
        desiredPressure = Mathf.Clamp01(desiredPressure);
    }

    private void ApplyEffects()
    {
        if (cameraTarget == null)
        {
            return;
        }

        cameraTarget.localRotation = cameraTarget.localRotation * Quaternion.Inverse(lastTargetOffset);
        transform.localRotation = transform.localRotation * Quaternion.Inverse(lastCameraRoll);

        float grabStrength = grabDuration > 0f ? Mathf.Clamp01(grabTimer / grabDuration) : 0f;
        float effectivePressure = Mathf.Max(currentPressure, grabStrength);
        float time = Time.time * (1.5f + effectivePressure);

        float pitchOffset = Mathf.Sin(time * 2.1f) * swayPitch * currentPressure + grabPitchOffset * grabStrength;
        float rollOffset = Mathf.Sin(time * 3.4f) * swayRoll * currentPressure;

        lastTargetOffset = Quaternion.Euler(pitchOffset, 0f, 0f);
        lastCameraRoll = Quaternion.Euler(0f, 0f, rollOffset);

        cameraTarget.localRotation = cameraTarget.localRotation * lastTargetOffset;
        transform.localRotation = transform.localRotation * lastCameraRoll;

        if (cachedCamera != null)
        {
            float pulse = Mathf.Sin(time * 2.6f) * 0.5f * currentPressure;
            cachedCamera.fieldOfView = baseFieldOfView + maxFovOffset * currentPressure + grabFovOffset * grabStrength + pulse;
        }

        if (playerRoot != null && pressureSource != null)
        {
            Vector3 toSource = pressureSource.position - playerRoot.position;
            toSource.y = 0f;
            if (toSource.sqrMagnitude > 0.001f)
            {
                Vector3 desiredForward = toSource.normalized;
                float yawSpeed = Mathf.Lerp(yawPullSpeed * lookDampening, grabYawPullSpeed, grabStrength);
                float maxRadiansDelta = Mathf.Deg2Rad * yawSpeed * Time.deltaTime * effectivePressure;
                Vector3 rotated = Vector3.RotateTowards(playerRoot.forward, desiredForward, maxRadiansDelta, 0f);
                playerRoot.rotation = Quaternion.LookRotation(rotated, Vector3.up);
            }
        }

        if (currentPressure <= 0.001f && grabStrength <= 0.001f)
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

    private void RestoreDefaults()
    {
        currentPressure = 0f;
        desiredPressure = 0f;
        grabTimer = 0f;
        pressureTouchedThisFrame = false;

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

        if (firstPersonController != null)
        {
            firstPersonController.enabled = controllerWasEnabled || firstPersonController.enabled;
        }
    }
}
