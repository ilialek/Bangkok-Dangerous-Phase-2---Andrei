using UnityEngine;

namespace GenericBehaviorTree
{

    public class FindPlayerNode : Node
    {
        private Transform _self;
        private string _playerTag;
        private float _detectionRange;

        public FindPlayerNode(Transform self, string playerTag, float detectionRange)
        {
            _self = self;
            _playerTag = playerTag;
            _detectionRange = detectionRange;
        }

        public override NodeState Evaluate()
        {
            object targetObject = GetData("target");

            if (targetObject == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag(_playerTag);

                if (player == null)
                {
                    state = NodeState.FAILURE;
                    return state;
                }

                float distance = Vector3.Distance(_self.position, player.transform.position);
                if (distance > _detectionRange)
                {
                    state = NodeState.FAILURE;
                    return state;
                }

                parent.SetData("target", player.transform);
                state = NodeState.SUCCESS;
                return state;
            }

            Transform target = (Transform)targetObject;
            if (target == null)
            {
                ClearData("target");
                state = NodeState.FAILURE;
                return state;
            }

            float currentDistance = Vector3.Distance(_self.position, target.position);
            if (currentDistance > _detectionRange)
            {
                ClearData("target");
                state = NodeState.FAILURE;
                return state;
            }

            state = NodeState.SUCCESS;
            return state;
        }
    }
}
