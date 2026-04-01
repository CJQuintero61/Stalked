using UnityEngine;
using UnityEngine.AI;
using System.Collections;

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

    [Header("Attack")]
    public float attackRange = 2.3f;
    public float attackCooldown = 2.0f;
    public float attackDuration = 1.0f;
    public float postAttackMoveDelay = 0.3f;
    public float attackPathRangeBuffer = 0.25f;
    public int damage = 25;

    [Header("Aggression")]
    public float aggression = 0f;
    public float aggressionIncreasePerSecond = 0.03f;
    public float maxAggression = 5f;
    public float baseSpeed = 3.5f;
    public float bonusSpeedPerAggro = 0.35f;

    private bool isSeen;
    private bool isAttacking;
    private bool inPostAttackRecovery;
    private float lastAttackTime;
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

        if (isAttacking || inPostAttackRecovery)
            return;

        if (isSeen)
            FreezeEnemy();
        else
            ChasePlayer();

        TryAttack();
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
    }

    void ChasePlayer()
    {
        if (!agent.isOnNavMesh) return;

        agent.isStopped = false;

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

    void TryAttack()
    {
        if (isAttacking || inPostAttackRecovery) return;

        if (IsPlayerInAttackRange() && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    bool IsPlayerInAttackRange()
    {
        Vector3 enemyPosition = agent != null && agent.isOnNavMesh ? agent.nextPosition : transform.position;
        Vector3 playerPosition = player.position;

        enemyPosition.y = 0f;
        playerPosition.y = 0f;

        float directDistance = Vector3.Distance(enemyPosition, playerPosition);
        if (directDistance > attackRange)
            return false;

        bool hasUsablePath =
            agent != null &&
            agent.isOnNavMesh &&
            !agent.pathPending &&
            agent.hasPath &&
            agent.pathStatus == NavMeshPathStatus.PathComplete;

        if (!hasUsablePath)
            return true;

        return agent.remainingDistance <= attackRange + attackPathRangeBuffer;
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (IsPlayerInAttackRange())
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        inPostAttackRecovery = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            needsDestinationRefresh = false;
            chaseTimer = chaseUpdateRate;
            agent.SetDestination(player.position);
        }

        yield return new WaitForSeconds(postAttackMoveDelay);

        inPostAttackRecovery = false;
        needsDestinationRefresh = true;
        chaseTimer = 0f;
    }
}
