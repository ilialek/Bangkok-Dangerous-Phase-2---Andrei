using UnityEngine;

[CreateAssetMenu(fileName = "New Player Data", menuName = "Combat/Player Data", order = 1)]
public class PlayerData : ScriptableObject
{
    public int maxHealth;
    public float maxHeat;
    public float heatLoseRate;
    public float armor;
    public float superArmor;
    public float damageMultiplier;
}