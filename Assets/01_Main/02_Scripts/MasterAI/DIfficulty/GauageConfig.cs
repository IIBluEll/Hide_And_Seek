using UnityEngine;

public enum MASTERAI_DIFFICULTY
{
    EASY,
    NORMAL,
    HARD,
}

public struct GauageConfigData
{
    public float StressDecreaseRate;
    public float AlertDecreaseRate;
    public float MaxStressThreshold;
    public float CommandInterval;
}


[CreateAssetMenu(fileName = "GauageConfig", menuName = "Scriptable Objects/GauageConfig")]
public class GauageConfig : ScriptableObject
{
    [SerializeField] private GauageConfigData _easy;
    [SerializeField] private GauageConfigData _normal;
    [SerializeField] private GauageConfigData _hard;

    public GauageConfigData GetConfigData(MASTERAI_DIFFICULTY difficulty)
    {
        return difficulty switch
        {
            MASTERAI_DIFFICULTY.EASY => _easy,
            MASTERAI_DIFFICULTY.NORMAL => _normal,
            MASTERAI_DIFFICULTY.HARD => _hard,
            _ => _normal,
        };
    }
}
