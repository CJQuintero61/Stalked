using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance; 

    [Header("UI Reference")]
    public TextMeshProUGUI objectiveText;

    void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(gameObject); // Keep this alive between scenes
        }
        else 
        { 
            Destroy(gameObject); 
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshObjective();
    }

    public void RefreshObjective()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log("Scene loaded: " + currentSceneName);

        if (currentSceneName == "Cellar")
        {
            UpdateObjective("Find a Light Source");
        }
        else if (currentSceneName == "Game") 
        {
            // Check if manager exists
            if (GameManager.Instance == null) {
                Debug.LogError("ObjectiveManager: GameManager.Instance is NULL!");
                return;
            }

            bool allItemsCollected = GameManager.Instance.hasCollar && 
                                     GameManager.Instance.hasFlashlight && 
                                     GameManager.Instance.hasCellarKey;
            
            Debug.Log("Checking items... Collar: " + GameManager.Instance.hasCollar + 
                      " | Flashlight: " + GameManager.Instance.hasFlashlight + 
                      " | Key: " + GameManager.Instance.hasCellarKey);

            if (allItemsCollected)
            {
                UpdateObjective("Escape");
            }
            else
            {
                UpdateObjective("Find Cliffard");
            }
        }
    }

    public void UpdateObjective(string newObjective)
    {
        if (objectiveText != null)
        {
            objectiveText.text = newObjective;
        }
    }
}
