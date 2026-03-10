using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections.Generic;
using System.Linq;

public class EnemyCameraLockOn : MonoBehaviour
{
    [Header("Lock-On Settings")]
    [SerializeField] private LayerMask combatLayerMask = 1 << 6; // Combat layer
    [SerializeField] private float lockOnRange = 15f;
    [SerializeField] private float lockOnAngle = 60f; // Maximum angle from camera forward to consider for lock-on
    
    [Header("Camera References")]
    [SerializeField] private CinemachineCamera mainCamera; // Single camera that changes targets
    [SerializeField] private Transform playerTransform; // Player to follow when not locked on
    [SerializeField] private Transform cameraTransform;
    
    [Header("Camera Distance Settings")]
    [SerializeField] private float normalOrbitRadius = 5f; // Normal camera distance
    [SerializeField] private float maxLockOnRadius = 12f; // Maximum distance when locked on
    [SerializeField] private float minLockOnRadius = 3f; // Minimum distance when locked on
    [SerializeField] private float cameraAdjustmentSpeed = 2f; // How fast camera adjusts distance
    
    [Header("Lock-On Camera Angle Constraints")]
    [SerializeField] private float minPlayerEnemyAngle = 160f; // Minimum angle from camera->player->enemy
    [SerializeField] private float maxPlayerEnemyAngle = 200f; // Maximum angle from camera->player->enemy
    [SerializeField] private float targetPlayerEnemyAngle = 180f; // Ideal angle (180 = directly opposite)
    [SerializeField] private float verticalOffset = 2f; // Height offset for camera
    [SerializeField] private float verticalSensitivity = 2f; // Sensitivity for vertical camera movement during lock-on
    
    [Header("Smooth Transitions")]
    [SerializeField] private float targetSwitchSpeed = 3f; // How fast camera transitions between targets
    [SerializeField] private float positionSmoothTime = 0.5f; // Smooth time for camera positioning
    [SerializeField] private float disableLockOnTransitionTime = 1f; // Time to smoothly return to player when disabling lock-on
    
    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    
    [Header("Target Switching")]
    [SerializeField] private float targetSwitchCooldown = 0.2f;
    
    // Lock-on state
    private bool isLockedOn = false;
    private Transform currentTarget = null;
    private List<Transform> availableTargets = new List<Transform>();
    private float lastTargetSwitchTime = 0f;

    private bool isDisablingLockOn = false;
    private float disableTransitionTimer = 0f;
    private Vector3 disableStartMidpoint;
    private Vector3 disableTargetPosition;

    private Vector2 lastLookInput = Vector2.zero;

    private Transform originalLookAtTarget;
    private CinemachineOrbitalFollow orbitalFollow;
    private CinemachineInputAxisController inputAxisController;
    private Vector2 originalInputGain;
    private float lockOnVerticalAngle = 0f;

    private GameObject midpointTarget;
    private Vector3 targetMidpointPosition;
    private Vector3 currentMidpointPosition;
    private Vector3 midpointVelocity;

    private float targetHorizontalAngle;
    private float currentHorizontalAngle;
    private float horizontalAngleVelocity;

    private void Awake()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        if (playerTransform == null)
            playerTransform = transform;

        if (mainCamera != null)
        {
            originalLookAtTarget = mainCamera.LookAt;
            orbitalFollow = mainCamera.GetComponent<CinemachineOrbitalFollow>();
            inputAxisController = mainCamera.GetComponent<CinemachineInputAxisController>();

            if (orbitalFollow != null)
            {
                normalOrbitRadius = orbitalFollow.Radius;
                currentHorizontalAngle = orbitalFollow.HorizontalAxis.Value;
            }

            if (inputAxisController != null)
            {
                originalInputGain = new Vector2(
                    inputAxisController.Controllers[0].Input.Gain,
                    inputAxisController.Controllers[1].Input.Gain
                );
            }
        }
    }

    private void Update()
    {
        HandleLockOnInput();

        if (isLockedOn)
        {
            HandleTargetSwitching();
            ValidateCurrentTarget();
            PositionCameraWithAngleConstraint();
            UpdateMidpointTarget();
        }
        else if (isDisablingLockOn)
        {
            HandleDisableLockOnTransition();
        }
    }

    private void OnDestroy()
    {
        CleanupMidpointTarget();
    }

    private void HandleLockOnInput()
    {
        if (playerInput.actions["Lock On"].triggered)
        {
            if (!isLockedOn && !isDisablingLockOn)
            {
                TryLockOnToTarget();
            }
            else if (isLockedOn)
            {
                StartDisableLockOn();
            }
        }
    }

    private void TryLockOnToTarget()
    {
        UpdateAvailableTargets();

        if (availableTargets.Count > 0)
        {
            Transform closestTarget = GetClosestTargetToCamera();
            if (closestTarget != null)
            {
                EnableLockOn(closestTarget);
            }
        }
    }

    private void UpdateAvailableTargets()
    {
        availableTargets.Clear();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            if (((1 << enemy.layer) & combatLayerMask) != 0)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);

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

    private bool HasLineOfSight(Transform target)
    {
        Vector3 directionToTarget = target.position - transform.position;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToTarget.normalized,
                           out RaycastHit hit, directionToTarget.magnitude, ~0))
        {
            return hit.transform == target;
        }

        return true;
    }

    private Transform GetClosestTargetToCamera()
    {
        if (availableTargets.Count == 0) return null;

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

    private void EnableLockOn(Transform target)
    {
        currentTarget = target;
        isLockedOn = true;
        isDisablingLockOn = false;
        lockOnVerticalAngle = 0f;

        CreateMidpointTarget();

        if (mainCamera != null && midpointTarget != null)
        {
            mainCamera.LookAt = midpointTarget.transform;
        }

        if (orbitalFollow != null)
        {
            currentHorizontalAngle = orbitalFollow.HorizontalAxis.Value;
        }

        if (inputAxisController != null)
        {
            inputAxisController.Controllers[0].Input.Gain = 0f;
            inputAxisController.Controllers[1].Input.Gain = originalInputGain.y * 0.3f;
        }
    }

    private void StartDisableLockOn()
    {
        if (!isLockedOn) return;

        isDisablingLockOn = true;
        disableTransitionTimer = 0f;

        if (midpointTarget != null)
        {
            disableStartMidpoint = midpointTarget.transform.position;
        }
        else
        {
            disableStartMidpoint = (playerTransform.position + currentTarget.position) * 0.5f;
        }

        disableTargetPosition = originalLookAtTarget != null ? originalLookAtTarget.position : playerTransform.position;

        currentTarget = null;
        isLockedOn = false;
    }

    private void HandleDisableLockOnTransition()
    {
        if (!isDisablingLockOn) return;

        disableTransitionTimer += Time.deltaTime;
        float normalizedTime = disableTransitionTimer / disableLockOnTransitionTime;

        if (normalizedTime >= 1f)
        {
            CompleteLockOnDisable();
            return;
        }

        float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

        if (midpointTarget != null)
        {
            Vector3 currentTargetPos = Vector3.Lerp(disableStartMidpoint, disableTargetPosition, easedTime);
            midpointTarget.transform.position = currentTargetPos;
        }

        if (orbitalFollow != null)
        {
            float targetRadius = Mathf.Lerp(orbitalFollow.Radius, normalOrbitRadius, easedTime);
            orbitalFollow.Radius = targetRadius;
        }

        if (inputAxisController != null)
        {
            float horizontalGain = Mathf.Lerp(0f, originalInputGain.x, easedTime);
            float verticalGain = Mathf.Lerp(originalInputGain.y * 0.3f, originalInputGain.y, easedTime);

            inputAxisController.Controllers[0].Input.Gain = horizontalGain;
            inputAxisController.Controllers[1].Input.Gain = verticalGain;
        }
    }

    private void CompleteLockOnDisable()
    {
        isDisablingLockOn = false;

        CleanupMidpointTarget();

        if (mainCamera != null)
        {
            mainCamera.LookAt = originalLookAtTarget;
        }

        if (orbitalFollow != null)
        {
            orbitalFollow.Radius = normalOrbitRadius;
        }

        if (inputAxisController != null)
        {
            inputAxisController.Controllers[0].Input.Gain = originalInputGain.x;
            inputAxisController.Controllers[1].Input.Gain = originalInputGain.y;
        }
    }

    private void CreateMidpointTarget()
    {
        if (midpointTarget == null)
        {
            midpointTarget = new GameObject("LockOn_Midpoint");
            midpointTarget.hideFlags = HideFlags.HideInHierarchy;
        }

        Vector3 initialMidpoint = (playerTransform.position + currentTarget.position) * 0.5f;
        initialMidpoint.y += 1f;

        targetMidpointPosition = initialMidpoint;
        currentMidpointPosition = initialMidpoint;
        midpointTarget.transform.position = initialMidpoint;
    }

    private void CleanupMidpointTarget()
    {
        if (midpointTarget != null)
        {
            if (Application.isPlaying)
                Destroy(midpointTarget);
            else
                DestroyImmediate(midpointTarget);
            midpointTarget = null;
        }
    }

    private void UpdateMidpointTarget()
    {
        if (midpointTarget == null || currentTarget == null) return;

        Vector3 newMidpoint = (playerTransform.position + currentTarget.position) * 0.5f;
        newMidpoint.y += 1f;

        targetMidpointPosition = newMidpoint;

        currentMidpointPosition = Vector3.SmoothDamp(currentMidpointPosition, targetMidpointPosition,
                                                     ref midpointVelocity, positionSmoothTime);

        midpointTarget.transform.position = currentMidpointPosition;
    }

    private void PositionCameraWithAngleConstraint()
    {
        if (currentTarget == null || orbitalFollow == null || playerTransform == null) return;

        Vector2 lookInput = playerInput.actions["Look"].ReadValue<Vector2>();
        lockOnVerticalAngle += lookInput.y * verticalSensitivity * Time.deltaTime * 10f;
        lockOnVerticalAngle = Mathf.Clamp(lockOnVerticalAngle, -30f, 30f);

        Vector3 playerPos = playerTransform.position;
        Vector3 enemyPos = currentTarget.position;

        float distanceBetweenTargets = Vector3.Distance(playerPos, enemyPos);
        float requiredRadius = Mathf.Clamp((distanceBetweenTargets * 0.6f) + 3f, minLockOnRadius, maxLockOnRadius);

        Vector3 enemyToPlayer = (playerPos - enemyPos).normalized;
        Vector3 directionFromPlayer = -enemyToPlayer;

        float rotationAngle = (180f - targetPlayerEnemyAngle) * 0.5f;
        Vector3 cameraDirection = Quaternion.AngleAxis(rotationAngle, Vector3.up) * directionFromPlayer;

        Vector3 idealCameraPos = playerPos + (cameraDirection * requiredRadius);
        idealCameraPos.y = playerPos.y + verticalOffset + (lockOnVerticalAngle * 0.1f);

        // Verify angle constraint
        Vector3 playerToCamera = (idealCameraPos - playerPos).normalized;
        Vector3 playerToEnemy = (enemyPos - playerPos).normalized;
        float actualAngle = Vector3.Angle(-playerToCamera, playerToEnemy);

        if (actualAngle < minPlayerEnemyAngle || actualAngle > maxPlayerEnemyAngle)
        {
            float clampedAngle = Mathf.Clamp(actualAngle, minPlayerEnemyAngle, maxPlayerEnemyAngle);
            float angleOffset = clampedAngle - actualAngle;

            cameraDirection = Quaternion.AngleAxis(angleOffset, Vector3.up) * cameraDirection;
            idealCameraPos = playerPos + (cameraDirection * requiredRadius);
            idealCameraPos.y = playerPos.y + verticalOffset + (lockOnVerticalAngle * 0.1f);
        }

        Vector3 directionFromFollow = (idealCameraPos - orbitalFollow.FollowTarget.position);
        directionFromFollow.y = 0f;
        directionFromFollow.Normalize();

        targetHorizontalAngle = Mathf.Atan2(directionFromFollow.x, directionFromFollow.z) * Mathf.Rad2Deg;

        if (orbitalFollow != null)
        {
            orbitalFollow.Radius = Mathf.Lerp(orbitalFollow.Radius, requiredRadius, cameraAdjustmentSpeed * Time.deltaTime);

            currentHorizontalAngle = Mathf.SmoothDampAngle(currentHorizontalAngle, targetHorizontalAngle,
                                                          ref horizontalAngleVelocity, positionSmoothTime);
            orbitalFollow.HorizontalAxis.Value = currentHorizontalAngle;
        }
    }

    private void HandleTargetSwitching()
    {
        if (Time.time - lastTargetSwitchTime < targetSwitchCooldown) return;

        Vector2 lookInput = playerInput.actions["Look"].ReadValue<Vector2>();

        if (Mathf.Abs(lookInput.x) > 0.7f && Mathf.Abs(lastLookInput.x) <= 0.7f)
        {
            UpdateAvailableTargets();

            if (availableTargets.Count > 1)
            {
                Transform nextTarget = GetNextTarget(lookInput.x > 0);
                if (nextTarget != null && nextTarget != currentTarget)
                {
                    currentTarget = nextTarget;
                    lastTargetSwitchTime = Time.time;
                }
            }
        }

        lastLookInput = lookInput;
    }

    private Transform GetNextTarget(bool switchRight)
    {
        if (availableTargets.Count <= 1) return null;

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Transform bestTarget = null;
        float bestScore = float.MinValue;

        foreach (Transform target in availableTargets)
        {
            if (target == currentTarget) continue;

            Vector3 directionToTarget = (target.position - cameraTransform.position).normalized;
            directionToTarget.y = 0f;

            float dotProduct = Vector3.Dot(cameraRight, directionToTarget);
            float score = switchRight ? dotProduct : -dotProduct;

            if (score > bestScore && score > 0)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        return bestTarget ?? availableTargets.FirstOrDefault(t => t != currentTarget);
    }

    private void ValidateCurrentTarget()
    {
        if (currentTarget == null)
        {
            StartDisableLockOn();
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > lockOnRange || !HasLineOfSight(currentTarget))
        {
            StartDisableLockOn();
        }
    }

    public bool IsLockedOn => isLockedOn;
    public Transform CurrentTarget => currentTarget;
    public bool IsTransitioning => isDisablingLockOn;
}