using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
public class ReturnToRepairCamp : Action
{
    public float retreatThreshold = 0.4f;
    public float resumeThreshold = 0.9f;

    private Health health;
    private NavMeshAgent agent;

    private bool returning;
    private float previousStoppingDistance;

    public override void OnAwake()
    {
        health = gameObject.GetComponentInChildren<Health>();
        agent = gameObject.GetComponent<NavMeshAgent>();
    }

    public override void OnStart()
    {
        returning = false;

        if (health == null || agent == null ||
            !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        // Ne pas rentrer au camp si le drone est encore
        // suffisamment en bonne santé.
        if (health.Value <= 0f)
            return;

        GameObject camp = GameObject.Find("ZoneReparationRouge");

        if (camp == null)
            return;

        if (!NavMesh.SamplePosition(
            camp.transform.position,
            out NavMeshHit hit,
            5f,
            agent.areaMask))
            return;

        previousStoppingDistance = agent.stoppingDistance;
        agent.stoppingDistance = 0.2f;
        agent.isStopped = false;

        returning = agent.SetDestination(hit.position);

        if (!returning)
        {
            agent.stoppingDistance = previousStoppingDistance;
            return;
        }

        Debug.Log($"[REPARATION] {gameObject.name} retourne au camp");
    }

    public override TaskStatus OnUpdate()
    {
        if (!returning || health == null ||
            agent == null || !agent.isActiveAndEnabled ||
            !agent.isOnNavMesh)
            return TaskStatus.Failure;

        if (health.Value >= health.MaxValue * resumeThreshold)
        {
            Debug.Log($"[REPARATION] {gameObject.name} repart au combat");
            return TaskStatus.Success;
        }

        // Tant que le drone n'est pas suffisamment soigne,
        // cette action reste active.
        return TaskStatus.Running;
    }

    public override void OnEnd()
    {
        if (returning && agent != null &&
            agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.stoppingDistance = previousStoppingDistance;
        }

        returning = false;
    }
}