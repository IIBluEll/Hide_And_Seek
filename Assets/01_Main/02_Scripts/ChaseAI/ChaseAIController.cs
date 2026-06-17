using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ChaseAIController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CHASEAI_STATE _currentState = CHASEAI_STATE.IDLE;

    [Space(5f), Header("이동 수치")]
    [SerializeField] private float _walkSpeed = 4f;
    [SerializeField] private float _runSpeed = 7f;
    [SerializeField] private float _idleWaitTime = 2f;

    [Space(5f), Header("시야 수치")]
    [SerializeField] private float _sightRange = 15f;
    [SerializeField] private float _horizontalSightAngle = 120f;
    [SerializeField] private float _verticalSightAngle = 60f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private Transform _eyeTransform;

    private NavMeshAgent _agent;
    private Transform _targetPlayer;

    private ManageChaseMovement _chaseMovement;
    private ManageChasePerception _chasePerception;
    private ManageChaseStateMachine _chaseState;

    public CHASEAI_STATE CurrentState => _currentState;

    #region Unity Cycle

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _walkSpeed;

        if(_eyeTransform == null)
        {
            _eyeTransform = transform;
        }

        _chaseMovement = new ManageChaseMovement(_agent);
        _chasePerception = new ManageChasePerception(_sightRange , _horizontalSightAngle , _verticalSightAngle , _obstacleMask , _eyeTransform);
        _chaseState = new ManageChaseStateMachine(transform, _chaseMovement , _chasePerception , _walkSpeed , _runSpeed , _idleWaitTime , this.GetCancellationTokenOnDestroy());
    }

    private void Update()
    {
        if(!gameObject.activeSelf)
        {
            return;
        }

        _chaseState.Tick(OnPlayerContacted , OnRetreatArrived);
        _currentState = _chaseState.CurrentState;
    }

    private void OnDestroy()
    {
        _chaseState.Dispose();
    }

    #endregion

    public void Initalize(Transform player)
    {
        _targetPlayer = player;
        _chasePerception.SetTarget(_targetPlayer);
    }

    #region MasterAI 명령 받기

    public bool IsAvailableForCommand()
    {
        return gameObject.activeSelf && _chaseState.IsAvailableForCommand();
    }

    public bool IsRetreating()
    {
        return _chaseState.IsRetreating();
    }

    public bool IsVanished()
    {
        return _chaseState.IsVanished();
    }

    public bool IsChasing()
    {
        return _chaseState.IsChasing();
    }

    public void MoveToTarget(Vector3 targetPos)
    {
        _chaseState.CommandMoveToTarget(targetPos);
    }

    public void InvestgateNoise(Vector3 targetPos)
    {
        _chaseState.CommandInvestigateNoise(targetPos);
    }

    public void OrderRetreat(Vector3 ventPos)
    {
        _chaseState.CommandOrderRetreat(ventPos);
    }

    public void Spawn(Vector3 spawnPos)
    {
        _chaseMovement.Warp(spawnPos);
        gameObject.SetActive(true);

        _chaseState.CommandSpawn();
    }

    public void Vanish()
    {
        MasterAI_Provider.Instance.OnChaseAIVanish();

        _chaseState.CancelWaitExternal();
        _chaseMovement.ResetPath();

        gameObject.SetActive(false);
        _chaseState.CommandSetDeactivate();
        
        Debug.Log("[Chase AI] 추격 AI 퇴근 완료");
    }

    #endregion

    private void OnPlayerContacted()
    {
        MasterAI_Provider.Instance.ReportPlayerContact();
    }

    private void OnRetreatArrived()
    {
        Vanish();
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
