using UnityEngine;

public class NPCSmokingState : NPCStationaryState
{
    public NPCSmokingState(NPCStateMachine stateMachine) : base(stateMachine) { }
  
    public override void Enter()
    {
        base.Enter();
        stateTimer = Random.Range(
            stateMachine.SmokingConfig.minSmokingTime, 
            stateMachine.SmokingConfig.maxSmokingTime
            );
        stateMachine.Animator.SetTrigger("Smoke");
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
