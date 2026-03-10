using UnityEngine;

public interface IState
{
    void OnEnterState();
    void OnExitState();
    void ExecuteState();
}
