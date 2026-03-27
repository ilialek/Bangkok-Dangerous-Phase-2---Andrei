using UnityEngine;

public class NPCIdleState : NPCState
{
    private float idleTimer;

    public NPCIdleState(NPCStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        idleTimer = stateMachine.GetRandomIdleDuration();
        stateMachine.agent.isStopped = true;
        stateMachine.agent.ResetPath();
    }

    public override void Tick()
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f)
        {
            stateMachine.ChangeState(stateMachine.RoamState);
        }
    }

    public override void Exit()
    {
    }
}