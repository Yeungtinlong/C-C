using UnityEngine;

[CreateAssetMenu(menuName = "Entity Config/Health Config")]
public class HealthConfigSO : ScriptableObject
{
    [SerializeField] private int _maxHealth = default;
    public int MaxHealth => _maxHealth;
}
