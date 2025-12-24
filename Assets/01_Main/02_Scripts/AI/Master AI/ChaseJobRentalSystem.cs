using System;
using UnityEngine;

public struct ChaseJob
{
    public bool IsValid;

    public EZONE_ID TargetZoneId;
    
    public float Priority;
    public float IssuedAt;
    public float ExpiresAt;

    public bool IsExpired(float nowTime)
    {
        return !IsValid || nowTime >= ExpiresAt;
    }
}

public class ChaseJobRentalSystem
{
    private readonly MasterAIConfig_SO _config;

    private readonly int _zoneCount;
    private readonly int _topK;

    private readonly float[] _arr_sortKeys;
    private readonly int[] _arr_zoneIndices;

    private ChaseJob _currentJob;

    public ChaseJobRentalSystem(MasterAIConfig_SO config , int zoneCount , ZoneArea[] arr_ZoneAreaId)
    {
        _config = config;
        _zoneCount = zoneCount;

        _topK = Mathf.Clamp(_config.HintTopK , 1 , _zoneCount);

        _arr_sortKeys = new float[ _zoneCount ];
        _arr_zoneIndices = new int[ _zoneCount ];

        _currentJob = default;
    }

    public void ClearJob()
    {
        _currentJob = default;
    }

    public bool TryRequestJob(ZoneSuspicionSystem zoneSuspicionSystem , float nowTime , out ChaseJob job)
    {
        job = default;

        if ( zoneSuspicionSystem == null || _config == null )
        {
            return false;
        }

        // 유효한 Lease가 있으면 그대로 반환
        if ( _currentJob.IsValid && !_currentJob.IsExpired(nowTime) )
        {
            job = _currentJob;
            return true;
        }

        if ( !TryPickZoneByTopKRoulette(zoneSuspicionSystem , out var tZoneId , out var tPriority) )
        {
            return false;
        }

        var tTtlSec = Mathf.Max(0.1f, _config.HintTTLSec);

        _currentJob = new ChaseJob
        {
            IsValid = true ,
            TargetZoneId = tZoneId ,
            Priority = tPriority ,
            IssuedAt = nowTime ,
            ExpiresAt = nowTime + tTtlSec
        };

        job = _currentJob;
        return true;
    }

    private bool TryPickZoneByTopKRoulette(ZoneSuspicionSystem zoneSuspicionSystem , out EZONE_ID zoneId , out float priority)
    {
        zoneId = default;
        priority = -1f;

        // 1) 정렬 버퍼 채우기
        //    keys = -suspicion 로 두면 Array.Sort가 오름차순 정렬이므로 결과가 "내림차순"처럼 됨.
        for ( int i = 0; i < _zoneCount; i++ )
        {
            var tValue = zoneSuspicionSystem.GetZoneSuspicion((EZONE_ID)i);

            // 최소 우선순위 컷은 여기서 처리하면 룰렛 후보에서 자연스럽게 제외됨
            if ( _config.HintMinPriority > 0f && tValue < _config.HintMinPriority )
            {
                tValue = float.NegativeInfinity;
            }

            _arr_sortKeys[ i ] = -tValue;
            _arr_zoneIndices[ i ] = i;
        }

        // 2) suspicion 내림차순 정렬 효과
        Array.Sort(_arr_sortKeys , _arr_zoneIndices);

        // 3) TopK 후보로 룰렛
        var tPower = Mathf.Max(0.1f, _config.HintRoulettePower);
        var tTotalWeight = 0f;

        // 가중치 합
        for ( int i = 0; i < _topK; i++ )
        {
            var tIdx = _arr_zoneIndices[i];
            var tValue = zoneSuspicionSystem.GetZoneSuspicion((EZONE_ID)tIdx);

            if ( _config.HintMinPriority > 0f && tValue < _config.HintMinPriority )
            {
                continue;
            }

            // NegativeInfinity는 자동으로 스킵
            if ( float.IsNegativeInfinity(tValue) )
            {
                continue;
            }

            tTotalWeight += Mathf.Pow(tValue , tPower);
        }

        if ( tTotalWeight <= 0f )
        {
            return false;
        }

        // 룰렛 선택
        var tPick = UnityEngine.Random.value * tTotalWeight;

        for ( int i = 0; i < _topK; i++ )
        {
            var tIdx = _arr_zoneIndices[i];
            var tValue = zoneSuspicionSystem.GetZoneSuspicion((EZONE_ID)tIdx);

            if ( _config.HintMinPriority > 0f && tValue < _config.HintMinPriority )
            {
                continue;
            }

            if ( float.IsNegativeInfinity(tValue) )
            {
                continue;
            }

            tPick -= Mathf.Pow(tValue , tPower);

            if ( tPick > 0f )
            {
                continue;
            }

            zoneId = (EZONE_ID)tIdx;
            priority = tValue;
            return true;
        }

        // 부동소수 오차 폴백: TopK 중 가장 높은 후보(첫 번째 유효 후보)
        for ( int i = 0; i < _topK; i++ )
        {
            var tIdx = _arr_zoneIndices[i];
            var tValue = zoneSuspicionSystem.GetZoneSuspicion((EZONE_ID)tIdx);

            if ( _config.HintMinPriority > 0f && tValue < _config.HintMinPriority )
            {
                continue;
            }

            if ( float.IsNegativeInfinity(tValue) )
            {
                continue;
            }

            zoneId = (EZONE_ID)tIdx;
            priority = tValue;
            return true;
        }

        return false;
    }
}
