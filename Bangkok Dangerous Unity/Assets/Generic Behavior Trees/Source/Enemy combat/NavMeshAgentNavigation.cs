using UnityEngine;
using UnityEngine.AI;

namespace GenericBehaviorTree
{
    public static class NavMeshAgentNavigation
    {
        public static bool IsReady(NavMeshAgent agent)
        {
            return agent != null && agent.enabled && agent.isOnNavMesh;
        }

        public static void Stop(NavMeshAgent agent)
        {
            if (!IsReady(agent)) return;

            if (agent.hasPath)
                agent.ResetPath();

            agent.isStopped = true;
        }

        public static bool MoveTo(NavMeshAgent agent, Vector3 destination, float stoppingDistance, float repathThreshold)
        {
            if (!IsReady(agent)) return false;

            agent.isStopped = false;
            agent.stoppingDistance = stoppingDistance;

            if (!agent.hasPath || Vector3.Distance(agent.destination, destination) > repathThreshold)
                return agent.SetDestination(destination);

            return true;
        }
    }
}
