using UnityEngine;
using FlowGraph;
using System.Collections.Generic;
using System;

namespace Dialogue
{
    [CreateAssetMenu(fileName = "Dialogue", menuName = "Narrative/Dialogue")]
    public class ScriptableDialogue : FlowGraphObject<TextNode>
    {
        public override string Name => "Dialogue";

        public override List<Type> NodeTypes => new List<Type>()
        {
            typeof(TextNode),
            typeof(BranchNode)
        };
    }
}