using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tomatech.AFASS;
using Tomatech.RePalette;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tomatech/Tiles/ThemeRuleTile")]
public class ThemeRuleTile : RuleOverrideTile, ISavable, IThemeable
{
    [Header("DON'T MODIFY ABOVE HERE")]
    [SerializeField]
    [AddressableKey]
    string addressableKey;
    public string SaveKey => addressableKey;

    //TODO: create specialised container for storing tiling rules
    [SerializeField]
    ThemeAssetReference<RuleTile> replacementTileAddress;

    private new void OnEnable()
    {
        RepaletteResourceManager.RegisterThemeable(this);
        UpdateThemeContent().JustRun();
    }

    public virtual async Task UpdateThemeContent()
    {
        var assetTask = replacementTileAddress?.GetAsset();
        if (assetTask != null)
        {
            await assetTask;
            m_InstanceTile = assetTask.Result;
            m_Tile = m_InstanceTile;
            Override();
        }
    }

    private void OnDisable()
    {
        RepaletteResourceManager.UnregisterThemeable(this);
    }
}
