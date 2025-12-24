using System;
using UnityEngine;

public class MasterAI_Provider : MonoBehaviour, IPlayerStayConsumer
{
    [Header("Configs")]
    [SerializeField] private MasterAIConfig_SO _aiConfig;

    [Header("Player")]
    [SerializeField] private Transform _playerTrans;

    [Header("Zones (Optional)")]
    [SerializeField] private ZoneArea[] _arr_zoneAreas;

    private ZoneArea[] _arr_zoneAreaById;
    private Transform[] _arr_ventSpotById;

    private ZoneSuspicionSystem _zoneSuspicionSystem;
    private ChaseJobRentalSystem _chaseJobRentalSystem;

    private bool _isActive;
    private float _tickSec;

    public bool IsActive => _isActive;

    // Debug
    public bool IsAutoStart = false;

    private void Awake()
    {
        InitSubSystem();
    }

    private void Start()
    {
        if(IsAutoStart)
        {
            SetActiveAI(true);
        }
    }

    private void Update()
    {
        if(!_isActive || _aiConfig == null)
        {
            return;
        }

        _tickSec += Time.deltaTime;
    
        if(_tickSec < 1.0f)
        {
            return;
        }

        _tickSec = 0f;

        _zoneSuspicionSystem.Tick();
    }

    private void InitSubSystem()
    {
        if(_aiConfig == null)
        {
            Debug.LogError("AI Config SO 파일이 없습니다");
            return;
        }

        var tZoneCount = Enum.GetValues(typeof(EZONE_ID)).Length;

        BuildZoneMap(tZoneCount);

        _zoneSuspicionSystem = new(_aiConfig , tZoneCount);
        _chaseJobRentalSystem = new(_aiConfig , tZoneCount , _arr_zoneAreaById);
    }

    private void BuildZoneMap(int zoneCount)
    {
        _arr_zoneAreaById = new ZoneArea[ zoneCount ];
        _arr_ventSpotById = new Transform[ zoneCount ];

        ZoneArea[] tSources = _arr_zoneAreas;

        if ( tSources == null || tSources.Length <= 0 )
        {
            tSources = FindObjectsOfType<ZoneArea>(true);
        }

        for ( int i = 0; i < tSources.Length; i++ )
        {
            ZoneArea tZoneArea = tSources[i];

            if ( tZoneArea == null )
            {
                continue;
            }

            int tIndex = (int)tZoneArea.ZoneIds;

            if ( tIndex < 0 || tIndex >= _arr_zoneAreaById.Length )
            {
                continue;
            }

            _arr_zoneAreaById[ tIndex ] = tZoneArea;

            // Zone당 환풍구 1개 캐싱
            _arr_ventSpotById[ tIndex ] = tZoneArea.VentSpot;
        }
    }

    #region Public API

    // AI 활성화
    public void SetActiveAI(bool isActive)
    {
        Debug.Log($"[MasterAI] AI 활성화 여부 : {isActive}");
        _isActive = isActive;
        _tickSec = 0;
    }

    // 플레이어가 한 zone에 너무 오래 머물러서 발생한 이벤트
    public void OnPlayerStayThresholdReached(ZoneArea zoneArea)
    {
        if(!_isActive || _zoneSuspicionSystem == null)
        {
            return;
        }

        _zoneSuspicionSystem.AddPlayerCampingSuspicion(zoneArea);
    }

    //추격 AI가 순찰, 조사 마치고 나서 의심도 감소
    public void ReportSearchJobCompleted(ZoneArea zoneArea , float multiplier)
    {
        if ( !_isActive || _zoneSuspicionSystem == null )
        {
            return;
        }

        _zoneSuspicionSystem.MultiplyZoneSuspicion(zoneArea, multiplier);
    }

    #endregion

    #region 추격AI에게 Job 전달 

    public bool TryRequestChaseJob(float nowTime, out ChaseJob job)
    {
        job = default;

        if(!_isActive || _chaseJobRentalSystem == null)
        {
            return false;
        }

        return _chaseJobRentalSystem.TryRequestJob(_zoneSuspicionSystem, nowTime, out job);
    }

    public bool TryGetZoneArea(EZONE_ID zoneId, out ZoneArea zoneArea)
    {
        zoneArea = null;

        if(_arr_zoneAreaById == null)
        {
            return false;
        }

        int tIndex = (int)zoneId;

        if ( tIndex < 0 || tIndex >= _arr_zoneAreaById.Length )
        {
            return false;
        }

        zoneArea = _arr_zoneAreaById[ tIndex ];
        return zoneArea != null;
    }

    public void ClearChaseJob()
    {
        _chaseJobRentalSystem?.ClearJob();
    }

    #endregion

    #region 추격AI에게 복귀 환풍구 지정 ( 해당 zone에 플레이어 있으면 다른 환풍구 지정 )

    public bool TryGetReturnVentSpot(Vector3 chaseWorldPos , out Transform ventSpotTrans)
    {
        ventSpotTrans = null;

        if ( _arr_ventSpotById == null || _arr_ventSpotById.Length <= 0 )
        {
            return false;
        }

        bool tHasPlayerZone = TryGetPlayerZoneId(out EZONE_ID tPlayerZoneId);

        float tBestDistSqr = float.MaxValue;
        int tBestIndex = -1;

        float tBestNonPlayerDistSqr = float.MaxValue;
        int tBestNonPlayerIndex = -1;

        for ( int i = 0; i < _arr_ventSpotById.Length; i++ )
        {
            Transform tVentSpotTrans = _arr_ventSpotById[i];

            if ( tVentSpotTrans == null )
            {
                continue;
            }

            float tDistSqr = (tVentSpotTrans.position - chaseWorldPos).sqrMagnitude;

            // 전체 최단
            if ( tDistSqr < tBestDistSqr )
            {
                tBestDistSqr = tDistSqr;
                tBestIndex = i;
            }

            // 플레이어 Zone 제외 최단
            if ( tHasPlayerZone && (EZONE_ID)i == tPlayerZoneId )
            {
                continue;
            }

            if ( tDistSqr < tBestNonPlayerDistSqr )
            {
                tBestNonPlayerDistSqr = tDistSqr;
                tBestNonPlayerIndex = i;
            }
        }

        if ( tBestIndex < 0 )
        {
            return false;
        }

        // 최단이 플레이어 Zone이면 -> 다음 후보(플레이어 Zone 제외 최단)
        if ( tHasPlayerZone && (EZONE_ID)tBestIndex == tPlayerZoneId && tBestNonPlayerIndex >= 0 )
        {
            ventSpotTrans = _arr_ventSpotById[ tBestNonPlayerIndex ];
            return ventSpotTrans != null;
        }

        ventSpotTrans = _arr_ventSpotById[ tBestIndex ];
        return ventSpotTrans != null;
    }

    private bool TryGetPlayerZoneId(out EZONE_ID zoneId)
    {
        zoneId = default;

        if ( _playerTrans == null || _arr_zoneAreaById == null )
        {
            return false;
        }

        Vector3 tPlayerPos = _playerTrans.position;

        for ( int i = 0; i < _arr_zoneAreaById.Length; i++ )
        {
            ZoneArea tZoneArea = _arr_zoneAreaById[i];

            if ( tZoneArea == null )
            {
                continue;
            }

            if ( tZoneArea.ContainsWorldPos(tPlayerPos) )
            {
                zoneId = (EZONE_ID)i;
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Debug API

    public float GetZoneSuspicion(EZONE_ID zoneId)
    {
        if ( _zoneSuspicionSystem == null )
        {
            return 0f;
        }

        return _zoneSuspicionSystem.GetZoneSuspicion(zoneId);
    }

    #endregion
}
