using System.Numerics;

public static class FlashlightInterruptUtility
{
    public static bool ShouldInterrupt(
        bool flashlightIsOn,
        Vector3 beamOrigin,
        Vector3 beamForward,
        Vector3 targetPosition,
        float dotThreshold,
        float maxRange)
    {
        if (!flashlightIsOn)
        {
            return false;
        }

        Vector3 toTarget = targetPosition - beamOrigin;
        float distance = toTarget.Length();
        if (distance <= 0.001f || distance > maxRange)
        {
            return false;
        }

        Vector3 normalizedForward = Vector3.Normalize(beamForward);
        Vector3 normalizedToTarget = Vector3.Normalize(toTarget);
        return Vector3.Dot(normalizedForward, normalizedToTarget) >= dotThreshold;
    }
}
