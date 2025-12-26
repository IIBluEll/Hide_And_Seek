using HM.CodeBase;
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
    [SerializeField] private ChaseAi_Controller _aiController;
    [SerializeField] private Transform _playerTransform;

    [Header("세팅")]
    [SerializeField] private float _minSpawnRadius = 10.0f;
    [SerializeField] private float _maxSpawnRadius = 20.0f;

    [Space(10f), Header("청각 범위")]
    [SerializeField] private float _hearingDis = 20f;

    [Space(10f), Header("활동 시간")]
    [SerializeField] private float _activeDuration = 60.0f;  // 1분간 활동
    [SerializeField] private float _dormantDuration = 20.0f; // 20초간 휴식

    private MASTERAI_PHASE _currentPhase = MASTERAI_PHASE.DORMANT;
    private float _phaseTimer = 0f;

    private Vector3 _debugLastTargetPos = Vector3.zero;

    public override void Awake()
    {
        base.Awake();

        if(_aiController != null && _playerTransform != null)
        {
            _aiController.Initalize(_playerTransform);
            _aiController.Vanish();
        }
    }

    private void Update()
    {
        if(_aiController == null || _playerTransform == null)
        {
            return;
        }

        UpdateCycle();

        if (_currentPhase == MASTERAI_PHASE.ACTIVE && _aiController.IsAvailableForCommand() )
        {
            GiveNextPatrolCommand();
        }
    }

    private void UpdateCycle()
    {
        _phaseTimer += Time.deltaTime;

        if(_currentPhase == MASTERAI_PHASE.ACTIVE)
        {
            // 활동 시간이 끝났고 + 추격 중이 아니면 -> 퇴근
            if ( _phaseTimer >= _activeDuration && !_aiController.IsChasing() )
            {
                EnterDormantMode();
            }
        }
        else
        {
            // 휴식 시간이 끝났으면 -> 출근 (스폰)
            if ( _phaseTimer >= _dormantDuration )
            {
                EnterActiveMode();
            }
        }
    }

    private void EnterDormantMode()
    {
        Debug.Log("[MasterAI] 에이리언 퇴근 (Dormant 진입)");
        _currentPhase = MASTERAI_PHASE.DORMANT;
        _phaseTimer = 0f;

        // 에이리언을 맵에서 치워버림
        _aiController.Vanish();
    }

    private void EnterActiveMode()
    {
        Debug.Log("[MasterAI] 에이리언 출근 (Active 진입)");

        // 스폰 위치 계산
        Vector3 spawnPos = CalculatePoint();

        // 유효한 위치가 아니면 플레이어 뒤쪽 멀리 강제 지정 (예외처리)
        if ( spawnPos == Vector3.zero )
            spawnPos = _playerTransform.position - _playerTransform.forward * 20f;

        _currentPhase = MASTERAI_PHASE.ACTIVE;
        _phaseTimer = 0f;

        // 해당 위치에 에이리언 소환
        _aiController.Spawn(spawnPos);
    }

    private void GiveNextPatrolCommand()
    {
        Vector3 targetPos = CalculatePoint();

        if ( targetPos != Vector3.zero )
        {
            _debugLastTargetPos = targetPos;
            _aiController.MoveToTarget(targetPos);
        }
    }

    private Vector3 CalculatePoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        Vector3 direction = new Vector3(randomCircle.x, 0, randomCircle.y);

        float distance = Random.Range(_minSpawnRadius, _maxSpawnRadius);

        Vector3 potentialPos = _playerTransform.position + (direction * distance);

        NavMeshHit hit;
        if ( NavMesh.SamplePosition(potentialPos , out hit , 2.0f , NavMesh.AllAreas) )
        {
            return hit.position;
        }

        return Vector3.zero;
    }

    public void ReportNoise(Vector3 noisePos, float loudness)
    {
        float tDisToChaseAI = Vector3.Distance(noisePos, _aiController.transform.position);

        if ( tDisToChaseAI <= _hearingDis * loudness)
        {
            Debug.Log($"[MasterAI] 소음 감지! 거리: {tDisToChaseAI:F1}m");

            _aiController.InVestigateNoise(noisePos);
        }
    }

    private void OnDrawGizmos()
    {
        if ( _playerTransform == null ) return;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(_playerTransform.position , _minSpawnRadius);
        Gizmos.DrawWireSphere(_playerTransform.position , _maxSpawnRadius);

        if ( _debugLastTargetPos != Vector3.zero )
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_debugLastTargetPos , 0.5f);

            Gizmos.DrawLine(_playerTransform.position , _debugLastTargetPos);
        }
    }
}
