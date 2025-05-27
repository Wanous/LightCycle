using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    public CharacterController player;
    public Transform groundCheckPoint;
    public Material segmentLineMaterial;
    public GameObject collisions;
    public EndOfGame endOfGame;

    [Header("Movement")]
    public float minSpeed = 2f;
    public float maxSpeed = 30f;
    public float acceleration = 3f;
    public float brakingDeceleration = 15f;
    public float progressiveDecelerationRate = 0.2f;
    private float currentDeceleration;
    public float baseSteerSpeed = 120f;
    [Range(0f, 1f)] public float steerSpeedReductionFactor = 0.5f;
    public float gravity = -19.62f;
    public float jumpHeight = 1.5f;
    private bool canJump = false;
    public float jumpCooldown = 3f;
    private float lastJumpTime;

    [Header("Boost")]
    public float boostDuration = 0.1f;
    public float boostCooldown = 5f;
    private bool isBoosting = false;
    private float boostStartTime;
    private float lastBoostTime;
    private bool canDash = true;


    [Header("Slope Movement")]
    public float slopeForceMultiplier = 5f;
    public float maxSlopeAngle = 45f;
    private bool isOnSlope = false;
    private Vector3 slopeNormal;
    public float uphillSpeedMultiplier = 0.5f;
    public float downhillSpeedMultiplier = 1.2f;
    private float slopeAngle;

    [Header("Leaning")]
    public Transform leanTarget;
    public float maxLeanAngle = 20f;
    public float leanSpeed = 4f;
    public float maxSlopeLeanAngle = 10f;
    [Range(0f, 1f)] public float slopeLeanSensitivity = 0.5f;
    public Transform leftChassisGroundCheck;
    public Transform rightChassisGroundCheck;
    public float chassisSideCheckMaxDistance = 0.5f;
    public float chassisCentralCheckMaxDistance = 0.6f;

    [Header("Trail Collision")]
    public float trailLifetime = 4.2f;
    public float colliderSpacing = 1.0f;
    public float positionOffset = 1.0f;
    public float collisionActivationDelay = 0.5f;
    public float segmentLineWidth = 0.2f;
    public float playerToColliderLineWidth = 0.3f;
    public string trailColliderTag = "Trail";
    public string hazardTag = "Hazard";

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.1f;
    public Transform frontWheelCheck;
    public Transform rearWheelCheck;

    [Header("Camera")]
    public Camera Cam;
    private float baseFOV = 55f;
    private float maxFOV = 90f;
    private float smoothSpeed = 5f;

    [Header("Effects")]
    [SerializeField] ParticleSystem OrangeEffect;
    [SerializeField] ParticleSystem darkOrangeEffect;
    [SerializeField] ParticleSystem BlackEffect;
    public AudioSource ExplosionSound;

    [Header("Wheel Rotation")]
    public Transform frontWheel;
    public Transform rearWheel;
    public float wheelRotationMultiplier = 40f;

    [Header("Speedometer")]
    public RectTransform speedometerNeedle;
    public float minSpeedForNeedle = 0f;
    public float maxSpeedForNeedle = 30f;
    public float minNeedleAngle = 0f;
    public float maxNeedleAngle = -270f;
    public GameObject speedometerCanvas;

    [Header("Respawn")]
    public List<Transform> spawnPoints;
    public float respawnDelay = 2.0f;
    private int currentSpawnPointIndex = 0;
    private bool hasSpawned = false;
    public string[] sceneNamePrefixesToDeactivate = { "Level" };

    private float currentMoveSpeed;
    private Vector3 velocity;
    private bool isGroundedStatus = false;
    private bool isDead = false;
    private GameObject playerToColliderLineObject;
    private LineRenderer playerToColliderLineRenderer;
    private float frontWheelRollAngle = 0f;
    private float rearWheelRollAngle = 0f;
    private float currentSteerInput = 0f;
    private float currentLeanAngleZ = 0f;
    private float currentLeanAngleX = 0f;
    private bool isBraking = false;
    private float deathTime;
    private Vector3 previousFramePosition;
    private Vector3 storedSlopeNormal = Vector3.up;

    private class TrailPoint
    {
        public Vector3 WorldPosition;
        public float Timestamp;
    }
    private List<TrailPoint> trailPoints = new List<TrailPoint>();
    private List<GameObject> trailColliderObjects = new List<GameObject>();

    void Start()
    {
        if (player == null) player = GetComponent<CharacterController>();

        if (player == null) { this.enabled = false; return; }
        if (leanTarget == null) { leanTarget = this.transform; }
        if (groundLayer.value == 0) { this.enabled = false; return; }
        if (frontWheelCheck == null) { this.enabled = false; return; }
        if (rearWheelCheck == null) { this.enabled = false; return; }

        spawnPoints = new List<Transform>();
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnPointObjects.Length == 0)
        {
            this.enabled = false; return;
        }
        foreach (GameObject spawnPointObject in spawnPointObjects)
        {
            spawnPoints.Add(spawnPointObject.transform);
        }
        if (!hasSpawned)
        {
            transform.position = spawnPoints[0].position;
            currentSpawnPointIndex = 0;
            hasSpawned = true;
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

        previousFramePosition = transform.position;
        endOfGame = GameObject.FindObjectOfType<EndOfGame>();

        if (Setting.Instance != null)
        {
            if (Setting.Instance.unlocked > 2) canJump = true;
            if (Setting.Instance.unlocked > 3) canDash = true;
        }

        lastJumpTime = -jumpCooldown;
        lastBoostTime = -boostCooldown;
    }

    void Update()
    {
        if (isDead)
        {
            if (Time.time >= deathTime + respawnDelay)
            {
                RespawnPlayer();
            }
            return;
        }

        HandleGroundCheck();
        HandleMovementInput();
        HandleBoost();
        ApplyGravity();
        HandleLeaning();
        ApplyMovement();
        UpdateWheelRotation();
        UpdateTrailSystem();
        UpdatePlayerToColliderLine();
        UpdateSpeedometerNeedle();

        if (Setting.Instance.paused)
            speedometerCanvas.SetActive(false);
        else
            speedometerCanvas.SetActive(true);

        previousFramePosition = transform.position;
    }

    void HandleGroundCheck()
    {
        bool frontGrounded = Physics.Raycast(frontWheelCheck.position, Vector3.down, out RaycastHit frontHit, 1f, groundLayer);
        bool rearGrounded = Physics.Raycast(rearWheelCheck.position, Vector3.down, out RaycastHit rearHit, 1f, groundLayer);
        isGroundedStatus = frontGrounded || rearGrounded;

        if (frontGrounded && rearGrounded)
            slopeNormal = (frontHit.normal + rearHit.normal).normalized;
        else if (frontGrounded)
            slopeNormal = frontHit.normal;
        else if (rearGrounded)
            slopeNormal = rearHit.normal;
        else
            slopeNormal = Vector3.up;

        if (isGroundedStatus)
        {
            storedSlopeNormal = slopeNormal;
        }
        else
        {
            slopeNormal = storedSlopeNormal;
        }

        isOnSlope = Vector3.Angle(Vector3.up, slopeNormal) > 1f;
        slopeAngle = Vector3.Angle(Vector3.up, slopeNormal);

        Debug.DrawRay(frontWheelCheck.position, Vector3.down * 1f, frontGrounded ? Color.green : Color.red);
        Debug.DrawRay(rearWheelCheck.position, Vector3.down * 1f, rearGrounded ? Color.green : Color.red);
    }

    void HandleMovementInput()
    {
        float accelerationInput = Input.GetAxis("Vertical");
        float speedMultiplier = 1f;

        if (isOnSlope && isGroundedStatus)
        {
            if (Vector3.Dot(transform.forward, Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized) > 0.1f)
            {
                speedMultiplier = downhillSpeedMultiplier;
            }
            else if (Vector3.Dot(transform.forward, Vector3.ProjectOnPlane(Vector3.up, slopeNormal).normalized) > 0.1f)
            {
                speedMultiplier = uphillSpeedMultiplier;
            }
        }

        if (accelerationInput > 0)
        {
            isBraking = false;
            currentDeceleration = progressiveDecelerationRate;
            currentMoveSpeed += acceleration * speedMultiplier * Time.deltaTime;
            currentMoveSpeed = Mathf.Clamp(currentMoveSpeed, minSpeed, maxSpeed);
        }
        else if (accelerationInput == 0)
        {
            if (currentMoveSpeed > minSpeed)
            {
                isBraking = false;
                currentMoveSpeed -= currentDeceleration * Time.deltaTime;
                currentMoveSpeed = Mathf.Max(currentMoveSpeed, minSpeed);
                currentDeceleration += progressiveDecelerationRate * Time.deltaTime;
            }
            else
            {
                isBraking = false;
                currentMoveSpeed = minSpeed;
                currentDeceleration = progressiveDecelerationRate;
            }
        }
        else
        {
            isBraking = true;
            currentMoveSpeed += accelerationInput * brakingDeceleration * Time.deltaTime;
            currentMoveSpeed = Mathf.Clamp(currentMoveSpeed, minSpeed, maxSpeed);
            currentDeceleration = progressiveDecelerationRate;
        }

        float speedFactor = Mathf.InverseLerp(minSpeed, maxSpeed, currentMoveSpeed);
        float adjustedSteerSpeed = baseSteerSpeed * (1f - (speedFactor * steerSpeedReductionFactor));
        currentSteerInput = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up * currentSteerInput * adjustedSteerSpeed * Time.deltaTime);

        if (canJump && Input.GetButtonDown("Jump") && isGroundedStatus && Time.time >= lastJumpTime + jumpCooldown)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;
        }
    }

    void HandleBoost()
    {
        if (Input.GetKeyDown("e") && Time.time >= lastBoostTime + boostCooldown && canDash && currentMoveSpeed > 5)
        {
            isBoosting = true;
            boostStartTime = Time.time;
            lastBoostTime = Time.time;
            currentMoveSpeed = maxSpeed + 142;
        }

        if (isBoosting && Time.time < boostStartTime + boostDuration)
        {   
            isBoosting = false;
        }
    }


    void ApplyGravity()
    {
        if (isGroundedStatus && velocity.y < 0)
        {
            velocity.y = -5f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    void HandleLeaning()
    {
        if (leanTarget == null) return;

        float targetSteerLeanAngleZ = -currentSteerInput * maxLeanAngle;
        Vector3 lateralVelocity = Vector3.ProjectOnPlane(player.velocity, transform.forward);
        float lateralSpeed = lateralVelocity.magnitude;
        float speedFactorForLean = Mathf.InverseLerp(minSpeed, maxSpeed * 0.8f, currentMoveSpeed);
        float speedLeanInfluence = Mathf.Sign(-currentSteerInput) * lateralSpeed * 1.0f * speedFactorForLean;
        targetSteerLeanAngleZ += speedLeanInfluence;
        targetSteerLeanAngleZ = Mathf.Clamp(targetSteerLeanAngleZ, -maxLeanAngle * 1.5f, maxLeanAngle * 1.5f);
        currentLeanAngleZ = Mathf.LerpAngle(currentLeanAngleZ, targetSteerLeanAngleZ, leanSpeed * Time.deltaTime);

        float targetSlopeLeanAngleX = 0f;

        if (isGroundedStatus)
        {
            RaycastHit centralHit;
            Vector3 groundNormalForLean = storedSlopeNormal;
            bool centralRayHit = Physics.Raycast(transform.position + transform.up * 0.1f,
                                                 Vector3.down, out centralHit,
                                                 chassisCentralCheckMaxDistance,
                                                 groundLayer, QueryTriggerInteraction.Ignore);
            if (centralRayHit)
            {
                groundNormalForLean = centralHit.normal;
            }
            Debug.DrawRay(transform.position + transform.up * 0.1f, Vector3.down * chassisCentralCheckMaxDistance, centralRayHit ? Color.yellow : Color.white);

            float centralBodySlopeAngle = Vector3.Angle(Vector3.up, groundNormalForLean);

            if (centralBodySlopeAngle > 1.5f)
            {
                targetSlopeLeanAngleX = Vector3.SignedAngle(transform.up, groundNormalForLean, transform.forward) * slopeLeanSensitivity;
            }

            if (leftChassisGroundCheck != null && rightChassisGroundCheck != null)
            {
                RaycastHit leftChassisHitInfo, rightChassisHitInfo;
                bool leftChassisGrounded = Physics.Raycast(leftChassisGroundCheck.position, Vector3.down, out leftChassisHitInfo, chassisSideCheckMaxDistance, groundLayer);
                bool rightChassisGrounded = Physics.Raycast(rightChassisGroundCheck.position, Vector3.down, out rightChassisHitInfo, chassisSideCheckMaxDistance, groundLayer);

                Debug.DrawRay(leftChassisGroundCheck.position, Vector3.down * chassisSideCheckMaxDistance, leftChassisGrounded ? Color.cyan : Color.magenta);
                Debug.DrawRay(rightChassisGroundCheck.position, Vector3.down * chassisSideCheckMaxDistance, rightChassisGrounded ? Color.cyan : Color.magenta);

                float terrainSlopeAngleFromWheels = Vector3.Angle(Vector3.up, storedSlopeNormal);

                if (terrainSlopeAngleFromWheels < 5.0f)
                {
                    bool leftSideOnFlat = leftChassisGrounded && Vector3.Angle(Vector3.up, leftChassisHitInfo.normal) < 5.0f;
                    bool rightSideOnFlat = rightChassisGrounded && Vector3.Angle(Vector3.up, rightChassisHitInfo.normal) < 5.0f;

                    if (leftSideOnFlat && rightSideOnFlat)
                    {
                        targetSlopeLeanAngleX = 0f;
                    }
                    else if (leftSideOnFlat && !rightChassisGrounded && currentLeanAngleX > 1.0f)
                    {
                        targetSlopeLeanAngleX = Mathf.Lerp(targetSlopeLeanAngleX, 0f, 0.1f);
                    }
                    else if (rightSideOnFlat && !leftChassisGrounded && currentLeanAngleX < -1.0f)
                    {
                        targetSlopeLeanAngleX = Mathf.Lerp(targetSlopeLeanAngleX, 0f, 0.1f);
                    }
                }
            }
            targetSlopeLeanAngleX = Mathf.Clamp(targetSlopeLeanAngleX, -maxSlopeLeanAngle, maxSlopeLeanAngle);
        }

        currentLeanAngleX = Mathf.LerpAngle(currentLeanAngleX, targetSlopeLeanAngleX, leanSpeed * Time.deltaTime);

        leanTarget.localRotation = Quaternion.Euler(currentLeanAngleX, 0f, currentLeanAngleZ);
    }

    void ApplyMovement()
    {
        Vector3 forwardDirection = transform.forward;
        Vector3 moveDirectionOnSlope = forwardDirection;

        if (isOnSlope && isGroundedStatus)
        {
            moveDirectionOnSlope = Vector3.ProjectOnPlane(forwardDirection, slopeNormal).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(moveDirectionOnSlope, slopeNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        else if (isGroundedStatus)
        {
            Quaternion targetRotation = Quaternion.LookRotation(transform.forward, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        Vector3 moveVector = moveDirectionOnSlope * currentMoveSpeed;
        moveVector.y = velocity.y;

        player.Move(moveVector * Time.deltaTime);

        if (Cam != null)
        {
            float targetFOV = Mathf.Lerp(baseFOV, maxFOV, currentMoveSpeed / maxSpeed);
            Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
        }
    }

    void UpdateWheelRotation()
    {
        float rotationSpeed = currentMoveSpeed * wheelRotationMultiplier;
        float rollDelta = rotationSpeed * Time.deltaTime;

        if (frontWheel != null)
        {
            float targetSteerAngle = currentSteerInput * 30f;
            frontWheelRollAngle -= rollDelta;
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
        while (removeCount < trailPoints.Count && trailPoints[removeCount].Timestamp < cutoffTime)
        {
            removeCount++;
        }

        if (removeCount > 0)
        {
            for (int i = 0; i < removeCount && trailColliderObjects.Count > 0; i++)
            {
                if (trailColliderObjects[0] != null) Destroy(trailColliderObjects[0]);
                trailColliderObjects.RemoveAt(0);
            }
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

    void CreateColliderObject()
    {
        GameObject colliderObj = new GameObject($"TrailColliderSegment_{trailColliderObjects.Count}");
        colliderObj.tag = trailColliderTag;

        CapsuleCollider capsule = colliderObj.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.direction = 2;

        Rigidbody rb = colliderObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        TrailSegmentCollider segmentHelper = colliderObj.AddComponent<TrailSegmentCollider>();
        segmentHelper.Initialize(collisionActivationDelay);

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
        capsule.height = segmentLength + (capsule.radius * 2f);

        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
    }

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

    void UpdateSpeedometerNeedle()
    {
        if (speedometerNeedle == null) return;
        float normalizedSpeed = Mathf.InverseLerp(minSpeedForNeedle, maxSpeedForNeedle, currentMoveSpeed);
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);
        float targetNeedleAngle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, normalizedSpeed);
        speedometerNeedle.localEulerAngles = new Vector3(0f, 0f, targetNeedleAngle);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.gameObject == gameObject) return;

        bool isHazardCollision = other.gameObject.CompareTag(hazardTag) && currentMoveSpeed > 15f;

        if (other.gameObject.CompareTag(trailColliderTag) || isHazardCollision ||
            other.gameObject.CompareTag("plane") || transform.position.y < -10 ||
            (other.gameObject.CompareTag("Player") && other.gameObject != collisions) ||
            other.gameObject.CompareTag("Enemy"))
        {
            TriggerDeathSequence();
        }
    }

    void TriggerDeathSequence()
    {
        if (isDead) return;
        isDead = true;
        deathTime = Time.time;

        if (OrangeEffect != null) OrangeEffect.Play();
        if (darkOrangeEffect != null) darkOrangeEffect.Play();
        if (BlackEffect != null) BlackEffect.Play();
        if (ExplosionSound != null) ExplosionSound.Play();

        if (player != null) player.enabled = false;
    }

    void RespawnPlayer()
    {
        if (ShouldLoadMenuScene())
        {
            speedometerCanvas.SetActive(false);
            endOfGame.GameOver();
            endOfGame.nextLevelButton.gameObject.SetActive(false);
            return;
        }

        isDead = false;

        TogglePlayer(false);
        MoveToNextSpawnPoint();
        TogglePlayer(true);

        ResetMovementState();
        ClearTrail();
    }

    bool ShouldLoadMenuScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (sceneNamePrefixesToDeactivate == null) return false;

        foreach (string prefix in sceneNamePrefixesToDeactivate)
        {
            if (!string.IsNullOrEmpty(prefix) &&
                currentScene.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    void TogglePlayer(bool isEnabled)
    {
        if (player != null)
            player.enabled = isEnabled;
    }

    void MoveToNextSpawnPoint()
    {
        currentSpawnPointIndex = (currentSpawnPointIndex + 1) % spawnPoints.Count;
        transform.position = spawnPoints[currentSpawnPointIndex].position;
        transform.rotation = spawnPoints[currentSpawnPointIndex].rotation * Quaternion.Euler(0, 90f, 0);
    }

    void ResetMovementState()
    {
        velocity = Vector3.zero;
        currentMoveSpeed = minSpeed;
        currentDeceleration = brakingDeceleration;
        storedSlopeNormal = Vector3.up;
        currentLeanAngleX = 0f;
        currentLeanAngleZ = 0f;
        lastJumpTime = Time.time; 
        isBoosting = false; 
        lastBoostTime = Time.time;

        if (leanTarget != null)
            leanTarget.localRotation = Quaternion.identity;
    }


    void ClearTrail()
    {
        foreach (GameObject colliderObj in trailColliderObjects)
        {
            if (colliderObj != null) Destroy(colliderObj);
        }
        trailColliderObjects.Clear();
        trailPoints.Clear();
        if (playerToColliderLineRenderer != null)
            playerToColliderLineRenderer.enabled = false;
    }

    void OnDestroy()
    {
        if (playerToColliderLineObject != null) Destroy(playerToColliderLineObject);
        foreach (var colliderObj in trailColliderObjects)
        {
            if (colliderObj != null) Destroy(colliderObj);
        }
        trailColliderObjects.Clear();
        trailPoints.Clear();
    }

    public class TrailSegmentCollider : MonoBehaviour
    {
        private float creationTime;
        private float activationDelay;
        private CapsuleCollider capsuleCollider;
        private bool isInitialized = false;

        public void Initialize(float delay)
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider == null)
            {
                enabled = false;
                return;
            }
            creationTime = Time.time;
            activationDelay = delay;
            capsuleCollider.enabled = false;
            isInitialized = true;
        }

        void Update()
        {
            if (!isInitialized || capsuleCollider == null) return;

            if (!capsuleCollider.enabled)
            {
                if (Time.time >= creationTime + activationDelay)
                {
                    capsuleCollider.enabled = true;
                }
            }
        }
    }
}