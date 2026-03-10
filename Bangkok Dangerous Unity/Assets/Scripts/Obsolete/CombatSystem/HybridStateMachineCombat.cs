using UnityEngine;
using UnityEngine.InputSystem;

public class HybridStateMachineCombat : MonoBehaviour
{
    public enum CombatState { Idle, Attacking, ComboWindow, Recovery }
    
    [Header("Combat Setup")]
    [SerializeField] private CombatStyleSO combatStyle;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Animator animator;
    
    [Header("State Machine")]
    [SerializeField] private CombatState currentState = CombatState.Idle;
    [SerializeField] private float stateTimer = 0f;
    
    private AttackSO currentAttack;
    private string inputBuffer = "";
    private float lastInputTime;
    private int comboCount = 0;
    
    private void Update()
    {
        stateTimer += Time.deltaTime;
        
        switch (currentState)
        {
            case CombatState.Idle:
                HandleIdleState();
                break;
            case CombatState.Attacking:
                HandleAttackingState();
                break;
            case CombatState.ComboWindow:
                HandleComboWindowState();
                break;
            case CombatState.Recovery:
                HandleRecoveryState();
                break;
        }
    }
    
    private void HandleIdleState()
    {
        if (playerInput.actions["Attack"].triggered)
        {
            StartAttack(combatStyle.startingLightAttack, "L");
        }
        else if (playerInput.actions["Attack Heavy"].triggered)
        {
            StartAttack(combatStyle.startingHeavyAttack, "H");
        }
    }
    
    private void HandleAttackingState()
    {
        //float normalizedTime = stateTimer / currentAttack.animationDuration;
        
        //if (normalizedTime >= currentAttack.comboWindowStart)
        //{
        //    ChangeState(CombatState.ComboWindow);
        //}
    }
    
    private void HandleComboWindowState()
    {
        //float normalizedTime = stateTimer / currentAttack.animationDuration;
        
        //if (playerInput.actions["Attack"].triggered)
        //{
        //    inputBuffer += "L";
        //    TryExecuteCombo();
        //}
        //else if (playerInput.actions["Attack Heavy"].triggered)
        //{
        //    inputBuffer += "H";
        //    TryExecuteCombo();
        //}
        
        //if (normalizedTime >= currentAttack.comboWindowEnd)
        //{
        //    ChangeState(CombatState.Recovery);
        //}
    }
    
    private void HandleRecoveryState()
    {
        //float normalizedTime = stateTimer / currentAttack.animationDuration;
        
        //if (normalizedTime >= 1f)
        //{
        //    ChangeState(CombatState.Idle);
        //    ResetCombo();
        //}
    }
    
    private void StartAttack(AttackSO attack, string input)
    {
        currentAttack = attack;
        inputBuffer = input;
        comboCount = 1;
        
        animator.SetTrigger(attack.animationTrigger);
        animator.SetLayerWeight(1, 1);
        ChangeState(CombatState.Attacking);
        
        Debug.Log($"Starting attack: {attack.name} with input: {inputBuffer}");
    }
    
    private void TryExecuteCombo()
    {
        AttackSO nextAttack = null;
        
        if (inputBuffer.EndsWith("L") && currentAttack.lightAttackFollowUps.Length > 0)
        {
            nextAttack = currentAttack.lightAttackFollowUps[0];
        }
        else if (inputBuffer.EndsWith("H") && currentAttack.heavyAttackFollowUps.Length > 0)
        {
            nextAttack = currentAttack.heavyAttackFollowUps[0];
        }
        
        if (nextAttack != null)
        {
            currentAttack = nextAttack;
            comboCount++;
            
            animator.SetTrigger(nextAttack.animationTrigger);
            ChangeState(CombatState.Attacking);
            
            Debug.Log($"Combo continued: {inputBuffer} -> {nextAttack.name}");
        }
    }
    
    private void ChangeState(CombatState newState)
    {
        currentState = newState;
        stateTimer = 0f;
    }
    
    private void ResetCombo()
    {
        currentAttack = null;
        inputBuffer = "";
        comboCount = 0;
        animator.SetLayerWeight(1, 0);
    }
    
    public bool IsAttacking() => currentState != CombatState.Idle;
    public bool CanMove() => currentState == CombatState.Idle || 
                            (currentAttack != null && currentAttack.canCancelToMovement);
    public CombatState GetCurrentState() => currentState;
}
