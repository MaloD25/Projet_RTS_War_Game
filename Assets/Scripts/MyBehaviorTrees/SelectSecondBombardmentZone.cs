
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Selectionne la zone du deuxieme bombardement.")]
public class SelectSecondBombardmentZone : Action
{
    public SharedTransform target;

    private bool secondShotAttempted = false;

    public override TaskStatus OnUpdate()
    {
        // Chaque tourelle ne tente ce tir qu'une seule fois.
        if (secondShotAttempted)
            return TaskStatus.Failure;

        secondShotAttempted = true;

        GameObject zone = GameObject.Find("ZoneBombardement2");

        if (zone == null)
        {
            Debug.LogWarning(
                "ZoneBombardement2 introuvable !"
            );
            return TaskStatus.Failure;
        }

        float distance = Vector3.Distance(
            transform.position,
            zone.transform.position
        );

        // Portee autorisee des tourelles : 5 a 50 metres.
        if (distance <= 5f || distance >= 50f)
        {
            Debug.LogWarning(
                $"{gameObject.name} : deuxieme zone hors de portee ({distance:F1} m)"
            );

            return TaskStatus.Failure;
        }

        target.Value = zone.transform;

        Debug.Log(
            $"[BOMBARDEMENT 2] {gameObject.name} " +
            $"prepare son deuxieme tir a t={Time.time:F1}s"
        );

        return TaskStatus.Success;
    }
}
