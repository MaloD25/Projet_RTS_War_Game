using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Linq;

[TaskCategory("MyTasks")]
[TaskDescription("Select the enemy turret with the lowest health")]
public class SelectWeakestEnemyTurret : Action
{
    IArmyElement m_ArmyElement;

    public SharedTransform target;
    public SharedFloat minRadius;
    public SharedFloat maxRadius;

    public override void OnAwake()
    {
        m_ArmyElement =
            (IArmyElement)GetComponent(typeof(IArmyElement));
    }

    public override TaskStatus OnUpdate()
    {
        if (m_ArmyElement.ArmyManager == null)
            return TaskStatus.Running;

        Turret[] allTurrets =
            GameObject.FindObjectsOfType<Turret>();

        Turret weakestTurret =
            allTurrets
            .Where(turret =>
                turret.gameObject.tag != gameObject.tag &&
                Vector3.Distance(
                    transform.position,
                    turret.transform.position
                ) > minRadius.Value &&
                Vector3.Distance(
                    transform.position,
                    turret.transform.position
                ) < maxRadius.Value
            )
            .OrderBy(turret => turret.Health)
            .FirstOrDefault();

        if (weakestTurret == null)
            return TaskStatus.Failure;

        target.Value =
            weakestTurret.transform;

        return TaskStatus.Success;
    }
}