using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class LeaveCornfield : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent object (Canvas or Panel) containing the CanvasGroup component.")]
    public CanvasGroup endingContainer;

    [Tooltip("The UI Image component where the ending graphic will be displayed.")]
    public Image textDisplay;

    [Header("Ending Graphics (Sprites)")]
    [Tooltip("PNG/Sprite for escaping WITH Cliffard.")]
    public Sprite goodEndingGraphic;
    [Tooltip("PNG/Sprite for escaping WITHOUT Cliffard.")]
    public Sprite badEndingGraphic;

    [Header("Scene Loading")]
    [Tooltip("The exact name of the Cutscene scene to load for the good ending.")]
    public string goodEndingSceneName = "EndingCutscene";
    
    [Tooltip("The Build Index of the Main Menu scene to load for the bad ending.")]
    public int mainMenuSceneIndex = 0;

    [Header("Settings")]
    public float fadeDuration = 3.0f;
    public float waitTimeBeforeRestart = 6.0f;

    private bool isEnding = false;

    void Start()
    {
        if (endingContainer != null)
        {
            endingContainer.alpha = 0;
            endingContainer.interactable = false;
            endingContainer.blocksRaycasts = false;
        }
    }

    // Accepting the bool from PlayerInteraction.cs to fix the overload error
    public void EscapeMaze(bool rescuedCliffard)
    {
        if (isEnding) return;
        isEnding = true;

        // 1. Freeze the game world immediately
        Time.timeScale = 0f;

        // 2. Set the correct graphic
        if (textDisplay != null)
        {
            textDisplay.sprite = rescuedCliffard ? goodEndingGraphic : badEndingGraphic;
            textDisplay.preserveAspect = true;
            textDisplay.color = Color.white; 
        }

        // Pass the boolean into the coroutine so it knows which scene to load later
        StartCoroutine(ExecuteEndingSequence(rescuedCliffard));
    }

    private IEnumerator ExecuteEndingSequence(bool isGoodEnding)
    {
        // 3. Fade in using Unscaled Time (since regular time is frozen)
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            if (endingContainer != null)
            {
                endingContainer.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            }
            yield return null;
        }

        if (endingContainer != null)
        {
            endingContainer.alpha = 1f;
            endingContainer.blocksRaycasts = true;
        }

        // 4. Use Realtime wait so the pause doesn't last forever
        yield return new WaitForSecondsRealtime(waitTimeBeforeRestart);

        // 5. CRITICAL: Reset Time.timeScale before changing scenes
        // If you don't do this, the next scene (Main Menu or Cutscene) will be frozen!
        Time.timeScale = 1f;

        // 6. Load the appropriate scene based on the ending achieved
        if (isGoodEnding)
        {
            Debug.Log("Good Ending! Loading Cutscene...");
            if (!string.IsNullOrEmpty(goodEndingSceneName))
            {
                SceneManager.LoadScene(goodEndingSceneName);
            }
            else
            {
                Debug.LogWarning("Cutscene Scene Name is missing! Defaulting to Main Menu.");
                SceneManager.LoadScene(mainMenuSceneIndex);
            }
        }
        else
        {
            Debug.Log("Bad Ending. Returning to menu...");
            SceneManager.LoadScene(mainMenuSceneIndex); 
        }
    }

    // Handles the trigger exit if the player walks into a volume
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            bool cliffardStatus = false;
            if (GameManager.Instance != null)
            {
                cliffardStatus = GameManager.Instance.hasCliffard;
            }
            EscapeMaze(cliffardStatus);
        }
    }
}