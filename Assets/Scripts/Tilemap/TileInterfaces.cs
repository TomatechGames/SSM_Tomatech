using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using PlayerControllerProxy = PlayerController.PlayerControllerProxy;


public interface IBreakableTile
{
    public void BreakTile(Tilemap map, Vector3Int coordinates, GameObject breakSource, PlayerControllerProxy playerProxy = null);

    protected static void BasicBreakTile(Tilemap map, Vector3Int coordinates, GameObject particleInstance)
    {
        if (particleInstance)
        {
            //GameObject spawned = Object.Instantiate(particleInstance);
            particleInstance.transform.position = map.CellToWorld(coordinates)+(Vector3)(Vector2.one*0.5f);
        }
        map.SetTile(coordinates, null);
    }
}

public interface IBumpableTile
{
    public enum BumpDirection
    {
        Left, 
        Right,
        Down,
        Up,
    };


    public void BumpTile(Tilemap map, Vector3Int coordinates, BumpDirection dir, GameObject bumpSource, PlayerControllerProxy playerProxy = null);

    protected static void BasicBumpTile(Tilemap map, Vector3Int coordinates, TileBumpFX bumpFXPrefab, TileBase tileOnBumpComplete)
    {
        if (!Application.isPlaying || TileBumpFX.IsBumpInProgress(coordinates))
            return;

        if (!bumpFXPrefab)
        {
            Debug.Log("no bump fx prefab provided");
            return;
        }

        var bumpFX = Object.Instantiate(bumpFXPrefab);
        bumpFX.transform.position = map.CellToWorld(coordinates) + new Vector3(0.5f, 0.5f, 0);
        bumpFX.tileOnComplete = tileOnBumpComplete;
        bumpFX.map = map;
        bumpFX.coordinates = coordinates;
        bumpFX.TileSprite = map.GetSprite(coordinates);
        bumpFX.Register();

        map.SetTransformMatrix(coordinates, Matrix4x4.TRS(new Vector3(0, 0, -20), Quaternion.identity, Vector3.one));
    }
}