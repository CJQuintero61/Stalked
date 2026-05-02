public readonly struct DeathSceneInfo
{
    public DeathSceneInfo(
        string retrySceneName,
        string panelTitle,
        string subtitle,
        string retryButtonLabel,
        string quitButtonLabel)
    {
        RetrySceneName = retrySceneName;
        PanelTitle = panelTitle;
        Subtitle = subtitle;
        RetryButtonLabel = retryButtonLabel;
        QuitButtonLabel = quitButtonLabel;
    }

    public string RetrySceneName { get; }
    public string PanelTitle { get; }
    public string Subtitle { get; }
    public string RetryButtonLabel { get; }
    public string QuitButtonLabel { get; }

    public static DeathSceneInfo BuildForScene(string sourceSceneName)
    {
        string normalizedSourceSceneName = string.IsNullOrWhiteSpace(sourceSceneName)
            ? "Cellar"
            : sourceSceneName;

        string retrySceneName = normalizedSourceSceneName == "Cellar"
            ? "Game"
            : normalizedSourceSceneName;

        return new DeathSceneInfo(
            retrySceneName,
            "YOU DIED!",
            string.Empty,
            "RESTART",
            "QUIT");
    }
}
