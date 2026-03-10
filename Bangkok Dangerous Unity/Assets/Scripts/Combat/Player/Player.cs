using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The Player class handles player movement, interactions, and combat state management.
/// </summary>
public class Player : MonoBehaviour
{
    #region Serialized Fields

    [Header("Attributes")]
    [SerializeField] private PlayerData data;
    private int maxHealth = 100;
    private int currentHealth;
    private float maxHeat = 100f;
    private float heatLoseRate = 10f;
    private float armor = 0f;
    private float superArmor = 0f;
    private float damageMultiplier = 1f;

    [Header("Movement")]
    [SerializeField] private float maxMoveSpeed = 6f;
    [SerializeField] private float turnSpeed = 8f;
    [SerializeField] private float parameterSmoothTime = 0.1f;
    
    [Header("Combat Movement")]
    [SerializeField] private float combatMoveSpeed = 6f;
    [SerializeField] private float combatParameterSmoothTime = 0.15f;
    
    #endregion

    #region Private Fields
    
    // Components
    private PlayerInput playerInput;
    private Animator animator;
    private CharacterController characterController;
    private Transform cameraTransform;
    private InputBufferCombatSystem inputBufferCombat;
    private CharacterCameraLock characterLock;
    
    // Normal Movement Parameters
    private float currentSpeedParam = 0f;
    private float currentTurnParam = 0f;
    private float speedVelocity = 0f;
    private float turnVelocity = 0f;
    
    // Combat Movement Parameters
    private float currentCombatX = 0f;
    private float currentCombatY = 0f;
    private float combatXVelocity = 0f;
    private float combatYVelocity = 0f;
    
    // Combat State
    private bool useInputBufferCombat = true;
    private bool wasInCombatLastFrame = false;
    private bool manualCombatState = false;

    //Health system

    [SerializeField] private PlayerHealthSystem playerHealthSystem;

    //Skill unlocks
    private bool fistStyleUnlock = false;
    private bool heavyAttUnlocked = false;
    private bool dashUnlocked = false;
    private bool armorUnlocked = false;

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        EventBus<PlayerTakeDamageEvent>.Subscribe(e => PlayerTakeDamage((int)e.damageAmount));
        EventBus<RuneUnlockEvent>.Subscribe(RuneUnlock);
    }

    private void InitizalizeData(PlayerData pData)
    {
        maxHealth = pData.maxHealth;
        currentHealth = maxHealth;
        maxHeat = pData.maxHeat;
        heatLoseRate = pData.heatLoseRate;
        armor = pData.armor;
        superArmor = pData.superArmor;
        damageMultiplier = pData.damageMultiplier;
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.Normal;
        playerInput = GetComponent<PlayerInput>();
        cameraTransform = Camera.main.transform;

        inputBufferCombat = GetComponent<InputBufferCombatSystem>();
        characterLock = GetComponent<CharacterCameraLock>();
        playerHealthSystem = GetComponent<PlayerHealthSystem>();
    }

    private void Start()
    {
        playerInput.enabled = true;
        InitizalizeData(data);
    }

    private void FixedUpdate()
    {
        HandleMove();
        HandleCombatStateTransitions();
    }

    private void OnDisable()
    {
        EventBus<PlayerTakeDamageEvent>.Unsubscribe(e => PlayerTakeDamage((int)e.damageAmount));
        EventBus<RuneUnlockEvent>.Unsubscribe(RuneUnlock);
        playerInput.enabled = false;
    }
    
    #endregion

    #region Combat State Management

    /// <summary>
    /// Sets the combat state manually, overriding automatic character lock detection.
    /// </summary>
    /// <param name="inCombat">Whether the player should be in combat state</param>
    public void SetInCombatState(bool inCombat)
    {
        manualCombatState = inCombat;
    }

    /// <summary>
    /// Handles transitions between normal and combat movement blend trees.
    /// </summary>
    private void HandleCombatStateTransitions()
    {
        bool isInCombat = manualCombatState || (characterLock != null && characterLock.IsCharacterLocked);
        
        if (isInCombat && !wasInCombatLastFrame)
        {
            animator.SetTrigger("Combat");
        }
        else if (!isInCombat && wasInCombatLastFrame)
        {
            animator.SetTrigger("Normal");
        }
        
        wasInCombatLastFrame = isInCombat;
    }
    
    #endregion

    #region Movement

    /// <summary>
    /// Processes player movement input and switches between normal and combat animation sets.
    /// </summary>
    private void HandleMove()
    {
        bool canMove = true;
        
        if (useInputBufferCombat && inputBufferCombat != null)
        {
            canMove = inputBufferCombat.CanMove();
        }

        if (!canMove)
        {
            return;
        }

        bool isInCombat = characterLock != null && characterLock.IsCharacterLocked;
        
        if (isInCombat)
        {
            HandleCombatMovement();
        }
        else
        {
            HandleNormalMovement();
        }
    }

    /// <summary>
    /// Handles normal camera-relative movement.
    /// </summary>
    private void HandleNormalMovement()
    {
        Vector2 movement = playerInput.actions["Move"].ReadValue<Vector2>();

        // Get camera-relative directions
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Calculate movement vector
        Vector3 moveVec = (camForward * movement.y + camRight * movement.x);
        
        if (characterLock != null)
        {
            moveVec = characterLock.GetLockAdjustedMovement(moveVec);
        }
        
        // Calculate animation parameters
        float targetSpeed = Mathf.Clamp01(moveVec.magnitude);
        float targetTurn = 0f;

        if (moveVec.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(moveVec.x, moveVec.z) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.y;
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            
            targetTurn = Mathf.Clamp(angleDifference / 180f, -1f, 1f);

            // Rotate character
            if (characterLock == null || !characterLock.ShouldOverrideRotation())
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveVec, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        // Smooth animation parameters
        currentSpeedParam = Mathf.SmoothDamp(currentSpeedParam, targetSpeed, ref speedVelocity, parameterSmoothTime);
        currentTurnParam = Mathf.SmoothDamp(currentTurnParam, targetTurn, ref turnVelocity, parameterSmoothTime);

        animator.SetFloat("Speed", currentSpeedParam);
        animator.SetFloat("Turn", currentTurnParam);

        // Move character with speed multiplier from healing items
        float speedMod = playerHealthSystem != null ? playerHealthSystem.SpeedMultiplier : 1f;
        characterController.Move(moveVec * maxMoveSpeed * speedMod * Time.deltaTime);
    }

    /// <summary>
    /// Handles combat camera-relative movement using Combat X and Combat Y parameters based on character's facing direction.
    /// </summary>
    private void HandleCombatMovement()
    {
        Vector2 movement = playerInput.actions["Move"].ReadValue<Vector2>();

        // Get camera-relative directions
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Calculate movement vector
        Vector3 moveVec = (camForward * movement.y + camRight * movement.x);
        moveVec.y = 0f;

        if (characterLock != null)
        {
            moveVec = characterLock.GetLockAdjustedMovement(moveVec);
        }

        // Get character-relative directions
        Vector3 characterForward = transform.forward;
        Vector3 characterRight = transform.right;
        characterForward.y = 0f;
        characterRight.y = 0f;
        characterForward.Normalize();
        characterRight.Normalize();

        // Calculate combat movement parameters
        float targetCombatY = Vector3.Dot(moveVec.normalized, characterForward);
        float targetCombatX = Vector3.Dot(moveVec.normalized, characterRight);

        float movementMagnitude = moveVec.magnitude;
        targetCombatX *= movementMagnitude;
        targetCombatY *= movementMagnitude;

        targetCombatX = Mathf.Clamp(targetCombatX, -1f, 1f);
        targetCombatY = Mathf.Clamp(targetCombatY, -1f, 1f);

        // Smooth combat parameters
        currentCombatX = Mathf.SmoothDamp(currentCombatX, targetCombatX, ref combatXVelocity, combatParameterSmoothTime);
        currentCombatY = Mathf.SmoothDamp(currentCombatY, targetCombatY, ref combatYVelocity, combatParameterSmoothTime);

        animator.SetFloat("Combat X", currentCombatX);
        animator.SetFloat("Combat Y", currentCombatY);

        // Rotate character
        if (moveVec.sqrMagnitude > 0.001f)
        {
            if (characterLock == null || !characterLock.ShouldOverrideRotation())
            {
                float targetAngle = Mathf.Atan2(moveVec.x, moveVec.z) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }
        
        moveVec.y = 0f;
        // Move character with speed multiplier from healing items
        float speedMod = playerHealthSystem != null ? playerHealthSystem.SpeedMultiplier : 1f;
        characterController.Move(moveVec * combatMoveSpeed * speedMod * Time.deltaTime);
    }

    #endregion

    #region HealthSystem
    private void PlayerTakeDamage(int damage)
    {
        playerHealthSystem.TakeDamage(damage);
    }
    #endregion

    #region Skills

    private void RuneUnlock(RuneUnlockEvent evt)
    {
        Rune rune = evt.rune;

        if (rune != null)
        {
            foreach (var unlock in rune.unlocks)
            {
                if (unlock.type == RuneType.Stat)
                {
                    UpgradeStat(unlock.statType, unlock.statValue);
                }
                else if (unlock.type == RuneType.Skill)
                {
                    UnlockSkill(unlock.unlockedRune);
                }
            }
        }
    }

    private void UpgradeStat(StatType stat, float value)
    {
        Debug.Log($"Upgraded {stat.ToString()} by {value}");

        switch (stat)
        {
            case StatType.Health:
                maxHealth += (int)value;
                currentHealth += (int)value;
                break;
            case StatType.Speed:
                maxMoveSpeed += (int)value;
                break;
            case StatType.Damage:
                damageMultiplier += value;
                break;
            case StatType.Defence:
                armor += value;
                superArmor += value;
                break;
        }
    }

    private void UnlockSkill(Rune skill)
    {
        Debug.Log($"Unlocked {skill.skillName}");

        switch (skill.skillName)
        {
            case "Fist Style":
                fistStyleUnlock = true;
                break;
            case "Heavy Attack":
                heavyAttUnlocked = true;
                break;
            case "Dash":
                dashUnlocked = true;
                break;
            case "Armor":
                armorUnlocked = true;
                break;
        }
    }

    #endregion

    #region Public API

    public bool IsInCombatMovement => manualCombatState || (characterLock != null && characterLock.IsCharacterLocked);
    public Vector2 GetCombatParameters() => new Vector2(currentCombatX, currentCombatY);
    public bool IsManualCombatState => manualCombatState;
    public bool IsFistStyleUnlocked => fistStyleUnlock;
    public bool IsHeavyAttackUnlocked => heavyAttUnlocked;
    public bool IsDashUnlocked => dashUnlocked;
    public bool IsArmorUnlocked => armorUnlocked;
    public float DamageMultiplier
    {
        get
        {
            float baseDamage = damageMultiplier;
            if (playerHealthSystem != null)
            {
                baseDamage *= playerHealthSystem.DamageMultiplier;
            }
            return baseDamage;
        }
    }
    
    #endregion
}