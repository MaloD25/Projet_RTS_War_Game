using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.AI;

public class ArmyManagerRed : ArmyManager
{
    public bool HasEnemyOfType<T>() where T : ArmyElement
    {
        return GetAllEnemiesOfType<T>(false).Count > 0;
    }

    private GameObject m_FocusDroneTarget;

    public GameObject GetEmergencyEnemyDrone(
    Vector3 centerPos,
    float emergencyRadius)
    {
        var drones = GetAllEnemiesOfType<Drone>(false)
            .Where(drone =>
                Vector3.Distance(
                    centerPos,
                    drone.transform.position
                ) < emergencyRadius
            )
            .OrderBy(drone =>
                Vector3.Distance(
                    centerPos,
                    drone.transform.position
                ))
            .ToList();

        if (drones.Count == 0)
            return null;

        return drones[0].gameObject;
    }

    public GameObject GetPriorityEnemyTurret(
        Vector3 centerPos,
        float minRadius,
        float maxRadius)
    {
        var turrets = GetAllEnemiesOfType<Turret>(false)
            .Where(turret =>
            {
                float distance = Vector3.Distance(
                    centerPos,
                    turret.transform.position
                );

                return distance > minRadius &&
                       distance < maxRadius;
            })
            .OrderBy(turret => turret.Health)
            .ThenBy(turret =>
                Vector3.Distance(
                    centerPos,
                    turret.transform.position
                ))
            .ToList();

        if (turrets.Count == 0)
            return null;

        return turrets[0].gameObject;
    }

    public bool TryGetBestBombardmentPoint(
    Vector3 turretPosition,
    out Vector3 bestPoint)
    {
        Vector3 center = GetDefenseCenter();

        const float ringRadius = 14f;
        const float damageRadius = 5f;

        const float turretMinRange = 5f;
        const float turretMaxRange = 50f;

        const int numberOfZones = 12;

        var enemies = GetAllEnemiesOfType<Drone>(false);

        bestPoint = Vector3.zero;
        int bestScore = 0;


        for (int i = 0; i < numberOfZones; i++)
        {
            float angle =
                (360f / numberOfZones) * i;

            float angleRad =
                angle * Mathf.Deg2Rad;

            Vector3 zone = center + new Vector3(
                Mathf.Cos(angleRad),
                0f,
                Mathf.Sin(angleRad)
            ) * ringRadius;

            // La zone doit être à portée de cette tourelle
            float distanceFromTurret =
                Vector3.Distance(turretPosition, zone);

            if (distanceFromTurret <= turretMinRange ||
                distanceFromTurret >= turretMaxRange)
            {
                continue;
            }

            int score = 0;

            foreach (Drone enemy in enemies)
            {
                float enemyDistance =
                    Vector3.Distance(
                        zone,
                        enemy.transform.position
                    );

                if (enemyDistance <= damageRadius)
                {
                    score++;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = zone;
            }
        }

        return bestScore > 0;
    }

    public Vector3 GetDefenseCenter()
    {
        var turrets = m_ArmyElements
            .Where(element => element is Turret)
            .Select(element => ((MonoBehaviour)element).transform.position)
            .ToList();

        if (turrets.Count == 0)
            return transform.position;

        Vector3 center = Vector3.zero;

        foreach (Vector3 position in turrets)
        {
            center += position;
        }

        return center / turrets.Count;
    }

    public Vector3 GetDroneDefensePosition(Drone drone)
    {
        var drones = m_ArmyElements
            .Where(element => element is Drone)
            .Select(element => (Drone)element)
            .OrderBy(element => element.GetInstanceID())
            .ToList();

        int index = drones.IndexOf(drone);

        if (index < 0 || drones.Count == 0)
            return GetDefenseCenter();

        const float defenseRadius = 12f;

        float angleStep = 360f / drones.Count;
        float angle = angleStep * index;
        float angleRad = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(angleRad),
            0f,
            Mathf.Sin(angleRad)
        ) * defenseRadius;

        return GetDefenseCenter() + offset;
    }

    public GameObject GetBestClusteredDroneTarget(
        Vector3 turretPosition,
        float minRadius,
        float maxRadius)
    {
        var enemies = GetAllEnemiesOfType<Drone>(false)
            .Where(enemy =>
            {
                float distance =
                    Vector3.Distance(
                        turretPosition,
                        enemy.transform.position
                    );

                return distance > minRadius &&
                       distance < maxRadius;
            })
            .ToList();

        if (enemies.Count == 0)
            return null;

        const float explosionRadius = 5f;

        Drone bestTarget = enemies
            .OrderByDescending(candidate =>
                enemies.Count(other =>
                    Vector3.Distance(
                        candidate.transform.position,
                        other.transform.position
                    ) <= explosionRadius
                )
            )
            .ThenBy(candidate =>
                Vector3.Distance(
                    turretPosition,
                    candidate.transform.position
                )
            )
            .First();

        return bestTarget.gameObject;
    }

    public GameObject GetFocusedEnemyDrone(
        Vector3 requesterPosition,
        float minRadius,
        float maxRadius)
    {
        // Si la cible actuelle existe encore et est accessible,
        // on continue de l'attaquer.
        if (m_FocusDroneTarget != null)
        {
            float distance = Vector3.Distance(
                requesterPosition,
                m_FocusDroneTarget.transform.position
            );

            if (distance > minRadius && distance < maxRadius)
            {
                return m_FocusDroneTarget;
            }
        }

        // Sinon on choisit une nouvelle cible.
        var enemies = GetAllEnemiesOfType<Drone>(false)
            .Where(enemy =>
            {
                float distance = Vector3.Distance(
                    requesterPosition,
                    enemy.transform.position
                );

                return distance > minRadius &&
                       distance < maxRadius;
            })
            .OrderBy(enemy => enemy.Health)
            .ThenBy(enemy =>
                Vector3.Distance(
                    requesterPosition,
                    enemy.transform.position
                ));

        Drone selectedEnemy = enemies.FirstOrDefault();

        if (selectedEnemy == null)
        {
            m_FocusDroneTarget = null;
            return null;
        }

        m_FocusDroneTarget = selectedEnemy.gameObject;

        return m_FocusDroneTarget;
    }

    public override void ArmyElementHasBeenKilled(GameObject go)
	{
		base.ArmyElementHasBeenKilled(go);
		if (m_ArmyElements.Count == 0)
		{
			GUIUtility.systemCopyBuffer = "0\t" +((int)Timer.Value).ToString()+"\t0\t0\t0";
		}
	}

    public GameObject GetClosestEnemy<T>(
    Vector3 centerPos,
    float minRadius,
    float maxRadius) where T : ArmyElement
    {
        var enemies = GetAllEnemiesOfType<T>(false)
            .Where(item =>
            {
                float distance = Vector3.Distance(centerPos, item.transform.position);

                return distance > minRadius &&
                       distance < maxRadius;
            })
            .OrderBy(item =>
                Vector3.Distance(centerPos, item.transform.position));

        return enemies.FirstOrDefault()?.gameObject;
    }

    public void GreenArmyIsDead(string deadArmyTag)
    {
        int nDrones = 0, nTurrets = 0, health = 0;
        ComputeStatistics(ref nDrones, ref nTurrets, ref health);
		GUIUtility.systemCopyBuffer = "1\t" + ((int)Timer.Value).ToString() + "\t"+nDrones.ToString()+"\t"+nTurrets.ToString()+"\t"+health.ToString();
		
		RefreshHudDisplay(); //pour une dernière mise à jour en cas de victoire
	}

}