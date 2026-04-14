using UnityEngine;
using System.Collections; // Required for Coroutines

public class DelayedAudioLoop : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;

    [Header("Delay Settings (Seconds)")]
    public float minDelay = 2f;
    public float maxDelay = 5f;

    void Start()
    {
        // Start the continuous loop the moment the object is turned on
        StartCoroutine(PlaySoundWithDelay());
    }

    IEnumerator PlaySoundWithDelay()
    {
        // "while (true)" creates an infinite loop that runs as long as the object is active
        while (true) 
        {
            if (audioSource != null && audioSource.clip != null)
            {
                // 1. Play the sound
                audioSource.Play();
                
                // 2. Wait for the audio clip to finish playing completely
                yield return new WaitForSeconds(audioSource.clip.length);
            }

            // 3. Pick a random delay time between your minimum and maximum
            float waitTime = Random.Range(minDelay, maxDelay);

            // 4. Wait for that amount of seconds before looping back to the top!
            yield return new WaitForSeconds(waitTime);
        }
    }
}
