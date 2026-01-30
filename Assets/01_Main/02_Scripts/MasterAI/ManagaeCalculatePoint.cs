using UnityEngine;
using UnityEngine.AI;

public class ManagaeCalculatePoint
{
    private Transform _chaseTransform;
    private Transform _playerTransform;
    private LayerMask _zoneLayerMask;

    private float _maxSearch;
    private float _minSearch;

    //TODO : 벤트 수색 반경을 MasterAI에서 받아올것인가?
    private float _maxVentSearch = 20f;

    public ManagaeCalculatePoint(Transform chaseAiTransform ,Transform playerTransform, LayerMask zoneLayerMask, float maxSearchRadius, float minSearchRadius)
    {
        _chaseTransform = chaseAiTransform;
        _playerTransform = playerTransform;
        _zoneLayerMask = zoneLayerMask;
        _maxSearch = maxSearchRadius;
        _minSearch = minSearchRadius;
    }

    #region 수색 지점 계산

    public Vector3 CalculateSearchPoint(float areaAlert)
    {
        float tCurrentRadius  = Mathf.Lerp(_maxSearch, _minSearch, areaAlert);

        var tSelectZone = GetRandomZone(_playerTransform, tCurrentRadius, false);

        if(tSelectZone != null)
        {
            Debug.Log("[SearchPoint] 선택된 Zone: " + tSelectZone.ZoneID);

            if ( areaAlert > 0.6f )
            {
                // 경계도가 높으면 은신처 수색
                return tSelectZone.GetNearHidingSpot(_playerTransform.position);
            }

            return tSelectZone.GetRandomSearchPoint();
        }

        return CalculateRanomPoint(tCurrentRadius);
    }

    // 플레이어 기준 랜덤 좌표 ( 복도에 있을 시 )
    private Vector3 CalculateRanomPoint(float radius)
    {
        Vector2 tRandomCircle = Random.insideUnitCircle.normalized * radius;
        Vector3 tTargetPos = _playerTransform.position + new Vector3(tRandomCircle.x, 0, tRandomCircle.y);

        if ( TryGetValidPoint(_playerTransform.position , radius , out Vector3 validPos , 20) )
        {
            return validPos;
        }

        Debug.Log("좌표 구하기 실패! 플레이어 좌표 반환");
        return _playerTransform.position;
    }

    #endregion

    #region 벤트 계산

    public Transform CalculateVentPoint()
    {
        int tMaxTry = 10;
        float tAddDistance = 20f;

        for ( int i = 0; i < tMaxTry; i++ )
        {
            float tRadius = _maxVentSearch + (tAddDistance * i);

            var tSelectZone = GetRandomZone(_chaseTransform, tRadius, true);

            if ( tSelectZone != null )
            {
                return tSelectZone.VentPoint;
            }
        }

        //TODO : 만약에 벤트가 없으면?
        return null;
    }

    #endregion

    #region 계산 메서드

    // 타겟 주변의 랜덤 zone 반환
    private ZoneInfo GetRandomZone(Transform target , float radius, bool isSpawn)
    {
        // 타겟 주변에 있는 zone 검사
        var tHitCollider = Physics.OverlapSphere(target.position, radius, _zoneLayerMask, QueryTriggerInteraction.Collide);

        // 스폰/디스폰이 아닐 때
        if ( tHitCollider.Length > 0 && !isSpawn)
        {
            //zone들 중 하나 랜덤 선택
            var tRandomCol = tHitCollider[Random.Range(0, tHitCollider.Length)];
            return tRandomCol.GetComponent<ZoneInfo>();
        }
        else if(tHitCollider.Length > 0 && isSpawn )
        {
            // 스폰 / 디스폰일 경우 벤트를 못찾을 경우를 대비해 반복문
            int tRandomNum = 0;
            ZoneInfo tSelectedZone = null;

            for ( int i = 0; i < tHitCollider.Length; i++)
            {
                var tZone = tHitCollider[i].GetComponent<ZoneInfo>();
                if ( tZone == null || tZone.VentPoint == null )
                {
                    continue;
                }

                tRandomNum++;

                if ( Random.Range(0 , tRandomNum) == 0 )
                {
                    tSelectedZone = tZone;
                }
            }

            return tSelectedZone;
        }

        return null;
    }

    private bool TryGetValidPoint(Vector3 center , float radius , out Vector3 result , int attemptCount = 10)
    {
        for ( int i = 0; i < attemptCount; i++ )
        {
            // 중심에서 랜덤한 방향과 거리의 좌표 생성
            Vector2 randomCircle = Random.insideUnitCircle;
            Vector3 randomDir = new Vector3(randomCircle.x, 0, randomCircle.y);

            // 시도할 후보 좌표 (중심 + 랜덤 벡터)
            Vector3 candidatePos = center + (randomDir * radius);

            NavMeshHit hit;
            if ( NavMesh.SamplePosition(candidatePos , out hit , 2.0f , NavMesh.AllAreas) )
            {
                result = hit.position;
                return true;
            }
        }
        result = center;
        return false;
    }
    #endregion
}
