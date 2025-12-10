using UnityEngine;

public class PlayerZoneTracker : MonoBehaviour
{
    [Header("체류 판정 설정")]
    public float LoiterThreshold = 3f;     // 이 시간 이상 머무르면 '수상' 판정 시작
    public float ReportInterval = 1f;      // MasterAI에 보고하는 주기(초)

    private ZoneArea _currentZone;

    private float _stayTimer;

    private float _nextReportTime;

    private void Update()
    {
        if ( _currentZone == null )
        {
            return;
        }

        _stayTimer += Time.deltaTime;

        // 체류 시간이 기준치 미만이면 보고하지 않음
        if ( _stayTimer < LoiterThreshold )
        {
            return;
        }

        // ReportInterval 주기로 MasterAI에 체류 상태 보고
        if ( Time.time >= _nextReportTime )
        {
            if ( MasterAIProvider.Instance != null )
            {
                // 최근 ReportInterval 동안 머문 시간으로 보고
                float tDeltaStay = ReportInterval;
                MasterAIProvider.Instance.ReportPlayerLoitering(_currentZone.ZoneId , tDeltaStay);
            }
            _nextReportTime = Time.time + ReportInterval;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ZoneArea tZone = other.GetComponent<ZoneArea>();
        if ( tZone == null )
        {
            return;
        }

        _currentZone = tZone;
        _stayTimer = 0f;
        _nextReportTime = Time.time + ReportInterval;
        // 필요 시 개발용 로그 남김
        DevConsoleProvider.Log($"[PlayerZone] Enter zone: {_currentZone.ZoneId}");
        Debug.Log($"[PlayerZone] Enter zone: {_currentZone.ZoneId}");
    }

    private void OnTriggerExit(Collider other)
    {
        ZoneArea tZone = other.GetComponent<ZoneArea>();
        if ( tZone == null )
        {
            return;
        }
        if ( tZone == _currentZone )
        {
            // 존을 벗어나면 타이머 초기화
            Debug.Log($"[PlayerZone] Exit zone: {_currentZone.ZoneId}");
            DevConsoleProvider.Log($"[PlayerZone] Exit zone: {_currentZone.ZoneId}");
            _currentZone = null;
            _stayTimer = 0f;

        }
    }
}
