using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class ManageChaseStateMachine
{
    private CHASEAI_STATE _currentState = CHASEAI_STATE.IDLE;

    private Transform _chaseTransform;

    private readonly ManageChaseMovement _chaseMovement;
    private readonly ManageChasePerception _chasePerception;

    private float _walkSpeed;
    private float _runSpeed;
    private float _idleWaitTime;

    private bool _isWaiting = false;

    private CancellationToken _destoryToken;
    private CancellationTokenSource _waitCts;

    public CHASEAI_STATE CurrentState => _currentState;

    public ManageChaseStateMachine(Transform chaseTransform , ManageChaseMovement chaseMovement , ManageChasePerception chasePerception , float walkSpeed , float runSpeed , float idleWaitTime , CancellationToken destoryToken)
    {
        _chaseTransform = chaseTransform; 

        _chaseMovement = chaseMovement;
        _chasePerception = chasePerception;

        _walkSpeed = walkSpeed;
        _runSpeed = runSpeed;
        _idleWaitTime = idleWaitTime;

        _destoryToken = destoryToken;
    }

    public bool IsAvailableForCommand()
    {
        return _currentState == CHASEAI_STATE.IDLE && !_isWaiting;
    }

    public bool IsRetreating()
    {
        return _currentState == CHASEAI_STATE.RETREAT;
    }

    public bool IsVanished()
    {
        return _currentState == CHASEAI_STATE.DEACTIVATE;
    }

    public bool IsChasing()
    {
        return _currentState == CHASEAI_STATE.CHASE;
    }

    public void CommandMoveToTarget(Vector3 targetPos)
    {
        if(!IsAvailableForCommand())
        {
            return;
        }

        _chaseMovement.MoveAgent(targetPos , _walkSpeed);
        ChangeState(CHASEAI_STATE.PATROL);
    }

    public void CommandInvestigateNoise(Vector3 targetPos)
    {
        if(_currentState == CHASEAI_STATE.CHASE || _currentState == CHASEAI_STATE.RETREAT || _currentState == CHASEAI_STATE.COMMUTE)
        {
            return;
        }

        Debug.Log("[Chase AI] 소음 감지!");
        CancelWait();

        _chaseMovement.MoveAgent(targetPos , _walkSpeed);
        ChangeState(CHASEAI_STATE.INVESTIGATE);
    }

    public void CommandOrderRetreat(Vector3 ventPos)
    {
        if(_currentState == CHASEAI_STATE.CHASE || _currentState == CHASEAI_STATE.RETREAT || _currentState == CHASEAI_STATE.DEACTIVATE)
        {
            return;
        }

        Debug.Log("[Chase AI] 추격 AI 퇴근하러감");
        CancelWait();

        _chaseMovement.MoveAgent(ventPos , _walkSpeed);
        ChangeState(CHASEAI_STATE.RETREAT);
    }

    public void CommandSpawn()
    {
        ChangeState(CHASEAI_STATE.COMMUTE);

        Debug.Log("[Chase AI] 스폰중");
        WaitAndSwitchToIdle_async().Forget();
    }

    public void CommandSetDeactivate()
    {
        ChangeState(CHASEAI_STATE.DEACTIVATE);
    }

    public void CancelWaitExternal()
    {
        CancelWait();
    }

    public void Tick(Action onPlayerContacted, Action onRetreatArrived)
    {
        if(_chasePerception != null && _chasePerception.HasTarget())
        {
            DetectPlayer(_chaseTransform, onPlayerContacted);
        }

        switch(_currentState)
        {
            case CHASEAI_STATE.PATROL:
            case CHASEAI_STATE.INVESTIGATE:
                CheckArrival();
                break;

            case CHASEAI_STATE.CHASE:
                ChaseUpdate();
                break;

            case CHASEAI_STATE.RETREAT:
                CheckRetreatArrival(onRetreatArrived);
                break;
        }
    }

    public void Dispose()
    {
        CancelWait();
    }

    private void ChangeState(CHASEAI_STATE newState)
    {
        _currentState = newState;
    }

    private void CheckArrival()
    {
        if(_chaseMovement.IsPathPending())
        {
            return;
        }

        if(_chaseMovement.IsArrived())
        {
            WaitAndSwitchToIdle_async().Forget();
        }
    }

    private void CheckRetreatArrival(Action onRetreatArrived)
    {
        if(_chaseMovement.IsPathPending())
        {
            return;
        }

        if(_chaseMovement.IsArrivedForRetreat())
        {
            onRetreatArrived?.Invoke();
        }
    }

    private async UniTaskVoid WaitAndSwitchToIdle_async()
    {
        if(_isWaiting)
        {
            return;
        }
        _isWaiting = true;

        if(_currentState == CHASEAI_STATE.COMMUTE)
        {
            Debug.Log("[Chase AI] 출근 완료. 대기 중...");
        }
        else
        {
            Debug.Log("[Chase AI] 목적지 도착. 주위를 살피는 중...");
        }

        _waitCts = new CancellationTokenSource();
        var linkCts = CancellationTokenSource.CreateLinkedTokenSource( _waitCts.Token , _destoryToken);
        
        try
        {

            await UniTask.Delay(TimeSpan.FromSeconds(_idleWaitTime) , cancellationToken: linkCts.Token);
            ChangeState(CHASEAI_STATE.IDLE);
        }
        catch ( OperationCanceledException ex) 
        {
            Debug.LogError($"{ex.Message}");
        }
        finally
        {
            _isWaiting = false;

            if(_waitCts != null)
            {
                _waitCts.Cancel();
                _waitCts = null;
            }
        }
    }

    private void CancelWait()
    {
        if ( _isWaiting && _waitCts != null )
        {
            _waitCts.Cancel();
            _isWaiting = false;
        }
    }

    private void DetectPlayer(Transform chaseTransform, Action onPlayerContacted)
    {
        if(!_chasePerception.IsPlayerVisible(chaseTransform))
        {
            return;
        }

        if(_currentState != CHASEAI_STATE.CHASE)
        {
            StartChase();
        }

        onPlayerContacted?.Invoke();
    }

    private void StartChase()
    {
        Debug.Log("플레이어 발견 !!! 추격 개시 !!!");
        CancelWait();

        ChangeState(CHASEAI_STATE.CHASE);
        _chaseMovement.MoveAgent(_chasePerception.GetPlayerPosition() , _runSpeed);
    }

    private void ChaseUpdate()
    {
        if(!_chasePerception.HasTarget())
        {
            return;
        }

        if(_chasePerception.IsPlayerVisible(_chaseTransform))
        {
            _chaseMovement.SetDestination(_chasePerception.GetPlayerPosition());
        }
        else
        { // 시야에서 놓침
            if(!_chaseMovement.IsPathPending() && !_chaseMovement.IsArrived())
            {
                Debug.Log("플레이어 놓침. 마지막 위치 수색 전환");
                ChangeState(CHASEAI_STATE.INVESTIGATE);
            }
        }

    }
}
