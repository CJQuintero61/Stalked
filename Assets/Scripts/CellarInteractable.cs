using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

public class CellarInteractable : MonoBehaviour
{
    [Header("Scene Settings")]
    public string sceneToLoad; // Type the EXACT name of your cellar scene here

    public void OpenCellar()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log("Loading Scene: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("No scene name assigned to the Cellar script!");
        }
    }
}