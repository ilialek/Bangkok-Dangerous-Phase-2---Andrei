using UnityEngine;

public class NPCTalkingState : NPCStationaryState
{
    public NPCTalkingState(NPCStateMachine stateMachine)     : base(stateMachine) { }

    public override void Enter()
    {
        base.Enter();

        stateTimer = Random.Range(
            stateMachine.TalkingConfig.minTalkingTime, 
            stateMachine.TalkingConfig.maxTalkingTime
            );

        stateMachine.Animator.SetTrigger("Talk");
    }

    public override void Tick()
    {
        TickTimer();
        
        if (IsTimerFinished())
        {
            stateMachine.ChangeState(stateMachine.RoamState);
        }
    }
}
