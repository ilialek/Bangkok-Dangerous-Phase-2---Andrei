using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Utilities;

public enum EnemyState
{
    Idle,
    Moving,
    Attacking,
    Blocking,
    Dodging,
    Stunned,
    Staggered
}

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;

    #region Enemy Data
    private string enemyName;
    private int maxHealth;
    private float moveSpeed;
    private int attackDamage;
    private float attackCooldown;
    private CombatStyleSO combatStyle;
    #endregion

    private int health;
    private Transform player;
    private bool canMove;

    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Hit Feedback")]
    [SerializeField] private float hitFlashDuration = 0.2f;
    [SerializeField] private Color hitFlashColor = Color.red;
    
    private Renderer enemyRenderer;
    private Material[] originalMaterials;
    private Material[] flashMaterials;
    private Coroutine hitFlashCoroutine;

    private StateMachine stateMachine = new StateMachine();
    private GameObject playerObject;
    private bool canAttack = false;

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

    private void Awake()
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

        player = GameObject.FindGameObjectWithTag("Player").transform;
        Player playerScript = player.GetComponent<Player>();


        canMove = true;

        var EnemyIdleState = new EnemyIdle();
        var EnemyMoveState = new EnemyMove(this, moveSpeed, rotationSpeed, player);
        var EnemyAttackState = new EnemyAttack(this, attackDamage, attackCooldown, combatStyle, playerScript);

        Func<bool> PlayerInRange() => () => playerObject != null;
        Func<bool> PlayerOutOfRange() => () => playerObject == null;
        Func<bool> CanAttack() => () => canAttack;
        Func<bool> CannotAttack() => () => !canAttack;

        stateMachine.AddTransition(EnemyIdleState, EnemyMoveState, PlayerInRange());
        stateMachine.AddTransition(EnemyMoveState, EnemyIdleState, PlayerOutOfRange());

        stateMachine.AddTransition(EnemyMoveState, EnemyAttackState, CanAttack());
        stateMachine.AddTransition(EnemyAttackState, EnemyMoveState, CannotAttack());

        stateMachine.SetState(EnemyIdleState);

        SetupHitFeedback();
    }

    private void Initialize(EnemyData data)
    {
        enemyName = data.enemyName;
        maxHealth = data.maxHealth;
        moveSpeed = data.moveSpeed;
        attackDamage = data.attackDamage;
        attackCooldown = data.attackCooldown;
        combatStyle = data.combatStyle;
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

    private void Update()
    {
        stateMachine.Execute();

        StateTransitionChecks();
    }

    private void StateTransitionChecks()
    {
        PlayerInRange();
        PlayerInAttackRange();
    }

    private void PlayerInRange()
    {
        Vector3 distance = player.position - transform.position;
        if (distance.magnitude > 10.0f)
        {
            playerObject = null;
        }
        else
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }
    }

    private void PlayerInAttackRange()
    {
        if (playerObject == null) 
        {
            canAttack = false;
            return;
        }
        Vector3 distance = player.position - transform.position;
        if (distance.magnitude <= 2.0f)
        {
            canAttack = true;
        }
        else
        {
            canAttack = false;
        }
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

        //Trigger hit animation, VFX, etc.
        
        TriggerHitFlash();

        // Trigger a small camera shake to give feedback when enemy is hit
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.12f, 0.15f);
        }

        ApplyAttackEffects(attackEvent.attackEffects, attackEvent.attacker);

        if (health <= 0)
        {
            EventBus<EnemyKilledEvent>.Publish(new EnemyKilledEvent(this));
            Destroy(gameObject);
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

    private void ApplyAttackEffects(List<AttackEffect> effects, GameObject attacker)
    {
        if (effects == null) return;
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case AttackEffect.Stun:
                    //SetEnemyState(EnemyState.Stunned);
                    break;
                case AttackEffect.Knockback:
                    //SetEnemyState(EnemyState.Staggered);
                    break;
                case AttackEffect.Bleed:
                    //Bleed effect
                    break;
                case AttackEffect.Stagger:
                    //SetEnemyState(EnemyState.Staggered);
                    break;
            }
        }
    }

    #region State Management

    private void UpdateState()
    {
        //switch (currentState)
        //{
        //    case EnemyState.Idle:
        //        //Idle behavior, standing, looking arround, maybe moving slightly?
        //        break;
        //    case EnemyState.Moving:
        //        //Moving towards player or circle around player or patrolling (patrolling only valid in noncombat situation) 
        //        break;
        //    case EnemyState.Attacking:
        //        //Executing attack behavior, checking for hits...
        //        break;
        //    case EnemyState.Blocking:
        //        //Executing blocking behavior (preventing or reducing damage taken, reducing super armor)
        //        break;
        //    case EnemyState.Dodging:
        //        //Start dodge animation and prevent incoming damage/attacks/effects for duration of dodge
        //        break;
        //    case EnemyState.Stunned:
        //        //Interrupt current action, play hit reaction and stun animation, prevent enemy actions for stun duration
        //        break;
        //    case EnemyState.Staggered:
        //        //Interrupt current action, play stagger animation, prevent enemy actions for stagger duration
        //        break;
        //}
    }

    #endregion

    private void OnDestroy()
    {
        if (flashMaterials != null)
        {
            foreach (Material mat in flashMaterials)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }
}
