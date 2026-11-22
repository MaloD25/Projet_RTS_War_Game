
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("V3 - Cherche le meilleur point d'explosion entre les drones ennemis.")]
public class SelectPredictedEnemyDrone : Action
{
    public SharedTransform target;

    private const float MinRange = 5f;
    private const float MaxRange = 50f;
    private const float ExplosionRadius = 5f;

    // Anticipation volontairement limitee.
    private const float PredictionTime = 1.5f;
    private const float MaxPredictionDistance = 7f;

    private Transform aimPoint;

    public override void OnAwake()
    {
        GameObject marker = new GameObject(
            "ArtilleryAim_" + gameObject.name
        );

        marker.transform.SetParent(transform);
        aimPoint = marker.transform;
    }

    public override TaskStatus OnUpdate()
    {
        if (TerrainManager.Instance == null)
            return TaskStatus.Failure;

        Drone[] allDrones = Object.FindObjectsByType<Drone>(
            FindObjectsSortMode.None
        );

        Vector3[] positions = new Vector3[allDrones.Length];
        float[] speeds = new float[allDrones.Length];
        float[] healths = new float[allDrones.Length];

        int count = 0;

        // ETAPE 1 : recuperer les drones verts
        foreach (Drone drone in allDrones)
        {
            if (drone == null ||
                drone.gameObject.CompareTag(gameObject.tag))
                continue;

            Vector3 position = drone.transform.position;
            Vector3 velocity = Vector3.zero;

            NavMeshAgent agent =
                drone.GetComponent<NavMeshAgent>();

            if (agent != null &&
                agent.enabled &&
                agent.isOnNavMesh)
            {
                velocity = agent.velocity;
                velocity.y = 0f;
            }

            // Petite anticipation avec distance maximale
            Vector3 offset = Vector3.ClampMagnitude(
                velocity * PredictionTime,
                MaxPredictionDistance
            );

            positions[count] = position + offset;
            speeds[count] = velocity.magnitude;
            healths[count] = drone.Health;

            count++;
        }

        if (count == 0)
            return TaskStatus.Failure;

        Vector3 bestPosition = Vector3.zero;
        float bestScore = float.NegativeInfinity;
        int bestHits = 0;

        // ETAPE 2 : tester plusieurs points de bombardement
        for (int i = 0; i < count; i++)
        {
            // Point sur le drone i
            EvaluatePosition(
                positions[i],
                positions,
                speeds,
                healths,
                count,
                ref bestPosition,
                ref bestScore,
                ref bestHits
            );

            // Points au milieu de deux drones
            for (int j = i + 1; j < count; j++)
            {
                if (Vector3.Distance(
                    positions[i],
                    positions[j]
                ) > ExplosionRadius * 2f)
                    continue;

                Vector3 midpoint =
                    (positions[i] + positions[j]) * 0.5f;

                EvaluatePosition(
                    midpoint,
                    positions,
                    speeds,
                    healths,
                    count,
                    ref bestPosition,
                    ref bestScore,
                    ref bestHits
                );
            }
        }

        if (bestHits == 0)
            return TaskStatus.Failure;

        // ETAPE 3 : transmettre le point aux Tasks de tir
        aimPoint.position = bestPosition;
        target.Value = aimPoint;

        Debug.Log(
            $"[ARTILLERIE V3] {gameObject.name} " +
            $"vise une zone avec {bestHits} drone(s)"
        );

        Debug.DrawLine(
            transform.position,
            bestPosition,
            Color.yellow,
            3f
        );

        return TaskStatus.Success;
    }

    private void EvaluatePosition(
        Vector3 candidate,
        Vector3[] positions,
        float[] speeds,
        float[] healths,
        int count,
        ref Vector3 bestPosition,
        ref float bestScore,
        ref int bestHits)
    {
        Vector3 ground = Vector3.zero;
        Vector3 normal = Vector3.zero;

        if (!TerrainManager.Instance
            .GetVerticallyAlignedPositionOnTerrain(
                candidate,
                ref ground,
                ref normal))
            return;

        float range = Vector3.Distance(
            transform.position,
            ground
        );

        if (range <= MinRange || range >= MaxRange)
            return;

        float score = 0f;
        int hits = 0;

        // ETAPE 4 : evaluer les degats potentiels
        for (int i = 0; i < count; i++)
        {
            Vector2 dronePos = new Vector2(
                positions[i].x,
                positions[i].z
            );

            Vector2 impactPos = new Vector2(
                ground.x,
                ground.z
            );

            float distance = Vector2.Distance(
                dronePos,
                impactPos
            );

            if (distance > ExplosionRadius)
                continue;

            hits++;

            // Chaque drone dans la zone rapporte des points
            score += 10f;

            // Bonus pour les drones affaiblis
            if (healths[i] <= 10f)
                score += 7f;
            else if (healths[i] <= 20f)
                score += 3f;

            // Bonus aux positions proches du centre
            score += (ExplosionRadius - distance) * 0.5f;

            // Penalite pour les drones rapides
            score -= speeds[i] * 0.5f;
        }

        // Legere preference pour les tirs proches
        score -= range * 0.02f;

        if (score > bestScore && hits > 0)
        {
            bestScore = score;
            bestPosition = ground;
            bestHits = hits;
        }
    }
}
