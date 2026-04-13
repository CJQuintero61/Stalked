using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ScarecrowEnemy : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Camera playerCamera;
    public Transform eyePoint;
    public Animator animator;
    public AudioSource audioSource;
    public NavMeshAgent agent;

    [Header("Vision Check")]
    public float viewAngleThreshold = 0.7f;
    public float maxSightDistance = 40f;
    public float seenFreezeBuffer = 0.1f;

    [Header("Movement")]
    public float chaseUpdateRate = 0.2f;
    public float stoppingDistance = 2.2f;
    public float navMeshRecoveryDistance = 4f;

    [Header("Attack")]
    public int attackDamage = 20;
    public float attackRange = 2.6f;
    public float attackCooldown = 1.15f;
    public float attackFacingSpeed = 10f;
    public bool attackRequiresLineOfSight = true;
    public string attackTriggerName = "";

    [Header("Anti-Stare")]
    public float maxFrozenByStareTime = 3.5f;
    public float stareBreakChaseDuration = 4f;
    public float stareBreakCooldown = 6f;

    [Header("Aggression")]
    public float aggression = 0f;
    public float aggressionIncreasePerSecond = 0.03f;
    public float maxAggression = 5f;
    public float baseSpeed = 3.5f;
    public float bonusSpeedPerAggro = 0.35f;
    public float speedIncreaseAggressionThreshold = 3.5f;
    public float ignoreStareAggressionThreshold = 4.5f;

    [Header("Aggression Behavior")]
    public float behaviorDecisionInterval = 3f;
    public float lowAggressionChaseChance = 0.15f;
    public float highAggressionChaseChance = 0.95f;
    public float lowAggressionIdleChance = 0.65f;
    public float highAggressionIdleChance = 0.15f;
    public float wanderRadius = 12f;
    public float wanderDestinationTolerance = 1.25f;

    [Header("Scarecrow Switching")]
    public bool enableScarecrowSwitching = true;
    public float minSwitchDelay = 8f;
    public float maxSwitchDelay = 18f;
    public float minSwitchDistanceFromPlayer = 10f;
    public float maxPreferredSwitchDistanceFromPlayer = 90f;
    public float switchSpawnPause = 0.75f;
    public bool onlySwitchWhenNotSeen = true;
    public bool avoidSwitchingIntoView = true;
    public float switchViewDotThreshold = 0.55f;
    public float switchSightDistance = 45f;

    private bool isSeen;
    private float chaseTimer;
    private float seenTimer;
    private float frozenByStareTimer;
    private float stareBreakTimer;
    private float stareBreakCooldownTimer;
    private float attackCooldownTimer;
    private float switchTimer;
    private float switchPauseTimer;
    private float behaviorDecisionTimer;
    private bool needsDestinationRefresh = true;
    private bool canUseAttackTrigger;
    private bool warnedMissingPlayerHealth;
    private GameObject currentDecoySpot;
    private PlayerHealth playerHealth;
    private NavMeshPath chasePath;
    private List<GameObject> decoyScarecrows;
    private AggressionBehavior currentBehavior = AggressionBehavior.Idle;

    public bool IsSeenByPlayer => isSeen;
    public bool IsIgnoringPlayerStare => ShouldIgnorePlayerSight();

    private enum AggressionBehavior
    {
        Idle,
        Wander,
        Chase
    }

    void Start()
    {
        EnsureRuntimeState();
        CacheReferences();

        if (agent != null)
            agent.stoppingDistance = stoppingDistance;

        ScheduleNextSwitch();
    }

    void Update()
    {
        if (player == null || playerCamera == null || agent == null) return;

        EnsureRuntimeState();

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        UpdateAggression();
        UpdateSeenState();
        UpdateAntiStareState();
        UpdateSwitching();

        if (switchPauseTimer > 0f)
        {
            switchPauseTimer -= Time.deltaTime;
            FreezeEnemy();
            return;
        }

        if (isSeen && !ShouldIgnorePlayerSight())
            FreezeEnemy();
        else
            UpdateAggressionBehavior();
    }

    public void ConfigureSwitchingTargets(List<GameObject> decoys, Transform playerTransform, Camera camera)
    {
        EnsureRuntimeState();
        decoyScarecrows.Clear();

        if (decoys != null)
        {
            foreach (GameObject decoy in decoys)
            {
                if (decoy != null)
                    decoyScarecrows.Add(decoy);
            }
        }

        if (player == null)
            player = playerTransform;

        if (playerCamera == null)
            playerCamera = camera;

        CacheReferences();
        ScheduleNextSwitch();
    }

    public void MoveIntoDecoy(GameObject decoy)
    {
        EnsureRuntimeState();

        if (decoy == null)
            return;

        if (currentDecoySpot != null)
            currentDecoySpot.SetActive(true);

        currentDecoySpot = decoy;
        currentDecoySpot.SetActive(false);

        Vector3 targetPosition = decoy.transform.position;
        Quaternion targetRotation = decoy.transform.rotation;

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            targetPosition = hit.position;

        WarpTo(targetPosition, targetRotation);
        ResetAfterSwitch();
        ScheduleNextSwitch();
    }

    public void ResetAfterSwitch()
    {
        isSeen = false;
        seenTimer = 0f;
        frozenByStareTimer = 0f;
        stareBreakTimer = 0f;
        chaseTimer = 0f;
        attackCooldownTimer = 0f;
        behaviorDecisionTimer = 0f;
        currentBehavior = AggressionBehavior.Idle;
        switchPauseTimer = switchSpawnPause;
        needsDestinationRefresh = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        if (animator != null)
            animator.SetBool("IsMoving", false);
    }

    void UpdateAggression()
    {
        aggression += aggressionIncreasePerSecond * Time.deltaTime;
        aggression = Mathf.Clamp(aggression, 0f, maxAggression);

        float highAggressionProgress = Mathf.InverseLerp(speedIncreaseAggressionThreshold, maxAggression, aggression);
        agent.speed = baseSpeed + (highAggressionProgress * maxAggression * bonusSpeedPerAggro);
    }

    void UpdateSeenState()
    {
        if (ShouldIgnorePlayerSight())
        {
            isSeen = false;
            seenTimer = 0f;
            return;
        }

        bool directlySeen = CanPlayerSeeEnemy();

        if (directlySeen)
        {
            seenTimer = seenFreezeBuffer;
            isSeen = true;
        }
        else
        {
            seenTimer -= Time.deltaTime;
            if (seenTimer <= 0f)
                isSeen = false;
        }
    }

    bool CanPlayerSeeEnemy()
    {
        Vector3 targetPoint = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.6f;
        Vector3 dirToEnemy = (targetPoint - playerCamera.transform.position).normalized;

        float dot = Vector3.Dot(playerCamera.transform.forward, dirToEnemy);
        if (dot < viewAngleThreshold) return false;

        float distance = Vector3.Distance(playerCamera.transform.position, targetPoint);
        if (distance > maxSightDistance) return false;

        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, dirToEnemy, out hit, maxSightDistance))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    void UpdateSwitching()
    {
        EnsureRuntimeState();

        if (!enableScarecrowSwitching || decoyScarecrows.Count <= 1)
            return;

        if (ShouldIgnorePlayerSight())
            return;

        switchTimer -= Time.deltaTime;
        if (switchTimer > 0f)
            return;

        if (onlySwitchWhenNotSeen && isSeen)
        {
            switchTimer = 1f;
            return;
        }

        GameObject nextDecoy = PickNextDecoySpot();
        if (nextDecoy != null)
            MoveIntoDecoy(nextDecoy);
        else
            switchTimer = 2f;
    }

    GameObject PickNextDecoySpot()
    {
        EnsureRuntimeState();
        decoyScarecrows.RemoveAll(decoy => decoy == null);

        List<GameObject> preferredCandidates = new List<GameObject>();
        List<(GameObject decoy, float distance)> fallbackCandidates = new List<(GameObject decoy, float distance)>();

        foreach (GameObject decoy in decoyScarecrows)
        {
            if (decoy == currentDecoySpot)
                continue;

            float distanceFromPlayer = player != null
                ? Vector3.Distance(player.position, decoy.transform.position)
                : 0f;

            if (player != null && distanceFromPlayer < minSwitchDistanceFromPlayer)
                continue;

            if (avoidSwitchingIntoView && WouldPlayerSeePosition(decoy.transform.position))
                continue;

            if (player == null || distanceFromPlayer <= maxPreferredSwitchDistanceFromPlayer)
                preferredCandidates.Add(decoy);
            else
                fallbackCandidates.Add((decoy, distanceFromPlayer));
        }

        if (preferredCandidates.Count > 0)
            return preferredCandidates[Random.Range(0, preferredCandidates.Count)];

        if (fallbackCandidates.Count == 0)
            return null;

        fallbackCandidates.Sort((a, b) => a.distance.CompareTo(b.distance));
        int nearestFallbackCount = Mathf.Min(3, fallbackCandidates.Count);
        return fallbackCandidates[Random.Range(0, nearestFallbackCount)].decoy;
    }

    bool WouldPlayerSeePosition(Vector3 position)
    {
        if (playerCamera == null)
            return false;

        Vector3 targetPoint = position + Vector3.up * 1.6f;
        Vector3 cameraPosition = playerCamera.transform.position;
        Vector3 dirToSpot = targetPoint - cameraPosition;
        float distance = dirToSpot.magnitude;

        if (distance > switchSightDistance)
            return false;

        dirToSpot.Normalize();
        float dot = Vector3.Dot(playerCamera.transform.forward, dirToSpot);
        if (dot < switchViewDotThreshold)
            return false;

        if (Physics.Raycast(cameraPosition, dirToSpot, out RaycastHit hit, switchSightDistance))
            return Vector3.Distance(hit.point, targetPoint) < 2f;

        return true;
    }

    void WarpTo(Vector3 position, Quaternion rotation)
    {
        bool warped = false;

        if (agent != null && agent.enabled)
        {
            warped = agent.Warp(position);
        }

        if (!warped)
        {
            transform.position = position;
        }

        transform.rotation = rotation;
    }

    void CacheReferences()
    {
        EnsureRuntimeState();

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (player != null)
        {
            if (playerHealth == null)
                playerHealth = player.GetComponent<PlayerHealth>();

            if (playerHealth == null)
                playerHealth = player.GetComponentInParent<PlayerHealth>();

            if (playerHealth == null)
                playerHealth = player.GetComponentInChildren<PlayerHealth>();
        }

        canUseAttackTrigger = HasAnimatorTrigger(attackTriggerName);
    }

    void ScheduleNextSwitch()
    {
        switchTimer = Random.Range(minSwitchDelay, maxSwitchDelay);
    }

    void FreezeEnemy()
    {
        if (!agent.isOnNavMesh) return;

        agent.isStopped = true;
        needsDestinationRefresh = true;

        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
        }
    }

    void UpdateAggressionBehavior()
    {
        if (ShouldIgnorePlayerSight())
        {
            currentBehavior = AggressionBehavior.Chase;
            ChasePlayer();
            return;
        }

        behaviorDecisionTimer -= Time.deltaTime;
        if (behaviorDecisionTimer <= 0f || HasFinishedWandering())
            ChooseAggressionBehavior();

        switch (currentBehavior)
        {
            case AggressionBehavior.Chase:
                ChasePlayer();
                break;
            case AggressionBehavior.Wander:
                Wander();
                break;
            default:
                Idle();
                break;
        }
    }

    void ChooseAggressionBehavior()
    {
        behaviorDecisionTimer = behaviorDecisionInterval;

        float aggressionProgress = Mathf.InverseLerp(0f, maxAggression, aggression);
        float chaseChance = Mathf.Lerp(lowAggressionChaseChance, highAggressionChaseChance, aggressionProgress);

        if (Random.value < chaseChance)
        {
            currentBehavior = AggressionBehavior.Chase;
            needsDestinationRefresh = true;
            return;
        }

        float idleChance = Mathf.Lerp(lowAggressionIdleChance, highAggressionIdleChance, aggressionProgress);
        currentBehavior = Random.value < idleChance ? AggressionBehavior.Idle : AggressionBehavior.Wander;

        if (currentBehavior == AggressionBehavior.Wander && !TrySetWanderDestination())
            currentBehavior = AggressionBehavior.Idle;
    }

    void Idle()
    {
        if (!agent.isOnNavMesh) return;

        agent.isStopped = true;
        needsDestinationRefresh = true;

        if (animator != null)
            animator.SetBool("IsMoving", false);
    }

    void Wander()
    {
        if (!agent.isOnNavMesh) return;

        agent.isStopped = false;

        if (!agent.hasPath && !TrySetWanderDestination())
        {
            currentBehavior = AggressionBehavior.Idle;
            Idle();
            return;
        }

        if (animator != null)
            animator.SetBool("IsMoving", agent.velocity.sqrMagnitude > 0.01f);
    }

    bool HasFinishedWandering()
    {
        if (currentBehavior != AggressionBehavior.Wander || agent == null || !agent.isOnNavMesh)
            return false;

        if (agent.pathPending)
            return false;

        return !agent.hasPath || agent.remainingDistance <= wanderDestinationTolerance;
    }

    bool TrySetWanderDestination()
    {
        if (agent == null || !agent.isOnNavMesh)
            return false;

        for (int i = 0; i < 8; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                return true;
            }
        }

        return false;
    }

    void ChasePlayer()
    {
        if (agent == null || !agent.enabled)
            return;

        if (!EnsureAgentOnNavMesh())
        {
            if (animator != null)
                animator.SetBool("IsMoving", false);

            return;
        }

        if (TryAttackPlayer())
            return;

        chaseTimer -= Time.deltaTime;
        bool shouldRefreshDestination =
            needsDestinationRefresh ||
            !agent.hasPath ||
            chaseTimer <= 0f;

        if (shouldRefreshDestination)
        {
            chaseTimer = chaseUpdateRate;
            if (!TrySetChaseDestination())
            {
                Idle();
                return;
            }
        }

        agent.isStopped = false;

        if (animator != null)
            animator.SetBool("IsMoving", agent.desiredVelocity.sqrMagnitude > 0.01f);
    }

    bool TryAttackPlayer()
    {
        if (!IsPlayerInAttackRange())
            return false;

        agent.isStopped = true;
        needsDestinationRefresh = true;
        FacePlayer();

        if (animator != null)
            animator.SetBool("IsMoving", false);

        if (attackCooldownTimer > 0f)
            return true;

        if (playerHealth == null)
            CacheReferences();

        if (playerHealth != null && playerHealth.currentHealth > 0)
        {
            if (canUseAttackTrigger)
                animator.SetTrigger(attackTriggerName);

            playerHealth.TakeDamage(attackDamage);
            attackCooldownTimer = attackCooldown;
        }
        else if (!warnedMissingPlayerHealth)
        {
            warnedMissingPlayerHealth = true;
            Debug.LogWarning("ScarecrowEnemy could not find PlayerHealth on the player, so attacks will not deal damage.", this);
        }

        return true;
    }

    bool IsPlayerInAttackRange()
    {
        if (player == null)
            return false;

        Vector3 attackOrigin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.4f;
        Vector3 playerTarget = player.position + Vector3.up * 1f;
        Vector3 directionToPlayer = playerTarget - attackOrigin;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > attackRange)
            return false;

        if (!attackRequiresLineOfSight || distanceToPlayer <= 0.001f)
            return true;

        if (Physics.Raycast(attackOrigin, directionToPlayer.normalized, out RaycastHit hit, attackRange + 0.5f))
            return hit.transform.root == player.root;

        return false;
    }

    bool TrySetChaseDestination()
    {
        if (player == null || agent == null || !agent.enabled)
            return false;

        EnsureRuntimeState();

        if (!EnsureAgentOnNavMesh())
            return false;

        bool foundPath = agent.CalculatePath(player.position, chasePath);
        if (!foundPath || chasePath.status == NavMeshPathStatus.PathInvalid || chasePath.corners.Length == 0)
        {
            TryRecoverChasePosition();
            return false;
        }

        needsDestinationRefresh = false;
        agent.isStopped = false;
        return agent.SetPath(chasePath);
    }

    void EnsureRuntimeState()
    {
        if (decoyScarecrows == null)
            decoyScarecrows = new List<GameObject>();

        if (chasePath == null)
            chasePath = new NavMeshPath();
    }

    bool EnsureAgentOnNavMesh()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            return true;

        return TryRecoverChasePosition();
    }

    bool TryRecoverChasePosition()
    {
        if (agent == null || !agent.enabled)
            return false;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshRecoveryDistance, NavMesh.AllAreas))
        {
            WarpTo(hit.position, transform.rotation);
            if (agent.isOnNavMesh)
            {
                needsDestinationRefresh = true;
                return true;
            }
        }

        if (enableScarecrowSwitching)
        {
            GameObject nextDecoy = PickNextDecoySpot();
            if (nextDecoy != null)
            {
                MoveIntoDecoy(nextDecoy);
                return agent.isOnNavMesh;
            }
        }

        return false;
    }

    void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 flatDirection = player.position - transform.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            attackFacingSpeed * Time.deltaTime);
    }

    bool HasAnimatorTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger &&
                parameter.name == triggerName)
            {
                return true;
            }
        }

        return false;
    }

    void UpdateAntiStareState()
    {
        if (stareBreakTimer > 0f)
        {
            stareBreakTimer -= Time.deltaTime;
            frozenByStareTimer = 0f;
            return;
        }

        if (stareBreakCooldownTimer > 0f)
            stareBreakCooldownTimer -= Time.deltaTime;

        if (isSeen)
        {
            frozenByStareTimer += Time.deltaTime;
            if (frozenByStareTimer >= maxFrozenByStareTime && stareBreakCooldownTimer <= 0f)
            {
                BreakOutOfStareFreeze();
            }
        }
        else
        {
            frozenByStareTimer = 0f;
        }
    }

    void BreakOutOfStareFreeze()
    {
        stareBreakTimer = stareBreakChaseDuration;
        stareBreakCooldownTimer = stareBreakCooldown;
        isSeen = false;
        seenTimer = 0f;
        frozenByStareTimer = 0f;
        needsDestinationRefresh = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    bool ShouldIgnorePlayerSight()
    {
        return stareBreakTimer > 0f || aggression >= ignoreStareAggressionThreshold;
    }
}
