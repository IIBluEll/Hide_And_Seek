using AI.MasterAI;
using UnityEngine;

namespace AI.ChaseAI
{
    [RequireComponent(typeof(Chase_PerceptionSystem))]
    [RequireComponent(typeof(Chase_MovementSystem))]
    public class Chase_Controller : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Chase_PerceptionSystem _perception;
        [SerializeField] private Chase_MovementSystem _movement;

        [SerializeField] private Master_DirectorSystem _director; 
        [SerializeField] private Transform _playerTransform;

        [Space(5f), Header("AI Action")]
        [SerializeField] private float _killDistance = 1.5f;
        [SerializeField] private float _investigateWaitTime = 2.0f;

        private MasterAI_Configs _config;
        private AIEvidence _currentEvidence = AIEvidence.Empty;

        private bool _isRetreating = false;

        // 대기 상태 관련
        private float _waitTimer = 0f;
        private bool _isWaiting = false;

        public CHASEAI_CONFIDENCE_STATE CurrentConfidence { get; private set; } = CHASEAI_CONFIDENCE_STATE.LOW;

        public void Initialize(MasterAI_Configs config)
        {
            _config = config;
            CurrentConfidence = CHASEAI_CONFIDENCE_STATE.LOW;
            _currentEvidence = AIEvidence.Empty;
            _isRetreating = false;

            UpdateAngerState();
        }

        #region Unity LifeCycle

        private void Update()
        {
            if(_config == null || _director == null)
            {
                return;
            }

            if(_isRetreating)
            {
                ProcessRetreat();
                return;
            }

            float tTime = Time.time;
            float tDelta = Time.deltaTime;

            CheckVisualPerception(tTime);
            CheckEvideenceExpiration(tTime);
            UpdateConfidenceLevel();
            ConfidenceBehavior(tDelta);
            UpdateAngerState();
        }

        #endregion

        #region 감각 로직
        // 시각 정보
        private void CheckVisualPerception(float time)
        {
            if (_perception.CheckSight(_playerTransform)) 
            {
                // 시야에 플레이어가 걸리면 강력한 증거
                _currentEvidence = new AIEvidence(CHASEAI_PERCEPTION_TYPE.VISUAL, _playerTransform.position);
            }
        }

        // 소음 발생시 외부 이벤트 호출
        public void OnAudioPerception(Vector3 position)
        {
            // 시각에 걸려있지 않을 때
            if(CurrentConfidence != CHASEAI_CONFIDENCE_STATE.HIGH)
            {
                _currentEvidence = new AIEvidence(CHASEAI_PERCEPTION_TYPE.AUDIO, position);

                Debug.Log($"[ChaseAI] Heard noise at {position}");
            }
        }

        // 증거 시간 만료 체크
        private void CheckEvideenceExpiration(float time)
        {
            if ( (_currentEvidence.Type == CHASEAI_PERCEPTION_TYPE.NONE) )
            {
                return;
            }

            float tDuration = (_currentEvidence.Type == CHASEAI_PERCEPTION_TYPE.VISUAL) ? _config.SightMemoryTime : _config.SoundMemoryTime;

            if(!_currentEvidence.IsValid(time, tDuration))
            {
                _currentEvidence = AIEvidence.Empty;
                Debug.Log($"[ChaseAI] Evidence expired");
            }
        }

        // 증거에 따른 행동 변화
        private void UpdateConfidenceLevel()
        {
            CurrentConfidence = _currentEvidence.Type switch
            {
                CHASEAI_PERCEPTION_TYPE.VISUAL => CHASEAI_CONFIDENCE_STATE.HIGH,
                CHASEAI_PERCEPTION_TYPE.AUDIO => CHASEAI_CONFIDENCE_STATE.MID,
                _ => CHASEAI_CONFIDENCE_STATE.LOW,
            };
        }

        // Anger 수치에 따른 이동 속도 변화
        private void UpdateAngerState()
        {
            CHASEAI_ANGER_PHASE tPhase = _director.GetChaseAIAngerPhase();
            _movement.UpdateSpeed(tPhase,_config);
        }
        #endregion

        #region 행동 로직
        // 증거에 따른 행동 로직
        private void ConfidenceBehavior(float deltaTime)
        {
            // AI 끼임 예외처리
            if(_movement.IsPathFailedOrStuck())
            {
                Debug.LogWarning("[ChaseAI] 길을 찾을 수 없거나 끼임 발생! 현재 목표를 포기합니다.");

                _movement.StopMove();
                _currentEvidence = AIEvidence.Empty;
                _isWaiting = false;

                return;
            }

            if ( _isWaiting )
            {
                _movement.StopMove();
                _waitTimer -= deltaTime;

                if ( _waitTimer <= 0f )
                {
                    _isWaiting = false;
                    _currentEvidence = AIEvidence.Empty;
                }
                return; // 대기 중엔 아래 이동 로직을 타지 않음
            }

            switch (CurrentConfidence)
            {
                case CHASEAI_CONFIDENCE_STATE.HIGH:
                    
                    // 시야에 플레이어가 보이는 경우, 플레이어 위치로 이동
                    _movement.SetDestination(_currentEvidence.LastKnownPosition);
                    break;

                case CHASEAI_CONFIDENCE_STATE.MID:

                    // 소리가 들리는 경우, 소리가 난 위치로 이동
                    _movement.SetDestination(_currentEvidence.LastKnownPosition);

                    if(_movement.HasReachedDestination())
                    {
                        StartWaiting();
                    }
                    break;

                case CHASEAI_CONFIDENCE_STATE.LOW:
                    
                    if(_movement.HasReachedDestination())
                    {
                        StartWaiting();
                        Vector3 tNextPatrolPoint = _director.RequestPatrolDestination();
                        _movement.SetDestination(tNextPatrolPoint);
                    }
                    break;
            }
        }

        private void StartWaiting()
        {
            _isWaiting = true;
            _waitTimer = _investigateWaitTime;
            // TODO: 두리번거리는 애니메이션 재생
        }

        //TODO : 사망 점프 스퀘어 구현필요

        #endregion
        #region MasterAI 명령

        public void OrderRetreat(Vector3 retreatPos)
        {
            _isRetreating = true;
            _isWaiting = false;
            CurrentConfidence = CHASEAI_CONFIDENCE_STATE.LOW;

            _movement.SetDestination(retreatPos);
        }

        private void ProcessRetreat()
        {
            if(_movement.HasReachedDestination(0.5f))
            {
                gameObject.SetActive(false); // 퇴근 장소에 도착하면 비활성화
            }
        }

        public CHASEAI_CONFIDENCE_STATE GetCurrentConfidence()
        {
            return CurrentConfidence;
        }
        #endregion
    }
}

