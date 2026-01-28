using UnityEngine;

[System.Serializable]
public class Manage_Gauage
{
    [Header("Current Status")]
    public float GlobalStress = 0f;
    public float AreaAlert = 0f;

    [Space(10f), Header("Settings")]
    [SerializeField] private float _stressDecreaseRate = 2f;
    [SerializeField] private float _alertDecreaseRate = 5f;
    [SerializeField] private float _maxStressThreshold = 100f;

    private const float MAX_STRESS_LIMIT = 120f;
    private const float MAX_ALERT_LIMIT = 100f;
    private const float SAFE_DISTANCE = 20f;

    public bool IsMaxStressReached => GlobalStress >= _maxStressThreshold;
    public bool IsStressZero => GlobalStress <= 0f;
    public bool IsAlertZero => AreaAlert <= 0f;

    public float AlertRatio => Mathf.Clamp01(AreaAlert / 100f);

    public void UpdateGauages(float deltaTime, float distanceToPlayer, bool isChasing, MASTERAI_PHASE currentPhase )
    {
        if ( currentPhase == MASTERAI_PHASE.ACTIVE )
        {
            if ( distanceToPlayer > SAFE_DISTANCE )
            {
                GlobalStress -= _stressDecreaseRate * 0.5f * deltaTime;
            }
            else
            {
                float tTooClose = (SAFE_DISTANCE - distanceToPlayer) * 0.8f;
                GlobalStress += tTooClose * deltaTime;
            }
        }
        else
        {
            if ( GlobalStress > 0 )
            {
                GlobalStress -= _stressDecreaseRate * 10f * deltaTime;
            }
        }

        if ( !isChasing && AreaAlert > 0 )
        {
            AreaAlert -= _alertDecreaseRate * deltaTime;
        }

        GlobalStress = Mathf.Clamp(GlobalStress , 0 , MAX_STRESS_LIMIT);
        AreaAlert = Mathf.Clamp(AreaAlert , 0 , MAX_ALERT_LIMIT);
    }

    #region public API

    public void IncreaseAlert(float amount)
    {
        AreaAlert = Mathf.Clamp(AreaAlert + amount , 0 , MAX_ALERT_LIMIT);
    }

    public void IncreaseStress(float amount)
    {
        GlobalStress = Mathf.Clamp(GlobalStress + amount , 0 , MAX_STRESS_LIMIT);
    }

    public void OnPlayerContact(float deltaTime)
    {
        AreaAlert = 100f;
        GlobalStress += 10f * deltaTime;
    }

    public void Reset()
    {
        GlobalStress = 0f;
        AreaAlert = 0f;
    }

    #endregion
}
