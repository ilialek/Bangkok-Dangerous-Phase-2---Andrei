using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputBufferCombatSystem : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Combat Setup")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask combatLayerMask = 6;

    [Header("Combat Styles")]
    [SerializeField] private CombatStyleSO fistCombatStyle;
    [SerializeField] private CombatStyleSO kickCombatStyle;
    [SerializeField] private float styleSwitchCooldown = 0.5f;


    [Header("Input Buffer")]
    [SerializeField] private float inputBufferTime = 0.2f;
    [SerializeField] private float comboResetTime = 2f;

    [Header("Current Input Sequence")]
    [SerializeField] private string currentInputSequence = "";

    [Header("Dodge Settings")]
    [SerializeField] private float dodgeDistance = 5f;
    [SerializeField] private float dodgeDuration = 0.5f;
    [SerializeField] private float dodgeStaminaCost = 20f;
    [SerializeField] private float dodgeCooldown = 0.5f;
    [SerializeField] private AnimationCurve dodgeSpeedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private float iFrameStartTime = 0.1f;
    [SerializeField] private float iFrameEndTime = 0.4f;

    [Header("Block Settings")]
    [SerializeField] private float blockStaminaDrainRate = 5f;
    [SerializeField] private float blockDamageReduction = 0.7f;
    [SerializeField] private float blockStaminaCostOnHit = 15f;
    [SerializeField] private float perfectBlockWindow = 0.2f;
    [SerializeField] private float perfectBlockDamageReduction = 1f;
    
    #endregion

    #region Private Fields
    
    // Timing
    private float lastInputTime;
    private float attackTimer = 0f;
    private float lastStyleSwitchTime = -999f;

    // State
    private bool isAttacking = false;
    private bool inComboWindow = false;
    private bool comboContinued = false;
    private bool isDodging = false;
    private bool isBlocking = false;

    // Combat
    private AttackSO currentAttack;
    private Queue<string> inputBuffer = new Queue<string>();
    private CombatStyleSO combatStyle;
    private int currentAnimationLayerIndex = 1;

    // Animation
    private float weightAmount = 0f;
    private float weightIncrease = 0.05f;
    
    // Hit Detection
    private bool hitDetectionActive = false;
    private HashSet<Collider> hitTargetsThisAttack = new HashSet<Collider>();
    private List<Collider> activeHitColliders = new List<Collider>();

    // Dodge State
    private float dodgeTimer = 0f;
    private Vector3 dodgeDirection = Vector3.zero;
    private float lastDodgeTime = -999f;
    private bool hasIFrames = false;

    // Block State
    private float blockStartTime = 0f;
    private bool isPerfectBlock = false;

    // Components
    private AttackColliderManager attackColManager;
    private CharacterController characterController;
    private Transform cameraTransform;
    private Player player;
    private PlayerHealthSystem healthSystem;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        attackColManager = GetComponent<AttackColliderManager>();
        characterController = GetComponent<CharacterController>();
        player = GetComponent<Player>();
        healthSystem = GetComponent<PlayerHealthSystem>();
        cameraTransform = Camera.main.transform;
        combatStyle = kickCombatStyle;
        
        // Set animator to use normal time so combat animations ARE affected by time slow
        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.Normal;
    }

    private void Update()
    {
        // Update animator speed based on speed multiplier
        if (animator != null && healthSystem != null)
        {
            animator.speed = healthSystem.SpeedMultiplier;
        }

        if (isDodging)
        {
            if (player.IsDashUnlocked)
                UpdateDodgeState();
        }
        else if (isBlocking)
        {
            if (player.IsArmorUnlocked)
                UpdateBlockState();
        }
        else if (isAttacking)
        {
            UpdateAttackState();
            
            if (hitDetectionActive)
            {
                CheckForHits();
            }
        }

        HandleCombatStyleSwitching();
        HandleCombatInput();
        ProcessInputBuffer();
        CheckComboReset();
    }

    #endregion

    #region Combat Style Switching

    /// <summary>
    /// Handles D-Pad input for switching between combat styles.
    /// </summary>
    private void HandleCombatStyleSwitching()
    {
        InputControl control = playerInput.actions["StyleSwitch"].activeControl;
        // Don't allow style switching during attacks
        if (isAttacking)
        {
            return;
        }

        // Check cooldown
        if (Time.time - lastStyleSwitchTime < styleSwitchCooldown)
        {
            return;
        }

        Vector2 dpadInput = playerInput.actions["StyleSwitch"].ReadValue<Vector2>();

        // Check for up input (Fists)
        if (dpadInput.y > 0.5f)
        {
            if (!player.IsFistStyleUnlocked)
                return;
            currentAnimationLayerIndex = 2;
            SwitchCombatStyle(fistCombatStyle);
        }
        // Check for down input (Kicks)
        else if (dpadInput.y < -0.5f)
        {
            currentAnimationLayerIndex = 1;
            SwitchCombatStyle(kickCombatStyle);
        }
        else if (playerInput.actions["SwitchStyle"].triggered)
        {
            string controlName = playerInput.actions["SwitchStyle"].activeControl.displayName;

            if (controlName.Contains("Up"))
            {
                if (!player.IsFistStyleUnlocked)
                    return;
                currentAnimationLayerIndex = 2;
                SwitchCombatStyle(fistCombatStyle);
            }
            else
            {
                currentAnimationLayerIndex = 1;
                SwitchCombatStyle(kickCombatStyle);
            }
        }
    }

    /// <summary>
    /// Switches to a new combat style.
    /// </summary>
    /// <param name="newStyle">The combat style to switch to</param>
    private void SwitchCombatStyle(CombatStyleSO newStyle)
    {
        if (newStyle == null)
        {
            Debug.LogWarning("Attempted to switch to null combat style!");
            return;
        }

        // Don't switch if already using this style
        if (combatStyle == newStyle)
        {
            return;
        }

        combatStyle = newStyle;
        lastStyleSwitchTime = Time.time;

        // Clear any pending inputs when switching styles
        ClearInputBuffer();
        ResetCombo();
        OnAttackEnd();

        //Stop all animations, return to normal state and then switch style, make sure the layers are done blending

        Debug.Log($"Switched to combat style: {combatStyle.styleName}");
    }

    /// <summary>
    /// Gets the current combat style.
    /// </summary>
    public CombatStyleSO GetCurrentCombatStyle()
    {
        return combatStyle;
    }

    /// <summary>
    /// Checks if the player can currently switch combat styles.
    /// </summary>
    public bool CanSwitchStyle()
    {
        if (isAttacking)
        {
            return false;
        }

        if (Time.time - lastStyleSwitchTime < styleSwitchCooldown)
        {
            return false;
        }

        return true;
    }

    #endregion

    #region Attack State Management

    private void UpdateAttackState()
    {
        // Apply speed multiplier to attack speed when player has speed modifier
        float speedMultiplier = healthSystem != null ? healthSystem.SpeedMultiplier : 1f;
        attackTimer += Time.unscaledDeltaTime * speedMultiplier;

        if (currentAttack != null)
        {
            float normalizedTime = attackTimer / animator.GetCurrentAnimatorClipInfo(1)[0].clip.length;
            
            if (normalizedTime >= currentAttack.comboWindowStart && 
                normalizedTime <= currentAttack.comboWindowEnd)
            {
                if (!inComboWindow)
                {
                    inComboWindow = true;
                }
            }
            
            if (normalizedTime >= 1f)
            {
                OnAttackEnd();
            }
        }
    }

    private void ExecuteAttack(AttackSO attack)
    {
        currentAttack = attack;
        isAttacking = true;
        inComboWindow = false;
        attackTimer = 0f;
        
        hitDetectionActive = false;
        hitTargetsThisAttack.Clear();

        animator.SetTrigger(attack.animationTrigger);
        StartCoroutine(BlendAnimations(true));

        if (attack.isFinisher)
        {
            ClearInputBuffer();
        }
    }

    public void OnAttackEnd()
    {
        // Check if combo has continued before doing cleanup
        if (comboContinued)
        {
            comboContinued = false;
            return;
        }

        bool wasFinisher = currentAttack != null && currentAttack.isFinisher;
        
        isAttacking = false;
        inComboWindow = false;
        hitDetectionActive = false;
        hitTargetsThisAttack.Clear();
        activeHitColliders.Clear();
        currentAttack = null;
        attackTimer = 0f;
        
        StartCoroutine(BlendAnimations(false));

        if (attackColManager != null)
        {
            attackColManager.DeactivateAllColliders();
        }

        if (wasFinisher)
        {
            ClearInputBuffer();
        }
    }
    
    #endregion

    #region Dodge System

    /// <summary>
    /// Updates dodge state including movement and i-frame handling.
    /// </summary>
    private void UpdateDodgeState()
    {
        dodgeTimer += Time.unscaledDeltaTime;
        float normalizedTime = dodgeTimer / dodgeDuration;

        // Update i-frames
        if (normalizedTime >= iFrameStartTime && normalizedTime <= iFrameEndTime)
        {
            hasIFrames = true;
        }
        else
        {
            hasIFrames = false;
        }

        // Move character during dodge
        if (characterController != null && dodgeDirection != Vector3.zero)
        {
            float speedMultiplier = dodgeSpeedCurve.Evaluate(normalizedTime);
            Vector3 movement = dodgeDirection * dodgeDistance * speedMultiplier * Time.unscaledDeltaTime / dodgeDuration;
            characterController.Move(movement);
        }

        // End dodge
        if (normalizedTime >= 1f)
        {
            EndDodge();
        }
    }

    /// <summary>
    /// Attempts to execute a dodge roll.
    /// </summary>
    private void TryDodge()
    {
        // Check cooldown
        if (Time.time - lastDodgeTime < dodgeCooldown)
        {
            return;
        }

        // TODO: Check stamina here when stamina system is implemented
        // if (currentStamina < dodgeStaminaCost) return;

        // Get movement input for dodge direction
        Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
        
        // Calculate dodge direction based on camera
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        dodgeDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

        // Default to backward dodge if no input
        if (dodgeDirection.sqrMagnitude < 0.1f)
        {
            dodgeDirection = -transform.forward;
        }

        // Start dodge
        isDodging = true;
        dodgeTimer = 0f;
        lastDodgeTime = Time.time;

        // Cancel attack if possible
        if (isAttacking)
        {
            OnAttackEnd();
        }

        // Trigger animation
        animator.SetTrigger("Dodge");

        // TODO: Consume stamina when stamina system is implemented
        // currentStamina -= dodgeStaminaCost;

        Debug.Log($"Dodge started in direction: {dodgeDirection}");
    }

    /// <summary>
    /// Ends the dodge state.
    /// </summary>
    private void EndDodge()
    {
        isDodging = false;
        hasIFrames = false;
        dodgeTimer = 0f;
        dodgeDirection = Vector3.zero;
    }

    #endregion

    #region Block System

    /// <summary>
    /// Updates block state including stamina drain.
    /// </summary>
    private void UpdateBlockState()
    {
        // Check if still holding block button
        if (!playerInput.actions["Block"].IsPressed())
        {
            EndBlock();
            return;
        }

        // Update perfect block window
        float timeSinceBlockStart = Time.time - blockStartTime;
        isPerfectBlock = timeSinceBlockStart <= perfectBlockWindow;

        // TODO: Drain stamina while blocking when stamina system is implemented
        // currentStamina -= blockStaminaDrainRate * Time.deltaTime;
        // if (currentStamina <= 0) EndBlock();

        // Update animator
        animator.SetBool("Blocking", true);
    }

    /// <summary>
    /// Starts blocking.
    /// </summary>
    private void StartBlock()
    {
        // Can't block while dodging
        if (isDodging)
        {
            return;
        }

        // Can block during attack if attack allows it
        if (isAttacking && currentAttack != null && !currentAttack.canCancelToMovement)
        {
            return;
        }

        isBlocking = true;
        blockStartTime = Time.time;
        isPerfectBlock = false;

        // Cancel attack if in progress
        if (isAttacking)
        {
            OnAttackEnd();
        }

        animator.SetBool("Blocking", true);
        Debug.Log("Block started");
    }

    /// <summary>
    /// Ends blocking.
    /// </summary>
    private void EndBlock()
    {
        isBlocking = false;
        isPerfectBlock = false;
        animator.SetBool("Blocking", false);
    }

    /// <summary>
    /// Processes an incoming attack while blocking.
    /// Should be called by an event system or damage handler.
    /// </summary>
    /// <param name="incomingDamage">The damage being blocked</param>
    /// <returns>The final damage after block reduction</returns>
    public float ProcessBlockedAttack(float incomingDamage)
    {
        if (!isBlocking)
        {
            return incomingDamage;
        }

        float damageReduction = isPerfectBlock ? perfectBlockDamageReduction : blockDamageReduction;
        float finalDamage = incomingDamage * (1f - damageReduction);

        // TODO: Consume stamina on block when stamina system is implemented
        // currentStamina -= blockStaminaCostOnHit;

        // Trigger block reaction animation
        if (isPerfectBlock)
        {
            animator.SetTrigger("PerfectBlock");
            Debug.Log("Perfect block!");
        }
        else
        {
            animator.SetTrigger("BlockHit");
        }

        Debug.Log($"Blocked attack! Damage: {incomingDamage} -> {finalDamage} (Reduction: {damageReduction * 100}%)");

        return finalDamage;
    }

    #endregion

    #region Input Handling
    
    private void HandleCombatInput()
    {
        // Attack inputs
        if (playerInput.actions["Attack"].triggered)
        {
            AddInputToBuffer("L");
        }
        else if (playerInput.actions["Attack Heavy"].triggered)
        {
            if (!player.IsHeavyAttackUnlocked)
                return;
            AddInputToBuffer("H");
        }

        // Dodge input
        if (playerInput.actions["Dodge"].triggered)
        {
            if (player.IsDashUnlocked)
                TryDodge();
        }

        // Block input
        if (playerInput.actions["Block"].triggered)
        {
            StartBlock();
        }
    }
    
    private void AddInputToBuffer(string input)
    {
        // Can't buffer attacks while dodging or blocking
        if (isDodging || isBlocking)
        {
            return;
        }

        inputBuffer.Enqueue(input);
        lastInputTime = Time.time;
        
        // Limit buffer size
        if (inputBuffer.Count > 1)
        {
            inputBuffer.Dequeue();
        }
    }
    
    private void ProcessInputBuffer()
    {
        // Don't process inputs while dodging or blocking
        if (isDodging || isBlocking)
        {
            return;
        }

        if (inputBuffer.Count > 0)
        {
            string input = inputBuffer.Peek();
            
            if (!isAttacking)
            {
                inputBuffer.Dequeue();
                currentInputSequence = input;
                
                AttackSO startingAttack = GetStartingAttack(input);
                if (startingAttack != null)
                {
                    ExecuteAttack(startingAttack);
                }
            }
            else if (inComboWindow && !currentAttack.isFinisher)
            {
                inputBuffer.Dequeue();
                AttackSO nextAttack = GetNextAttackFromCurrentAttack(input);
                
                if (nextAttack != null)
                {
                    ApplyBetweenMovement();

                    currentInputSequence += input;
                    comboContinued = true;
                    ExecuteAttack(nextAttack);
                }
            }
        }
    }


    //Check for player change in move direction first then apply it
    //This should not interrupt the animator layer change and the
    //movement should be applied right before the attack gets played
    //There should be a max angle the player can chage directions
    //to not make it look too snappy
    private void ApplyBetweenMovement()
    {

    }
    
    #endregion

    #region Attack Selection
    
    private AttackSO GetStartingAttack(string input)
    {
        if (input == "L" && combatStyle.startingLightAttack != null)
        {
            return combatStyle.startingLightAttack;
        }
        else if (input == "H" && combatStyle.startingHeavyAttack != null)
        {
            return combatStyle.startingHeavyAttack;
        }
        
        return null;
    }
    
    private AttackSO GetNextAttackFromCurrentAttack(string input)
    {
        if (currentAttack == null)
        {
            return null;
        }
        
        if (input == "L" && currentAttack.lightAttackFollowUps.Length > 0)
        {
            return currentAttack.lightAttackFollowUps[0];
        }
        else if (input == "H" && currentAttack.heavyAttackFollowUps.Length > 0)
        {
            return currentAttack.heavyAttackFollowUps[0];
        }
        
        return null;
    }
    
    #endregion

    #region Combo Management
    
    private void ClearInputBuffer()
    {
        inputBuffer.Clear();
        currentInputSequence = "";
    }
    
    private void CheckComboReset()
    {
        if (Time.time - lastInputTime > comboResetTime)
        {
            ResetCombo();
        }
    }
    
    private void ResetCombo()
    {
        currentInputSequence = "";
        inputBuffer.Clear();
        
        if (!isAttacking)
        {
            currentAttack = null;
        }
    }
    
    #endregion

    #region Hit Detection
    
    /// <summary>
    /// Checks for hits during active hit detection frames of an attack animation.
    /// Uses the active colliders from the AttackColliderManager.
    /// </summary>
    private void CheckForHits()
    {
        if (!isAttacking || currentAttack == null)
        {
            return;
        }

        foreach (Collider activeCollider in activeHitColliders)
        {
            if (activeCollider == null || !activeCollider.enabled)
            {
                continue;
            }

            Collider[] overlapping = Physics.OverlapSphere(
                activeCollider.bounds.center,
                activeCollider.bounds.extents.magnitude,
                combatLayerMask
            );

            foreach (Collider hit in overlapping)
            {
                if (hit.CompareTag("Enemy") && !hitTargetsThisAttack.Contains(hit))
                {
                    //Add to hit targets immediately to prevent multiple hits
                    hitTargetsThisAttack.Add(hit);
                    ProcessHit(hit);
                }
            }
        }
    }

    /// <summary>
    /// Processes a successful hit against an enemy.
    /// </summary>
    private void ProcessHit(Collider enemyCollider)
    {
        float finalDamage = currentAttack.damage;
        finalDamage *= player.DamageMultiplier;

        float staminaCost = currentAttack.staminaCost;
        bool canBeBlocked = currentAttack.canBeBlocked;
        List<AttackEffect> effects = currentAttack.effects;

        EventBus<PlayerAttackHitEvent>.Publish(new PlayerAttackHitEvent(
            currentAttack,
            finalDamage,
            gameObject,
            enemyCollider.transform.gameObject,
            staminaCost,
            canBeBlocked,
            effects
        ));

        // Trigger camera shake on enemy hit (0.5 seconds, amplitude 2)
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.5f, 0.4f);
        }

        //Debug.Log($"Hit enemy: {enemyCollider.name} with attack: {currentAttack.name}");
    }
    
    #endregion

    #region Animation Events

    /// <summary>
    /// Called by animation event when hit detection should start.
    /// Automatically activates colliders based on the current attack's activeBodyParts.
    /// </summary>
    public void StartHitDetection()
    {
        if (isAttacking && currentAttack != null && attackColManager != null)
        {
            hitDetectionActive = true;
            activeHitColliders = attackColManager.ActivateColliders(currentAttack.activeBodyParts);
        }
    }

    /// <summary>
    /// Called by animation event when hit detection should end.
    /// Add this to your animation when the active frames end.
    /// </summary>
    public void EndHitDetection()
    {
        hitDetectionActive = false;
        activeHitColliders.Clear();

        if (attackColManager != null)
        {
            attackColManager.DeactivateAllColliders();
        }
    }

    /// <summary>
    /// Called by animation event when combo window opens.
    /// Add this to your animation when player can input next attack.
    /// </summary>
    public void StartComboWindow()
    {
        inComboWindow = true;
    }
    
    /// <summary>
    /// Called by animation event when combo window closes.
    /// Add this when the combo timing window ends.
    /// </summary>
    public void EndComboWindow()
    {
        inComboWindow = false;
    }
    
    /// <summary>
    /// Called by animation event when attack animation ends.
    /// Add this at the very end of your attack animation.
    /// </summary>
    public void AnimationAttackEnd()
    {
        OnAttackEnd();
    }

    /// <summary>
    /// Called by animation event when dodge animation ends.
    /// Add this at the end of your dodge animation.
    /// </summary>
    public void AnimationDodgeEnd()
    {
        EndDodge();
    }

    #endregion

    #region Animation

    public IEnumerator BlendAnimations(bool intoCombat)
    {
        if (intoCombat)
        {
            while (weightAmount < 1f)
            {
                weightAmount += weightIncrease;
                animator.SetLayerWeight(currentAnimationLayerIndex, weightAmount);
                yield return null;
            }
            animator.SetLayerWeight(currentAnimationLayerIndex, 1f);
        }
        else
        {
            while (weightAmount > 0f)
            {
                weightAmount -= weightIncrease;
                animator.SetLayerWeight(currentAnimationLayerIndex, weightAmount);
                yield return null;
            }
            animator.SetLayerWeight(currentAnimationLayerIndex, 0f);
        }
    }
    
    #endregion

    #region Public API

    public bool IsAttacking() => isAttacking;
    public bool IsDodging() => isDodging;
    public bool IsBlocking() => isBlocking;
    public bool HasIFrames() => hasIFrames;
    
    public bool CanMove()
    {
        // Can't move while dodging (dodge handles its own movement)
        if (isDodging)
        {
            return false;
        }

        // Can't move while blocking
        if (isBlocking)
        {
            return false;
        }

        if (!isAttacking)
        {
            return true;
        }

        if (currentAttack != null && currentAttack.canCancelToMovement)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the player can currently dodge.
    /// </summary>
    public bool CanDodge()
    {
        if (isDodging)
        {
            return false;
        }

        if (Time.time - lastDodgeTime < dodgeCooldown)
        {
            return false;
        }

        // TODO: Add stamina check when implemented
        // if (currentStamina < dodgeStaminaCost) return false;

        return true;
    }

    /// <summary>
    /// Checks if the player can currently block.
    /// </summary>
    public bool CanBlock()
    {
        if (isDodging)
        {
            return false;
        }

        if (isAttacking && currentAttack != null && !currentAttack.canCancelToMovement)
        {
            return false;
        }

        // TODO: Add stamina check when implemented
        // if (currentStamina <= 0) return false;

        return true;
    }
    
    #endregion
}
