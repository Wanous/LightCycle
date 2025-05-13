using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

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

    [Header("Disabled Scenes")]
    public int[] buildIndicesToDeactivateIn;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private Animator animator;

    private bool isJumping;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        GameObject spawnObj = GameObject.FindGameObjectWithTag("SpawnPoint");
        if (spawnObj != null)
        {
            transform.position = spawnObj.transform.position + Vector3.up * 2f;
            transform.rotation = spawnObj.transform.rotation;
        }
        else
        {
            Debug.LogWarning("SpawnPoint not found");
        }

        int idx = SceneManager.GetActiveScene().buildIndex;
        if (buildIndicesToDeactivateIn != null && buildIndicesToDeactivateIn.Contains(idx))
        {
            controller.enabled = false;
            this.enabled = false;
        }
    }

    void Update()
    {
        if (!enabled || controller == null || !controller.enabled) return;

        HandleGroundCheck();
        HandleMovement();
        HandleRotation();
        HandleJump();
        ApplyGravity();
        UpdateAnimations();
    }

    void HandleGroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            isJumping = false; // Reset jump state on landing
        }
    }

    void HandleMovement()
    {
        float verticalInput = Input.GetAxis("Vertical");
        float currentSpeed = Input.GetKey(KeyCode.Z) ? boostSpeed : baseSpeed;

        Vector3 move = transform.forward * currentSpeed * verticalInput;
        controller.Move(move * Time.deltaTime);
    }

    void HandleRotation()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up, horizontalInput * rotationSpeed * Time.deltaTime);
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
        float verticalInput = Input.GetAxis("Vertical");
        bool isRunning = Mathf.Abs(verticalInput) > 0.1f;

        animator.SetBool("RunForward", isRunning && verticalInput > 0f);
        animator.SetBool("RunBackward", isRunning && verticalInput < 0f);
        animator.SetBool("IsJumping", isJumping);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
