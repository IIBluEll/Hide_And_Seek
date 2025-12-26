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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position , _sightRange);

        // 시야각 표현이 좀 복잡해지므로, 간단하게 '정면 방향' 라인만 표시해도 충분합니다.
        Gizmos.color = Color.blue;
        Vector3 eyePos = (_eyeTransform != null) ? _eyeTransform.position : transform.position;
        Gizmos.DrawRay(eyePos , transform.forward * _sightRange);
    }
    #endregion
}
