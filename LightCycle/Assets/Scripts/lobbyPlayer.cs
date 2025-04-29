using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementCC : MonoBehaviour
{
    [Header("Paramètres de déplacement")]
    public float moveSpeed = 6f; // Vitesse de déplacement horizontal
    public float jumpHeight = 2f; // Hauteur du saut
    public float gravity = -9.81f; // Gravité (valeur négative)

    [Header("Vérification du sol")]
    public Transform groundCheck;      // Point d'origine pour vérifier si le joueur est au sol (généralement un Transform placé aux pieds du personnage)
    public float groundDistance = 0.4f; // Rayon de la sphère de vérification du sol
    public LayerMask groundMask;       // Calque correspondant au sol

    [Header("Scènes désactivées")]
    public int[] buildIndicesToDeactivateIn; // Indices des scènes dans lesquelles ce script doit être désactivé

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        // Position initiale du personnage au lancement
        transform.position = new Vector3(-4.7f, 96f, 50.2f);

        // Récupérer la référence au CharacterController
        controller = GetComponent<CharacterController>();

        // Désactiver ce script dans les scènes spécifiées
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (buildIndicesToDeactivateIn != null && buildIndicesToDeactivateIn.Contains(currentSceneIndex))
        {
            this.enabled = false;
        }
    }

    void Update()
    {
        // Vérifier si le personnage est au sol en envoyant une sphère au niveau de groundCheck
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            // Rester « attaché » au sol quand on n'est pas en train de sauter
            velocity.y = -2f;
        }

        // Récupérer les entrées de déplacement horizontal et vertical (axes standards Unity : Horizontal = A/D ou flèches, Vertical = W/S ou flèches)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Calculer le vecteur de déplacement sur le plan X-Z en combinant gauche-droite et avant-arrière
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Gestion du saut : si le personnage est au sol et que le joueur appuie sur la touche de saut
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            // Calculer la vitesse initiale de saut nécessaire pour atteindre jumpHeight
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Appliquer la gravité en permanence
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        // Visualiser le GroundCheck par une sphère rouge dans l'éditeur
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
