using UnityEngine;

public class ManageChasePerception
{
    private readonly float _sightRange;
    private readonly float _horizontalSightAngle;
    private readonly float _verticalSightAngle;
    private readonly LayerMask _obstacleMask;

    private readonly Transform _eyeTransform;
    private Transform _targetPlayer;

    public ManageChasePerception(float sightRange, float horizontalSightAngle, float verticalSightAngle, LayerMask obstacleMask, Transform eyeTransform)
    {
        _sightRange = sightRange;
        _horizontalSightAngle = horizontalSightAngle;
        _verticalSightAngle = verticalSightAngle;
        _obstacleMask = obstacleMask;
        _eyeTransform = eyeTransform;
    }

    public void SetTarget(Transform targetPlayer)
    {
        _targetPlayer = targetPlayer;
    }

    public bool IsPlayerVisible(Transform chaseTransform)
    {
        if ( _targetPlayer == null )
        {
            return false;
        }

        // 거리 체크
        float tDist = Vector3.Distance(chaseTransform.position, _targetPlayer.position);
        if ( tDist > _sightRange )
        {
            return false;
        }

        // 시야각 체크
        Vector3 tTargetLocal = _eyeTransform.InverseTransformPoint(_targetPlayer.position);
        if ( tTargetLocal.z < 0 )
        {
            return false;
        }

        float tAngleH = Mathf.Atan2(tTargetLocal.x, tTargetLocal.z) * Mathf.Rad2Deg;
        if ( Mathf.Abs(tAngleH) > _horizontalSightAngle * 0.5f ) return false;

        float tAngleV = Mathf.Atan2(tTargetLocal.y, tTargetLocal.z) * Mathf.Rad2Deg;
        if ( Mathf.Abs(tAngleV) > _verticalSightAngle * 0.5f ) return false;

        // 장애물 체크
        Vector3 tDir = (_targetPlayer.position - _eyeTransform.position).normalized;

        // 레이캐스트로 장애물 확인
        if ( Physics.Raycast(_eyeTransform.position , tDir , tDist , _obstacleMask) )
        {
            return false;
        }

        return true;
    }

    public Vector3 GetPlayerPosition()
    {
        return _targetPlayer != null ? _targetPlayer.position : Vector3.zero;
    }

    public bool HasTarget()
    {
        return _targetPlayer != null;
    }
}
