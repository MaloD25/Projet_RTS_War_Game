using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Returns Success if an enemy drone is nearby")]
public class IsEnemyDroneNearby : Conditional
{
    public SharedFloat radius = 10f;

    IArmyElement m_ArmyElement;

    public override void OnAwake()
    {
        m_ArmyElement =
            (IArmyElement)GetComponent(typeof(IArmyElement));
    }

    public override TaskStatus OnUpdate()
    {
        if (m_ArmyElement.ArmyManager == null)
            return TaskStatus.Failure;

        GameObject enemy =
            m_ArmyElement.ArmyManager.GetRandomEnemy<Drone>(
                transform.position,
                0f,
                radius.Value
            );

        if (enemy != null)
            return TaskStatus.Success;

        return TaskStatus.Failure;
    }
}