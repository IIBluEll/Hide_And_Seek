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
    [SerializeField] private ChaseAi_Controller _chaseAI;
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

    private float _timer = 0f;
    private float _lastContactTime = float.MinValue;

    private List<ZoneInfo> _allZones; 
    private ZoneInfo _playerCurrentZone;

    private Vector3 _debugLastTargetPos = Vector3.zero;
    private MASTERAI_PHASE _currentPhase = MASTERAI_PHASE.DORMANT;

    public float GlobalStress { get; private set; } = 0f;
    public float AreaAlert { get; private set; } = 0f;

    #region Unity LifeCycle

    private void Start()
    {
        _allZones = FindObjectsOfType<ZoneInfo>().ToList();

        if (_chaseAI != null && _playerTransform != null)
        {
            _chaseAI.Initalize(_playerTransform);
            _chaseAI.Vanish();
        }
    }

    private void Update()
    {
        if ( _chaseAI == null || _playerTransform == null)
        {
            return;
        }

        UpdateGauges();
        ProcessMasterLogic();
    }

    #endregion

    #region 긴장도 / 경계도 변수 로직

    private void UpdateGauges()
    {
        float tTime = Time.deltaTime;

        // 피로도 관리
        if(_currentPhase == MASTERAI_PHASE.ACTIVE)
        {
            float tDist = Vector3.Distance(_playerTransform.position, _chaseAI.transform.position);

            //TODO 현재 거리가 멀어도 스트레스가 증가중! 고쳐야함
            float tStress = Mathf.Clamp(20f - tDist, 0f, 20f) * 0.5f;
            GlobalStress += ( tStress + 1f ) * tTime;
        }
        else
        {
            if(GlobalStress > 0)
            {
                GlobalStress -= _stressDecreaseRate * tTime;
            }
        }

        // 경계도 관리
        if(!_chaseAI.IsChasing() && AreaAlert > 0)
        {
            AreaAlert -= _alertDecreaseRate * tTime;
        }

        // 값 범위
        GlobalStress = Mathf.Clamp(GlobalStress , 0 , 150f);
        AreaAlert = Mathf.Clamp(AreaAlert , 0f , 100f);
    }

    private void ProcessMasterLogic()
    {
        _timer += Time.deltaTime;

        if ( _currentPhase == MASTERAI_PHASE.DORMANT )
        {
            // 피로도, 경계도 없으면 다시 등장
            if ( GlobalStress <= 0f && GlobalStress <= 0 )
            {
                EnterActiveMode();
            }
        }
        else
        {
            // 퇴장 조건 - 스트레스가 기준치 이상, 추격 중 아님, 마지막 추격 후 10초 이상
            bool isSafeTime = (Time.time - _lastContactTime) > 10f;

            if ( GlobalStress >= _maxStressThreshold && AreaAlert <= 0f && !_chaseAI.IsChasing() && isSafeTime )
            {
                EnterDormantMode();
            }
            else if ( _timer >= _commandInterval && _chaseAI.IsAvailableForCommand() )
            {
                _timer = 0;
                GiveNextSearchCommand();
            }
        }
    }
    #endregion

    #region 좌표 구하기 && 명령 로직

    private void GiveNextSearchCommand()
    {
        Vector3 tTargetPos = CalculateTacticalPoint();

        if ( tTargetPos != Vector3.zero )
        {
            _debugLastTargetPos = tTargetPos;
            _chaseAI.MoveToTarget(tTargetPos);
            Debug.Log($"[Director] 수색 명령: {tTargetPos} (경계도: {AreaAlert:F0})");
        }
    }

    private Vector3 CalculateTacticalPoint()
    {
        float tCurrentRadius = Mathf.Lerp(_maxSearch,_minSearch, AreaAlert/100);

        //플레이어 주변에 있는 zone 검사
        var tHitCollider = Physics.OverlapSphere(_playerTransform.position, tCurrentRadius, _zoneLayerMask);

        if ( tHitCollider.Length > 0 )
        {
            // zone들 중 하나 랜덤 선택
            var tRandomCol = tHitCollider[Random.Range(0, tHitCollider.Length)];
            ZoneInfo tSelectZone = tRandomCol.GetComponent<ZoneInfo>();

            // 선택된 zone 내부 수색 포인트 반환
            if ( tSelectZone != null )
            {
                if ( AreaAlert > 50f )
                {
                    // 경계도가 높으면 은신처 수색
                    return tSelectZone.GetNearHidingSpot(_playerTransform.position);
                }

                return tSelectZone.GetRandomSearchPoint();
            }
        }

        // zone이 없을 경우 랜덤 좌표
        return CalculateRandomPoint(tCurrentRadius);
    }

    // 플레이어 기준 랜덤 좌표
    private Vector3 CalculateRandomPoint(float radius)
    {
        Vector2 tRandomCircle = Random.insideUnitCircle.normalized * radius;
        Vector3 tTargetPos = _playerTransform.position + new Vector3(tRandomCircle.x,0,tRandomCircle.y);

        NavMeshHit tHit;
        if(NavMesh.SamplePosition(tTargetPos, out tHit, 2.0f, NavMesh.AllAreas))
        {
            return tHit.position;
        }

        //TODO 지금 힌트를 제대로 제공하지 못해 플레이어 좌표 반환중
        Debug.Log("좌표 구하기 실패! 플레이어 좌표 반환");
        return _playerTransform.position;
    }

    #endregion

    #region 상태 전환 로직

    private void EnterActiveMode()
    {
        Debug.Log("[Master AI] 추격 AI 활성화");

        Vector3 tSpawnPos = Vector3.zero;
        ZoneInfo tSpwanZone = GetTacticalSpawnZone();

        if(tSpwanZone != null)
        {
            tSpawnPos = tSpwanZone.GetRandomSpawnPoint();
        }
        else
        {
            tSpawnPos = CalculateSpawnPos();
        }

        _currentPhase = MASTERAI_PHASE.ACTIVE;
        _chaseAI.Spawn(tSpawnPos);
    }

    private void EnterDormantMode()
    {
        //TODO : 나중에 환기구 찾아서 가게끔 구현해야됨
        _chaseAI.Vanish();

        _currentPhase = MASTERAI_PHASE.DORMANT;
        GlobalStress = 0f;
        _timer = 0f;
    }

    // 스폰하기 위한 근처 zone 찾기
    private ZoneInfo GetTacticalSpawnZone()
    {
        var candidateZones = _allZones.Where(zone =>
        {
            if (zone == _playerCurrentZone) return false; // 현재 방 제외

            float dist = Vector3.Distance(zone.transform.position, _playerTransform.position);
            return dist >= 15.0f && dist <= 35.0f; // 적절한 거리
        }).ToList();

        if ( candidateZones.Count > 0 )
        {
            // 후보 중 랜덤 선택
            return candidateZones[ Random.Range(0 , candidateZones.Count) ];
        }

        return null; // 적절한 Zone을 못 찾음
    }

    // 스폰하기 위한 zone이 없을 경우
    private Vector3 CalculateSpawnPos()
    {
        // 플레이어 뒤쪽 20m 지점을 기준으로
        Vector3 potentialPos = _playerTransform.position - _playerTransform.forward * 20f;

        NavMeshHit hit;
        // NavMesh 위의 점인지 엄격하게 체크 (반경 5m 내)
        if ( NavMesh.SamplePosition(potentialPos , out hit , 5.0f , NavMesh.AllAreas) )
        {
            return hit.position;
        }

        // 정말 갈 데가 없으면 플레이어 위치에서 10m 떨어진 아무 NavMesh 위
        if ( NavMesh.SamplePosition(_playerTransform.position + Random.onUnitSphere * 10f , out hit , 10f , NavMesh.AllAreas) )
        {
            return hit.position;
        }

        // 최악의 경우: 그냥 에이리언 있던 자리 (에러 방지)
        return _chaseAI.transform.position;
    }
    #endregion

    #region 외부 API && 이벤트

    // 소음 발생시
    public void ReportNoise(Vector3 noisePos, float loudness)
    {
        _lastContactTime = Time.time;

        float tIncreaseAmount = loudness * 30f;
        AreaAlert += tIncreaseAmount;
        GlobalStress += tIncreaseAmount * 0.2f;

        // TODO : 나중에 소음은 추격 AI가 보고만 하기
        if(_currentPhase == MASTERAI_PHASE.ACTIVE)
        {
            _chaseAI.InVestigateNoise(noisePos);
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
        if ( _playerTransform == null ) return;

        // 현재 적용 중인 가변 반경 그리기 (파란색 -> 빨간색 변함)
        float currentRadius = Mathf.Lerp(_maxSearch, _minSearch, AreaAlert / 100f);
        Gizmos.color = Color.Lerp(Color.blue , Color.red , AreaAlert / 100f);
        Gizmos.color = new Color(Gizmos.color.r , Gizmos.color.g , Gizmos.color.b , 0.2f); // 반투명
        Gizmos.DrawWireSphere(_playerTransform.position , currentRadius);

        // 마지막 명령 위치
        if ( _debugLastTargetPos != Vector3.zero )
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_debugLastTargetPos , 0.3f);
            Gizmos.DrawLine(_chaseAI.transform.position , _debugLastTargetPos);
        }
    }
}
