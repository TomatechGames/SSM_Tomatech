using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputState = PlayerInputProviderBase.InputState;
using CollisionInfo = TomatoCollision.CollisionInfo;
using SimulationInfo = PlayerController.SimulationInfo;
using SimulationConstants = PlayerController.SimulationConstants;
using PlayerControllerProxy = PlayerController.PlayerControllerProxy;
using Aarthificial.Reanimation.Nodes;
using Aarthificial.Reanimation;
using System;

[CreateAssetMenu(menuName ="Tomatech/Powerups/Powerless Mario")]
public class PlayerPowerupState : ScriptableObject
{
    public enum PowerupSize
    {
        Mini,
        Small,
        Large,
        Giant
    }

    [SerializeField]
    ReanimatorNode rootNode;
    public virtual ReanimatorNode RootNode => rootNode;
    [SerializeField]
    Vector2 baseSize = new(1, 0.75f);
    public virtual Vector2 BaseSize => baseSize;
    [SerializeField]
    Vector2 crouchSize = new(1, 0.75f);
    public virtual Vector2 CrouchSize=>crouchSize;
    [SerializeField]
    PowerupSize powerupSize = PowerupSize.Mini;
    public PowerupSize PowerSize => powerupSize;

    [Serializable]
    public class PowerupData
    {
        //per powerup data goes here
    }

    public virtual PowerupData CreatePowerupData(PlayerControllerProxy playerProxy) => new();


    public virtual void OnSetPowerup(PlayerControllerProxy playerProxy)
    {

    }

    public virtual void Movement(float dTime, PlayerControllerProxy playerProxy)
    {

        if (!playerProxy.SimulationInfo.latestCollisionInfo.isGrounded && playerProxy.SimulationInfo.latestCollisionInfo.below)
        {
            playerProxy.SimulationInfo.xVel += playerProxy.SimulationConstants.gravityForce * dTime * Mathf.Sign(playerProxy.SimulationInfo.latestCollisionInfo.primaryNormal.x);
            playerProxy.SimulationInfo.xVel = Mathf.Clamp(playerProxy.SimulationInfo.xVel, -15, 15);
            //xAccel = -xVel * 20;
        }
        else
        {
            float prevDir = Mathf.Sign(playerProxy.SimulationInfo.xVel);

            float targetXVel = 0;
            if (playerProxy.SimulationInfo.inputState.moveDirection.x != 0 && !(playerProxy.SimulationInfo.crouched && playerProxy.SimulationInfo.latestCollisionInfo.below))
                targetXVel = playerProxy.SimulationInfo.inputState.isRunning ? playerProxy.SimulationConstants.runMovementSpeed : playerProxy.SimulationConstants.walkMovementSpeed;
            bool changingDirection = (Mathf.Sign(playerProxy.SimulationInfo.xVel) == -Mathf.Sign(playerProxy.SimulationInfo.inputState.moveDirection.x)) && playerProxy.SimulationInfo.inputState.moveDirection.x != 0 && playerProxy.SimulationInfo.xVel != 0;
            float currentAccel = (Mathf.Abs(playerProxy.SimulationInfo.xVel) <= playerProxy.SimulationConstants.walkMovementSpeed) ? playerProxy.SimulationConstants.walkAccel : playerProxy.SimulationConstants.runAccel;

            playerProxy.SimulationInfo.skidding = changingDirection && playerProxy.SimulationInfo.latestCollisionInfo.below && (playerProxy.SimulationInfo.skidding || Mathf.Abs(playerProxy.SimulationInfo.xVel) > playerProxy.SimulationConstants.walkMovementSpeed);

            if ((Mathf.Abs(targetXVel) < Mathf.Abs(playerProxy.SimulationInfo.xVel) && playerProxy.SimulationInfo.latestCollisionInfo.below) || changingDirection)
            {
                playerProxy.SimulationInfo.xVel += (playerProxy.SimulationInfo.skidding ? playerProxy.SimulationConstants.skidDecel : playerProxy.SimulationConstants.stillDecel) * Mathf.Sign(playerProxy.SimulationInfo.xVel) * dTime;
                if (prevDir != Mathf.Sign(playerProxy.SimulationInfo.xVel))
                    playerProxy.SimulationInfo.xVel = 0;
            }
            else if (Mathf.Abs(targetXVel) >= Mathf.Abs(playerProxy.SimulationInfo.xVel))
            {
                playerProxy.SimulationInfo.xVel += currentAccel * playerProxy.SimulationInfo.inputState.moveDirection.x * dTime;
                if (Mathf.Abs(playerProxy.SimulationInfo.xVel) > targetXVel)
                    playerProxy.SimulationInfo.xVel = targetXVel * Mathf.Sign(playerProxy.SimulationInfo.inputState.moveDirection.x);
            }

            #region oldVelocityStuff
            /*
            switch (Mathf.Abs(xVel))
            {
                // when not moving in any direction, decelerate to still
                case var _ when inputState.moveDirection.x == 0:
                    if (xVel == 0 || !prevInfo.below)
                        break;
                    xVel += stillDecel * Mathf.Sign(xVel) * dTime;
                    if (prevDir != Mathf.Sign(xVel))
                        xVel = 0;
                    break;

                // when below walk speed, accellerate in input direction
                case var e when e <= walkMovementSpeed:
                    xVel += walkAccel * inputState.moveDirection.x * dTime;
                    if (Mathf.Abs(xVel) > walkMovementSpeed && !inputState.isRunning)
                        xVel = walkMovementSpeed * Mathf.Sign(inputState.moveDirection.x);
                    break;

                // when above walk speed and not running, decelerate to walk speed
                case var e when e > walkMovementSpeed && !inputState.isRunning:
                    if (!prevInfo.below)
                        break;
                    xVel += stillDecel * Mathf.Sign(xVel) * dTime;
                    if (walkMovementSpeed > Mathf.Abs(xVel))
                        xVel = walkMovementSpeed * Mathf.Sign(inputState.moveDirection.x);
                    break;

                case var _ when !inputState.isRunning:
                    break;

                // when above walk speed, on ground, and changing directions, skid
                //case var _ when skidding || (Mathf.Sign(xVel) != Mathf.Sign(inputState.moveDirection.x) && inputState.moveDirection.x != 0):
                //    if (!skidding)
                //    {
                //        skidding = true;
                //        skidEndSpeed = -xVel;
                //    }
                //    if(Mathf.Sign(xVel) == Mathf.Sign(inputState.moveDirection.x))
                //    {
                //        skidding = false;
                //        break;
                //    }
                //    xVel -= skidDecel * inputState.moveDirection.x * dTime;
                //    if (Mathf.Sign(xVel) == Mathf.Sign(inputState.moveDirection.x))
                //    {
                //        xVel = skidEndSpeed;
                //        skidding = false;
                //    }
                //    break;

                // when above walk speed, below run speed, and moving in same direction, accellerate to run speed
                case var e when e <= runMovementSpeed:
                    xVel += runAccel * inputState.moveDirection.x * dTime;
                    if (Mathf.Abs(xVel) > runMovementSpeed)
                        xVel = runMovementSpeed * Mathf.Sign(inputState.moveDirection.x);
                    break;

                // when above run speed, decelerate to run speed
                case var e when e > runMovementSpeed:
                    if (!prevInfo.below)
                        break;
                    xVel += stillDecel * Mathf.Sign(xVel) * dTime;
                    if (runMovementSpeed > Mathf.Abs(xVel))
                        xVel = runMovementSpeed * Mathf.Sign(inputState.moveDirection.x);
                    break;
            }
            */
            #endregion

        }
    }

    public virtual void Jumping(float dTime, PlayerControllerProxy playerProxy)
    {
        playerProxy.SimulationInfo.jumpedThisSim = false;
        if (playerProxy.SimulationInfo.jumpBuffer > 0 && playerProxy.SimulationInfo.latestCollisionInfo.isGrounded && !playerProxy.SimulationInfo.latestCollisionInfo.above && playerProxy.SimulationInfo.yVel <= 0)
        {
            playerProxy.SimulationInfo.jumpedThisSim = true;
            playerProxy.SimulationInfo.jumpBuffer = 0;
            playerProxy.SimulationInfo.yVel = playerProxy.SimulationConstants.jumpSpeed;
            playerProxy.SimulationInfo.antiGravCurrent = JumpTimeFromXSpeed(playerProxy.SimulationInfo.xVel, playerProxy.SimulationConstants);
            playerProxy.SimulationInfo.antiGravMinimumCurrent = playerProxy.SimulationConstants.antiGravMinimum;
            playerProxy.SimulationInfo.latestCollisionInfo.LeaveGround();
        }
        if (!playerProxy.SimulationInfo.inputState.IsHoldingJump)
            playerProxy.SimulationInfo.antiGravCurrent = 0;
    }

    public virtual void Gravity(float dTime, PlayerControllerProxy playerProxy)
    {
        if (playerProxy.SimulationInfo.latestCollisionInfo.above)
        {
            playerProxy.SimulationInfo.antiGravCurrent = 0;
            playerProxy.SimulationInfo.antiGravMinimumCurrent = 0;
        }

        if (playerProxy.SimulationInfo.latestCollisionInfo.isGrounded || !playerProxy.SimulationInfo.latestCollisionInfo.below)
        {
            if (playerProxy.SimulationInfo.antiGravCurrent <= 0 && playerProxy.SimulationInfo.antiGravMinimumCurrent <= 0)
                playerProxy.SimulationInfo.yVel -= playerProxy.SimulationConstants.gravityForce * dTime;
            else
            {
                playerProxy.SimulationInfo.antiGravCurrent -= dTime;
                playerProxy.SimulationInfo.antiGravMinimumCurrent -= dTime;
            }
        }
    }

    protected float JumpTimeFromXSpeed(float xSpeed, SimulationConstants simulationConstants)
    {
        float jumpHeight;

        if (Mathf.Abs(xSpeed) < simulationConstants.walkMovementSpeed)
            jumpHeight = Mathf.Lerp(simulationConstants.stillJumpHeight, simulationConstants.walkJumpHeight, Mathf.InverseLerp(0, simulationConstants.walkMovementSpeed, Mathf.Abs(xSpeed)));
        else
            jumpHeight = Mathf.Lerp(simulationConstants.walkJumpHeight, simulationConstants.runJumpHeight, Mathf.InverseLerp(simulationConstants.walkMovementSpeed, simulationConstants.runMovementSpeed, Mathf.Abs(xSpeed)));
        //Debug.Log(jumpHeight);
        return (jumpHeight - simulationConstants.jumpFalloffDistance) * simulationConstants.oneOverJumpSpeed;
    }

    public virtual void Crouching(float dTime, PlayerControllerProxy playerProxy)
    {
        bool shouldBeCrouched = playerProxy.SimulationInfo.inputState.moveDirection.y < -0.5f;
        if (shouldBeCrouched && !playerProxy.SimulationInfo.crouched && (playerProxy.SimulationInfo.latestCollisionInfo.below || playerProxy.SimulationInfo.jumpedThisSim))
        {
            playerProxy.SimulationInfo.crouched = true;
            playerProxy.SetSize(CrouchSize);
            //float difference = (baseSize.y - crouchSize.y) * 0.5f;
            //player.transform.position = player.transform.position + (difference * Vector3.down);
            //player.colDetection.size = crouchSize;
            //player.reanimator.transform.localPosition = 0.5f * player.colDetection.size.y * Vector2.down;
        }
        //TODO: check if player has room to uncrouch
        if (!shouldBeCrouched && playerProxy.SimulationInfo.crouched && (playerProxy.SimulationInfo.latestCollisionInfo.below || playerProxy.SimulationInfo.jumpedThisSim || playerProxy.SimulationInfo.yVel < 0))
        {
            playerProxy.SimulationInfo.crouched = false;
            playerProxy.SetSize(BaseSize);
            //float difference = (baseSize.y - crouchSize.y) * 0.5f;
            //player.transform.position = player.transform.position + (difference * Vector3.up);
            //player.colDetection.size = baseSize;
            //player.reanimator.transform.localPosition = 0.5f * player.colDetection.size.y * Vector2.down;
        }
    }

    public virtual void Interations(float dTime, PlayerControllerProxy playerProxy)
    {
        //projectiles or smtn
    }

    public virtual void Appearance(float dTime, PlayerControllerProxy playerProxy)
    {
        if (playerProxy.SimulationInfo.inputState.moveDirection.x != 0)
            playerProxy.SimulationInfo.facingRight = playerProxy.SimulationInfo.inputState.moveDirection.x > 0;

        if (playerProxy.SimulationInfo.latestCollisionInfo.below || playerProxy.SimulationInfo.jumpedThisSim) // only do this in SMB1
        {
            Vector3 baseSize = playerProxy.PlayerReanimator.transform.localScale;
            baseSize.x = Mathf.Abs(baseSize.x) * (playerProxy.SimulationInfo.facingRight ? 1 : -1);
            playerProxy.PlayerReanimator.transform.localScale = baseSize;
        }

        bool changed = false;

        SetReanimatorProperties(dTime, playerProxy, ref changed);

        if (changed)
            playerProxy.PlayerReanimator.ForceRerender();
    }

    protected virtual void SetReanimatorProperties(float dTime, PlayerControllerProxy playerProxy, ref bool changed)
    {
        SetReanimState("isCrouched", playerProxy.SimulationInfo.crouched, ref changed, playerProxy.PlayerReanimator);
        //SetReanimState("isSkidding", (xVel != 0 && inputState.moveDirection.x !=0 && Mathf.Sign(xVel) != Mathf.Sign(inputState.moveDirection.x)) ? 1 : 0, ref changed);
        SetReanimState("isSkidding", playerProxy.SimulationInfo.skidding, ref changed, playerProxy.PlayerReanimator);

        if (playerProxy.SimulationInfo.jumpedThisSim || playerProxy.SimulationInfo.latestCollisionInfo.below)
            SetReanimState("didJump", playerProxy.SimulationInfo.jumpedThisSim && !playerProxy.SimulationInfo.crouched, ref changed, playerProxy.PlayerReanimator);

        if (playerProxy.SimulationInfo.latestCollisionInfo.below)
            SetReanimState("yState", 1, ref changed, playerProxy.PlayerReanimator);
        else if (playerProxy.SimulationInfo.yVel < 0)
            SetReanimState("yState", 0, ref changed, playerProxy.PlayerReanimator);
        else
            SetReanimState("yState", 2, ref changed, playerProxy.PlayerReanimator);
        if (Mathf.Abs(playerProxy.SimulationInfo.xVel) <= 0.25f)
            SetReanimState("xState", 0, ref changed, playerProxy.PlayerReanimator);
        else if (Mathf.Abs(playerProxy.SimulationInfo.xVel) <= playerProxy.SimulationConstants.walkMovementSpeed * 0.5f)
            SetReanimState("xState", 1, playerProxy.PlayerReanimator);
        else if (Mathf.Abs(playerProxy.SimulationInfo.xVel) <= playerProxy.SimulationConstants.walkMovementSpeed + (playerProxy.SimulationConstants.runMovementSpeed - playerProxy.SimulationConstants.walkMovementSpeed) * 0)
            SetReanimState("xState", 2, playerProxy.PlayerReanimator);
        else
            SetReanimState("xState", 3, playerProxy.PlayerReanimator);
    }

    protected void SetReanimState(string stateName, int value, Reanimator reanimator)
    {
        reanimator.Set(stateName, value);
    }
    protected void SetReanimState(string stateName, bool value, Reanimator reanimator)
    {
        reanimator.Set(stateName, value);
    }

    protected void SetReanimState(string stateName, int value, ref bool changed, Reanimator reanimator)
    {
        if (reanimator.State.Get(stateName) != value)
        {
            changed = true;
            reanimator.Set(stateName, value);
        }
    }
    protected void SetReanimState(string stateName, bool value, ref bool changed, Reanimator reanimator)
    {
        if (reanimator.State.Get(stateName) != (value ? 1 : 0))
        {
            changed = true;
            reanimator.Set(stateName, value);
        }
    }
}
