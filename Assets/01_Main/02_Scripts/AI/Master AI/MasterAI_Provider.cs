using System;
using UnityEngine;

public class MasterAI_Provider : MonoBehaviour, IPlayerStayConsumer
{
    [Header("Configs")]
    [SerializeField] private MasterAIConfig_SO _aiConfig;

    private ZoneSuspicionSystem _zoneSuspicionSystem;

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

        _zoneSuspicionSystem = new(_aiConfig , tZoneCount);
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
