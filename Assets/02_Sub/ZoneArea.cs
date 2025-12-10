using UnityEngine;

public class ZoneArea : MonoBehaviour
{
    [field: SerializeField]
    public ZONE_ID ZoneId { get; private set; }

    [SerializeField]
    private Transform _centerTrans;

    public Vector3 CenterPosition
    {
        get
        {
            if ( _centerTrans != null )
            {
                return _centerTrans.position;
            }

            return transform.position;
        }
    }

    private void Awake()
    {
        // 마스터 AI에 자신을 등록
        if ( MasterAIProvider.Instance != null )
        {
            MasterAIProvider.Instance.RegisterZone(this);
        }
        else
        {
            Debug.LogError("[ZoneArea] MasterAIProvider 가 씬에 없습니다.");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 tCenter = _centerTrans != null ? _centerTrans.position : transform.position;
        Gizmos.DrawWireSphere(tCenter , 0.5f);
    }
#endif
}
