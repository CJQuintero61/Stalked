using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ScarecrowEnemy : MonoBehaviour, IDamageDealer
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
    public bool cornBlocksSight = true;
    public string[] cornNameKeywords = { "corn" };
    public float cornSightBlockRadius = 1.5f;
    public float cornSightSampleSpacing = 1.25f;
    public LayerMask cornCollisionMask = ~0;

    [Header("Movement")]
    public float chaseUpdateRate = 0.2f;
    public float stoppingDistance = 2.2f;

    [Header("Attack")]
    public int attackDamage = 15;
    public float attackRange = 2.4f;
    public float attackCooldown = 1.1f;
    public float attackTurnSpeed = 540f;

    [Header("Footsteps")]
    public AudioClip[] footstepSounds;
    public float stepDistance = 1.8f;
    public float footstepVolume = 0.65f;
    public float footstepPitch = 0.95f;
    public float footstepPitchVariance = 0.08f;
    public float minimumFootstepSpeed = 0.15f;

    [Header("Decoy Switching")]
    public bool enableScarecrowSwitching = true;
    public float minSwitchDelay = 8f;
    public float maxSwitchDelay = 18f;
    public float minSwitchDistanceFromPlayer = 10f;
    public float maxSwitchDistanceFromPlayer = 80f;
    public float switchSpawnPause = 0.75f;
    public bool avoidSwitchingIntoView = true;
    public float switchViewDotThreshold = 0.55f;
    public float switchSightDistance = 45f;

    private bool isSeen;
    private float chaseTimer;
    private float seenTimer;
    private float switchTimer;
    private float switchPauseTimer;
    private bool needsDestinationRefresh = true;
    private GameObject currentDecoySpot;
    private PlayerHealth playerHealth;
    private readonly List<GameObject> decoySpots = new List<GameObject>();
    private readonly List<Renderer> cornRenderers = new List<Renderer>();
    private readonly List<TerrainCornDetailLayer> cornDetailLayers = new List<TerrainCornDetailLayer>();
    private readonly List<Vector3> cornTerrainTreePositions = new List<Vector3>();
    private Vector3 lastFootstepPosition;
    private float footstepDistanceTravelled;
    private float attackCooldownTimer;
    public bool IsSeenByPlayer => isSeen;

    public Transform DamageSourceTransform => eyePoint != null ? eyePoint : transform;

    private struct TerrainCornDetailLayer
    {
        public Terrain terrain;
        public int layerIndex;
    }

    void Start()
    {
        CacheReferences();
        CacheCornSightBlockers();
        CachePlayerHealth();

        lastFootstepPosition = transform.position;

        if (agent != null)
            agent.stoppingDistance = stoppingDistance;
    }

    void Update()
    {
        if (player == null || playerCamera == null || agent == null)
            return;

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        UpdateSeenState();
        UpdateDecoySwitching();

        if (switchPauseTimer > 0f)
        {
            FreezeEnemy();
            UpdateFootsteps();
            return;
        }

        if (isSeen)
            FreezeEnemy();
        else if (TryAttackPlayer())
            FreezeEnemy();
        else
            ChasePlayer();

        UpdateFootsteps();
    }

    public void ConfigureSwitchingTargets(List<GameObject> decoys, Transform playerTransform, Camera camera)
    {
        if (player == null)
            player = playerTransform;

        if (playerCamera == null)
            playerCamera = camera;

        decoySpots.Clear();
        if (decoys != null)
        {
            foreach (GameObject decoy in decoys)
            {
                if (decoy != null)
                    decoySpots.Add(decoy);
            }
        }

        CacheReferences();
        CachePlayerHealth();
        ResetSwitchTimer();
    }

    public void MoveIntoDecoy(GameObject decoy)
    {
        if (decoy == null)
            return;

        if (currentDecoySpot != null)
        {
            currentDecoySpot.transform.SetPositionAndRotation(transform.position, transform.rotation);
            currentDecoySpot.SetActive(true);
        }

        currentDecoySpot = decoy;
        currentDecoySpot.SetActive(false);

        Vector3 targetPosition = decoy.transform.position;
        Quaternion targetRotation = decoy.transform.rotation;

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            targetPosition = hit.position;

        WarpTo(targetPosition, targetRotation);
        ResetAfterSpawn();
    }

    void ResetAfterSpawn()
    {
        isSeen = false;
        seenTimer = 0f;
        chaseTimer = 0f;
        switchPauseTimer = switchSpawnPause;
        needsDestinationRefresh = true;
        ResetSwitchTimer();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        if (animator != null)
            animator.SetBool("IsMoving", false);

        ResetFootstepTracking();
    }

    void UpdateDecoySwitching()
    {
        if (!enableScarecrowSwitching || decoySpots.Count <= 1)
            return;

        if (switchPauseTimer > 0f)
        {
            switchPauseTimer -= Time.deltaTime;
            return;
        }

        if (!isSeen)
            return;

        switchTimer -= Time.deltaTime;
        if (switchTimer > 0f)
            return;

        if (TryGetNextDecoy(out GameObject nextDecoy))
            MoveIntoDecoy(nextDecoy);
        else
            ResetSwitchTimer();
    }

    void ResetSwitchTimer()
    {
        float minimumDelay = Mathf.Max(0.1f, minSwitchDelay);
        float maximumDelay = Mathf.Max(minimumDelay, maxSwitchDelay);
        switchTimer = Random.Range(minimumDelay, maximumDelay);
    }

    bool TryGetNextDecoy(out GameObject nextDecoy)
    {
        List<GameObject> strictValidDecoys = new List<GameObject>();
        List<GameObject> relaxedValidDecoys = new List<GameObject>();

        foreach (GameObject decoy in decoySpots)
        {
            if (IsValidSwitchTarget(decoy, ignoreVisibilityCheck: false))
                strictValidDecoys.Add(decoy);

            if (IsValidSwitchTarget(decoy, ignoreVisibilityCheck: true))
                relaxedValidDecoys.Add(decoy);
        }

        if (strictValidDecoys.Count > 0)
        {
            nextDecoy = strictValidDecoys[Random.Range(0, strictValidDecoys.Count)];
            return true;
        }

        if (relaxedValidDecoys.Count > 0)
        {
            nextDecoy = relaxedValidDecoys[Random.Range(0, relaxedValidDecoys.Count)];
            return true;
        }

        nextDecoy = null;
        return false;
    }

    bool IsValidSwitchTarget(GameObject decoy, bool ignoreVisibilityCheck)
    {
        if (decoy == null || decoy == currentDecoySpot)
            return false;

        if (player != null)
        {
            float distanceFromPlayer = Vector3.Distance(player.position, decoy.transform.position);
            if (distanceFromPlayer < minSwitchDistanceFromPlayer)
                return false;

            if (maxSwitchDistanceFromPlayer > 0f && distanceFromPlayer > maxSwitchDistanceFromPlayer)
                return false;
        }

        if (!ignoreVisibilityCheck && avoidSwitchingIntoView && IsDecoyVisibleToPlayer(decoy))
            return false;

        return true;
    }

    bool IsDecoyVisibleToPlayer(GameObject decoy)
    {
        if (playerCamera == null || decoy == null)
            return false;

        Vector3 targetPoint = GetSightTargetPoint(decoy.transform);
        Vector3 toTarget = targetPoint - playerCamera.transform.position;
        float distance = toTarget.magnitude;

        if (distance <= 0.01f || distance > switchSightDistance)
            return false;

        Vector3 direction = toTarget / distance;
        float dot = Vector3.Dot(playerCamera.transform.forward, direction);
        if (dot < switchViewDotThreshold)
            return false;

        if (Physics.Raycast(playerCamera.transform.position, direction, out RaycastHit hit, distance))
            return hit.transform == decoy.transform || hit.transform.IsChildOf(decoy.transform);

        return false;
    }

    Vector3 GetSightTargetPoint(Transform targetTransform)
    {
        if (eyePoint != null)
        {
            Vector3 localEyeOffset = transform.InverseTransformPoint(eyePoint.position);
            return targetTransform.TransformPoint(localEyeOffset);
        }

        return targetTransform.position + Vector3.up * 1.6f;
    }

    void UpdateSeenState()
    {
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
        Vector3 targetPoint = GetSightTargetPoint(transform);
        Vector3 sightStart = playerCamera.transform.position;
        Vector3 dirToEnemy = (targetPoint - sightStart).normalized;

        float dot = Vector3.Dot(playerCamera.transform.forward, dirToEnemy);
        if (dot < viewAngleThreshold)
            return false;

        float distance = Vector3.Distance(sightStart, targetPoint);
        if (distance > maxSightDistance)
            return false;

        if (IsSightBlockedByCorn(sightStart, targetPoint))
            return false;

        if (Physics.Raycast(sightStart, dirToEnemy, out RaycastHit hit, maxSightDistance))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    bool IsSightBlockedByCorn(Vector3 sightStart, Vector3 sightEnd)
    {
        if (!cornBlocksSight)
            return false;

        return IsCornColliderBlockingSight(sightStart, sightEnd) ||
               IsCornRendererBlockingSight(sightStart, sightEnd) ||
               IsCornTerrainTreeBlockingSight(sightStart, sightEnd) ||
               IsCornTerrainDetailBlockingSight(sightStart, sightEnd);
    }

    bool IsCornColliderBlockingSight(Vector3 sightStart, Vector3 sightEnd)
    {
        Vector3 direction = sightEnd - sightStart;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
            return false;

        direction /= distance;
        RaycastHit[] hits = Physics.RaycastAll(
            sightStart,
            direction,
            distance,
            cornCollisionMask,
            QueryTriggerInteraction.Collide);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && IsCornObject(hit.collider.transform))
                return true;
        }

        return false;
    }

    bool IsCornRendererBlockingSight(Vector3 sightStart, Vector3 sightEnd)
    {
        Vector3 direction = sightEnd - sightStart;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
            return false;

        Ray sightRay = new Ray(sightStart, direction / distance);
        foreach (Renderer cornRenderer in cornRenderers)
        {
            if (cornRenderer == null)
                continue;

            Bounds bounds = cornRenderer.bounds;
            bounds.Expand(cornSightBlockRadius * 2f);

            if (bounds.Contains(sightStart))
                return true;

            if (bounds.IntersectRay(sightRay, out float hitDistance) && hitDistance <= distance)
                return true;
        }

        return false;
    }

    bool IsCornTerrainTreeBlockingSight(Vector3 sightStart, Vector3 sightEnd)
    {
        float blockingRadiusSqr = cornSightBlockRadius * cornSightBlockRadius;
        foreach (Vector3 treePosition in cornTerrainTreePositions)
        {
            if (DistanceToSegmentXZSquared(treePosition, sightStart, sightEnd) <= blockingRadiusSqr)
                return true;
        }

        return false;
    }

    bool IsCornTerrainDetailBlockingSight(Vector3 sightStart, Vector3 sightEnd)
    {
        float distance = Vector3.Distance(sightStart, sightEnd);
        if (distance <= 0.01f)
            return false;

        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.25f, cornSightSampleSpacing)));
        for (int i = 1; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            Vector3 samplePoint = Vector3.Lerp(sightStart, sightEnd, t);
            if (IsOnCornTerrainDetail(samplePoint))
                return true;
        }

        return false;
    }

    float DistanceToSegmentXZSquared(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
    {
        Vector2 pointXZ = new Vector2(point.x, point.z);
        Vector2 startXZ = new Vector2(segmentStart.x, segmentStart.z);
        Vector2 endXZ = new Vector2(segmentEnd.x, segmentEnd.z);
        Vector2 segment = endXZ - startXZ;

        float segmentLengthSqr = segment.sqrMagnitude;
        if (segmentLengthSqr <= Mathf.Epsilon)
            return (pointXZ - startXZ).sqrMagnitude;

        float projection = Mathf.Clamp01(Vector2.Dot(pointXZ - startXZ, segment) / segmentLengthSqr);
        Vector2 closestPoint = startXZ + segment * projection;
        return (pointXZ - closestPoint).sqrMagnitude;
    }

    bool IsOnCornTerrainDetail(Vector3 position)
    {
        foreach (TerrainCornDetailLayer cornDetailLayer in cornDetailLayers)
        {
            Terrain terrain = cornDetailLayer.terrain;
            if (terrain == null || terrain.terrainData == null)
                continue;

            TerrainData terrainData = terrain.terrainData;
            Vector3 localPosition = position - terrain.transform.position;

            if (localPosition.x < 0f || localPosition.z < 0f ||
                localPosition.x > terrainData.size.x || localPosition.z > terrainData.size.z)
                continue;

            int detailX = Mathf.FloorToInt((localPosition.x / terrainData.size.x) * terrainData.detailWidth);
            int detailZ = Mathf.FloorToInt((localPosition.z / terrainData.size.z) * terrainData.detailHeight);

            detailX = Mathf.Clamp(detailX, 0, terrainData.detailWidth - 1);
            detailZ = Mathf.Clamp(detailZ, 0, terrainData.detailHeight - 1);

            int[,] details = terrainData.GetDetailLayer(detailX, detailZ, 1, 1, cornDetailLayer.layerIndex);
            if (details[0, 0] > 0)
                return true;
        }

        return false;
    }

    void CacheCornSightBlockers()
    {
        cornRenderers.Clear();
        cornDetailLayers.Clear();
        cornTerrainTreePositions.Clear();

        if (!cornBlocksSight)
            return;

        foreach (Renderer sceneRenderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (sceneRenderer != null && IsCornObject(sceneRenderer.transform))
                cornRenderers.Add(sceneRenderer);
        }

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null)
                continue;

            DetailPrototype[] detailPrototypes = terrain.terrainData.detailPrototypes;
            for (int i = 0; i < detailPrototypes.Length; i++)
            {
                DetailPrototype detailPrototype = detailPrototypes[i];
                if (IsCornName(detailPrototype.prototype != null ? detailPrototype.prototype.name : null) ||
                    IsCornName(detailPrototype.prototypeTexture != null ? detailPrototype.prototypeTexture.name : null))
                {
                    cornDetailLayers.Add(new TerrainCornDetailLayer
                    {
                        terrain = terrain,
                        layerIndex = i
                    });
                }
            }

            CacheCornTerrainTrees(terrain);
        }
    }

    void CacheCornTerrainTrees(Terrain terrain)
    {
        TerrainData terrainData = terrain.terrainData;
        TreePrototype[] treePrototypes = terrainData.treePrototypes;
        if (treePrototypes == null || treePrototypes.Length == 0)
            return;

        HashSet<int> cornPrototypeIndexes = new HashSet<int>();
        for (int i = 0; i < treePrototypes.Length; i++)
        {
            GameObject treePrefab = treePrototypes[i].prefab;
            if (treePrefab != null && IsCornName(treePrefab.name))
                cornPrototypeIndexes.Add(i);
        }

        if (cornPrototypeIndexes.Count == 0)
            return;

        foreach (TreeInstance treeInstance in terrainData.treeInstances)
        {
            if (!cornPrototypeIndexes.Contains(treeInstance.prototypeIndex))
                continue;

            Vector3 terrainSpacePosition = Vector3.Scale(treeInstance.position, terrainData.size);
            cornTerrainTreePositions.Add(terrain.transform.position + terrainSpacePosition);
        }
    }

    bool IsCornObject(Transform objectTransform)
    {
        Transform current = objectTransform;
        while (current != null)
        {
            if (IsCornName(current.name))
                return true;

            current = current.parent;
        }

        return false;
    }

    bool IsCornName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName) || cornNameKeywords == null)
            return false;

        foreach (string keyword in cornNameKeywords)
        {
            if (!string.IsNullOrEmpty(keyword) &&
                objectName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    void WarpTo(Vector3 position, Quaternion rotation)
    {
        bool warped = false;

        if (agent != null && agent.enabled)
            warped = agent.Warp(position);

        if (!warped)
            transform.position = position;

        transform.rotation = rotation;
    }

    void CacheReferences()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void CachePlayerHealth()
    {
        if (player == null)
            return;

        if (playerHealth == null)
            playerHealth = player.GetComponent<PlayerHealth>() ?? player.GetComponentInChildren<PlayerHealth>();
    }

    void FreezeEnemy()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        needsDestinationRefresh = true;

        if (animator != null)
            animator.SetBool("IsMoving", false);
    }

    bool TryAttackPlayer()
    {
        if (!IsPlayerWithinAttackRange())
            return false;

        FacePlayer();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
            animator.SetBool("IsMoving", false);

        CachePlayerHealth();
        if (attackCooldownTimer > 0f || playerHealth == null)
            return true;

        playerHealth.TakeDamage(attackDamage, this);
        attackCooldownTimer = Mathf.Max(0.1f, attackCooldown);
        Debug.Log($"Scarecrow attacked player for {attackDamage} damage.");
        return true;
    }

    bool IsPlayerWithinAttackRange()
    {
        if (player == null)
            return false;

        Vector3 playerPosition = player.position;
        Vector3 enemyPosition = transform.position;
        playerPosition.y = 0f;
        enemyPosition.y = 0f;
        return Vector3.Distance(enemyPosition, playerPosition) <= attackRange;
    }

    void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            attackTurnSpeed * Time.deltaTime);
    }

    void UpdateFootsteps()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastFootstepPosition);
        lastFootstepPosition = transform.position;

        if (agent == null || audioSource == null || footstepSounds == null || footstepSounds.Length == 0)
        {
            footstepDistanceTravelled = 0f;
            return;
        }

        bool isMoving = !agent.isStopped && agent.velocity.magnitude > minimumFootstepSpeed;
        if (!isMoving)
            return;

        footstepDistanceTravelled += distanceMoved;

        if (footstepDistanceTravelled < stepDistance)
            return;

        PlayFootstep();
        footstepDistanceTravelled = 0f;
    }

    void ResetFootstepTracking()
    {
        footstepDistanceTravelled = 0f;
        lastFootstepPosition = transform.position;
    }

    void PlayFootstep()
    {
        int clipIndex = Random.Range(0, footstepSounds.Length);
        audioSource.pitch = footstepPitch + Random.Range(-footstepPitchVariance, footstepPitchVariance);
        audioSource.PlayOneShot(footstepSounds[clipIndex], footstepVolume);
    }

    void ChasePlayer()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = false;

        if (animator != null)
            animator.SetBool("IsMoving", true);

        chaseTimer -= Time.deltaTime;
        bool shouldRefreshDestination =
            needsDestinationRefresh ||
            !agent.hasPath ||
            chaseTimer <= 0f;

        if (shouldRefreshDestination)
        {
            chaseTimer = chaseUpdateRate;
            needsDestinationRefresh = false;
            agent.SetDestination(player.position);
        }
    }
}
