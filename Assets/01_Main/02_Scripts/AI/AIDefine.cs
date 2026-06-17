using UnityEngine;

namespace AI
{
    // 기획서 3.2: 디렉터 상태
    public enum MASTERAI_PHASE
    {
        ACTIVE,
        DORMANT,
    }

    // 기획서 4.2 : 추격AI 확신
    public enum CHASEAI_CONFIDENCE_STATE
    {
        LOW,
        MID,
        HIGH
    }

    // 기획서 4.3 : 추격AI 분노
    public enum CHASEAI_ANGER_PHASE
    {
        MENACE,
        STALK,
        HUNT
    }

    // 추격AI 감각
    public enum CHASEAI_PERCEPTION_TYPE
    {
        NONE,
        AUDIO,
        VISUAL
    }
}

