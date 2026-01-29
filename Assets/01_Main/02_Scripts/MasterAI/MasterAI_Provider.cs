using HM.CodeBase;
using UnityEngine;
public enum MASTERAI_PHASE
{
    ACTIVE,
    DORMANT,
}

public class MasterAI_Provider : ASingletone<MasterAI_Provider>
{
    [Header("난이도 설정 데이터")]
    [SerializeField] private MasterAI_Config _configData;

    [Header("참조")]
    [SerializeField] private ChaseAI_Controller _chaseAI;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private LayerMask _zoneLayerMask;

    [Space(10f), Header("시스템 모듈")]
    [SerializeField] private ManageGauage _gaugeSystem;
    [SerializeField] private ManageZone _zoneManager;
    [SerializeField] private MasterState_Dormant _dormantState;
    [SerializeField] private MasterState_Active _activeState;
    [SerializeField] private ManagaeCalculatePoint _manage_SearchPoint;

    private float _lastContactTime = float.MinValue;

    private Vector3 _debugLastTargetPos = Vector3.zero;
    private MASTERAI_PHASE _currentPhase = MASTERAI_PHASE.DORMANT;
    private IMasterState _currentStateLogic;

    //Debug
    public MasterAI_Config ConfigData => _configData;
    public ManageGauage GaugeSystem => _gaugeSystem;
    public ChaseAI_Controller ChaseAI => _chaseAI;
    public ManageZone ZoneMng => _zoneManager;
    public float LastContactTime => _lastContactTime;

    #region Unity LifeCycle

    private void Start()
    {
        if ( _configData == null )
        {
            Debug.LogError("MasterAI_Data가 할당되지 않았습니다! 기본값 생성 또는 할당 필요.");
            return;
        }

        _zoneManager.Initialize();
        _gaugeSystem.Reset();

        if ( _chaseAI != null && _playerTransform != null )
        {
            _chaseAI.Initalize(_playerTransform);
            _chaseAI.Vanish();
        }

        _manage_SearchPoint = new ManagaeCalculatePoint(_chaseAI.transform , _playerTransform , _zoneLayerMask , _configData.MaxSearchRadius , _configData.MinSearchRadius);

        ChangePhase(MASTERAI_PHASE.DORMANT);
    }

    private void Update()
    {
        if ( _chaseAI == null || _playerTransform == null )
        {
            return;
        }

        float dist = Vector3.Distance(_playerTransform.position, _chaseAI.transform.position);

        _gaugeSystem.UpdateGauages(Time.deltaTime , dist , _chaseAI.IsChasing() , _currentPhase, _configData);

        _currentStateLogic?.Update(this);
    }

    #endregion

    #region State 관리 && State별 행동

    public void ChangePhase(MASTERAI_PHASE newPhase)
    {
        _currentStateLogic?.Exit(this);

        _currentPhase = newPhase;
        switch(newPhase)
        {
            case MASTERAI_PHASE.DORMANT:
                
                _currentStateLogic = _dormantState;
                break;

            case MASTERAI_PHASE.ACTIVE:

                _currentStateLogic = _activeState;
                break;
        }

        _currentStateLogic?.Enter(this);
    }

    public void OrderSpawn()
    {
        Debug.Log("[Master AI] 추격 AI 스폰 명령");
        ZoneInfo tSpawnZone = _zoneManager.GetRandomZoneNotPlayer(true);

        Vector3 spawnPos = (tSpawnZone != null && tSpawnZone.VentPoint != null)
            ? tSpawnZone.VentPoint.position
            : _manage_SearchPoint.CalculateVentPoint().position; // 벤트 못찾았을떼

        _chaseAI.Spawn(spawnPos);
    }

    //TODO : 더 똑똑한 명령 로직 필요 EX) 플레이어가 구석에 가만히 있으면 추격 AI가 같은 zone만 순찰돌고 있음 <- 수정필요
    public void OrderSearch()
    {
        Vector3 tTargetPos = _manage_SearchPoint.CalculateSearchPoint(_gaugeSystem.AlertRatio);

        if(tTargetPos != Vector3.zero)
        {
            _chaseAI.MoveToTarget(tTargetPos);
            Debug.Log($"[Director] 수색 명령: {tTargetPos} (경계도: {_gaugeSystem.AreaAlert:F0})");
        }
    }

    public void OrderRetreat()
    {
        ZoneInfo retreatZone = _zoneManager.GetRandomZoneNotPlayer(true);

        if ( retreatZone != null && retreatZone.VentPoint != null )
        {
            _chaseAI.OrderRetreat(retreatZone.VentPoint.position);
            Debug.Log($"[Director] 퇴근 명령 -> {retreatZone.name}");
        }
        else
        {
            _chaseAI.OrderRetreat(_manage_SearchPoint.CalculateVentPoint().position);
        }
    }

    // 추격 AI가 퇴근 완료했을때
    public void OnChaseAIVanish()
    {
        _currentPhase = MASTERAI_PHASE.DORMANT;
    }
    #endregion


    #region Zone 관리

    public void SetCurrentZone(ZoneInfo zone)
    {
        _zoneManager.SetPlayerZone(zone);
    }

    public void ClearCurrentZone(ZoneInfo zone)
    {
        _zoneManager.ClearPlayerZone(zone);
    }

    #endregion

    #region 외부 API && 이벤트

    // 소음 발생시
    public void ReportNoise(Vector3 noisePos , float loudness)
    {
        float hearingDistance = _configData.HearingDistanceMultiplier * loudness;
        float distToAI = Vector3.Distance(noisePos, _chaseAI.transform.position);

        if ( distToAI <= hearingDistance )
        {
            // 들림! -> 경계도 상승 및 조사 명령
            float increaseAmount = loudness * _configData.NoiseReactionMultiplier;
            _gaugeSystem.IncreaseAlert(increaseAmount, _configData);
            _gaugeSystem.IncreaseStress(increaseAmount * 0.2f, _configData);

            if ( _currentPhase == MASTERAI_PHASE.ACTIVE )
            {
                _chaseAI.InvestgateNoise(noisePos);
            }
        }
        else
        {
            // 안 들림 (너무 멂) -> 무시
            Debug.Log("소리가 났지만 AI가 못 들음");
        }
    }

    // 추격AI가 플레이어 발견시
    public void ReportPlayerContact()
    {
        _lastContactTime = Time.time;
        _gaugeSystem.OnPlayerContact(Time.deltaTime, _configData);
    }

    #endregion

    // Debug
    private void OnDrawGizmos()
    {
        //if ( _playerTransform == null ) return;

        //// 현재 적용 중인 가변 반경 그리기 (파란색 -> 빨간색 변함)
        //float currentRadius = Mathf.Lerp(_maxSearch, _minSearch, _gaugeSystem.AlertRatio);

        //Gizmos.color = Color.Lerp(Color.blue , Color.red , _gaugeSystem.AlertRatio);
        //Gizmos.color = new Color(Gizmos.color.r , Gizmos.color.g , Gizmos.color.b , 0.2f); // 반투명
        //Gizmos.DrawWireSphere(_playerTransform.position , currentRadius);

        //// 마지막 명령 위치
        //if ( _debugLastTargetPos != Vector3.zero )
        //{
        //    Gizmos.color = Color.red;
        //    Gizmos.DrawSphere(_debugLastTargetPos , 0.3f);
        //    Gizmos.DrawLine(_chaseAI.transform.position , _debugLastTargetPos);
        //}
    }
}
