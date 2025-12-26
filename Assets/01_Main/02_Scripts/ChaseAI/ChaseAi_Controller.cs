using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using System;
using System.Threading;

public enum CHASEAI_STATE
{
    IDLE,
    PATROL,
    CHASE,
    INVESTIGATE,
}

[RequireComponent(typeof(NavMeshAgent))]
public class ChaseAi_Controller : MonoBehaviour
{
    [Header("현재 상태")]
    [SerializeField] private CHASEAI_STATE _currentState = CHASEAI_STATE.IDLE;

    [Header("세팅")]
    [SerializeField] private float _idleWaitTime = 3f;

    [Space(10f),Header("시야 설정")]
    [SerializeField] private float _sightRange = 15.0f;
    [SerializeField] private float _horizontalSightAngle = 120.0f; // 좌우 시야각 
    [SerializeField] private float _verticalSightAngle = 60.0f;    // 위아래 시야각 
    [SerializeField] private LayerMask _obstacleMask;   
    [SerializeField] private Transform _eyeTransform;   

    private NavMeshAgent _agent;
    private Transform _targetPlayer;

    private bool _isWaiting = false;

    private CancellationTokenSource _waitCts;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = 3.5f;
        _agent.angularSpeed = 120f;

        if ( _eyeTransform == null )
        {
            _eyeTransform = transform;
        }
    }

    private void OnDestroy()
    {
        _waitCts?.Cancel();
        _waitCts?.Dispose();
    }

    public void Initalize(Transform player)
    {
        _targetPlayer = player;
    }

    private void Update()
    {
        if ( _targetPlayer != null )
        {
            DetectPlayer();
        }

        switch (_currentState)
        {
            case CHASEAI_STATE.PATROL:
            case CHASEAI_STATE.INVESTIGATE:

                CheckArrival();
                break;

            case CHASEAI_STATE.CHASE:

                ChaseUpdate();
                break;
        }
    }

    private void ChangeState(CHASEAI_STATE state)
    {
        _currentState = state;
    }

    public bool IsAvailableForCommand()
    {
        return _currentState == CHASEAI_STATE.IDLE && !_isWaiting;
    }
    public bool IsChasing()
    {
        return _currentState == CHASEAI_STATE.CHASE;
    }

    // 소리 듣고 조사
    public void InVestigateNoise(Vector3 targetPos)
    {
        if ( _currentState == CHASEAI_STATE.CHASE )
        {
            return;
        }

        Debug.Log($"[Alien] 소리가 들렸다! {targetPos} 확인하러 간다.");

        if ( _isWaiting && _waitCts != null )
        {
            _waitCts.Cancel();
            _isWaiting = false;
        }

        _agent.SetDestination(targetPos);
        _agent.isStopped = false;
        _agent.speed = 4f;

        ChangeState(CHASEAI_STATE.INVESTIGATE);
    }

    public void MoveToTarget(Vector3 targetPos)
    {
        if ( _currentState == CHASEAI_STATE.CHASE )
        {
            return;
        }

        _agent.SetDestination(targetPos);
        _agent.isStopped = false;
        _agent.speed = 3.5f;

        ChangeState(CHASEAI_STATE.PATROL);
        Debug.Log($"[Alien] 이동 시작: {targetPos}");
    }

    private void StartChase()
    {
        Debug.Log("!!! 발견 !!! 추격 시작 !!!");

        if ( _isWaiting && _waitCts != null )
        {
            _waitCts.Cancel();
            _isWaiting = false;
        }

        ChangeState(CHASEAI_STATE.CHASE);
        _agent.speed = 5.0f;
    }

    private void CheckArrival()
    {
        if(_agent.pathPending)
        {
            return;
        }

        if(_agent.remainingDistance <= _agent.stoppingDistance)
        {
            if(_agent.hasPath || _agent.velocity.sqrMagnitude == 0f)
            {
                WaitAndSwitchToIdle_async().Forget();
            }
        }
    }

    private async UniTaskVoid WaitAndSwitchToIdle_async()
    {
        if ( _isWaiting )
        {
            return;
        }

        _isWaiting = true;
        Debug.Log("[Alien] 목적지 도착. 주위를 살피는 중...");

        _waitCts = new CancellationTokenSource();
        var linkCts = CancellationTokenSource.CreateLinkedTokenSource(_waitCts.Token, this.GetCancellationTokenOnDestroy());

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_idleWaitTime) , cancellationToken : linkCts.Token);

            Debug.Log("[Alien] 대기 완료. 다음 명령 대기.");
            ChangeState(CHASEAI_STATE.IDLE);
        }
        catch ( OperationCanceledException )
        {
            Debug.Log("[Alien] 대기 중단됨! (소리/추격)");
        }
        finally
        {
            _isWaiting = false;
            linkCts.Dispose();
            _waitCts = null;
        }
    }

    #region AI 시야 로직

    private void DetectPlayer()
    {
        float distanceToTarget = Vector3.Distance(transform.position, _targetPlayer.position);
        if ( distanceToTarget > _sightRange ) return;

        // [핵심 변경] 플레이어 위치를 '내(Alien) 기준의 로컬 좌표'로 변환
        Vector3 targetLocal = _eyeTransform.InverseTransformPoint(_targetPlayer.position);

        // 2. 방향 체크 (뒤에 있으면 무시)
        if ( targetLocal.z < 0 ) return; // z가 음수면 내 등 뒤에 있다는 뜻

        // 3. 수평(좌우) 각도 계산 및 체크
        // Atan2(x, z)는 평면상의 각도를 라디안으로 반환함 ->도로 변환
        float angleH = Mathf.Atan2(targetLocal.x, targetLocal.z) * Mathf.Rad2Deg;
        if ( Mathf.Abs(angleH) > _horizontalSightAngle * 0.5f ) return; // 좌우 범위를 벗어남

        // 4. 수직(위아래) 각도 계산 및 체크
        // Atan2(y, z)는 높이 각도를 반환함
        float angleV = Mathf.Atan2(targetLocal.y, targetLocal.z) * Mathf.Rad2Deg;

        if ( Mathf.Abs(angleV) > _verticalSightAngle * 0.5f ) return; // 위아래 범위를 벗어남

        // 5. 장애물(Raycast) 체크 (기존과 동일)
        Vector3 dirToTarget = (_targetPlayer.position - _eyeTransform.position).normalized;

        if ( !Physics.Raycast(_eyeTransform.position , dirToTarget , distanceToTarget , _obstacleMask) )
        {
            if ( _currentState != CHASEAI_STATE.CHASE)
            {
                StartChase();
            }
        }
    }

    private void ChaseUpdate()
    {
        if ( _targetPlayer == null )
        {
            return;
        }

        _agent.SetDestination(_targetPlayer.position);

        // TODO : 추격 포기 로직
    }

    // [New] 퇴근 (비활성화)
    public void Vanish()
    {
        // 동작 멈춤
        _agent.isStopped = true;
        _agent.ResetPath();

        // 대기 중인 UniTask 취소
        if ( _waitCts != null ) _waitCts.Cancel();
        _isWaiting = false;

        // 상태 초기화
        _currentState = CHASEAI_STATE.IDLE;

        // 게임 오브젝트 끄기 (안 보임)
        gameObject.SetActive(false);
    }

    // [New] 출근 (스폰)
    public void Spawn(Vector3 position)
    {
        // 위치 이동 (NavMeshAgent는 transform.position 말고 Warp를 써야 함)
        _agent.Warp(position);

        // 켜기
        gameObject.SetActive(true);

        Debug.Log($"[Alien] 스폰 완료: {position}");

        // 바로 Idle 상태로 시작하면 MasterAI가 다음 프레임에 명령을 줌
        _currentState = CHASEAI_STATE.IDLE;
    }

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
        Vector3 dirTR = eyeRotation * Quaternion.Euler(-halfV,  halfH, 0) * Vector3.forward; // Top-Right
        Vector3 dirBL = eyeRotation * Quaternion.Euler( halfV, -halfH, 0) * Vector3.forward; // Bottom-Left
        Vector3 dirBR = eyeRotation * Quaternion.Euler( halfV,  halfH, 0) * Vector3.forward; // Bottom-Right

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
    #endregion
}
