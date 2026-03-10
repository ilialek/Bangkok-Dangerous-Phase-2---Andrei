using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SkillTreeSystem : MonoBehaviour
{
    [SerializeField] private int level = 1;
    [SerializeField] private int skillPoints = 40;
    [SerializeField] private int experiencePoints = 0;
    [SerializeField] private int experienceThreshold = 50;
    [SerializeField] private float thresholdPercentIncrease = 1.2f;
    [SerializeField] private List<Rune> acquiredRunes = new List<Rune>();

    [Header("Dw about this")]
    [SerializeField] private TMP_Text text;
    private bool enoughSP = false;

    private void OnEnable()
    {
        EventBus<EnemyKilledEvent>.Subscribe(GetExp);
    }

    private void OnDisable()
    {
        EventBus<EnemyKilledEvent>.Unsubscribe(GetExp);
    }

    private void Start()
    {
        text.text = $"{skillPoints.ToString()}";
    }

    private void GetExp(EnemyKilledEvent evt)
    {
        experiencePoints += evt.experience;
        //Debug.Log($"Current xp: {experiencePoints.ToString()}");
        CheckExpThreshold();
    }

    private bool CheckExpThreshold()
    {
        if (experiencePoints >= experienceThreshold)
        {
            int prevLevel = level;
            experiencePoints -= experienceThreshold;
            LevelUp();
            CheckExpThreshold();

            if (level - prevLevel > 1)
            {
                //Debug.Log($"Leveled up : {level - prevLevel} times");
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    private void LevelUp()
    {
        level++;
        experienceThreshold = (int)(experienceThreshold * thresholdPercentIncrease);
        //Debug.Log("Leveled up!");
        //Debug.Log($"Next exp threshold: {experienceThreshold.ToString()}");
        //Debug.Log($"Final exp: {experiencePoints.ToString()}");
        GetLevelUpReward(level);
    }

    private void GetLevelUpReward(int curLevel)
    {
        skillPoints += 4;
        text.text = $"SP: {skillPoints.ToString()}";
    }

    public void UnlockRune(Rune rune)
    {
        if (rune.CanUnlock(acquiredRunes) && rune.HasEnoughPoints(skillPoints))
        {
            acquiredRunes.Add(rune);
            skillPoints -= rune.skillPointCost;
            text.text = $"SP: {skillPoints.ToString()}";
            enoughSP = true;
            EventBus<RuneUnlockEvent>.Publish(new RuneUnlockEvent(rune));
        }
        else
        {
            Debug.Log("Not enough SP");
            foreach (var prereq in rune.prerequisites)
            {
                Debug.Log($"Skill requires: {prereq.name}");
            }
        }
    }

    public void UpdateText(TMP_Text txt)
    {
        if (enoughSP)
        {
            txt.text = "Acquired";
            enoughSP = false;
        }
    }
}