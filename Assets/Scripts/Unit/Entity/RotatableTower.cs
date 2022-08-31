using UnityEngine;

public class RotatableTower : MonoBehaviour
{
    [SerializeField] private float _maxAngularSpeed = default;
    [SerializeField] private Transform _towerAnchor = default;

    public float MaxAngularSpeed => _maxAngularSpeed;
    public Transform TowerAnchor => _towerAnchor;

    public void RotateTo(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion q = Quaternion.Slerp(_towerAnchor.rotation, targetRotation, _maxAngularSpeed * Mathf.Deg2Rad * Time.deltaTime);
        _towerAnchor.rotation = Quaternion.RotateTowards(_towerAnchor.rotation, q, _maxAngularSpeed * Time.deltaTime);
    }
}
