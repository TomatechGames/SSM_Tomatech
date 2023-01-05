using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerControllerProxy = PlayerController.PlayerControllerProxy;

public class FireMarioState : PlayerPowerupState
{
    [SerializeField]
    Vector2 fireballSpawnOffset;
    [SerializeField]
    GameObject fireballPrefab;
    [SerializeField]
    float shootAnimTime = 0.25f;
    [SerializeField]
    float fireballMaxCooldown = 3f;

    class FireMarioData : PowerupData
    {
        public float currentShootTime;
        public float primaryCooldown;
        public List<Projectile> primaryInstances = new();
        public float secondaryCooldown;
        public List<Projectile> secondaryInstances = new();
    }

    public override PowerupData CreatePowerupData(PlayerControllerProxy playerProxy) => new FireMarioData();

    public override void Interations(float dTime, PlayerControllerProxy playerProxy)
    {
        var powerupData = playerProxy.GetPowerupData<FireMarioData>();

        base.Interations(dTime, playerProxy);

        if (playerProxy.SimulationInfo.crouched)
            powerupData.currentShootTime = 0;

        powerupData.primaryCooldown -= dTime;
        powerupData.secondaryCooldown -= dTime;

        if (playerProxy.SimulationInfo.runBuffer>0 && !playerProxy.SimulationInfo.crouched)
        {
            bool isPrimary = true;
            if (powerupData.primaryCooldown <= 0)
                powerupData.primaryCooldown = fireballMaxCooldown;
            else if (powerupData.secondaryCooldown <= 0)
            {
                isPrimary = false;
                powerupData.secondaryCooldown = fireballMaxCooldown;
            }
            else
                return;

            playerProxy.SimulationInfo.runBuffer = 0;
            powerupData.currentShootTime = shootAnimTime;
            GameObject spawned = Instantiate(fireballPrefab);
            int dir = (int)playerProxy.PlayerReanimator.transform.localScale.x;
            spawned.transform.position = playerProxy.Position + (fireballSpawnOffset*new Vector2(dir, 1));
            var projectile = spawned.GetComponent<Projectile>();
            float baseSpeed = projectile.speed;
            projectile.speed += playerProxy.SimulationInfo.xVel * dir * 0.5f;
            projectile.speed = Mathf.Max(projectile.speed, baseSpeed);
            projectile.speed *= dir;
            projectile.onPreDestroy += (Projectile proj) => RemoveFromInstanceList(proj, isPrimary, powerupData);
            (isPrimary ? powerupData.secondaryInstances : powerupData.primaryInstances).Add(projectile);

        }
    }

    void RemoveFromInstanceList(Projectile instance, bool isPrimary, FireMarioData powerupData)
    {
        var instanceList = isPrimary ? powerupData.secondaryInstances : powerupData.primaryInstances;
        if (instanceList.Contains(instance))
            instanceList.Remove(instance);
        if (instanceList.Count == 0)
        {
            if (isPrimary)
                powerupData.primaryCooldown = 0;
            else
                powerupData.secondaryCooldown = 0;
        }
    }

    protected override void SetReanimatorProperties(float dTime, PlayerControllerProxy playerProxy, ref bool changed)
    {
        var powerupData = playerProxy.GetPowerupData<FireMarioData>();

        if (powerupData.currentShootTime > 0)
        {
            SetReanimState("didJump", false, ref changed, playerProxy.PlayerReanimator);
            SetReanimState("runFrame", 1, ref changed, playerProxy.PlayerReanimator);
        }
        powerupData.currentShootTime -= dTime;
        SetReanimState("isShooting", powerupData.currentShootTime > 0, ref changed, playerProxy.PlayerReanimator);
        base.SetReanimatorProperties(dTime, playerProxy, ref changed);
    }
}
