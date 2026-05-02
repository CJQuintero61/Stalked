using UnityEngine;

public class StopAudioTrigger : MonoBehaviour
{
    [Header("Audio Settings")]
    public GameObject audioObjectToStop; // Drag your looping audio object here!

    [Header("Narrative Settings (Optional)")]
    public string newObjectiveText; // Leave blank if you don't want the text to change

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        // Check if it's the player and we haven't triggered this yet
        if (other.CompareTag("Player") && hasTriggered == false)
        {
            // 1. Instantly turn off the object making the noise
            if (audioObjectToStop != null)
            {
                audioObjectToStop.SetActive(false);
            }

            // 2. Optionally update the objective text
            // (!string.IsNullOrEmpty checks to make sure you actually typed something in the box)
            if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(newObjectiveText))
            {
                ObjectiveManager.Instance.UpdateObjective(newObjectiveText);
            }

            // 3. Lock the trigger forever
            hasTriggered = true;
        }
    }
}
