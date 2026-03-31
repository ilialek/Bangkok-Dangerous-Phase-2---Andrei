using UnityEngine;

public class NPCIdleState : NPCState
{
    private float idleTimer;

    public NPCIdleState(NPCStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        NPCIdleStateConfig config = stateMachine.IdleConfig;

        if (config == null)
        {
            Debug.LogWarning($"{stateMachine.name}: Missing Idle State Config.");
            return;
        }

        idleTimer = Random.Range(config.minIdleTime, config.maxIdleTime);

        if (stateMachine.Agent != null)
        {
            stateMachine.Agent.isStopped = true;
            stateMachine.Agent.ResetPath();
        }
    }

    public override void Tick()
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f)
        {
            stateMachine.ChangeState(stateMachine.RoamState);
        }
    }
}