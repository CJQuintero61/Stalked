using UnityEngine;
using UnityEngine.SceneManagement;

public class CellarInteractable : MonoBehaviour
{
    [Header("Persistence")]
    [Tooltip("Every cellar entrance needs a unique ID! (e.g., 'MainCellarHatch')")]
    public string uniqueID;

    [Header("Scene Settings")]
    public string sceneToLoad; 

    [Header("Lock Settings")]
    public bool isLocked = true;

    [Header("Door Models")]
    public GameObject closedDoor;
    public GameObject hiddenOpenDoors; 

    [Header("Audio")]
    public AudioSource openSound; 

    void Start()
    {
        // --- PERSISTENCE LOGIC ADDED HERE ---
        // 1. When the scene loads, check if we already unlocked this door
        if (GameManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            // We use 'true' in the dictionary to mean "has been unlocked"
            bool wasUnlocked = GameManager.Instance.GetObjectState(uniqueID, false);
            
            if (wasUnlocked == true)
            {
                isLocked = false;
            }
        }

        // 2. Snap the visual models to match the current lock state
        if (isLocked == true)
        {
            if (closedDoor != null) closedDoor.SetActive(true);
            if (hiddenOpenDoors != null) hiddenOpenDoors.SetActive(false);
        }
        else
        {
            if (closedDoor != null) closedDoor.SetActive(false);
            if (hiddenOpenDoors != null) hiddenOpenDoors.SetActive(true);
        }
    }

    public bool TryOpenCellar(bool playerHasKey)
    {
        if (isLocked == true)
        {
            if (playerHasKey == true)
            {
                // 1. Unlock the door permanently
                isLocked = false; 
                
                // 2. Hide the closed door instantly
                if (closedDoor != null) closedDoor.SetActive(false);
                
                // 3. Reveal the single game object containing your open doors
                if (hiddenOpenDoors != null) hiddenOpenDoors.SetActive(true);

                // 4. Play the open sound!
                if (openSound != null)
                {
                    openSound.Play();
                }
                
                ObjectiveManager.Instance.UpdateObjective("Investigate the Cellar");

                // --- PERSISTENCE LOGIC ADDED HERE ---
                // 5. Tell the GameManager to remember we unlocked this!
                if (GameManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
                {
                    GameManager.Instance.SaveObjectState(uniqueID, true); 
                }

                return true; // Success! It unlocked and swapped the doors.
            }
            else
            {
                return false; // Failed! It is still locked.
            }
        }
        else
        {
            // If it is ALREADY unlocked and the doors are swapped, clicking it again loads the scene!
            LoadCellarScene();
            return true; 
        }
    }

    private void LoadCellarScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("No scene name assigned to the Cellar script!");
        }
    }
}