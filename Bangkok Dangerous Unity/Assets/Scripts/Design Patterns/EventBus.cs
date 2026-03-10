using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Event { }

public class EventBus<T> where T : Event
{
    public static event Action<T> OnEvent;

    public static void Publish(T pEvent)
    {
        OnEvent?.Invoke(pEvent);
    }

    public static void Subscribe(Action<T> listener)
    {
        OnEvent += listener;
    }

    public static void Unsubscribe(Action<T> listener)
    {
        OnEvent -= listener;
    }
}

public class RuneUnlockEvent : Event
{
    public Rune rune;

    public RuneUnlockEvent(Rune rune)
    {
        this.rune = rune;
    }
}

public class EnemyKilledEvent : Event
{
    public EnemyAI enemy;
    public BaseAI baseEnemy;
    public int experience;

    public EnemyKilledEvent(EnemyAI enemy)
    {
        this.enemy = enemy;
    }
    public EnemyKilledEvent(BaseAI enemy, int exp)
    {
        baseEnemy = enemy;
        this.experience = exp;
    }

}

public class PlayerTakeDamageEvent : Event
{
    public float damageAmount;
    public PlayerTakeDamageEvent(float damage)
    {
        damageAmount = damage;
    }
}

public class PlayerAttackHitEvent : Event
{
    public AttackSO attackData;
    public float finalDamage;
    public GameObject attacker;
    public GameObject target;
    public float staminaCost;
    public bool canBeBlocked;
    public List<AttackEffect> attackEffects;

    public PlayerAttackHitEvent(AttackSO attack, float damage, GameObject from, GameObject to, float stamina, bool blockable, List<AttackEffect> effects)
    {
        attackData = attack;
        finalDamage = damage;
        attacker = from;
        target = to;
        staminaCost = stamina;
        canBeBlocked = blockable;
        attackEffects = new List<AttackEffect>();

        if (effects != null)
        {
            foreach (var ef in effects)
            {
                attackEffects.Add(ef);
            }
        }
    }
}
