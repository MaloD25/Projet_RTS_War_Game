using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Turret rotates towards target")]

public class TurretSeekTarget : Action
{
	public SharedTransform target;
	Turret turret;

	bool hasRotated;

	public override void OnAwake()
	{
		turret = GetComponent<Turret>();
	}

    public override void OnStart()
    {
        hasRotated = false;

        if (target.Value != null)
        {
            turret.RotateTowards(
                target.Value.position,
                () => hasRotated = true
            );
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (target.Value == null)
            return TaskStatus.Failure;

        if (hasRotated)
            return TaskStatus.Success;

        return TaskStatus.Running;
    }


}