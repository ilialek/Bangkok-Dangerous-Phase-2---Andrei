using UnityEngine;

namespace GenericBehaviorTree
{
    public class IsPlayerInRangeNode : Node
    {
        private Transform _self;
        private float _range;

        public IsPlayerInRangeNode(Transform self, float range)
        {
            _self = self;
            _range = range;
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

            state = distance <= _range ? NodeState.SUCCESS : NodeState.FAILURE;
            return state;
        }
    }
}
