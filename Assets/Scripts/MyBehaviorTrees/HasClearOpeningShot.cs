
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Verifie la trajectoire des missiles en suivant le relief.")]
public class HasClearOpeningShot : Conditional
{
    public SharedTransform target;

    private const float MaxUsefulRange = 55f;
    private const float SampleStep = 1.5f;

    public override TaskStatus OnUpdate()
    {
        if (target.Value == null ||
            TerrainManager.Instance == null)
            return TaskStatus.Failure;

        Transform[] children =
            gameObject.GetComponentsInChildren<Transform>(true);

        bool foundSpawn = false;

        foreach (Transform child in children)
        {
            if (!child.name.StartsWith("MissileSpawn"))
                continue;

            foundSpawn = true;

            if (!IsTrajectoryClear(
                child.position,
                target.Value.position))
            {
                return TaskStatus.Failure;
            }
        }

        return foundSpawn
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }

    private bool IsTrajectoryClear(
        Vector3 origin,
        Vector3 destination)
    {
        Vector3 groundStart = Vector3.zero;
        Vector3 normal = Vector3.zero;

        if (!TerrainManager.Instance
            .GetVerticallyAlignedPositionOnTerrain(
                origin,
                ref groundStart,
                ref normal))
            return false;

        // Hauteur du missile au-dessus du sol.
        float missileHeight =
            origin.y - groundStart.y;

        // Le missile avance dans la direction X/Z.
        Vector3 direction = destination - origin;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance < 0.1f ||
            distance > MaxUsefulRange)
            return false;

        int steps = Mathf.CeilToInt(
            distance / SampleStep
        );

        Vector3 previousPoint = origin;

        for (int i = 1; i <= steps; i++)
        {
            float fraction = (float)i / steps;

            Vector3 horizontalPoint =
                origin + direction * fraction;

            Vector3 ground = Vector3.zero;
            Vector3 terrainNormal = Vector3.zero;

            if (!TerrainManager.Instance
                .GetVerticallyAlignedPositionOnTerrain(
                    horizontalPoint,
                    ref ground,
                    ref terrainNormal))
                return false;

            // Estimation de la trajectoire du missile
            // a hauteur constante au-dessus du terrain.
            Vector3 nextPoint = new Vector3(
                horizontalPoint.x,
                ground.y + missileHeight,
                horizontalPoint.z
            );

            Vector3 segment = nextPoint - previousPoint;
            float segmentLength = segment.magnitude;

            if (segmentLength > 0.01f)
            {
                RaycastHit[] hits = Physics.RaycastAll(
                    previousPoint,
                    segment.normalized,
                    segmentLength,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );

                foreach (RaycastHit hit in hits)
                {
                    Transform hitTransform =
                        hit.collider.transform;

                    // Ignorer notre propre drone.
                    if (hitTransform.IsChildOf(transform))
                        continue;

                    // Ignorer les unites :
                    // elles sont des cibles potentielles.
                    if (hit.collider
                        .GetComponentInParent<ArmyElement>()
                        != null)
                        continue;

                    // Ignorer les projectiles en vol.
                    if (hit.collider
                        .GetComponentInParent<Missile>()
                        != null ||
                        hit.collider
                        .GetComponentInParent<Rocket>()
                        != null)
                        continue;

                    // Un obstacle bloque la trajectoire.
                    Debug.Log(
                        $"[TIR BLOQUE] {gameObject.name} : " +
                        $"obstacle {hit.collider.name}"
                    );

                    Debug.DrawLine(
                        previousPoint,
                        nextPoint,
                        Color.red,
                        1f
                    );

                    return false;
                }
            }

            Debug.DrawLine(
                previousPoint,
                nextPoint,
                Color.green,
                1f
            );

            previousPoint = nextPoint;
        }

        return true;
    }
}
