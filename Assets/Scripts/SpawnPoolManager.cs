using System.Collections;
using System.Collections.Generic;
using Tomatech.AFASS;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SpawnPoolManager : SingletonBehavior<SpawnPoolManager>
{
    Dictionary<string, (GameObject, Queue<ISpawnable>)> poolDict = new();
    [SerializeField]
    int maxPoolSize;

    public void InitializePool(string spawnID)
    {
        if (poolDict.ContainsKey(spawnID))
            return;
        var savableAsset = SavableResourceManager.GetSaveableAsset<GameObject>(spawnID);
        Debug.Log(spawnID);
        Debug.Log(savableAsset);
        if (savableAsset.TryGetComponent<ISpawnable>(out var spawnableAsset))
        {
            Queue<ISpawnable> newQueue = new();
            for (int i = 0; i < maxPoolSize; i++)
            {
                newQueue.Enqueue(Instantiate(savableAsset).GetComponent<ISpawnable>());
            }
            poolDict.Add(spawnID, (savableAsset, newQueue));
        }
    }

    public ISpawnable SpawnFromPool(string spawnID, ISpawner sourceSpawner = null)
    {
        if (!poolDict.ContainsKey(spawnID))
        {
            InitializePool(spawnID);
        }
        var pool = poolDict[spawnID].Item2;
        if (pool.Count > 0)
        {
            var spawned = pool.Dequeue();
            if (sourceSpawner != null)
                spawned.OnSpawn(sourceSpawner);
            return spawned;
        }
        return null;
    }

    public void ReturnToPool(ISpawnable spawnable)
    {
        poolDict[spawnable.SaveKey].Item2.Enqueue(spawnable);
    }

    public void ClearPool(string spawnID)
    {
        if (!poolDict.ContainsKey(spawnID))
            return;
        var pool = poolDict[spawnID].Item2;
        while (pool.Count>0)
        {
            Destroy(pool.Dequeue().GameObject);
        }
        poolDict.Remove(spawnID);
    }
}
