using System;
using UnityEngine;

public class ZoneSuspicionSystem
{
    private readonly MasterAIConfig_SO _config;
    private readonly float[] _arr_ZoneSuspicion;

    public ZoneSuspicionSystem(MasterAIConfig_SO config, int zoneCount)
    {
        _config = config;
        _arr_ZoneSuspicion = new float[zoneCount];
    }

    public void Tick()
    {
        // 각 Zone에서 의심도 지속적으로 감소
        for ( int i = 0; i < _arr_ZoneSuspicion.Length; i++ )
        {
            _arr_ZoneSuspicion[ i ] = MathF.Max(0f , _arr_ZoneSuspicion[ i ] - _config.DecreasePerSec);
        }
    }

    #region Public API
    
    public void SetZoneSuspicion(ZoneArea zoneArea, float amount)
    {
        if ( zoneArea == null )
        {
            return;
        }

        AddSuspicion(zoneArea, amount);
    }

    //의심도 배율 조정
    public void MultiplyZoneSuspicion(ZoneArea zoneArea , float multiPlier)
    {
        if ( zoneArea == null )
        {
            return;
        }

        var tIndex = (int)zoneArea.ZoneIds;

        if ( tIndex < 0 || tIndex > _arr_ZoneSuspicion.Length)
        {
            return;
        }

        _arr_ZoneSuspicion[ tIndex ] = Mathf.Clamp(_arr_ZoneSuspicion[ tIndex ] * Mathf.Max(0f , multiPlier) , 0f , _config.SuspicionMax);
    }

    // 플레이어가 한 Zone에 너무 오래 머무를 때
    public void AddPlayerCampingSuspicion(ZoneArea zoneArea)
    {
        if ( zoneArea == null )
        {
            return;
        }

        AddSuspicionWithSpread(zoneArea , _config.StaySuspicionPerCamp);
    }
    #endregion

    #region 의심도 증가 로직
    // 해당 Zone 이웃 Zone들도 일정 비율만큼 상승
    private void AddSuspicionWithSpread(ZoneArea zoneArea , float amount)
    {
        if ( amount <= 0f )
        {
            return;
        }

        AddSuspicion(zoneArea , amount );

        var tNeighbors = zoneArea.Neighbors;
        var tSpreadAmount = amount * _config.SpreadRatio;

        if( tNeighbors == null || tNeighbors.Length <= 0)
        {
            return;
        }

        foreach ( var tNeighbor in tNeighbors )
        {
            if( tNeighbor == null)
            {
                continue;
            }

            AddSuspicion(tNeighbor , tSpreadAmount);
        }
    }

    private void AddSuspicion(ZoneArea zoneArea , float amount)
    {
        var tIndex = (int)zoneArea.ZoneIds;

        if(tIndex < 0 || tIndex >= _arr_ZoneSuspicion.Length )
        {
            return;
        }

        _arr_ZoneSuspicion[ tIndex ] = Mathf.Clamp(_arr_ZoneSuspicion[ tIndex ] + amount , 0f , _config.SuspicionMax);
    }
    #endregion

    #region 각 Zone 의심도 반환 로직

    public EZONE_ID GetTopZoneId()
    {
        var tBestIndex = 0;
        var tBestValue = -1f;

        for ( int i = 0; i < _arr_ZoneSuspicion.Length; i++ )
        {
            if ( _arr_ZoneSuspicion[ i ] <= tBestValue )
            {
                continue;
            }

            tBestValue = _arr_ZoneSuspicion[ i ];
            tBestIndex = i;
        }

        return (EZONE_ID)tBestIndex;
    }

    public float GetZoneSuspicion(EZONE_ID zoneId)
    {
        var tIndex = (int)zoneId;

        if ( tIndex < 0 || tIndex >= _arr_ZoneSuspicion.Length )
        {
            return 0f;
        }

        return _arr_ZoneSuspicion[ tIndex ];
    }
    #endregion

    public void ResetAllSuspicion()
    {
        for ( int i = 0; i < _arr_ZoneSuspicion.Length; i++ )
        {
            _arr_ZoneSuspicion[ i ] = 0f;
        }
    }
}
