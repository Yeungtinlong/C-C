using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailLight : Projectile
{
    [SerializeField] private LineRenderer _railLight = default;
    // [SerializeField] private LayerMask _unitLayer = default;
    [SerializeField] private float _fadeOutTime = default;

    private Material _originMat;
    private Material _currentMat;
    private Color _currentColor;

    protected override void PlayEffects() { }

    protected override void StopEffects() { }

    protected override void OnTriggerEnter(Collider other) { }

    protected override IEnumerator Fly()
    {
        _originMat = _railLight.material;
        _currentMat = _originMat;
        _currentColor = _currentMat.color;

        _railLight.positionCount = 2;
        _railLight.SetPosition(0, _launchOriginPosition);
        _railLight.SetPosition(1, _targetOriginPosition);

        MakeDamage(_target);
        IsValid = false;

        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime;

            if (timer > _fadeOutTime)
            {
                _railLight.positionCount = 0;
                OnHitTarget.Invoke(this);
                yield break;
            }

            Color.RGBToHSV(_currentColor, out float h, out float s, out float v);
            v = 2f * (_fadeOutTime - timer) / _fadeOutTime;
            _railLight.material.SetColor("_EmissionColor", Color.HSVToRGB(h, s, v));

            yield return null;
        }
    }
}
