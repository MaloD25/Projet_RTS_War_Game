
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Detecte les impacts imminents des grenades ennemies.")]
public class IsRocketImpactImminent : Conditional
{
    public SharedVector3 impactPoint = new SharedVector3();

    private const float RocketTravelTime = 4f;
    private const float WarningTime = 3.5f;
    private const float DangerRadius = 8f;

    private static readonly Dictionary<Rocket, float> firstSeen =
        new Dictionary<Rocket, float>();

    public struct RocketForecast
    {
        public Vector3 impact;
        public float seconds;

        public RocketForecast(Vector3 position, float time)
        {
            impact = position;
            seconds = time;
        }
    }

    // Accessible egalement au script d'esquive.
    public static List<RocketForecast> GetEnemyForecasts(
        string ownTag)
    {
        List<RocketForecast> forecasts =
            new List<RocketForecast>();

        Rocket[] rockets = Object.FindObjectsByType<Rocket>(
            FindObjectsSortMode.None
        );

        foreach (Rocket rocket in rockets)
        {
            if (rocket == null ||
                rocket.gameObject.CompareTag(ownTag))
                continue;

            if (!firstSeen.TryGetValue(rocket, out float start))
            {
                start = Time.time;
                firstSeen[rocket] = start;
            }

            float elapsed = Time.time - start;
            float remaining = RocketTravelTime - elapsed;

            // Attendre que la vitesse balistique soit initialisee.
            if (elapsed < Time.fixedDeltaTime * 2f ||
                remaining <= 0f)
                continue;

            Rigidbody rb = rocket.GetComponent<Rigidbody>();

            if (rb == null)
                continue;

            Vector3 predictedImpact =
                rb.position
                + rb.linearVelocity * remaining
                + 0.5f * Physics.gravity
                * remaining * remaining;

            forecasts.Add(
                new RocketForecast(predictedImpact, remaining)
            );
        }

        // Nettoyer les grenades detruites.
        if (Time.frameCount % 120 == 0)
        {
            List<Rocket> expired = new List<Rocket>();

            foreach (var entry in firstSeen)
            {
                if (entry.Key == null)
                    expired.Add(entry.Key);
            }

            foreach (Rocket expiredRocket in expired)
                firstSeen.Remove(expiredRocket);
        }

        return forecasts;
    }

    public override TaskStatus OnUpdate()
    {
        List<RocketForecast> forecasts =
            GetEnemyForecasts(gameObject.tag);

        NavMeshAgent agent = GetComponent<NavMeshAgent>();

        float earliestDanger = float.MaxValue;
        bool dangerFound = false;
        Vector3 dangerPosition = Vector3.zero;

        foreach (RocketForecast forecast in forecasts)
        {
            if (forecast.seconds > WarningTime)
                continue;

            Vector3 current = transform.position;
            Vector3 future = current;

            // Petite anticipation du deplacement du drone.
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                future += agent.velocity *
                    Mathf.Min(forecast.seconds, 1f);
            }

            float currentDistance = HorizontalDistance(
                current, forecast.impact
            );

            float futureDistance = HorizontalDistance(
                future, forecast.impact
            );

            if (Mathf.Min(currentDistance, futureDistance)
                <= DangerRadius &&
                forecast.seconds < earliestDanger)
            {
                earliestDanger = forecast.seconds;
                dangerPosition = forecast.impact;
                dangerFound = true;
            }
        }

        if (!dangerFound)
            return TaskStatus.Failure;

        impactPoint.Value = dangerPosition;

        Debug.DrawLine(
            transform.position,
            dangerPosition,
            Color.yellow
        );

        return TaskStatus.Success;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }
}
