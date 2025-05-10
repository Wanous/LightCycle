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
    private Animator animator; // << Ajouté

    void Start()
    {
        transform.position = new Vector3(-4.7f, 96f, 50.2f);
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>(); // << Ajouté

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (buildIndicesToDeactivateIn != null && buildIndicesToDeactivateIn.Contains(currentSceneIndex))
        {
            this.enabled = false;
        }
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Debug.Log("Valeur de x : " + x);
        Debug.Log("Valeur de z : " + z);

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        bool moving = false;
        if(move.magnitude > 0.01f) // Utilise une petite marge pour éviter les erreurs de virgule flottante
        {
            moving = true;
        }
        animator.SetBool("Moving", moving);

        float speed = new Vector3(x, 0, z).magnitude;
        if (animator != null)
        {
            animator.SetFloat("Speed", speed);
        }

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
