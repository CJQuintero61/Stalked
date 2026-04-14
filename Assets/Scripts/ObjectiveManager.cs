using UnityEngine;
using TMPro;

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
        // Set your very first objective when the game starts!
        UpdateObjective("Find Cliffard");
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
