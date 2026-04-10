using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class DollNightcrawlerFollower : MonoBehaviour
{
    private enum DollState
    {
        Search,
        Peek,
        Pressure,
        Grab,
        Cooldown
    }

    [Header("References")]
    public Transform playerTarget;
    public Camera playerCamera;
    public PlayerHealth playerHealth;
    public PlayerDollFearEffects fearEffects;
    public Transform eyePoint;
    public AudioSource audioSource;
    public string playerTag = "Player";

    [Header("Movement")]
    public float stoppingDistance = 1.5f;
    public float relocationRefreshRate = 0.75f;
    public float relocateMinRadius = 3.5f;
    public float relocateMaxRadius = 8.5f;
    public float retreatMinRadius = 6.5f;
    public float retreatMaxRadius = 11f;
    public float destinationSampleRadius = 2.5f;
    public int relocationSamples = 18;

    [Header("Perception")]
    [Range(-1f, 1f)]
    public float directViewDot = 0.85f;
    [Range(-1f, 1f)]
    public float blindSpotDot = 0.2f;
    public float maxSightDistance = 30f;
    public float pressureDistance = 2.8f;
    public float pressureAnchorDistance = 1.8f;

    [Header("State Timings")]
    public float peekDuration = 0.9f;
    public float pressureBuildDuration = 1.4f;
    public float grabDuration = 1.1f;
    public float cooldownDuration = 2.5f;
    public float pressureDestinationRefreshRate = 0.2f;

    [Header("Combat")]
    public int grabDamage = 25;
    public float grabSnapDistance = 1.2f;

    [Header("Animation")]
    public float movingSpeedThreshold = 0.05f;
    public float crawlAnimationSpeed = 1f;

    private Animator animator;
    private NavMeshAgent agent;
    private DollState currentState;
    private float stateTimer;
    private float destinationTimer;
    private float pressureProgress;
    private bool grabDamageApplied;

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
            agent.stoppingDistance = stoppingDistance;
        }

        ResolveReferences();
        if (HasValidContext())
        {
            EnterSearch(false);
        }
        else
        {
            currentState = DollState.Search;
        }
    }

    void OnDisable()
    {
        if (fearEffects != null)
        {
            fearEffects.ClearDollPressure();
        }
    }

    void Update()
    {
        if (agent == null || animator == null)
        {
            return;
        }

        ResolveReferences();
        if (!HasValidContext())
        {
            HaltDoll();
            return;
        }

        if (!agent.isOnNavMesh)
        {
            HaltDoll();
            return;
        }

        stateTimer -= Time.deltaTime;
        destinationTimer -= Time.deltaTime;

        bool directlySeen = CanPlayerDirectlySeeDoll();
        bool inBlindSpot = IsInPlayerBlindSpot();
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        switch (currentState)
        {
            case DollState.Search:
                UpdateSearch(directlySeen, inBlindSpot, distanceToPlayer);
                break;
            case DollState.Peek:
                UpdatePeek(directlySeen, inBlindSpot, distanceToPlayer);
                break;
            case DollState.Pressure:
                UpdatePressure(directlySeen, inBlindSpot, distanceToPlayer);
                break;
            case DollState.Grab:
                UpdateGrab();
                break;
            case DollState.Cooldown:
                UpdateCooldown();
                break;
        }

        UpdateAnimation();
    }

    private void UpdateSearch(bool directlySeen, bool inBlindSpot, float distanceToPlayer)
    {
        agent.isStopped = false;
        pressureProgress = 0f;
        fearEffects?.ClearDollPressure();

        if (directlySeen)
        {
            TrySetRelocation(retreatMinRadius, retreatMaxRadius);
            stateTimer = relocationRefreshRate;
            return;
        }

        if (distanceToPlayer <= pressureDistance && inBlindSpot)
        {
            EnterPressure();
            return;
        }

        bool shouldRelocate =
            destinationTimer <= 0f ||
            !agent.hasPath ||
            ReachedDestination();

        if (shouldRelocate)
        {
            bool chosePoint = TrySetRelocation(relocateMinRadius, relocateMaxRadius);
            stateTimer = relocationRefreshRate;
            if (!chosePoint)
            {
                return;
            }
        }

        if (ReachedDestination())
        {
            EnterPeek();
        }
    }

    private void UpdatePeek(bool directlySeen, bool inBlindSpot, float distanceToPlayer)
    {
        agent.isStopped = true;
        fearEffects?.ClearDollPressure();

        if (directlySeen)
        {
            EnterSearch(true);
            return;
        }

        if (distanceToPlayer <= pressureDistance && inBlindSpot)
        {
            EnterPressure();
            return;
        }

        if (stateTimer <= 0f)
        {
            EnterSearch(false);
        }
    }

    private void UpdatePressure(bool directlySeen, bool inBlindSpot, float distanceToPlayer)
    {
        if (directlySeen)
        {
            EnterSearch(true);
            return;
        }

        if (distanceToPlayer > pressureDistance || !inBlindSpot)
        {
            EnterSearch(false);
            return;
        }

        agent.isStopped = false;

        if (destinationTimer <= 0f)
        {
            destinationTimer = pressureDestinationRefreshRate;
            TrySetPressureAnchor();
        }

        pressureProgress += Time.deltaTime / Mathf.Max(pressureBuildDuration, 0.01f);
        fearEffects?.SetDollPressure(pressureProgress);

        if (pressureProgress >= 1f)
        {
            EnterGrab();
        }
    }

    private void UpdateGrab()
    {
        agent.isStopped = true;
        fearEffects?.SetDollPressure(1f);

        if (!grabDamageApplied && stateTimer <= grabDuration * 0.5f)
        {
            playerHealth?.TakeDamage(grabDamage);
            grabDamageApplied = true;
        }

        if (stateTimer <= 0f)
        {
            EnterCooldown();
        }
    }

    private void UpdateCooldown()
    {
        fearEffects?.ClearDollPressure();
        agent.isStopped = false;

        if (!agent.hasPath || ReachedDestination())
        {
            TrySetRelocation(retreatMinRadius, retreatMaxRadius);
        }

        if (stateTimer <= 0f)
        {
            EnterSearch(false);
        }
    }

    private void EnterSearch(bool emergencyRetreat)
    {
        currentState = DollState.Search;
        stateTimer = relocationRefreshRate;
        destinationTimer = 0f;
        pressureProgress = 0f;
        grabDamageApplied = false;
        agent.isStopped = false;
        fearEffects?.ClearDollPressure();

        float minRadius = emergencyRetreat ? retreatMinRadius : relocateMinRadius;
        float maxRadius = emergencyRetreat ? retreatMaxRadius : relocateMaxRadius;
        TrySetRelocation(minRadius, maxRadius);
    }

    private void EnterPeek()
    {
        currentState = DollState.Peek;
        stateTimer = peekDuration;
        pressureProgress = 0f;
        agent.isStopped = true;
    }

    private void EnterPressure()
    {
        currentState = DollState.Pressure;
        pressureProgress = 0f;
        destinationTimer = 0f;
        agent.isStopped = false;
        TrySetPressureAnchor();
    }

    private void EnterGrab()
    {
        currentState = DollState.Grab;
        stateTimer = grabDuration;
        grabDamageApplied = false;
        agent.isStopped = true;
        fearEffects?.PlayDollGrab(grabDuration);

        Vector3 grabPosition;
        if (TryGetBehindPlayerPosition(grabSnapDistance, out grabPosition))
        {
            agent.Warp(grabPosition);
        }

        FacePlayer();
    }

    private void EnterCooldown()
    {
        currentState = DollState.Cooldown;
        stateTimer = cooldownDuration;
        destinationTimer = 0f;
        pressureProgress = 0f;
        fearEffects?.ClearDollPressure();
        agent.isStopped = false;
        TrySetRelocation(retreatMinRadius, retreatMaxRadius);
    }

    private void ResolveReferences()
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

        if (playerHealth == null && playerTarget != null)
        {
            playerHealth = playerTarget.GetComponent<PlayerHealth>();
        }

        if (fearEffects == null && playerCamera != null)
        {
            fearEffects = playerCamera.GetComponent<PlayerDollFearEffects>();
        }

        if (fearEffects != null)
        {
            fearEffects.SetPressureSource(transform);
        }
    }

    private bool HasValidContext()
    {
        return playerTarget != null && playerCamera != null;
    }

    private void HaltDoll()
    {
        agent.isStopped = true;
        animator.speed = 0f;
        fearEffects?.ClearDollPressure();
    }

    private void UpdateAnimation()
    {
        bool isMoving =
            currentState != DollState.Peek &&
            (agent.pathPending ||
             agent.remainingDistance > agent.stoppingDistance + 0.05f ||
             agent.velocity.sqrMagnitude > movingSpeedThreshold * movingSpeedThreshold);

        animator.speed = isMoving ? crawlAnimationSpeed : 0f;
    }

    private bool CanPlayerDirectlySeeDoll()
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

    private bool IsInPlayerBlindSpot()
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
        return dot <= blindSpotDot;
    }

    private Vector3 GetFocusPoint()
    {
        if (eyePoint != null)
        {
            return eyePoint.position;
        }

        return transform.position + Vector3.up * 0.9f;
    }

    private bool ReachedDestination()
    {
        return !agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.2f;
    }

    private bool TrySetRelocation(float minRadius, float maxRadius)
    {
        if (TryFindRelocationPoint(minRadius, maxRadius, out Vector3 destination))
        {
            destinationTimer = relocationRefreshRate;
            agent.SetDestination(destination);
            return true;
        }

        return false;
    }

    private bool TrySetPressureAnchor()
    {
        if (TryGetBehindPlayerPosition(pressureAnchorDistance, out Vector3 anchor))
        {
            agent.SetDestination(anchor);
            return true;
        }

        return TrySetRelocation(relocateMinRadius, relocateMaxRadius);
    }

    private bool TryGetBehindPlayerPosition(float radius, out Vector3 result)
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
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, destinationSampleRadius, agent.areaMask))
            {
                result = hit.position;
                return true;
            }
        }

        return false;
    }

    private bool TryFindRelocationPoint(float minRadius, float maxRadius, out Vector3 bestPoint)
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
        float[] anchorAngles = { 180f, 145f, 215f, 110f, 250f };

        for (int i = 0; i < relocationSamples; i++)
        {
            float anchorAngle = anchorAngles[i % anchorAngles.Length];
            float angle = anchorAngle + Random.Range(-18f, 18f);
            float radius = Random.Range(minRadius, maxRadius);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * flatForward;
            Vector3 candidate = playerPosition + direction * radius;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, destinationSampleRadius, agent.areaMask))
            {
                continue;
            }

            Vector3 fromPlayer = hit.position - playerPosition;
            fromPlayer.y = 0f;
            if (fromPlayer.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            Vector3 normalized = fromPlayer.normalized;
            float frontDot = Vector3.Dot(flatForward, normalized);
            float sideAmount = Mathf.Abs(Vector3.Dot(flatRight, normalized));
            bool directlyVisible = IsPointDirectlyVisible(hit.position);

            float score = 0f;
            score += -frontDot * 3f;
            score += sideAmount;
            score += directlyVisible ? -2f : 1f;

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = hit.position;
            }
        }

        return bestScore > float.NegativeInfinity;
    }

    private bool IsPointDirectlyVisible(Vector3 point)
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

        if (Physics.Raycast(cameraOrigin, direction, out RaycastHit hit, distance + 0.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == transform || hit.transform.IsChildOf(transform);
        }

        return false;
    }

    private void FacePlayer()
    {
        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= 0.001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
    }

    private static Vector3 GetFlatForward(Vector3 source)
    {
        source.y = 0f;
        return source.normalized;
    }
}
