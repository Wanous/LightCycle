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
    public float turnSmoothTime = 0.1f; // Ajustez pour la réactivité de la rotation

    [Header("Vérification du sol")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Scènes désactivées")]
    public int[] buildIndicesToDeactivateIn;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private bool isGrounded;
    private float turnSmoothVelocity;

    // Animator hashes for performance
    private readonly int runForwardHash = Animator.StringToHash("RunForward");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator   = GetComponent<Animator>();

        // Disable this script (and controller) in specific build indices
        int idx = SceneManager.GetActiveScene().buildIndex;
        if (buildIndicesToDeactivateIn != null && buildIndicesToDeactivateIn.Contains(idx))
        {
            Debug.Log($"PlayerMovementCC disabled in scene #{idx}");
            controller.enabled = false;
            this.enabled       = false;
        }
    }

    void Update()
    {
        // If script/controller disabled, force Idle and bail out
        if (!enabled || (controller != null && !controller.enabled))
        {
            if (animator != null)
                animator.SetBool(runForwardHash, false);
            return;
        }

        // 1) Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        // 2) Read input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0f, v).normalized;
        bool  moving = dir.magnitude >= 0.1f;

        // 3) Handle movement + rotation
        if (moving)
        {
            // calculate target angle in world space
            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref turnSmoothVelocity,
                turnSmoothTime
            );
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // move in that direction
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir * moveSpeed * Time.deltaTime);
        }

        // 4) Animate
        if (animator != null)
            animator.SetBool(runForwardHash, moving);

        // 5) Jump
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            // e.g. animator.SetTrigger("Jump"); if you have a jump clip
        }

        // 6) Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
