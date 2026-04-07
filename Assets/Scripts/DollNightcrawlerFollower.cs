using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class DollNightcrawlerFollower : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTarget;
    public string playerTag = "Player";

    [Header("Movement")]
    public float destinationRefreshRate = 0.2f;
    public float stoppingDistance = 1.6f;

    [Header("Animation")]
    public float movingSpeedThreshold = 0.05f;
    public float crawlAnimationSpeed = 1f;

    private Animator animator;
    private NavMeshAgent agent;
    private float destinationTimer;

    void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (agent != null)
        {
            agent.stoppingDistance = stoppingDistance;
        }

        FindPlayerTarget();
    }

    void Update()
    {
        if (agent == null || animator == null)
        {
            return;
        }

        if (playerTarget == null)
        {
            FindPlayerTarget();
            animator.speed = 0f;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            animator.speed = 0f;
            return;
        }

        destinationTimer -= Time.deltaTime;
        if (destinationTimer <= 0f)
        {
            destinationTimer = destinationRefreshRate;
            agent.SetDestination(playerTarget.position);
        }

        bool isMoving =
            agent.pathPending ||
            agent.remainingDistance > agent.stoppingDistance + 0.05f ||
            agent.velocity.sqrMagnitude > movingSpeedThreshold * movingSpeedThreshold;

        animator.speed = isMoving ? crawlAnimationSpeed : 0f;
    }

    void FindPlayerTarget()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }
}
