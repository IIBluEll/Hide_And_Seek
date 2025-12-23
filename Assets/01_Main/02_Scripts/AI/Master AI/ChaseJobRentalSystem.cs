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
    private readonly ZoneArea[] _arr_ZoneAreaId;

    private readonly int _topK;
    private readonly int[] _arr_Indices;
    private readonly float[] _arr_Values;

    private ChaseJob _currentJob;

    public ChaseJobRentalSystem(MasterAIConfig_SO config , int zoneCount , ZoneArea[] arr_ZoneAreaId)
    {
        _config = config;
        _zoneCount = zoneCount;
        _arr_ZoneAreaId = arr_ZoneAreaId;

        _topK = Mathf.Clamp(_config.HintTopK , 1 , _zoneCount);
        _arr_Indices = new int[ _topK ];
        _arr_Values = new float[ _topK ];

        _currentJob = default;
    }

    public void ClearJob()
    {
        _currentJob = default;
    }

    private bool IsZoneAreaValid(EZONE_ID zoneId)
    {
        if(_arr_ZoneAreaId == null)
        {
            return false;
        }

        var tIndex = (int )zoneId;

        if (tIndex < 0 || tIndex > _arr_ZoneAreaId.Length)
        {
            return false;
        }

        return _arr_ZoneAreaId[ tIndex ] != null;
    }
}
