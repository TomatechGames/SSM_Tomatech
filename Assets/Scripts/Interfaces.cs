using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using Tomatech.AFASS;
using UnityEngine;

public interface ISpawner
{
    public static ISpawner debugPairSpawner;
    static int SpawnerIDFromGameObject(GameObject gameObject) => gameObject.GetInstanceID();
    public int SpawnerID { get; }
    public JSONObject SpawnData { get; }
    public bool IsSpawnDataValid(JSONObject spawnData);
    public void SetContainedSpawnable(ISpawnable spawnable);
    public void InformDespawn(ISpawnable spawnable, bool consumed);

    [ContextMenu("Set Pairable Spawner")]
    public void SetPairableSpawner()
    {
        debugPairSpawner = this;
    }

    struct SpawnerConfig
    {
        public int maximumSpawns;
        public bool onePerSpawner;
        public bool refillIfNotConsumed;
    }
}

//spawners should use this for recieving drag and drops.
//this allows objects that arent spawnable when placed normally (like coins) to
//specify a spawnable to use when dragged onto
public interface ISpawnableProvider 
{
    public ISpawnable GetSpawnable();
}

public interface ISpawnable : ISavable
{
    public GameObject GameObject { get; }
    public ISpawner SpawnedFrom { get; set; }
    public bool IsSpawned { get; }
    public void OnSpawn(ISpawner fromSpawner)
    {
        SpawnedFrom = fromSpawner;
    }

    public void Despawn(bool consumed = false)
    {
        if (SpawnedFrom != null)
            SpawnedFrom.InformDespawn(this, consumed);
    }


    [ContextMenu("Pair To Spawner")]
    public void PairToSpawner()
    {
        ISpawner.debugPairSpawner?.SetContainedSpawnable(this);
    }

    public ISpawner.SpawnerConfig GetSpawnerConfig() => 
    new()
    {
        maximumSpawns = 0,
        onePerSpawner = false,
        refillIfNotConsumed = false,
    };
}