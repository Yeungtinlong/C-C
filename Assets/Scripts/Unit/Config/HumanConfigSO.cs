using UnityEngine;

[CreateAssetMenu(fileName = "HumanConfigSO", menuName = "Entity Config/Human Config")]
public class HumanConfigSO : ScriptableObject
{
    [Header("Movement Setting")]
    [SerializeField] private float _moveSpeed = default;
    [SerializeField] private float _layMoveSpeed = default;
    
    [Header("Sight Setting")]
    [SerializeField] private float _sightRangeOnStanding = default;
    [SerializeField] private float _sightRangeOnLaying = default;
    
    [Header("Damage Setting")]
    [SerializeField][Range(0f, 1f)] private float _reduceDamageScaleOnLaying = default;
    [SerializeField][Range(0f, 1f)] private float _reduceDamageScaleOnStanding = default;
    
    public float MoveSpeed => _moveSpeed;
    public float LayMoveSpeed => _layMoveSpeed;
    public float SightRangeOnStanding => _sightRangeOnStanding;
    public float SightRangeOnLaying => _sightRangeOnLaying;
    public float ReduceDamageScaleOnLaying => _reduceDamageScaleOnLaying;
    public float ReduceDamageScaleOnStanding => _reduceDamageScaleOnStanding;

}
