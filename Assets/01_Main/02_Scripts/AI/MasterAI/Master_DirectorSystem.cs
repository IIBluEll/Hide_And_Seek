using AI.ChaseAI;
using UnityEngine;

namespace AI.MasterAI
{
    public class Master_DirectorSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private MasterAI_Configs _config;

        [Space(5f), Header("Sub-Systems")]
        [SerializeField] private Master_GaugeSystem _gaugeSystem;
        [SerializeField] private Master_ZoneSystem _zoneSystem;

        [Space(5f), Header("Target")]
        [SerializeField] private Chase_Controller _chaseAI;
        [SerializeField] private Transform _playerTransform;

        private float _respawnTimer = 0f;
        private bool _isActivated = false;

        private MASTERAI_PHASE _currentPhase = MASTERAI_PHASE.ACTIVE;

        #region Unity LifeCycle

        private void Start()
        {
            // 게임 시작 시엔 무조건 몬스터를 숨기고 시스템 대기
            if ( _chaseAI != null )
            {
                _chaseAI.gameObject.SetActive(false);
            }

            _isActivated = false;
        }

        private void Update()
        {
            if ( !_isActivated || _chaseAI == null || _config == null ) return;

            float tDeltaTime = Time.deltaTime;

            switch ( _currentPhase )
            {
                case MASTERAI_PHASE.ACTIVE:
                    HandleActivePhase(tDeltaTime);
                    break;

                case MASTERAI_PHASE.DORMANT:
                    HandleDormantPhase(tDeltaTime);
                    break;
            }
        }
        #endregion

        #region Master AI 활동 변경
        private void SwitchToDormant()
        {
            Debug.Log("[Master_Director] Stress Full -> Retreat Order");

            _currentPhase = MASTERAI_PHASE.DORMANT;

            if(_chaseAI.gameObject.activeSelf)
            {
                Vector3 tRetreatPos = _zoneSystem.GetNearestRetreatVent(_chaseAI.transform);
                _chaseAI.OrderRetreat(tRetreatPos);
            }

            _respawnTimer = 0f;
        }

        private void SwitchToActive()
        {
            Debug.Log("[Master_Director] Respawn Timer Complete -> Active Phase");
            _currentPhase = MASTERAI_PHASE.ACTIVE;

            _gaugeSystem.ResetStress();
            SpawnChaseAI();
        }
        #endregion

        #region Master AI 활동 로직
        // 활동 상태일때
        private void HandleActivePhase(float deltaTime)
        {
            if ( !_chaseAI.gameObject.activeSelf )
            {
                SwitchToDormant();
                return;
            }

            var tCurrentConfidence = _chaseAI.isActiveAndEnabled ? _chaseAI.GetCurrentConfidence() : CHASEAI_CONFIDENCE_STATE.LOW;

            _gaugeSystem.UpdateGauage(deltaTime , _config , tCurrentConfidence);

            if(_gaugeSystem.IsStressFull(_config))
            {
                SwitchToDormant();
            }
        }

        // 휴면 상태일때
        private void HandleDormantPhase(float deltaTime)
        {
            _respawnTimer += deltaTime;

            if(_respawnTimer >= _config.RespawnCooldown)
            {
                SwitchToActive();
            }
        }
        #endregion

        #region Public API

        //외부 이벤트로 Maseter AI 시스템 활성화
        public void ActivateMasterAI()
        {
            if(_isActivated)
            {
                return;
            }

            Debug.Log("[Master_Director] System Activated. State: DORMANT");

            _isActivated = true;
            _gaugeSystem.Initialize();

            _currentPhase = MASTERAI_PHASE.DORMANT;
            _respawnTimer = 0f;
        }

        // MasterAi 시스템 비활성화
        public void DeactivateMasterAI()
        {
            _isActivated = false;
            _chaseAI.gameObject.SetActive(false);
            Debug.Log("[Master_Director] System Deactivated.");
        }

        public Vector3 RequestPatrolDestination()
        {
            return _zoneSystem.GetRandomPatrolDestination();
        }

        public CHASEAI_ANGER_PHASE GetChaseAIAngerPhase()
        {
            return _gaugeSystem.GetCurrentAngerPhase(_config);
        }

        #endregion

        private void SpawnChaseAI()
        {
            Vector3 tSpawnPos = _zoneSystem.GetBestSpawnVent(_playerTransform); 
            _chaseAI.transform.position = tSpawnPos;
            _chaseAI.gameObject.SetActive(true);
            _chaseAI.Initialize(_config); // 몬스터 초기화
        }
    }
}

