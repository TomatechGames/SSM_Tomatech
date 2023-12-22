using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;
using PlayerControllerProxy = PlayerController.PlayerControllerProxy;

[CreateAssetMenu(menuName ="Tomatech/Tiles/BrickTile")]
public class BrickTile : ThemeRuleTile, IBreakableTile, IBumpableTile
{
    [SerializeField]
    GameObject breakParticlePrefab;
    [SerializeField]
    TileBumpFX bumpFXPrefab;
    [SerializeField]
    TileBase tileOnBumpComplete;

    public override async Task UpdateThemeContent()
    {
        await base.UpdateThemeContent();
        if(breakParticlePrefab && breakParticlePrefab.TryGetComponent<ThemedParticleRetriever>(out var particleTheme))
        {
            await particleTheme.UpdateThemeContent();
        }
    }

    //public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    //{
    //    base.GetTileData(position, tilemap, ref tileData);
    //    //unlock transform
    //    tileData.flags &= ~TileFlags.LockTransform;
    //}

    public void BumpTile(Tilemap map, Vector3Int coordinates, IBumpableTile.BumpDirection dir, GameObject bumpSource, PlayerControllerProxy playerProxy = null)
    {
        //when hit by shell or a strong player, redirect to break
        if (playerProxy is not null && (int)playerProxy.PowerupState.PowerSize <= 1 && dir == IBumpableTile.BumpDirection.Up)
            IBumpableTile.BasicBumpTile(map, coordinates, bumpFXPrefab, tileOnBumpComplete);
        else
            BreakTile(map, coordinates, bumpSource, playerProxy);
    }

    public void BreakTile(Tilemap map, Vector3Int coordinates, GameObject breakSource, PlayerControllerProxy playerProxy = null)
    {
        IBreakableTile.BasicBreakTile(map, coordinates, Instantiate(breakParticlePrefab));
    }
}
