
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("MyTasks")]
[TaskDescription("Repartit les drones rouges entre les ennemis.")]
public class SelectRedFocusTarget : Action
{
    public SharedTransform target;

    public int maxDronesPerTurret = 2;
    public int maxDronesPerEnemyDrone = 3;

    private Drone myDrone;

    private static readonly Dictionary<Drone, ArmyElement> assignments =
        new Dictionary<Drone, ArmyElement>();

    public override void OnAwake()
    {
        myDrone = GetComponent<Drone>();
    }

    public override TaskStatus OnUpdate()
    {
        if (myDrone == null)
            return TaskStatus.Failure;

        CleanAssignments();

        if (assignments.TryGetValue(myDrone, out ArmyElement current))
        {
            if (IsValidEnemy(current))
            {
                target.Value = current.transform;
                return TaskStatus.Success;
            }

            assignments.Remove(myDrone);
        }

        ArmyElement selected = FindBestEnemy<Turret>(
            maxDronesPerTurret
        );

        if (selected == null)
        {
            selected = FindBestEnemy<Drone>(
                maxDronesPerEnemyDrone
            );
        }

        if (selected == null)
        {
            target.Value = null;
            return TaskStatus.Failure;
        }

        assignments[myDrone] = selected;
        target.Value = selected.transform;

        Debug.Log(
            $"[FOCUS RED] {gameObject.name} attaque {selected.gameObject.name}"
        );

        return TaskStatus.Success;
    }

    private bool IsValidEnemy(ArmyElement enemy)
    {
        return enemy != null &&
               enemy.Health > 0f &&
               !enemy.gameObject.CompareTag(gameObject.tag);
    }

    private ArmyElement FindBestEnemy<T>(int maxAttackers)
        where T : ArmyElement
    {
        T[] enemies = Object.FindObjectsByType<T>(
            FindObjectsSortMode.None
        );

        ArmyElement bestAvailable = null;
        float bestAvailableScore = float.MaxValue;

        ArmyElement bestFallback = null;
        float bestFallbackScore = float.MaxValue;

        int limit = Mathf.Max(1, maxAttackers);

        foreach (T enemy in enemies)
        {
            if (!IsValidEnemy(enemy))
                continue;

            int attackers = CountAttackers(enemy);

            float distance = Vector3.Distance(
                transform.position,
                enemy.transform.position
            );

            float score =
                distance +
                enemy.Health * 0.1f +
                attackers * 20f;

            if (attackers < limit &&
                score < bestAvailableScore)
            {
                bestAvailableScore = score;
                bestAvailable = enemy;
            }

            if (score < bestFallbackScore)
            {
                bestFallbackScore = score;
                bestFallback = enemy;
            }
        }

        return bestAvailable != null
            ? bestAvailable
            : bestFallback;
    }

    private int CountAttackers(ArmyElement enemy)
    {
        int count = 0;

        foreach (ArmyElement assigned in assignments.Values)
        {
            if (assigned == enemy)
                count++;
        }

        return count;
    }

    private static void CleanAssignments()
    {
        List<Drone> toRemove = new List<Drone>();

        foreach (var entry in assignments)
        {
            if (entry.Key == null ||
                entry.Value == null ||
                entry.Value.Health <= 0f)
            {
                toRemove.Add(entry.Key);
            }
        }

        foreach (Drone drone in toRemove)
        {
            assignments.Remove(drone);
        }
    }
}
