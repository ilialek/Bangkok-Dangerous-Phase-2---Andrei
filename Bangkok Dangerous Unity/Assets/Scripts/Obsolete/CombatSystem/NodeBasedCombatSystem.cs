using UnityEngine;
using UnityEngine.InputSystem;

public class NodeBasedCombatSystem : MonoBehaviour
{
    [Header("Combat Setup")]
    [SerializeField] private CombatStyleSO combatStyle;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Animator animator;
    
    [Header("State")]
    private AttackSO currentAttack;
    private bool isAttacking = false;
    private bool inComboWindow = false;
    private float attackTimer = 0f;
    
    private void Update()
    {
        if (isAttacking)
        {
            UpdateAttackState();
        }
        
        HandleCombatInput();
    }
    
    private void HandleCombatInput()
    {
        if (playerInput.actions["Attack"].triggered)
        {
            TryExecuteLightAttack();
        }
        else if (playerInput.actions["Attack Heavy"].triggered)
        {
            TryExecuteHeavyAttack();
        }
    }
    
    private void TryExecuteLightAttack()
    {
        AttackSO attackToExecute = null;
        
        if (!isAttacking)
        {
            // Start new combo
            attackToExecute = combatStyle.startingLightAttack;
        }
        else if (inComboWindow && currentAttack.lightAttackFollowUps.Length > 0)
        {
            // Continue combo
            attackToExecute = currentAttack.lightAttackFollowUps[0];
        }
        
        if (attackToExecute != null)
        {
            ExecuteAttack(attackToExecute);
        }
    }
    
    private void TryExecuteHeavyAttack()
    {
        AttackSO attackToExecute = null;
        
        if (!isAttacking)
        {
            attackToExecute = combatStyle.startingHeavyAttack;
        }
        else if (inComboWindow && currentAttack.heavyAttackFollowUps.Length > 0)
        {
            attackToExecute = currentAttack.heavyAttackFollowUps[0];
        }
        
        if (attackToExecute != null)
        {
            ExecuteAttack(attackToExecute);
        }
    }
    
    private void ExecuteAttack(AttackSO attack)
    {
        currentAttack = attack;
        isAttacking = true;
        inComboWindow = false;
        attackTimer = 0f;
        
        animator.SetTrigger(attack.animationTrigger);
        animator.SetLayerWeight(1, 1);
        Debug.Log($"Executing attack: {attack.name}");
    }
    
    private void UpdateAttackState()
    {
        attackTimer += Time.deltaTime;
        //float normalizedTime = attackTimer / currentAttack.animationDuration;
        
        //// Check if we're in combo window
        //if (normalizedTime >= currentAttack.comboWindowStart && 
        //    normalizedTime <= currentAttack.comboWindowEnd)
        //{
        //    inComboWindow = true;
        //}
        //else
        //{
        //    inComboWindow = false;
        //}
        
        // Check if attack is finished
        //if (normalizedTime >= 1f)
        //{
        //    EndAttack();
        //}
    }
    
    private void EndAttack()
    {
        isAttacking = false;
        inComboWindow = false;
        currentAttack = null;
        attackTimer = 0f;
        animator.SetLayerWeight(1, 0);
    }
    
    public bool IsAttacking() => isAttacking;
    
    // Fixed CanMove method
    public bool CanMove() 
    { 
        // If not attacking at all, can always move
        if (!isAttacking) 
            return true;
            
        // If attacking and canCancelToMovement is true, can move
        if (currentAttack != null && currentAttack.canCancelToMovement) 
            return true;
            
        // If attacking and canCancelToMovement is false, cannot move
        return false;
    }
}
