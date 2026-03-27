using UnityEngine;

public class NPCRoamState : NPCState
{
    public NPCRoamState(NPCStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.agent.isStopped = false;

        if (stateMachine.TryGetRandomRoamPoint(out Vector3 destination))
        {
            stateMachine.agent.SetDestination(destination);
        }
        else
        {
            stateMachine.ChangeState(stateMachine.IdleState);
        }
    }

    public override void Tick()
    {
        if (stateMachine.HasReachedDestination())
        {
            stateMachine.ChangeState(stateMachine.IdleState);
        }
    }

    public override void Exit()
    {
    }
}