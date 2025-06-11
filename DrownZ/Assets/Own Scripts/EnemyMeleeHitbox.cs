using UnityEngine;

public class EnemyMeleeHitbox : MonoBehaviour
{
    public float damageAmount = 25f;
    private bool hasHit = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        if (other.CompareTag("Player") && other.TryGetComponent<cowsins.PlayerStats>(out var player))
        {
            player.Damage(damageAmount, false);
            hasHit = true;
        }
        ResetHit();
    }

    public void ResetHit() => hasHit = false;
}
