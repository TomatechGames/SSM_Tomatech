using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tomatech/Tiles/TerrainRuleTile")]
public class TerrainRuleTile : RuleTile<TerrainRuleTile.Neighbor> {
    public enum TerrainType
    {
        Square,
        Slope,
        SteepSlope
    }
    public TerrainType terrainType;
    [SerializeField]
    RuleTile fallbackRuleSource;

    public class Neighbor : TilingRuleOutput.Neighbor
    {
        public const int Slope = 3;
        public const int SteepSlope = 4;
        public const int Square = 5;
    }

    public override bool RuleMatch(int neighbor, TileBase tile) {
        if(tile is RuleOverrideTile overrideTile)
        {
            tile = overrideTile.m_InstanceTile;
        }
        if(tile is TerrainRuleTile terrainRuleTile)
        {
            switch (neighbor)
            {
                case Neighbor.This: return true;
                case Neighbor.Slope: return terrainRuleTile.terrainType == TerrainType.Slope;
                case Neighbor.SteepSlope: return terrainRuleTile.terrainType == TerrainType.SteepSlope;
                case Neighbor.Square: return terrainRuleTile.terrainType == TerrainType.Square;
                case Neighbor.NotThis: return false;
            }
        }
        else
            return neighbor == Neighbor.NotThis;
        return true;
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);

        if (tileData.sprite == m_DefaultSprite && fallbackRuleSource)
        {
            var tilingRuleBackup = m_TilingRules;
            m_TilingRules = fallbackRuleSource.m_TilingRules;
            base.GetTileData(position, tilemap, ref tileData);
            m_TilingRules = tilingRuleBackup;
        }
    }

}