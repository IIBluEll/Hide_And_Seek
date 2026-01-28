using UnityEngine;

public interface IMasterState
{
    void Enter(MasterAI_Provider masterAI);
    void Update(MasterAI_Provider masterAI);
    void Exit(MasterAI_Provider masterAI);
}

// 퇴근 모드
[System.Serializable]
public class MasterState_Dormant : IMasterState
{
    [Header("변수")]
    [SerializeField] private float _respawnCooldown = 15f;

    private float _timer;

    public void Enter(MasterAI_Provider masterAI)
    {
        _timer = 0f;
        Debug.Log("[Director] 휴식 상태 진입");
    }

    public void Update(MasterAI_Provider masterAI)
    {
        _timer += Time.deltaTime;
        
        if(_timer >= _respawnCooldown && masterAI.GaugeSystem.IsStressZero)
        {
            masterAI.ChangePhase(MASTERAI_PHASE.ACTIVE);
        }
    }

    public void Exit(MasterAI_Provider masterAI) { }
}

// 활동 모드
[System.Serializable]
public class MasterState_Active : IMasterState
{
    [Header("변수")]
    [SerializeField] private float _commandInterval = 5f;

    private float _timer;

    public void Enter(MasterAI_Provider masterAI)
    {
        _timer = 0f;
        masterAI.OrderSpawn();
    }

    public void Update(MasterAI_Provider masterAI)
    {
        _timer += Time.deltaTime;

        if(ShouldRetreat(masterAI))
        {
            masterAI.ChangePhase(MASTERAI_PHASE.DORMANT);
            return;
        }

        if ( _timer >= _commandInterval && masterAI.ChaseAI.IsAvailableForCommand() && !masterAI.ChaseAI.IsRetreating() )
        {
            _timer = 0f;
            masterAI.OrderSearch();
        }
    }

    public void Exit(MasterAI_Provider masterAI)
    {
        masterAI.OrderRetreat();
    }

    // 퇴근 조건 판단 로직
    private bool ShouldRetreat(MasterAI_Provider context)
    {
        bool isSafeTime = (Time.time - context.LastContactTime) > 10f;

        return context.GaugeSystem.IsMaxStressReached   
               && !context.ChaseAI.IsChasing()    
               && !context.ChaseAI.IsRetreating() 
               && isSafeTime;                     
    }
}
