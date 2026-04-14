using UnityEngine;

public class BarkingStartsShed : MonoBehaviour
{
    [Header("Narrative Settings")]
    public string newObjectiveText; // e.g., "Investigate the barking dogs"
    
    [Header("Audio Settings")]
    // NEW: We changed this from AudioSource to GameObject!
    public GameObject barkingLoopObject; 

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && hasTriggered == false)
        {
            // NEW: Turn on the object that holds our DelayedAudioLoop script!
            if (barkingLoopObject != null)
            {
                barkingLoopObject.SetActive(true);
            }

            // Update the UI text
            if (ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.UpdateObjective(newObjectiveText);
            }

            hasTriggered = true;
        }
    }
}
