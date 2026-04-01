using UnityEngine;

public class NPCIdleState : NPCStationaryState
{
    public NPCIdleState(NPCStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        base.Enter();

        stateTimer = Random.Range(
            stateMachine.IdleConfig.minIdleTime, 
            stateMachine.IdleConfig.maxIdleTime
            );

        int idleVariant = stateMachine.IdleConfig.GetRandomIdleVariant();
        stateMachine.Animator.SetInteger("IdleVariant", idleVariant);   
        stateMachine.Animator.SetTrigger("Idle");   
    }

    public override void Tick()
    {
        TickTimer();

        if (IsTimerFinished())
        {
            stateMachine.NotifyStationaryStateFinished();
        }
    }
}