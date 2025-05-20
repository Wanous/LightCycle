using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;


// Require necessary components to ensure they exist on the GameObject
[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{   
    // --- Enemy Components ---
    [FormerlySerializedAs("player")] [Header("Components")]
    public CharacterController enemy;
    [Tooltip("Assign a child GameObject positioned at the player's base for ground checks")]
    public Transform groundCheckPoint; // Note: This seems unused if front/rearWheelCheck are primary for grounding
    [Tooltip("Material to use for the line segments between colliders")]
    public Material segmentLineMaterial;
    
    // --- Player Coordonate ---
    public Transform player;
    
    // --- Movement Parameters ---
    [Header("Movement")]
    public float minSpeed = 5f;
    public float maxSpeed = 50f;
    public float acceleration = 10f;
    public float brakingDeceleration = 30f;
    public float progressiveDecelerationRate = 2.5f;
    public float mindistanceofdash = 10f;
    public float maxdistanceofdash = 20f;
    public float angleToSlowDown = 60f;
    private float currentDeceleration;
    [Tooltip("Base steer speed at minimum speed")]
    public float baseSteerSpeed = 180f;
    [Tooltip("Factor to reduce steer speed as speed increases (0 for no reduction, higher values for more reduction)")]
    [Range(0f, 1f)] public float steerSpeedReductionFactor = 0.5f;
    public float gravity = -19.62f;
    public float jumpHeight = 2f;
    private readonly bool showdetection = false;
    public bool canJump = true;
    private readonly bool canaccelerate = true;
    private readonly bool canslowdown = true;
    private readonly bool canfollowthetarget = true;
    private readonly bool candetecteobjectandvoid = true;
    
    // --- SpawnPoint Parameters ---
    private SpawnPointEnemy spawnpoint;

    public void Spawnpointset(SpawnPointEnemy spawnPointEnemy)
    {
        spawnpoint = spawnPointEnemy;
    }
    
    // --- Slope Movement Parameters ---
    [Header("Slope Movement")]
    public float slopeForceMultiplier = 5f; // Note: This variable is declared but not explicitly used in ApplyMovement for force.
    public float maxSlopeAngle = 45f; // Note: This variable is declared but not explicitly used to limit movement on slopes.
    private bool isOnSlope;
    private Vector3 slopeNormal;
    public float uphillSpeedMultiplier = 0.5f;
    public float downhillSpeedMultiplier = 1.2f;
    
    // --- Leaning Parameters ---
    [Header("Leaning")]
        
    [Tooltip("The Transform to apply lean rotation to (e.g., the bike chassis). If null, leans the root object.")]
    public Transform leanTarget;
    [Tooltip("Maximum lean angle in degrees when turning")]
    public float maxLeanAngle = 25f;
    [Tooltip("How quickly the player leans into/out of turns")]
    public float leanSpeed = 5f;
    [Tooltip("Maximum lean angle in degrees when on a slope")]
    public float maxSlopeLeanAngle = 10f;
    [Tooltip("How much the slope affects leaning (0-1)")]
    [Range(0f, 1f)] public float slopeLeanSensitivity = 1f;
    [Tooltip("Transform for the left side chassis ground check raycast origin. Place on the chassis.")]
    public Transform leftChassisGroundCheck;
    [Tooltip("Transform for the right side chassis ground check raycast origin. Place on the chassis.")]
    public Transform rightChassisGroundCheck;
    [Tooltip("Max distance for the chassis side ground check raycasts.")]
    public float chassisSideCheckMaxDistance = 0.5f;
    [Tooltip("Max distance of the raycast downwards from the vehicle's center for slope detection during leaning.")]
    public float chassisCentralCheckMaxDistance = 0.6f;


    // --- Trail Collision Parameters ---
    [Header("Trail Collision")]
    public LayerMask trailLayer;
    public float trailLifetime = 4.2f;
    public float colliderSpacing = 0.1f;
    public float positionOffset = 1.0f;
    [Tooltip("Delay in seconds before a trail collider becomes active to prevent self-collision.")]
    public float collisionActivationDelay = 0.2f;
    public float segmentLineWidth = 0.3f;
    public float playerToColliderLineWidth = 0.3f;
    [Tooltip("The tag to apply to the trail collider objects")]
    public string trailColliderTag = "Trail";
    [Tooltip("The tag of the colliders that will cause death")]
    public string hazardTag = "Hazard";

    // --- Ground Check Parameters ---
    [Header("Ground Check")]
    [Tooltip("The physics layer(s) considered 'ground'")]
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f; // Used for central lean ray, but front/rear wheel checks use fixed 1f
    public Transform frontWheelCheck; // Moved from Leaning to Ground Check as it's for grounding
    public Transform rearWheelCheck;  // Moved from Leaning to Ground Check
    
    // --- Effects ---
    [FormerlySerializedAs("OrangeEffect")]
    [Header("Effects")]
    [SerializeField] ParticleSystem orangeEffect;
    [SerializeField] ParticleSystem darkOrangeEffect;
    [FormerlySerializedAs("BlackEffect")] [SerializeField] ParticleSystem blackEffect;

    // --- Wheel Rotation ---
    [Header("Wheel Rotation")]
    public Transform frontWheel;
    public Transform rearWheel;
    public float wheelRotationMultiplier = -50f;

    // --- Private Variables ---
    private float currentMoveSpeed;
    private Vector3 velocity;
    private bool isGroundedStatus; // Overall grounded status based on wheels
    private bool isDead;
    private GameObject playerToColliderLineObject;
    private LineRenderer playerToColliderLineRenderer;
    private float frontWheelRollAngle;
    private float rearWheelRollAngle;
    private float currentSteerInput;
    private float currentLeanAngleZ; // Z-axis lean (steering)
    private float currentLeanAngleX; // X-axis lean (slope/side)
    private bool isBraking;
    /*
    private float deathTime;
    private Vector3 previousFramePosition;
    */
    private Vector3 storedSlopeNormal = Vector3.up; // Stores the normal of the ground last touched by wheels

    // --- Trail Data Structures ---
    private class TrailPoint
    {
        public Vector3 WorldPosition;
        public float Timestamp;
    }
    private readonly List<TrailPoint> trailPoints = new List<TrailPoint>();
    private readonly List<GameObject> trailColliderObjects = new List<GameObject>();

    // --- Initialization ---
    void Start()
    {
        // Initialisation
        isOnSlope = false;
        isGroundedStatus = false; // Overall grounded status based on wheels
        isDead = false;
        frontWheelRollAngle = 0f;
        rearWheelRollAngle = 0f;
        currentSteerInput = 0f;
        currentLeanAngleZ = 0f; // Z-axis lean (steering)
        currentLeanAngleX = 0f; // X-axis lean (slope/side)
        isBraking = false;
        
        if (enemy == null) enemy = GetComponent<CharacterController>();
        
        groundLayer = LayerMask.GetMask("Ground");
        
        // Null Checks
        if (enemy == null)
        {
            Debug.LogError("CharacterController missing!", this);
            this.enabled = false;
            return;
        }
        
        // groundCheckPoint is not strictly necessary if front/rearWheelCheck are used for main ground detection
        if (groundCheckPoint == null)
            Debug.LogWarning("Ground Check Point not assigned (though front/rear wheel checks are primary).", this);
        if (segmentLineMaterial == null) Debug.LogWarning("Segment Line Material not assigned.", this);
        if (frontWheel == null) Debug.LogWarning("Front Wheel not assigned.", this);
        if (rearWheel == null) Debug.LogWarning("Rear Wheel not assigned.", this);
        if (leanTarget == null)
        {
            Debug.LogWarning("Lean Target not assigned. Leaning root object.", this);
            leanTarget = this.transform;
        }

        if (groundLayer.value == 0)
        {
            Debug.LogError("Ground Layer not set!", this);
            this.enabled = false;
            return;
        }

        // if (speedometerNeedle == null) Debug.LogWarning("Speedometer Needle not assigned.", this);
        if (frontWheelCheck == null)
        {
            Debug.LogError("Front Wheel Check Transform not assigned!", this);
            this.enabled = false;
            return;
        }

        if (rearWheelCheck == null)
        {
            Debug.LogError("Rear Wheel Check Transform not assigned!", this);
            this.enabled = false;
            return;
        }

        // New Lean Check Transforms
        if (leftChassisGroundCheck == null)
        {
            Debug.LogWarning("Left Chassis Ground Check Transform not assigned. Lean correction might be impaired.",
                this);
        }

        if (rightChassisGroundCheck == null)
        {
            Debug.LogWarning("Right Chassis Ground Check Transform not assigned. Lean correction might be impaired.",
                this);
        }

        currentMoveSpeed = minSpeed;
        currentDeceleration = brakingDeceleration;

        playerToColliderLineObject = new GameObject("PlayerToColliderLine");
        playerToColliderLineRenderer = playerToColliderLineObject.AddComponent<LineRenderer>();
        playerToColliderLineRenderer.useWorldSpace = true;
        playerToColliderLineRenderer.positionCount = 2;
        playerToColliderLineRenderer.startWidth = playerToColliderLineWidth;
        playerToColliderLineRenderer.endWidth = playerToColliderLineWidth;
        playerToColliderLineRenderer.sortingOrder = -1;
        if (segmentLineMaterial != null) playerToColliderLineRenderer.material = segmentLineMaterial;
    }

    // --- Frame Update ---
    void Update()
    {
        HandleGroundCheck();    // Determines isGroundedStatus and slopeNormal from wheels
        HandleMovementInput();  // Handles acceleration, speed, steering
        ApplyGravity();         // Applies gravity to velocity.y
        HandleLeaning();        // Calculates and applies lean to leanTarget
        ApplyMovement();        // Moves the CharacterController
        UpdateWheelRotation();  // Rotates wheel visuals
        UpdateTrailSystem();    // Manages light trail colliders
        UpdatePlayerToColliderLine(); // Visual line to last trail point
    }

    // --- Ground Check Logic (Wheels) ---
    void HandleGroundCheck()
    {
        // Raycast downwards from front and rear wheel positions
        // The length of this ray (1f) should be tuned to your vehicle's wheel radius/suspension
        bool frontGrounded = Physics.Raycast(frontWheelCheck.position, Vector3.down, out RaycastHit frontHit, 1f, groundLayer);
        bool rearGrounded = Physics.Raycast(rearWheelCheck.position, Vector3.down, out RaycastHit rearHit, 1f, groundLayer);
        isGroundedStatus = frontGrounded || rearGrounded; // Player is grounded if at least one wheel is

        // Determine the combined slope normal based on wheel contacts
        if (frontGrounded && rearGrounded)
            slopeNormal = (frontHit.normal + rearHit.normal).normalized; // Average normal if both wheels hit
        else if (frontGrounded)
            slopeNormal = frontHit.normal; // Use front wheel normal
        else if (rearGrounded)
            slopeNormal = rearHit.normal; // Use rear wheel normal
        else
            slopeNormal = Vector3.up; // Assume flat (no slope) if airborne

        // Store the slope normal when grounded for use when airborne (prevents snapping)
        if (isGroundedStatus)
        {
            storedSlopeNormal = slopeNormal;
        }
        else
        {
            // When in the air, keep using the last grounded normal for consistent behavior (e.g. air control if any, or lean persistence)
            slopeNormal = storedSlopeNormal;
        }

        // Determine if on a slope and the angle of the slope
        isOnSlope = Vector3.Angle(Vector3.up, slopeNormal) > 1f; // Threshold for being "on a slope"
        
        // Debug rays for wheel ground checks
        Debug.DrawRay(frontWheelCheck.position, Vector3.down * 1f, frontGrounded ? Color.green : Color.red);
        Debug.DrawRay(rearWheelCheck.position, Vector3.down * 1f, rearGrounded ? Color.green : Color.red);
    }


    private readonly float log180 = Mathf.Log(180);

    // --- Input Handling and Speed Calculation ---
    // ReSharper disable Unity.PerformanceAnalysis
    void HandleMovementInput()
    {
        
        player = GameObject.FindWithTag("Player").transform;
        if (player == null) Debug.LogError("no player assigned");
        
        Vector3 target = GetPositionOnCircle(player.position, player.eulerAngles.y);
        
        float signedangle = CalculAngletoCible(transform, target);
        
        
        // Steering
        float speedFactor = Mathf.InverseLerp(minSpeed, maxSpeed, currentMoveSpeed);
        float adjustedSteerSpeed = baseSteerSpeed * (1f - speedFactor * steerSpeedReductionFactor);


        // Handle Detection of Player
        currentSteerInput = 0;
        if (canfollowthetarget)
        {
            if (signedangle < 0)
            {
                currentSteerInput = -Mathf.Log(-signedangle + 1) / log180;
            }

            if (signedangle > 0)
            {
                currentSteerInput = Mathf.Log(signedangle + 1) / log180;
            }
        }

        bool jump = false;
        
        // Handle Detection of Obstacle and Void
        if (candetecteobjectandvoid)
        {
            RaycastHit obstacleHitL = default;
            RaycastHit obstacleHitR = default;

            bool obstacleL = false;
            bool obstacleR = false;
            bool voidL = true;
            bool voidR = true;

            float i = 75;

            while (i > 10 && !obstacleL && !obstacleR && voidL && voidR)
            {
                // Rotate enemy's forward vector to left and right
                Vector3 dirLeft = Quaternion.AngleAxis(-i, Vector3.up) * enemy.transform.forward;
                Vector3 dirRight = Quaternion.AngleAxis(i, Vector3.up) * enemy.transform.forward;

                // Obstacle raycasts (horizontal)
                obstacleL |= Physics.Raycast(enemy.transform.position, dirLeft, out obstacleHitL, 1.5f);
                obstacleR |= Physics.Raycast(enemy.transform.position, dirRight, out obstacleHitR, 1.5f);

                // Calculate ground check origins 2.5 units ahead
                Vector3 leftCheckPos = enemy.transform.position + dirLeft * 2.5f;
                Vector3 rightCheckPos = enemy.transform.position + dirRight * 2.5f;

                // Void detection (vertical downward)
                bool leftGround = Physics.Raycast(leftCheckPos, Vector3.down, 2f, groundLayer);
                bool rightGround = Physics.Raycast(rightCheckPos, Vector3.down, 2f, groundLayer);

                voidL &= leftGround;
                voidR &= rightGround;
        
                // Debug rays
                if (showdetection)
                {
                    Debug.DrawRay(enemy.transform.position, dirLeft * 2.5f, Color.red);
                    Debug.DrawRay(enemy.transform.position, dirRight * 2.5f, Color.green);
                    Debug.DrawRay(leftCheckPos, Vector3.down * 2f, Color.black);
                    Debug.DrawRay(rightCheckPos, Vector3.down * 2f, Color.black);
                }

                i--;
            }

            // Decision logic
            if (!voidR || (obstacleHitL.distance < obstacleHitR.distance))
            {
                if (canJump && obstacleHitL.distance < 1f)
                {
                    currentSteerInput = 1; // steer right
                    jump = true;
                }
                else
                {
                    currentSteerInput = -1; // steer left
                }
            }
            else if (!voidL || (obstacleHitR.distance < obstacleHitL.distance))
            {
                if (canJump && obstacleHitR.distance < 1f)
                {
                    currentSteerInput = -1; // steer left
                    jump = true;
                }
                else
                {
                    currentSteerInput = 1; // steer right
                }
            }
        }

        
        // Handle Jumping
        if (canJump)
        {
            if (jump && isGroundedStatus)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        
        transform.Rotate(Vector3.up * currentSteerInput * adjustedSteerSpeed * Time.deltaTime);
        
        float distance = Vector3.Distance(target, enemy.transform.position);
        float accelerationInput = 0;

        // Handle Acceleration
        if (canaccelerate)
        {
            accelerationInput += 1;
            
            if ( distance < maxdistanceofdash && distance > mindistanceofdash) accelerationInput -= 1;

        }

        // Handle Braking
        if (canslowdown)
        {
            bool obstacles = Physics.Raycast(transform.position, GetPositionOnCircle(transform.position, transform.eulerAngles.y, 1), out RaycastHit _, 5);
            // If there is an Obstacle in Front Slowdown the moto
            if (isBraking || (distance < maxdistanceofdash && distance > mindistanceofdash) || obstacles || Mathf.Abs(signedangle) > angleToSlowDown)
            {
                accelerationInput -= 1;
            }
        }
        
        float speedMultiplier = 1f;
        if (isOnSlope && isGroundedStatus) // Apply speed multiplier only if grounded on a slope
        {
            // Check dot product of forward direction with world down, projected onto slope plane might be more robust
            // For simplicity, checking if player is generally pointing downhill/uphill relative to world up.
            // A more accurate check would involve projecting transform.forward onto the slope plane and then checking its Y component.
            if (Vector3.Dot(transform.forward, Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized) > 0.1f) // Simplified check for downhill
            {
                speedMultiplier = downhillSpeedMultiplier;
            }
            else if (Vector3.Dot(transform.forward, Vector3.ProjectOnPlane(Vector3.up, slopeNormal).normalized) > 0.1f) // Simplified check for uphill
            {
                speedMultiplier = uphillSpeedMultiplier;
            }
        }

        if (accelerationInput > 0) // Accelerating
        {
            isBraking = false;
            currentDeceleration = progressiveDecelerationRate; // Reset deceleration when accelerating
            currentMoveSpeed += acceleration * speedMultiplier * Time.deltaTime;
            currentMoveSpeed = Mathf.Clamp(currentMoveSpeed, minSpeed, maxSpeed);
        }
        else if (accelerationInput == 0) // No throttle input (coasting/natural deceleration)
        {
            if (currentMoveSpeed > minSpeed)
            {
                isBraking = false; // Not actively braking, but decelerating
                currentMoveSpeed -= currentDeceleration * Time.deltaTime;
                currentMoveSpeed = Mathf.Max(currentMoveSpeed, minSpeed);
                currentDeceleration += progressiveDecelerationRate * Time.deltaTime; // Deceleration increases over time
            }
            else
            {
                isBraking = false;
                currentMoveSpeed = minSpeed;
                currentDeceleration = progressiveDecelerationRate; // Reset deceleration
            }
        }
        else // Braking/Reversing (accelerationInput < 0)
        {
            isBraking = true;
            // Using brakingDeceleration for more responsive braking.
            // If you want reverse, you'd handle negative currentMoveSpeed or a separate reverse gear.
            currentMoveSpeed += accelerationInput * brakingDeceleration * Time.deltaTime; // Effectively decelerates
            currentMoveSpeed = Mathf.Clamp(currentMoveSpeed, minSpeed, maxSpeed); // Assuming minSpeed is the lowest forward speed
            currentDeceleration = progressiveDecelerationRate; // Reset progressive deceleration
        }
    }
    
    private float CalculAngletoCible(Transform ai, Vector3 playerdirection)
    {
        // Direction of the AI
        Vector3 directionAI = ai.forward;

        // Direction to the player
        Vector3 directiontoPlayer = (playerdirection - ai.position).normalized;

        // Calcul of the signed angle around the axis Y
        float angle = Vector3.SignedAngle(directionAI, directiontoPlayer, Vector3.up);

        return angle;
    }
    
    Vector3 GetPositionOnCircle(Vector3 origin, float angleDeg, float radius = 3)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float x = Mathf.Sin(angleRad) * radius;
        float z = Mathf.Cos(angleRad) * radius;
        return origin + new Vector3(x, 0, z);
    }
    
   // --- Apply Gravity ---
    void ApplyGravity()
    {
        if (isGroundedStatus && velocity.y < 0)
        {
            // Apply a small downward force to stick to slopes better, CharacterController specific behavior
            velocity.y = -5f; // Adjust this value as needed, -2f to -5f is common
        }
        else
        {
            velocity.y += gravity * Time.deltaTime; // Apply gravity
        }
    }

    // --- Apply Leaning Visual ---
    void HandleLeaning()
    {
        if (leanTarget is null) return;

        // --- Z-axis Lean (Steering + Speed) ---
        float targetSteerLeanAngleZ = -currentSteerInput * maxLeanAngle;
        Vector3 lateralVelocity = Vector3.ProjectOnPlane(enemy.velocity, transform.forward); // Use CharacterController's velocity
        float lateralSpeed = lateralVelocity.magnitude;
        float speedFactorForLean = Mathf.InverseLerp(minSpeed, maxSpeed * 0.8f, currentMoveSpeed);
        float speedLeanInfluence = Mathf.Sign(-currentSteerInput) * lateralSpeed * 1.0f * speedFactorForLean; // Adjust 1.0f multiplier
        targetSteerLeanAngleZ += speedLeanInfluence;
        targetSteerLeanAngleZ = Mathf.Clamp(targetSteerLeanAngleZ, -maxLeanAngle * 1.5f, maxLeanAngle * 1.5f);
        currentLeanAngleZ = Mathf.LerpAngle(currentLeanAngleZ, targetSteerLeanAngleZ, leanSpeed * Time.deltaTime);

        // --- X-axis Lean (Slope/Side Correction) ---
        float targetSlopeLeanAngleX = 0f; // Default to trying to be upright

        if (isGroundedStatus) // Only apply active leaning/correction if grounded based on wheel checks
        {
            // Primary determination of slope based on a central raycast under the main body/pivot
            Vector3 groundNormalForLean = storedSlopeNormal; // Fallback to wheel-derived normal
            bool centralRayHit = Physics.Raycast(transform.position + transform.up * 0.1f, // Start ray slightly above pivot
                                                 Vector3.down, out RaycastHit centralHit,
                                                 chassisCentralCheckMaxDistance,
                                                 groundLayer, QueryTriggerInteraction.Ignore);
            if (centralRayHit)
            {
                groundNormalForLean = centralHit.normal; // Use hit normal if central ray connects
            }
            // Debug central lean ray
            Debug.DrawRay(transform.position + transform.up * 0.1f, Vector3.down * chassisCentralCheckMaxDistance, centralRayHit ? Color.yellow : Color.white);


            float centralBodySlopeAngle = Vector3.Angle(Vector3.up, groundNormalForLean);

            if (centralBodySlopeAngle > 1.5f) // If the ground under the body center is sloped (threshold of 1.5 degrees)
            {
                // Calculate target lean based on this central slope.
                // This makes the chassis try to align its 'up' vector perpendicular to the groundNormalForLean.
                targetSlopeLeanAngleX = Vector3.SignedAngle(transform.up, groundNormalForLean, transform.forward) * slopeLeanSensitivity;
            }
            // else, targetSlopeLeanAngleX remains 0 from initialization (try to be upright based on central check)


            // --- Correction using Side Chassis Raycasts ---
            if (leftChassisGroundCheck != null && rightChassisGroundCheck != null) // Ensure transforms are assigned
            {
                bool leftChassisGrounded = Physics.Raycast(leftChassisGroundCheck.position, Vector3.down, out RaycastHit leftChassisHitInfo, chassisSideCheckMaxDistance, groundLayer);
                bool rightChassisGrounded = Physics.Raycast(rightChassisGroundCheck.position, Vector3.down, out RaycastHit rightChassisHitInfo, chassisSideCheckMaxDistance, groundLayer);

                Debug.DrawRay(leftChassisGroundCheck.position, Vector3.down * chassisSideCheckMaxDistance, leftChassisGrounded ? Color.cyan : Color.magenta);
                Debug.DrawRay(rightChassisGroundCheck.position, Vector3.down * chassisSideCheckMaxDistance, rightChassisGrounded ? Color.cyan : Color.magenta);

                // Get overall terrain flatness based on wheel contacts (storedSlopeNormal)
                float terrainSlopeAngleFromWheels = Vector3.Angle(Vector3.up, storedSlopeNormal);

                if (terrainSlopeAngleFromWheels < 5.0f) // If the general terrain (from wheels) is considered flat (e.g., less than 5 degrees)
                {
                    bool leftSideOnFlat = leftChassisGrounded && Vector3.Angle(Vector3.up, leftChassisHitInfo.normal) < 5.0f;
                    bool rightSideOnFlat = rightChassisGrounded && Vector3.Angle(Vector3.up, rightChassisHitInfo.normal) < 5.0f;

                    if (leftSideOnFlat && rightSideOnFlat)
                    {
                        targetSlopeLeanAngleX = 0f; // Both chassis sides on flat ground, force upright. This is key for reset.
                    }
                    // If one side is hanging (e.g., off a ledge) but the other is on flat ground,
                    // and the bike is leaned towards the hanging side, encourage it to level out.
                    else if (leftSideOnFlat && !rightChassisGrounded && currentLeanAngleX > 1.0f) // Leaned right, but right side has no ground, left is flat
                    {
                        // Encourage leveling if the supporting (left) side is flat.
                        // Lerp towards 0 or a smaller lean angle.
                        targetSlopeLeanAngleX = Mathf.Lerp(targetSlopeLeanAngleX, 0f, 0.1f); // Gently try to correct
                    }
                    else if (rightSideOnFlat && !leftChassisGrounded && currentLeanAngleX < -1.0f) // Leaned left, but left side has no ground, right is flat
                    {
                        targetSlopeLeanAngleX = Mathf.Lerp(targetSlopeLeanAngleX, 0f, 0.1f); // Gently try to correct
                    }
                }
            }
            // Clamp the final target lean angle for X-axis
            targetSlopeLeanAngleX = Mathf.Clamp(targetSlopeLeanAngleX, -maxSlopeLeanAngle, maxSlopeLeanAngle);
        }
        // If not isGroundedStatus (in air), targetSlopeLeanAngleX remains 0 from its initialization.
        // This means in air, the bike will try to self-right its X-axis lean naturally via the Lerp.

        currentLeanAngleX = Mathf.LerpAngle(currentLeanAngleX, targetSlopeLeanAngleX, leanSpeed * Time.deltaTime);

        // Apply the combined lean rotation to the lean target
        leanTarget.localRotation = Quaternion.Euler(currentLeanAngleX, 0f, currentLeanAngleZ);
    }


    // --- Apply Final Movement to CharacterController ---
    void ApplyMovement()
    {
        Vector3 forwardDirection = transform.forward; // Player's current forward direction
        Vector3 moveDirectionOnSlope = forwardDirection; // Default to player's forward

        if (isOnSlope && isGroundedStatus)
        {
            // Project the forward vector onto the slope plane
            moveDirectionOnSlope = Vector3.ProjectOnPlane(forwardDirection, slopeNormal).normalized;

            // Align player rotation smoothly to the slope normal for the 'up' vector
            Quaternion targetRotation = Quaternion.LookRotation(moveDirectionOnSlope, slopeNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f); // Adjust 10f for alignment speed
        }
        // If not on a slope but grounded, ensure the player is upright (or lerp to upright if you want smooth transition from slope)
        else if (isGroundedStatus)
        {
            Quaternion targetRotation = Quaternion.LookRotation(transform.forward, Vector3.up); // Standard upright rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }


        // Construct the final movement vector
        Vector3 moveVector = moveDirectionOnSlope * currentMoveSpeed; // Horizontal movement
        moveVector.y = velocity.y; // Vertical movement (gravity, jump)

        if(!isDead && enabled)enemy.Move(moveVector * Time.deltaTime); // Apply movement via CharacterController
        
    }


    // --- Wheel Rotation ---
    void UpdateWheelRotation()
    {
        float rotationSpeed = currentMoveSpeed * wheelRotationMultiplier;
        float rollDelta = rotationSpeed * Time.deltaTime;

        if (frontWheel != null)
        {
            float targetSteerAngle = currentSteerInput * 30f; // Max visual steer angle for front wheel
            frontWheelRollAngle -= rollDelta; // Assuming negative roll for forward movement
            frontWheelRollAngle %= 360f;
            frontWheel.localRotation = Quaternion.Euler(frontWheelRollAngle, targetSteerAngle, 0f);
        }

        if (rearWheel != null)
        {
            rearWheelRollAngle -= rollDelta;
            rearWheelRollAngle %= 360f;
            rearWheel.localRotation = Quaternion.Euler(rearWheelRollAngle, 0f, 0f);
        }
    }

    // --- Trail System Logic ---
    void UpdateTrailSystem()
    {
        RecordTrailPosition();
        RemoveOldTrailPoints();
        UpdateColliderObjects();
    }

    void RecordTrailPosition()
    {
        Vector3 spawnPosition = transform.position - (transform.forward * positionOffset);
        if (trailPoints.Count == 0 || Vector3.Distance(trailPoints[^1].WorldPosition, spawnPosition) >= colliderSpacing)
        {
            trailPoints.Add(new TrailPoint { WorldPosition = spawnPosition, Timestamp = Time.time });
        }
    }

    void RemoveOldTrailPoints()
    {
        float cutoffTime = Time.time - trailLifetime;
        int removeCount = 0;
        while (removeCount < trailPoints.Count && trailPoints[removeCount].Timestamp < cutoffTime)
        {
            removeCount++;
        }

        if (removeCount > 0)
        {
            for (int i = 0; i < removeCount && trailColliderObjects.Count > 0; i++) // Ensure trailColliderObjects is not empty
            {
                if (trailColliderObjects[0] != null) Destroy(trailColliderObjects[0]);
                trailColliderObjects.RemoveAt(0);
            }
            // If more points expired than colliders (should be rare with current logic but good to be safe)
            if (trailPoints.Count < removeCount) removeCount = trailPoints.Count;

            trailPoints.RemoveRange(0, removeCount);
        }
    }

    void UpdateColliderObjects()
    {
        int requiredColliders = Mathf.Max(0, trailPoints.Count - 1);

        while (trailColliderObjects.Count < requiredColliders) { CreateColliderObject(); }
        while (trailColliderObjects.Count > requiredColliders) { RemoveLastColliderObject(); }

        for (int i = 0; i < trailColliderObjects.Count; i++)
        {
            if (i < trailPoints.Count - 1)
            {
                UpdateSingleColliderObject(trailColliderObjects[i], trailPoints[i], trailPoints[i + 1]);
            }
            else
            {
                if (trailColliderObjects[i].activeSelf) trailColliderObjects[i].SetActive(false);
            }
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    void CreateColliderObject()
    {
        GameObject colliderObj = new GameObject($"TrailColliderSegment_{trailColliderObjects.Count}")
        {
            tag = trailColliderTag
        };

        CapsuleCollider capsule = colliderObj.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.direction = 2; // Z-axis aligned

        Rigidbody rb = colliderObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        TrailSegmentCollider segmentHelper = colliderObj.AddComponent<TrailSegmentCollider>();
        segmentHelper.Initialize(collisionActivationDelay); // Collider initially disabled by helper

        LineRenderer line = colliderObj.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = segmentLineWidth;
        line.endWidth = segmentLineWidth;
        line.numCapVertices = 4;
        line.sortingOrder = 0;
        if (segmentLineMaterial != null) line.material = segmentLineMaterial;

        trailColliderObjects.Add(colliderObj);
    }

    void RemoveLastColliderObject()
    {
        if (trailColliderObjects.Count == 0) return;
        int lastIndex = trailColliderObjects.Count - 1;
        GameObject toRemove = trailColliderObjects[lastIndex];
        trailColliderObjects.RemoveAt(lastIndex);
        if (toRemove != null) Destroy(toRemove);
    }

    void UpdateSingleColliderObject(GameObject colliderObj, TrailPoint startPoint, TrailPoint endPoint)
    {
        CapsuleCollider capsule = colliderObj.GetComponent<CapsuleCollider>();
        LineRenderer line = colliderObj.GetComponent<LineRenderer>();
        if (capsule == null || line == null)
        {
            Debug.LogError("Missing components on trail segment!", colliderObj);
            return;
        }

        Vector3 startPos = startPoint.WorldPosition;
        Vector3 endPos = endPoint.WorldPosition;
        Vector3 segmentVector = endPos - startPos;
        float segmentLength = segmentVector.magnitude;

        colliderObj.transform.position = (startPos + endPos) / 2f;
        if (segmentLength > 0.001f)
        {
            colliderObj.transform.rotation = Quaternion.LookRotation(segmentVector.normalized);
        }

        capsule.radius = segmentLineWidth / 2f;
        capsule.height = segmentLength + (capsule.radius * 2f); // Account for caps

        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
    }

    void UpdatePlayerToColliderLine()
    {
        if (playerToColliderLineRenderer == null) return;
        if (trailPoints.Count > 0)
        {
            playerToColliderLineRenderer.enabled = true;
            playerToColliderLineRenderer.SetPosition(0, transform.position); // Current player position
            playerToColliderLineRenderer.SetPosition(1, trailPoints[^1].WorldPosition); // Last trail point
        }
        else
        {
            playerToColliderLineRenderer.enabled = false;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (isDead) return; // Already dead, do nothing

        // Check for collision with own trail OR hazard tag OR out of bounds
        bool isHazardCollision = other.gameObject.CompareTag(hazardTag) && currentMoveSpeed > 1f; // Only die from hazard if moving
        if (other.gameObject.CompareTag(trailColliderTag) || isHazardCollision || other.gameObject.CompareTag("plane") || transform.position.y < -10)
        {
            TriggerDeathSequence();
            // No need to call StartCoroutine(DelayedRespawn()) here, TriggerDeathSequence handles disabling
            // and Update will call RespawnPlayer after delay if this script is re-enabled by RespawnPlayer.
            // The respawn delay is handled by checking Time.time in the main Update loop of the *next* enabled frame.
            // For this to work, RespawnPlayer must re-enable this script.
        }
    }

    // --- Death Logic ---
    void TriggerDeathSequence()
    {
        if (isDead) return; // Prevent multiple triggers
        isDead = true;
        
        if (orangeEffect != null) orangeEffect.Play();
        if (darkOrangeEffect != null) darkOrangeEffect.Play();
        if (blackEffect != null) blackEffect.Play();

        if (enemy != null) enemy.enabled = false; // Disable CharacterController
        // Important: Disable this script. RespawnPlayer will re-enable it.
        // This prevents Update from running while "dead" other than the respawn check.
        // However, the respawn check needs to be moved if the script is disabled.
        // Let's keep it enabled but use the isDead flag to gate most of Update.
        // this.enabled = false; // Reconsidering this line.
        // If this script is disabled, the Update loop won't run to check for respawn.
        // So, keep script enabled, and Update checks `isDead`.
        spawnpoint.unitalive--;
        ClearTrail();
        Invoke(nameof(Destroy), 0.5f);
    }

    void Destroy()
    {
        Destroy(gameObject);
    }
    
    void ClearTrail()
    {
        if (playerToColliderLineObject != null) Destroy(playerToColliderLineObject);
        foreach (GameObject colliderObj in trailColliderObjects)
        {
            if (colliderObj != null) Destroy(colliderObj);
        }
        trailColliderObjects.Clear();
        trailPoints.Clear();
        if (playerToColliderLineRenderer != null)
            playerToColliderLineRenderer.enabled = false;
    }
    
    // Helper Script for Trail Collider Activation Delay
    public class TrailSegmentCollider : MonoBehaviour
    {
        private float creationTime;
        private float activationDelay;
        private CapsuleCollider capsuleCollider;
        private bool isInitialized;

        // ReSharper disable Unity.PerformanceAnalysis
        public void Initialize(float delay)
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider == null)
            {
                Debug.LogError("TrailSegmentCollider requires a CapsuleCollider component!", this);
                enabled = false; // Disable this script if no collider
                return;
            }
            creationTime = Time.time;
            activationDelay = delay;
            capsuleCollider.enabled = false; // Initially disable the collider
            isInitialized = true;
        }

        void Update()
        {
            if (!isInitialized || capsuleCollider is null) return;

            // If collider is still disabled and activation time has passed
            if (!capsuleCollider.enabled)
            {
                if (Time.time >= creationTime + activationDelay)
                {
                    capsuleCollider.enabled = true; // Enable the collider
                }
            }
        }
    }
}
