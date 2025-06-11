using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float attackRange = 2.5f;
    public float chaseRange = 10f;

    private Animator animator;
    private bool isDead = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDead)
        {
            animator.SetBool("isDead", true);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        animator.SetFloat("distanceToPlayer", distance);

        if (distance > chaseRange)
        {
            // Patrullar
            animator.SetBool("seePlayer", false);
            animator.SetBool("isWalking", true);
            animator.SetBool("isAttacking", false);
        }
        else if (distance > attackRange)
        {
            // Correr hacia el jugador
            animator.SetBool("seePlayer", true);
            animator.SetBool("isWalking", false);
            animator.SetBool("isAttacking", false);
        }
        else
        {
            // Atacar
            animator.SetBool("isAttacking", true);
            animator.SetTrigger("attackAlt");
        }
    }

    public void Die()
    {
        isDead = true;
    }
}
