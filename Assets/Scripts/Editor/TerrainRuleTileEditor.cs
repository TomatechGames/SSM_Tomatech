using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(TerrainRuleTile))]
public class TerrainRuleTileEditor : RuleTileEditor
{
    [Serializable]
    class RuleTileRuleWrapper
    {
        /// <summary>
        /// List of Rules to serialize
        /// </summary>
        [SerializeField]
        public List<RuleTile.TilingRule> rules = new List<RuleTile.TilingRule>();
    }

    [MenuItem("CONTEXT/RuleTile/Paste Rules Upside Down")]
    public static void PasteRulesYFlipped(MenuCommand item)
    {
        RuleTile tile = item.context as RuleTile;
        if (tile == null)
            return;

        try
        {
            RuleTileRuleWrapper rulesWrapper = new();
            EditorJsonUtility.FromJsonOverwrite(EditorGUIUtility.systemCopyBuffer, rulesWrapper);
            foreach (var rule in rulesWrapper.rules)
            {
                for (int i = 0; i < rule.m_NeighborPositions.Count; i++)
                {
                    var prev = rule.m_NeighborPositions[i];
                    prev.y *= -1;
                    rule.m_NeighborPositions[i] = prev;
                }
                rule.m_Sprites = new Sprite[1];
            }
            
            tile.m_TilingRules.AddRange(rulesWrapper.rules);
        }
        catch (Exception)
        {
            Debug.LogError("Unable to paste rules from system copy buffer");
        }
    }
}
