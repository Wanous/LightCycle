using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public float minSpeed = 5f;
    public float maxSpeed = 50f;
    public float acceleration = 5f;
    [Tooltip("Base steer speed at minimum speed")]
    public float baseSteerSpeed = 180f;
    [Tooltip("Factor to reduce steer speed as speed increases (0 for no reduction, higher values for more reduction)")]
    [Range(0f, 1f)] public float steerSpeedReductionFactor = 0.5f;
    public float gravity = -19.62f;
    public float jumpHeight = 2f;
    public bool canJump = true;

    // --- Leaning Parameters ---
    [Header("Leaning")]
    [Tooltip("The Transform to apply lean rotation to (e.g., the bike chassis). If null, leans the root object.")]
    public Transform leanTarget;
    [Tooltip("Maximum lean angle in degrees when turning")]
    public float maxLeanAngle = 25f;
    [Tooltip("How quickly the player leans into/out of turns")]
    public float leanSpeed = 5f;
    [Tooltip("Maximum lean angle in degrees when on a slope")]
    public float maxSlopeLeanAngle = 15f;
    [Tooltip("How much the slope affects leaning (0-1)")]
    [Range(0f, 1f)] public float slopeLeanSensitivity = 0.5f;

    // --- Trail Collision Parameters ---
    [Header("Trail Collision")]
    public float trailLifetime = 4.2f;
    public float colliderSpacing = 1.0f;
    public float positionOffset = 1.0f;
    public float collisionActivationDelay = 0.5f;
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

    // --- Effects ---
    [Header("Effects")]
    [SerializeField] ParticleSystem OrangeEffect;
    [SerializeField] ParticleSystem darkOrangeEffect;
    [SerializeField] ParticleSystem BlackEffect;

    // --- Wheel Rotation ---
    [Header("Wheel Rotation")]
    public Transform frontWheel;
    public Transform rearWheel;
    public float wheelRotationMultiplier = 50f;


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

        currentMoveSpeed = minSpeed;

        // Setup player-to-collider line renderer...
        playerToColliderLineObject = new GameObject("PlayerToColliderLine");
        playerToColliderLineRenderer = playerToColliderLineObject.AddComponent<LineRenderer>();
        playerToColliderLineRenderer.useWorldSpace = true;
        playerToColliderLineRenderer.positionCount = 2;
        playerToColliderLineRenderer.startWidth = playerToColliderLineWidth;
        playerToColliderLineWidth = playerToColliderLineWidth;
        playerToColliderLineRenderer.sortingOrder = -1;
        if (segmentLineMaterial != null) playerToColliderLineRenderer.material = segmentLineMaterial;
    }

    // --- Frame Update ---
    void Update()
    {
        if (isDead) return;

        HandleGroundCheck();
        HandleMovementInput(); // Steering is handled here now
        ApplyGravity();
        HandleLeaning();
        ApplyMovement();
        UpdateWheelRotation();
        UpdateTrailSystem();
        UpdatePlayerToColliderLine(); // Call the function here
    }

    // --- Ground Check Logic ---
    void HandleGroundCheck()
    {
        if (groundCheckPoint != null)
        {
            isGroundedStatus = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
        }
        else { isGroundedStatus = false; }
    }

    // --- Input Handling and Speed Calculation ---
    void HandleMovementInput()
    {
        float accelerationInput = Input.GetAxis("Vertical");
        currentMoveSpeed += accelerationInput * acceleration * Time.deltaTime;
        currentMoveSpeed = Mathf.Clamp(currentMoveSpeed, minSpeed, maxSpeed);

        // Calculate the adjusted steer speed based on current speed
        float speedFactor = Mathf.InverseLerp(minSpeed, maxSpeed, currentMoveSpeed); // 0 at minSpeed, 1 at maxSpeed
        float adjustedSteerSpeed = baseSteerSpeed * (1f - (speedFactor * steerSpeedReductionFactor));

        currentSteerInput = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up * currentSteerInput * adjustedSteerSpeed * Time.deltaTime); // Apply adjusted steer speed

        if (canJump && Input.GetButtonDown("Jump") && isGroundedStatus)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    // --- Apply Gravity ---
    void ApplyGravity()
    {
        if (isGroundedStatus && velocity.y < 0) { velocity.y = -2f; }
        velocity.y += gravity * Time.deltaTime;
    }

    // --- Apply Leaning Visual ---
    void HandleLeaning()
    {
        if (leanTarget == null) return;

        // 1. Calculate lean based on steering input (Z-axis lean)
        float targetSteerLeanAngleZ = -currentSteerInput * maxLeanAngle;
        currentLeanAngleZ = Mathf.LerpAngle(currentLeanAngleZ, targetSteerLeanAngleZ, leanSpeed * Time.deltaTime);

        // 2. Calculate lean based on slope (X-axis lean)
        float targetSlopeLeanAngleX = 0f;
        if (isGroundedStatus && groundCheckPoint != null)
        {
            if (Physics.Raycast(groundCheckPoint.position, Vector3.down, out RaycastHit hit, groundCheckRadius * 2f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                // Project the player's forward onto the world up vector to get the forward direction on a flat plane
                Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                // The normal of the ground indicates the slope direction
                Vector3 groundNormal = hit.normal;
                // The angle between the world up and the ground normal gives the slope angle
                float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);

                // Calculate the direction of the slope relative to the player's forward
                Vector3 slopeDirection = Vector3.Cross(flatForward, groundNormal);

                // If the slope direction points to the player's right, lean back (positive X angle)
                // If it points to the left, lean forward (negative X angle)
                targetSlopeLeanAngleX = Vector3.Dot(transform.right, slopeDirection) * slopeAngle * slopeLeanSensitivity * maxSlopeLeanAngle;
                targetSlopeLeanAngleX = Mathf.Clamp(targetSlopeLeanAngleX, -maxSlopeLeanAngle, maxSlopeLeanAngle);
            }
        }
        currentLeanAngleX = Mathf.LerpAngle(currentLeanAngleX, targetSlopeLeanAngleX, leanSpeed * Time.deltaTime);

        // Apply the combined lean rotation
        leanTarget.localRotation = Quaternion.Euler(currentLeanAngleX, 0f, currentLeanAngleZ);
    }

    // --- Apply Final Movement to CharacterController ---
    void ApplyMovement()
    {
        Vector3 moveDirection = transform.forward * currentMoveSpeed;
        moveDirection.y = velocity.y;
        player.Move(moveDirection * Time.deltaTime);
    }

    // --- Wheel Rotation ---
    void UpdateWheelRotation()
    {
        float rotationSpeed = currentMoveSpeed * wheelRotationMultiplier;
        float rollDelta = rotationSpeed * Time.deltaTime;

        if (frontWheel != null)
        {
            float targetSteerAngle = currentSteerInput * 30f;
            frontWheelRollAngle -= rollDelta;
            frontWheelRollAngle %= 360f;
            // Assumes wheel rolls around local X, steers around local Y
            frontWheel.localRotation = Quaternion.Euler(frontWheelRollAngle, targetSteerAngle, 0f);
        }

        if (rearWheel != null)
        {
            rearWheelRollAngle -= rollDelta;
            rearWheelRollAngle %= 360f;
            // Assumes wheel rolls around local X
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
        if (trailPoints.Count == 0 || Vector3.Distance(trailPoints[trailPoints.Count - 1].WorldPosition, spawnPosition) >= colliderSpacing)
        {
            trailPoints.Add(new TrailPoint { WorldPosition = spawnPosition, Timestamp = Time.time });
        }
    }

    void RemoveOldTrailPoints()
    {
        float cutoffTime = Time.time - trailLifetime;
        int removeCount = 0;
        while (removeCount < trailPoints.Count && trailPoints[removeCount].Timestamp < cutoffTime) { removeCount++; }

        if (removeCount > 0)
        {
            for (int i = 0; i < removeCount; i++)
            {
                if (trailColliderObjects.Count > 0)
                {
                    GameObject toRemove = trailColliderObjects[0];
                    trailColliderObjects.RemoveAt(0);
                    if (toRemove != null) Destroy(toRemove);
                }
            }
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
                trailColliderObjects[i].SetActive(true);
            }
            else { trailColliderObjects[i].SetActive(false); }
        }
    }

    void CreateColliderObject()
    {
        GameObject colliderObj = new GameObject($"TrailColliderSegment_{trailColliderObjects.Count}");
        colliderObj.tag = trailColliderTag; // Use the trailColliderTag here

        CapsuleCollider capsule = colliderObj.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.direction = 2; // Z-axis

        Rigidbody rb = colliderObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

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
        if (capsule == null || line == null) return;

        Vector3 startPos = startPoint.WorldPosition;
        Vector3 endPos = endPoint.WorldPosition;
        Vector3 segmentVector = endPos - startPos;
        float segmentLength = segmentVector.magnitude;

        colliderObj.transform.position = (startPos + endPos) / 2f;
        if (segmentLength > 0.001f) { colliderObj.transform.rotation = Quaternion.LookRotation(segmentVector.normalized); }

        capsule.radius = segmentLineWidth / 2f;
        capsule.height = segmentLength + (capsule.radius * 2f);

        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
    }

    // --- Update the line between the player and the last recorded trail point ---
    void UpdatePlayerToColliderLine()
    {
        if (playerToColliderLineRenderer == null) return;
        if (trailPoints.Count > 0)
        {
            playerToColliderLineRenderer.enabled = true;
            playerToColliderLineRenderer.SetPosition(0, transform.position);
            playerToColliderLineRenderer.SetPosition(1, trailPoints[trailPoints.Count - 1].WorldPosition);
        }
        else
        {
            playerToColliderLineRenderer.enabled = false;
        }
    }

    // --- Collision Handling ---
    void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        // Check for the tag instead of the layer
        if (other.gameObject.tag == trailColliderTag)
        {
            GameObject collidedSegmentObject = other.gameObject;
            int colliderIndex = -1;
            for (int i = 0; i < trailColliderObjects.Count; ++i)
            {
                if (trailColliderObjects[i] == collidedSegmentObject)
                {
                    colliderIndex = i;
                    break;
                }
            }

            if (colliderIndex != -1 && colliderIndex < trailPoints.Count)
            {
                float pointTimestamp = trailPoints[colliderIndex].Timestamp;
                if ((Time.time - pointTimestamp) > collisionActivationDelay)
                {
                    TriggerDeathSequence();
                }
            }
        }
        else if (other.gameObject.tag == hazardTag)
        {
            TriggerDeathSequence();
        }
    }

    // --- Death Logic ---
    void TriggerDeathSequence()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Player Died!");
        if (OrangeEffect != null) OrangeEffect.Play();
        if (darkOrangeEffect != null) darkOrangeEffect.Play();
        if (BlackEffect != null) BlackEffect.Play();
        if (player != null) player.enabled = false;
        this.enabled = false;
    }

    // --- Cleanup ---
    void OnDestroy()
    {
        if (playerToColliderLineObject != null) Destroy(playerToColliderLineObject);
        foreach (var colliderObj in trailColliderObjects) { if (colliderObj != null) Destroy(colliderObj); }
        trailColliderObjects.Clear();
        trailPoints.Clear();
    }
}