using UnityEngine;

namespace AI.MasterAI
{
    [System.Serializable]
    public class Master_GaugeSystem
    {
        public float GlobalStress { get; private set; }
        public float Anger { get; private set; }

        [SerializeField] private float _missionProgressRatio;

        [SerializeField] private CHASEAI_ANGER_PHASE _currentAngerPhase = CHASEAI_ANGER_PHASE.MENACE;

        private const float PHASE_DOWN_BUFFER = 10f;

        public void Initialize()
        {
            GlobalStress = 0f;
            Anger = 0f;
            _missionProgressRatio = 0f;
            _currentAngerPhase = CHASEAI_ANGER_PHASE.MENACE;
        }

        public void SetMissionProgress(float progressRatio)
        {
            _missionProgressRatio = Mathf.Clamp01(progressRatio);
        }

        #region 긴장도, 화남 관리

        public void UpdateGauage(float deltaTime , MasterAI_Configs config , CHASEAI_CONFIDENCE_STATE state)
        {
            float tStressRate = (state == CHASEAI_CONFIDENCE_STATE.HIGH) ? config.StressRate_Chase : config.StressRate_Passive;

            GlobalStress += tStressRate * deltaTime;
            GlobalStress = Mathf.Min(GlobalStress , config.MaxStress);

            if ( state == CHASEAI_CONFIDENCE_STATE.LOW )
            {
                Anger -= config.AngerDecayRate * deltaTime;
                Anger = Mathf.Max(Anger , 0f);
            }
        }

        public void AddAnger(float amount)
        {
            Anger += amount;
            Anger = Mathf.Min(Anger , 100f);
        }

        public void ResetStress()
        {
            GlobalStress = 0f;
        }

        #endregion

        #region 추격AI 분노 상태 관리

        public CHASEAI_ANGER_PHASE GetCurrentAngerPhase(MasterAI_Configs config)
        {
            float tStalkThreshold = config.GetStalkThreshold(_missionProgressRatio);
            float tHuntThreshold = config.GetHuntThreshold(_missionProgressRatio);

            // 추격AI의 분노가 기준선을 왔다갔다 하면 상태도 왔다갔다 할 수 있으므로 버퍼 적용
            switch ( _currentAngerPhase )
            {
                case CHASEAI_ANGER_PHASE.MENACE:

                    if ( Anger >= tHuntThreshold )
                    {
                        _currentAngerPhase = CHASEAI_ANGER_PHASE.HUNT;
                    }
                    else if ( Anger >= tStalkThreshold )
                    {
                        _currentAngerPhase = CHASEAI_ANGER_PHASE.STALK;
                    }
                    break;

                case CHASEAI_ANGER_PHASE.STALK:

                    if ( Anger >= tHuntThreshold )
                    {
                        _currentAngerPhase = CHASEAI_ANGER_PHASE.HUNT;
                    }
                    else if ( Anger < tStalkThreshold - PHASE_DOWN_BUFFER )
                    {
                        _currentAngerPhase = CHASEAI_ANGER_PHASE.MENACE;
                    }
                    break;

                case CHASEAI_ANGER_PHASE.HUNT:

                    if ( Anger < tHuntThreshold - PHASE_DOWN_BUFFER )
                    {
                        if ( Anger >= tStalkThreshold )
                        {
                            _currentAngerPhase = CHASEAI_ANGER_PHASE.STALK;
                        }
                        else
                        {
                            _currentAngerPhase = CHASEAI_ANGER_PHASE.MENACE;
                        }
                    }
                    break;
            }

            return _currentAngerPhase;
        }

        public bool IsStressFull(MasterAI_Configs config)
        {
            return GlobalStress >= config.MaxStress;
        }

        #endregion
    }
}

