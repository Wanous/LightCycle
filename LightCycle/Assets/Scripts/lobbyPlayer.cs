using UnityEngine;

public class PlayerMovementCC : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundLayer;
    public bool isGrounded;

    private CharacterController controller;
    private Vector3 velocity;
    private float horizontalInput;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("CharacterController component not found on this GameObject!");
            enabled = false;
        }

        if (groundCheck == null)
        {
            Debug.LogError("Ground Check Transform not assigned!");
            enabled = false;
        }
    }

    void Update()
    {
        // Perform ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Reset downward velocity when grounded
        }

        // Get horizontal input
        horizontalInput = Input.GetAxisRaw("Horizontal");
        Vector3 move = transform.right * horizontalInput * moveSpeed;

        // Handle jump input
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Calculate the initial upward velocity required to reach the jump height
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        // Move the character using the CharacterController
        controller.Move(move * Time.deltaTime + velocity * Time.deltaTime);
    }

    // Optional: Visualize the ground check sphere in the editor
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}