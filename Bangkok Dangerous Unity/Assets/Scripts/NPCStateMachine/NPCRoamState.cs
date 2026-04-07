using UnityEngine;

public class NPCRoamState : NPCState
{
    public NPCRoamState(NPCStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.Agent.isStopped = false;

        stateMachine.Animator.ResetTrigger("Smoke");
        stateMachine.Animator.ResetTrigger("Idle");
        stateMachine.Animator.ResetTrigger("Talk");

        if (stateMachine.TryGetRandomRoamPoint(out Vector3 destination))
        {
            stateMachine.Agent.SetDestination(destination);
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
            stateMachine.HandleRoamDestinationReached();
        }
    }
}