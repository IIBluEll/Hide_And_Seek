using UnityEngine;

[CreateAssetMenu(fileName = "MasterAI_Config", menuName = "Scriptable Objects/MasterAI_Config")]
public class MasterAI_Config : ScriptableObject
{
    [Header("Search Settings (수색 설정)")]
    public float MaxSearchRadius = 25f; 
    public float MinSearchRadius = 5f;  

    [Header("State Timers (상태 타이머)")]
    public float RespawnCooldown = 15f; 
    public float CommandInterval = 5f;  

    [Header("Gauge Settings (수치 설정)")]
    public float StressDecreaseRate = 2f;    
    public float AlertDecreaseRate = 5f;     
    public float MaxStressThreshold = 100f;  
    public float MaxStressLimit = 120f;      
    public float MaxAlertLimit = 100f;       
    public float SafeDistance = 20f;         

    [Header("Sensory (감각/반응)")]
    public float NoiseReactionMultiplier = 30f; 
    public float StressIncreaseMultiplier = 0.2f;
    public float HearingDistanceMultiplier = 20f; 
}
