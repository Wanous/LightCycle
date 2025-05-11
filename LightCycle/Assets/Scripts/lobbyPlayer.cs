using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementCC : MonoBehaviour
{
    [Header("Paramètres de déplacement")]
    public float baseSpeed = 0.2f;         // Vitesse normale
    public float boostSpeed = 0.5f;        // Vitesse quand on appuie sur Z
    public float rotationSpeed = 120f;     // Vitesse de rotation (degrés/seconde)
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

    void Start()
    {
        controller = GetComponent<CharacterController>();

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
        if (!enabled || (controller != null && !controller.enabled))
            return;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        // Rotation avec Q (gauche) et D (droite)
        float rotateInput = 0f;
        if (Input.GetKey(KeyCode.A)) rotateInput = -1f; // A ≙ Q sur clavier AZERTY
        if (Input.GetKey(KeyCode.D)) rotateInput = 1f;
        transform.Rotate(Vector3.up, rotateInput * rotationSpeed * Time.deltaTime);

        // Vitesse : boost si Z est pressé
        float currentSpeed = Input.GetKey(KeyCode.Z) ? boostSpeed : baseSpeed;

        // Avance constante dans la direction actuelle
        Vector3 move = transform.forward * currentSpeed;
        controller.Move(move * Time.deltaTime);

        // Saut
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}

