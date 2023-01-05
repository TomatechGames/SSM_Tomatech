using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName ="Tomatech/Management/Reference Registry")]
public class ReferenceRegistry : ScriptableObject
{
    [SerializeField]
    List<AddressedObject<GameObject>> m_PrefabList;

    [SerializeField]
    List<AddressedObject<TileBase>> m_TileList;

    [ContextMenu("Manual Initialize")]
    void InitializeDicts()
    {
        prefabDict = new();
        for (int i = 0; i < m_PrefabList.Count; i++)
        {
            if(!prefabDict.ContainsKey(m_PrefabList[i].key))
                prefabDict.Add(m_PrefabList[i].key, m_PrefabList[i].value);
        }
        tileDict = new();
        for (int i = 0; i < m_TileList.Count; i++)
        {
            if (!tileDict.ContainsKey(m_TileList[i].key))
                tileDict.Add(m_TileList[i].key, m_TileList[i].value);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            InitializeDicts();
    }

    Dictionary<string, GameObject> prefabDict;
    Dictionary<string, TileBase> tileDict;

    public GameObject RetrievePrefab(string address)
    {
        if (prefabDict == null)
            InitializeDicts();
        return prefabDict[address];
    }

    public TileBase RetrieveTile(string address)
    {
        if (tileDict == null)
            InitializeDicts();
        return tileDict[address];
    }

    [System.Serializable]
    public struct AddressedObject<T> where T : Object
    {
        public string key;
        public T value;
    }
}
