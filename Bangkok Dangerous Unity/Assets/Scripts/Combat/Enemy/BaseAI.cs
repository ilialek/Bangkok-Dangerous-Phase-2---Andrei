using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using static HybridStateMachineCombat;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class BaseAI : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;

    #region Enemy Data
    private string enemyName;
    private int maxHealth;
    private int carriedExperience;
    #endregion

    private int health;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootInterval = 2f;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private int damage = 10;
    private float timerToShoot;

    private Player player;
    private bool canShoot = false;
    private Transform target;

    [Header("Hit Feedback")]
    [SerializeField] private float hitFlashDuration = 0.2f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float deathLaunchForce = 8f;
    [SerializeField] private float deathLaunchUpward = 2f;
    [SerializeField] private Animator animator;

    [Header("Telegraph UI")]
    [SerializeField] private GameObject telegraphUIPrefab;
    [SerializeField] private Vector3 telegraphUIOffset = new Vector3(0f, 2.5f, 0f);
    [SerializeField] private float hitDelayAmount = 1f;
    [SerializeField] private float flashThreshold = 1f;
    [SerializeField] private float flashSpeed = 8f;
    [SerializeField] private Color normalColor = Color.yellow;
    [SerializeField] private Color flashColor = Color.red;

    private GameObject telegraphUIInstance;
    private Image telegraphFillImage;
    private Image telegraphIconImage;
    private TextMeshProUGUI telegraphTextTMP;
    private Canvas telegraphCanvas;
    private bool isFlashing = false;

    private Renderer enemyRenderer;
    private Rigidbody rb;
    private Collider enemyCollider;
    private NavMeshAgent navAgent;
    private Coroutine transformKnockbackCoroutine;
    private Coroutine transformLaunchCoroutine;
    private Material[] originalMaterials;
    private Material[] flashMaterials;
    private Coroutine hitFlashCoroutine;

    #region Eventbus Subscriptions
    private void OnEnable()
    {
        EventBus<PlayerAttackHitEvent>.Subscribe(OnPlayerAttackHit);
    }

    private void OnDisable()
    {
        EventBus<PlayerAttackHitEvent>.Unsubscribe(OnPlayerAttackHit);
    }
    #endregion

    private void Start()
    {
        if (enemyData != null)
        {
            Initialize(enemyData);
            health = maxHealth;
        }
        else
        {
            Debug.LogError("EnemyData is not assigned in the inspector.", this);
        }

        player = FindFirstObjectByType<Player>();

        SetupHitFeedback();
        SetupTelegraphUI();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    private void Initialize(EnemyData data)
    {
        enemyName = data.enemyName;
        maxHealth = data.maxHealth;
        carriedExperience = data.carriedExperience;
    }

    private void SetupHitFeedback()
    {
        enemyRenderer = GetComponentInChildren<Renderer>();

        if (enemyRenderer != null)
        {
            originalMaterials = enemyRenderer.materials;

            flashMaterials = new Material[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                flashMaterials[i] = new Material(originalMaterials[i]);
                flashMaterials[i].color = hitFlashColor;

                if (flashMaterials[i].HasProperty("_EmissionColor"))
                {
                    flashMaterials[i].EnableKeyword("_EMISSION");
                    flashMaterials[i].SetColor("_EmissionColor", hitFlashColor * 0.5f);
                }
            }
        }
    }

    #region Telegraph UI Setup
    private void SetupTelegraphUI()
    {
        if (telegraphUIPrefab == null)
        {
            Debug.LogWarning($"BaseAI on {gameObject.name}: No telegraph UI prefab assigned!");
            return;
        }

        // Instantiate UI as child
        telegraphUIInstance = Instantiate(telegraphUIPrefab, transform);
        telegraphUIInstance.transform.localPosition = telegraphUIOffset;

        // Get canvas and set it to world space
        telegraphCanvas = telegraphUIInstance.GetComponent<Canvas>();
        if (telegraphCanvas != null)
        {
            telegraphCanvas.renderMode = RenderMode.WorldSpace;
            telegraphCanvas.worldCamera = Camera.main;
        }

        // Find UI components (adjust names based on your prefab structure)
        telegraphFillImage = telegraphUIInstance.transform.Find("Phill")?.GetComponent<Image>();
        telegraphIconImage = telegraphUIInstance.transform.Find("Icon")?.GetComponent<Image>();
        telegraphTextTMP = telegraphUIInstance.transform.Find("TimerText")?.GetComponent<TextMeshProUGUI>();

        if (telegraphFillImage == null)
            Debug.LogWarning($"BaseAI on {gameObject.name}: Telegraph UI missing 'Fill' Image component!");
        if (telegraphIconImage == null)
            Debug.LogWarning($"BaseAI on {gameObject.name}: Telegraph UI missing 'Icon' Image component!");

        // Initialize colors
        if (telegraphFillImage != null)
            telegraphFillImage.color = normalColor;
        if (telegraphIconImage != null)
            telegraphIconImage.color = normalColor;

        // Hide UI initially
        telegraphUIInstance.SetActive(false);
    }

    private void UpdateTelegraphUI()
    {
        if (telegraphUIInstance == null || !canShoot)
        {
            if (telegraphUIInstance != null)
                telegraphUIInstance.SetActive(false);
            return;
        }

        telegraphUIInstance.SetActive(true);

        // Calculate progress (inverted: full at start, empty when about to shoot)
        float progress = timerToShoot / shootInterval;
        float timeRemaining = shootInterval - timerToShoot;

        // Update fill amount
        if (telegraphFillImage != null)
        {
            telegraphFillImage.fillAmount = 1f - progress; // Inverse so it empties as time passes
        }

        // Update timer text
        if (telegraphTextTMP != null)
        {
            telegraphTextTMP.text = Mathf.Ceil(timeRemaining).ToString("F0");
        }

        // Flash warning when close to shooting
        if (timeRemaining <= flashThreshold && !isFlashing)
        {
            StartCoroutine(FlashTelegraphUI());
        }
        else if (timeRemaining > flashThreshold && isFlashing)
        {
            StopCoroutine(FlashTelegraphUI());
            isFlashing = false;
            ResetTelegraphUIColor();
        }

        // Make UI face camera
        if (telegraphCanvas != null && Camera.main != null)
        {
            telegraphUIInstance.transform.LookAt(telegraphUIInstance.transform.position + Camera.main.transform.forward);
        }
    }

    private IEnumerator FlashTelegraphUI()
    {
        isFlashing = true;

        while (isFlashing)
        {
            float t = (Mathf.Sin(Time.time * flashSpeed) + 1f) / 2f; // Oscillate between 0 and 1
            Color currentColor = Color.Lerp(normalColor, flashColor, t);

            if (telegraphFillImage != null)
                telegraphFillImage.color = currentColor;
            if (telegraphIconImage != null)
                telegraphIconImage.color = currentColor;

            yield return null;
        }
    }

    private void ResetTelegraphUIColor()
    {
        if (telegraphFillImage != null)
            telegraphFillImage.color = normalColor;
        if (telegraphIconImage != null)
            telegraphIconImage.color = normalColor;
    }
    #endregion

    private void Update()
    {
        CheckForPlayer();
        Shoot();
        UpdateTelegraphUI();
    }

    private void CheckForPlayer()
    {
        Vector3 distance = player.transform.position - transform.position;
        if (distance.magnitude <= 10.0f)
        {
            target = player.transform;
            canShoot = true;
            transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        }
        else
        {
            target = null;
            canShoot = false;
        }
    }

    private void Shoot()
    {
        if (!canShoot)
        {
            timerToShoot = 0f;
            return;
        }

        if (timerToShoot <= shootInterval)
        {
            timerToShoot += Time.deltaTime;
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab);
        bullet.transform.position = firePoint.position;
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.InitializeBullet(target.position, bulletSpeed, damage);

        timerToShoot = 0f;
    }

    private void OnPlayerAttackHit(PlayerAttackHitEvent attackEvent)
    {
        if (attackEvent.target == gameObject)
        {
            ProcessAttackHit(attackEvent);
        }
    }

    private void ProcessAttackHit(PlayerAttackHitEvent attackEvent)
    {
        health -= (int)attackEvent.finalDamage;

        TriggerOnHitReaction();

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.12f, 0.15f);
        }

        ApplyKnockback(attackEvent.attacker);
        ApplyHitDelay(); // Add delay to attack timer

        ApplyAttackEffects(attackEvent.attackEffects, attackEvent.attacker);

        if (health <= 0)
        {
            EventBus<EnemyKilledEvent>.Publish(new EnemyKilledEvent(this, carriedExperience));

            LaunchDeath(attackEvent.attacker);

            if (enemyCollider != null) enemyCollider.enabled = false;
            canShoot = false;

            StartCoroutine(DelayedDestroyRoutine());
        }
    }

    /// <summary>
    /// Adds delay to the attack timer when enemy is hit.
    /// Timer increases up to the maximum of shootInterval.
    /// </summary>
    private void ApplyHitDelay()
    {
        // Reduce the timer (which increases time until next shot)
        timerToShoot = Mathf.Max(0f, timerToShoot - hitDelayAmount);

        //Debug.Log($"BaseAI: Hit delay applied! Timer: {timerToShoot:F2}s / {shootInterval:F2}s");
    }

    private void LaunchDeath(GameObject attacker)
    {
        Vector3 launchDir = attacker == null ? -transform.forward : (transform.position - attacker.transform.position).normalized;
        launchDir.y = 0f; // keep horizontal direction for main push
        Vector3 launchForce = launchDir * deathLaunchForce + Vector3.up * deathLaunchUpward;

        // If we have a Rigidbody, use physics impulse
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(launchForce, ForceMode.Impulse);
            return;
        }

        // If NavMeshAgent exists, disable it so we can move the transform
        if (navAgent != null)
        {
            navAgent.enabled = false;
        }

        if (transformLaunchCoroutine != null)
            StopCoroutine(transformLaunchCoroutine);

        // Use normal scaled time so death launch is affected by time scale
        transformLaunchCoroutine = StartCoroutine(TransformImpulseCoroutine(launchForce, 0.5f, false));
    }

    private void TriggerOnHitReaction()
    {
        animator.SetTrigger("Hit");
    }

    private void ApplyKnockback(GameObject attacker)
    {
        if (attacker == null) return;

        Vector3 knockbackDirection = (transform.position - attacker.transform.position).normalized;
        knockbackDirection.y = 0f; // Keep knockback horizontal

        if (rb != null)
        {
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
            return;
        }

        // Fallback: disable NavMeshAgent and move transform over a short time
        if (navAgent != null)
        {
            navAgent.enabled = false;
        }

        if (transformKnockbackCoroutine != null)
            StopCoroutine(transformKnockbackCoroutine);

        transformKnockbackCoroutine = StartCoroutine(TransformImpulseCoroutine(knockbackDirection * knockbackForce, 0.15f, false));
    }

    private IEnumerator TransformImpulseCoroutine(Vector3 totalDisplacement, float duration, bool useUnscaled)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = useUnscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.position += totalDisplacement * (dt / duration);
            elapsed += dt;
            yield return null;
        }
    }

    #region Hit Flash Effect
    /// <summary>
    /// Triggers the red flash effect when enemy is hit
    /// </summary>
    private void TriggerHitFlash()
    {
        if (enemyRenderer == null) return;

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }

        hitFlashCoroutine = StartCoroutine(HitFlashCoroutine());
    }

    /// <summary>
    /// Coroutine that handles the hit flash effect
    /// </summary>
    private IEnumerator HitFlashCoroutine()
    {
        if (enemyRenderer != null && flashMaterials != null)
        {
            enemyRenderer.materials = flashMaterials;
        }

        yield return new WaitForSeconds(hitFlashDuration);

        if (enemyRenderer != null && originalMaterials != null)
        {
            enemyRenderer.materials = originalMaterials;
        }

        hitFlashCoroutine = null;
    }
    #endregion

    private IEnumerator DelayedDestroyRoutine()
    {
        // Hide telegraph UI on death
        if (telegraphUIInstance != null)
            telegraphUIInstance.SetActive(false);

        // Wait in real time so slow motion doesn't shorten the delay
        yield return new WaitForSecondsRealtime(0.5f);

        // Optionally add cleanup here (disable AI, colliders, etc.) before destroying
        Destroy(gameObject);
    }

    private void ApplyAttackEffects(List<AttackEffect> effects, GameObject attacker)
    {
        if (effects == null) return;
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case AttackEffect.Stun:
                    //
                    break;
                case AttackEffect.Knockback:
                    //
                    break;
                case AttackEffect.Bleed:
                    //
                    break;
                case AttackEffect.Stagger:
                    //
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        if (telegraphUIInstance != null)
        {
            Destroy(telegraphUIInstance);
        }
    }
}