using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class InputSystemPlayerInputProvider : PlayerInputProviderBase
{
    [SerializeField]
    PlayerInput playerInput;

    [Space]
    [SerializeField]
    string mapName;
    [Space]
    [SerializeField]
    string moveName;
    [SerializeField]
    string jumpName;
    [SerializeField]
    string runName;
    [SerializeField]
    string spinName;

    InputState currentState;

    private void Start()
    {
        playerInput.onActionTriggered += ReadAction;
    }

    private void ReadAction(InputAction.CallbackContext ctx)
    {
        // only use actions in the selected map
        if (ctx.action.actionMap.name != mapName)
            return;

        //set input state value based on action name
        if (ctx.action.name == moveName)
        {
            currentState.moveDirection = Vector2.zero;
            currentState.rawMoveDirection = ctx.action.ReadValue<Vector2>();

            if (MathF.Abs(currentState.rawMoveDirection.x) > 0.5f)
                currentState.moveDirection.x = Mathf.Sign(currentState.rawMoveDirection.x);
            if (MathF.Abs(currentState.rawMoveDirection.y) > 0.5f)
                currentState.moveDirection.y = Mathf.Sign(currentState.rawMoveDirection.y);
        }
        else if (ctx.action.name == jumpName)
            currentState.isJumping = ctx.action.IsPressed();
        else if (ctx.action.name == runName)
            currentState.isRunning = ctx.action.IsPressed();
        else if (ctx.action.name == spinName)
            currentState.isSpining = ctx.action.IsPressed();
    }

    public override InputState GetInputState()
    {
        return currentState;
    }
}
