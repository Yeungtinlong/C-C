using UnityEngine;

[CreateAssetMenu(menuName = "Entity Config/Count Config")]
public class CountConfigSO : ScriptableObject
{
    [SerializeField] private int _count = default;
    public int Count => _count;
}
