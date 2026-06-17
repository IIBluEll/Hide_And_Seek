using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavmeshAgent 전담
/// </summary>
public class ManageChaseMovement
{
    private readonly NavMeshAgent _agent;

    public ManageChaseMovement(NavMeshAgent agent)
    {
        _agent = agent;
    }

    public void MoveAgent(Vector3 pos, float speed)
    {
        _agent.isStopped = false;
        _agent.speed = speed;
        _agent.SetDestination(pos);
    }

    public void Warp(Vector3 pos)
    {
        _agent.Warp(pos);
    }

    public void SetDestination(Vector3 pos)
    {
        _agent.SetDestination(pos);
    }

    public void ResetPath()
    {
        _agent.ResetPath();
    }
    
    public bool IsPathPending()
    {
        return _agent.pathPending;
    }

    public bool IsArrived()
    {
        if(_agent.pathPending)
        {
            return false;
        }

        if ( _agent.remainingDistance <= _agent.stoppingDistance )
        {
            if(_agent.hasPath || _agent.velocity.sqrMagnitude == 0f)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsArrivedForRetreat()
    {
        if(_agent.pathPending)
        {
            return false;
        }

        return _agent.remainingDistance <= _agent.stoppingDistance;
    }
}
