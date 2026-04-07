using UnityEngine;

public abstract class NPCState
{
    protected NPCStateMachine stateMachine;

    public NPCState(NPCStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Tick() { }
    public virtual void Exit() { }
}
