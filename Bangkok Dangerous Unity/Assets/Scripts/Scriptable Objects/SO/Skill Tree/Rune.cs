using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Rune", menuName = "Combat/Rune")]
public class Rune : ScriptableObject
{
    public string skillName;
    public int skillPointCost;
    public List<Rune> prerequisites;
    public List<RuneUnlock> unlocks;

    public bool CanUnlock(List<Rune> acquiredRunes)
    {
        foreach (var prereq in prerequisites)
        {
            if (!acquiredRunes.Contains(prereq))
            {
                return false;
            }
        }
        return true;
    }
    
    public bool HasEnoughPoints(int amount)
    {
        if (amount < skillPointCost)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}


[Serializable]
public class RuneUnlock
{
    public RuneType type = RuneType.Default;

    public Rune unlockedRune;
    public StatType statType;
    public float statValue;
}

public enum RuneType
{
    Default,
    Skill,
    Stat
}

public enum StatType
{
    Health,
    Damage,
    Defence,
    Speed,
    Crit
}