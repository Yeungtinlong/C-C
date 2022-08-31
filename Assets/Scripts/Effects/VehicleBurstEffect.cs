using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleBurstEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem _bigSmoke = default;
    [SerializeField] private ParticleSystem _spark = default;
    [SerializeField] private ParticleSystem _explosion = default;
    [SerializeField] private Rigidbody[] _burstObjects = default;
    [SerializeField] private float _burstDelay = default;
    private float _burstTimer = 0f;

    private void OnEnable()
    {
        for (int i = 0; i < _burstObjects.Length; i++)
        {
            _burstObjects[i].isKinematic = true;
        }
    }

    public void Play()
    {
        _bigSmoke.Play();
        _spark.Play();
        StartCoroutine(StartBurst());
    }

    public void Stop()
    {
        _bigSmoke.Stop();
        _spark.Stop();
    }

    private IEnumerator StartBurst()
    {
        while (true)
        {
            _burstTimer += Time.deltaTime;

            if (_burstTimer > _burstDelay)
            {
                _burstTimer = 0f;
                Burst();
                yield break;
            }

            yield return null;
        }
    }

    private void Burst()
    {
        _bigSmoke.Stop();
        _spark.Stop();
        _explosion.Play();
        
        float randomX = UnityEngine.Random.Range(-2f, 2f);
        float randomZ = UnityEngine.Random.Range(-2f, 2f);
        
        for (int i = 0; i < _burstObjects.Length; i++)
        {
            _burstObjects[i].isKinematic = false;
            _burstObjects[i].AddExplosionForce(300f, transform.position + new Vector3(randomX, 0f, randomZ), 0f, 10f);
        }
    }
}