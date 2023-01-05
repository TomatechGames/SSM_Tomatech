using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TomatoCollision : MonoBehaviour
{
    // Sideways collisions ignore surfaces with a normal angle (relative to local down) less than this, while upwards collisions only react to those surfaces
    public const float CEILING_ANGLE_MAXIMUM = 46;
    // Collisions with a normal angle (relative to local down) less than this are only used by downward collisions, and greater than this are used by sideways collisions
    public const float WALL_ANGLE_MINIMUM = 85;
    // Only downward collisions with a normal angle (relative to local down) less than this will be considered "floor"
    // This is never used in collison calculation, aside from setting the isGrounded property
    public const float FLOOR_ANGLE_MAXIMUM = 46;


    [Tooltip("A small value used to stabelise ray/boxcast collisions")]
    public float skin = 0.05f;
    [Tooltip("Downwards collisions will start this distance above the center of the collider, to improve stability on steep slopes. Increasing this does not reduce the range of collision scans")]
    public float scanOriginOffset = 0.25f;
    [Tooltip("The size of the collider. This should always be taller than it is wide")]
    public Vector2 size = Vector2.one;
    //[Tooltip("The distance from the center of the object that the downwards casts will try to collide with. This is limited to scanDistance, so if this is larger than scanDistance, colliders outside of scanDistance will not be considered")]
    //public float baseDistance = 0.5f;
    //[Tooltip("The full distance from the center of the object that the downwards casts can scan. Colliders outside of baseDistance will not push the player, but will impact the primaryNormal and primaryRight")]
    //public float scanDistance = 1;
    public float overscanDistance = 0.5f;
    [Tooltip("How far up the collider should side collision checks begin, relative to down scan distance (width*0.5). Improves side collision stability when moving up steep slopes, but causes the player to jump up whenever a flat surface passes inside the area (like how minecraft stairs work)")]
    public float sideStartsAt = 0.8f;
    [Tooltip("When leaving a sharp edge at high speed, should the player stick to the corner or fly off in the direction of the slope (design preference)")]
    public bool snapToSharpEdge = false;
    public LayerMask collisionMask;

    // A struct for reading collision detection results
    [Space]
    [SerializeField]
    protected CollisionInfo prevInfo;
    public CollisionInfo latestInfo;

    [Header("Placeholder Physics")]
    public bool autoSimulate;
    public float moveSpeedPerSecond = 0.1f;
    public float gravity = 0.1f;
    public bool debugSimulate;

    [Serializable]
    public struct CollisionInfo
    {
        public bool above, below, left, right;
        public bool isGrounded;
        public int faceDir;
        public Vector2 primaryNormal, primaryRight, finalMoveDelta, initialPosition;
        public void Validate()
        {
            if (primaryNormal == Vector2.zero)
                primaryNormal = Vector2.up;
            if(primaryRight==Vector2.zero)
                primaryRight = Vector2.right;

            primaryNormal.x = Mathf.Round(primaryNormal.x * 1000) * 0.001f;
            primaryNormal.y = Mathf.Round(primaryNormal.y * 1000) * 0.001f;
            primaryRight.x = Mathf.Round(primaryRight.x * 1000) * 0.001f;
            primaryRight.y = Mathf.Round(primaryRight.y * 1000) * 0.001f;
        }

        public void ApplyToTransform(Transform applyTo)
        {
            applyTo.position = applyTo.position + (Vector3)finalMoveDelta;
        }

        public void LeaveGround()
        {
            below = false;
            isGrounded = false;
            primaryRight = Vector2.right;
            primaryNormal = Vector2.up;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void FixedUpdate()
    {
        // Teleports player to mouse cursor for testing purposes

        // A built-in movement controller with no velocity or accelleration.
        // When a substitute for whenever a movement controller has not yet been designed, this can act as a placeholder
        if (autoSimulate)
        {
            //movement simulations are reccomended to be ran 5 times per fixed update
            for (int i = 0; i < 5; i++)
            {
                Vector2 moveDelta = -gravity * Time.fixedDeltaTime * 0.2f * Vector2.up;
                moveDelta += Input.GetAxis("Horizontal") * moveSpeedPerSecond * Time.fixedDeltaTime * 0.2f * latestInfo.primaryRight;

                latestInfo.initialPosition = transform.position;
                SimulateCollision(moveDelta, latestInfo);
                if (!debugSimulate)
                    latestInfo.ApplyToTransform(transform);
            }
        }
    }

    // Movement controllers reference this script and call Simulate with a vector representing how far to move the player.
    // Horizontal movement should be multiplied by collisionInfo.primaryRight to smoothly move along slopes
    // TODO: rework all to account for variable gravity direction
    //static float Delta => Time.fixedDeltaTime * 0.2f;

    //public void Simulate(Vector2 moveDelta, CollisionInfo prevInfo)
    //{
    //    CollisionInfo cachedPrevInfo = prevInfo;
    //    latestInfo = prevInfo;
    //    Vector2 finalMoveComposite = Vector2.zero;
    //    moveDelta *= 0.2f;
    //    for (int i = 0; i < 5; i++)
    //    {
    //        SubSimulate(moveDelta, latestInfo);
    //        moveDelta = latestInfo.finalMoveDelta;
    //        latestInfo.initialPosition += moveDelta;
    //        finalMoveComposite += latestInfo.finalMoveDelta;
    //    }
    //    this.prevInfo = cachedPrevInfo;
    //    latestInfo.finalMoveDelta = finalMoveComposite;
    //}

    public void SimulateCollision(Vector2 moveDelta, CollisionInfo prevInfo)
    {
        this.prevInfo = prevInfo;
        this.prevInfo.Validate();
        latestInfo = new CollisionInfo();

        UpCollisions(ref moveDelta);

        SideCollisions(ref moveDelta, 1);
        SideCollisions(ref moveDelta, -1);

        DownCollisions(ref moveDelta);

        latestInfo.finalMoveDelta = moveDelta;
    }

    // handle top collsions in a 92 degree range
    // flat + midair = simple resist
    // slope + midair = maintain horizontal velocity
    // grounded = resist down slope
    void UpCollisions(ref Vector2 moveDelta)
    {
        Vector2 assumedFinalPosition = prevInfo.initialPosition + moveDelta;

        float totalScanDist = size.y - ((size.x * 0.5f) + scanOriginOffset);

        Vector2 rayStart = assumedFinalPosition + (((size.y*0.5f)-totalScanDist) * Vector2.up);
        Vector2 raySide = (size.x - (skin * 2)) * 0.5f * Vector2.right;

        float currentPushAmount = 0;
        bool stuck = false;

        RaycastHit2D lHit = Physics2D.Raycast(rayStart - raySide, Vector2.up, totalScanDist, collisionMask);
        RaycastHit2D rHit = Physics2D.Raycast(rayStart + raySide, Vector2.up, totalScanDist, collisionMask);
        RaycastHit2D bHit = Physics2D.BoxCast(rayStart, new Vector2(size.x - (skin * 2), 0.001f), 0, Vector2.up, totalScanDist, collisionMask);

        Vector2 slopeNormal = Vector2.down;

        if (lHit.collider && ((lHit.normal.y < 0 && lHit.normal.x>0) || lHit.distance == 0))
        {
            if (lHit.distance == 0)
                stuck = true;
            else if (totalScanDist - lHit.distance > currentPushAmount)
            {
                currentPushAmount = totalScanDist - lHit.distance;
                slopeNormal = lHit.normal;
            }
        }
        if (rHit.collider && ((rHit.normal.y < 0 && rHit.normal.x < 0)|| rHit.distance==0))
        {
            if (rHit.distance == 0)
                stuck = true;
            else if (totalScanDist - rHit.distance > currentPushAmount)
            {
                currentPushAmount = totalScanDist - rHit.distance;
                slopeNormal = rHit.normal;
            }
        }
        if (bHit.collider && (bHit.normal == Vector2.down || bHit.distance==0))
        {
            if (bHit.distance == 0)
                stuck = true;
            else if (totalScanDist - bHit.distance > currentPushAmount)
            {
                currentPushAmount = totalScanDist - bHit.distance;
                slopeNormal = bHit.normal;
            }
        }

        if (stuck)
        {
            latestInfo.above = true;
            return;
        }

        float slopeAngle = Mathf.Round(Vector2.SignedAngle(Vector2.down, slopeNormal)*1000)*0.001f;

        if (Mathf.Abs(slopeAngle) > CEILING_ANGLE_MAXIMUM || currentPushAmount <= 0)
            return;

        latestInfo.above = true;

        if (!prevInfo.below || (slopeAngle==0 && prevInfo.primaryNormal==Vector2.up))
        {
            moveDelta.y -= currentPushAmount;
            return;
        }

        //if ((prevInfo.primaryNormal.x != 0 && slopeNormal.x != 0 && 
        //    Mathf.Sign(slopeNormal.x) == -Mathf.Sign(prevInfo.primaryNormal.x)) || 
        //    prevInfo.primaryRight.y != 0
        //    )
        //{
        //Debug.Log("up");
        UpCornerCollision(ref moveDelta, currentPushAmount, slopeAngle, slopeNormal);

        if (prevInfo.primaryNormal.x > 0)
            latestInfo.left = true;
        else
            latestInfo.right = true;
        //}
        //else
        //    Debug.Log("Unexpected scenario");
    }

    void UpCornerCollision(ref Vector2 moveDelta, float currentPushAmount, float slopeAngle, Vector2 slopeNormal)
    {
        float totalScanDist = size.y - ((size.x * 0.5f) + scanOriginOffset);
        Vector2 origin = prevInfo.initialPosition + moveDelta + (((size.y * 0.5f) - totalScanDist) * Vector2.up);

        Vector2 downwardsSlopeDir = prevInfo.primaryRight * Mathf.Sign(-prevInfo.primaryRight.y);
        float floorAngle = Mathf.Round(Vector2.SignedAngle(Vector2.down, downwardsSlopeDir) * 1000) * 0.001f;

        downwardsSlopeDir = Vector2.ClampMagnitude(downwardsSlopeDir, size.magnitude+moveDelta.magnitude);

        // calculate shortest dist to edge using hit normal
        float cornerToEdge = currentPushAmount * Mathf.Cos(Mathf.Deg2Rad * slopeAngle);
        // calculate hypotenuse of triangle given angle and adjacent
        float finalPushAmount = cornerToEdge / Mathf.Cos(Mathf.Deg2Rad * (Mathf.Abs(floorAngle) + (slopeAngle * -Mathf.Sign(prevInfo.primaryNormal.x))));

        Debug.DrawLine(origin, origin + (Vector2.up * (totalScanDist - currentPushAmount)), Color.black);
        Debug.DrawLine(origin + (Vector2.up * totalScanDist), origin + (Vector2.up * (totalScanDist - currentPushAmount)), Color.blue);
        Debug.DrawLine(origin + (Vector2.up * totalScanDist), origin + (Vector2.up * totalScanDist) + (cornerToEdge * slopeNormal), Color.green);
        Debug.DrawLine(origin + (Vector2.up * totalScanDist), origin + (Vector2.up * totalScanDist) + (finalPushAmount * downwardsSlopeDir), Color.magenta);

        Vector2 change = finalPushAmount * downwardsSlopeDir;
        Debug.DrawLine(prevInfo.initialPosition, prevInfo.initialPosition + change);
        moveDelta += change;

        if (change.magnitude > 100)
        {
            Debug.LogError("An odd collision occured and sent the player very far away. I'm still not sure what causes this");
        }
    }


    // handle top collsions in a 49 degree range (5 down, 44 up)
    // simple resist

    // When a ceiling slope surface is hit, this uses the normal of the slope to push the player down while maintaining their momentum (leads to odd behavior when a semi-steep ceiling slope is hit, WIP)
    void SideCollisions(ref Vector2 moveDelta, float colDirScale = 0)
    {
        Vector2 assumedFinalPosition = prevInfo.initialPosition + moveDelta;

        float sideFloorLeeway = sideStartsAt * size.x * 0.5f;
        Vector2 rayUp = ((size.y * 0.5f) - skin) * Vector2.up;
        Vector2 rayDown = ((size.y * 0.5f) - (sideFloorLeeway + skin)) * Vector2.down;
        Vector2 boxStart = assumedFinalPosition + (rayUp + rayDown) / 2;

        if (colDirScale == 0)
            colDirScale = moveDelta.x > 0 ? 1 : -1;
        Vector2 colDir = colDirScale * Vector2.right;
        float currentPushAmount = 0;
        float totalScanDist = (size.x * 0.5f);
        Vector2 slopeNormal = -colDir;
        bool stuck = false;

        RaycastHit2D uHit = Physics2D.Raycast(assumedFinalPosition + rayUp, colDir, size.x * 0.5f, collisionMask);
        RaycastHit2D dHit = Physics2D.Raycast(assumedFinalPosition + rayDown, colDir, size.x * 0.5f, collisionMask);
        RaycastHit2D bHit = Physics2D.BoxCast(boxStart, new Vector2(0.001f, size.y - ((skin * 2) + sideFloorLeeway)), 0, colDir, size.x * 0.5f, collisionMask);

        if (uHit.collider && uHit.distance > 0 && Vector2.Angle(Vector2.up, uHit.normal) >= WALL_ANGLE_MINIMUM)
        {
            if (uHit.distance == 0)
                stuck = true;
            else if (totalScanDist - uHit.distance > currentPushAmount)
            {
                currentPushAmount = totalScanDist - uHit.distance;
                slopeNormal = uHit.normal;
            }

            //float normalAngle = Vector2.SignedAngle(-colDir, uHit.normal);
            //if (Mathf.Abs(normalAngle) <= 10 || prevInfo.below)
            //    currentPushAmount = (size.x * 0.5f) - uHit.distance;
            //else if(uHit.normal.y < 0)
            //{
            //    currentDownPushAmount = -((size.x * 0.5f) - uHit.distance) * Mathf.Tan(Mathf.Deg2Rad * (90 - normalAngle));
            //    latestInfo.above = true;
            //}
        }
        if (dHit.collider && dHit.distance > 0)
        {
            //Debug.Log(Vector2.Angle(Vector2.up, dHit.normal));
            if (Vector2.Angle(Vector2.up, dHit.normal) >= WALL_ANGLE_MINIMUM)
            {
                if (dHit.distance == 0)
                    stuck = true;
                else if (totalScanDist - dHit.distance > currentPushAmount)
                {
                    currentPushAmount = totalScanDist - dHit.distance;
                    slopeNormal = dHit.normal;
                }
            }
        }
        if (bHit.collider && bHit.normal == -colDir && bHit.distance > 0)
        {
            if (bHit.distance == 0)
                stuck = true;
            else if (totalScanDist - bHit.distance > currentPushAmount)
            {
                currentPushAmount = totalScanDist - bHit.distance;
                slopeNormal = -colDir;
            }
        }

        float worldSlopeAngle = Mathf.Round(Vector2.SignedAngle(Vector2.up, slopeNormal) * 1000) * 0.001f;
        if (Mathf.Abs(worldSlopeAngle) >= 180 - CEILING_ANGLE_MAXIMUM || (currentPushAmount <= 0 && !stuck))
            return;
        float slopeAngle = Mathf.Round(Vector2.SignedAngle(Vector2.right * colDirScale, slopeNormal) * 1000) * 0.001f;

        if(colDirScale==1)
            latestInfo.right = true;
        else
            latestInfo.left = true;
        if (stuck)
            return;

        if (!prevInfo.below)
        {
            moveDelta.x -= currentPushAmount*colDirScale;
            return;
        }

        //Debug.Log("side");
        SideCornerCollision(ref moveDelta, currentPushAmount, slopeAngle, slopeNormal, colDirScale);
    }

    //TODO: merge corner collision functions. DON'T REPEAT YOURSELF
    void SideCornerCollision(ref Vector2 moveDelta, float currentPushAmount, float slopeAngle, Vector2 slopeNormal, float colDirScale)
    {
        Vector2 origin = prevInfo.initialPosition + ((size.y - (skin * 2)) * 0.5f * Mathf.Max(-colDirScale * Mathf.Sign(slopeAngle), 0) * Vector2.up) + moveDelta;

        float totalScanDist = size.x * 0.5f;
        //Vector2 downwardsSlopeDir = prevInfo.primaryRight * Mathf.Sign(-prevInfo.primaryRight.y);
        //float floorAngle = Vector2.SignedAngle(Vector2.right, prevInfo.primaryRight);
        float floorAngle = Mathf.Round(Vector2.SignedAngle(Vector2.right, prevInfo.primaryRight) * 1000) * 0.001f;

        // calculate shortest dist to edge using hit normal
        float cornerToEdge = -currentPushAmount * Mathf.Cos(Mathf.Deg2Rad * slopeAngle);
        // calculate hypotenuse of triangle given angle and adjacent
        float finalPushAmount = (colDirScale * cornerToEdge) / Mathf.Cos(Mathf.Deg2Rad * (Mathf.Abs(floorAngle) + (slopeAngle * Mathf.Sign(prevInfo.primaryNormal.x))));

        finalPushAmount = Mathf.Min(finalPushAmount, 5);
        
        Debug.DrawLine(origin, origin + ((totalScanDist - currentPushAmount) * colDirScale * Vector2.right), Color.black);
        Debug.DrawLine(origin + (colDirScale * totalScanDist * Vector2.right), origin + ((totalScanDist - currentPushAmount) * colDirScale * Vector2.right), Color.blue);
        Debug.DrawLine(origin + (colDirScale * totalScanDist * Vector2.right), origin + (colDirScale * totalScanDist * Vector2.right) + (cornerToEdge * slopeNormal), Color.green);
        Debug.DrawLine(origin + (colDirScale * totalScanDist * Vector2.right), origin + (colDirScale * totalScanDist * Vector2.right) + (finalPushAmount * prevInfo.primaryRight), Color.magenta);

        Vector2 change = finalPushAmount * prevInfo.primaryRight.normalized;
        Debug.DrawLine(prevInfo.initialPosition, prevInfo.initialPosition + change);
        moveDelta += change;
    }

    // This dynamically limits the effective range of it's raycasts to align the lower center of the player with whatever slope they're standing on.
    // Needs a slight rework to account for adjustable collider sizes, and to allow for gravity in any direction
    void DownCollisions(ref Vector2 moveDelta)
    {
        Vector2 assumedFinalPosition = prevInfo.initialPosition + moveDelta;

        float baseDistance = size.x * 0.5f;
        float scanDistance = baseDistance + overscanDistance + scanOriginOffset;

        Vector2 rayStart = assumedFinalPosition + (((-size.y*0.5f)+ baseDistance + scanOriginOffset) *Vector2.up);
        Vector2 raySide = (size.x - (skin * 2)) * 0.5f * Vector2.right;

        RaycastHit2D mHit = Physics2D.Raycast(rayStart, Vector2.down, scanDistance, collisionMask);
        RaycastHit2D lHit = Physics2D.Raycast(rayStart - raySide, Vector2.down, scanDistance, collisionMask);
        RaycastHit2D rHit = Physics2D.Raycast(rayStart + raySide, Vector2.down, scanDistance, collisionMask);
        RaycastHit2D bHit = Physics2D.BoxCast(rayStart, new Vector2(size.x - (skin * 2), 0.001f), 0, Vector2.down, scanDistance, collisionMask);

        Vector2 primaryNormal = Vector2.zero;
        float currentPushAmount = 0;
        float boxLimit = baseDistance;
        bool forceReportGrounded = false;

        if (mHit.collider && Mathf.Abs(Vector2.SignedAngle(Vector2.up, mHit.normal)) < WALL_ANGLE_MINIMUM)
        {

            if (mHit.distance == 0)
            {
                latestInfo.below = true;
                latestInfo.isGrounded = true;
                currentPushAmount = Mathf.Max(currentPushAmount, -moveDelta.y);
            }
            else
            {
                Debug.DrawLine(rayStart, mHit.point);
                primaryNormal = mHit.normal;
                currentPushAmount = Mathf.Max(baseDistance - (mHit.distance - scanOriginOffset), prevInfo.below ? -99 : 0);
                if (currentPushAmount < 0)
                {
                    //Debug.Log("snap to ground");
                    forceReportGrounded = true;
                }
                boxLimit = GetLimitation(primaryNormal);
            }
        }
        if (lHit.collider && Mathf.Abs(Vector2.SignedAngle(Vector2.up, lHit.normal)) < WALL_ANGLE_MINIMUM && lHit.collider is not CircleCollider2D)
        {
            if (lHit.distance == 0)
            {
                latestInfo.below = true;
                latestInfo.isGrounded = true;
                currentPushAmount = Mathf.Max(currentPushAmount, -moveDelta.y);
            }
            else
            {
                Debug.DrawLine(rayStart - raySide, lHit.point);
                float primaryLimit = primaryNormal.x > 0 ? GetLimitation(primaryNormal) : baseDistance;
                float thisLimit = Mathf.Min(primaryLimit, GetLimitation(lHit.normal));
                float thisPushAmount = Mathf.Max(thisLimit - (lHit.distance - scanOriginOffset), currentPushAmount);
                if (thisPushAmount > currentPushAmount)
                {
                    currentPushAmount = thisPushAmount;
                    primaryNormal = lHit.normal;
                }
                if (primaryNormal == Vector2.zero)
                    primaryNormal = lHit.normal;
            }
        }
        if (rHit.collider && Mathf.Abs(Vector2.SignedAngle(Vector2.up, rHit.normal)) < WALL_ANGLE_MINIMUM && rHit.collider is not CircleCollider2D)
        {
            if (rHit.distance == 0)
            {
                latestInfo.below = true;
                latestInfo.isGrounded = true;
                currentPushAmount = Mathf.Max(currentPushAmount, -moveDelta.y);
            }
            else
            {
                Debug.DrawLine(rayStart + raySide, rHit.point);
                float primaryLimit = primaryNormal.x < 0 ? GetLimitation(primaryNormal) : baseDistance;
                float thisLimit = Mathf.Min(primaryLimit, GetLimitation(rHit.normal));
                float thisPushAmount = Mathf.Max(thisLimit - (rHit.distance - scanOriginOffset), currentPushAmount);
                if (thisPushAmount > currentPushAmount)
                {
                    currentPushAmount = thisPushAmount;
                    primaryNormal = rHit.normal;
                }
                if (primaryNormal == Vector2.zero)
                    primaryNormal = rHit.normal;
            }
        }
        if (bHit.collider && bHit.normal == Vector2.up)
        {
            if (bHit.distance==0)
            {
                latestInfo.below = true;
                latestInfo.isGrounded = true;
                currentPushAmount = Mathf.Max(currentPushAmount, -moveDelta.y);
            }
            else
            {
                Debug.DrawLine(rayStart + raySide + (bHit.distance * Vector2.down), rayStart - raySide + (bHit.distance * Vector2.down));
                float pointDistance = ((rayStart.y - scanOriginOffset) - bHit.point.y) - 0.0021f;
                float thisPushAmount = Mathf.Max(boxLimit - pointDistance, currentPushAmount);

                if ((baseDistance - pointDistance) < 0 && prevInfo.below && snapToSharpEdge)
                {
                    currentPushAmount = thisPushAmount;
                    primaryNormal = Vector2.up;
                    forceReportGrounded = true;
                }
                else
                if (thisPushAmount > currentPushAmount)
                {
                    currentPushAmount = thisPushAmount;
                    primaryNormal = Vector2.up;
                }
            }
        }

        if (currentPushAmount <= 0 && !forceReportGrounded)
            primaryNormal = Vector2.up;
        else
        {
            latestInfo.below = true;
            if (Vector2.Angle(Vector2.up, primaryNormal) < FLOOR_ANGLE_MAXIMUM)
                latestInfo.isGrounded = true;
            latestInfo.primaryNormal = primaryNormal;
        }

        latestInfo.primaryRight = Quaternion.Euler(0, 0, -90) * primaryNormal;
        moveDelta.y += currentPushAmount;
    }

    float GetLimitation(Vector2 hitNormal) =>
        Mathf.Lerp(size.x * 0.5f, 0, Mathf.Abs(hitNormal.x / hitNormal.y));
}
