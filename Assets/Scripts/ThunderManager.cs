using System.Collections;
using UnityEngine;

public class ThunderManager : MonoBehaviour
{
    [Header("Audio Setup")]
    public AudioSource audioSource;
    public AudioClip thunderClip;

    [Header("Lightning Setup")]
    public Light lightningLight; // Assign your Directional or Point light here
    public float flashDuration = 0.1f;

    [Header("Interval Settings (in Seconds)")]
    public float minInterval = 180f; // 3 minutes
    public float maxInterval = 600f; // 10 minutes

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Ensure light is off at the start
        if (lightningLight != null)
            lightningLight.enabled = false;

        // Start the infinite loop
        StartCoroutine(ThunderRoutine());
    }

    IEnumerator ThunderRoutine()
    {
        while (true)
        {
            // 1. Wait for a random duration
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            // 2. Trigger the visual flash
            if (lightningLight != null)
            {
                StartCoroutine(LightningFlash());
            }

            // 3. Small delay between light and sound (Realism!)
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));

            // 4. Play the sound
            PlayThunder();
        }
    }

    IEnumerator LightningFlash()
    {
        // First strike
        lightningLight.enabled = true;
        yield return new WaitForSeconds(flashDuration);
        lightningLight.enabled = false;

        // Small gap
        yield return new WaitForSeconds(0.05f);

        // Second strike (flicker)
        lightningLight.enabled = true;
        yield return new WaitForSeconds(flashDuration * 1.5f);
        lightningLight.enabled = false;
    }

    void PlayThunder()
    {
        if (audioSource != null && thunderClip != null)
        {
            // Randomize pitch slightly for horror variety
            audioSource.pitch = Random.Range(0.7f, 1.0f);
            audioSource.PlayOneShot(thunderClip);
            
            Debug.Log("Thunder struck!");
        }
    }
}
