using UnityEngine;

[System.Serializable]
public class ManageGauage
{
    [Header("Current Status")]
    public float GlobalStress = 0f;
    public float AreaAlert = 0f;

    public bool IsMaxStressReached(MasterAI_Config config) => GlobalStress >= config.MaxStressThreshold;
    public bool IsStressZero => GlobalStress <= 0f;
    public bool IsAlertZero => AreaAlert <= 0f;

    public float AlertRatio => Mathf.Clamp01(AreaAlert / 100f);

    public void UpdateGauages(float deltaTime, float distanceToPlayer, bool isChasing, MASTERAI_PHASE currentPhase, MasterAI_Config config )
    {
        if ( currentPhase == MASTERAI_PHASE.ACTIVE )
        {
            if ( distanceToPlayer > config.SafeDistance )
            {
                GlobalStress -= config.StressDecreaseRate * 0.5f * deltaTime;
            }
            else
            {
                float tTooClose = (config.SafeDistance - distanceToPlayer) * 0.8f;
                GlobalStress += tTooClose * deltaTime;
            }
        }
        else
        {
            if ( GlobalStress > 0 )
            {
                GlobalStress -= config.StressDecreaseRate * 10f * deltaTime;
            }
        }

        //TODO : AI가 퇴근 도중에도 긴장도가 떨어짐 -> 수정 필요
        if ( !isChasing && AreaAlert > 0 )
        {
            AreaAlert -= config.AlertDecreaseRate * deltaTime;
        }

        GlobalStress = Mathf.Clamp(GlobalStress , 0 , config.MaxStressLimit);
        AreaAlert = Mathf.Clamp(AreaAlert , 0 , config.MaxAlertLimit);
    }

    #region public API

    public void IncreaseAlert(float amount, MasterAI_Config config)
    {
        AreaAlert = Mathf.Clamp(AreaAlert + amount , 0 , config.MaxAlertLimit);
    }

    public void IncreaseStress(float amount, MasterAI_Config config)
    {
        GlobalStress = Mathf.Clamp(GlobalStress + amount , 0 , config.MaxStressLimit);
    }

    public void OnPlayerContact(float deltaTime, MasterAI_Config config)
    {
        AreaAlert = config.MaxAlertLimit;
        GlobalStress += 10f * deltaTime;
    }

    public void Reset()
    {
        GlobalStress = 0f;
        AreaAlert = 0f;
    }

    #endregion
}
