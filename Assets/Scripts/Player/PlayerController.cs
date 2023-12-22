using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Aarthificial.Reanimation;
using InputState = PlayerInputProviderBase.InputState;
using CollisionInfo = TomatoCollision.CollisionInfo;
using System;
using DG.Tweening;
using Aarthificial.Reanimation.Nodes;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour, ISimulatable
{

    public PlayerInputProviderBase playerInputProvider;
    public TomatoCollision colDetection;
    public Reanimator reanimator;
    [Space]
    [SerializeField] PlayerPowerupState powerupState;
    [SerializeReference] PlayerPowerupState.PowerupData powerupData;
    public PlayerPowerupState PowerupState => powerupState;

    //[Space]
    //[SerializeField] float walkMovementSpeed = 5;
    //[SerializeField] float runMovementSpeed = 12;
    //[SerializeField] float stillToWalkTime = 0.25f;
    //[SerializeField] float walkToRunTime = 0.75f;
    //[SerializeField] float runToStillTime = 1f;
    //[SerializeField] float runSkidTime = 0.75f;
    //[SerializeField] float stillJumpHeight = 4;
    //[SerializeField] float walkJumpHeight = 5;
    //[SerializeField] float runJumpHeight = 5.5f;
    //[SerializeField] float minJumpHeight = 1;
    //[SerializeField] float jumpSpeed = 12;
    //[SerializeField] float gravityForce = 50;
    //[SerializeField] float xMaxSpeed = 20;
    //[SerializeField] float maxJumpBuffer = 0.1f;
    //[SerializeField] Vector2 yMinMaxVelocity = new(-13, 20);
    //[Space]
    //public Vector2 baseSize;
    //public Vector2 crouchSize;

    [Space]
    [SerializeField]
    SimulationConstants simulationConstants;
    [SerializeField]
    SimulationInfo simulationInfo;

    [Space]
    public Tilemap tempMapRef;
    [SerializeField] Vector2 bumpBoxPositioning;

    PlayerControllerProxy m_playerControllerProxy;
    public PlayerControllerProxy ThisPlayerControllerProxy
    {
        get {
            if (m_playerControllerProxy is null)
            {
                m_playerControllerProxy = new(this);
            }
            return m_playerControllerProxy;
        }
    }

    //TODO: move these back into the player controller class, have everything be accessible thru the proxy
    [Serializable]
    public class SimulationConstants
    {
        public float walkMovementSpeed;
        public float runMovementSpeed;
        public float stillToWalkTime;
        public float walkToRunTime;
        public float runToStillTime;
        public float runSkidTime;
        public float stillJumpHeight;
        public float walkJumpHeight;
        public float runJumpHeight;
        public float minJumpHeight;
        public float jumpSpeed;
        public float gravityForce;
        public float xMaxSpeed;
        public float maxRunBuffer;
        public float maxJumpBuffer;
        public float maxSpinBuffer;
        public Vector2 yMinMaxVelocity;

        [Space]
        public float walkAccel;
        public float runAccel;
        public float stillDecel;
        public float skidDecel;

        [Space]
        public float oneOverJumpSpeed;
        public float jumpFalloffDistance;
        public float antiGravMinimum;

        [Space]
        [SerializeField]
        PlayerPowerupState miniDefaultState;
        public PlayerPowerupState MiniDefaultState => miniDefaultState;

        [SerializeField]
        PlayerPowerupState smallDefaultState;
        public PlayerPowerupState SmallDefaultState => smallDefaultState;

        [SerializeField]
        PlayerPowerupState largeDefaultState;
        public PlayerPowerupState LargeDefaultState => largeDefaultState;

        [SerializeField]
        PlayerPowerupState giantDefaultState;
        public PlayerPowerupState GiantDefaultState => giantDefaultState;
    }

    [Serializable]
    public class SimulationInfo
    {
        public CollisionInfo latestCollisionInfo;
        public InputState inputState;

        [Space]
        public float xVel;
        public float yVel;
        public float antiGravCurrent;
        public float antiGravMinimumCurrent;

        [Space]
        public float runBuffer;
        public float jumpBuffer;
        public float spinBuffer;

        public bool facingRight;
        public bool crouched;
        public bool skidding;
        public bool jumpedThisSim;
    }

    public class PlayerControllerProxy
    {
        public PlayerControllerProxy(PlayerController target)
        {
            this.target = target;
        }
        private readonly PlayerController target;
        //public PlayerController Player => target;
        public Vector2 Position => target.transform.position;
        public void SetSize(Vector2 newSize) => target.SetSize(newSize);
        public SimulationConstants SimulationConstants => target.simulationConstants;
        public SimulationInfo SimulationInfo => target.simulationInfo;
        public Reanimator PlayerReanimator => target.reanimator;
        public PlayerPowerupState PowerupState => target.PowerupState;
        public PlayerPowerupState.PowerupData PowerupData => target.powerupData;
        public T GetPowerupData<T>() where T : PlayerPowerupState.PowerupData => PowerupData as T;
    }

    bool cachedRun;
    bool cachedJump;
    bool cachedSpin;
    //float skidEndSpeed;
    int defaultAnimFPS;

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

    private void Start()
    {
        CalculateMovementConstants();
        defaultAnimFPS = reanimator.FPS;
        reanimator.Ticked += SetReanimFramerate;

        if (powerupState == null)
            SetPowerupState(ScriptableObject.CreateInstance<PlayerPowerupState>(), true);

        SetSize(powerupState.BaseSize);
    }

    PlayerPowerupState cachedPowerupState;
    private void OnValidate()
    {
        CalculateMovementConstants();
        if (!Application.isPlaying)
            simulationInfo.latestCollisionInfo.below = true;
        SetPowerupState(PowerupState, !Application.isPlaying);
    }

    void CalculateMovementConstants()
    {
        simulationConstants.walkAccel = simulationConstants.walkMovementSpeed / simulationConstants.stillToWalkTime;
        simulationConstants.runAccel = (simulationConstants.runMovementSpeed - simulationConstants.walkMovementSpeed) / simulationConstants.walkToRunTime;
        simulationConstants.stillDecel = -simulationConstants.runMovementSpeed / simulationConstants.runToStillTime;
        simulationConstants.skidDecel = -simulationConstants.runMovementSpeed / simulationConstants.runSkidTime;

        //part 1: constant upward velocity of jumpSpeed for TARGET seconds
        //part 2: gravity decelerates from runMovementSpeed to 0
        // v=u+at
        // 0=jumpSpeed+grav*time
        //-jumpSpeed/grav = time
        float decelTime = simulationConstants.jumpSpeed / simulationConstants.gravityForce;
        // s=ut+at^2
        // s=jumpSpeed*time + gravity*(time*time)*0.5
        simulationConstants.jumpFalloffDistance = simulationConstants.jumpSpeed * decelTime - simulationConstants.gravityForce * (decelTime * decelTime) * 0.5f;
        // s=ut+(at^2)*0.5
        // jumpHeight-falloffDistance = jumpSpeed*time
        // time = (jumpHeight-falloffDistance)*(1/jumpSpeed)
        simulationConstants.oneOverJumpSpeed = 1 / simulationConstants.jumpSpeed;
        simulationConstants.antiGravMinimum = (simulationConstants.minJumpHeight - simulationConstants.jumpFalloffDistance) * simulationConstants.oneOverJumpSpeed;
    }

    public void SetPowerupState(PlayerPowerupState newPowerupState, bool immediate=false)
    {
        if (!newPowerupState)
            return;
        powerupData = newPowerupState.CreatePowerupData(ThisPlayerControllerProxy);
        newPowerupState.OnSetPowerup(ThisPlayerControllerProxy);

        //Debug.Log(immediate);

        if (simulationInfo.crouched)
            SetSize(newPowerupState.CrouchSize);
        else
            SetSize(newPowerupState.BaseSize);

        if (!immediate && cachedPowerupState)
            StartPowerupFX(cachedPowerupState, newPowerupState);
        else
        {
            reanimator.root = newPowerupState.RootNode;

            newPowerupState.Appearance(0, ThisPlayerControllerProxy);
            reanimator.ForceRerender();
        }

        powerupState = newPowerupState;
        cachedPowerupState = powerupState;
    }

    private void Update()
    {
        var inputState = playerInputProvider.GetInputState();

        if (inputState.isRunning && !cachedRun)
            simulationInfo.runBuffer = simulationConstants.maxRunBuffer;
        cachedRun = inputState.isRunning;

        if (inputState.isJumping && !cachedJump)
            simulationInfo.jumpBuffer = simulationConstants.maxJumpBuffer;
        cachedJump = inputState.isJumping;

        if (inputState.isSpining && !cachedSpin)
            simulationInfo.spinBuffer = simulationConstants.maxSpinBuffer;
        cachedSpin = inputState.isSpining;
    }

    //void FixedUpdate()
    //{
    //    var inputState = playerInputProvider.GetInputState();

    //    for (int i = 0; i < 10; i++)
    //    {
    //        SimulateMovement(Time.fixedDeltaTime * 0.1f, inputState);
    //    }
    //}

    public void Simulate() =>
        SimulateMovement(playerInputProvider.GetInputState());

    void SimulateMovement(InputState inputState)
    {
        simulationInfo.latestCollisionInfo = colDetection.latestInfo;

        if ((simulationInfo.latestCollisionInfo.right && simulationInfo.xVel > 0) || (simulationInfo.latestCollisionInfo.left && simulationInfo.xVel < 0))
        {
            simulationInfo.xVel = 0;
        }
        if ((simulationInfo.latestCollisionInfo.above && simulationInfo.yVel > 0) || (simulationInfo.latestCollisionInfo.isGrounded && simulationInfo.yVel < 0))
            simulationInfo.yVel = 0;

        simulationInfo.inputState = inputState;
        powerupState.Movement(PhysicsSimulator.SIM_DELTA_TIME, ThisPlayerControllerProxy);
        //Movement(PhysicsRunner.SimulatedDeltaTime, inputState, ref prevInfo);

        powerupState.Jumping(PhysicsSimulator.SIM_DELTA_TIME, ThisPlayerControllerProxy);
        powerupState.Gravity(PhysicsSimulator.SIM_DELTA_TIME, ThisPlayerControllerProxy);
        //JumpingAndGravity(PhysicsRunner.SimulatedDeltaTime, inputState, ref prevInfo);

        powerupState.Crouching(PhysicsSimulator.SIM_DELTA_TIME, ThisPlayerControllerProxy);
        //Crouching(inputState, prevInfo);

        powerupState.Interations(PhysicsSimulator.SIM_DELTA_TIME, ThisPlayerControllerProxy);

        powerupState.Appearance(PhysicsSimulator.SIM_DELTA_TIME, ThisPlayerControllerProxy);
        //Appearance(inputState, prevInfo);

        simulationInfo.xVel = Mathf.Clamp(simulationInfo.xVel, -simulationConstants.xMaxSpeed, simulationConstants.xMaxSpeed);
        simulationInfo.yVel = Mathf.Clamp(simulationInfo.yVel, simulationConstants.yMinMaxVelocity.x, simulationConstants.yMinMaxVelocity.y);

        // if on a surface, xVel will be applied relative to the slope of the surface
        Vector2 moveComposite = (simulationInfo.xVel * simulationInfo.latestCollisionInfo.primaryRight) + (simulationInfo.yVel * Vector2.up);
        simulationInfo.latestCollisionInfo.initialPosition = transform.position;
        colDetection.SimulateCollision(moveComposite * PhysicsSimulator.SIM_DELTA_TIME, simulationInfo.latestCollisionInfo);
        var nextInfo = colDetection.latestInfo;
        nextInfo.ApplyToTransform(transform);

        // logic for landing on steep slope
        if (!simulationInfo.latestCollisionInfo.below && nextInfo.below && !nextInfo.isGrounded)
        {
            simulationInfo.xVel = simulationInfo.yVel * -Mathf.Sign(nextInfo.primaryNormal.x);
            simulationInfo.yVel = 0;
        }

        // when leaving a surface, replace slope-space velocity with world-space
        if (simulationInfo.latestCollisionInfo.below && !nextInfo.below && !((moveComposite.x < 0 && nextInfo.left) || (moveComposite.x > 0 && nextInfo.right)))
        {
            simulationInfo.xVel = moveComposite.x;
            simulationInfo.yVel = Mathf.Clamp(moveComposite.y, -simulationConstants.runMovementSpeed * 0.72f, simulationConstants.runMovementSpeed * 0.72f);
        }

        // try bump tiles when colliding above
        if (nextInfo.above && !nextInfo.below)
        {
            simulationInfo.yVel = 0;
            float finalBumpWidth = colDetection.size.x - bumpBoxPositioning.x;
            Vector2 leftPos = new(transform.position.x - (finalBumpWidth * 0.5f), transform.position.y + (colDetection.size.y * 0.5f) + bumpBoxPositioning.y);
            BoundsInt tileRange = new()
            {
                min = tempMapRef.WorldToCell(leftPos),
                max = tempMapRef.WorldToCell(leftPos + (Vector2.right * finalBumpWidth)) + Vector3Int.one
            };

            bool shouldIncreaseBump = true;
            foreach (Vector3Int coord in tileRange.allPositionsWithin)
            {
                if (tempMapRef.GetTile(coord))
                {
                    shouldIncreaseBump = false;
                    break;
                }
            }


            //only increase bump box when no tiles would be hit by initial bump box (TODO: shrink player collision surfaces for everything instead)
            if (shouldIncreaseBump)
            {

                finalBumpWidth = colDetection.size.x;
                leftPos = new(transform.position.x - (finalBumpWidth * 0.5f), transform.position.y + (colDetection.size.y * 0.5f) + bumpBoxPositioning.y);
                tileRange = new()
                {
                    min = tempMapRef.WorldToCell(leftPos),
                    max = tempMapRef.WorldToCell(leftPos + (Vector2.right * finalBumpWidth)) + Vector3Int.one
                };
            }

            foreach (Vector3Int coord in tileRange.allPositionsWithin)
            {
                if(tempMapRef.GetTile(coord) is IBumpableTile bumpableTile)
                {
                    bumpableTile.BumpTile(tempMapRef, coord, IBumpableTile.BumpDirection.Up, gameObject, ThisPlayerControllerProxy);
                }
            }
        }

        simulationInfo.runBuffer -= PhysicsSimulator.SIM_DELTA_TIME;
        simulationInfo.jumpBuffer -= PhysicsSimulator.SIM_DELTA_TIME;
        simulationInfo.spinBuffer -= PhysicsSimulator.SIM_DELTA_TIME;
    }

    public void SetSize(Vector2 newSize)
    {
        Vector2 currentSize = colDetection.size;
        float difference = (currentSize.y - newSize.y) * 0.5f;
        transform.position = transform.position + (difference * Vector3.down);
        colDetection.size = newSize;
        reanimator.transform.localPosition = ((0.5f * colDetection.size.y)+0.0625f) * Vector2.down;
    }

    #region oldLogic

    //void Movement(float dTime, InputState inputState, ref CollisionInfo prevInfo)
    //{
    //    if (!prevInfo.isGrounded && prevInfo.below)
    //    {
    //        simulationInfo.xVel += simulationConstants.gravityForce * dTime * Mathf.Sign(prevInfo.primaryNormal.x);
    //        simulationInfo.xVel = Mathf.Clamp(simulationInfo.xVel, -15, 15);
    //        //xAccel = -xVel * 20;
    //    }
    //    else
    //    {
    //        float prevDir = Mathf.Sign(simulationInfo.xVel);

    //        float targetXVel = 0;
    //        if (inputState.moveDirection.x != 0 && !(simulationInfo.crouched && prevInfo.below))
    //            targetXVel = inputState.isRunning ? simulationConstants.runMovementSpeed : simulationConstants.walkMovementSpeed;
    //        bool changingDirection = (Mathf.Sign(simulationInfo.xVel) == -Mathf.Sign(inputState.moveDirection.x)) && inputState.moveDirection.x!=0 && simulationInfo.xVel !=0;
    //        float currentAccel = (Mathf.Abs(simulationInfo.xVel) <= simulationConstants.walkMovementSpeed) ? simulationConstants.walkAccel : simulationConstants.runAccel;

    //        simulationInfo.skidding = changingDirection && prevInfo.below && (simulationInfo.skidding || Mathf.Abs(simulationInfo.xVel) > simulationConstants.walkMovementSpeed);

    //        if ((Mathf.Abs(targetXVel)<Mathf.Abs(simulationInfo.xVel) && prevInfo.below) || changingDirection)
    //        {
    //            simulationInfo.xVel += (simulationInfo.skidding ? simulationConstants.skidDecel : simulationConstants.stillDecel) * Mathf.Sign(simulationInfo.xVel) * dTime;
    //            if (prevDir != Mathf.Sign(simulationInfo.xVel))
    //                simulationInfo.xVel = 0;
    //        }
    //        else if (Mathf.Abs(targetXVel) >= Mathf.Abs(simulationInfo.xVel))
    //        {
    //            simulationInfo.xVel += currentAccel * inputState.moveDirection.x * dTime;
    //            if (Mathf.Abs(simulationInfo.xVel) > targetXVel)
    //                simulationInfo.xVel = targetXVel * Mathf.Sign(inputState.moveDirection.x);
    //        }

    //        #region oldVelocityStuff
    //        /*
    //        switch (Mathf.Abs(xVel))
    //        {
    //            // when not moving in any direction, decelerate to still
    //            case var _ when inputState.moveDirection.x == 0:
    //                if (xVel == 0 || !prevInfo.below)
    //                    break;
    //                xVel += stillDecel * Mathf.Sign(xVel) * dTime;
    //                if (prevDir != Mathf.Sign(xVel))
    //                    xVel = 0;
    //                break;

    //            // when below walk speed, accellerate in input direction
    //            case var e when e <= walkMovementSpeed:
    //                xVel += walkAccel * inputState.moveDirection.x * dTime;
    //                if (Mathf.Abs(xVel) > walkMovementSpeed && !inputState.isRunning)
    //                    xVel = walkMovementSpeed * Mathf.Sign(inputState.moveDirection.x);
    //                break;

    //            // when above walk speed and not running, decelerate to walk speed
    //            case var e when e > walkMovementSpeed && !inputState.isRunning:
    //                if (!prevInfo.below)
    //                    break;
    //                xVel += stillDecel * Mathf.Sign(xVel) * dTime;
    //                if (walkMovementSpeed > Mathf.Abs(xVel))
    //                    xVel = walkMovementSpeed * Mathf.Sign(inputState.moveDirection.x);
    //                break;

    //            case var _ when !inputState.isRunning:
    //                break;

    //            // when above walk speed, on ground, and changing directions, skid
    //            //case var _ when skidding || (Mathf.Sign(xVel) != Mathf.Sign(inputState.moveDirection.x) && inputState.moveDirection.x != 0):
    //            //    if (!skidding)
    //            //    {
    //            //        skidding = true;
    //            //        skidEndSpeed = -xVel;
    //            //    }
    //            //    if(Mathf.Sign(xVel) == Mathf.Sign(inputState.moveDirection.x))
    //            //    {
    //            //        skidding = false;
    //            //        break;
    //            //    }
    //            //    xVel -= skidDecel * inputState.moveDirection.x * dTime;
    //            //    if (Mathf.Sign(xVel) == Mathf.Sign(inputState.moveDirection.x))
    //            //    {
    //            //        xVel = skidEndSpeed;
    //            //        skidding = false;
    //            //    }
    //            //    break;

    //            // when above walk speed, below run speed, and moving in same direction, accellerate to run speed
    //            case var e when e <= runMovementSpeed:
    //                xVel += runAccel * inputState.moveDirection.x * dTime;
    //                if (Mathf.Abs(xVel) > runMovementSpeed)
    //                    xVel = runMovementSpeed * Mathf.Sign(inputState.moveDirection.x);
    //                break;

    //            // when above run speed, decelerate to run speed
    //            case var e when e > runMovementSpeed:
    //                if (!prevInfo.below)
    //                    break;
    //                xVel += stillDecel * Mathf.Sign(xVel) * dTime;
    //                if (runMovementSpeed > Mathf.Abs(xVel))
    //                    xVel = runMovementSpeed * Mathf.Sign(inputState.moveDirection.x);
    //                break;
    //        }
    //        */
    //        #endregion

    //    }
    //}

    //void JumpingAndGravity(float dTime, InputState inputState, ref CollisionInfo prevInfo)
    //{
    //    simulationInfo.jumpedThisSim = false;
    //    if (simulationInfo.jumpBuffer >0 && prevInfo.isGrounded && !prevInfo.above && simulationInfo.yVel <= 0)
    //    {
    //        simulationInfo.jumpedThisSim = true;
    //        simulationInfo.jumpBuffer = 0;
    //        simulationInfo.yVel = simulationConstants.jumpSpeed;
    //        simulationInfo.antiGravCurrent = JumpTimeFromXSpeed(simulationInfo.xVel);
    //        simulationInfo.antiGravMinimumCurrent = simulationConstants.antiGravMinimum;
    //        prevInfo.isGrounded = false;
    //        prevInfo.below = false;
    //        prevInfo.primaryNormal = Vector2.right;
    //        prevInfo.primaryRight = Vector2.right;
    //    }
    //    if (!inputState.IsHoldingJump)
    //        simulationInfo.antiGravCurrent = 0;
    //    if (prevInfo.above)
    //    {
    //        simulationInfo.antiGravCurrent = 0;
    //        simulationInfo.antiGravMinimumCurrent = 0;
    //    }

    //    simulationInfo.jumpBuffer -= dTime;


    //    if (prevInfo.isGrounded || !prevInfo.below)
    //    {
    //        if (simulationInfo.antiGravCurrent <= 0 && simulationInfo.antiGravMinimumCurrent <= 0)
    //            simulationInfo.yVel -= simulationConstants.gravityForce * dTime;
    //        else
    //        {
    //            simulationInfo.antiGravCurrent -= dTime;
    //            simulationInfo.antiGravMinimumCurrent -= dTime;
    //        }
    //    }
    //}

    //float JumpTimeFromXSpeed(float xSpeed)
    //{
    //    float jumpHeight;

    //    if (Mathf.Abs(xSpeed) < simulationConstants.walkMovementSpeed)
    //        jumpHeight = Mathf.Lerp(simulationConstants.stillJumpHeight, simulationConstants.walkJumpHeight, Mathf.InverseLerp(0, simulationConstants.walkMovementSpeed, Mathf.Abs(xSpeed)));
    //    else
    //        jumpHeight = Mathf.Lerp(simulationConstants.walkJumpHeight, simulationConstants.runJumpHeight, Mathf.InverseLerp(simulationConstants.walkMovementSpeed, simulationConstants.runMovementSpeed, Mathf.Abs(xSpeed)));
    //    //Debug.Log(jumpHeight);
    //    return (jumpHeight - simulationConstants.jumpFalloffDistance) * simulationConstants.oneOverJumpSpeed;
    //}

    //void Crouching(InputState inputState, CollisionInfo prevInfo)
    //{
    //    bool shouldBeCrouched = inputState.moveDirection.y < -0.5f;

    //    if (shouldBeCrouched && !simulationInfo.crouched && prevInfo.below)
    //    {
    //        simulationInfo.crouched = true;
    //        float difference = (powerupState.BaseSize.y - powerupState.CrouchSize.y) * 0.5f;
    //        transform.position = transform.position + (difference * Vector3.down);
    //        colDetection.size = powerupState.CrouchSize;
    //        reanimator.transform.localPosition = Vector2.down * colDetection.size.y * 0.5f;
    //    }
    //    if (!shouldBeCrouched && simulationInfo.crouched && (prevInfo.below || simulationInfo.yVel <0))
    //    {
    //        simulationInfo.crouched = false;
    //        float difference = (powerupState.BaseSize.y - powerupState.CrouchSize.y) * 0.5f;
    //        transform.position = transform.position + (difference * Vector3.up);
    //        colDetection.size = powerupState.BaseSize;
    //        reanimator.transform.localPosition = Vector2.down * colDetection.size.y * 0.5f;
    //    }
    //}

    //void Appearance(InputState inputState, CollisionInfo prevInfo)
    //{
    //    if (inputState.moveDirection.x != 0)
    //        simulationInfo.facingRight = inputState.moveDirection.x > 0;
    //    if (prevInfo.below || simulationInfo.jumpedThisSim) // only do this in SMB1
    //        reanimator.transform.localScale = new Vector3(simulationInfo.facingRight ? 1 : -1, 1, 1);

    //    bool changed = false;

    //    SetReanimState("isCrouched", simulationInfo.crouched ? 1 : 0, ref changed);
    //    //SetReanimState("isSkidding", (xVel != 0 && inputState.moveDirection.x !=0 && Mathf.Sign(xVel) != Mathf.Sign(inputState.moveDirection.x)) ? 1 : 0, ref changed);
    //    SetReanimState("isSkidding", simulationInfo.skidding ? 1 : 0, ref changed);

    //    if (prevInfo.below)
    //        SetReanimState("yState", 1, ref changed);
    //    else if (simulationInfo.yVel < 0)
    //        SetReanimState("yState", 0, ref changed);
    //    else
    //        SetReanimState("yState", 2, ref changed);
    //    if (Mathf.Abs(simulationInfo.xVel) <=0.25f)
    //        SetReanimState("xState", 0, ref changed);
    //    else if (Mathf.Abs(simulationInfo.xVel) <= simulationConstants.walkMovementSpeed *0.5f)
    //        SetReanimState("xState", 1);
    //    else if (Mathf.Abs(simulationInfo.xVel) <= simulationConstants.walkMovementSpeed + (simulationConstants.runMovementSpeed - simulationConstants.walkMovementSpeed)*0)
    //        SetReanimState("xState", 2);
    //    else
    //        SetReanimState("xState", 3);

    //    if(changed)
    //        reanimator.ForceRerender();
    //}

    //void SetReanimState(string stateName, int value)
    //{
    //    reanimator.Set(stateName, value);
    //}
    //void SetReanimState(string stateName, bool value)
    //{
    //    reanimator.Set(stateName, value);
    //}

    //void SetReanimState(string stateName, int value, ref bool changed)
    //{
    //    if (reanimator.State.Get(stateName) != value)
    //    {
    //        changed = true;
    //        reanimator.Set(stateName, value);
    //    }
    //}
    //void SetReanimState(string stateName, bool value, ref bool changed)
    //{
    //    if (reanimator.State.Get(stateName) != (value ? 1 : 0))
    //    {
    //        changed = true;
    //        reanimator.Set(stateName, value);
    //    }
    //}
    #endregion

    private void SetReanimFramerate()
    {
        reanimator.FPS = reanimator.State.Get("setFPS", defaultAnimFPS);
    }

    void StartPowerupFX(PlayerPowerupState fromState, PlayerPowerupState toState)
    {
        //powerups may specify a custom swap effect
        //Debug.Log(fromState.name);
        //Debug.Log(toState.name);

        PlayerPowerupState largestState = fromState.PowerSize > toState.PowerSize ? fromState : toState;
        PlayerPowerupState smallestState = fromState.PowerSize > toState.PowerSize ? toState : fromState;

        PlayerPowerupState defaultLargestState = DefaultStateOfPowerup(largestState);
        if (fromState.PowerSize == toState.PowerSize)
            defaultLargestState = smallestState;

        //if (largestState.PowerSize!= smallestState.PowerSize)
        //{
        //    animSwapState = DefaultStateOfPowerup(largestState);
        //}

        var dtSequence = DOTween.Sequence();

        
        Vector2 relScale = 
            (simulationInfo.crouched ? smallestState.CrouchSize : smallestState.BaseSize)
            / 
            (simulationInfo.crouched ? largestState.CrouchSize : largestState.BaseSize);

        Vector2 fromSize = fromState.PowerSize > toState.PowerSize ? Vector2.one : relScale;
        Vector2 toSize = fromState.PowerSize > toState.PowerSize ? relScale : Vector2.one;

        //1
        //0
        //2
        //1
        //3

        //dtSequence.Append(DOVirtual.DelayedCall(0, () => SetReanimRootImmediate(defaultLargestStata.RootNode)));
        //dtSequence.AppendInterval(0.15f);
        //dtSequence.Join(reanimator.transform.DOScale(Vector2.Lerp(relScale, Vector2.one, 0.5f + (0.125f * 1)), 0));

        //dtSequence.Append(DOVirtual.DelayedCall(0, () => SetReanimRootImmediate(largestState.RootNode)));
        //dtSequence.AppendInterval(0.15f);
        //dtSequence.Join(reanimator.transform.DOScale(Vector2.Lerp(relScale, Vector2.one, 0.5f + (0.125f * 0)), 0));

        dtSequence.Append(DOVirtual.DelayedCall(0, () => SetReanimRootImmediate(largestState.RootNode)));
        dtSequence.AppendInterval(0.1f);
        dtSequence.Join(reanimator.transform.DOScale(Vector2.Lerp(fromSize, toSize, 0.5f + (0.16666f * 1)), 0));

        dtSequence.Append(DOVirtual.DelayedCall(0, () => SetReanimRootImmediate(defaultLargestState.RootNode)));
        dtSequence.AppendInterval(0.1f);
        dtSequence.Join(reanimator.transform.DOScale(Vector2.Lerp(fromSize, toSize, 0.5f + (0.16666f * 0)), 0));

        dtSequence.Append(DOVirtual.DelayedCall(0, () => SetReanimRootImmediate(largestState.RootNode)));
        dtSequence.AppendInterval(0.1f);
        dtSequence.Join(reanimator.transform.DOScale(Vector2.Lerp(fromSize, toSize, 0.5f + (0.16666f * 2)), 0));

        dtSequence.Append(DOVirtual.DelayedCall(0, () => SetReanimRootImmediate(defaultLargestState.RootNode)));
        dtSequence.AppendInterval(0.1f);
        dtSequence.Join(reanimator.transform.DOScale(Vector2.Lerp(fromSize, toSize, 0.5f + (0.16666f * 1)), 0));

        dtSequence.Append(DOVirtual.DelayedCall(0, () => SetReanimRootImmediate(largestState.RootNode)));
        dtSequence.AppendInterval(0.1f);
        dtSequence.Join(reanimator.transform.DOScale(Vector2.Lerp(fromSize, toSize, 0.5f + (0.16666f * 3)), 0));


        dtSequence.Append(DOVirtual.DelayedCall(0, () => SetReanimRootImmediate(toState.RootNode)));
        dtSequence.AppendInterval(0.1f);
        dtSequence.Join(reanimator.transform.DOScale(Vector2.one, 0));

        //dtSequence.Append(DOVirtual.DelayedCall(0, () => SetReanimRootImmediate(toState.RootNode)));
        //dtSequence.Join(reanimator.transform.DOScale(Vector2.one, 0));


        //dtSequence.PrependInterval(1);
        dtSequence.Play();
    }

    PlayerPowerupState DefaultStateOfPowerup(PlayerPowerupState largeState)
    {
        return largeState.PowerSize switch
        {
            PlayerPowerupState.PowerupSize.Mini => simulationConstants.MiniDefaultState,
            PlayerPowerupState.PowerupSize.Small => simulationConstants.SmallDefaultState,
            PlayerPowerupState.PowerupSize.Large => simulationConstants.LargeDefaultState,
            PlayerPowerupState.PowerupSize.Giant => simulationConstants.GiantDefaultState,
            _ => largeState,
        };
    }

    void SetReanimRootImmediate(ReanimatorNode rootNode)
    {
        //Debug.Log("set to " + rootNode.name);
        reanimator.root = rootNode;
        reanimator.ForceRerender();
    }
}
