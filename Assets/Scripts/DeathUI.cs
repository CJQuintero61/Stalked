using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathUI : MonoBehaviour
{
    // called by the Restart button's OnClick in the Inspector
    public void Restart()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        string restartSceneName = DeathSceneActionUtility.ResolveRestartSceneName(activeSceneName, activeSceneName);

        DeathSceneActionUtility.PrepareForRestart();
        SceneManager.LoadScene(restartSceneName);
    }

    // called by the Quit button's OnClick in the Inspector
    public void Quit()
    {
        DeathSceneActionUtility.QuitGame();
    }
}
