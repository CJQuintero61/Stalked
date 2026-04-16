using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Required for the New Input System

public class MazeGameManager : MonoBehaviour
{
    [Header("Transition Settings")]
    public string nextSceneName = "MainWorld";
    public Vector3 spawnPositionInNextScene = new Vector3(10, 0, 10);
    
    [Header("UI")]
    [Tooltip("A UI GameObject (like a Text element) that says 'Press E to Exit'")]
    public GameObject exitPromptUI;

    private bool hasReachedEnd = false;
    private bool isAtEntrance = false;

    // Static variables persist across scene loads to help the teleporter find its way
    public static Vector3 GlobalSpawnPoint;
    public static bool ShouldTeleport = false;

    void Start()
    {
        if (exitPromptUI != null) exitPromptUI.SetActive(false);
    }

    void Update()
    {
        // Check for 'E' press using the New Input System
        if (hasReachedEnd && isAtEntrance)
        {
            // Verify a keyboard exists and the 'E' key was pressed this frame
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                ExitMaze();
            }
        }
    }

    public void OnPlayerEnterTrigger(bool isStart)
    {
        if (isStart)
        {
            isAtEntrance = true;
            // Show prompt if objective is complete
            if (hasReachedEnd && exitPromptUI != null)
            {
                exitPromptUI.SetActive(true);
            }
        }
        else
        {
            // Player reached the end
            if (!hasReachedEnd)
            {
                hasReachedEnd = true;
                Debug.Log("Objective Updated: Return to entrance to exit!");
            }
        }
    }

    public void OnPlayerExitTrigger(bool isStart)
    {
        if (isStart)
        {
            isAtEntrance = false;
            if (exitPromptUI != null) exitPromptUI.SetActive(false);
        }
    }

    void ExitMaze()
    {
        // Set global variables for the next scene to read
        GlobalSpawnPoint = spawnPositionInNextScene;
        ShouldTeleport = true;

        SceneManager.LoadScene(nextSceneName);
    }
}