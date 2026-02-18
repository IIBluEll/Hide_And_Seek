using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NoiseObject : MonoBehaviour
{
    [Header("소음 설정")]
    [SerializeField] private float _noiseLoudness = 1f;
    [SerializeField] private float _noiseRadius = 15f;
    [SerializeField] private float _collisionThreshold = 2f;

    [Space(10f), Header("탐지 설정")]
    [SerializeField] private LayerMask _monsterMask;

    [Space(10f),Header("쿨타임")]
    [SerializeField] private float _noiseCooldown = 1f;
    private float _lastNoiseTime = 0f;

    private void OnCollisionEnter(Collision collision)
    {
        if(Time.time - _lastNoiseTime < _noiseCooldown)
        {
            return;
        }

        if(collision.relativeVelocity.magnitude < _collisionThreshold)
        {
            return;
        }

        MakeNoise();
    }

    private void MakeNoise()
    {
        _lastNoiseTime = Time.time;

        Collider[] tHitColliders = Physics.OverlapSphere(transform.position, _noiseRadius, _monsterMask);

        foreach ( Collider tCollider in tHitColliders )
        {
            AI.ChaseAI.Chase_Controller tMonster = tCollider.GetComponentInParent<AI.ChaseAI.Chase_Controller>();

            if ( tMonster != null )
            {
                //몬스터에게 소리 발생 위치 전달
                tMonster.OnAudioPerception(transform.position);
            }
        }

        Debug.Log($"[NoiseObject] 소음 발생! 반경: {_noiseRadius}m");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f , 0.5f , 0f , 0.3f); // 주황색 반투명
        Gizmos.DrawSphere(transform.position , _noiseRadius);
    }
}
