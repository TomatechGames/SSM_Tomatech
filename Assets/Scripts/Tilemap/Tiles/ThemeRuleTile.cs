using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tomatech.AFASS;
using Tomatech.RePalette;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tomatech/ThemeTile")]
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
        var _ = UpdateThemeContent();
    }

    public async Task UpdateThemeContent()
    {
        var assetTask = replacementTileAddress?.GetAsset();
        if (assetTask != null)
        {
            await assetTask;
            m_InstanceTile = assetTask.Result;
            m_Tile = m_InstanceTile;
            Debug.Log(assetTask.Result);
            Override();
        }
    }

    private void OnDisable()
    {
        RepaletteResourceManager.UnregisterThemeable(this);
    }
}
