using UnityEngine;

namespace AI.MasterAI
{
    [CreateAssetMenu(fileName = "MasterAI_Config" , menuName = "AI/MasterAI Config")]
    public class MasterAI_Configs : ScriptableObject
    {
        [Header("Gauge Settings")]
        [Tooltip("퇴근(Dormant) 기준이 되는 최대 스트레스")]
        public float MaxStress = 100f;

        [Tooltip("일반 활동 중 초당 스트레스 증가량")]
        public float StressRate_Passive = 2.0f;

        [Tooltip("CHASE(추격) 중 초당 스트레스 증가량")]
        public float StressRate_Chase = 10.0f;

        [Tooltip("분노(Anger) 자연 감소량 (초당)")]
        public float AngerDecayRate = 1.0f;


        [Space(10f),Header("Memory Settings")]
        [Tooltip("시야 상실 후 추격(HUNT) 상태 유지 시간")]
        public float SightMemoryTime = 5.0f;

        [Tooltip("소음 발생 위치 조사(Search) 최대 지속 시간")]
        public float SoundMemoryTime = 10.0f;


        [Space(10f),Header("Speed Settings")]
        [Tooltip("MENACE 단계 이동 속도 (탐색)")]
        public float WalkSpeed = 2.0f;

        [Tooltip("STALK 단계 이동 속도 (빠른 걷기)")]
        public float StalkSpeed = 4.0f;

        [Tooltip("HUNT 단계 이동 속도 (달리기)")]
        public float RunSpeed = 6.0f;


        [Space(10f),Header("Respawn Settings")]
        [Tooltip("퇴근 후 재출근까지 대기 시간")]
        public float RespawnCooldown = 20.0f;


        [Space(10f),Header("Mission Threshold Shift")]
        [Tooltip("진행도 0%일 때, Stalk 단계로 진입하기 위한 Anger 수치")]
        public float AngerThreshold_Stalk_Early = 50f;

        [Tooltip("진행도 100%일 때, Stalk 단계로 진입하기 위한 Anger 수치")]
        public float AngerThreshold_Stalk_Late = 20f;

        [Tooltip("진행도 0%일 때, Hunt 단계로 진입하기 위한 Anger 수치")]
        public float AngerThreshold_Hunt_Early = 80f;

        [Tooltip("진행도 100%일 때, Hunt 단계로 진입하기 위한 Anger 수치")]
        public float AngerThreshold_Hunt_Late = 40f;

        // 현재 미션 진행도에 따른 Stalk 진입 임계값 반환
        public float GetStalkThreshold(float missionProgressRatio)
        {
            return Mathf.Lerp(AngerThreshold_Stalk_Early, AngerThreshold_Stalk_Late, missionProgressRatio);
        }

        // 현재 미션 진행도에 따른 Hunt 진입 임계값 반환
        public float GetHuntThreshold(float missionProgressRatio)
        {
            return Mathf.Lerp(AngerThreshold_Hunt_Early, AngerThreshold_Hunt_Late, missionProgressRatio);
        }
    }
}

