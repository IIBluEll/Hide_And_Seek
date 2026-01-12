using HM.CodeBase;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public enum MASTERAI_PHASE
{
    ACTIVE,
    DORMANT,
}

public class MasterAI_Provider : ASingletone<MasterAI_Provider>
{
    [Header("참조")]
    [SerializeField] private ChaseAI_Controller _chaseAI;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private LayerMask _zoneLayerMask;

    [Space(10f), Header("수색 반경")]
    [SerializeField] private float _maxSearch = 25f;
    [SerializeField] private float _minSearch = 5f;

    [Header("지표 수치 변수")]
    private float _stressDecreaseRate = 2f;     // 피로도 감소
    private float _alertDecreaseRate = 5f;      // 경계도 감소
    private float _maxStressThreshold = 100f;   // 퇴근 기준 피로도
    private float _commandInterval = 5f;        // 추격AI에게 명령 내리는 속도
    [SerializeField] private float _respawnCooldown = 15f; // 재등장 쿨타임

    private float _timer = 0f;
    private float _lastContactTime = float.MinValue;

    private List<ZoneInfo> _allZones;
    private ZoneInfo _playerCurrentZone;

    private Vector3 _debugLastTargetPos = Vector3.zero;
    private MASTERAI_PHASE _currentPhase = MASTERAI_PHASE.DORMANT;

    public float GlobalStress { get; private set; } = 0f;
    public float AreaAlert { get; private set; } = 0f;

    //Debug
    public MASTERAI_PHASE CurrentPhase => _currentPhase;

    public Managae_CalculatePoint Manage_SearchPoint;

    #region Unity LifeCycle

    private void Start()
    {
        _allZones = FindObjectsOfType<ZoneInfo>().ToList();

        if (_chaseAI != null && _playerTransform != null)
        {
            _chaseAI.Initalize(_playerTransform);
            _chaseAI.Vanish();
        }

        Manage_SearchPoint = new Managae_CalculatePoint(_chaseAI.transform , _playerTransform, _zoneLayerMask, _maxSearch, _minSearch);
    }

    private void Update()
    {
        if (_chaseAI == null || _playerTransform == null)
        {
            return;
        }

        UpdateGauges();
        ProcessMasterLogic();
    }

    #endregion

    public void SetCurrentZone(ZoneInfo zone)
    {
        _playerCurrentZone = zone;
    }

    public void ClearCurrentZone(ZoneInfo zone)
    {
        if (_playerCurrentZone == zone)
        {
            _playerCurrentZone = null;
        }
    }


    #region 긴장도 / 경계도 변수 로직

    private void UpdateGauges()
    {
        float tTime = Time.deltaTime;

        // 피로도 관리
        if (_currentPhase == MASTERAI_PHASE.ACTIVE)
        {
            float tDist = Vector3.Distance(_playerTransform.position, _chaseAI.transform.position);
            float tSafeDist = 20f;

            // 거리에 따른 피로도 변화
            if (tDist > tSafeDist)
            {
                GlobalStress -= _stressDecreaseRate * 0.5f * tTime;
            }
            else
            {
                float tTooClose = (tSafeDist - tDist) * 0.8f;
                GlobalStress += tTooClose * tTime;
            }
        }
        else
        {
            if (GlobalStress > 0)
            {
                GlobalStress -= _stressDecreaseRate * tTime;
            }
        }

        // 경계도 관리
        if (!_chaseAI.IsChasing() && AreaAlert > 0)
        {
            AreaAlert -= _alertDecreaseRate * tTime;
        }

        // 값 범위
        GlobalStress = Mathf.Clamp(GlobalStress, 0, 150f);
        AreaAlert = Mathf.Clamp(AreaAlert, 0f, 100f);
    }

    private void ProcessMasterLogic()
    {
        _timer += Time.deltaTime;

        if (_currentPhase == MASTERAI_PHASE.DORMANT)
        {
            // 피로도, 경계도 없으면 다시 등장
            // 쿨타임 조건 추가 (_timer >= _respawnCooldown)
            if (GlobalStress <= 0f && AreaAlert <= 0f && _timer >= _respawnCooldown)
            {
                EnterActiveMode();
            }
        }
        else
        {
            // 퇴장 조건 - 스트레스가 기준치 이상, 추격 중 아님, 마지막 추격 후 10초 이상
            bool isSafeTime = (Time.time - _lastContactTime) > 10f;

            if (GlobalStress >= _maxStressThreshold && AreaAlert <= 0f && !_chaseAI.IsChasing() && isSafeTime)
            {
                EnterDormantMode();
            }
            else if (_timer >= _commandInterval && _chaseAI.IsAvailableForCommand())
            {
                _timer = 0;
                GiveNextSearchCommand();
            }
        }
    }
    #endregion

    #region 좌표 구하기 && 명령 로직

    //TODO : 더 똑똑한 명령 로직 필요 EX) 플레이어가 구석에 가만히 있으면 추격 AI가 같은 zone만 순찰돌고 있음 <- 수정필요
    private void GiveNextSearchCommand()
    {
        //Vector3 tTargetPos = CalculateTacticalPoint();

        Vector3 tTargetPos = Manage_SearchPoint.CalculateSearchPoint(AreaAlert / 100f);

        if (tTargetPos != Vector3.zero)
        {
            _debugLastTargetPos = tTargetPos;
            _chaseAI.MoveToTarget(tTargetPos);
            Debug.Log($"[Director] 수색 명령: {tTargetPos} (경계도: {AreaAlert:F0})");
        }
    }
    #endregion

    #region 상태 전환 로직

    private void EnterActiveMode()
    {
        Debug.Log("[Master AI] 추격 AI 활성화");

        Vector3 tSpawnPos = Manage_SearchPoint.CalculateVentPoint().position;

        _currentPhase = MASTERAI_PHASE.ACTIVE;
        _chaseAI.Spawn(tSpawnPos);
    }

    private void EnterDormantMode()
    {
        //TODO : 각 존의 환기구 좌표 중에서 가장 가까운 곳으로 퇴근 명령
        Vector3 retreatPos = Manage_SearchPoint.CalculateVentPoint().position;
        
        _chaseAI.OrderRetreat(retreatPos);

        Debug.Log($"[Director] 퇴근 명령 하달 -> 목표: {retreatPos}");
    }

    // 추격 AI가 퇴근 완료했을때
    public void OnChaseAIVanish()
    {
        _currentPhase = MASTERAI_PHASE.DORMANT;
        GlobalStress = 0f;
        _timer = 0f;
    }
    #endregion

    #region 외부 API && 이벤트

    // 소음 발생시
    public void ReportNoise(Vector3 noisePos, float loudness)
    {
        float hearingDistance = 20.0f * loudness;
        float distToAI = Vector3.Distance(noisePos, _chaseAI.transform.position);

        if ( distToAI <= hearingDistance )
        {
            // 들림! -> 경계도 상승 및 조사 명령
            float increaseAmount = loudness * 30f;
            AreaAlert += increaseAmount;
            GlobalStress += increaseAmount * 0.2f;

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
        AreaAlert = 100f;
        GlobalStress += 10f * Time.deltaTime;
    }

    #endregion

    // Debug
    private void OnDrawGizmos()
    {
        if (_playerTransform == null) return;

        // 현재 적용 중인 가변 반경 그리기 (파란색 -> 빨간색 변함)
        float currentRadius = Mathf.Lerp(_maxSearch, _minSearch, AreaAlert / 100f);
        Gizmos.color = Color.Lerp(Color.blue, Color.red, AreaAlert / 100f);
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f); // 반투명
        Gizmos.DrawWireSphere(_playerTransform.position, currentRadius);

        // 마지막 명령 위치
        if (_debugLastTargetPos != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_debugLastTargetPos, 0.3f);
            Gizmos.DrawLine(_chaseAI.transform.position, _debugLastTargetPos);
        }
    }
}
