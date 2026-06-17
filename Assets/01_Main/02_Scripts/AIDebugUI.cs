using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AIDebugUI : MonoBehaviour
{
    //[Header("UI References")]
    //[SerializeField] private TMP_Text _txtGlobalState;   // 예: Director: ACTIVE / Alien: CHASE
    //[SerializeField] private TMP_Text _txtValues;        // 예: Stress: 50 / Alert: 100

    //[Header("Gauges")]
    //[SerializeField] private Slider _sliderStress;   // 피로도 게이지
    //[SerializeField] private Image _fillStress;      // 피로도 색상 변경용
    //[SerializeField] private Slider _sliderAlert;    // 경계도 게이지
    //[SerializeField] private Image _fillAlert;       // 경계도 색상 변경용

    //// 내부 참조
    //private ChaseAI_Controller _chaser;

    //private void Start()
    //{
    //    // 씬에 있는 추격자 찾기
    //    _chaser = FindObjectOfType<ChaseAI_Controller>();

    //    // 슬라이더 범위 설정
    //    if ( _sliderStress ) _sliderStress.maxValue = 100f; // 혹은 150
    //    if ( _sliderAlert ) _sliderAlert.maxValue = 100f;
    //}

    //private void Update()
    //{
    //    // 싱글톤이 없으면 패스
    //    if ( MasterAI_Provider.Instance == null || _chaser == null ) return;

    //    UpdateTextInfo();
    //    UpdateGauges();
    //}

    //private void UpdateTextInfo()
    //{
    //    var director = MasterAI_Provider.Instance;

    //    // 상단 상태 텍스트
    //    // 예: [DIR: ACTIVE] [AI: PATROL]
    //    _txtGlobalState.text = $"[DIR: {director.CurrentPhase}]  [AI: {_chaser.CurrentState}]";

    //    // 상세 수치 텍스트
    //    // 예: Stress: 45.2  Alert: 0.0
    //    //_txtValues.text = $"Stress: {director.GlobalStress:F1}   Alert: {director.AreaAlert:F1}";
    //}

    //private void UpdateGauges()
    //{
    //    var director = MasterAI_Provider.Instance;

    //    // 1. 스트레스 (피로도) 게이지
    //    if ( _sliderStress )
    //    {
    //        float val = director.GlobalStress;
    //        _sliderStress.value = val;

    //        // 색상 연출: 낮으면 초록 -> 높으면 빨강
    //        if ( _fillStress )
    //            _fillStress.color = Color.Lerp(Color.green , Color.red , val / 100f);
    //    }

    //    // 2. 경계도 (의심) 게이지
    //    if ( _sliderAlert )
    //    {
    //        float val = director.AreaAlert;
    //        _sliderAlert.value = val;

    //        // 색상 연출: 낮으면 파랑(평온) -> 높으면 노랑/빨강(경고)
    //        if ( _fillAlert )
    //            _fillAlert.color = Color.Lerp(Color.cyan , Color.magenta , val / 100f);
    //    }
    //}
}
