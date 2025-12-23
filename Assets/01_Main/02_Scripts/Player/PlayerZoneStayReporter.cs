using UnityEngine;
public interface IPlayerStayConsumer
{
    void OnPlayerStayThresholdReached(ZoneArea zoneArea);
}

public class PlayerZoneStayReporter : MonoBehaviour
{
    [SerializeField] private float _stayThresholdSec = 10f;

    private IPlayerStayConsumer _stayConsumer;

    private ZoneArea _currentZone;

    private float _stayAccSec;
    [SerializeField] private bool _isReportingEnabled;

    private void Update()
    {
        if ( !_isReportingEnabled || _currentZone == null )
        {
            return;
        }

        _stayAccSec += Time.deltaTime;
        
        if ( _stayAccSec < _stayThresholdSec )
        {
            return;
        }

        _stayAccSec = 0;

        _stayConsumer?.OnPlayerStayThresholdReached(_currentZone);
    }

    public void Initialize(IPlayerStayConsumer stayConsumer)
    {
        _stayConsumer = stayConsumer;
    }
    
    public void SetReportingEnable(bool isEnabled)
    {
        _isReportingEnabled = isEnabled;
        _stayAccSec = 0.0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        var tZoneArea = other.GetComponentInParent<ZoneArea>();
        
        if ( tZoneArea == null )
        {
            return;
        }

        SetCurrentZone(tZoneArea);

        Debug.Log($"트리거 진입 : {tZoneArea.ZoneIds}");
    }

    private void OnTriggerExit(Collider other)
    {
        var tZoneArea = other.GetComponentInParent<ZoneArea>();

        if ( tZoneArea == null )
        {
            return;
        }

        if ( _currentZone == tZoneArea )
        {
            SetCurrentZone(null);
        }

        Debug.Log($"트리거 퇴장 : {tZoneArea.ZoneIds}");
    }

    private void SetCurrentZone(ZoneArea nextZoneArea)
    {
        if ( _currentZone == nextZoneArea )
        {
            return;
        }

        _currentZone = nextZoneArea;
        _stayAccSec = 0f;
    }
}
