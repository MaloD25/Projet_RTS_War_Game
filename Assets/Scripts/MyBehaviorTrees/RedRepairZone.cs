using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class RedRepairZone : MonoBehaviour
{
    [SerializeField] private float healPerSecond = 5f;
    private void OnTriggerEnter(Collider other)
    {
        Drone drone = other.GetComponentInParent<Drone>();

        if (drone != null &&
            drone.ArmyManager is ArmyManagerRed)
        {
            Debug.Log(
                $"[REPARATION] {drone.name} ENTRE dans le camp"
            );
        }
    }
    private void OnTriggerStay(Collider other)
    {
        Drone drone = other.GetComponentInParent<Drone>();

        if (drone == null ||
            !(drone.ArmyManager is ArmyManagerRed))
            return;

        Health health = drone.GetComponentInChildren<Health>();

        if (health == null ||
            health.Value <= 0f ||
            health.Value >= health.MaxValue)
            return;

        float previousHealth = health.Value;

        health.Heal(healPerSecond * Time.fixedDeltaTime);

        // Afficher un message chaque fois que le drone
        // récupère 10 % supplémentaires de sa vie.
        int previousLevel = Mathf.FloorToInt(
            previousHealth / health.MaxValue * 10f
        );

        int currentLevel = Mathf.FloorToInt(
            health.Value / health.MaxValue * 10f
        );

        if (currentLevel > previousLevel)
        {
            Debug.Log(
                $"[REPARATION] {drone.name} se soigne : " +
                $"{health.Value:F1}/{health.MaxValue:F1} PV"
            );
        }
    }
}