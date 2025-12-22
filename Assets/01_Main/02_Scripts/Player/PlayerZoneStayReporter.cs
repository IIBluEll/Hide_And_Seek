using UnityEngine;
public interface IPlayerStayConsumer
{
    void OnPlayerStayThresholdReached(ZoneArea zoneArea , float stayThresholdSec , int triggerCount);
}

public class PlayerZoneStayReporter : MonoBehaviour
{
    [SerializeField] private float _stayThresholdSec = 3f;

    private IPlayerStayConsumer _stayConsumer;

    private ZoneArea _currentZone;

    private float _stayAccSec;
    private int _triggerCount;
    private bool _isReportingEnabled;

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

        _stayAccSec -= _stayThresholdSec;
        _triggerCount++;

        _stayConsumer?.OnPlayerStayThresholdReached(_currentZone , _stayThresholdSec , _triggerCount);
    }

    public void Initialize(IPlayerStayConsumer stayConsumer)
    {
        _stayConsumer = stayConsumer;
    }
    
    public void SetReportingEnable(bool isEnabled)
    {
        _isReportingEnabled = isEnabled;
        _stayAccSec = 0.0f;
        _triggerCount = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        var tZoneArea = other.GetComponentInParent<ZoneArea>();
        
        if ( tZoneArea == null )
        {
            return;
        }

        SetCurrentZone(tZoneArea);
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
    }

    private void SetCurrentZone(ZoneArea nextZoneArea)
    {
        if ( _currentZone == nextZoneArea )
        {
            return;
        }

        _currentZone = nextZoneArea;
        _stayAccSec = 0f;
        _triggerCount = 0;
    }
}
