using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementTutorial : MonoBehaviour
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

    // Component references
    private CharacterController controller;
    private Animator animator;

    // Movement variables
    private Vector3 velocity;
    private bool isGrounded;
    private bool isJumping;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (controller == null)
            Debug.LogError("Missing CharacterController component!", this);
    }

    void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleRotation();
        HandleJump();
        ApplyGravity();
        UpdateAnimations();
    }

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
        //controller.Move(move * Time.deltaTime);
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

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}

