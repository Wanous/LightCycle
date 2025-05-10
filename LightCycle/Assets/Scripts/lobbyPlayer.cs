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

    [Header("Vérification du sol")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Scènes désactivées")]
    public int[] buildIndicesToDeactivateIn;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private Animator animator;

    // Hash for faster animator lookups
    private int isRunningHash;

    void Start()
    {
        // Optionally start the player at a fixed position
        transform.position = new Vector3(-4.7f, 96f, 50.2f);

        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        isRunningHash = Animator.StringToHash("isRunning");

        // Disable this script in specific scenes
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (buildIndicesToDeactivateIn != null && buildIndicesToDeactivateIn.Contains(currentSceneIndex))
        {
            this.enabled = false;
        }
    }

    void Update()
    {
        // 1) Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;  // keep a small downward force to stay “grounded”

        // 2) Get input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        // 3) Move the character
        controller.Move(move * moveSpeed * Time.deltaTime);

        // 4) Update Animator bool
        bool isRunning = move.sqrMagnitude > 0.01f;
        animator.SetBool(isRunningHash, isRunning);

        // 5) Jump
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 6) Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
