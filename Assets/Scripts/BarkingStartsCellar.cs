using UnityEngine;

public class BarkingStartsCellar : MonoBehaviour
{
    [Header("Persistence")]
    [Tooltip("Give this a unique ID, like 'CellarBarkingEvent'")]
    public string uniqueID;

    [Header("Audio Settings")]
    public GameObject newBarkingAudio; 

    [Header("Narrative Settings")]
    public string newObjectiveText; 

    private bool hasTriggered = false;

    void Start()
    {
        if (GameManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            bool wasTriggered = GameManager.Instance.GetObjectState(uniqueID, false);

            if (wasTriggered == true)
            {
                // REPLACED: Destroy(gameObject);
                // Instead, just turn off the collider so it can't be walked into.
                // This keeps the parent object alive so it doesn't break your hierarchy!
                GetComponent<Collider>().enabled = false;
                this.enabled = false; 
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && hasTriggered == false)
        {
            PlayerInteraction playerScript = other.transform.root.GetComponentInChildren<PlayerInteraction>();

            if (playerScript != null)
            {
                if (GameManager.Instance != null && GameManager.Instance.hasCollar == true)
                {
                    if (newBarkingAudio != null) newBarkingAudio.SetActive(true);
                    
                    if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(newObjectiveText))
                    {
                        ObjectiveManager.Instance.UpdateObjective(newObjectiveText);
                    }

                    hasTriggered = true;

                    if (!string.IsNullOrEmpty(uniqueID))
                    {
                        GameManager.Instance.SaveObjectState(uniqueID, true);
                    }

                    // REPLACED: Destroy(gameObject);
                    // Disarm the trigger so it cannot fire again while you are standing here.
                    GetComponent<Collider>().enabled = false;
                    this.enabled = false;
                }
            }
            else
            {
                Debug.LogError("Could not find the PlayerInteraction script on the player.");
            }
        }
    }
}