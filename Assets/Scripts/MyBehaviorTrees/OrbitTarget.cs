

using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Red Army")]
public class OrbitTarget : Action
{
    public SharedTransform target;

    public float orbitRadius = 8f;
    public float angularSpeed = 45f;
    public float updateInterval = 0.3f;

    private NavMeshAgent agent;
    private float nextUpdate;
    private float direction;

    private float oldSpeed;
    private float oldStoppingDistance;
    private bool oldAutoBraking;
    private bool oldUpdateRotation;

    public override void OnStart()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null || target.Value == null)
            return;

        oldSpeed = agent.speed;
        oldStoppingDistance = agent.stoppingDistance;
        oldAutoBraking = agent.autoBraking;
        oldUpdateRotation = agent.updateRotation;

        // Valeur de déplacement imposée par le sujet
        agent.speed = 10f;
        agent.stoppingDistance = 0.2f;
        agent.autoBraking = false;
        agent.updateRotation = false;
        agent.isStopped = false;

        direction = Random.value > 0.5f ? 1f : -1f;
        nextUpdate = 0f;

        Debug.Log("ORBIT START : " + gameObject.name);
    }

    public override TaskStatus OnUpdate()
    {
        if (target.Value == null ||
            agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
        {
            return TaskStatus.Failure;
        }

        if (Time.time < nextUpdate)
            return TaskStatus.Running;

        nextUpdate = Time.time + updateInterval;

        Vector3 center = target.Value.position;
        Vector3 offset = transform.position - center;
        offset.y = 0f;

        float distance = offset.magnitude;
        Vector3 desiredPosition;

        // Phase 1 : avancer vers la cible
        if (distance > orbitRadius + 2f)
        {
            desiredPosition = center;
        }
        // Phase 2 : tourner autour de la cible
        else
        {
            float angle = Mathf.Atan2(offset.z, offset.x);

            // Anticiper suffisamment pour créer
            // un déplacement réel sur le NavMesh
            float lookAheadAngle = 40f * Mathf.Deg2Rad;

            angle += direction * lookAheadAngle;

            desiredPosition = center +
                new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle)
                ) * orbitRadius;
        }

        if (NavMesh.SamplePosition(
            desiredPosition,
            out NavMeshHit hit,
            3f,
            agent.areaMask))
        {
            agent.SetDestination(hit.position);
        }

        return TaskStatus.Running;
    }

    public override void OnEnd()
    {
        if (agent == null || !agent.enabled)
            return;

        agent.speed = oldSpeed;
        agent.stoppingDistance = oldStoppingDistance;
        agent.autoBraking = oldAutoBraking;
        agent.updateRotation = oldUpdateRotation;
    }
}
