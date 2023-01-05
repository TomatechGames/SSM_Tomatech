using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class TileTestingObject : MonoBehaviour
{
    [SerializeField]
    Tilemap mapRef;

    [SerializeField]
    PlayerController playerRef;

    private void Update()
    {
        if (Gamepad.current.leftTrigger.wasPressedThisFrame)
        {
            StartTest();
        }
    }

    [ContextMenu("Start Test")]
    private void StartTest()
    {
        Vector3Int tileCoord = mapRef.WorldToCell(transform.position);
        TileBase retrievedTile = mapRef.GetTile(tileCoord);
        if (retrievedTile && retrievedTile is IBumpableTile bumpableTile)
        {
            var proxy = playerRef ? playerRef.ThisPlayerControllerProxy : null;
            bumpableTile.BumpTile(mapRef, tileCoord, IBumpableTile.BumpDirection.Down, gameObject, proxy);
        }
    }
}
