using UnityEngine;
using UnityEngine.AI;

public class Pathfinder : MonoBehaviour
{
    private NavMeshAgent _agent;
    private NavMeshObstacle _obstacle;

    public NavMeshAgent Agent => _agent;
    public NavMeshObstacle Obstacle => _obstacle;
    
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _obstacle = GetComponent<NavMeshObstacle>();
    }
}
