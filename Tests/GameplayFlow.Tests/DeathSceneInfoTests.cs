using System.Numerics;
using NUnit.Framework;

namespace GameplayFlow.Tests;

public class DeathSceneInfoTests
{
    [Test]
    public void BuildForCellarRestartsInMainGameScene()
    {
        DeathSceneInfo info = DeathSceneInfo.BuildForScene("Cellar");

        Assert.That(info.RetrySceneName, Is.EqualTo("Game"));
        Assert.That(info.PanelTitle, Is.EqualTo("YOU DIED!"));
        Assert.That(info.Subtitle, Is.EqualTo(string.Empty));
        Assert.That(info.RetryButtonLabel, Is.EqualTo("RESTART"));
        Assert.That(info.QuitButtonLabel, Is.EqualTo("QUIT"));
    }

    [Test]
    public void BuildForOtherSceneKeepsSameScarecrowDeathCopy()
    {
        DeathSceneInfo info = DeathSceneInfo.BuildForScene("Game");

        Assert.That(info.RetrySceneName, Is.EqualTo("Game"));
        Assert.That(info.PanelTitle, Is.EqualTo("YOU DIED!"));
        Assert.That(info.Subtitle, Is.EqualTo(string.Empty));
        Assert.That(info.RetryButtonLabel, Is.EqualTo("RESTART"));
        Assert.That(info.QuitButtonLabel, Is.EqualTo("QUIT"));
    }

    [Test]
    public void FlashlightInterruptRequiresLightRangeAndAim()
    {
        Vector3 beamOrigin = new(0f, 0f, 0f);
        Vector3 forward = Vector3.Normalize(new Vector3(0f, 0f, 1f));

        bool centeredHit = FlashlightInterruptUtility.ShouldInterrupt(
            flashlightIsOn: true,
            beamOrigin: beamOrigin,
            beamForward: forward,
            targetPosition: new Vector3(0f, 0f, 5f),
            dotThreshold: 0.82f,
            maxRange: 11f);

        bool offAxisMiss = FlashlightInterruptUtility.ShouldInterrupt(
            flashlightIsOn: true,
            beamOrigin: beamOrigin,
            beamForward: forward,
            targetPosition: new Vector3(5f, 0f, 5f),
            dotThreshold: 0.82f,
            maxRange: 11f);

        bool outOfRangeMiss = FlashlightInterruptUtility.ShouldInterrupt(
            flashlightIsOn: true,
            beamOrigin: beamOrigin,
            beamForward: forward,
            targetPosition: new Vector3(0f, 0f, 15f),
            dotThreshold: 0.82f,
            maxRange: 11f);

        Assert.That(centeredHit, Is.True);
        Assert.That(offAxisMiss, Is.False);
        Assert.That(outOfRangeMiss, Is.False);
    }

    [Test]
    public void ResolveRestartSceneNameKeepsCurrentGameplaySceneForInlineDeathUi()
    {
        string resolved = DeathSceneNavigationUtility.ResolveRestartSceneName("Game", "Cellar");

        Assert.That(resolved, Is.EqualTo("Game"));
    }

    [Test]
    public void ResolveRestartSceneNameUsesStoredSceneForDedicatedGameOver()
    {
        string resolved = DeathSceneNavigationUtility.ResolveRestartSceneName("GameOver", "Game");

        Assert.That(resolved, Is.EqualTo("Game"));
    }

    [Test]
    public void StandaloneCellarLaunchHoldsDollUntilFlashlightIsPickedUp()
    {
        bool shouldHold = CellarDirectLaunchUtility.ShouldHoldDollUntilFlashlight(
            sceneName: "Cellar",
            hasCellarKey: false,
            hasFlashlight: false);

        Assert.That(shouldHold, Is.True);
    }

    [Test]
    public void NormalCellarEntryDoesNotHoldDoll()
    {
        bool shouldHold = CellarDirectLaunchUtility.ShouldHoldDollUntilFlashlight(
            sceneName: "Cellar",
            hasCellarKey: true,
            hasFlashlight: false);

        Assert.That(shouldHold, Is.False);
    }
}
