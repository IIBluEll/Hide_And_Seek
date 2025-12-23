using UnityEngine;

[CreateAssetMenu(fileName = "MasterAIConfig_SO", menuName = "Scriptable Objects/MasterAIConfig_SO")]
public class MasterAIConfig_SO : ScriptableObject
{
    #region 의심도 시스템
    [Header("Zone Suspicion System")]
    public float SuspicionMax = 100;

    [Tooltip("1초마다 모든 ZoneSuspicion에서 감소되는 값")]
    public float DecreasePerSec = 2.5f;

    [Tooltip("의심도가 증가할 때 이웃 존들도 증가하는 비율")]
    [Range(0f, 1f)]
    public float SpreadRatio = 0.2f;

    [Tooltip("플레이어가 한 Zone에 오래 있을때 증가하는 의심도 값")]
    public float StaySuspicionPerCamp = 10f;
    #endregion
}
