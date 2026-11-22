
using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Autorise le combat normal apres les 10 premieres secondes.")]
public class IsOpeningFinished : Conditional
{
    public override TaskStatus OnUpdate()
    {
        if (Timer.Instance == null)
            return TaskStatus.Failure;

        return Timer.Value >= 5f
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }
}
