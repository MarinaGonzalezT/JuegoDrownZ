using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;

    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    public Transform player;
    public float attackRange = 2.5f;
    public float chaseRange = 10f;
    public float loseRange = 20f;
    public float waitTime = 2f;
    public GameObject experiencePrefabs;
    public int countXP = 1;
    public float speed = 4.2f;

    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool isChasing = false;
    private bool isAttacked = false;
    private bool isDead = false;

    // Para no patrullar
    private enum CreepState { Idle, Sniffing, Eating }
    private CreepState creepState = CreepState.Idle;
    private bool isReturning = false;

    private Animator animator;

    private bool reproducido = false;

    void Start()
    {
        EnemyAlertSystem.allEnemies.Add(this);
        if (gameObject.name.Contains("Creep"))
        {
            animator = GetComponentInChildren<Animator>();
        }
        else
        {
            animator = GetComponent<Animator>();
        }

        agent = GetComponent<NavMeshAgent>();

        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        animator.SetFloat("distanceToPlayer", distance);

        if (isChasing && distance > loseRange && !isAttacked)
        {
            animator.SetBool("seePlayer", false);
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            agent.speed = 2.2f;
            isReturning = true;

            isChasing = false;
        }

        if (distance > chaseRange && !isChasing)
        {
            if (patrolPoints.Length == 1) NoPatrol();
            else PatrolBehavior();
        }
        else if (distance > attackRange && !isDead)
        {
            isChasing = true;

            // Correr hacia el jugador
            animator.SetBool("seePlayer", true);
            animator.SetBool("isWalking", false);
            animator.SetBool("isAttacking", false);

            Sonidos soundManager = GetComponent<Sonidos>();

            if (soundManager != null && !reproducido)
            {
                soundManager.ReproducirSonidoGhoul();
                reproducido = true;
            }

            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(player.position);
        }
        else
        {
            // Atacar
            animator.SetBool("isAttacking", true);
            animator.SetTrigger("attackAlt");
            agent.isStopped = true;
        }
    }

    private void PatrolBehavior()
    {
        // Patrullar y pararse
        animator.SetBool("seePlayer", false);
        animator.SetBool("isAttacking", false);

        if (isWaiting)
        {
            agent.isStopped = true;
            animator.SetBool("isWalking", false);
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitTime)
            {
                isWaiting = false;
                waitTimer = 0f;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                agent.isStopped = false;
                animator.SetBool("isWalking", true);
            }
        }
        else
        {
            agent.isStopped = false;
            animator.SetBool("isWalking", true);

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                isWaiting = true;
                waitTimer = 0f;
                agent.isStopped = true;
                animator.SetBool("isWalking", false);
            }
        }
    }

    private void NoPatrol()
    {
        agent.isStopped = true;
        animator.SetBool("isWalking", false);
        animator.SetBool("seePlayer", false);
        animator.SetBool("isAttacking", false);

        if (!isReturning)
        {
            waitTimer += Time.deltaTime;

            switch (creepState)
            {
                case CreepState.Idle:
                    if (waitTimer >= waitTime)
                    {
                        waitTimer = 0f;
                        creepState = CreepState.Sniffing;
                        animator.SetBool("isSniffing", true);
                        animator.SetBool("isEating", false);
                    }
                    break;

                case CreepState.Sniffing:
                    if (waitTimer >= waitTime + 2)
                    {
                        waitTimer = 0f;
                        creepState = CreepState.Eating;
                        animator.SetBool("isSniffing", false);
                        animator.SetBool("isEating", true);
                    }
                    break;

                case CreepState.Eating:
                    if (waitTimer >= waitTime + 2.4)
                    {
                        waitTimer = 0f;
                        creepState = CreepState.Idle;
                        animator.SetBool("isEating", false);
                        animator.SetBool("isSniffing", false);
                    }
                    break;
            }
        }
        else
        {
            agent.isStopped = false;
            animator.SetBool("isWalking", true);
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            if (agent.remainingDistance < 0.5f)
            {
                isReturning = false;
                creepState = CreepState.Idle;
            }
        }
    }

    public void Die()
    {
        isDead = true;

        animator.SetBool("isDead", true);
        animator.SetTrigger("die");
        agent.isStopped = true;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        EnemyAlertSystem.allEnemies.Remove(this);

        Invoke(nameof(DropExperience), 3f);

        return;
    }

    public void ForceChasePlayer(bool alertOthers = true)
    {
        if (isDead) return;

        isAttacked = true;
        isChasing = true;
        agent.isStopped = false;
        animator.SetBool("seePlayer", true);
        agent.speed = speed;
        agent.SetDestination(player.position);

        if (alertOthers)
        {
            float alertRadius = 15f;
            EnemyAlertSystem.AlertNearbyEnemies(transform.position, alertRadius);
        }
    }

    private void DropExperience()
    {
        for (int i = 0; i < countXP; i++)
        {
            Vector3 offset = Random.insideUnitCircle * 1.5f;
            offset.y = 0.5f;
            Vector3 spawnPosition = transform.position + offset;

            GameObject experience = Instantiate(
                experiencePrefabs,
                spawnPosition,
                Quaternion.identity
             );
        }
    }
    
    public bool IsDead()
    {
        return isDead;
    }
}