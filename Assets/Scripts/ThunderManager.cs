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

    [Header("Normal Interval Settings")]
    public float minInterval = 180f; // 3 minutes
    public float maxInterval = 600f; // 10 minutes

    [Header("Storm Interval Settings (With Cliffard)")]
    public float stormMinInterval = 10f; 
    public float stormMaxInterval = 30f; 

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Ensure light is off at the start
        if (lightningLight != null)
            lightningLight.enabled = false;

        // Start the continuous checking loop
        StartCoroutine(ThunderRoutine());
    }

    IEnumerator ThunderRoutine()
    {
        float timer = 0f;
        float currentTargetWaitTime = GetNextInterval();
        bool wasCliffardFound = CheckForCliffard();

        while (true)
        {
            // Advance the timer by the time passed since the last frame
            timer += Time.deltaTime;

            // Constantly check if Cliffard was JUST picked up
            bool isCliffardFoundNow = CheckForCliffard();

            if (isCliffardFoundNow && !wasCliffardFound)
            {
                // The player just got Cliffard! 
                wasCliffardFound = true;
                
                // Reset the timer and instantly switch to the shorter storm intervals
                timer = 0f;
                currentTargetWaitTime = Random.Range(stormMinInterval, stormMaxInterval);
                
                Debug.Log("Thunderstorm intensifying!");
            }

            // If our timer reaches the target wait time, trigger the thunder
            if (timer >= currentTargetWaitTime)
            {
                // Trigger the visual flash
                if (lightningLight != null)
                {
                    StartCoroutine(LightningFlash());
                }

                // Small delay between light and sound (Realism!)
                yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));

                // Play the sound
                PlayThunder();

                // Reset the timer and pick a new wait time for the next strike
                timer = 0f;
                currentTargetWaitTime = GetNextInterval();
            }

            // Wait until the next frame before looping again
            yield return null; 
        }
    }

    private bool CheckForCliffard()
    {
        // Safely check the GameManager to see if Cliffard is secured
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.hasCliffard;
        }
        return false;
    }

    private float GetNextInterval()
    {
        // Decide which set of variables to use based on the current state
        if (CheckForCliffard())
        {
            return Random.Range(stormMinInterval, stormMaxInterval);
        }
        else
        {
            return Random.Range(minInterval, maxInterval);
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
