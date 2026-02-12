using UnityEngine;
using UnityEngine.AI;

namespace AI.ChaseAI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Chase_MovementSystem : MonoBehaviour
    {
        private NavMeshAgent _agent;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        public void UpdateSpeed(CHASEAI_ANGER_PHASE phase , MasterAI.MasterAI_Config config)
        {
            float tTargetSpeed = config.WalkSpeed;

            switch ( phase )
            {
                case CHASEAI_ANGER_PHASE.MENACE:

                    tTargetSpeed = config.WalkSpeed;
                    break;

                case CHASEAI_ANGER_PHASE.STALK:

                    tTargetSpeed = config.StalkSpeed;
                    break;

                case CHASEAI_ANGER_PHASE.HUNT:

                    tTargetSpeed = config.RunSpeed;
                    break;
            }

            _agent.speed = tTargetSpeed;
        }

        public void SetDestination(Vector3 targetPos)
        {
            if ( _agent.isOnNavMesh )
            {
                _agent.SetDestination(targetPos);
                _agent.isStopped = false;
            }
        }

        public void StopMove()
        {
            if ( _agent.isOnNavMesh )
            {
                _agent.isStopped = true;
            }
        }

        public bool HasReachedDestination(float stoppingDistance = 1.0f)
        {
            if ( _agent.pathPending )
            {
                return false;
            }
                
            return _agent.remainingDistance <= stoppingDistance;
        }

        public void Warp(Vector3 pos)
        {
            _agent.Warp(pos);
        }
    }

}

