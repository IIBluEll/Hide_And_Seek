using UnityEngine;
using UnityEngine.AI;

public enum CHASE_AI_STATE
{
    REQUEST_JOB,
    MOVE_TO_ZONE,
    INVESTIGATE,
    RETURN_VENT
}

[RequireComponent(typeof(NavMeshAgent))]
public class Chase_AI : MonoBehaviour
{
    private MasterAI_Provider _masterAI;
    private NavMeshAgent _agent;

    [Header("도착 판정 거리"), SerializeField]
    private float _arriveToFinal = 0.2f;

    [Header("수색 관련 수치")]
    [SerializeField] private float _investigateDuration = 4f;
    [SerializeField] private float _pointWait = 1f;
    [SerializeField] private float _suspicionMultiplier = 0.6f;

    private CHASE_AI_STATE _state;

    private ChaseJob _currentJob;
    private ZoneArea _currentZone;

    private float _investigateEndTime;
    private float _waitEndTime;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        _state = CHASE_AI_STATE.REQUEST_JOB;

        _currentJob = default;
        _currentZone = null;

        _investigateEndTime = 0f;
        _waitEndTime = 0f;
    }


}
