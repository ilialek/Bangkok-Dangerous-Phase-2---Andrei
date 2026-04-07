using UnityEngine;

public class NPCStationaryState : NPCState
{
    protected float stateTimer;

    public NPCStationaryState(NPCStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.LogWarning($"NPC: {stateMachine.gameObject.name} entered stationary state: {GetType().Name}");
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

    public override void Exit()
    {
        Debug.LogWarning($"NPC: {stateMachine.gameObject.name} left stationary state: {GetType().Name}");
    }
}
