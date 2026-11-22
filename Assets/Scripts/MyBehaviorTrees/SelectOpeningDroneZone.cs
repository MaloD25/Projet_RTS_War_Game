
using System.Linq;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Gere les deux phases de tir des drones rouges pendant les 10 premieres secondes.")]
public class SelectOpeningDroneZone : Conditional
{
    public SharedTransform target;

    private const float Phase2Start = 5f;
    private const float OpeningDuration = 5f;

    private Transform zone1;
    private Transform zone2;
    private Transform zone3;

    private Transform assignedZone;
    private Transform lastSelectedZone;

    private bool initialized = false;

    public override TaskStatus OnUpdate()
    {
        // Fin de l'ouverture a 10 secondes.
        if (Timer.Instance == null ||
            Timer.Value >= OpeningDuration)
        {
            return TaskStatus.Failure;
        }

        // Initialiser les zones une seule fois.
        if (!initialized)
        {
            GameObject obj1 =
                GameObject.Find("ZoneTirDroneBas1");

            GameObject obj2 =
                GameObject.Find("ZoneTirDroneBas2");

            GameObject obj3 =
                GameObject.Find("ZoneTirDroneBas3");

            if (obj1 == null || obj2 == null || obj3 == null)
            {
                Debug.LogWarning(
                    "[OUVERTURE DRONE] Une des trois zones est introuvable."
                );

                return TaskStatus.Failure;
            }

            zone1 = obj1.transform;
            zone2 = obj2.transform;
            zone3 = obj3.transform;

            // Recuperer les drones de notre equipe.
            Drone[] allies =
                Object.FindObjectsByType<Drone>(
                    FindObjectsSortMode.None
                )
                .Where(d =>
                    d != null &&
                    d.gameObject.CompareTag(gameObject.tag))
                .OrderBy(d =>
                    Vector3.Distance(
                        d.transform.position,
                        zone1.position
                    ) -
                    Vector3.Distance(
                        d.transform.position,
                        zone2.position
                    ))
                .ThenBy(d => d.gameObject.name)
                .ToArray();

            int myIndex = -1;

            for (int i = 0; i < allies.Length; i++)
            {
                if (allies[i].gameObject == gameObject)
                {
                    myIndex = i;
                    break;
                }
            }

            if (myIndex == -1)
                return TaskStatus.Failure;

            // Avec 13 drones : 6 dans le groupe 1,
            // 7 dans le groupe 2.
            int group1Count = allies.Length / 2;

            assignedZone = myIndex < group1Count
                ? zone1
                : zone2;

            initialized = true;
        }

        Transform selectedZone;

        if (Timer.Value < Phase2Start)
        {
            // PHASE 1 : deux groupes.
            selectedZone = assignedZone;
        }
        else
        {
            // PHASE 2 : tous les drones sur zone 3.
            selectedZone = zone3;
        }

        if (selectedZone == null)
            return TaskStatus.Failure;

        target.Value = selectedZone;

        // Afficher un message seulement
        // lorsqu'on change de zone.
        if (lastSelectedZone != selectedZone)
        {
            Debug.Log(
                $"[OUVERTURE DRONE] {gameObject.name} " +
                $"vise maintenant {selectedZone.name} " +
                $"a {Timer.Value:F1}s"
            );

            lastSelectedZone = selectedZone;
        }

        return TaskStatus.Success;
    }
}
