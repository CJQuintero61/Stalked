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

    [Header("Aggression")]
    public float aggression = 0f;
    public float aggressionIncreasePerSecond = 0.03f;
    public float maxAggression = 5f;
    public float baseSpeed = 3.5f;
    public float bonusSpeedPerAggro = 0.35f;

    private bool isSeen;
    private float chaseTimer;
    private float seenTimer;
    private bool needsDestinationRefresh = true;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (agent != null)
            agent.stoppingDistance = stoppingDistance;
    }

    void Update()
    {
        if (player == null || playerCamera == null || agent == null) return;

        UpdateAggression();
        UpdateSeenState();

        if (isSeen)
            FreezeEnemy();
        else
            ChasePlayer();
    }

    void UpdateAggression()
    {
        aggression += aggressionIncreasePerSecond * Time.deltaTime;
        aggression = Mathf.Clamp(aggression, 0f, maxAggression);

        agent.speed = baseSpeed + (aggression * bonusSpeedPerAggro);
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

    void ChasePlayer()
    {
        if (!agent.isOnNavMesh) return;

        agent.isStopped = false;

        if (animator != null)
        {
            animator.SetBool("IsMoving", true);
        }

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