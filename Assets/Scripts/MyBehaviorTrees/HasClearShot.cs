
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Verifie qu'aucun obstacle ne bloque le tir.")]
public class HasClearShot : Conditional
{
    public SharedTransform target;

    public override TaskStatus OnUpdate()
    {
        return IsClear(transform, target.Value)
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }

    public static bool IsClear(Transform shooter, Transform enemy)
    {
        if (shooter == null || enemy == null)
            return false;

        Collider shooterCollider =
            shooter.GetComponentInChildren<Collider>();

        Collider enemyCollider =
            enemy.GetComponentInChildren<Collider>();

        Vector3 start = shooterCollider != null
            ? shooterCollider.bounds.center
            : shooter.position + Vector3.up;

        Vector3 end = enemyCollider != null
            ? enemyCollider.bounds.center
            : enemy.position + Vector3.up;

        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance < 0.01f)
            return false;

        RaycastHit[] hits = Physics.RaycastAll(
            start,
            direction.normalized,
            distance + 0.1f,
            ~0,
            QueryTriggerInteraction.Collide
        );

        float nearestEnemy = float.PositiveInfinity;
        float nearestObstacle = float.PositiveInfinity;

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;

            // Ignorer les colliders de notre drone.
            if (hitTransform.IsChildOf(shooter))
                continue;

            // Detecter le drone ennemi.
            if (hitTransform.IsChildOf(enemy))
            {
                nearestEnemy = Mathf.Min(
                    nearestEnemy, hit.distance
                );
                continue;
            }

            // Ignorer les detecteurs et les projectiles.
            if (hit.collider.isTrigger ||
                hit.collider.GetComponentInParent<Missile>() != null ||
                hit.collider.GetComponentInParent<Rocket>() != null)
                continue;

            nearestObstacle = Mathf.Min(
                nearestObstacle, hit.distance
            );
        }

        bool clear = nearestEnemy < nearestObstacle;

        Debug.DrawLine(
            start,
            end,
            clear ? Color.green : Color.red,
            0.2f
        );

        return clear;
    }
}
