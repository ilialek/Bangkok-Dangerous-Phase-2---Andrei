using System;
using UnityEngine;

public class CombatEvents
{
    public event Action<Vector3> onFightStarted;

    public void FightStarted(Vector3 fightOrigin)
    {
        onFightStarted?.Invoke(fightOrigin);
    }
}