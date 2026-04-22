using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class DeathHandler : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Image fadeImage;
    public GameObject deathUI;
    public MonoBehaviour playerController;

    [Header("Timing")]
    public float cameraLerpDuration = 1.5f;
    public float holdOnAngelDuration = 0.5f;
    public float fadeDuration = 1.0f;

    private PlayerHealth playerHealth;
    private Transform killerTransform;
    private CinemachineBrain cinemachineBrain;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerHealth.OnDeath += HandleDeath;

        // Cache the brain on the camera
        cinemachineBrain = playerCamera.GetComponent<CinemachineBrain>();
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnDeath -= HandleDeath;
    }

    void HandleDeath(Transform killer)
    {
        // Disable player controller
        if (playerController != null)
            playerController.enabled = false;

        // Disable ALL virtual cameras so Cinemachine stops competing
        foreach (CinemachineCamera vc in FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None))
            vc.enabled = false;

        // Disable the brain so Cinemachine fully releases the camera transform
        if (cinemachineBrain != null)
            cinemachineBrain.enabled = false;

        // Unparent camera so the player hierarchy can't interfere either
        playerCamera.transform.SetParent(null);

        // get the killer's transform to pan the camera to the enemy that killed the player
        killerTransform = killer;

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (killerTransform != null)
        {
            Quaternion startRot = playerCamera.transform.rotation;
            Vector3 dir = (killerTransform.position - playerCamera.transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dir);

            float elapsed = 0f;
            while (elapsed < cameraLerpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / cameraLerpDuration);
                playerCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            yield return new WaitForSeconds(holdOnAngelDuration);
        }

        // Fade to black
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
        }

        if (deathUI != null)
            deathUI.SetActive(true);
    }
}