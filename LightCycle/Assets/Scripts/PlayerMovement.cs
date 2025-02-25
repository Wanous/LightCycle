using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Le controlleur pour le joueur
    public CharacterController player;

    // Settings
    public float MoveSpeed = 5;
    public float SteerSpeed = 180;
    public float TrailSpeed = 5;
    public int Gap = 4;
    private float gravity = -19.62f; // Gravité pour tomber
    private Vector3 velocity; // Vitesse de chute
    private float jumpHeight = 2f; // Puissance du saut

    // Références
    public GameObject TrailPrefab;
    private GameObject TrailContainer; // Conteneur unique pour ce joueur


    // Listes
    private List<GameObject> TrailParts = new List<GameObject>();
    private List<Vector3> PositionsHistory = new List<Vector3>();

    public bool jump = false; // Activer la capacité de sauter
    public Transform isGrounded; // Vérification si le joueur touche le sol
    public LayerMask ground;
    private bool grounded = false;

    void Start()
    {
        // Créer un conteneur pour les segments du trail de ce joueur
        TrailContainer = new GameObject("TrailContainer_" + gameObject.name);

        StartCoroutine(GrowTrailOverTime(17, 0.5f)); // Ajoute 20 segments progressivement
    }

    void Update()
    {
        playerMovement();
    }

    private void playerMovement()
    {
        // Vérifier si le joueur est sur le sol
        grounded = Physics.CheckSphere(isGrounded.position, 0.2f, ground);

        // Si le joueur est au sol, arrêter l'accélération de la gravité
        if (grounded && velocity.y < 0)
            velocity.y = -2f;

        // Actionner le saut si activé
        if (jump && Input.GetButtonDown("Jump") && grounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Application de la gravité
        velocity.y += gravity * Time.deltaTime;
        player.Move(velocity * Time.deltaTime);

        // Avancer
        Vector3 move = transform.forward * MoveSpeed * Time.deltaTime;
        player.Move(move);

        // Tourner
        float steerDirection = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up * steerDirection * SteerSpeed * Time.deltaTime);

        // Stocker l'historique des positions
        PositionsHistory.Insert(0, transform.position);

        // Limiter la taille de la liste
        int maxHistory = Gap * TrailParts.Count + 1;
        if (PositionsHistory.Count > maxHistory)
        {
            PositionsHistory.RemoveAt(PositionsHistory.Count - 1);
        }

        // Déplacer les segments du trail
        int index = 1;
        foreach (var trail in TrailParts)
        {
            Vector3 point = PositionsHistory[Mathf.Clamp(index * Gap, 0, PositionsHistory.Count - 1)];

            // Déplacement du segment vers le point
            Vector3 moveDirection = point - trail.transform.position;
            trail.transform.position += moveDirection * TrailSpeed * Time.deltaTime;

            index++;
        }
    }

    private IEnumerator GrowTrailOverTime(int count, float delay)
    {
        for (int i = 0; i < count; i++)
        {
            GrowTrail();
            yield return new WaitForSeconds(delay);
        }
    }

    private void GrowTrail()
    {
        // Instancier un segment du trail
        GameObject trail = Instantiate(TrailPrefab);

        // Mettre le trail dans le conteneur au lieu du joueur
        if (TrailContainer != null)
        {
            trail.transform.SetParent(TrailContainer.transform);
        }

        TrailParts.Add(trail);

        // Ajouter un collider pour détecter les collisions
        if (trail.GetComponent<BoxCollider>() == null)
        {
            BoxCollider collider = trail.AddComponent<BoxCollider>();
            collider.isTrigger = false; // Activer les collisions physiques
        }
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Trail"))
        {
            Debug.Log("Collision avec le trail !");
            // Gérer la collision (ex: Game Over)
        }
    }
}
