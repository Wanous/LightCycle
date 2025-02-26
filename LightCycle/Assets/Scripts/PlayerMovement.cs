using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Le contrôleur pour le joueur
    public CharacterController player;

    // Paramètres de mouvement
    private float MoveSpeed = 5f;
    public float MaxSpeed = 50f;
    public float MinSpeed = 5f;
    public float acc = 5f;
    public float SteerSpeed = 180;
    private float gravity = -19.62f; // Gravité 
    private Vector3 velocity; // Vélocité pour le saut 
    private float jumpHeight = 2f; // Puissance de saut

    // Paramètres de la traînée
    public TrailRenderer trailRenderer; // Reference au TrailRenderer 
    public float TrailLength = 2f; // longueur du trail en secondes
    public bool ShowTrail = true; // La visibilité du trail
    private float ColliderLengthMultiplier = 10f; // mettre en public pour changer mais ça se colle automatiquement à la taille du Trail
    public float ColliderSpacing = 1f; // Distance entre les colliders

    // Vérification pour le sol 
    public bool jump = false; // Activer la capacité de sauter
    public Transform isGrounded; // Vérification si le joueur touche le sol
    public LayerMask ground; // Définition du sol
    private bool grounded = false; // Indique si le joueur est au sol

    // Effets de particules
    [SerializeField] ParticleSystem OrangeEffect;
    [SerializeField] ParticleSystem darkOrangeEffect;
    [SerializeField] ParticleSystem BlackEffect;

    // Listes pour stocker les positions du joueur pour les colliders
    private List<Vector3> trailPositions = new List<Vector3>();
    private List<GameObject> trailColliders = new List<GameObject>();

    void Start()
    {
        // Initialise le TrailRenderer
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }

        // Initialise les paramètres du trail
        trailRenderer.time = TrailLength; // Longueur du trail
        trailRenderer.enabled = ShowTrail; // Visibilité
        //ChangeLenghtCollider(); // Au cas où
    }

    void Update()
    {
        playerMovement();
        UpdateTrail();
    }

    private void ChangeLenghtCollider()
    {
        ColliderLengthMultiplier = 5 * TrailLength;
    }

    private void playerMovement()
    {
        // Vérifier si le joueur est sur le sol
        grounded = Physics.CheckSphere(isGrounded.position, 0.2f, ground);

        // Réinitialisation de la gravité au sol
        if (grounded && velocity.y < 0)
            velocity.y = -2f;

        // Gestion du saut
        if (jump && Input.GetButtonDown("Jump") && grounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Appliquer la gravité avant le mouvement
        velocity.y += gravity * Time.deltaTime;
        player.Move(velocity * Time.deltaTime);

        MoveSpeed = Mathf.Clamp(MoveSpeed + acc * Input.GetAxis("Vertical"), MinSpeed, MaxSpeed);

        // Ajuster la vitesse en fonction de l'entrée utilisateur
        Vector3 move = transform.forward * MoveSpeed * Time.deltaTime;
        player.Move(move);

         // Tourner le joueur
        float steerDirection = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up * steerDirection * SteerSpeed * Time.deltaTime);

        // Stock la nouvelle position du joueur 
        RecordTrailPositions();
    }

    private void RecordTrailPositions()
    {
        // Ajoute la position actuelle du joueur dans la liste si la distance est plus grande que ColliderSpacing
        if (trailPositions.Count == 0 || Vector3.Distance(trailPositions[trailPositions.Count - 1], transform.position) >= ColliderSpacing)
        {
            trailPositions.Add(transform.position);
        }

        // Enlève les positions les plus anciennes si la liste dépasse la longueur maximum
        int maxPositions = Mathf.CeilToInt(TrailLength * ColliderLengthMultiplier - 1);
        //Debug.Log(maxPositions); // test
        if (trailPositions.Count > maxPositions)
        {
            trailPositions.RemoveAt(0);
        }
    }

    private void UpdateTrail()
    {
        // Update la longueur du trail 
        trailRenderer.time = TrailLength;

        // change la visibilité du Trail (si demandé)
        trailRenderer.enabled = ShowTrail;

        // Update les colliders selon à partir positions stockées 
        UpdateTrailColliders();
    }

    private void UpdateTrailColliders()
    {
        // Calcule le nombre de colliders nécéssaires
        int colliderCount = trailPositions.Count;

        // Assure qu'il y a assez de collider
        for (int i = trailColliders.Count; i < colliderCount; i++)
        {
            AddTrailCollider();
        }

        // Enlève l'excès de collider
        for (int i = trailColliders.Count; i > colliderCount; i--)
        {
            RemoveTrailCollider();
        }

        // Update les positions des collider 
        for (int i = 0; i < trailColliders.Count; i++)
        {
            if (i < trailPositions.Count)
            {
                trailColliders[i].transform.position = trailPositions[i];
            }
        }
    }

    private void AddTrailCollider()
    {


        // ajoute un collider à la position courante du trail 
        GameObject colliderObject = new GameObject("TrailCollider");
        colliderObject.transform.position = transform.position; // Initialise la position 
        colliderObject.transform.SetParent(transform);

        BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
        collider.isTrigger = true; // autorise la détection de collisions
        collider.size = new Vector3(1, 1, 1); // Ajustement de la taille du collider 

        // Ajoute le collider à la liste
        trailColliders.Add(colliderObject);

    }

    private void RemoveTrailCollider()
    {
        // Enlève le dernier collider dans la liste
        if (trailColliders.Count > 0)
        {
            GameObject colliderToRemove = trailColliders[trailColliders.Count - 1];
            trailColliders.RemoveAt(trailColliders.Count - 1);

            // Regarde si le collider existe avant de le supprimer
            if (colliderToRemove != null)
            {
                Destroy(colliderToRemove);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Vérifier si le joueur entre en collision avec un segment de traînée
        if (trailColliders.Contains(other.gameObject) )
        {
            int colliderIndex = trailColliders.IndexOf(other.gameObject);
            Debug.Log($"Collided with TrailCollider at index: {colliderIndex}");
            if (colliderIndex == trailColliders.Count - 1 || colliderIndex == trailColliders.Count ||colliderIndex == trailColliders.Count - 2) return;
            // Jouer les effets de particules
            OrangeEffect.Play();
            darkOrangeEffect.Play();
            BlackEffect.Play();

            Destroy(this); // Tuer le joueur
            // Destroy(gameObject); // Alternative pour détruire l'objet joueur
        }
    }
}
