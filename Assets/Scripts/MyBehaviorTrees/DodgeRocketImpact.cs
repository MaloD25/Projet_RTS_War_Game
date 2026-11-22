
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Esquive les explosions ennemies sur le NavMesh.")]
public class DodgeRocketImpact : Action
{
    public SharedVector3 impactPoint;

    private const float SafeRadius = 8f;
    private const float AllySpacing = 7f;
    private const float MaxDodgeTime = 3f;

    private NavMeshAgent agent;

    private bool moving;
    private bool changedStoppingDistance;

    private Vector3 dodgeDestination;
    private float dodgeStartTime;
    private float previousStoppingDistance;

    public override void OnAwake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnStart()
    {
        moving = false;
        changedStoppingDistance = false;
        dodgeStartTime = Time.time;

        if (!CanMove())
            return;

        // Pendant l'esquive, le drone doit atteindre
        // sa destination et non s'arreter 6 m avant.
        previousStoppingDistance = agent.stoppingDistance;
        agent.stoppingDistance = 0.3f;
        changedStoppingDistance = true;

        List<IsRocketImpactImminent.RocketForecast> rockets =
            IsRocketImpactImminent.GetEnemyForecasts(gameObject.tag);

        if (rockets.Count == 0)
            return;

        Vector3 away = transform.position - impactPoint.Value;
        away.y = 0f;

        if (away.sqrMagnitude < 0.01f)
            away = transform.right;

        away.Normalize();

        Drone[] drones = Object.FindObjectsByType<Drone>(
            FindObjectsSortMode.None
        );

        Vector3 bestPosition = Vector3.zero;
        float bestScore = float.NegativeInfinity;
        bool found = false;

        float[] distances = { 5f, 8f, 11f, 14f };

        // Rechercher plusieurs trajectoires d'esquive.
        for (int angle = 0; angle < 360; angle += 30)
        {
            Vector3 direction =
                Quaternion.Euler(0f, angle, 0f) * away;

            foreach (float distance in distances)
            {
                Vector3 candidate =
                    transform.position + direction * distance;

                if (!NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    1.5f,
                    agent.areaMask))
                    continue;

                if (HorizontalDistance(
                    transform.position,
                    hit.position
                ) < 2f)
                    continue;

                NavMeshPath path = new NavMeshPath();

                if (!agent.CalculatePath(hit.position, path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                    continue;

                float pathLength = 0f;

                for (int i = 1; i < path.corners.Length; i++)
                {
                    pathLength += Vector3.Distance(
                        path.corners[i - 1],
                        path.corners[i]
                    );
                }

                float closestExplosion = 1000f;
                float dangerPenalty = 0f;

                // Evaluer toutes les grenades ennemies.
                foreach (var rocket in rockets)
                {
                    if (rocket.seconds <= 0f ||
                        rocket.seconds > 4f)
                        continue;

                    float clearance = HorizontalDistance(
                        hit.position,
                        rocket.impact
                    );

                    closestExplosion = Mathf.Min(
                        closestExplosion,
                        clearance
                    );

                    if (clearance < SafeRadius)
                    {
                        dangerPenalty +=
                            (SafeRadius - clearance) * 30f;
                    }

                    float currentDistance = HorizontalDistance(
                        transform.position,
                        rocket.impact
                    );

                    if (currentDistance < SafeRadius)
                    {
                        float estimatedTravelTime =
                            pathLength / Mathf.Max(agent.speed, 0.1f)
                            + 0.4f;

                        if (estimatedTravelTime > rocket.seconds)
                            dangerPenalty += 15f;
                    }
                }

                // Eviter les regroupements de drones rouges.
                float allyPenalty = 0f;

                foreach (Drone ally in drones)
                {
                    if (ally == null ||
                        ally.gameObject == gameObject ||
                        !ally.gameObject.CompareTag(gameObject.tag))
                        continue;

                    float separation = HorizontalDistance(
                        hit.position,
                        ally.transform.position
                    );

                    if (separation < AllySpacing)
                    {
                        allyPenalty +=
                            (AllySpacing - separation) * 3f;
                    }
                }

                float score =
                    Mathf.Min(closestExplosion, 20f) * 2f
                    - pathLength
                    - dangerPenalty
                    - allyPenalty;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = hit.position;
                    found = true;
                }
            }
        }

        if (!found)
        {
            Debug.LogWarning(
                $"[ESQUIVE] {gameObject.name} : " +
                "aucun chemin disponible."
            );

            return;
        }

        dodgeDestination = bestPosition;

        agent.isStopped = false;
        moving = agent.SetDestination(dodgeDestination);

        if (moving)
        {
            Debug.Log(
                $"[ESQUIVE DEBUT] {gameObject.name} " +
                $"vers {dodgeDestination}"
            );

            Debug.DrawLine(
                transform.position,
                dodgeDestination,
                Color.cyan,
                3f
            );
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (!moving || !CanMove())
            return TaskStatus.Failure;

        // Eviter un blocage indefini dans cette Task.
        if (Time.time - dodgeStartTime > MaxDodgeTime)
        {
            Debug.LogWarning(
                $"[ESQUIVE TIMEOUT] {gameObject.name}"
            );

            return TaskStatus.Failure;
        }

        // Laisser Unity calculer le chemin.
        if (agent.pathPending)
            return TaskStatus.Running;

        // Verifier la distance REELLE a la destination.
        float distance = HorizontalDistance(
            transform.position,
            dodgeDestination
        );

        if (distance <= 0.8f)
        {
            Debug.Log(
                $"[ESQUIVE TERMINEE] {gameObject.name}"
            );

            return TaskStatus.Success;
        }

        // Un chemin perdu ne doit pas bloquer le drone.
        if (Time.time - dodgeStartTime > 0.6f &&
            !agent.hasPath)
        {
            Debug.LogWarning(
                $"[ESQUIVE SANS CHEMIN] {gameObject.name}"
            );

            return TaskStatus.Failure;
        }

        return TaskStatus.Running;
    }


    public override void OnEnd()
    {
        // Un drone detruit peut ne plus avoir
        // de NavMeshAgent actif ou valide.
        if (agent != null &&
            agent.isActiveAndEnabled &&
            agent.isOnNavMesh)
        {
            agent.ResetPath();

            if (changedStoppingDistance)
            {
                agent.stoppingDistance =
                    previousStoppingDistance;
            }
        }

        moving = false;
        changedStoppingDistance = false;
    }


    private bool CanMove()
    {
        return agent != null &&
               agent.enabled &&
               agent.isOnNavMesh;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }
}
