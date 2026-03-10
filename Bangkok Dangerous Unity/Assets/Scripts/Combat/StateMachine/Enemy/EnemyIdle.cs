using UnityEngine;

public class EnemyIdle : IState
{
    //This state handles the idle behavior of the enemy - idle animation, patrolling and looking for the player.

    public void ExecuteState()
    {
        Debug.Log("Enemy is idling.");
    }

    public void OnEnterState()
    {
        
    }

    public void OnExitState()
    {
        
    }
}
