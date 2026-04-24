using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class CellarExit : MonoBehaviour
{
    [Header("Scene Loading")]
    public string nextSceneName = "Game"; 
    public string spawnPointName = "CellarDoorsSpawn";

    [Header("UI Settings")]
    public GameObject interactPanel; 
    public TextMeshProUGUI promptText;

    private bool playerInZone = false;
    private PlayerInteraction playerScript;

    void Update()
    {
        if (playerInZone == true && playerScript != null)
        {
            if (GameManager.Instance.hasCliffard == true)
            {
                UpdateUI(true, "Press E to Exit Cellar");

                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    LoadNextScene();
                }
            }
            else
            {
                UpdateUI(true, "You can't leave without Cliffard.");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Camera.main != null)
            {
                playerScript = Camera.main.GetComponent<PlayerInteraction>();
                
                if (playerScript != null)
                {
                    playerInZone = true;
                    // TELL THE RAYCAST TO BACK OFF AND LEAVE THE UI ALONE
                    playerScript.isInTriggerZone = true; 
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerScript != null)
            {
                // GIVE CONTROL OF THE UI BACK TO THE RAYCAST
                playerScript.isInTriggerZone = false; 
            }
            
            playerInZone = false;
            playerScript = null; 
            UpdateUI(false, ""); 
        }
    }

    // --- THESE ARE THE MISSING FUNCTIONS --- //

    private void UpdateUI(bool state, string message)
    {
        if (interactPanel != null) interactPanel.SetActive(state);
        if (promptText != null) promptText.text = message;
    }

    private void LoadNextScene()
    {
        UpdateUI(false, ""); 
        
        // Save the spawn point name into our immortal memory!
        SceneMemory.targetSpawnName = spawnPointName;
        
        SceneManager.LoadScene(nextSceneName);
    }
}