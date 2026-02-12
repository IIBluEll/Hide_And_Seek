using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI.MasterAI
{
    [Serializable]
    public class Master_ZoneSystem : MonoBehaviour
    {
        [Header("Zone Configuration")]
        [SerializeField] private List<ZoneInfo> _zoneList = new List<ZoneInfo>();

        public Vector3 GetRandomPatrolDestination()
        {
            if(_zoneList.Count == 0)
            {
                Debug.LogError("[Master_ZoneSystem] 등록된 Zone이 없습니다.");
                return transform.position;
            }

            //TODO : 존 선택 로직 개선 필요 && 고도화 필요
            int tRandomIndex = UnityEngine.Random.Range(0, _zoneList.Count);
            return _zoneList[tRandomIndex].GetRandomSearchPoint();
        }
    }
}

