using UnityEngine;
using UnityEngine.AI;
using System.Linq;

namespace BehaviorDesigner.Runtime.Tasks.Movement
{
    [TaskDescription("Seek the target specified using the Unity NavMesh.")]
    [TaskCategory("Movement")]
    [HelpURL("https://www.opsive.com/support/documentation/behavior-designer-movement-pack/")]
    [TaskIcon("Assets/Behavior Designer Movement/Editor/Icons/{SkinColor}SeekIcon.png")]

    public class MySeek : NavMeshMovement
    {
        [Tooltip("The GameObject that the agent is seeking")]
        public SharedTransform target;

        private Drone myDrone;

        private const float formationRadius = 12f;

        private const float updateInterval = 0.2f;
        private float nextUpdateTime;

        public override void OnAwake()
        {
            base.OnAwake();

            myDrone = GetComponent<Drone>();
        }

        public override void OnStart()
        {
            base.OnStart();

            m_NavMeshAgent.stoppingDistance =
                m_ArriveDistance.Value;

            nextUpdateTime = 0f;

            UpdateDestination();
        }

        public override TaskStatus OnUpdate()
        {
            if (target.Value == null)
                return TaskStatus.Failure;

            if (Time.time >= nextUpdateTime)
            {
                UpdateDestination();

                nextUpdateTime =
                    Time.time + updateInterval;
            }

            if (HasArrived())
                return TaskStatus.Success;

            return TaskStatus.Running;
        }

        private void UpdateDestination()
        {
            if (target.Value == null)
                return;

            Vector3 destination =
                target.Value.position;

            // Formation spéciale uniquement
            // pour les DRONES ROUGES
            if (myDrone != null &&
                myDrone.ArmyManager is ArmyManagerRed)
            {
                Turret targetTurret =
                    target.Value.GetComponent<Turret>();

                // Formation uniquement quand
                // la cible est une tourelle
                if (targetTurret != null)
                {
                    Drone[] alliedDrones =
                        GameObject
                        .FindObjectsOfType<Drone>()
                        .Where(drone =>
                            drone.gameObject.CompareTag(
                                myDrone.gameObject.tag
                            ))
                        .OrderBy(drone =>
                            drone.GetInstanceID()
                        )
                        .ToArray();

                    int myIndex =
                        System.Array.IndexOf(
                            alliedDrones,
                            myDrone
                        );

                    if (myIndex >= 0 &&
                        alliedDrones.Length > 0)
                    {
                        float angleStep =
                            360f /
                            alliedDrones.Length;

                        float angle =
                            angleStep *
                            myIndex;

                        float angleRad =
                            angle *
                            Mathf.Deg2Rad;

                        Vector3 offset =
                            new Vector3(
                                Mathf.Cos(angleRad),
                                0f,
                                Mathf.Sin(angleRad)
                            )
                            * formationRadius;

                        destination =
                            target.Value.position +
                            offset;
                    }
                }
            }

            NavMeshHit hit;

            if (NavMesh.SamplePosition(
                destination,
                out hit,
                5f,
                NavMesh.AllAreas))
            {
                SetDestination(
                    hit.position
                );
            }
            else
            {
                SetDestination(
                    target.Value.position
                );
            }
        }

        public override void OnReset()
        {
            base.OnReset();

            target = null;
        }
    }
}