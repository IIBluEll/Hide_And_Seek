using UnityEngine;
using UnityEngine.AI;

public enum AI_STATE
{
    PATROL,
    INVESTIGATE, // 소리 난 곳으로 이동
    CHASE,       // 플레이어 추격
    SEARCH       // 마지막 목격 지점 주변 수색
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AIPerception_view))]
public class EnemyAIController : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private Transform _playerTrans;
    [SerializeField] private Transform[] _arr_patrolPoints;

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
    private AIPerception_view _perception;
    private MasterAIProvider _masterAI; // ★ 마스터 AI 참조

    private AI_STATE _state = AI_STATE.PATROL;

    private int _currentPatrolIndex;
    private Vector3 _lastKnownPosition;
    private float _searchTimer;
    private float _suspicionLevel; // 0~1

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _perception = GetComponent<AIPerception_view>();

        if ( _playerTrans == null )
        {
            tFindPlayer();
        }

        tInitPatrol();
        DevConsoleProvider.Log("[AI] EnemyAIController 초기화 완료");
    }

    private void OnEnable()
    {
        // ★ 마스터 AI 이벤트 구독
        if ( MasterAIProvider.Instance != null )
        {
            _masterAI = MasterAIProvider.Instance;
            _masterAI.OnRoughHintUpdated += OnMasterRoughHint;
            _masterAI.OnExactPositionReported += OnMasterExactPosition;
        }
        else
        {
            DevConsoleProvider.Log("[AI] MasterAIProvider 없음 → 단독 AI로 동작");
        }
    }

    private void OnDisable()
    {
        if ( _masterAI != null )
        {
            _masterAI.OnRoughHintUpdated -= OnMasterRoughHint;
            _masterAI.OnExactPositionReported -= OnMasterExactPosition;
        }
    }

    private void Update()
    {
        tUpdateSuspicion();

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
            DevConsoleProvider.Log("[AI] Player Transform 자동 획득 완료");
        }
        else
        {
            DevConsoleProvider.Log("[AI] Player 찾기 실패: 'Player' 태그 오브젝트 없음");
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

    private void SetState(AI_STATE nextState)
    {
        if ( _state == nextState )
        {
            return;
        }

        DevConsoleProvider.Log($"[AI] State: {_state} -> {nextState}");
        _state = nextState;

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

    // ===== Master AI 이벤트 핸들러 =====

    private void OnMasterRoughHint(Vector3 hintPos)
    {
        // 이미 시야로 추격 중이면 두리뭉술 힌트는 무시
        if ( _state == AI_STATE.CHASE && _playerTrans != null && tCanSeePlayer() )
        {
            return;
        }

        _lastKnownPosition = hintPos;
        _agent.SetDestination(hintPos);
        SetState(AI_STATE.INVESTIGATE);
    }

    private void OnMasterExactPosition(Vector3 exactPos)
    {
        _lastKnownPosition = exactPos;
        _agent.SetDestination(exactPos);
        SetState(AI_STATE.CHASE);
    }

    private void tReportExactToMaster(Vector3 playerPos)
    {
        if ( _masterAI == null )
        {
            return;
        }

        // MasterAI 쪽에 Vector3 기반 오버로드 추가해서 사용
        _masterAI.ReportExactPlayerPosition(playerPos);
    }

    private void tReportNoiseToMaster(Vector3 noisePos , float priority)
    {
        if ( _masterAI == null )
        {
            return;
        }

        _masterAI.ReportNoise(noisePos , priority);
    }

    // ===== 상태별 로직 =====

    private void UpdatePatrol()
    {
        // 1) 로컬 시야로 플레이어 발견
        if ( tCanSeePlayer() )
        {
            _lastKnownPosition = _playerTrans.position;
            _agent.SetDestination(_playerTrans.position);

            // ★ 마스터에게 "정확 위치" 보고
            tReportExactToMaster(_lastKnownPosition);

            SetState(AI_STATE.CHASE);
            return;
        }

        // 2) 그냥 순찰
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

            // ★ 추격 중에도 계속 마스터에게 위치 갱신
            tReportExactToMaster(_lastKnownPosition);
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
        // 조사 도중 직접 플레이어를 보면 바로 추격으로
        if ( _playerTrans != null && tCanSeePlayer() )
        {
            _lastKnownPosition = _playerTrans.position;
            _agent.SetDestination(_lastKnownPosition);
            tReportExactToMaster(_lastKnownPosition);
            SetState(AI_STATE.CHASE);
            return;
        }

        // 지정된 조사 지점에 도착하면 SEARCH로 전환
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
            tReportExactToMaster(_lastKnownPosition);
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

        // 마지막 목격 지점 주변을 랜덤 수색
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
        // 이미 플레이어를 직접 보면서 추격 중이면 소리 무시
        if ( _state == AI_STATE.CHASE && _playerTrans != null && tCanSeePlayer() )
        {
            return;
        }

        _lastKnownPosition = noisePos;
        _agent.SetDestination(noisePos);

        // ★ 마스터에게 소리 위치 보고 (다른 추격 AI용)
        tReportNoiseToMaster(noisePos , priority);

        DevConsoleProvider.Log($"[AI] 소리 감지, INVESTIGATE 상태로 전환. pos={noisePos}, prio={priority}");
        SetState(AI_STATE.INVESTIGATE);
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
