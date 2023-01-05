using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileBumpFX : MonoBehaviour
{
    const float totalTime = 0.2f;
    const float totalOffset = 0.25f;
    const float totalScale = 1.2f;
    const float halfTime = totalTime * 0.5f;

    [SerializeField]
    SpriteRenderer tileSpriteRenderer;
    public Sprite TileSprite
    {
        get => tileSpriteRenderer.sprite;
        set => tileSpriteRenderer.sprite = value;
    }

    public TileBase tileOnComplete;
    public Tilemap map;
    public Vector3Int coordinates;
    public Action<Tilemap, Vector3Int> invokeOnComplete;
    private static List<Vector3Int> currentBumps = new();

    public static bool IsBumpInProgress(Vector3Int coord)
    {
        return currentBumps.Contains(coord);
    }

    public void Register()
    {
        currentBumps.Add(coordinates);
    }

    IEnumerator Start()
    {
        //laxy workaround to make blocks on the right appear on top of blocks on the left
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.x*-0.002f);
        //dotween breaks when sequences are created on the first frame
        yield return null;
        Sequence bumpSequence = DOTween.Sequence();
        bumpSequence.Append(transform.DOMoveY(transform.position.y+totalOffset, halfTime).SetEase(Ease.OutSine));
        bumpSequence.Join(transform.DOScale(totalScale, halfTime).SetEase(Ease.OutSine));
        bumpSequence.Append(transform.DOMoveY(transform.position.y, halfTime).SetEase(Ease.InSine));
        bumpSequence.Join(transform.DOScale(1, halfTime).SetEase(Ease.InSine));
        bumpSequence.onComplete += CompleteBump;
    }

    void CompleteBump()
    {
        map.SetTransformMatrix(coordinates, Matrix4x4.identity);
        if (tileOnComplete)
            map.SetTile(coordinates, tileOnComplete);
        invokeOnComplete?.Invoke(map, coordinates);
        currentBumps.Remove(coordinates);
        Destroy(gameObject);
    }
}
