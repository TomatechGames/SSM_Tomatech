using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerInputProviderBase : MonoBehaviour
{
    // abstracts the process of delivering inputs from the PlayerInput component to the PlayerController component,
    // so we can swap it out with automated inputs for stuff like walking from the goal pole to the doorway, or other stuff

    public struct InputState
    {
        public Vector2 rawMoveDirection;
        public Vector2 moveDirection;
        public bool isJumping;
        public bool isRunning;
        public bool isSpining;

        public bool IsHoldingJump => isJumping || isSpining;
    }

    public abstract InputState GetInputState();
}
