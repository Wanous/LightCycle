using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementCC : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseSpeed = 3f;
    public float boostSpeed = 6f;
    public float rotationSpeed = 120f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Scene Activation")]
    public string[] sceneNamePrefixesToDeactivate = { "multi" };
    public bool debugActivationState = true;
    public bool completelyDeactivateObject = true;
    public float sceneCheckInterval = 1f;

    // Component references
    private CharacterController controller;
    private Animator animator;

    // Movement variables
    private Vector3 velocity;
    private bool isGrounded;
    private bool isJumping;

    // State management
    private bool shouldBeActive = true;
    private float timeSinceLastSceneCheck = 0f;

    // Cone detection
    private bool isInsideCone = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (controller == null)
            Debug.LogError("Missing CharacterController component!", this);
        if (animator == null)
            Debug.LogWarning("Missing Animator component!", this);
    }

    void Start()
    {
        CheckSceneActivation();
        UpdateActivationState();

        if (shouldBeActive)
        {
            GoToSpawnPoint();
            DebugLog("Player initialized at spawn point in active state");
        }
    }

    void Update()
    {
        timeSinceLastSceneCheck += Time.deltaTime;
        if (timeSinceLastSceneCheck >= sceneCheckInterval)
        {
            timeSinceLastSceneCheck -= sceneCheckInterval;
            CheckSceneActivation();
            UpdateActivationState();
        }

        if (shouldBeActive && !isInsideCone)
        {
            HandleGroundCheck();
            HandleMovement();
            HandleRotation();
            HandleJump();
            ApplyGravity();
            UpdateAnimations();
        }
    }

    void CheckSceneActivation()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        bool newShouldBeActive = true;

        if (sceneNamePrefixesToDeactivate != null && sceneNamePrefixesToDeactivate.Length > 0)
        {
            foreach (string prefix in sceneNamePrefixesToDeactivate)
            {
                if (!string.IsNullOrEmpty(prefix) &&
                    currentScene.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    newShouldBeActive = false;
                    DebugLog($"Scene '{currentScene}' matches deactivation prefix '{prefix}'");
                    break;
                }
            }
        }

        if (newShouldBeActive != shouldBeActive)
        {
            shouldBeActive = newShouldBeActive;
            DebugLog($"Activation state changed to: {shouldBeActive} in scene: {currentScene}");
            if (shouldBeActive)
            {
                GoToSpawnPoint();
            }
        }
        else
        {
            DebugLog($"Activation state remains: {shouldBeActive} in scene: {currentScene}");
        }
    }

    void UpdateActivationState()
    {
        if (controller != null)
        {
            controller.enabled = shouldBeActive;
            DebugLog($"CharacterController enabled: {controller.enabled}");
        }

        if (animator != null)
        {
            animator.enabled = shouldBeActive;
            if (!shouldBeActive)
            {
                animator.Rebind();
                animator.Update(0f);
            }
            DebugLog($"Animator enabled: {animator.enabled}");
        }

        this.enabled = shouldBeActive;
        DebugLog($"Main script enabled: {this.enabled}");

        if (!shouldBeActive && completelyDeactivateObject)
        {
            gameObject.SetActive(false);
            DebugLog("Player GameObject completely deactivated");
        }
        else if (shouldBeActive && completelyDeactivateObject && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            DebugLog("Player GameObject reactivated");
            GoToSpawnPoint();
        }
    }

    void GoToSpawnPoint()
    {
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("SpawnPoint");
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.transform.position + Vector3.up * 2f;
            transform.rotation = spawnPoint.transform.rotation;
            DebugLog("Player moved to spawn point");
        }
        else
        {
            DebugLogWarning("SpawnPoint not found - cannot move to it");
        }
    }

    #region Movement Methods
    void HandleGroundCheck()
    {
        if (groundCheck == null) return;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            if (isJumping) isJumping = false;
        }
    }

    void HandleMovement()
    {
        float vertical = Input.GetAxis("Vertical");
        float speed = Input.GetKey(KeyCode.Z) ? boostSpeed : baseSpeed;
        Vector3 move = transform.forward * speed * vertical;
        controller.Move(move * Time.deltaTime);
    }

    void HandleRotation()
    {
        float horizontal = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up, horizontal * rotationSpeed * Time.deltaTime);
    }

    void HandleJump()
    {
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumping = true;
        }
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        float vertical = Input.GetAxis("Vertical");
        bool moving = Mathf.Abs(vertical) > 0.1f;

        animator.SetBool("RunForward", moving && vertical > 0);
        animator.SetBool("RunBackward", moving && vertical < 0);
        animator.SetBool("IsJumping", isJumping);
    }
    #endregion

    #region Cone Detection
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Cone"))
        {
            isInsideCone = true;
            DebugLog("Entered a cone!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Cone"))
        {
            isInsideCone = false;
            DebugLog("Exited a cone!");
        }
    }
    #endregion

    #region Debug Utilities
    void DebugLog(string message)
    {
        if (debugActivationState)
            Debug.Log($"[PlayerController] {message}", this);
    }

    void DebugLogWarning(string message)
    {
        if (debugActivationState)
            Debug.LogWarning($"[PlayerController] {message}", this);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
    #endregion
}