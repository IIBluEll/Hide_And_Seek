using System;
using UnityEngine;
using Random = UnityEngine.Random;

public enum EZONE_ID
{
    START_ZONE,
    ENGINEROOM_ZONE,
    RESTAURANT_ZONE,
    WAREHOUSE_ZONE,
    ZONE_04,
    ZONE_05
}

public class ZoneArea : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private EZONE_ID _zoneId;

    [Header("Volume")]
    [SerializeField] private Collider _triggerCol;

    [Header("Graph")]
    [SerializeField] private ZoneArea[] _arr_neighbors;

    [Header("Points")]
    [SerializeField] private Transform[] _arr_patrolPoints;
    [SerializeField] private Transform[] _arr_investigatePoints;
    [SerializeField] private Transform _ventSpot;
    [SerializeField] private Transform _cctvPoint;

    public EZONE_ID ZoneIds => _zoneId;

    public ZoneArea[] Neighbors => _arr_neighbors;

    public Transform VentSpot => _ventSpot;

    #region ¿ÜºÎ API
    public Vector3 GetCenterPos()
    {
        if ( _triggerCol != null )
        {
            return _triggerCol.bounds.center;
        }

        return transform.position;
    }

    public Transform GetRandomPatrolPoint()
    {
        if ( _arr_patrolPoints == null || _arr_patrolPoints.Length <= 0 )
        {
            return null;
        }

        var tIndex = Random.Range(0, _arr_patrolPoints.Length);
        return _arr_patrolPoints[ tIndex ];
    }

    public Transform GetRandomInvestigatePoint()
    {
        if ( _arr_investigatePoints == null || _arr_investigatePoints.Length <= 0 )
        {
            return null;
        }

        var tIndex = Random.Range(0, _arr_investigatePoints.Length);
        return _arr_investigatePoints[ tIndex ];
    }

    public bool ContainsWorldPos(Vector3 worldPos)
    {
        if(_triggerCol == null)
        {
            return false;
        }

        return _triggerCol.bounds.Contains(worldPos);
    }
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if ( _triggerCol == null )
            return;

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireCube(_triggerCol.bounds.center , _triggerCol.bounds.size);
    }
#endif
}
