using UnityEngine;
using UnityEngine.AI;

public class NavMeshMovement : MonoBehaviour
{
    [SerializeField] private Transform targetPositionTransform;

    private NavMeshAgent navMeshAgent;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }
    
    private void Update()
    {
        navMeshAgent.destination = targetPositionTransform.position;
    }
}
