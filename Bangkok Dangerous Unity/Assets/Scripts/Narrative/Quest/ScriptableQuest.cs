using UnityEngine;
using FlowGraph;
using System.Collections.Generic;
using System;

namespace Quest
{
    [CreateAssetMenu(fileName = "Quest", menuName = "Narrative/Quest")]
    public class ScriptableQuest : FlowGraphObject<MissionNode>
    {
        public override string Name => "Quest";

        public override List<Type> NodeTypes => new List<Type>()
        {
            typeof(MissionNode),
        };
    }
}