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

    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool isChasing = false;

    private Animator animator;
    private bool isDead = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
        if (isDead)
        {
            animator.SetBool("isDead", true);
            agent.isStopped = true;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        animator.SetFloat("distanceToPlayer", distance);

        if (isChasing && distance > loseRange)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            agent.speed = 2.2f;

            isChasing = false;
        } 
        else
        {
            if (distance > chaseRange)
            {
                PatrolBehavior();
            }
            else if (distance > attackRange)
            {
                isChasing = true;

                // Correr hacia el jugador
                animator.SetBool("seePlayer", true);
                animator.SetBool("isWalking", false);
                animator.SetBool("isAttacking", false);

                agent.isStopped = false;
                agent.speed = 4.2f;
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
    public void Die()
    {
        isDead = true;
        agent.isStopped = true;
    }
}
