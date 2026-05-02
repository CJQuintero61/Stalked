using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class DeathSceneActionUtility
{
    public static string ResolveRestartSceneName(string activeSceneName, string storedRetrySceneName)
    {
        return DeathSceneNavigationUtility.ResolveRestartSceneName(activeSceneName, storedRetrySceneName);
    }

    public static void PrepareForRestart()
    {
        Time.timeScale = 1f;
        DeathSceneState.Clear();
    }

    public static void QuitGame()
    {
        Application.Quit();

        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #endif
    }
}
