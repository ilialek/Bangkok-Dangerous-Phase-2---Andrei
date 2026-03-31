using UnityEngine;

public class NPCStationaryState : NPCState
{
    protected float stateTimer;

    public NPCStationaryState(NPCStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.Agent.isStopped = true;
        stateMachine.Agent.ResetPath();
    }

    protected void TickTimer()
    {
        stateTimer -= Time.deltaTime;
    }

    protected bool IsTimerFinished()
    {
        return stateTimer <= 0f;
    }   
}
