public static class DeathSceneNavigationUtility
{
    public static string ResolveRestartSceneName(string activeSceneName, string storedRetrySceneName)
    {
        if (activeSceneName == "GameOver" && !string.IsNullOrWhiteSpace(storedRetrySceneName))
        {
            return storedRetrySceneName;
        }

        return activeSceneName;
    }
}
