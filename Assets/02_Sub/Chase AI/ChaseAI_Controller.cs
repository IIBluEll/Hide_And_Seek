using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AI;

public enum AI_STATE
{
    PATROL,
    INVESTIGATE,
    CHASE,
    SEARCH
}

public class ChaseAI_Controller : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private Transform _playerTrans;
    [SerializeField] private Transform[] _arr_patrolPoints;

    // ★[추가] 마스터 AI가 힌트 줄 Zone 정보
    [Header("Zone 정보 (Master AI 힌트용)")]
    [SerializeField] private ZoneArea[] _arr_zoneAreas; // zoneId -> 위치 매핑

    [Header("수색 / 의심 관련")]
    public float SearchDurationMin = 4f;
    public float SearchDurationMax = 12f;
    public float SearchRadiusMin = 2f;
    public float SearchRadiusMax = 8f;
    public float SuspicionIncreaseOnLoseSight = 0.3f;
    public float SuspicionDecayPerSecond = 0.05f;

    [Header("디버그 옵션")]
    public bool DrawLastKnownPositionGizmo = true;

    private NavMeshAgent _agent;
    private ChaseAI_Eye _perception;

    private AI_STATE _state = AI_STATE.PATROL;

    private int _currentPatrolIndex;
    private Vector3 _lastKnownPosition;
    private float _searchTimer;
    private float _suspicionLevel; // 0~1

    // ★[추가] 마스터 AI 연동용 플래그
    private bool _isRestMode = false;
    private bool _isCloseToPlayerFlag = false;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _perception = GetComponent<ChaseAI_Eye>();

        if ( _playerTrans == null )
        {
            tFindPlayer();
        }

        tInitPatrol();
    }

    private void Update()
    {
        tUpdateSuspicion();

        // ★[추가] 휴식 모드 처리 + NavMesh 정지
        if ( _isRestMode )
        {
            if ( _agent != null )
            {
                _agent.isStopped = true;
            }

            UpdateCloseToPlayerFlag(); // 긴장도용 "가까움" 플래그는 계속 보고
            return;
        }
        else
        {
            if ( _agent != null )
            {
                _agent.isStopped = false;
            }
        }

        // ★[추가] 마스터 AI에 "플레이어와 가까운지" 상태 보고
        UpdateCloseToPlayerFlag();

        switch ( _state )
        {
            case AI_STATE.PATROL:
                UpdatePatrol();
                break;
            case AI_STATE.INVESTIGATE:
                UpdateInvestigate();
                break;
            case AI_STATE.CHASE:
                UpdateChase();
                break;
            case AI_STATE.SEARCH:
                UpdateSearch();
                break;
        }
    }

    private void tFindPlayer()
    {
        GameObject tPlayerObj = GameObject.FindGameObjectWithTag("Player");
        if ( tPlayerObj != null )
        {
            _playerTrans = tPlayerObj.transform;
            Debug.Log("[AI] Player Transform 자동 획득 완료");
        }
        else
        {
            Debug.Log("[AI] Player 찾기 실패: 'Player' 태그 오브젝트 없음");
        }
    }

    private void tInitPatrol()
    {
        if ( _arr_patrolPoints != null && _arr_patrolPoints.Length > 0 )
        {
            _currentPatrolIndex = 0;
            _agent.SetDestination(_arr_patrolPoints[ _currentPatrolIndex ].position);
        }
    }

    private void tUpdateSuspicion()
    {
        if ( _suspicionLevel > 0f )
        {
            _suspicionLevel -= SuspicionDecayPerSecond * Time.deltaTime;
            if ( _suspicionLevel < 0f )
            {
                _suspicionLevel = 0f;
            }
        }
    }

    // ★[변경] 상태 전환 시 MasterAI에 CHASE on/off 보고
    private void SetState(AI_STATE nextState)
    {
        if ( _state == nextState )
        {
            return;
        }

        bool tWasChasing = ( _state == AI_STATE.CHASE );
        bool tWillChasing = ( nextState == AI_STATE.CHASE );

        Debug.Log($"[AI] State: {_state} -> {nextState}");
        _state = nextState;

        // ★[추가] CHASE 진입/탈출 시 긴장도용 플래그 보고
        if ( tWasChasing != tWillChasing && MasterAI_Provider.Instance != null )
        {
            MasterAI_Provider.Instance.ReportEnemyChaseStateChanged(tWillChasing);
        }

        if ( nextState == AI_STATE.SEARCH )
        {
            _searchTimer = 0f;
        }
    }

    private bool tCanSeePlayer()
    {
        if ( _playerTrans == null )
        {
            return false;
        }

        return _perception.CanSee(_playerTrans);
    }

    // ===== 상태별 로직 =====

    private void UpdatePatrol()
    {
        if ( tCanSeePlayer() )
        {
            _lastKnownPosition = _playerTrans.position;
            _agent.SetDestination(_playerTrans.position);
            SetState(AI_STATE.CHASE);
            return;
        }

        if ( _arr_patrolPoints == null || _arr_patrolPoints.Length == 0 )
        {
            return;
        }

        if ( !_agent.pathPending && _agent.remainingDistance < 0.3f )
        {
            _currentPatrolIndex = ( _currentPatrolIndex + 1 ) % _arr_patrolPoints.Length;
            Vector3 tNextPos = _arr_patrolPoints[_currentPatrolIndex].position;
            _agent.SetDestination(tNextPos);
        }
    }

    private void UpdateChase()
    {
        if ( _playerTrans == null )
        {
            SetState(AI_STATE.SEARCH);
            return;
        }

        if ( tCanSeePlayer() )
        {
            _lastKnownPosition = _playerTrans.position;
            _agent.SetDestination(_lastKnownPosition);
        }
        else
        {
            // 시야에서 놓친 상태
            if ( _agent.remainingDistance < 0.5f )
            {
                _suspicionLevel = Mathf.Clamp01(_suspicionLevel + SuspicionIncreaseOnLoseSight);
                SetState(AI_STATE.SEARCH);
            }
        }
    }

    private void UpdateInvestigate()
    {
        if ( _playerTrans != null && tCanSeePlayer() )
        {
            _lastKnownPosition = _playerTrans.position;
            _agent.SetDestination(_lastKnownPosition);
            SetState(AI_STATE.CHASE);
            return;
        }

        if ( _agent.remainingDistance < 0.5f )
        {
            SetState(AI_STATE.SEARCH);
        }
    }

    private void UpdateSearch()
    {
        if ( _playerTrans != null && tCanSeePlayer() )
        {
            _lastKnownPosition = _playerTrans.position;
            _agent.SetDestination(_lastKnownPosition);
            SetState(AI_STATE.CHASE);
            return;
        }

        _searchTimer += Time.deltaTime;

        float tDuration = Mathf.Lerp(SearchDurationMin , SearchDurationMax , _suspicionLevel);
        if ( _searchTimer > tDuration )
        {
            SetState(AI_STATE.PATROL);
            tInitPatrol();
            return;
        }

        if ( !_agent.pathPending && _agent.remainingDistance < 0.3f )
        {
            float tRadius = Mathf.Lerp(SearchRadiusMin , SearchRadiusMax , _suspicionLevel);

            Vector3 tRandomOffset = Random.insideUnitSphere * tRadius;
            tRandomOffset.y = 0f;
            Vector3 tCandidate = _lastKnownPosition + tRandomOffset;

            NavMeshHit tHit;
            if ( NavMesh.SamplePosition(tCandidate , out tHit , 2f , NavMesh.AllAreas) )
            {
                _agent.SetDestination(tHit.position);
            }
        }
    }

    // ===== 청각(소리) 진입점 =====

    public void OnHeardNoise(Vector3 noisePos , float priority)
    {
        if ( _state == AI_STATE.CHASE && _playerTrans != null && tCanSeePlayer() )
        {
            return;
        }

        _lastKnownPosition = noisePos;
        _agent.SetDestination(noisePos);

        Debug.Log($"[AI] 소리 감지, INVESTIGATE 상태로 전환. pos={noisePos}, prio={priority}");
        SetState(AI_STATE.INVESTIGATE);
    }

    // ===== 마스터 AI 연동용 추가 메서드 =====

    /// <summary>
    /// ★[추가] MasterAI가 zoneId 힌트를 줬을 때 호출됨.
    /// - 휴식 모드면 무시
    /// - 이미 CHASE 중이면 무시
    /// </summary>
    public void SetInvestigationTarget(int zoneId)
    {
        if ( _isRestMode )
        {
            return;
        }

        if ( _state == AI_STATE.CHASE )
        {
            return;
        }

        if ( !TryGetZoneCenterPosition(zoneId , out Vector3 tCenterPos) )
        {
            Debug.LogWarning($"[AI] SetInvestigationTarget: zoneId={zoneId} 에 해당하는 ZoneArea 없음");
            return;
        }

        _lastKnownPosition = tCenterPos;
        _agent.SetDestination(tCenterPos);
        Debug.Log($"[AI] MasterAI 힌트 수신: zoneId={zoneId}, pos={tCenterPos}");
        SetState(AI_STATE.INVESTIGATE);
    }

    /// <summary>
    /// ★[추가] MasterAI가 긴장도 과열 시/해제 시 호출하는 휴식 모드.
    /// </summary>
    public void SetRestMode(bool isRest)
    {
        _isRestMode = isRest;

        if ( _agent != null )
        {
            _agent.isStopped = isRest;
        }

        if ( isRest )
        {
            // 쉬는 동안 로컬 의심도 / 수색 타이머 리셋
            _suspicionLevel = 0f;
            _searchTimer = 0f;
            Debug.Log("[AI] RestMode ON");
        }
        else
        {
            Debug.Log("[AI] RestMode OFF");
            // 다시 기본 패트롤로 시작
            SetState(AI_STATE.PATROL);
            tInitPatrol();
        }
    }

    /// <summary>
    /// ★[추가] MasterAI.CloseDistanceForTension 기준으로
    /// "플레이어와 가까운 상태" on/off를 판단하고, 변화 시에만 보고.
    /// </summary>
    private void UpdateCloseToPlayerFlag()
    {
        if ( _playerTrans == null || MasterAI_Provider.Instance == null )
        {
            return;
        }

        float tDistance = Vector3.Distance(transform.position , _playerTrans.position);
        float tCloseDist = 15f;

        bool tIsNowClose = ( tDistance <= tCloseDist );
        if ( tIsNowClose == _isCloseToPlayerFlag )
        {
            return;
        }

        _isCloseToPlayerFlag = tIsNowClose;
        MasterAI_Provider.Instance.ReportEnemyCloseToPlayerChanged(_isCloseToPlayerFlag);
    }

    /// <summary>
    /// ★[추가] zoneId -> ZoneArea 중심좌표 변환
    /// </summary>
    private bool TryGetZoneCenterPosition(int zoneId , out Vector3 centerPos)
    {
        centerPos = transform.position;

        if ( _arr_zoneAreas == null || _arr_zoneAreas.Length == 0 )
        {
            return false;
        }

        for ( int i = 0; i < _arr_zoneAreas.Length; i++ )
        {
            ZoneArea tZone = _arr_zoneAreas[i];
            if ( tZone == null )
            {
                continue;
            }

            if ( tZone.ZoneId == zoneId )
            {
                centerPos = tZone.transform.position;
                return true;
            }
        }

        return false;
    }

    // ===== 디버그 Gizmo =====

    private void OnDrawGizmosSelected()
    {
        if ( !DrawLastKnownPositionGizmo )
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_lastKnownPosition , 0.3f);
    }
}
