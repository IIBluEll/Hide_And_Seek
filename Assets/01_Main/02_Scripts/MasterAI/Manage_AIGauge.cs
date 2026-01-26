using UnityEngine;

public class Manage_AIGauge
{
    public float GlobalStress { get; private set; } 
    public float AreaAlert { get; private set; }

    private const float MAX_STRESS = 150f;
    private const float MAX_ALERT = 100f;
    private const float SAFE_DISTANCE = 20f;

    public void Tick(float deltaTime , MASTERAI_PHASE currentPhase , bool isChasing , float distPlayerToAI , float stressDecreaseRate , float alertDecreaseRate)
    {      
        if ( currentPhase == MASTERAI_PHASE.ACTIVE )
        {
            // 거리에 따른 피로도 변화
            if ( distPlayerToAI > SAFE_DISTANCE )
            {
                GlobalStress -= stressDecreaseRate * 0.5f * deltaTime;
            }
            else
            {
                float tTooClose = (SAFE_DISTANCE - distPlayerToAI) * 0.8f;
                GlobalStress += tTooClose * deltaTime;
            }
        }
        else 
        {
            // 비활성 상태에서는 피로도 자연 감소
            if ( GlobalStress > 0 )
            {
                GlobalStress -= stressDecreaseRate * deltaTime;
            }
        }

        // 추격 중이 아닐 때만 경계도 감소
        if ( !isChasing && AreaAlert > 0 )
        {
            AreaAlert -= alertDecreaseRate * deltaTime;
        }

        Clamp();
    }

    public bool ReportNoise(Vector3 noisePos , float loudness , Vector3 chaseAiPos)
    {
        float hearingDistance = 20.0f * loudness;
        float distToAI = Vector3.Distance(noisePos, chaseAiPos);

        if ( distToAI <= hearingDistance )
        {
            float increaseAmount = loudness * 30f;
            AreaAlert += increaseAmount;
            GlobalStress += increaseAmount * 0.2f;

            Clamp();
            return true; // 들림
        }

        return false; // 안 들림
    }

    public void ReportPlayerContact(float deltaTime)
    {
        AreaAlert = 100f; // 즉시 최대 경계
        GlobalStress += 10f * deltaTime;
        Clamp();
    }

    public void OnChaseAIVanish()
    {
        GlobalStress = 0f;

        Clamp();
    }

    private void Clamp()
    {
        GlobalStress = Mathf.Clamp(GlobalStress , 0f , MAX_STRESS);
        AreaAlert = Mathf.Clamp(AreaAlert , 0f , MAX_ALERT);
    }
}
