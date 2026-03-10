using UnityEngine;

public class EnemyMove : IState
{
    private EnemyAI enemy;
    private float moveSpeed;
    private float rotationSpeed;
    private Transform target;

    private CharacterController characterController;

    public EnemyMove(EnemyAI owner, float speed, float rotation, Transform player)
    {
        enemy = owner;
        moveSpeed = speed;
        rotationSpeed = rotation;
        target = player;

        characterController = enemy.GetComponent<CharacterController>();
    }

    public void ExecuteState()
    {
        MoveTowardsPlayer();
    }

    public void OnEnterState()
    {
        
    }

    public void OnExitState()
    {
        
    }

    private void MoveTowardsPlayer()
    {
        Vector3 direction = (target.position - enemy.transform.position).normalized;
        characterController.Move(direction * moveSpeed * Time.deltaTime);

        Vector3 lookDirection = new Vector3(direction.x, 0, direction.z);
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            enemy.gameObject.transform.rotation = Quaternion.Slerp(enemy.gameObject.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
