public static class DeathSceneState
{
    private static DeathSceneInfo? pendingInfo;

    public static void Register(string sourceSceneName)
    {
        pendingInfo = DeathSceneInfo.BuildForScene(sourceSceneName);
    }

    public static DeathSceneInfo GetOrDefault(string defaultSceneName)
    {
        return pendingInfo ?? DeathSceneInfo.BuildForScene(defaultSceneName);
    }

    public static void Clear()
    {
        pendingInfo = null;
    }
}
