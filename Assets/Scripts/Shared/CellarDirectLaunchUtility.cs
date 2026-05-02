public static class CellarDirectLaunchUtility
{
    public static bool ShouldHoldDollUntilFlashlight(string sceneName, bool hasCellarKey, bool hasFlashlight)
    {
        bool isStandaloneCellarLaunch = sceneName == "Cellar" && !hasCellarKey;
        return isStandaloneCellarLaunch && !hasFlashlight;
    }
}
