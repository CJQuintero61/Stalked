using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // Creates a Singleton so we can access this from anywhere
    public static GameManager Instance { get; private set; }

   // Inside GameManager.cs
[Header("Player Inventory Booleans")]
public bool hasCollar = false;
public bool hasFlashlight = false;
public bool hasCellarKey = false;
public bool hasCliffard = false;

    // This dictionary tracks changes in the game scene (e.g., "Was this specific item picked up?" or "Is this door open?")
    private Dictionary<string, bool> sceneObjectStates = new Dictionary<string, bool>();

    private void Awake()
    {
        // Singleton pattern: ensure only one GameManager ever exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keeps this object alive when loading the cellar
    }

    // Call this when an object's state changes (like picking up an item)
    public void SaveObjectState(string objectID, bool state)
    {
        if (sceneObjectStates.ContainsKey(objectID))
        {
            sceneObjectStates[objectID] = state;
        }
        else
        {
            sceneObjectStates.Add(objectID, state);
        }
    }

    // Call this when the scene loads to check what the object should do
    public bool GetObjectState(string objectID, bool defaultState = false)
    {
        if (sceneObjectStates.ContainsKey(objectID))
        {
            return sceneObjectStates[objectID];
        }
        return defaultState;
    }
}
