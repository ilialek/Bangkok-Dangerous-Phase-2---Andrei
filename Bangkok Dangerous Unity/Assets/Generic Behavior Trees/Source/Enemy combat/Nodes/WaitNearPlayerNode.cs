using UnityEngine;
using UnityEngine.AI;

namespace GenericBehaviorTree
{
    public class WaitNearPlayerNode : Node
    {
        private Transform _self;
        private NavMeshAgent _agent;
        private float _waitRange;

        public WaitNearPlayerNode(Transform self, NavMeshAgent agent, float waitRange)
        {
            _self = self;
            _agent = agent;
            _waitRange = waitRange;
        }

        public override NodeState Evaluate()
        {
            object targetObject = GetData("target");

            if (targetObject == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            Transform target = (Transform)targetObject;
            if (target == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            float distance = Vector3.Distance(_self.position, target.position);

            if (distance > _waitRange)
            {
                state = NodeState.FAILURE;
                return state;
            }

            NavMeshAgentNavigation.Stop(_agent);

            Vector3 lookPos = target.position - _self.position;
            lookPos.y = 0f;

            if (lookPos != Vector3.zero)
                _self.rotation = Quaternion.LookRotation(lookPos);

            state = NodeState.RUNNING;
            return state;
        }
    }
}
