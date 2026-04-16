using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // 1. Add this to check the current scene!

public class ObjectiveManager : MonoBehaviour
{
    // This is the "Singleton". It allows any script to talk to this one instantly!
    public static ObjectiveManager Instance; 

    [Header("UI Reference")]
    public TextMeshProUGUI objectiveText;

    void Awake()
    {
        // Set up the Singleton logic
        if (Instance == null) 
        { 
            Instance = this; 
        }
        else 
        { 
            Destroy(gameObject); 
        }
    }

    void Start()
    {
        // 2. Get the name of the currently active scene
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 3. Set the initial objective based on the scene name
        switch (currentSceneName)
        {
            case "Cellar": // Replace with your exact scene name
                UpdateObjective("Find a Light Source");
                break;
            case "Game": 
                UpdateObjective("Find Cliffard");
                break;
            default:
                // A fallback in case the scene name isn't listed above
                UpdateObjective("Find Cliffard");
                break;
        }
    }

    // Any script can call this function to change the text
    public void UpdateObjective(string newObjective)
    {
        if (objectiveText != null)
        {
            objectiveText.text = newObjective;
            Debug.Log("Objective Updated: " + newObjective);
        }
    }
}
