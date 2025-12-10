using HM.CodeBase;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ZONE_ID
{
    ZONE_0,
    ZONE_1,
    ZONE_2,
    ZONE_3,
    ZONE_4,
    ZONE_5
}

public class MasterAIProvider : ASingletone<MasterAIProvider>
{
    [Header("Player Reference")]
    [SerializeField]
    private Transform _playerTrans;

    [Header("Rough Hint Settings")]
    [SerializeField]
    private float _roughHintNoiseRadius = 5f;   // 좌표 노이즈


    // 존 정보 / 의심도
    private readonly Dictionary<ZONE_ID , ZoneArea> _dic_zoneInfo
        = new Dictionary<ZONE_ID , ZoneArea>();

    private readonly Dictionary<ZONE_ID , float> _dic_zoneSuspicion
        = new Dictionary<ZONE_ID , float>();

    // 마지막으로 알려진 플레이어 정보
    private ZONE_ID _lastKnownZoneId;
    private Vector3 _lastKnownExactPosition;
    private bool _hasLastKnownPosition = false;

    // 추격 AI들이 구독할 이벤트
    public event Action<Vector3> OnRoughHintUpdated;   // 두리뭉술 좌표
    public event Action<Vector3> OnExactPositionReported; // 정확 좌표

    #region Zone 등록 / 조회

    public void RegisterZone(ZoneArea zoneArea)
    {
        if ( _dic_zoneInfo.ContainsKey(zoneArea.ZoneId) )
        {
            Debug.LogWarning($"[MasterAI] Zone {zoneArea.ZoneId} 이 이미 등록되어 있습니다.");
            _dic_zoneInfo[ zoneArea.ZoneId ] = zoneArea;
        }
        else
        {
            _dic_zoneInfo.Add(zoneArea.ZoneId , zoneArea);
        }

        if ( !_dic_zoneSuspicion.ContainsKey(zoneArea.ZoneId) )
        {
            _dic_zoneSuspicion.Add(zoneArea.ZoneId , 0f);
        }
    }

    public ZoneArea GetZone(ZONE_ID zoneId)
    {
        ZoneArea tZone;
        if ( _dic_zoneInfo.TryGetValue(zoneId , out tZone) )
        {
            return tZone;
        }

        return null;
    }

    #endregion

    #region 외부 API

    // CCTV / 추격 AI에게 발각되었을 경우 정확한 좌표 전달
    public void ReportExactPlayerPosition(ZONE_ID zoneId, Vector3 playerPos)
    {
        _lastKnownZoneId = zoneId;
        _lastKnownExactPosition = playerPos;
        _hasLastKnownPosition = true;

        AddSuspicion(zoneId , 10f);

        OnExactPositionReported?.Invoke(playerPos);
    }

    // 대략적인 플레이어 위치 전달
    public void ReportSuspiciousZone(ZONE_ID zoneId, float suspicionAmount)
    {
        AddSuspicion(zoneId, suspicionAmount);

        Vector3 tHintPos = GetRoughHintPosition(zoneId);
        OnRoughHintUpdated?.Invoke(tHintPos);
    }

    public void ReportNoise(Vector3 noisePos , float noiseStrength)
    {
        // 나중에: 가장 가까운 존 찾고 의심도 올리는 로직 추가
        ZONE_ID tClosestZone = GetClosestZoneId(noisePos);
        AddSuspicion(tClosestZone , noiseStrength);

        Vector3 tHintPos = GetRoughHintPosition(tClosestZone);
        OnRoughHintUpdated?.Invoke(tHintPos);
    }

    public ZONE_ID GetClosestZoneId(Vector3 worldPos)
    {
        float tBestSqrDist = float.MaxValue;
        ZONE_ID tBestId = _lastKnownZoneId;

        foreach ( var tPair in _dic_zoneInfo )
        {
            Vector3 tCenter = tPair.Value.CenterPosition;
            float tDist = (tCenter - worldPos).sqrMagnitude;

            if ( tDist < tBestSqrDist )
            {
                tBestSqrDist = tDist;
                tBestId = tPair.Key;
            }
        }

        return tBestId;
    }

    public void ReportExactPlayerPosition(Vector3 playerPos)
    {
        ZONE_ID tZoneId = GetClosestZoneId(playerPos);
        ReportExactPlayerPosition(tZoneId , playerPos);
    }

    #endregion

    #region 내부 메서드

    private void AddSuspicion(ZONE_ID zoneId, float amount)
    {
        float tOld;

        if(!_dic_zoneSuspicion.TryGetValue(zoneId , out tOld) )
        {
            tOld = 0f;
        }

        _dic_zoneSuspicion[ zoneId ] = tOld + amount;
    }

    // 존 중심 + 약간의 랜덤 편차를 줘서 “두리뭉술한 좌표” 생성
    private Vector3 GetRoughHintPosition(ZONE_ID zoneId)
    {
        ZoneArea tZone = GetZone(zoneId);
        if ( tZone == null )
        {
            // fallback: 마지막 정확 좌표가 있으면 거기에 노이즈 추가
            if ( _hasLastKnownPosition )
            {
                return AddNoise(_lastKnownExactPosition);
            }

            // 더 이상 정보가 없을 때는 그냥 (0,0,0)
            return Vector3.zero;
        }

        Vector3 tCenter = tZone.CenterPosition;
        return AddNoise(tCenter);
    }

    private Vector3 AddNoise(Vector3 basePos)
    {
        Vector2 tRandom = UnityEngine.Random.insideUnitCircle * _roughHintNoiseRadius;
        Vector3 tResult = basePos;
        tResult.x += tRandom.x;
        tResult.z += tRandom.y;
        return tResult;
    }

    

    #endregion
}
