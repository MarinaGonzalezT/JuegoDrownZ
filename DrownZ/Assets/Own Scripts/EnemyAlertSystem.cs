using System.Collections.Generic;
using UnityEngine;
public static class EnemyAlertSystem
{
    public static List<EnemyAI> allEnemies = new List<EnemyAI>();

    public static void AlertNearbyEnemies(Vector3 playerPosition, float alertRadius)
    {
        foreach (EnemyAI enemy in allEnemies)
        {
            if (enemy == null || enemy.IsDead()) continue;

            float distanceToPlayer = Vector3.Distance(enemy.transform.position, playerPosition);
            if (distanceToPlayer <= alertRadius)
            {
                enemy.ForceChasePlayer(false); // sin alertar en cadena
            }
        }
    }
}
