using UnityEngine;

public class StopAndPlayTrigger : MonoBehaviour
{
    [Header("Persistence")]
    [Tooltip("Give this a unique ID, like 'StopBarkingEvent'")]
    public string uniqueID;

    [Header("Audio to Stop")]
    public GameObject audioObjectToStop; 

    [Header("Audio to Play")]
    public AudioClip oneShotSound; 

    [Header("Narrative Settings (Optional)")]
    public string newObjectiveText; 

    private bool hasTriggered = false;

    void Start()
    {
        if (GameManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            bool wasTriggered = GameManager.Instance.GetObjectState(uniqueID, false);

            if (wasTriggered == true)
            {
                // Ensure the looping audio stays permanently off
                if (audioObjectToStop != null)
                {
                    audioObjectToStop.SetActive(false);
                }

                // YOUR IDEA: Cleanly turns off this entire checkpoint object
                gameObject.SetActive(false);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && hasTriggered == false)
        {
            PlayerInteraction playerScript = other.transform.root.GetComponentInChildren<PlayerInteraction>();

            if (playerScript != null && GameManager.Instance != null && GameManager.Instance.hasCollar == true)
            {
                if (audioObjectToStop != null)
                {
                    audioObjectToStop.SetActive(false);
                }

                if (oneShotSound != null)
                {
                    // Spawns independently, so it survives the trigger shutting down
                    AudioSource.PlayClipAtPoint(oneShotSound, transform.position);
                }

                if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(newObjectiveText))
                {
                    ObjectiveManager.Instance.UpdateObjective(newObjectiveText);
                }

                hasTriggered = true;

                if (!string.IsNullOrEmpty(uniqueID))
                {
                    GameManager.Instance.SaveObjectState(uniqueID, true);
                }

                // YOUR IDEA: Turns off the empty object right after triggering
                gameObject.SetActive(false);
            }
        }
    }
}
