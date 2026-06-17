using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ManageZone
{
    [Header("Debug Info")]
    [SerializeField] private ZoneInfo _currentZone;

    private List<ZoneInfo> _allZones = new List<ZoneInfo>();

    public ZoneInfo CurrentZone => _currentZone;
    public bool IsPlayerInZone => _currentZone != null;

    public void Initialize()
    {
        _allZones = Object.FindObjectsOfType<ZoneInfo>().ToList();
        Debug.Log($"[ZoneManager] 맵 로드 완료: 총 {_allZones.Count}개의 구역이 등록됨.");
    }

    public void SetPlayerZone(ZoneInfo zone)
    {
        if ( _currentZone != zone )
        {
            _currentZone = zone;
        }
    }

    public void ClearPlayerZone(ZoneInfo zone)
    {
        if ( _currentZone == zone )
        {
            _currentZone = null;
        }
    }

    // 플레이어가 있는 존을 제외한 랜덤한 존 반환
    // 퇴근 중이라면 벤트가 있는 존만 반환
    public ZoneInfo GetRandomZoneNotPlayer(bool requireVent = true)
    {
        var tAvailableZones = _allZones.Where(z =>
        z != _currentZone && (!requireVent || z.VentPoint != null)).ToList();

        if(tAvailableZones.Count == 0)
        {
            return null;
        }

        return tAvailableZones[Random.Range(0, tAvailableZones.Count)];
    }

    // 가장 가까운 존 반환
    public ZoneInfo GetNearestZone(Vector3 position)
    {
        return _allZones.OrderBy(z =>
        Vector3.Distance(z.transform.position, position)).FirstOrDefault();
    }
}
