using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Select non targeted enemy turret")]

public class SelectEnemyTurret : Action
{
	IArmyElement m_ArmyElement;
	public SharedTransform target;
	public SharedFloat minRadius;
	public SharedFloat maxRadius;

	public override void OnAwake()
	{
		m_ArmyElement =(IArmyElement) GetComponent(typeof(IArmyElement));
	}

    public override TaskStatus OnUpdate()
    {
        if (m_ArmyElement.ArmyManager == null)
            return TaskStatus.Running;

        GameObject enemy;

        if (m_ArmyElement.ArmyManager is ArmyManagerRed redManager)
        {
            enemy = redManager.GetPriorityEnemyTurret(
                transform.position,
                minRadius.Value,
                maxRadius.Value
            );
        }
        else
        {
            enemy = m_ArmyElement.ArmyManager.GetRandomEnemy<Turret>(
                transform.position,
                minRadius.Value,
                maxRadius.Value
            );
        }

        if (enemy == null)
            return TaskStatus.Failure;

        target.Value = enemy.transform;

        return TaskStatus.Success;
    }
}