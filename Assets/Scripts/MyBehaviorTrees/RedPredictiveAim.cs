
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Anticipe le mouvement de la cible et oriente progressivement le drone rouge.")]
public class RedPredictiveAim : Action
{
    public SharedTransform target;

    public SharedFloat slerpCoef;
    public SharedFloat arrivalAngle;
    public SharedFloat maxDistance;
    public SharedFloat timeout;

    private float startTime;
    private NavMeshAgent shooterAgent;

    private const float MissileAcceleration = 40f;
    private const float MissileMaxSpeed = 20f;

    public override void OnAwake()
    {
        shooterAgent = GetComponent<NavMeshAgent>();
    }

    public override void OnStart()
    {
        startTime = Time.time;
    }

    public override TaskStatus OnUpdate()
    {
        if (target.Value == null)
            return TaskStatus.Failure;

        float distance = Vector3.Distance(
            transform.position,
            target.Value.position
        );

        if (distance > maxDistance.Value)
            return TaskStatus.Failure;

        if (Time.time - startTime > timeout.Value)
            return TaskStatus.Failure;

        // Calculer la position estimee de la cible
        // au moment ou le missile pourrait l'atteindre.
        Vector3 predictedPosition = PredictTargetPosition();

        // Conserver la rotation horizontale d'origine.
        Vector3 direction = predictedPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return TaskStatus.Failure;

        Quaternion desiredRotation =
            Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            Time.deltaTime * slerpCoef.Value
        );

        Debug.DrawLine(
            transform.position,
            predictedPosition,
            Color.cyan,
            0.1f
        );

        float angle = Quaternion.Angle(
            transform.rotation,
            desiredRotation
        );

        if (angle <= arrivalAngle.Value)
            return TaskStatus.Success;

        return TaskStatus.Running;
    }

    private Vector3 PredictTargetPosition()
    {
        Vector3 currentPosition = target.Value.position;

        NavMeshAgent enemyAgent =
            target.Value.GetComponent<NavMeshAgent>();

        // Une tourelle ou un drone immobile :
        // viser sa position actuelle.
        if (enemyAgent == null ||
            !enemyAgent.isActiveAndEnabled ||
            !enemyAgent.isOnNavMesh)
        {
            return currentPosition;
        }

        Vector3 velocity = enemyAgent.velocity;
        velocity.y = 0f;

        float speed = velocity.magnitude;

        if (speed < 0.1f)
            return currentPosition;

        float missileStartSpeed = 10f;

        if (shooterAgent != null &&
            shooterAgent.isActiveAndEnabled &&
            shooterAgent.isOnNavMesh)
        {
            missileStartSpeed = shooterAgent.speed;
        }

        float distance = Vector3.Distance(
            transform.position,
            currentPosition
        );

        float flightTime = CalculateMissileFlightTime(
            distance,
            missileStartSpeed
        );

        float predictedTravel = speed * flightTime;

        // Eviter de predire un deplacement
        // au-dela de la destination actuelle.
        if (!enemyAgent.pathPending &&
            !float.IsInfinity(enemyAgent.remainingDistance))
        {
            float remaining = Mathf.Max(
                0f,
                enemyAgent.remainingDistance -
                enemyAgent.stoppingDistance
            );

            predictedTravel = Mathf.Min(
                predictedTravel,
                remaining
            );
        }

        return currentPosition +
            velocity.normalized * predictedTravel;
    }

    private float CalculateMissileFlightTime(
        float distance,
        float startSpeed)
    {
        float v0 = Mathf.Clamp(
            startSpeed,
            0f,
            MissileMaxSpeed
        );

        float accelerationTime =
            (MissileMaxSpeed - v0) / MissileAcceleration;

        float accelerationDistance =
            (v0 + MissileMaxSpeed) *
            accelerationTime * 0.5f;

        if (distance <= accelerationDistance)
        {
            return (
                -v0 +
                Mathf.Sqrt(
                    v0 * v0 +
                    2f * MissileAcceleration * distance
                )
            ) / MissileAcceleration;
        }

        return accelerationTime +
            (distance - accelerationDistance) /
            MissileMaxSpeed;
    }
}
