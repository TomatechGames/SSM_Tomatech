using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapDataContainer : MonoBehaviour
{
    public const string CHECKPOINT_KEY = "_ChkPt";
    Tilemap m_AttachedTilemap;
    public Tilemap AttachedTilemap
    {
        get
        {
            if(!m_AttachedTilemap)
                m_AttachedTilemap = GetComponent<Tilemap>();
            return m_AttachedTilemap;
        }
    }

    [SerializeField]
    Dictionary<Vector3Int, JSONObject> tileDataDict = new();

#if UNITY_EDITOR
    int dictSetCount = 0;
    private void Update()
    {
        if (dictSetCount > 10)
            Debug.Log("Lots of tiles wrote to dictionary this frame");
        dictSetCount = 0;
    }
#endif


    void SetData(Vector3Int coords, JSONObject node)
    {
        coords.z = 0;
        tileDataDict.Add(coords, node);
        dictSetCount++;
    }

    JSONNode GetData(Vector3Int coords)
    {
        coords.z = 0;
        if (tileDataDict.ContainsKey(coords))
            return tileDataDict[coords];
        return null;
    }

    void WriteCheckpointData(string toParse)
    {
        JSONNode root = JSON.Parse(toParse);
        if (root != null && root.IsObject)
        {
            foreach (var kvp in root.AsObject)
            {
                string[] splitCoords = kvp.Key.Split(',');
                Vector3Int coords = new(int.Parse(splitCoords[0]), int.Parse(splitCoords[1]), 0);
                if (!tileDataDict.ContainsKey(coords))
                    tileDataDict[coords] = new();
                tileDataDict[coords][CHECKPOINT_KEY] = kvp.Value;
            }
        }
    }

    string ReadCheckpointData()
    {
        JSONObject root = new();
        foreach (var kvp in tileDataDict)
        {
            if(kvp.Value.ContainsKey(CHECKPOINT_KEY))
            root.Add($"{kvp.Key.x},{kvp.Key.y}", kvp.Value[CHECKPOINT_KEY]);
        }
        return root.ToString();
    }
}
