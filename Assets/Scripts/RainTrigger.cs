using UnityEngine;
using UnityEngine.SceneManagement; // Required to check which scene you are in!

public class WeatherController : MonoBehaviour
{
    [Header("Weather Settings")]
    [Tooltip("Drag your Heavy Rain particle system or GameObject here.")]
    public GameObject heavyRainObject;

    void Start()
    {
        // When the scene first loads, check if we should be raining
        UpdateWeather();
    }

    void Update()
    {
        // Continuously check the GameManager and Scene state
        UpdateWeather();
    }

    private void UpdateWeather()
    {
        // Safety check to make sure the GameManager and Rain object exist
        if (GameManager.Instance != null && heavyRainObject != null)
        {
            // 1. Do we have Cliffard?
            bool hasCliffard = GameManager.Instance.hasCliffard;
            
            // 2. Are we in the maze scene? (Make sure "Game" perfectly matches your scene name's spelling/capitalization)
            bool isInGameScene = SceneManager.GetActiveScene().name == "Game";

            // 3. The rain should only be active if BOTH are true
            bool shouldBeRaining = hasCliffard && isInGameScene;

            // Only update the active state if it needs to change (saves performance)
            if (heavyRainObject.activeSelf != shouldBeRaining)
            {
                heavyRainObject.SetActive(shouldBeRaining);
                
                if (shouldBeRaining)
                {
                    Debug.Log("In the Game scene with Cliffard! Starting the heavy rain...");
                }
                else
                {
                    Debug.Log("Rain stopped. (Either left the Game scene or missing Cliffard).");
                }
            }
        }
    }
}
