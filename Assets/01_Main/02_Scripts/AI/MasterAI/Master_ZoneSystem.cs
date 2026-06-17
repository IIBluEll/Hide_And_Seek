using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI.MasterAI
{
    [Serializable]
    public class Master_ZoneSystem : MonoBehaviour
    {
        [Header("Zone Configuration")]
        [SerializeField] private List<ZoneInfo> _zoneList = new();

        [SerializeField] private List<Transform> _ventList = new();

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

        // 플레이어와 가장 멀리 떨어진 벤트 반환
        public Vector3 GetBestSpawnVent(Transform playerTransform)
        {
            if(_ventList.Count == 0)
            {
                return transform.position;
            }

            if(playerTransform == null)
            {
                Debug.LogError("[Master_ZoneSystem] Player Transform이 할당되지 않았습니다.");
                return _ventList[ 0 ].position;
            }

            Transform tFarVent = _ventList[0];
            float tMaxDis = float.MinValue;

            foreach(Transform tVent in _ventList)
            {
                float tDis = Vector3.Distance(playerTransform.position, tVent.position);

                if(tDis > tMaxDis)
                {
                    tMaxDis = tDis;
                    tFarVent = tVent;
                }
            }

            return tFarVent.position;
        }

        // 몬스터 위치에서 가장 가까운 벤트 반환
        public Vector3 GetNearestRetreatVent(Transform chaseTransform)
        {
            if(_ventList.Count == 0)
            {
                return transform.position;
            }

            Transform tNearVent = _ventList[0];
            float tMinDis = float.MaxValue;

            foreach(Transform tVent in _ventList)
            {
                float tDis = Vector3.Distance(chaseTransform.position, tVent.position);

                if(tDis < tMinDis)
                {
                    tMinDis = tDis;
                    tNearVent = tVent;
                }
            }

            return tNearVent.position;
        }
    }
}

