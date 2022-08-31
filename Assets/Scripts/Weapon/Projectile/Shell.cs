using System.Collections;
using UnityEngine;

public class Shell : Projectile
{
    protected override IEnumerator Fly()
    {
        // ��һ֡����λ�ã�������Ч��λ��
        yield return null;
        
        while (true)
        {
            _transform.forward = Vector3.Lerp(_transform.forward, (_targetOriginPosition - _transform.position).normalized,
                Time.deltaTime * _maxSpeed * 0.1f);
            _transform.position += _transform.forward * Time.deltaTime * _maxSpeed;
            
            yield return null;
        }
    }
}