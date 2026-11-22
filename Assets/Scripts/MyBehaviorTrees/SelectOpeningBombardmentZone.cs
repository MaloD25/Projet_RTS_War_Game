
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Selectionne la zone de bombardement pour le premier tir.")]
public class SelectOpeningBombardmentZone : Action
{
    public SharedTransform target;

    private bool openingAttempted = false;

    public override TaskStatus OnUpdate()
    {
        // Une seule tentative de bombardement par tourelle
        if (openingAttempted)
            return TaskStatus.Failure;

        openingAttempted = true;

        GameObject zone = GameObject.Find("ZoneBombardement");

        if (zone == null)
        {
            Debug.LogWarning("ZoneBombardement introuvable !");
            return TaskStatus.Failure;
        }

        float distance = Vector3.Distance(
            transform.position,
            zone.transform.position
        );

        // Respect de la portee originale des tourelles
        if (distance <= 5f || distance >= 50f)
        {
            Debug.Log(
                $"{gameObject.name} : zone hors de portee ({distance:F1} m)"
            );
            return TaskStatus.Failure;
        }

        target.Value = zone.transform;

        Debug.Log(
            $"{gameObject.name} prepare son bombardement d'ouverture !"
        );

        return TaskStatus.Success;
    }
}
