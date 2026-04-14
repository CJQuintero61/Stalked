using UnityEngine;

public class StopAndPlayTrigger : MonoBehaviour
{
    [Header("Audio to Stop")]
    public GameObject audioObjectToStop; // The looping barking object

    [Header("Audio to Play")]
    public AudioSource oneShotAudio; // The new single-play sound

    [Header("Narrative Settings (Optional)")]
    public string newObjectiveText; 

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        // 1. Make sure it's the player and it hasn't triggered yet
        if (other.CompareTag("Player") && hasTriggered == false)
        {
            // 2. Grab the PlayerInteraction script (using the bulletproof root method!)
            PlayerInteraction playerScript = other.transform.root.GetComponentInChildren<PlayerInteraction>();

            // 3. Check if we found the script AND the player has the collar
            if (playerScript != null && playerScript.hasCollar == true)
            {
                // 4. Instantly turn OFF the looping barking audio
                if (audioObjectToStop != null)
                {
                    audioObjectToStop.SetActive(false);
                }

                // 5. Play the NEW single sound effect
                if (oneShotAudio != null)
                {
                    oneShotAudio.Play();
                }

                // 6. Update the objective text (if you typed one in)
                if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(newObjectiveText))
                {
                    ObjectiveManager.Instance.UpdateObjective(newObjectiveText);
                }

                // 7. Lock the trigger forever
                hasTriggered = true;
            }
        }
    }
}
