using UnityEngine;

public class BarkingStartsCellar : MonoBehaviour
{
    [Header("Audio Settings")]
    public GameObject newBarkingAudio; 

    [Header("Narrative Settings (Optional)")]
    public string newObjectiveText; 

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && hasTriggered == false)
        {
            Debug.Log("1. Player touched the trigger!");

            // Using GetComponentInParent just to be safe!
            PlayerInteraction playerScript = other.transform.root.GetComponentInChildren<PlayerInteraction>();

            if (playerScript != null)
            {
                Debug.Log("2. Found the PlayerInteraction script! Does player have collar? " + playerScript.hasCollar);

                if (playerScript.hasCollar == true)
                {
                    Debug.Log("3. Success! Turning on the audio.");
                    
                    if (newBarkingAudio != null) newBarkingAudio.SetActive(true);
                    
                    if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(newObjectiveText))
                    {
                        ObjectiveManager.Instance.UpdateObjective(newObjectiveText);
                    }

                    hasTriggered = true;
                }
            }
            else
            {
                Debug.LogError("Uh oh! Could not find the PlayerInteraction script on the player.");
            }
        }
    }
}