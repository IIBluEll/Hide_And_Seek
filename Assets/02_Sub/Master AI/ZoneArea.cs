using UnityEngine;

public class ZoneArea : MonoBehaviour
{
    [SerializeField] private int _zoneId = 0;
    public int ZoneId => _zoneId;

    private string _playerTag = "Player";

    [SerializeField]
    private float _reportInterval = 1f;

    private bool _isPlayerInside = false;
    private float _stayTimer = 0f;

    private void Update()
    {
        if ( !_isPlayerInside )
        {
            return;
        }

        float tDeltaTime = Time.deltaTime;
        _stayTimer += tDeltaTime;

        if ( _stayTimer >= _reportInterval )
        {
            _stayTimer = 0f;

            if ( MasterAI_Provider.Instance != null )
            {
                MasterAI_Provider.Instance.ReportPlayerStay(_zoneId);
            }
        }
    }
    private bool IsPlayer(Collider collider)
    {
        return collider.CompareTag(_playerTag);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ( !IsPlayer(other) )
        {
            return;
        }

        _isPlayerInside = true;
        _stayTimer = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        if ( !IsPlayer(other) )
        {
            return;
        }

        _isPlayerInside = false;
        _stayTimer = 0f;
    }

}
