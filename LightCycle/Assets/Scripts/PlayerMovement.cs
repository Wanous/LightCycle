using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Player components
    public CharacterController player;
    public Transform isGrounded;

    // Movement parameters
    private float MoveSpeed = 5;
    public float MaxSpeed = 50;
    public float MinSpeed = 5;
    public float Acceleration = 5;
    public float SteerSpeed = 180;
    private float gravity = -19.62f;
    private Vector3 velocity;
    private float jumpHeight = 2f;
    public bool jump;

    // Trail parameters
    public TrailRenderer trailRenderer;
    public float TrailLifetime = 4.2f;
    public float ColliderSpacing = 1.0f;
    public float PositionOffset = 2.0f;
    public LayerMask trailColliderLayer;

    // Ground check
    public LayerMask ground;
    private bool grounded = false;

    // Effects
    [SerializeField] ParticleSystem OrangeEffect;
    [SerializeField] ParticleSystem darkOrangeEffect;
    [SerializeField] ParticleSystem BlackEffect;

    private class TrailPoint
    {
        public Vector3 WorldPosition;
        public float Timestamp;
        public Vector3 ForwardDirection;
    }
    private List<TrailPoint> trailPoints = new List<TrailPoint>();
    private List<GameObject> trailColliders = new List<GameObject>();

    void Start()
    {
        InitializeTrail();
        Physics.IgnoreLayerCollision(gameObject.layer, trailColliderLayer, false);
    }

    void InitializeTrail()
    {
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }
        trailRenderer.time = TrailLifetime;
    }

    void Update()
    {
        HandleMovement();
        UpdateTrailSystem();
    }

    void HandleMovement()
    {
        grounded = Physics.CheckSphere(isGrounded.position, 0.2f, ground);
        HandleGravity();
        HandleJump();
        UpdateSpeed();
        MovePlayer();
        RecordTrailPosition();
    }

    void HandleGravity()
    {
        if (grounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        player.Move(velocity * Time.deltaTime);
    }

    void HandleJump()
    {
        if (jump && Input.GetButtonDown("Jump") && grounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    void UpdateSpeed()
    {
        MoveSpeed = Mathf.Clamp(
            MoveSpeed + Acceleration * Input.GetAxis("Vertical"),
            MinSpeed,
            MaxSpeed
        );
    }

    void MovePlayer()
    {
        player.Move(transform.forward * MoveSpeed * Time.deltaTime);
        transform.Rotate(Vector3.up * Input.GetAxis("Horizontal") * SteerSpeed * Time.deltaTime);
    }

    void RecordTrailPosition()
    {
        float spacing = ColliderSpacing * (MinSpeed / Mathf.Max(MoveSpeed, MinSpeed));
        spacing = Mathf.Clamp(spacing, 0.5f, ColliderSpacing);

        Vector3 spawnPosition = transform.position - (transform.forward * PositionOffset);

        if (ShouldRecordPosition(spacing, spawnPosition))
        {
            trailPoints.Add(new TrailPoint
            {
                WorldPosition = spawnPosition,
                Timestamp = Time.time,
                ForwardDirection = transform.forward
            });
        }

        RemoveOldPositions();
    }

    bool ShouldRecordPosition(float spacing, Vector3 spawnPosition)
    {
        return trailPoints.Count == 0 || 
            Vector3.Distance(trailPoints[^1].WorldPosition, spawnPosition) >= spacing;
    }

    void RemoveOldPositions()
    {
        float cutoff = Time.time - TrailLifetime;
        trailPoints.RemoveAll(point => point.Timestamp < cutoff);
    }

    void UpdateTrailSystem()
    {
        UpdateColliderCount();
        UpdateColliderPositions();
    }

    void UpdateColliderCount()
    {
        int requiredColliders = trailPoints.Count > 1 ? trailPoints.Count - 1 : 0;
        while (trailColliders.Count < requiredColliders) CreateCollider();
        while (trailColliders.Count > requiredColliders) RemoveCollider();
    }

    void CreateCollider()
    {
        GameObject colliderObject = new GameObject("TrailCollider");
        colliderObject.layer = trailColliderLayer;
        colliderObject.AddComponent<BoxCollider>().isTrigger = true;
        trailColliders.Add(colliderObject);
    }

    void RemoveCollider()
    {
        if (trailColliders.Count == 0) return;
        Destroy(trailColliders[^1]);
        trailColliders.RemoveAt(trailColliders.Count - 1);
    }

    void UpdateColliderPositions()
    {
        for (int i = 0; i < trailColliders.Count; i++)
        {
            if (i + 1 >= trailPoints.Count) continue;

            TrailPoint startPoint = trailPoints[i];
            TrailPoint endPoint = trailPoints[i + 1];

            Vector3 start = startPoint.WorldPosition;
            Vector3 end = endPoint.WorldPosition;

            UpdateCollider(trailColliders[i], start, end);
        }
    }

    void UpdateCollider(GameObject collider, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        collider.transform.position = (start + end) / 2f;
        collider.transform.rotation = Quaternion.LookRotation(segment);
        collider.GetComponent<BoxCollider>().size = new Vector3(
            trailRenderer.startWidth,
            trailRenderer.startWidth,
            segment.magnitude
        );
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsValidCollision(other.gameObject)) return;
        
        TriggerDeathEffects();
    }

    bool IsValidCollision(GameObject other)
    {
        // Check layer match and ensure it's not a brand new collider
        return other.layer == trailColliderLayer && 
               trailColliders.Contains(other) &&
               (Time.time - trailPoints[trailColliders.IndexOf(other)].Timestamp) > 0.5f;
    }

    void TriggerDeathEffects()
    {
        OrangeEffect.Play();
        darkOrangeEffect.Play();
        BlackEffect.Play();
        Destroy(this);
    }
}
