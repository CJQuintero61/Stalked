using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathUI : MonoBehaviour
{
    // called by the Restart button's OnClick in the Inspector
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // called by the Quit button's OnClick in the Inspector
    public void Quit()
    {
        Application.Quit();

        // stops play mode in the editor since Application.Quit() won't work there
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}