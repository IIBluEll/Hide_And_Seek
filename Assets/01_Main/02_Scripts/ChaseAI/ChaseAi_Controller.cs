using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using System;
using System.Threading;

//TODO : 출근중, 퇴근중, 퇴근완료 상태 추가 필요
public enum CHASEAI_STATE
{
    IDLE,
    PATROL,
    CHASE,
    INVESTIGATE,
    RETREAT,
}

[RequireComponent(typeof(NavMeshAgent))]
public class ChaseAI_Controller : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CHASEAI_STATE _currentState = CHASEAI_STATE.IDLE;

    [Space(10f), Header("이동")]
    [SerializeField] private float _walkSpeed = 4f;
    [SerializeField] private float _runSpeed = 7f;
    [SerializeField] private float _idleWaitTime = 3f;

    [Space(10f), Header("시야")]
    [SerializeField] private float _sightRange = 15f;
    [SerializeField] private float _horizontalSightAngle = 120f;
    [SerializeField] private float _verticalSightAngle = 60f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private Transform _eyeTransform;

    private NavMeshAgent _agent;
    private Transform _targetPlayer;

    private CancellationTokenSource _waitCts;
    private bool _isWaiting = false;

    // Debug용 현재 상태 확인 프로퍼티
    public CHASEAI_STATE CurrentState => _currentState;

    #region Unity LifeCycle

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _walkSpeed;

        if (_eyeTransform == null)
        {
            _eyeTransform = transform;
        }
    }
    
    public void Initalize(Transform player)
    {
        _targetPlayer = player;
    }

    private void Update()
    {
        // 퇴근 중이거나 맵에 없으면 감각 처리 안 함
        if ( !gameObject.activeSelf) return;

        // 1. 시야 감지 (플레이어 발견 시 즉시 Chase 전환)
        if ( _targetPlayer != null )
        {
            DetectPlayer();
        }

        // 2. 상태별 행동
        switch ( _currentState )
        {
            case CHASEAI_STATE.PATROL:
            case CHASEAI_STATE.INVESTIGATE:
                CheckArrival(); // 목적지 도착 체크
                break;

            case CHASEAI_STATE.CHASE:
                ChaseUpdate(); // 추격 로직
                break;

            case CHASEAI_STATE.RETREAT:
                CheckRetreatArrival(); // 환기구 도착 체크
                break;
        }
    }

    private void OnDestroy()
    {
        CancelWait();
    }
    #endregion

    #region MasterAI에게 명령 수신 로직

    // 명령 받을 수 있는 상태 체크
    public bool IsAvailableForCommand()
    {
        return gameObject.activeSelf && _currentState == CHASEAI_STATE.IDLE && !_isWaiting;
    }

    //퇴근 중인지
    public bool IsRetreating()
    {
        return _currentState == CHASEAI_STATE.RETREAT;
    }

    // 추격 상태인지 체크
    public bool IsChasing()
    {
        return _currentState == CHASEAI_STATE.CHASE;
    }

    // 명령 받은 위치로 이동
    public void MoveToTarget(Vector3 targetPos)
    {
        if (!IsAvailableForCommand())
        {
            return;
        }

        MoveAgent(targetPos, _walkSpeed);
        ChangeState(CHASEAI_STATE.PATROL);
    }

    // 소음이 들렸을 때
    public void InvestgateNoise(Vector3 targetPos)
    {
        if (_currentState == CHASEAI_STATE.CHASE || _currentState == CHASEAI_STATE.RETREAT)
        {
            return;
        }

        Debug.Log("[Chase AI] 소음 감지!");
        CancelWait();

        MoveAgent(targetPos, _walkSpeed);
        ChangeState(CHASEAI_STATE.INVESTIGATE);
    }

    // 퇴근 명령 -> 벤트로 이동
    public void OrderRetreat(Vector3 ventPos)
    {
        if (_currentState == CHASEAI_STATE.CHASE || CurrentState == CHASEAI_STATE.RETREAT )
        {
            return;
        }

        Debug.Log("[Chase AI] 추격 AI 퇴근하러감");
        CancelWait();

        MoveAgent(ventPos, _walkSpeed);
        ChangeState(CHASEAI_STATE.RETREAT);
    }

    // 스폰
    public void Spawn(Vector3 position)
    {
        gameObject.SetActive(true);
        _agent.Warp(position);

        ChangeState(CHASEAI_STATE.IDLE);

        Debug.Log("[Chase AI] 스폰함");
    }

    // 퇴근
    public void Vanish()
    {
        MasterAI_Provider.Instance.OnChaseAIVanish();
        CancelWait();
        _agent.ResetPath();

        gameObject.SetActive(false);
        ChangeState(CHASEAI_STATE.IDLE);

        Debug.Log("[Chase AI] 추격 AI 퇴근 완료");
    }
    #endregion

    #region 내부 로직

    private void MoveAgent(Vector3 pos, float speed)
    {
        _agent.isStopped = false;
        _agent.speed = speed;
        _agent.SetDestination(pos);
    }

    private void ChangeState(CHASEAI_STATE newState)
    {
        _currentState = newState;
    }

    private void CheckArrival()
    {
        if (_agent.pathPending )
        {
            return;
        }

        if (_agent.remainingDistance <= _agent.stoppingDistance)
        {
            if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f)
            {
                // 도착 후 잠시 대기
                WaitAndSwitchToIdle_async().Forget();
            }
        }
    }

    // 퇴근시 벤트 도착 체크
    private void CheckRetreatArrival()
    {
        if (_agent.pathPending)
        {
            return;
        }

        //TODO : 추격AI가 벤트에 들어가는 애니메이션 또는 사운드 재생
        if (_agent.remainingDistance <= _agent.stoppingDistance)
        {
            Vanish();
        }
    }

    private async UniTaskVoid WaitAndSwitchToIdle_async()
    {
        if (_isWaiting)
        {
            return;
        }

        _isWaiting = true;

        //TODO : 추격AI가 두리번 또는 무언가 뒤지는 애니메이션
        Debug.Log("[Alien] 도착. 주위를 살피는 중...");

        _waitCts = new CancellationTokenSource();
        var linkCts = CancellationTokenSource.CreateLinkedTokenSource(_waitCts.Token, this.GetCancellationTokenOnDestroy());

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_idleWaitTime), cancellationToken: linkCts.Token);
            ChangeState(CHASEAI_STATE.IDLE);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _isWaiting = false;
            linkCts.Dispose();
            _waitCts = null;
        }
    }

    private void CancelWait()
    {
        if (_isWaiting && _waitCts != null)
        {
            _waitCts.Cancel();
            _isWaiting = false;
        }
    }

    #endregion

    #region  추격 시스템 로직

    // 플레이어가 시야에 있는지 체크
    private bool IsPlayerVisible()
    {
        if(_targetPlayer == null)
        {
            return false;
        }

        // 거리 체크
        float tDist = Vector3.Distance(transform.position, _targetPlayer.position);
        if( tDist > _sightRange )
        {
            return false;
        }

        // 시야각 체크
        Vector3 tTargetLocal = _eyeTransform.InverseTransformPoint(_targetPlayer.position);
        if( tTargetLocal.z < 0 )
        {
            return false;
        }

        float tAngleH = Mathf.Atan2(tTargetLocal.x, tTargetLocal.z) * Mathf.Rad2Deg;
        if ( Mathf.Abs(tAngleH) > _horizontalSightAngle * 0.5f ) return false;

        float tAngleV = Mathf.Atan2(tTargetLocal.y, tTargetLocal.z) * Mathf.Rad2Deg;
        if ( Mathf.Abs(tAngleV) > _verticalSightAngle * 0.5f ) return false;

        // 장애물 체크
        Vector3 tDir = (_targetPlayer.position - _eyeTransform.position).normalized;

        // 레이캐스트로 장애물 확인
        if ( Physics.Raycast(_eyeTransform.position, tDir , tDist , _obstacleMask))
        {
            return false;
        }

        return true;
    }

    private void DetectPlayer()
    {
        if ( IsPlayerVisible() )
        {
            // 발견
            if ( _currentState != CHASEAI_STATE.CHASE )
            {
                StartChase();
            }

            MasterAI_Provider.Instance.ReportPlayerContact();
        }
    }

    private void StartChase()
    {
        Debug.Log("!!! 플레이어 발견 !!! 추격 개시 !!!");
        CancelWait();
        ChangeState(CHASEAI_STATE.CHASE);
        MoveAgent(_targetPlayer.position , _runSpeed);
    }

    private void ChaseUpdate()
    {
        if ( _targetPlayer == null ) return;

        if ( IsPlayerVisible() )
        {
            // 플레이어가 시야에 있으면 계속 추격
            _agent.SetDestination(_targetPlayer.position);
        }
        else
        {
            // 시야에서 플레이어 놓침
            if(!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                if(!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f)
                {
                    Debug.Log("플레이어 놓침. 마지막 위치 수색 전환");
                    ChangeState(CHASEAI_STATE.INVESTIGATE);
                }
            }
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
        // 눈 위치가 없으면 아직 실행 전이므로 transform 사용, 있으면 _eyeTransform 사용
        Transform eye = (_eyeTransform == null) ? transform : _eyeTransform;
        Vector3 origin = eye.position;

        // 1. 기본 거리 범위 그리기 (연한 노란색 구체)
        Gizmos.color = new Color(1f , 1f , 0f , 0.2f); // 반투명 노랑
        Gizmos.DrawWireSphere(origin , _sightRange);

        // 2. 시야각 계산을 위한 준비
        Gizmos.color = Color.cyan; // 시야각은 하늘색으로 표시
        float halfH = _horizontalSightAngle * 0.5f;
        float halfV = _verticalSightAngle * 0.5f;
        Quaternion eyeRotation = eye.rotation;

        // 3. 네 귀퉁이의 방향 벡터 계산 (쿼터니언 회전 조합)
        // eyeRotation: 현재 눈의 방향
        // Quaternion.Euler(-halfV, -halfH, 0): 로컬 기준 위로 V도, 왼쪽으로 H도 회전
        Vector3 dirTL = eyeRotation * Quaternion.Euler(-halfV, -halfH, 0) * Vector3.forward; // Top-Left
        Vector3 dirTR = eyeRotation * Quaternion.Euler(-halfV, halfH, 0) * Vector3.forward; // Top-Right
        Vector3 dirBL = eyeRotation * Quaternion.Euler(halfV, -halfH, 0) * Vector3.forward; // Bottom-Left
        Vector3 dirBR = eyeRotation * Quaternion.Euler(halfV, halfH, 0) * Vector3.forward; // Bottom-Right

        // 4. 최대 거리 지점 좌표 계산
        Vector3 farTL = origin + dirTL * _sightRange;
        Vector3 farTR = origin + dirTR * _sightRange;
        Vector3 farBL = origin + dirBL * _sightRange;
        Vector3 farBR = origin + dirBR * _sightRange;

        // 5. 선 그리기
        // 5-1. 눈에서 네 귀퉁이로 뻗어나가는 레이(Ray)
        Gizmos.DrawLine(origin , farTL);
        Gizmos.DrawLine(origin , farTR);
        Gizmos.DrawLine(origin , farBL);
        Gizmos.DrawLine(origin , farBR);

        // 5-2. 끝부분을 연결하여 사각형 프레임 만들기
        Gizmos.DrawLine(farTL , farTR); // 상단 가로선
        Gizmos.DrawLine(farTR , farBR); // 우측 세로선
        Gizmos.DrawLine(farBR , farBL); // 하단 가로선
        Gizmos.DrawLine(farBL , farTL); // 좌측 세로선

        // 6. (선택사항) 중앙 정면 방향 표시 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin , eye.forward * _sightRange);
    }
}
