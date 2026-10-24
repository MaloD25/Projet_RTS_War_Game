using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Red Army")]
public class StepAwayFromAlly : Action
{
    public float stepDistance = 4f;
    public float maxDuration = 1f;

    private NavMeshAgent agent;
    private float endTime;
    private bool moving;

    public override void OnStart()
    {
        moving = false;
        agent = GetComponent<NavMeshAgent>();

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        NavMeshAgent closest = null;
        float bestDistance = float.MaxValue;

        var agents = Object.FindObjectsByType<NavMeshAgent>(
            FindObjectsSortMode.None);

        foreach (var other in agents)
        {
            if (other == null || other == agent ||
                !other.CompareTag(gameObject.tag))
                continue;

            float distance = (other.transform.position -
                              transform.position).sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                closest = other;
            }
        }

        if (closest == null)
            return;

        Vector3 away = transform.position - closest.transform.position;
        away.y = 0f;

        if (away.sqrMagnitude < 0.01f)
            away = transform.right;

        Vector3 destination =
            transform.position + away.normalized * stepDistance;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit,
            2f, agent.areaMask))
        {
            moving = agent.SetDestination(hit.position);
            endTime = Time.time + maxDuration;
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (!moving)
            return TaskStatus.Failure;

        if (Time.time >= endTime)
            return TaskStatus.Success;

        if (!agent.pathPending &&
            agent.remainingDistance <= Mathf.Max(0.5f, agent.stoppingDistance))
            return TaskStatus.Success;

        return TaskStatus.Running;
    }

    public override void OnEnd()
    {
        if (moving && agent != null && agent.isOnNavMesh)
            agent.ResetPath();
    }
}