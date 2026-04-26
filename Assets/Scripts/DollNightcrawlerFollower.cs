using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class DollNightcrawlerFollower : MonoBehaviour
{
    private enum DollState
    {
        RoamHidden,
        Peek,
        Threaten,
        LungeWindow,
        Retreat,
        Recover
    }

    [Header("References")]
    public Transform playerTarget;
    public Camera playerCamera;
    public PlayerDollFearEffects fearEffects;
    public PlayerDollEncounterStatus encounterStatus;
    public FlashlightController flashlightController;
    public Transform eyePoint;
    public AudioSource audioSource;
    public string playerTag = "Player";
    public bool enableFearEffectsOnEncounter = true;

    [Header("Movement")]
    public float stoppingDistance = 1.2f;
    public float roamRefreshRate = 1.2f;
    public float roamMinRadius = 3f;
    public float roamMaxRadius = 9f;
    public float retreatMinRadius = 5f;
    public float retreatMaxRadius = 10f;
    public float minimumRetreatDistanceFromPlayer = 4f;
    public float initialPlacementSampleRadius = 3f;
    public float destinationSampleRadius = 2.5f;
    public int relocationSamples = 24;
    public float lungeSpeedMultiplier = 1.35f;

    [Header("Perception")]
    [Range(-1f, 1f)]
    public float directViewDot = 0.82f;
    [Range(-1f, 1f)]
    public float blindSpotDot = 0.2f;
    public float maxSightDistance = 24f;
    public float threatenDistance = 5f;
    public float closeEncounterRange = 1.75f;

    [Header("State Timings")]
    public float peekDuration = 1.2f;
    public float threatenBuildDuration = 1.6f;
    public float lungeTellDuration = 1f;
    public float retreatDuration = 1.5f;
    public float recoverDuration = 2.4f;
    public float seenGraceDuration = 1.15f;
    public float repeatAttackCooldown = 3.25f;
    public float pressureAnchorRefreshRate = 0.25f;

    [Header("Punish Loop")]
    public int followUpDamage = 34;
    public float vulnerabilityWindow = 7f;
    public float firstHitScareDuration = 0.75f;
    public float secondHitScareDuration = 1.15f;
    public float lungeSnapDistance = 1.4f;
    public float peekScareDuration = 0.3f;
    [Range(0f, 1f)] public float peekScareIntensity = 0.35f;
    public float lungeScareDuration = 0.85f;
    [Range(0f, 1f)] public float lungeScareIntensity = 1f;

    [Header("Flashlight Counterplay")]
    [Range(-1f, 1f)]
    public float flashlightDotThreshold = 0.82f;
    public float flashlightInterruptRange = 11f;
    public float flashlightInterruptCooldown = 0.4f;

    [Header("Ambush Scoring")]
    public float maxThreatPathLength = 7f;
    public float maxLungePathLength = 4f;
    public float maxRelocationPathLength = 13f;
    public float hiddenSpotBonus = 2.5f;
    public float cornerBonus = 1f;
    public float junctionBonus = 1.15f;
    public float deadEndBonus = 0.95f;
    public float directVisibilityPenalty = 3f;
    public float repeatSpotPenalty = 2f;
    public float pathLengthPenalty = 0.18f;
    public float featureProbeDistance = 1.75f;

    [Header("Audio Cues")]
    public AudioClip peekCue;
    public AudioClip threatenCue;
    public AudioClip lungeCue;
    public AudioClip flashlightRepelCue;
    public AudioClip scareCue;
    public float cueVolume = 0.9f;

    private Animator animator;
    private NavMeshAgent agent;
    private DollState currentState;
    private float stateTimer;
    private float destinationTimer;
    private float nextAllowedAttackTime;
    private float nextFlashlightInterruptTime;
    private float justSeenTimer;
    private float baseAgentSpeed;
    private bool hasStartedBehavior;
    private Vector3 lastAmbushPoint;
    private bool hasLastAmbushPoint;

    void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Start()
    {
        if (agent != null)
        {
            baseAgentSpeed = agent.speed;
            agent.stoppingDistance = stoppingDistance;
        }

        ResolveReferences();
        hasStartedBehavior = true;
        EnterRoamHidden(initialSpawn: true);
    }

    void OnDisable()
    {
        fearEffects?.ClearEncounterPresence();
        if (agent != null)
        {
            agent.speed = baseAgentSpeed > 0f ? baseAgentSpeed : agent.speed;
        }
    }

    void Update()
    {
        if (agent == null || animator == null)
        {
            return;
        }

        ResolveReferences();
        if (!HasValidContext() || !TryEnsureAgentOnNavMesh())
        {
            HaltDoll();
            return;
        }

        if (!hasStartedBehavior)
        {
            hasStartedBehavior = true;
            EnterRoamHidden(initialSpawn: true);
        }

        stateTimer -= Time.deltaTime;
        destinationTimer -= Time.deltaTime;
        justSeenTimer = Mathf.Max(0f, justSeenTimer - Time.deltaTime);

        bool directlySeen = CanPlayerDirectlySeeDoll();
        bool inBlindSpot = IsInPlayerBlindSpot();
        bool flashlightInterrupt = CanFlashlightInterrupt();
        float distanceToPlayer = GetFlatDistance(transform.position, playerTarget.position);

        if (flashlightInterrupt)
        {
            HandleFlashlightInterrupt();
            UpdateAnimation();
            return;
        }

        if (directlySeen && currentState != DollState.Recover)
        {
            justSeenTimer = seenGraceDuration;
        }

        switch (currentState)
        {
            case DollState.RoamHidden:
                UpdateRoamHidden(directlySeen, inBlindSpot, distanceToPlayer);
                break;
            case DollState.Peek:
                UpdatePeek(directlySeen, inBlindSpot, distanceToPlayer);
                break;
            case DollState.Threaten:
                UpdateThreaten(directlySeen, inBlindSpot, distanceToPlayer);
                break;
            case DollState.LungeWindow:
                UpdateLungeWindow(directlySeen, distanceToPlayer);
                break;
            case DollState.Retreat:
                UpdateRetreat(directlySeen, distanceToPlayer);
                break;
            case DollState.Recover:
                UpdateRecover();
                break;
        }

        UpdateAnimation();
    }

    void UpdateRoamHidden(bool directlySeen, bool inBlindSpot, float distanceToPlayer)
    {
        fearEffects?.ClearEncounterPresence();
        agent.isStopped = false;
        agent.speed = baseAgentSpeed;

        if (directlySeen)
        {
            EnterRetreat(wasInterrupted: true);
            return;
        }

        if (CanEnterThreatState(distanceToPlayer, inBlindSpot))
        {
            EnterThreaten();
            return;
        }

        bool needsNewDestination =
            destinationTimer <= 0f ||
            !agent.hasPath ||
            ReachedDestination();

        if (needsNewDestination)
        {
            TrySetAmbushDestination(roamMinRadius, roamMaxRadius);
            destinationTimer = roamRefreshRate;
        }

        if (ReachedDestination())
        {
            EnterPeek();
        }
    }

    void UpdatePeek(bool directlySeen, bool inBlindSpot, float distanceToPlayer)
    {
        fearEffects?.SetPeekPresence(1f);
        agent.isStopped = true;
        agent.speed = baseAgentSpeed;

        if (directlySeen)
        {
            EnterRetreat(wasInterrupted: true);
            return;
        }

        if (CanEnterThreatState(distanceToPlayer, inBlindSpot))
        {
            EnterThreaten();
            return;
        }

        if (stateTimer <= 0f)
        {
            EnterRoamHidden(initialSpawn: false);
        }
    }

    void UpdateThreaten(bool directlySeen, bool inBlindSpot, float distanceToPlayer)
    {
        if (directlySeen)
        {
            EnterRetreat(wasInterrupted: true);
            return;
        }

        if (!CanMaintainThreatState(distanceToPlayer, inBlindSpot))
        {
            EnterRetreat(wasInterrupted: false);
            return;
        }

        agent.isStopped = false;
        agent.speed = baseAgentSpeed;
        fearEffects?.SetThreatPresence(1f - Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, threatenBuildDuration)));

        if (destinationTimer <= 0f)
        {
            destinationTimer = pressureAnchorRefreshRate;
            TrySetThreatAnchor();
        }

        if (stateTimer <= 0f)
        {
            EnterLungeWindow();
        }
    }

    void UpdateLungeWindow(bool directlySeen, float distanceToPlayer)
    {
        fearEffects?.SetLungePresence(1f);
        agent.isStopped = false;
        agent.speed = baseAgentSpeed * lungeSpeedMultiplier;

        if (directlySeen)
        {
            EnterRetreat(wasInterrupted: true);
            return;
        }

        if (!CanMaintainLungeState())
        {
            EnterRetreat(wasInterrupted: false);
            return;
        }

        if (destinationTimer <= 0f)
        {
            destinationTimer = pressureAnchorRefreshRate;
            TrySetLungeDestination();
        }

        if (distanceToPlayer <= closeEncounterRange && CanResolvePunishAtCurrentPosition())
        {
            ResolvePunishEncounter();
            return;
        }

        if (stateTimer <= 0f)
        {
            EnterRetreat(wasInterrupted: false);
        }
    }

    void UpdateRetreat(bool directlySeen, float distanceToPlayer)
    {
        fearEffects?.ClearEncounterPresence();
        agent.isStopped = false;
        agent.speed = baseAgentSpeed;

        if ((stateTimer <= 0f && (!agent.hasPath || ReachedDestination())) ||
            distanceToPlayer >= minimumRetreatDistanceFromPlayer + 1f)
        {
            EnterRecover();
            return;
        }

        if (directlySeen && destinationTimer <= 0f)
        {
            destinationTimer = roamRefreshRate;
            TrySetAmbushDestination(retreatMinRadius, retreatMaxRadius);
        }
    }

    void UpdateRecover()
    {
        fearEffects?.ClearEncounterPresence();
        agent.isStopped = true;
        agent.speed = baseAgentSpeed;

        if (stateTimer <= 0f)
        {
            EnterRoamHidden(initialSpawn: false);
        }
    }

    void EnterRoamHidden(bool initialSpawn)
    {
        currentState = DollState.RoamHidden;
        stateTimer = roamRefreshRate;
        destinationTimer = 0f;
        agent.isStopped = false;
        agent.speed = baseAgentSpeed;
        fearEffects?.ClearEncounterPresence();

        if (initialSpawn)
        {
            nextAllowedAttackTime = Time.time + repeatAttackCooldown;
            justSeenTimer = seenGraceDuration;
        }

        TrySetAmbushDestination(roamMinRadius, roamMaxRadius);
    }

    void EnterPeek()
    {
        currentState = DollState.Peek;
        stateTimer = peekDuration;
        destinationTimer = 0f;
        agent.isStopped = true;
        agent.ResetPath();
        agent.speed = baseAgentSpeed;
        fearEffects?.PlayScareStagger(peekScareDuration, peekScareIntensity);
        PlayCue(peekCue);
    }

    void EnterThreaten()
    {
        currentState = DollState.Threaten;
        stateTimer = threatenBuildDuration;
        destinationTimer = 0f;
        agent.isStopped = false;
        agent.speed = baseAgentSpeed;
        PlayCue(threatenCue);
        TrySetThreatAnchor();
    }

    void EnterLungeWindow()
    {
        currentState = DollState.LungeWindow;
        stateTimer = lungeTellDuration;
        destinationTimer = 0f;
        agent.isStopped = false;
        agent.speed = baseAgentSpeed * lungeSpeedMultiplier;
        fearEffects?.PlayScareStagger(lungeScareDuration, lungeScareIntensity);
        PlayCue(lungeCue);
        TrySetLungeDestination();
    }

    void EnterRetreat(bool wasInterrupted)
    {
        currentState = DollState.Retreat;
        stateTimer = retreatDuration;
        destinationTimer = roamRefreshRate;
        agent.isStopped = false;
        agent.speed = baseAgentSpeed;
        fearEffects?.ClearEncounterPresence();

        if (wasInterrupted)
        {
            nextAllowedAttackTime = Mathf.Max(nextAllowedAttackTime, Time.time + repeatAttackCooldown);
            justSeenTimer = Mathf.Max(justSeenTimer, seenGraceDuration);
        }

        TrySetAmbushDestination(retreatMinRadius, retreatMaxRadius);
    }

    void EnterRecover()
    {
        currentState = DollState.Recover;
        stateTimer = recoverDuration;
        destinationTimer = 0f;
        agent.isStopped = true;
        agent.ResetPath();
        agent.speed = baseAgentSpeed;
        fearEffects?.ClearEncounterPresence();
    }

    void HandleFlashlightInterrupt()
    {
        nextFlashlightInterruptTime = Time.time + flashlightInterruptCooldown;
        nextAllowedAttackTime = Mathf.Max(nextAllowedAttackTime, Time.time + repeatAttackCooldown);
        justSeenTimer = Mathf.Max(justSeenTimer, seenGraceDuration);
        fearEffects?.PlayFlashRepel();
        PlayCue(flashlightRepelCue);
        EnterRetreat(wasInterrupted: true);
    }

    void ResolvePunishEncounter()
    {
        if (encounterStatus == null)
        {
            EnterRetreat(wasInterrupted: false);
            return;
        }

        bool dealtDamage;
        encounterStatus.ResolveDollPunish(followUpDamage, vulnerabilityWindow, out dealtDamage);

        if (dealtDamage)
        {
            fearEffects?.PlayScareStagger(secondHitScareDuration, 1f);
        }
        else
        {
            fearEffects?.PlayScareStagger(firstHitScareDuration, 0.8f);
        }

        PlayCue(scareCue);
        nextAllowedAttackTime = Time.time + repeatAttackCooldown;
        EnterRetreat(wasInterrupted: false);
    }

    bool CanEnterThreatState(float distanceToPlayer, bool inBlindSpot)
    {
        if (Time.time < nextAllowedAttackTime || justSeenTimer > 0f)
            return false;

        if (distanceToPlayer > threatenDistance || !inBlindSpot)
            return false;

        return TryCalculatePathMetrics(transform.position, playerTarget.position, out float pathLength, out _, out _) &&
               pathLength <= maxThreatPathLength;
    }

    bool CanMaintainThreatState(float distanceToPlayer, bool inBlindSpot)
    {
        if (distanceToPlayer > threatenDistance + 0.75f || !inBlindSpot)
            return false;

        return TryCalculatePathMetrics(transform.position, playerTarget.position, out float pathLength, out _, out _) &&
               pathLength <= maxThreatPathLength;
    }

    bool CanMaintainLungeState()
    {
        return TryCalculatePathMetrics(transform.position, playerTarget.position, out float pathLength, out _, out _) &&
               pathLength <= maxLungePathLength;
    }

    bool CanResolvePunishAtCurrentPosition()
    {
        return TryCalculatePathMetrics(transform.position, playerTarget.position, out float pathLength, out _, out _) &&
               pathLength <= maxLungePathLength;
    }

    bool TrySetThreatAnchor()
    {
        if (TryGetBehindPlayerPosition(lungeSnapDistance, maxThreatPathLength, out Vector3 anchor))
        {
            agent.SetDestination(anchor);
            return true;
        }

        return TrySetAmbushDestination(roamMinRadius, roamMaxRadius);
    }

    bool TrySetLungeDestination()
    {
        if (TryGetBehindPlayerPosition(lungeSnapDistance, maxLungePathLength, out Vector3 lungePosition))
        {
            agent.SetDestination(lungePosition);
            return true;
        }

        if (TryCalculatePathMetrics(transform.position, playerTarget.position, out float pathLength, out _, out _)
            && pathLength <= maxLungePathLength)
        {
            agent.SetDestination(playerTarget.position);
            return true;
        }

        return false;
    }

    bool TrySetAmbushDestination(float minRadius, float maxRadius)
    {
        if (!TryFindAmbushPoint(minRadius, maxRadius, out Vector3 destination))
            return false;

        if (GetFlatDistance(destination, playerTarget.position) < minimumRetreatDistanceFromPlayer &&
            minRadius >= retreatMinRadius)
        {
            return false;
        }

        agent.SetDestination(destination);
        lastAmbushPoint = destination;
        hasLastAmbushPoint = true;
        return true;
    }

    bool TryFindAmbushPoint(float minRadius, float maxRadius, out Vector3 bestPoint)
    {
        bestPoint = transform.position;
        float bestScore = float.NegativeInfinity;

        Vector3 playerPosition = playerTarget.position;
        Vector3 flatForward = GetFlatForward(playerCamera.transform.forward);
        if (flatForward.sqrMagnitude < 0.001f)
        {
            flatForward = GetFlatForward(playerTarget.forward);
        }
        if (flatForward.sqrMagnitude < 0.001f)
        {
            flatForward = Vector3.forward;
        }

        Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward).normalized;
        float[] anchorAngles = { 180f, 150f, 210f, 135f, 225f, 100f, 260f };
        int sampleCount = Mathf.Max(relocationSamples, anchorAngles.Length * 4);

        for (int i = 0; i < sampleCount; i++)
        {
            float anchorAngle = anchorAngles[i % anchorAngles.Length];
            float angle = anchorAngle + Random.Range(-22f, 22f);
            float radius = Random.Range(minRadius, maxRadius);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * flatForward;
            Vector3 candidate = playerPosition + direction * radius;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, destinationSampleRadius, agent.areaMask))
                continue;

            if (!TryCalculatePathMetrics(hit.position, playerPosition, out float pathLength, out int cornerCount, out NavMeshPath path))
                continue;

            if (pathLength > maxRelocationPathLength)
                continue;

            Vector3 fromPlayer = hit.position - playerPosition;
            fromPlayer.y = 0f;
            if (fromPlayer.sqrMagnitude <= 0.001f)
                continue;

            Vector3 normalized = fromPlayer.normalized;
            float frontDot = Vector3.Dot(flatForward, normalized);
            float sideAmount = Mathf.Abs(Vector3.Dot(flatRight, normalized));
            bool directlyVisible = IsWorldPointDirectlyVisible(hit.position + Vector3.up * 0.9f);
            float featureScore = EvaluateMazeFeatureScore(hit.position);
            float repeatPenalty = hasLastAmbushPoint
                ? Mathf.Clamp01(1f - (GetFlatDistance(hit.position, lastAmbushPoint) / Mathf.Max(0.1f, minRadius))) * repeatSpotPenalty
                : 0f;

            float score = 0f;
            score += -frontDot * 2.2f;
            score += sideAmount * 0.65f;
            score += directlyVisible ? -directVisibilityPenalty : hiddenSpotBonus;
            score += Mathf.Clamp(cornerCount, 1, 3) * cornerBonus;
            score += featureScore;
            score -= pathLength * pathLengthPenalty;
            score -= repeatPenalty;
            score += Mathf.Max(0f, path.corners.Length - 2) * 0.2f;

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = hit.position;
            }
        }

        return bestScore > float.NegativeInfinity;
    }

    float EvaluateMazeFeatureScore(Vector3 candidate)
    {
        int openDirections = 0;
        bool hasHorizontal = false;
        bool hasVertical = false;
        Vector3[] directions =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 probe = candidate + directions[i] * featureProbeDistance;
            if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, destinationSampleRadius, agent.areaMask))
                continue;

            if (!TryCalculatePathMetrics(candidate, hit.position, out float pathLength, out _, out _))
                continue;

            if (pathLength > featureProbeDistance * 1.75f)
                continue;

            openDirections++;
            if (i < 2) hasVertical = true;
            else hasHorizontal = true;
        }

        float score = 0f;
        if (openDirections <= 1)
            score += deadEndBonus;
        else if (openDirections >= 3)
            score += junctionBonus;
        else if (hasHorizontal && hasVertical)
            score += cornerBonus * 0.75f;

        return score;
    }

    bool TryGetBehindPlayerPosition(float radius, float maxPathLength, out Vector3 result)
    {
        result = transform.position;
        Vector3 flatForward = GetFlatForward(playerCamera.transform.forward);
        if (flatForward.sqrMagnitude < 0.001f)
        {
            flatForward = Vector3.forward;
        }

        Vector3[] directions =
        {
            -flatForward,
            Quaternion.Euler(0f, 40f, 0f) * -flatForward,
            Quaternion.Euler(0f, -40f, 0f) * -flatForward
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 candidate = playerTarget.position + directions[i].normalized * radius;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, destinationSampleRadius, agent.areaMask))
                continue;

            if (!TryCalculatePathMetrics(hit.position, playerTarget.position, out float pathLength, out _, out _))
                continue;

            if (pathLength > maxPathLength)
                continue;

            result = hit.position;
            return true;
        }

        return false;
    }

    bool CanPlayerDirectlySeeDoll()
    {
        Vector3 cameraOrigin = playerCamera.transform.position;
        Vector3 targetPoint = GetFocusPoint();
        Vector3 toDoll = targetPoint - cameraOrigin;
        float distance = toDoll.magnitude;
        if (distance > maxSightDistance || distance <= 0.001f)
        {
            return false;
        }

        Vector3 direction = toDoll / distance;
        float dot = Vector3.Dot(playerCamera.transform.forward, direction);
        if (dot < directViewDot)
        {
            return false;
        }

        if (Physics.Raycast(cameraOrigin, direction, out RaycastHit hit, distance + 0.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == transform || hit.transform.IsChildOf(transform);
        }

        return false;
    }

    bool IsInPlayerBlindSpot()
    {
        Vector3 cameraOrigin = playerCamera.transform.position;
        Vector3 targetPoint = GetFocusPoint();
        Vector3 toDoll = targetPoint - cameraOrigin;
        float distance = toDoll.magnitude;
        if (distance > maxSightDistance || distance <= 0.001f)
        {
            return false;
        }

        Vector3 direction = toDoll / distance;
        return Vector3.Dot(playerCamera.transform.forward, direction) <= blindSpotDot;
    }

    bool CanFlashlightInterrupt()
    {
        if ((currentState != DollState.Threaten && currentState != DollState.LungeWindow) ||
            flashlightController == null ||
            !flashlightController.IsFlashlightOn ||
            Time.time < nextFlashlightInterruptTime)
        {
            return false;
        }

        Transform beamOrigin = flashlightController.BeamOrigin;
        if (beamOrigin == null)
            return false;

        Vector3 toDoll = GetFocusPoint() - beamOrigin.position;
        float distance = toDoll.magnitude;
        if (distance > flashlightInterruptRange || distance <= 0.001f)
            return false;

        Vector3 direction = toDoll / distance;
        if (Vector3.Dot(beamOrigin.forward, direction) < flashlightDotThreshold)
            return false;

        if (Physics.Raycast(beamOrigin.position, direction, out RaycastHit hit, distance + 0.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == transform || hit.transform.IsChildOf(transform);
        }

        return false;
    }

    Vector3 GetFocusPoint()
    {
        if (eyePoint != null)
            return eyePoint.position;

        return transform.position + Vector3.up * 0.9f;
    }

    bool ReachedDestination()
    {
        return !agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.15f;
    }

    void ResolveReferences()
    {
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (flashlightController == null && playerTarget != null)
        {
            flashlightController = playerTarget.GetComponent<FlashlightController>() ??
                                   playerTarget.GetComponentInChildren<FlashlightController>(true);
        }

        PlayerHealth playerHealth = null;
        if (playerTarget != null)
        {
            playerHealth = playerTarget.GetComponent<PlayerHealth>() ??
                           playerTarget.GetComponentInChildren<PlayerHealth>(true);

            if (playerHealth == null)
            {
                playerHealth = playerTarget.gameObject.AddComponent<PlayerHealth>();
            }
        }

        if (encounterStatus == null && playerTarget != null)
        {
            encounterStatus = playerTarget.GetComponent<PlayerDollEncounterStatus>();
            if (encounterStatus == null)
            {
                encounterStatus = playerTarget.gameObject.AddComponent<PlayerDollEncounterStatus>();
            }
        }

        if (encounterStatus != null && encounterStatus.playerHealth == null)
        {
            encounterStatus.playerHealth = playerHealth;
        }

        if (fearEffects == null && playerCamera != null)
        {
            fearEffects = playerCamera.GetComponent<PlayerDollFearEffects>();
            if (fearEffects == null)
            {
                fearEffects = playerCamera.gameObject.AddComponent<PlayerDollFearEffects>();
            }
        }

        if (fearEffects != null)
        {
            if (enableFearEffectsOnEncounter)
            {
                fearEffects.effectsEnabled = true;
            }

            if (fearEffects.playerRoot == null && playerTarget != null)
            {
                fearEffects.playerRoot = playerTarget;
            }

            if (fearEffects.cameraTarget == null)
            {
                fearEffects.cameraTarget = FindCameraTarget();
            }

            fearEffects.SetPressureSource(transform);
        }
    }

    bool HasValidContext()
    {
        return playerTarget != null && playerCamera != null && encounterStatus != null;
    }

    void HaltDoll()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        animator.speed = 0f;
        fearEffects?.ClearEncounterPresence();
    }

    void UpdateAnimation()
    {
        bool isMoving =
            currentState != DollState.Peek &&
            currentState != DollState.Recover &&
            (agent.pathPending ||
             agent.remainingDistance > agent.stoppingDistance + 0.05f ||
             agent.velocity.sqrMagnitude > 0.05f * 0.05f);

        animator.speed = isMoving ? 1f : 0.18f;
    }

    bool TryEnsureAgentOnNavMesh()
    {
        if (agent == null || !agent.enabled)
        {
            return false;
        }

        if (agent.isOnNavMesh)
        {
            return true;
        }

        float sampleRadius = Mathf.Max(initialPlacementSampleRadius, destinationSampleRadius);
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleRadius, agent.areaMask))
        {
            agent.Warp(hit.position);
            agent.stoppingDistance = stoppingDistance;
            return agent.isOnNavMesh;
        }

        return false;
    }

    bool TryCalculatePathMetrics(Vector3 start, Vector3 end, out float pathLength, out int cornerCount, out NavMeshPath path)
    {
        pathLength = 0f;
        cornerCount = 0;
        path = new NavMeshPath();

        if (agent == null || !agent.enabled)
            return false;

        if (!NavMesh.CalculatePath(start, end, agent.areaMask, path) ||
            path.status != NavMeshPathStatus.PathComplete ||
            path.corners == null ||
            path.corners.Length < 2)
        {
            return false;
        }

        for (int i = 1; i < path.corners.Length; i++)
        {
            pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        cornerCount = Mathf.Max(0, path.corners.Length - 2);
        return true;
    }

    bool IsWorldPointDirectlyVisible(Vector3 point)
    {
        Vector3 cameraOrigin = playerCamera.transform.position;
        Vector3 toPoint = point - cameraOrigin;
        float distance = toPoint.magnitude;
        if (distance > maxSightDistance || distance <= 0.001f)
        {
            return false;
        }

        Vector3 direction = toPoint / distance;
        if (Vector3.Dot(playerCamera.transform.forward, direction) < directViewDot)
        {
            return false;
        }

        return !Physics.Raycast(
            cameraOrigin,
            direction,
            Mathf.Max(0.01f, distance - 0.05f),
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
    }

    Transform FindCameraTarget()
    {
        if (playerTarget != null)
        {
            Transform[] candidates = playerTarget.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < candidates.Length; i++)
            {
                Transform candidate = candidates[i];
                if (candidate.CompareTag("CinemachineTarget") || candidate.name == "PlayerCameraRoot")
                {
                    return candidate;
                }
            }
        }

        if (playerCamera != null)
        {
            return playerCamera.transform.parent != null ? playerCamera.transform.parent : playerCamera.transform;
        }

        return null;
    }

    void PlayCue(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, cueVolume);
        }
    }

    static Vector3 GetFlatForward(Vector3 source)
    {
        source.y = 0f;
        return source.normalized;
    }

    static float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
