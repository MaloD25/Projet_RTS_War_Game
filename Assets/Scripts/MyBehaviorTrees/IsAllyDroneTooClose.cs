
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Red Army")]
public class IsAllyDroneTooClose : Conditional
{
    public float triggerDistance = 5.5f;
    public float cooldown = 5f;

    private float nextCheckTime = 0f;

    public override TaskStatus OnUpdate()
    {
        if (Time.time < nextCheckTime)
            return TaskStatus.Failure;

        NavMeshAgent self = GetComponent<NavMeshAgent>();

        if (self == null)
            return TaskStatus.Failure;

        var agents = Object.FindObjectsByType<NavMeshAgent>(
            FindObjectsSortMode.None);

        foreach (var other in agents)
        {
            if (other == null || other == self)
                continue;

            if (!other.CompareTag(gameObject.tag))
                continue;

            Vector3 offset =
                other.transform.position - transform.position;

            offset.y = 0f;

            if (offset.sqrMagnitude <
                triggerDistance * triggerDistance)
            {
                nextCheckTime = Time.time + cooldown;
                return TaskStatus.Success;
            }
        }

        return TaskStatus.Failure;
    }
}
