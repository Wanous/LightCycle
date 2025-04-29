using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements like RectTransform

// Require necessary components to ensure they exist on the GameObject
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // --- Player Components ---
    [Header("Components")]
    public CharacterController player;
    [Tooltip("Assign a child GameObject positioned at the player's base for ground checks")]
    public Transform groundCheckPoint;
    [Tooltip("Material to use for the line segments between colliders")]
    public Material segmentLineMaterial;

    // --- Movement Parameters ---
    [Header("Movement")]
    public float minSpeed = 2f;
    public float maxSpeed = 30f;
    public float acceleration = 3f;
    public float brakingDeceleration = 15f;
    public float progressiveDecelerationRate = 0.2f; // Reduced for slower deceleration increase
    private float currentDeceleration;
    [Tooltip("Base steer speed at minimum speed")]
    public float baseSteerSpeed = 120f;
    [Tooltip("Factor to reduce steer speed as speed increases (0 for no reduction, higher values for more reduction)")]
    [Range(0f, 1f)] public float steerSpeedReductionFactor = 0.5f;
    public float gravity = -19.62f;
    public float jumpHeight = 1.5f;
    public bool canJump = true;

    // --- Leaning Parameters ---
    [Header("Leaning")]
    [Tooltip("The Transform to apply lean rotation to (e.g., the bike chassis). If null, leans the root object.")]
    public Transform leanTarget;
    [Tooltip("Maximum lean angle in degrees when turning")]
    public float maxLeanAngle = 20f;
    [Tooltip("How quickly the player leans into/out of turns")]
    public float leanSpeed = 4f;
    [Tooltip("Maximum lean angle in degrees when on a slope")]
    public float maxSlopeLeanAngle = 10f;
    [Tooltip("How much the slope affects leaning (0-1)")]
    [Range(0f, 1f)] public float slopeLeanSensitivity = 0.5f;

    // --- Trail Collision Parameters ---
    [Header("Trail Collision")]
    public float trailLifetime = 4.2f;
    public float colliderSpacing = 1.0f;
    public float positionOffset = 1.0f;
    [Tooltip("Delay in seconds before a trail collider becomes active to prevent self-collision.")]
    public float collisionActivationDelay = 0.5f; // Added tooltip for clarity
    public float segmentLineWidth = 0.2f;
    public float playerToColliderLineWidth = 0.3f;
    [Tooltip("The tag to apply to the trail collider objects")]
    public string trailColliderTag = "Trail"; // Public variable for the tag
    [Tooltip("The tag of the colliders that will cause death")]
    public string hazardTag = "Hazard"; // Tag for other hazards

    // --- Ground Check Parameters ---
    [Header("Ground Check")]
    [Tooltip("The physics layer(s) considered 'ground'")]
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

	[Header("Camera")]
	public Camera Cam;
	private float baseFOV = 55f;
	private float maxFOV = 90f;
	private float smoothSpeed = 5f;

    // --- Effects ---
    [Header("Effects")]
    [SerializeField] ParticleSystem OrangeEffect;
    [SerializeField] ParticleSystem darkOrangeEffect;
    [SerializeField] ParticleSystem BlackEffect;

    // --- Wheel Rotation ---
    [Header("Wheel Rotation")]
    public Transform frontWheel;
    public Transform rearWheel;
    public float wheelRotationMultiplier = 40f;

    // --- Speedometer Parameters ---
    [Header("Speedometer")]
    public RectTransform speedometerNeedle; // Assign the needle's RectTransform in the Inspector
    public float minSpeedForNeedle = 0f;       // Minimum speed for the needle's range
    public float maxSpeedForNeedle = 30f;       // Maximum speed for the needle's range
    public float minNeedleAngle = 0f;            // Angle of the needle at minSpeed
    public float maxNeedleAngle = -270f;        // Angle of the needle at maxSpeed (adjust as needed)

    // --- Respawn Parameters ---
    [Header("Respawn")]
    [Tooltip("A list of Transform objects representing the possible spawn points.")]
    public List<Transform> spawnPoints;
    [Tooltip("The time delay before the player respawns after dying.")]
    public float respawnDelay = 2.0f; // Added respawn delay
    private int currentSpawnPointIndex = 0; // Index of the current spawn point

    // --- Private Variables ---
    private float currentMoveSpeed;
    private Vector3 velocity;
    private bool isGroundedStatus = false;
    private bool isDead = false;
    private GameObject playerToColliderLineObject;
    private LineRenderer playerToColliderLineRenderer;
    private float frontWheelRollAngle = 0f;
    private float rearWheelRollAngle = 0f;
    private float currentSteerInput = 0f;
    private float currentLeanAngleZ = 0f; // Separate variable for Z-axis lean
    private float currentLeanAngleX = 0f; // Separate variable for X-axis lean
    private bool isBraking = false;
    private float deathTime; // To store the time of death for respawn delay

    // --- Trail Data Structures ---
    private class TrailPoint
    {
        public Vector3 WorldPosition;
        public float Timestamp;
    }
    private List<TrailPoint> trailPoints = new List<TrailPoint>();
    private List<GameObject> trailColliderObjects = new List<GameObject>();

    // --- Initialization ---
    void Start()
    {
        if (player == null) player = GetComponent<CharacterController>();

        // Null Checks...
        if (player == null) { Debug.LogError("CharacterController missing!", this); this.enabled = false; return; }
        if (groundCheckPoint == null) Debug.LogWarning("Ground Check Point not assigned.", this);
        if (segmentLineMaterial == null) Debug.LogWarning("Segment Line Material not assigned.", this);
        if (frontWheel == null) Debug.LogWarning("Front Wheel not assigned.", this);
        if (rearWheel == null) Debug.LogWarning("Rear Wheel not assigned.", this);
        if (leanTarget == null) { Debug.LogWarning("Lean Target not assigned. Leaning root object.", this); leanTarget = this.transform; }
        if (groundLayer.value == 0) Debug.LogError("Ground Layer not set!", this);
        if (speedometerNeedle == null) Debug.LogWarning("Speedometer Needle not assigned.", this); // Check for speedometer needle!

        // --- Find Spawn Points ---
        spawnPoints = new List<Transform>(); // Initialize the list
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint"); // Find all GameObjects with the tag
        if (spawnPointObjects.Length == 0)
        {
            Debug.LogError("No Spawn Points found in the scene!  Please create GameObjects with the tag 'SpawnPoint'.", this);
            this.enabled = false; // Disable the script to prevent errors
            return;
        }
        else
        {
            // Add the transforms of the found GameObjects to the list
            foreach (GameObject spawnPointObject in spawnPointObjects)
            {
                spawnPoints.Add(spawnPointObject.transform);
            }
            // Initialize the player's position to the first spawn point
            transform.position = spawnPoints[0].position;
            currentSpawnPointIndex = 0; // Initialize spawn point index
        }

        currentMoveSpeed = minSpeed;
        currentDeceleration = brakingDeceleration; // Initialize deceleration
        // Setup player-to-collider line renderer...
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
        if (isDead)
        {
            // Check if it's time to respawn
            if (Time.time >= deathTime + respawnDelay)
            {
                RespawnPlayer();
            }
            return; // Exit Update() if dead
        }

        HandleGroundCheck();
        HandleMovementInput();
        ApplyGravity();
        HandleLeaning();
        ApplyMovement();
        UpdateWheelRotation();
        UpdateTrailSystem();
        UpdatePlayerToColliderLine();
        UpdateSpeedometerNeedle(); // Update the speedometer needle!
    }

    // --- Ground Check Logic ---
    void HandleGroundCheck()
    {
        if (groundCheckPoint != null)
        {
            isGroundedStatus = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
        }
        else
        {
            // If no ground check point, assume not grounded for safety? Or default to true if preferred?
            // Defaulting to false seems safer to prevent unintended jumps/logic.
            isGroundedStatus = false;
            // Consider adding a Debug.LogWarning once in Start if groundCheckPoint is null
        }
    }

    // --- Input Handling and Speed Calculation ---
    void HandleMovementInput()
    {
        float accelerationInput = Input.GetAxis("Vertical"); // Typically -1 to 1

        if (accelerationInput > 0) // Accelerating
        {
            isBraking = false;
            currentDeceleration = brakingDeceleration; // Reset progressive deceleration
            currentMoveSpeed += accelerationInput * acceleration * Time.deltaTime;
            currentMoveSpeed = Mathf.Clamp(currentMoveSpeed, minSpeed, maxSpeed);
        }
        else if (accelerationInput == 0) // No input (coasting/natural deceleration)
        {
            // Only apply progressive deceleration if moving faster than minSpeed
            if (currentMoveSpeed > minSpeed)
            {
                isBraking = true; // Consider if coasting should count as 'braking' for other logic
                currentMoveSpeed -= currentDeceleration * Time.deltaTime;
                currentMoveSpeed = Mathf.Max(currentMoveSpeed, minSpeed); // Prevent going below min speed
                currentDeceleration += progressiveDecelerationRate * Time.deltaTime; // Increase deceleration rate
            }
            else
            {
                isBraking = false; // Not actively braking if at min speed
                currentMoveSpeed = minSpeed;
                currentDeceleration = brakingDeceleration; // Reset deceleration when stopped/at min
            }
        }
        else // Braking/Reversing (accelerationInput < 0)
        {
            isBraking = true;
            // Apply braking force; negative accelerationInput makes the result negative
            currentMoveSpeed += accelerationInput * brakingDeceleration * Time.deltaTime;
            currentMoveSpeed = Mathf.Clamp(currentMoveSpeed, minSpeed, maxSpeed); // Clamp ensures it doesn't go below minSpeed due to braking
            // Reset progressive deceleration since we are actively braking
            currentDeceleration = brakingDeceleration;
        }

        // Calculate the adjusted steer speed based on current speed
        float speedFactor = Mathf.InverseLerp(minSpeed, maxSpeed, currentMoveSpeed);
        float adjustedSteerSpeed = baseSteerSpeed * (1f - (speedFactor * steerSpeedReductionFactor));

        currentSteerInput = Input.GetAxis("Horizontal"); // Typically -1 to 1
        // Apply rotation based on input and adjusted steer speed
        transform.Rotate(Vector3.up * currentSteerInput * adjustedSteerSpeed * Time.deltaTime);

        // Handle Jumping
        if (canJump && Input.GetButtonDown("Jump") && isGroundedStatus)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    // --- Apply Gravity ---
    void ApplyGravity()
    {
        // Reset vertical velocity slightly below zero when grounded to help stick to slopes
        if (isGroundedStatus && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value helps ensure CheckSphere stays grounded
        }
        // Apply gravity constantly
        velocity.y += gravity * Time.deltaTime;
    }

    // --- Apply Leaning Visual ---
    void HandleLeaning()
    {
        if (leanTarget == null) return;

        // --- Z-axis Lean (Steering) ---
        // Target lean angle based on steering input
        float targetSteerLeanAngleZ = -currentSteerInput * maxLeanAngle;
        // Smoothly interpolate towards the target lean angle
        currentLeanAngleZ = Mathf.LerpAngle(currentLeanAngleZ, targetSteerLeanAngleZ, leanSpeed * Time.deltaTime);

        // --- X-axis Lean (Slope) ---
        float targetSlopeLeanAngleX = 0f;
        // Only apply slope lean if grounded and check point exists
        if (isGroundedStatus && groundCheckPoint != null)
        {
            // Raycast down to find the ground normal
            if (Physics.Raycast(groundCheckPoint.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, groundCheckRadius * 2f + 0.1f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                Vector3 groundNormal = hit.normal;
                // Calculate the slope angle relative to the world up direction
                float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);

                if (slopeAngle > 1f) // Only calculate lean if there's a noticeable slope
                {
                    // Get the direction of the slope perpendicular to the player's forward direction
                    Vector3 slopeRight = Vector3.Cross(groundNormal, transform.forward).normalized;
                    // Calculate the lean angle based on how much the player's right vector aligns with the slope's right direction
                    targetSlopeLeanAngleX = -Vector3.SignedAngle(transform.right, slopeRight, transform.forward);
                    // Apply sensitivity and clamp to max slope lean angle
                    targetSlopeLeanAngleX *= slopeLeanSensitivity;
                    targetSlopeLeanAngleX = Mathf.Clamp(targetSlopeLeanAngleX, -maxSlopeLeanAngle, maxSlopeLeanAngle);
                }
            }
        }
        // Smoothly interpolate towards the target slope lean angle
        currentLeanAngleX = Mathf.LerpAngle(currentLeanAngleX, targetSlopeLeanAngleX, leanSpeed * Time.deltaTime);


        // Apply the combined lean rotation to the lean target
        // Make sure this applies relative to the parent's rotation if leanTarget is a child
        leanTarget.localRotation = Quaternion.Euler(currentLeanAngleX, 0f, currentLeanAngleZ);
    }

    // --- Apply Final Movement to CharacterController ---
    void ApplyMovement()
    {
        // Calculate movement direction based on player's forward direction and current speed
        Vector3 moveDirection = transform.forward * currentMoveSpeed;
        // Include the vertical velocity (gravity/jumping)
        moveDirection.y = velocity.y;
        // Apply the movement using CharacterController.Move
        player.Move(moveDirection * Time.deltaTime);
		float targetFOV = Mathf.Lerp(baseFOV, maxFOV, currentMoveSpeed / maxSpeed);
		Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
    }

    // --- Wheel Rotation ---
    void UpdateWheelRotation()
    {
        // Calculate rotation amount based on speed and multiplier
        float rotationSpeed = currentMoveSpeed * wheelRotationMultiplier;
        float rollDelta = rotationSpeed * Time.deltaTime;

        if (frontWheel != null)
        {
            // Calculate steering angle for the front wheel visual
            float targetSteerAngle = currentSteerInput * 30f; // Max visual steer angle
            // Accumulate roll angle
            frontWheelRollAngle -= rollDelta; // Use subtraction if wheels roll "forward" visually as player moves
            frontWheelRollAngle %= 360f; // Keep angle within 0-360
            // Apply roll and steer rotation (Y-axis for steering, X-axis for rolling)
            frontWheel.localRotation = Quaternion.Euler(frontWheelRollAngle, targetSteerAngle, 0f);
        }

        if (rearWheel != null)
        {
            // Accumulate roll angle
            rearWheelRollAngle -= rollDelta;
            rearWheelRollAngle %= 360f;
            // Apply roll rotation (X-axis only)
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
        // Calculate spawn position slightly behind the player based on offset
        Vector3 spawnPosition = transform.position - (transform.forward * positionOffset);
        // Add a new point if list is empty or distance from last point is sufficient
        if (trailPoints.Count == 0 || Vector3.Distance(trailPoints[trailPoints.Count - 1].WorldPosition, spawnPosition) >= colliderSpacing)
        {
            trailPoints.Add(new TrailPoint { WorldPosition = spawnPosition, Timestamp = Time.time });
        }
    }

    void RemoveOldTrailPoints()
    {
        float cutoffTime = Time.time - trailLifetime;
        int removeCount = 0;
        // Find how many points are older than the lifetime
        while (removeCount < trailPoints.Count && trailPoints[removeCount].Timestamp < cutoffTime)
        {
            removeCount++;
        }

        // If points need removal
        if (removeCount > 0)
        {
            // Remove corresponding collider objects first
            for (int i = 0; i < removeCount && i < trailColliderObjects.Count; i++)
            {
                // Safe removal: Check if object exists before destroying
                if (trailColliderObjects[0] != null) Destroy(trailColliderObjects[0]);
                trailColliderObjects.RemoveAt(0);
            }
            // Handle potential mismatch if more points expire than colliders exist (shouldn't normally happen)
            int remainingCollidersToRemove = removeCount - trailColliderObjects.Count;
            if (remainingCollidersToRemove > 0)
            {
                Debug.LogWarning($"Trying to remove {removeCount} points, but only {trailColliderObjects.Count} colliders existed initially.");
                // Ensure we don't try to remove more colliders than exist
                removeCount = trailColliderObjects.Count;
            }


            // Remove the old points from the list
            trailPoints.RemoveRange(0, removeCount);

            // After removal, potentially adjust indices if needed elsewhere (though current logic seems okay)
        }
    }

    void UpdateColliderObjects()
    {
        // We need one collider segment *between* each pair of points
        int requiredColliders = Mathf.Max(0, trailPoints.Count - 1);

        // Add new collider objects if needed
        while (trailColliderObjects.Count < requiredColliders) { CreateColliderObject(); }
        // Remove excess collider objects
        while (trailColliderObjects.Count > requiredColliders) { RemoveLastColliderObject(); }

        // Update existing collider objects
        for (int i = 0; i < trailColliderObjects.Count; i++)
        {
            // Ensure points exist for this segment
            if (i < trailPoints.Count - 1)
            {
                // Update position, rotation, size, and line renderer
                UpdateSingleColliderObject(trailColliderObjects[i], trailPoints[i], trailPoints[i + 1]);
                // Ensure it's active (might have been deactivated if pool was larger before)
                // Activation is now handled by the TrailSegmentCollider script based on delay
                // if (!trailColliderObjects[i].activeSelf) trailColliderObjects[i].SetActive(true);
            }
            else
            {
                // This should ideally not happen if creation/deletion is correct
                // Deactivate object if points don't exist (e.g., pooling scenario)
                Debug.LogWarning($"Collider object at index {i} exists but corresponding points do not.");
                if (trailColliderObjects[i].activeSelf) trailColliderObjects[i].SetActive(false);
            }
        }
    }

    void CreateColliderObject()
    {
        // Create a new GameObject for the segment
        GameObject colliderObj = new GameObject($"TrailColliderSegment_{trailColliderObjects.Count}");
        colliderObj.tag = trailColliderTag; // Assign the specified tag

        // Add Capsule Collider for trigger detection
        CapsuleCollider capsule = colliderObj.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.direction = 2; // Z-axis aligned for Capsule

        // Add Rigidbody (kinematic, no gravity) - required for trigger events with non-kinematic Rigidbody (like player)
        Rigidbody rb = colliderObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // --- IMPORTANT CHANGE: Add and Initialize the TrailSegmentCollider script ---
        // This script manages the activation delay for the collider.
        TrailSegmentCollider segmentHelper = colliderObj.AddComponent<TrailSegmentCollider>();
        segmentHelper.Initialize(collisionActivationDelay);
        // The capsule collider is initially disabled by the Initialize method.
        // --- End of IMPORTANT CHANGE ---


        // Add Line Renderer for visualization
        LineRenderer line = colliderObj.AddComponent<LineRenderer>();
        line.useWorldSpace = true; // Positions are in world space
        line.positionCount = 2; // Start and end point
        line.startWidth = segmentLineWidth;
        line.endWidth = segmentLineWidth;
        line.numCapVertices = 4; // Rounded ends
        line.sortingOrder = 0; // Render order
        if (segmentLineMaterial != null) line.material = segmentLineMaterial;

        // Add to the list
        trailColliderObjects.Add(colliderObj);
    }

    void RemoveLastColliderObject()
    {
        if (trailColliderObjects.Count == 0) return; // Nothing to remove
        int lastIndex = trailColliderObjects.Count - 1;
        GameObject toRemove = trailColliderObjects[lastIndex];
        trailColliderObjects.RemoveAt(lastIndex);
        // Destroy the GameObject safely
        if (toRemove != null) Destroy(toRemove);
    }

    void UpdateSingleColliderObject(GameObject colliderObj, TrailPoint startPoint, TrailPoint endPoint)
    {
        CapsuleCollider capsule = colliderObj.GetComponent<CapsuleCollider>();
        LineRenderer line = colliderObj.GetComponent<LineRenderer>();
        // Failsafe if components somehow missing
        if (capsule == null || line == null)
        {
            Debug.LogError("Missing CapsuleCollider or LineRenderer on trail segment!", colliderObj);
            return;
        }

        Vector3 startPos = startPoint.WorldPosition;
        Vector3 endPos = endPoint.WorldPosition;
        Vector3 segmentVector = endPos - startPos;
        float segmentLength = segmentVector.magnitude;

        // Position the collider object at the midpoint of the segment
        colliderObj.transform.position = (startPos + endPos) / 2f;
        // Rotate the collider object to align with the segment direction
        if (segmentLength > 0.001f) // Avoid issues with zero-length vectors
        {
            colliderObj.transform.rotation = Quaternion.LookRotation(segmentVector.normalized);
        }

        // Update Capsule Collider dimensions
        capsule.radius = segmentLineWidth / 2f; // Radius matches line width
        capsule.height = segmentLength + (capsule.radius * 2f); // Height accounts for rounded caps extending beyond points

        // Update Line Renderer positions
        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
    }

    // --- Update the line between the player and the last recorded trail point ---
    void UpdatePlayerToColliderLine()
    {
        if (playerToColliderLineRenderer == null) return;
        // Only draw if there are points and the line renderer exists
        if (trailPoints.Count > 0)
        {
            playerToColliderLineRenderer.enabled = true;
            // Line from player's current position to the last recorded trail point
            playerToColliderLineRenderer.SetPosition(0, transform.position);
            playerToColliderLineRenderer.SetPosition(1, trailPoints[trailPoints.Count - 1].WorldPosition);
        }
        else
        {
            // Disable if no trail points exist
            playerToColliderLineRenderer.enabled = false;
        }
    }

    // --- Update Speedometer Needle ---
    void UpdateSpeedometerNeedle()
    {
        if (speedometerNeedle == null)
        {
            return;
        }

        float currentSpeed = currentMoveSpeed;
        float normalizedSpeed = Mathf.InverseLerp(minSpeedForNeedle, maxSpeedForNeedle, currentSpeed);
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);
        float targetNeedleAngle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, normalizedSpeed);
        speedometerNeedle.localEulerAngles = new Vector3(0f, 0f, targetNeedleAngle);
    }

    // --- Collision Handling ---
    void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.gameObject.CompareTag(trailColliderTag) || (other.gameObject.CompareTag(hazardTag) && currentMoveSpeed > 15) || other.gameObject.CompareTag("plane") || transform.position.y < -10)
        {
            TriggerDeathSequence();
            StartCoroutine(DelayedRespawn());
        }
    }

    IEnumerator DelayedRespawn()
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnPlayer();
    }

    // --- Death Logic ---
    void TriggerDeathSequence()
    {
        if (isDead) return;
        isDead = true;
        deathTime = Time.time;

        // Play particle effects if assigned
        if (OrangeEffect != null) OrangeEffect.Play();
        if (darkOrangeEffect != null) darkOrangeEffect.Play();
        if (BlackEffect != null) BlackEffect.Play();

        // Disable player control and this script
        if (player != null) player.enabled = false;
        this.enabled = false;
    }

    // --- Respawn Logic ---
    void RespawnPlayer()
    {
        isDead = false;
        if (player != null) player.enabled = true; // Re-enable the CharacterController
        this.enabled = true; // Re-enable this script

        // Choose the next spawn point
        currentSpawnPointIndex = (currentSpawnPointIndex + 1) % spawnPoints.Count;
        transform.position = spawnPoints[currentSpawnPointIndex].position;

        // Reset any other necessary state (e.g., velocity, speed, etc.)
        velocity = Vector3.zero;
        currentMoveSpeed = minSpeed;
        currentDeceleration = brakingDeceleration;
        //Clear the trail
        ClearTrail();
        Debug.Log("Player Respawned!");
    }

    void ClearTrail()
    {
        // Destroy all existing trail collider objects
        foreach (GameObject colliderObj in trailColliderObjects)
        {
            if (colliderObj != null)
            {
                Destroy(colliderObj);
            }
        }
        trailColliderObjects.Clear();
        trailPoints.Clear();
        if (playerToColliderLineRenderer != null)
            playerToColliderLineRenderer.enabled = false;
    }

    // --- Cleanup ---
    void OnDestroy()
    {
        // Clean up created GameObjects to prevent memory leaks in the editor or builds
        if (playerToColliderLineObject != null) Destroy(playerToColliderLineObject);
        foreach (var colliderObj in trailColliderObjects)
        {
            if (colliderObj != null) Destroy(colliderObj);
        }
        // Clear lists
        trailColliderObjects.Clear();
        trailPoints.Clear();
    }

    // --- New Helper Script for Trail Collider Activation Delay ---
    public class TrailSegmentCollider : MonoBehaviour
    {
        private float creationTime;
        private float activationDelay;
        private CapsuleCollider capsuleCollider;

        public void Initialize(float delay)
        {
            creationTime = Time.time;
            activationDelay = delay;
            capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                // Disable the collider immediately upon creation
                capsuleCollider.enabled = false;
            }
            else
            {
                Debug.LogError("TrailSegmentCollider requires a CapsuleCollider component!", this);
                enabled = false; // Disable this script if no collider is found
            }
        }

        void Update()
        {
            // Check if the collider is currently disabled and if enough time has passed
            if (capsuleCollider != null && !capsuleCollider.enabled)
            {
                if (Time.time >= creationTime + activationDelay)
                {
                    // Enable the collider after the delay
                    capsuleCollider.enabled = true;
                }
            }
        }
    }
}
