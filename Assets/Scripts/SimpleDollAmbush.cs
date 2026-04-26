using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SimpleDollAmbush : MonoBehaviour
{
    private enum DollState
    {
        Hidden,
        PopOut,
        ChasePlayer,
        RunAway
    }

    [Header("References")]
    public Transform player;
    public Camera playerCamera;
    public PlayerHealth playerHealth;
    public SimpleDollFearEffect fearEffect;
    public AudioSource audioSource;

    [Header("Audio")]
    public AudioClip popOutSound;
    public AudioClip hitSound;
    public AudioClip runAwaySound;

    [Header("Timing")]
    public float timeBeforeFirstAttack = 4f;
    public float attackCooldown = 7f;
    public float popOutTime = 1.2f;
    public float chaseTime = 4f;
    public float runAwayTime = 3f;

    [Header("Attack")]
    public int damage = 15;
    public float attackRange = 2.2f;
    public float timeBetweenHits = 1.25f;

    [Header("Movement")]
    public float popOutDistance = 4f;
    public float sideOffset = 1.5f;
    public float chaseSpeed = 4f;
    public float runAwaySpeed = 6f;
    public float runAwayDistance = 10f;

    [Header("Visibility")]
    public bool hideWhenWaiting = true;
    public Renderer[] dollRenderers;

    private NavMeshAgent agent;
    private DollState currentState;
    private float stateTimer;
    private float nextHitTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Start()
    {
        ResolveReferences();

        agent.stoppingDistance = 1.25f;

        EnterHidden(timeBeforeFirstAttack);
    }

    void Update()
    {
        ResolveReferences();

        if (player == null || agent == null)
        {
            return;
        }

        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case DollState.Hidden:
                UpdateHidden();
                break;

            case DollState.PopOut:
                UpdatePopOut();
                break;

            case DollState.ChasePlayer:
                UpdateChasePlayer();
                break;

            case DollState.RunAway:
                UpdateRunAway();
                break;
        }
    }

    void UpdateHidden()
    {
        if (stateTimer <= 0f)
        {
            EnterPopOut();
        }
    }

    void UpdatePopOut()
    {
        LookAtPlayer();

        if (stateTimer <= 0f)
        {
            EnterChasePlayer();
        }
    }

    void UpdateChasePlayer()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }

        TryDamagePlayer();

        if (stateTimer <= 0f)
        {
            EnterRunAway();
        }
    }

    void UpdateRunAway()
    {
        if (stateTimer <= 0f || HasReachedDestination())
        {
            EnterHidden(attackCooldown);
        }
    }

    void EnterHidden(float duration)
    {
        currentState = DollState.Hidden;
        stateTimer = duration;

        if (hideWhenWaiting)
        {
            SetDollVisible(false);
        }

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    void EnterPopOut()
    {
        currentState = DollState.PopOut;
        stateTimer = popOutTime;

        Vector3 ambushPosition = GetAmbushPosition();

        if (agent.isOnNavMesh)
        {
            agent.Warp(ambushPosition);
            agent.isStopped = true;
            agent.ResetPath();
        }
        else
        {
            transform.position = ambushPosition;
        }

        SetDollVisible(true);
        LookAtPlayer();

        if (fearEffect != null)
        {
            fearEffect.PlaySmallFearPulse();
        }

        PlaySound(popOutSound);
    }

    void EnterChasePlayer()
    {
        currentState = DollState.ChasePlayer;
        stateTimer = chaseTime;
        nextHitTime = 0f;

        SetDollVisible(true);

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
    }

    void EnterRunAway()
    {
        currentState = DollState.RunAway;
        stateTimer = runAwayTime;

        SetDollVisible(true);

        Vector3 runAwayPosition = GetRunAwayPosition();

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = runAwaySpeed;
            agent.SetDestination(runAwayPosition);
        }

        PlaySound(runAwaySound);
    }

    void TryDamagePlayer()
    {
        if (Time.time < nextHitTime)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            nextHitTime = Time.time + timeBetweenHits;

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }

            if (fearEffect != null)
            {
                fearEffect.PlayFearPulse();
            }

            PlaySound(hitSound);
        }
    }

    Vector3 GetAmbushPosition()
    {
        Vector3 cameraForward = playerCamera != null ? playerCamera.transform.forward : player.forward;
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude < 0.01f)
        {
            cameraForward = -player.forward;
        }

        cameraForward.Normalize();

        Vector3 behindPlayer = player.position - cameraForward * popOutDistance;

        float randomSide = Random.value > 0.5f ? 1f : -1f;
        Vector3 sideDirection = Vector3.Cross(Vector3.up, cameraForward).normalized;

        Vector3 targetPosition = behindPlayer + sideDirection * sideOffset * randomSide;

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return targetPosition;
    }

    Vector3 GetRunAwayPosition()
    {
        Vector3 directionAwayFromPlayer = transform.position - player.position;
        directionAwayFromPlayer.y = 0f;

        if (directionAwayFromPlayer.sqrMagnitude < 0.01f)
        {
            directionAwayFromPlayer = -player.forward;
        }

        directionAwayFromPlayer.Normalize();

        Vector3 targetPosition = transform.position + directionAwayFromPlayer * runAwayDistance;

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 6f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return targetPosition;
    }

    void LookAtPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    bool HasReachedDestination()
    {
        if (!agent.isOnNavMesh || agent.pathPending)
        {
            return false;
        }

        return agent.remainingDistance <= agent.stoppingDistance + 0.2f;
    }

    void SetDollVisible(bool visible)
    {
        if (dollRenderers == null || dollRenderers.Length == 0)
        {
            Renderer[] foundRenderers = GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < foundRenderers.Length; i++)
            {
                foundRenderers[i].enabled = visible;
            }

            return;
        }

        for (int i = 0; i < dollRenderers.Length; i++)
        {
            if (dollRenderers[i] != null)
            {
                dollRenderers[i].enabled = visible;
            }
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();

            if (playerHealth == null)
            {
                playerHealth = player.GetComponentInChildren<PlayerHealth>();
            }
        }

        if (fearEffect == null && playerCamera != null)
        {
            fearEffect = playerCamera.GetComponent<SimpleDollFearEffect>();
        }
    }
}