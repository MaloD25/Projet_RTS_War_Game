
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Suit une cible mobile et cherche une ligne de tir degagee.")]
public class ApproachVisibleTarget : Action
{
    public SharedTransform target;

    // Distance tactique choisie pour les premiers tests.
    public float firingDistance = 25f;

    public float updateInterval = 0.25f;
    public float maxChaseTime = 12f;

    private NavMeshAgent agent;

    private float previousStoppingDistance;
    private float startTime;
    private float nextUpdateTime;
    private float nextProgressCheck;

    private Vector3 lastPosition;

    public override void OnAwake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnStart()
    {
        startTime = Time.time;
        nextUpdateTime = 0f;
        nextProgressCheck = Time.time + 3f;
        lastPosition = transform.position;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        previousStoppingDistance = agent.stoppingDistance;

        // Distance de navigation, pas vitesse de deplacement.
        agent.stoppingDistance = 0.5f;
        agent.isStopped = false;
    }

    public override TaskStatus OnUpdate()
    {
        if (target.Value == null ||
            agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
            return TaskStatus.Failure;

        float distance = Vector3.Distance(
            transform.position,
            target.Value.position
        );

        // Assez proche et aucun obstacle :
        // on peut s'arreter pour viser.
        if (distance <= firingDistance &&
            HasClearShot.IsClear(transform, target.Value))
        {
            return TaskStatus.Success;
        }

        // Eviter les poursuites infinies.
        if (Time.time - startTime > maxChaseTime)
            return TaskStatus.Failure;

        // Actualiser regulierement la destination.
        if (Time.time >= nextUpdateTime)
        {
            if (!agent.SetDestination(target.Value.position))
                return TaskStatus.Failure;

            nextUpdateTime = Time.time + updateInterval;
        }

        // Si le drone est bloque, abandonner cette poursuite.
        if (Time.time >= nextProgressCheck)
        {
            float progress = Vector3.Distance(
                transform.position,
                lastPosition
            );

            if (progress < 0.5f)
                return TaskStatus.Failure;

            lastPosition = transform.position;
            nextProgressCheck = Time.time + 3f;
        }

        if (!agent.pathPending &&
            agent.hasPath &&
            agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            return TaskStatus.Failure;
        }

        return TaskStatus.Running;
    }

    public override void OnEnd()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.ResetPath();
        agent.stoppingDistance = previousStoppingDistance;
    }
}
