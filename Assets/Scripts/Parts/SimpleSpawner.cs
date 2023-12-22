using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SimpleSpawner : MonoBehaviour, ISpawner
{
    JSONObject spawnData = new();
    public JSONObject SpawnData => spawnData;
    int spawnerID;
    public int SpawnerID => spawnerID;

    string SpawnableKey => spawnData["k"];

    ISpawner.SpawnerConfig config;
    ISpawnable editorSpawnable; //also contains selected spawnable in level editor
    ISpawnable currentlySpawned;
    bool spawnableConsumed = false;

    [SerializeField]
    string debugSpawnRequest;

    private void Awake()
    {
        if(spawnerID==0)
            spawnerID = ISpawner.SpawnerIDFromGameObject(gameObject);
    }

    [ContextMenu("DebugSetSpawnData")]
    void DebugSetSpawnData()
    {
        SetContainedSpawnData(JSON.Parse(debugSpawnRequest).AsObject);
    }

    public bool IsSpawnDataValid(JSONObject newSpawnData) => true; //accepts all spawn types

    public void SetContainedSpawnData(JSONObject newSpawnData)
    {
        spawnData = newSpawnData.Clone().AsObject;
        editorSpawnable = SpawnPoolManager.Instance.SpawnFromPool(SpawnableKey, this);
        config = editorSpawnable.GetSpawnerConfig();
        if(true)//if not in editor
            editorSpawnable.Despawn();
    }

    public void SetContainedSpawnable(ISpawnable newSpawnable)
    {
        editorSpawnable = newSpawnable;
        spawnData["k"] = newSpawnable.SaveKey;
        spawnData["d"] = newSpawnable.ExtractSaveData();
        config = newSpawnable.GetSpawnerConfig();
    }

    void Initialize()
    {
        //immediately despawn the editor copy
        spawnableConsumed = false;
        //cover edge cases here, as there likely won't be many (eg. adding a separate spawner component for fireballs when bowser is placed)
    }

    [ContextMenu("tryspawn")]
    void TrySpawn()
    {
        Debug.Log(SpawnableKey);
        if (currentlySpawned == null && !spawnableConsumed)
            currentlySpawned = SpawnPoolManager.Instance.SpawnFromPool(SpawnableKey, this);
    }

    public void InformDespawn(ISpawnable spawnable, bool consumed)
    {
        currentlySpawned = null;
        if (consumed)
            spawnableConsumed = true;
    }
}
