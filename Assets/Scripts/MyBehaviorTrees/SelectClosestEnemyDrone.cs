
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Red Army")]
public class SelectClosestEnemyDrone : Action
{
    public SharedTransform target;
    public SharedFloat minRadius;
    public SharedFloat maxRadius;

    public override TaskStatus OnUpdate()
    {
        var drones = Object.FindObjectsByType<Drone>(
            FindObjectsSortMode.None);

        Drone closest = null;
        float bestDistance = float.MaxValue;

        foreach (Drone drone in drones)
        {
            if (drone == null ||
                drone.CompareTag(gameObject.tag))
                continue;

            float distance = Vector3.Distance(
                transform.position,
                drone.transform.position
            );

            if (distance <= minRadius.Value ||
                distance >= maxRadius.Value)
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                closest = drone;
            }
        }

        target.Value = closest != null
            ? closest.transform
            : null;

        return closest != null
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }
}
