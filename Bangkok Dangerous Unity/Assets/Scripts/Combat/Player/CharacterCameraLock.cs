using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages camera lock-on system for combat targeting.
/// Handles target acquisition, switching, and character rotation during lock-on.
/// </summary>
public class CharacterCameraLock : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Lock-On Settings")]
    [SerializeField] private LayerMask combatLayerMask = 1 << 6;
    [SerializeField] private float lockOnRange = 15f;
    [SerializeField] private float lockOnAngle = 60f;
    [SerializeField] private float autoLockOnRange = 5f;
    
    [Header("Character References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraTransform;
    
    [Header("Character Lock Settings")]
    [SerializeField] private float characterRotationSpeed = 8f;
    [SerializeField] private float lockBreakDistance = 20f;
    [SerializeField] private bool allowMovementDuringLock = true;
    [SerializeField] private float movementSpeedMultiplier = 0.8f;
    
    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    
    [Header("Target Switching")]
    [SerializeField] private float targetSwitchCooldown = 0.3f;
    
    #endregion

    #region Private Fields
    
    //Lock State
    private bool isCharacterLocked = false;
    private Transform currentTarget = null;
    
    //Target Management
    private List<Transform> availableTargets = new List<Transform>();
    private float lastTargetSwitchTime = 0f;
    
    //Components
    private Player playerController;
    
    #endregion

    #region Unity Lifecycle
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    private void Update()
    {
        HandleCharacterLockInput();
        
        if (isCharacterLocked)
        {
            HandleTargetSwitching();
            ValidateCurrentTarget();
            UpdateCharacterRotation();
        }
    }
    
    #endregion

    #region Initialization

    /// <summary>
    /// Initializes component references.
    /// </summary>
    private void InitializeComponents()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (playerTransform == null)
        {
            playerTransform = transform;
        }

        playerController = GetComponent<Player>();
    }
    
    #endregion

    #region Lock-On Management

    /// <summary>
    /// Handles input for toggling character lock-on.
    /// </summary>
    private void HandleCharacterLockInput()
    {
        UpdateAvailableTargets();

        if (playerInput.actions["Lock On"].triggered)
        {
            if (!isCharacterLocked)
            {
                TryLockOnToTarget();
            }
            else
            {
                DisableCharacterLock();
            }
        }
        else if (GetClosestTargetToPlayer() != null)
        {
            Transform target = GetClosestTargetToPlayer();

            float distanceToTarget = (transform.position - target.position).magnitude;

            if (distanceToTarget <= autoLockOnRange)
            {
                TryLockOnToTarget();
            }
            else
            {
                DisableCharacterLock();
            }
        }
    }
    
    /// <summary>
    /// Attempts to lock onto the nearest valid target.
    /// </summary>
    private void TryLockOnToTarget()
    {
        UpdateAvailableTargets();

        if (availableTargets.Count > 0)
        {
            Transform closestTarget = GetClosestTargetToCameraAngle();
            if (closestTarget != null)
            {
                EnableCharacterLock(closestTarget);
                
                if (playerController != null)
                {
                    playerController.SetInCombatState(true);
                }
            }
        }
    }
    
    /// <summary>
    /// Enables character lock on the specified target.
    /// </summary>
    private void EnableCharacterLock(Transform target)
    {
        currentTarget = target;
        isCharacterLocked = true;
    }
    
    /// <summary>
    /// Disables character lock and exits combat state.
    /// </summary>
    private void DisableCharacterLock()
    {
        currentTarget = null;
        isCharacterLocked = false;
        
        if (playerController != null)
        {
            playerController.SetInCombatState(false);
        }
    }

    /// <summary>
    /// Validates that the current target is still valid and within range.
    /// </summary>
    private void ValidateCurrentTarget()
    {
        if (currentTarget == null)
        {
            DisableCharacterLock();
            return;
        }

        float distance = Vector3.Distance(playerTransform.position, currentTarget.position);
        if (distance > lockBreakDistance)
        {
            DisableCharacterLock();
        }
    }

    #endregion

    #region Target Detection

    /// <summary>
    /// Updates the list of available targets within lock-on range and angle.
    /// </summary>
    private void UpdateAvailableTargets()
    {
        availableTargets.Clear();
        
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        foreach (GameObject enemy in enemies)
        {
            if (((1 << enemy.layer) & combatLayerMask) != 0)
            {
                float distance = Vector3.Distance(playerTransform.position, enemy.transform.position);
                
                if (distance <= lockOnRange)
                {
                    Vector3 directionToEnemy = (enemy.transform.position - cameraTransform.position).normalized;
                    Vector3 cameraForward = cameraTransform.forward;
                    cameraForward.y = 0f;
                    directionToEnemy.y = 0f;
                    
                    float angleToEnemy = Vector3.Angle(cameraForward, directionToEnemy);
                    
                    if (angleToEnemy <= lockOnAngle && HasLineOfSight(enemy.transform))
                    {
                        availableTargets.Add(enemy.transform);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Checks if there is a clear line of sight to the target.
    /// </summary>
    private bool HasLineOfSight(Transform target)
    {
        Vector3 directionToTarget = target.position - playerTransform.position;
        
        RaycastHit[] hits = Physics.RaycastAll(
            playerTransform.position, 
            directionToTarget.normalized, 
            directionToTarget.magnitude
        );
        
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.CompareTag("Player"))
            {
                continue;
            }
                
            if (hit.transform == target)
            {
                return true;
            }
                
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Gets the target closest to the camera's forward direction.
    /// </summary>
    private Transform GetClosestTargetToCameraAngle()
    {
        if (availableTargets.Count == 0)
        {
            return null;
        }
        
        Transform closestTarget = null;
        float smallestAngle = float.MaxValue;
        
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();
        
        foreach (Transform target in availableTargets)
        {
            Vector3 directionToTarget = (target.position - cameraTransform.position).normalized;
            directionToTarget.y = 0f;
            
            float angleToTarget = Vector3.Angle(cameraForward, directionToTarget);
            
            if (angleToTarget < smallestAngle)
            {
                smallestAngle = angleToTarget;
                closestTarget = target;
            }
        }
        
        return closestTarget;
    }

    private Transform GetClosestTargetToPlayer()
    {
        if (availableTargets.Count == 0)
        {
            return null;
        }

        Transform closestTarget = null;
        float smallestDistance = float.MaxValue;

        foreach (Transform target in availableTargets)
        {
            Vector3 directionToTarget = target.position - cameraTransform.position;
            directionToTarget.y = 0f;

            float distanceToTarget = directionToTarget.magnitude;

            if (distanceToTarget < smallestDistance)
            {
                smallestDistance = distanceToTarget;
                closestTarget = target;
            }
        }

        return closestTarget;
    }
    
    #endregion

    #region Target Switching

    /// <summary>
    /// Handles input for switching between available targets.
    /// </summary>
    private void HandleTargetSwitching()
    {
        if (Time.time - lastTargetSwitchTime < targetSwitchCooldown)
        {
            return;
        }

        if (playerInput.actions["LockTargetSwitchL"].triggered)
        {
            SwitchTarget(false);
        }
        else if (playerInput.actions["LockTargetSwitchR"].triggered)
        {
            SwitchTarget(true);
        }
    }

    /// <summary>
    /// Switches to the next target in the specified direction.
    /// </summary>
    private void SwitchTarget(bool switchRight)
    {
        UpdateAvailableTargets();

        if (availableTargets.Count > 1)
        {
            Transform nextTarget = GetNextTarget(switchRight);
            if (nextTarget != null && nextTarget != currentTarget)
            {
                currentTarget = nextTarget;
                lastTargetSwitchTime = Time.time;
            }
        }
    }
    
    /// <summary>
    /// Gets the next target based on relative position to the player.
    /// </summary>
    /// <param name="switchRight">True to switch right, false to switch left</param>
    private Transform GetNextTarget(bool switchRight)
    {
        if (availableTargets.Count <= 1)
        {
            return null;
        }
        
        Vector3 playerRight = playerTransform.right;
        playerRight.y = 0f;
        playerRight.Normalize();
        
        Transform bestTarget = null;
        float bestScore = float.MinValue;
        
        foreach (Transform target in availableTargets)
        {
            if (target == currentTarget)
            {
                continue;
            }
            
            Vector3 directionToTarget = (target.position - playerTransform.position).normalized;
            directionToTarget.y = 0f;
            
            float dotProduct = Vector3.Dot(playerRight, directionToTarget);
            float score = switchRight ? dotProduct : -dotProduct;
            
            if (score > bestScore && score > 0)
            {
                bestScore = score;
                bestTarget = target;
            }
        }
        
        return bestTarget ?? availableTargets.FirstOrDefault(t => t != currentTarget);
    }
    
    #endregion

    #region Character Rotation

    /// <summary>
    /// Updates character rotation to face the locked target.
    /// </summary>
    private void UpdateCharacterRotation()
    {
        if (currentTarget == null || playerTransform == null)
        {
            return;
        }
        
        Vector3 directionToTarget = currentTarget.position - playerTransform.position;
        directionToTarget.y = 0f;
        directionToTarget.Normalize();
        
        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            playerTransform.rotation = Quaternion.Slerp(
                playerTransform.rotation, 
                targetRotation, 
                characterRotationSpeed * Time.unscaledDeltaTime
            );
        }
    }
    
    #endregion

    #region Public API

    public bool IsCharacterLocked => isCharacterLocked;
    public Transform CurrentTarget => currentTarget;
    
    /// <summary>
    /// Adjusts movement based on lock-on state.
    /// </summary>
    public Vector3 GetLockAdjustedMovement(Vector3 originalMovement)
    {
        if (!isCharacterLocked || !allowMovementDuringLock)
        {
            return originalMovement;
        }

        return originalMovement * movementSpeedMultiplier;
    }
    
    /// <summary>
    /// Determines if lock-on should override normal rotation behavior.
    /// </summary>
    public bool ShouldOverrideRotation()
    {
        return isCharacterLocked && currentTarget != null;
    }
    
    #endregion
}
