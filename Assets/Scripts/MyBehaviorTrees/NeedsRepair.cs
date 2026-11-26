using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Verifie si le drone doit retourner se faire reparer.")]
public class NeedsRepair : Conditional
{
    public float retreatThreshold = 0.4f;
    public float resumeThreshold = 0.9f;

    private Health health;
    private bool repairing = false;

    public override void OnAwake()
    {
        health = gameObject.GetComponentInChildren<Health>();
    }

    public override TaskStatus OnUpdate()
    {
        if (health == null || health.MaxValue <= 0f ||
            health.Value <= 0f)
            return TaskStatus.Failure;

        float healthRatio = health.Value / health.MaxValue;

        // Si le drone est deja en reparation,
        // il doit attendre d'avoir retrouve 90 % de sa vie.
        if (repairing)
        {
            if (healthRatio >= resumeThreshold)
            {
                repairing = false;

                Debug.Log(
                    $"[REPARATION] {gameObject.name} est soigne " +
                    $"({healthRatio * 100f:F0}%) et reprend le combat"
                );
            }
        }
        // Sinon, il rentre au camp sous 40 % de vie.
        else if (healthRatio <= retreatThreshold)
        {
            repairing = true;

            Debug.Log(
                $"[REPARATION] {gameObject.name} est blesse : " +
                $"{healthRatio * 100f:F0}% de vie"
            );
        }

        return repairing
            ? TaskStatus.Success
            : TaskStatus.Failure;
    }
}