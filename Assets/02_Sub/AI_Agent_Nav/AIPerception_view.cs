using UnityEngine;

public class AIPerception_view : MonoBehaviour
{
    [Header("시야 설정")]
    public float SightRange = 15f;
    public float SightAngle = 60f;
    public LayerMask SightMask;

    public bool CanSee(Transform targetTrans)
    {
        if ( targetTrans == null )
        {
            return false;
        }

        Vector3 tDir = targetTrans.position - transform.position;
        float tDist = tDir.magnitude;

        if ( tDist > SightRange )
        {
            return false;
        }

        tDir.Normalize();
        float tAngle = Vector3.Angle(transform.forward , tDir);

        if ( tAngle > SightAngle * 0.5f )
        {
            return false;
        }

        Vector3 tEyePos = transform.position + Vector3.up;
        if ( Physics.Raycast(tEyePos , tDir , out RaycastHit tHit , SightRange , SightMask) )
        {
            if ( tHit.transform == targetTrans )
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // 시야 범위 / 각도 시각화 (디버그용)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position , SightRange);

        Vector3 tForward = transform.forward;
        Quaternion tLeftRot = Quaternion.AngleAxis(-SightAngle * 0.5f , Vector3.up);
        Quaternion tRightRot = Quaternion.AngleAxis(SightAngle * 0.5f , Vector3.up);

        Vector3 tLeftDir = tLeftRot * tForward;
        Vector3 tRightDir = tRightRot * tForward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position , transform.position + tLeftDir * SightRange);
        Gizmos.DrawLine(transform.position , transform.position + tRightDir * SightRange);
    }
}
