using UnityEngine;

public class BarkingStartsShed : MonoBehaviour
{
    [Header("Persistence")]
    [Tooltip("Give this a unique ID, like 'ShedBarkingEvent'")]
    public string uniqueID;

    [Header("Narrative Settings")]
    public string newObjectiveText; // e.g., "Investigate the barking dogs"
    
    [Header("Audio Settings")]
    public GameObject barkingLoopObject; 

    private bool hasTriggered = false;

    void Start()
    {
        // 1. Check if the player already triggered this event in a previous visit
        if (GameManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            bool wasTriggered = GameManager.Instance.GetObjectState(uniqueID, false);

            if (wasTriggered == true)
            {
                // 2. The event already happened! 
                // We turn off the BoxCollider and the script so it can never be triggered again,
                // but we DO NOT destroy the GameObject so your hierarchy stays safe.
                GetComponent<Collider>().enabled = false;
                this.enabled = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && hasTriggered == false)
        {
            // 3. Turn on the object that holds our DelayedAudioLoop script
            if (barkingLoopObject != null)
            {
                barkingLoopObject.SetActive(true);
            }

            // 4. Update the UI text
            if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(newObjectiveText))
            {
                ObjectiveManager.Instance.UpdateObjective(newObjectiveText);
            }

            hasTriggered = true;

            // 5. Save the state so the GameManager remembers for next time
            if (GameManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
            {
                GameManager.Instance.SaveObjectState(uniqueID, true);
            }

            // 6. Disarm the trigger so it physically cannot fire again while you are standing here
            GetComponent<Collider>().enabled = false;
            this.enabled = false;
        }
    }
}
