using UnityEngine;
using UnityEngine.SceneManagement;

public class CellarInteractable : MonoBehaviour
{
    [Header("Scene Settings")]
    public string sceneToLoad; 

    [Header("Lock Settings")]
    public bool isLocked = true;

    [Header("Door Models")]
    public GameObject closedDoor;
    public GameObject hiddenOpenDoors; 

    [Header("Audio")]
    public AudioSource openSound; // NEW: The audio source that plays the unlock/open sound

    void Start()
    {
        // Automatically ensure the correct doors are visible/hidden when the game starts
        if (closedDoor != null) closedDoor.SetActive(true);
        if (hiddenOpenDoors != null) hiddenOpenDoors.SetActive(false);
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