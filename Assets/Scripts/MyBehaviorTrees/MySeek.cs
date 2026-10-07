using UnityEngine;

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

        int index;
        static int nObjects = 0;

        public override void OnAwake()
        {
            base.OnAwake();

            index = nObjects++;
        }

        public override void OnStart()
        {
            base.OnStart();

            m_NavMeshAgent.stoppingDistance =
                m_ArriveDistance.Value;

            if (target.Value)
                SetDestination(target.Value.position);
        }

        public override TaskStatus OnUpdate()
        {
            if (target.Value == null)
                return TaskStatus.Failure;

            if (HasArrived())
                return TaskStatus.Success;

            //if((Time.frameCount % nObjects) ==index)
            //    SetDestination(target.Value.position);

            return TaskStatus.Running;
        }

        public override void OnReset()
        {
            base.OnReset();

            target = null;
        }
    }
}