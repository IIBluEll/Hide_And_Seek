using UnityEngine;

namespace AI.ChaseAI
{
    public class Chase_PerceptionSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _viewRadius = 10f;
        [SerializeField] private float _viewAngle = 90f;

        [SerializeField] private LayerMask _targetMask;   
        [SerializeField] private LayerMask _obstacleMask;

        [Space(5f), Header("Target")]
        [SerializeField] private Transform _eyeTransform;

        public bool CheckSight(Transform target)
        {
            if(target == null)
            {
                return false;
            }

            Vector3 tDirToTarget = (target.position - _eyeTransform.position).normalized;

            float tDis = Vector3.Distance(_eyeTransform.position, target.position);

            // 거리 체크
            if ( tDis > _viewRadius)
            {
                return false;
            }

            // 각도 체크
            if(Vector3.Angle(_eyeTransform.forward, tDirToTarget) > _viewAngle / 2f)
            {
                return false;
            }

            // 플레이어 방향으로 레이를 쐈을 때 장애물이 없어야 함
            if ( !Physics.Raycast(_eyeTransform.position, tDirToTarget, tDis, _obstacleMask))
            {
                return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if ( _eyeTransform == null ) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_eyeTransform.position , _viewRadius);
        }
    }
}

