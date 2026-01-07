using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NoiseObject : MonoBehaviour
{
    [Header("소음 설정")]
    [SerializeField] private float _noiseLoudness = 1f;
    [SerializeField] private float _noiseRadius = 15f;
    [SerializeField] private float _collisionThreshold = 2f;

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

        if(MasterAI_Provider.Instance != null)
        {
            MasterAI_Provider.Instance.ReportNoise(transform.position, _noiseLoudness);
        }
    }
}
