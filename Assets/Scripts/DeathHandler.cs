using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using System.Linq;

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
        // stop the player from moving after death
        if (playerController != null)
            playerController.enabled = false;

        // disable Cinemachine Brain to release control of the camera
        foreach (Behaviour b in playerCamera.GetComponents<Behaviour>())
        {
            if (b.GetType().Name.Contains("CinemachineBrain"))
            {
                b.enabled = false;
                break;
            }
        }

        // disable all virtual cameras so nothing fights for camera control
        foreach (Behaviour b in FindObjectsByType<Behaviour>(FindObjectsSortMode.None))
        {
            string typeName = b.GetType().Name;
            if (typeName.Contains("CinemachineVirtualCamera") || typeName.Contains("CinemachineCamera"))
                b.enabled = false;
        }

        // unparent the camera so the player hierarchy can't affect its rotation
        playerCamera.transform.SetParent(null);

        // get the killer's transform
        killerTransform = killer;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // pan the camera toward the killer if one exists
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

            // brief pause to let the player see what killed them
            yield return new WaitForSeconds(holdOnAngelDuration);
        }

        // fade the screen to black
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

        // stop all audio after the screen is fully faded
        foreach (AudioSource audio in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            audio.Stop();
        }

        // disable remaining player components now that the screen is hidden
        foreach (MonoBehaviour mb in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mb != this && mb != playerHealth)
                mb.enabled = false;
        }

        // disable all enemy scripts in the scene
        foreach (IDamageDealer enemy in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IDamageDealer>())
        {
            if (enemy is MonoBehaviour mb)
                mb.enabled = false;
        }

        // show the death UI
        if (deathUI != null)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            deathUI.SetActive(true);
        }
    }
}