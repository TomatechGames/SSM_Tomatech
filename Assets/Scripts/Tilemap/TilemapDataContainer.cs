using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapDataContainer : MonoBehaviour
{
    Tilemap m_AttachedTilemap;
    Tilemap AttachedTilemap
    {
        get
        {
            if(!m_AttachedTilemap)
                m_AttachedTilemap = GetComponent<Tilemap>();
            return m_AttachedTilemap;
        }
    }

    [SerializeField]
    [TextArea]
    string tempSource;
    Dictionary<Vector3Int, JSONNode> nodeMap = new();

    private void Awake()
    {
        Parse(tempSource);
    }

    [ContextMenu("Test Compile")]
    void CompileToTemp()
    {
        tempSource = Compile();
    }

    [ContextMenu("Test Map")]
    void TestMap()
    {
        JSONNode testNode = JSON.Parse("{\"test\":\"world\"}");
        Debug.Log(testNode);
        AddNode(Vector3Int.one, testNode);
        Debug.Log(GetNode(Vector3Int.one)["test"]);
    }

    void AddNode(Vector3Int coords, JSONNode node)
    {
        coords.z = 0;
        nodeMap.Add(coords, node);
    }

    JSONNode GetNode(Vector3Int coords)
    {
        coords.z = 0;
        return nodeMap[coords];
    }

    void Parse(string toParse)
    {
        JSONNode root = JSON.Parse(toParse);
        if (root != null)
        {
            foreach (var kvp in root.Linq)
            {
                string[] splitCoords = kvp.Key.Split(',');
                Vector3Int coords = new(int.Parse(splitCoords[0]), int.Parse(splitCoords[1]), 0);
                nodeMap.Add(coords, kvp.Value);
            }
        }
    }

    string Compile()
    {
        JSONNode root = new JSONObject();
        foreach (var kvp in nodeMap)
        {
            root.Add($"{kvp.Key.x},{kvp.Key.y}", kvp.Value);
        }
        return root.ToString();
    }
}
