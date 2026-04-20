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

    private bool isSeen;
    private float chaseTimer;
    private float seenTimer;
    private bool needsDestinationRefresh = true;
    private GameObject currentDecoySpot;
    public bool IsSeenByPlayer => isSeen;

    void Start()
    {
        CacheReferences();

        if (agent != null)
            agent.stoppingDistance = stoppingDistance;
    }

    void Update()
    {
        if (player == null || playerCamera == null || agent == null)
            return;

        UpdateSeenState();

        if (isSeen)
            FreezeEnemy();
        else
            ChasePlayer();
    }

    public void ConfigureSwitchingTargets(List<GameObject> decoys, Transform playerTransform, Camera camera)
    {
        if (player == null)
            player = playerTransform;

        if (playerCamera == null)
            playerCamera = camera;

        CacheReferences();
    }

    public void MoveIntoDecoy(GameObject decoy)
    {
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
        ResetAfterSpawn();
    }

    void ResetAfterSpawn()
    {
        isSeen = false;
        seenTimer = 0f;
        chaseTimer = 0f;
        needsDestinationRefresh = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        if (animator != null)
            animator.SetBool("IsMoving", false);
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
        Vector3 targetPoint = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.6f;
        Vector3 dirToEnemy = (targetPoint - playerCamera.transform.position).normalized;

        float dot = Vector3.Dot(playerCamera.transform.forward, dirToEnemy);
        if (dot < viewAngleThreshold)
            return false;

        float distance = Vector3.Distance(playerCamera.transform.position, targetPoint);
        if (distance > maxSightDistance)
            return false;

        if (Physics.Raycast(playerCamera.transform.position, dirToEnemy, out RaycastHit hit, maxSightDistance))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                return true;
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

    void FreezeEnemy()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        needsDestinationRefresh = true;

        if (animator != null)
            animator.SetBool("IsMoving", false);
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
