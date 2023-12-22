using System.Collections;
using System.Collections.Generic;
using Tomatech.AFASS;
using Unity.VisualScripting;
using UnityEngine;
using static PlayerController;
using CollisionInfo = TomatoCollision.CollisionInfo;

[RequireComponent(typeof(TomatoCollision))]
public class PowerupItem : MonoBehaviour, ISimulatable, ISpawnable
{
    public GameObject GameObject => gameObject;
    TomatoCollision m_Collision;
    protected TomatoCollision Collision
    {
        get
        {
            if (!m_Collision)
                m_Collision = GetComponent<TomatoCollision>();
            return m_Collision;
        }
    }

    ISpawner spawnedFrom;
    public ISpawner SpawnedFrom
    {
        get => spawnedFrom;
        set => spawnedFrom = value;
    }

    bool isSpawned;
    public bool IsSpawned => isSpawned;

    [SerializeField]
    [AddressableKey]
    string saveKey;
    public string SaveKey => saveKey;

    [SerializeField] protected PlayerPowerupState linkedPowerupState;
    [SerializeField] protected float gravityForce;
    [SerializeField] protected LayerMask playerLayer;
    protected CollisionInfo latestInfo;
    protected Vector2 vel;

    private void OnEnable()
    {
        if (PhysicsSimulator.Instance)
            PhysicsSimulator.Instance.RegisterSimulatable(this);
    }

    private void OnDisable()
    {
        if (PhysicsSimulator.Instance)
            PhysicsSimulator.Instance.UnregisterSimulatable(this);
    }

    public virtual void Simulate()
    {
        Motion();
        latestInfo.initialPosition = transform.position;
        Collision.SimulateCollision(vel * PhysicsSimulator.SIM_DELTA_TIME, latestInfo);
        latestInfo = Collision.latestInfo;
        latestInfo.ApplyToTransform(transform);

        var hitPlayers = Physics2D.OverlapBoxAll(transform.position, Collision.size, 0, playerLayer);
        if (hitPlayers.Length > 0)
        {
            Collider2D closestCol = hitPlayers[0];
            if(hitPlayers.Length > 1)
            {
                float recordDistance = 999;
                foreach (var playerBox in hitPlayers)
                {
                    float thisDist = Vector2.Distance(transform.position, playerBox.bounds.center);
                    if (thisDist < recordDistance)
                    {
                        closestCol = playerBox;
                        recordDistance = thisDist;
                    }
                }
            }
            PlayerController foundPlayer = closestCol.GetComponentInParent<PlayerController>();
            if (foundPlayer)
            {
                foundPlayer.SetPowerupState(linkedPowerupState);
                Debug.Log("Apply Powerup");
                Destroy(gameObject);
            }
        }
    }

    protected virtual void Motion()
    {
        if ((latestInfo.above && vel.y > 0) || (latestInfo.isGrounded && vel.y < 0))
            vel.y = 0;
        vel.y -= gravityForce * PhysicsSimulator.SIM_DELTA_TIME;
    }
}
