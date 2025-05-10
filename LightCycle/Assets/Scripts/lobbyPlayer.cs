using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementCC : MonoBehaviour
{
    [Header("Paramètres de déplacement")]
    public float moveSpeed = 6f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float turnSmoothTime = 0.1f; // Adjust for rotation responsiveness
    float turnSmoothVelocity;

    [Header("Vérification du sol")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Scènes désactivées")]
    public int[] buildIndicesToDeactivateIn;

    public CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    public Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // Deactivate script in certain scenes
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (buildIndicesToDeactivateIn != null && buildIndicesToDeactivateIn.Contains(currentSceneIndex))
        {
            Debug.Log($"PlayerMovementCC deactivated in scene index: {currentSceneIndex}");
            this.enabled = false;
            if (controller != null) controller.enabled = false; // Also disable the CharacterController component
        }
    }

    void Update()
    {
        // If the script or controller is disabled, do nothing.
        if (!this.enabled || (controller != null && !controller.enabled))
        {
            if (animator != null)
            {
                animator.SetBool("RunForward", false); // Ensure idle if disabled
            }
            return;
        }

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // A small downward force to ensure better grounding
        }

       float horizontal = Input.GetAxisRaw("Horizontal"); // Raw input for more immediate response
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // Check if there is any movement input
        bool isMoving = direction.magnitude >= 0.1f;

        if (isMoving)
        {
            // --- Player Rotation (World Relative) ---
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // --- Player Movement (Relative to Player's New Forward) ---
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);

            // --- Animation: Play RunForward ---
            if (animator != null)
            {
                animator.SetBool("RunForward", true);
            }
        }
        else
        {
            // --- Animation: Stop RunForward (Play Idle) ---
            if (animator != null)
            {
                animator.SetBool("RunForward", false);
            }
        }

        // --- Jumping ---
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            // Optional: Trigger a jump animation
            // if (animator != null) { animator.SetTrigger("JumpTrigger"); }
        }

        // --- Gravity ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}