using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Require necessary components to ensure they exist on the GameObject
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // --- Player Components ---
    [Header("Components")]
    // Assign the CharacterController in the Inspector (or it will be grabbed if added by RequireComponent)
    public CharacterController player;
    // Assign a child GameObject positioned at the player's base for ground checks
    public Transform isGrounded;
    // Assign a Material in the Inspector for the line segments
    [Tooltip("Material to use for the line segments between colliders")]
    public Material segmentLineMaterial;

    // --- Movement Parameters ---
    [Header("Movement")]
    [Tooltip("Minimum forward speed")]
    public float MinSpeed = 5f;
    [Tooltip("Maximum forward speed")]
    public float MaxSpeed = 50f;
    [Tooltip("Rate of speed change when accelerating/decelerating")]
    public float Acceleration = 5f;
    [Tooltip("Turning speed in degrees per second")]
    public float SteerSpeed = 180f;
    [Tooltip("Gravity force applied")]
    public float gravity = -19.62f; // Approx 2x Earth gravity
    [Tooltip("Initial upward velocity for jump")]
    public float jumpHeight = 2f;
    [Tooltip("Enable/disable jumping ability")]
    public bool canJump = true;

    // --- Trail Collision Parameters ---
    [Header("Trail Collision")]
    [Tooltip("How long the trail colliders persist before being destroyed")]
    public float TrailLifetime = 4.2f;
    [Tooltip("Minimum distance between recorded trail points")]
    public float ColliderSpacing = 1.0f;
    [Tooltip("How far behind the player the trail points are recorded")]
    public float PositionOffset = 1.0f;
    [Tooltip("The physics layer for the trail colliders")]
    public LayerMask trailColliderLayer; // Make sure this layer exists!
    [Tooltip("How long after a collider spawns before it can kill the player")]
    public float CollisionActivationDelay = 0.5f;
    [Tooltip("Width of the line segments drawn between colliders")]
    public float segmentLineWidth = 0.2f; // Increased line width
    [Tooltip("Width of the line segment connecting the player to the last collider")]
    public float playerToColliderLineWidth = 0.3f; // Width for the player-collider line

    // --- Ground Check Parameters ---
    [Header("Ground Check")]
    [Tooltip("The physics layer(s) considered 'ground'")]
    public LayerMask groundLayer;
    [Tooltip("Radius of the sphere check for grounding")]
    public float groundCheckRadius = 0.2f;

    // --- Effects ---
    [Header("Effects")]
    [SerializeField] ParticleSystem OrangeEffect;
    [SerializeField] ParticleSystem darkOrangeEffect;
    [SerializeField] ParticleSystem BlackEffect;

    // --- Private Variables ---
    private float currentMoveSpeed;
    private Vector3 velocity;
    private bool isGroundedStatus = false;
    private bool isDead = false;
    private GameObject playerToColliderLineObject; // GameObject for the line to the last collider
    private LineRenderer playerToColliderLineRenderer; // LineRenderer for the player-collider line

    // --- Trail Data Structures ---
    private class TrailPoint
    {
        public Vector3 WorldPosition;
        public float Timestamp;
        public Vector3 ForwardDirection;
    }
    private List<TrailPoint> trailPoints = new List<TrailPoint>();
    // Stores the GameObject for each collider segment (which now also holds a LineRenderer)
    private List<GameObject> trailColliderObjects = new List<GameObject>();

    // --- Initialization ---
    void Start()
    {
        // Get required components if not assigned
        if (player == null) player = GetComponent<CharacterController>(); ;

        // Check if material for segment lines is assigned
        if (segmentLineMaterial == null)
        {
            Debug.LogWarning("Segment Line Material not assigned in Inspector. Segment lines will not be visible.");
        }

        // Initialize speed
        currentMoveSpeed = MinSpeed;

        // Create the GameObject and LineRenderer for the player to last collider line
        playerToColliderLineObject = new GameObject("PlayerToColliderLine");
        playerToColliderLineRenderer = playerToColliderLineObject.AddComponent<LineRenderer>();
        playerToColliderLineRenderer.useWorldSpace = true;
        playerToColliderLineRenderer.positionCount = 2;
        playerToColliderLineRenderer.startWidth = playerToColliderLineWidth;
        playerToColliderLineRenderer.endWidth = playerToColliderLineWidth;
        if (segmentLineMaterial != null)
        {
            playerToColliderLineRenderer.material = segmentLineMaterial;
        }

        // Physics.IgnoreLayerCollision(gameObject.layer, trailColliderLayer, false); // Manage in Physics Settings
    }

    // --- Frame Update ---
    void Update()
    {
        if (isDead) return;

        HandleGroundCheck();
        HandleMovementInput();
        ApplyGravity();
        ApplyMovement();

        UpdateTrailSystem();
        UpdatePlayerToColliderLine();
    }

    // --- Movement Logic (Unchanged) ---
    void HandleGroundCheck()
    {
        if (isGrounded != null) { isGroundedStatus = Physics.CheckSphere(isGrounded.position, groundCheckRadius, groundLayer); }
        else { isGroundedStatus = false; Debug.LogWarning("isGrounded Transform not assigned!"); }
    }
    void HandleMovementInput()
    {
        float accelerationInput = Input.GetAxis("Vertical");
        currentMoveSpeed += accelerationInput * Acceleration * Time.deltaTime;
        currentMoveSpeed = Mathf.Clamp(currentMoveSpeed, MinSpeed, MaxSpeed);

        float steerInput = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up * steerInput * SteerSpeed * Time.deltaTime);

        if (canJump && Input.GetButtonDown("Jump") && isGroundedStatus)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
    void ApplyGravity()
    {
        if (isGroundedStatus && velocity.y < 0) { velocity.y = -2f; }
        velocity.y += gravity * Time.deltaTime;
    }
    void ApplyMovement()
    {
        Vector3 moveDirection = transform.forward * currentMoveSpeed;
        moveDirection.y = velocity.y;
        player.Move(moveDirection * Time.deltaTime);
    }

    // --- Trail System Logic ---
    void UpdateTrailSystem()
    {
        RecordTrailPosition();
        RemoveOldTrailPoints();
        UpdateColliderObjects();
    }

    // RecordTrailPosition (Unchanged)
    void RecordTrailPosition()
    {
        float dynamicSpacing = ColliderSpacing * (MinSpeed / Mathf.Max(currentMoveSpeed, MinSpeed));
        dynamicSpacing = Mathf.Clamp(dynamicSpacing, 0.1f, ColliderSpacing);
        Vector3 spawnPosition = transform.position - (transform.forward * PositionOffset);

        if (trailPoints.Count == 0 || Vector3.Distance(trailPoints[trailPoints.Count - 1].WorldPosition, spawnPosition) >= dynamicSpacing)
        {
            trailPoints.Add(new TrailPoint
            {
                WorldPosition = spawnPosition,
                Timestamp = Time.time,
                ForwardDirection = transform.forward
            });
        }
    }

    // RemoveOldTrailPoints (Unchanged)
    void RemoveOldTrailPoints()
    {
        float cutoffTime = Time.time - TrailLifetime;
        int removeCount = 0;
        while (removeCount < trailPoints.Count && trailPoints[removeCount].Timestamp < cutoffTime)
        {
            removeCount++;
        }
        if (removeCount > 0)
        {
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
            if (i < trailPoints.Count && i + 1 < trailPoints.Count)
            {
                UpdateSingleColliderObject(trailColliderObjects[i], trailPoints[i], trailPoints[i + 1]);
                trailColliderObjects[i].SetActive(true);
            }
            else { trailColliderObjects[i].SetActive(false); }
        }
    }

    // --- Modified: CreateColliderObject now adds and configures a LineRenderer ---
    void CreateColliderObject()
    {
        GameObject colliderObj = new GameObject("TrailColliderSegment");
        colliderObj.layer = trailColliderLayer;

        // Add CapsuleCollider
        CapsuleCollider capsule = colliderObj.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.direction = 2; // Z-axis

        // Add Rigidbody
        Rigidbody rb = colliderObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // --- Add and Configure LineRenderer for this segment ---
        LineRenderer line = colliderObj.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2; // Each line segment only connects two points
        line.startWidth = segmentLineWidth;
        line.endWidth = segmentLineWidth;

        // Assign the material specified in the Inspector
        if (segmentLineMaterial != null)
        {
            line.material = segmentLineMaterial;
        }
        else
        {
            // Material not assigned, line won't render. Warning issued in Start().
        }
        // Optional: Set sorting order if needed, e.g., to draw behind player trail
        // line.sortingOrder = -1;

        trailColliderObjects.Add(colliderObj);
    }

    // RemoveLastColliderObject (Unchanged - destroys the whole GameObject)
    void RemoveLastColliderObject()
    {
        if (trailColliderObjects.Count == 0) return;
        GameObject toRemove = trailColliderObjects[trailColliderObjects.Count - 1];
        trailColliderObjects.RemoveAt(trailColliderObjects.Count - 1);
        Destroy(toRemove);
    }

    // --- Modified: UpdateSingleColliderObject now updates the LineRenderer points ---
    void UpdateSingleColliderObject(GameObject colliderObj, TrailPoint startPoint, TrailPoint endPoint)
    {
        // --- Update Collider ---
        CapsuleCollider capsule = colliderObj.GetComponent<CapsuleCollider>();
        if (capsule == null) return; // Safety check

        Vector3 startPos = startPoint.WorldPosition;
        Vector3 endPos = endPoint.WorldPosition;
        Vector3 segmentVector = endPos - startPos;
        float segmentLength = segmentVector.magnitude;

        colliderObj.transform.position = (startPos + endPos) / 2f;

        if (segmentLength > 0.001f)
        {
            colliderObj.transform.rotation = Quaternion.LookRotation(segmentVector.normalized);
        }

        capsule.radius = 1 / 2f;
        capsule.height = segmentLength + 0.01f;

        // --- Update LineRenderer ---
        LineRenderer line = colliderObj.GetComponent<LineRenderer>();
        if (line != null) // Check if LineRenderer exists
        {
            // Set the start and end points for this segment's line
            line.SetPosition(0, startPos);
            line.SetPosition(1, endPos);
        }
    }

    // --- Update the line between the player and the last collider ---
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

    // --- Collision Handling (Unchanged) ---
    void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        if (other.gameObject.layer == trailColliderLayer)
        {
            GameObject otherGO = other.gameObject;
            int colliderIndex = -1;
            for (int i = 0; i < trailColliderObjects.Count; ++i)
            {
                if (trailColliderObjects[i] == otherGO) { colliderIndex = i; break; }
            }

            if (colliderIndex != -1 && colliderIndex < trailPoints.Count)
            {
                float pointTimestamp = trailPoints[colliderIndex].Timestamp;
                if ((Time.time - pointTimestamp) > CollisionActivationDelay)
                {
                    TriggerDeathSequence();
                }
            }
        }
    }

    // --- Death Logic (Unchanged) ---
    void TriggerDeathSequence()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Player Died - Collided with own trail!");
        if (OrangeEffect != null) OrangeEffect.Play();
        if (darkOrangeEffect != null) darkOrangeEffect.Play();
        if (BlackEffect != null) BlackEffect.Play();
        this.enabled = false;
        if (player != null) player.enabled = false;
    }

    // --- Cleanup (Unchanged - already destroys the GameObjects) ---
    void OnDestroy()
    {
        if (playerToColliderLineObject != null)
        {
            Destroy(playerToColliderLineObject);
        }
        foreach (var colliderObj in trailColliderObjects)
        {
            if (colliderObj != null) { Destroy(colliderObj); }
        }
        trailColliderObjects.Clear();
    }
}