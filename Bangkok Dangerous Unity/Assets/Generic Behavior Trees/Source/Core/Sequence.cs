using System.Collections.Generic;

namespace GenericBehaviorTree
{
    public class Sequence : Node
    {
        public Sequence() : base() { }
        public Sequence(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        state = NodeState.FAILURE;
                        return state;

                    case NodeState.RUNNING:
                        state = NodeState.RUNNING;
                        return state;

                    case NodeState.SUCCESS:
                        continue;

                    default:
                        state = NodeState.FAILURE;
                        return state;
                }
            }

            state = NodeState.SUCCESS;
            return state;
        }
    }
}