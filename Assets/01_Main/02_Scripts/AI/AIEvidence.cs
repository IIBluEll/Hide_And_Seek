using UnityEngine;

namespace AI.ChaseAI
{
    /// <summary>
    /// 추격 AI가 기억하는 단서 정보
    /// </summary>
    [System.Serializable]
    public struct AIEvidence
    {
        public CHASEAI_PERCEPTION_TYPE Type;    // 단서 유형
        public Vector3 LastKnownPosition;       // 마지막으로 알려진 위치
        public float TimeStamp;                 // 단서가 생성된 시간

        public AIEvidence(CHASEAI_PERCEPTION_TYPE type, Vector3 position)
        {
            Type = type;
            LastKnownPosition = position;
            TimeStamp = Time.time;
        }

        // 시간이 너무 지나면 만료
        public bool IsValid(float currentTime, float memoryDuration)
        {
            return (currentTime - TimeStamp) <= memoryDuration;
        }

        public static AIEvidence Empty => new AIEvidence(CHASEAI_PERCEPTION_TYPE.NONE, Vector3.zero);
    }
}

