using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CollisionInfo = TomatoCollision.CollisionInfo;

public class Projectile : MonoBehaviour, ISimulatable
{
    public TomatoCollision colDetection;

    public float speed = 15;
    public float bounceHeight = 1;
    public float bounceDist = 4;
    public int maxBounces = -1;
    public float maxLifetime = 10;
    public bool collides = true;
    public int maxYSpeed = 10;
    public event Action<Projectile> onPreDestroy;

    [Space]
    [SerializeField]
    float bounceVel;
    [SerializeField]
    float gravity;

    [Space]
    [SerializeField]
    Vector2 velocity;
    [SerializeField]
    CollisionInfo latestColInfo;
    [SerializeField]
    int currentBounces;
    [SerializeField]
    float currentLifetime;

    private void OnEnable()
    {
        if (PhysicsSimulator.Instance)
            PhysicsSimulator.Instance.RegisterSimulatable(this);
    }

    private void OnDisable()
    {
        if(PhysicsSimulator.Instance)
            PhysicsSimulator.Instance.UnregisterSimulatable(this);
    }

    private void OnValidate()
    {
        velocity.x = speed;
        velocity.y = -maxYSpeed;
        //float timeToBounceDist = bounceDist / speed;
        //bounceVel = (-2 * bounceHeight) / (timeToBounceDist * 0.5f);
        //gravity = (-2 * bounceHeight) / (timeToBounceDist * timeToBounceDist);

    }

    public virtual void Simulate()
    {
        velocity.x = speed;
        latestColInfo.initialPosition = transform.position;
        colDetection.SimulateCollision(velocity * PhysicsSimulator.SIM_DELTA_TIME, latestColInfo);
        var nextInfo = colDetection.latestInfo;
        nextInfo.ApplyToTransform(transform);

        bool trueBelow = nextInfo.below || nextInfo.isGrounded;

        if ((trueBelow && currentBounces > maxBounces && maxBounces>=0) || nextInfo.right || nextInfo.above || nextInfo.left || currentLifetime > maxLifetime)
        {
            onPreDestroy?.Invoke(this);
            Destroy(gameObject);
        }

        //gravity
        velocity.y += gravity*PhysicsSimulator.SIM_DELTA_TIME;
        velocity.y = Mathf.Clamp(velocity.y, -maxYSpeed, maxYSpeed);

        if (trueBelow)
        {
            //jump
            velocity.y = -bounceVel;
            nextInfo.LeaveGround();
            currentBounces++;
        }

        currentLifetime += PhysicsSimulator.SIM_DELTA_TIME;
        latestColInfo = nextInfo;
    }
}
