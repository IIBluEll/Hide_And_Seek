using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ZoneInfo : MonoBehaviour
{
    [Header("구역")]
    public string ZoneID;
    public Collider ZoneBounds;

    [Space(10f),Header("수색 포인트")]
    [SerializeField] private List<Transform> _searchPoints = new();
    [SerializeField] private List<Transform> _hidingSpots = new();

    [Space(10f), Header("AI 스폰 지점")]
    [SerializeField] private List<Transform> _spawnPoints = new();

    public bool IsPlayerInZone(Vector3 playerPos)
    {
        if(ZoneBounds == null) 
        {
            return false;
        }

        return ZoneBounds.bounds.Contains(playerPos);
    }

    // 랜덤 수색 지점 반환
    public Vector3 GetRandomSearchPoint()
    {
        if(_searchPoints.Count == 0)
        {
            return transform.position;
        }

        return _searchPoints[ Random.Range(0 , _searchPoints.Count) ].position;
    }

    // 플레이어와 가장 가까운 은신처 반환
    public Vector3 GetNearHidingSpot(Vector3 playerPos)
    {
        if ( _hidingSpots.Count == 0 )
        {
            return GetRandomSearchPoint();
        }

        return _hidingSpots.OrderBy(t => Vector3.SqrMagnitude(t.position - playerPos)).FirstOrDefault().position;
    }

    // 랜덤 스폰 위치 반환
    public Vector3 GetRandomSpawnPoint()
    {
        if(_spawnPoints.Count == 0)
        {
            return transform.position;
        }

        return _spawnPoints[Random.Range(0, _spawnPoints.Count) ].position;
    }
}
