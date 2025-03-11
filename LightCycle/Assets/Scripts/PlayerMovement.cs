using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Le contrôleur pour le joueur
    public CharacterController player;

    // Paramètres de mouvement
    private float MoveSpeed = 5;
    public float MaxSpeed = 50;
    public float MinSpeed = 5;
    public float Acceleration = 5;
    public float SteerSpeed = 180;
    private float gravity = -19.62f; // Gravité 
    private Vector3 velocity; // Vélocité pour le saut 
    private float jumpHeight = 2f; // Puissance de saut

    // Paramètres de la traînée
    public TrailRenderer trailRenderer; // Reference au TrailRenderer 
    public float TrailDistance = 8f; // Increased base trail length (from 10f to 15f)
    private float lastMoveSpeed; // Track speed changes

    public bool ShowTrail = true; // La visibilité du trail
    public float BaseColliderSpacing = 1.5f; // Slightly increased base spacing (from 1f to 1.5f)
    private float ColliderLengthMultiplier = 5f; 
    public float BaseTrailDistance = 15f; // Increased base trail length (from 10f to 15f)

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
        ColliderLengthMultiplier = MoveSpeed;

        trailRenderer.time = TrailDistance; // Longueur du trail
        trailRenderer.enabled = ShowTrail; // Visibilité
        InitializeTrail();
    }

    void Update()
    {
        // Update trail time if speed changes
        if (MoveSpeed != lastMoveSpeed)
        {
            UpdateTrailTime();
            lastMoveSpeed = MoveSpeed;
            ColliderLengthMultiplier = MoveSpeed;
        }

        playerMovement();
        UpdateTrail();
    }

    private void ChangeLenghtCollider()
    {
        ColliderLengthMultiplier = (5 - ((MoveSpeed / MaxSpeed) * 0.5f)) * TrailDistance;
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

        // Ajuster la vitesse du joueur
        MoveSpeed = Mathf.Clamp(MoveSpeed + Acceleration * Input.GetAxis("Vertical"), MinSpeed, MaxSpeed);

        // Ajuster la vitesse en fonction de l'entrée utilisateur
        Vector3 move = transform.forward * MoveSpeed * Time.deltaTime;
        player.Move(move);

        // Tourner le joueur
        float steerDirection = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up * steerDirection * SteerSpeed * Time.deltaTime);

        // Stock la nouvelle position du joueur 
        RecordTrailPositions();
    }

    private void InitializeTrail()
    {
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }
        trailRenderer.enabled = ShowTrail;
        UpdateTrailTime(); // Initialize trail time
    }

    private void UpdateTrailTime()
    {
        // Adjust trail distance based on speed
        TrailDistance = BaseTrailDistance * (MoveSpeed / MinSpeed);
        
        // Avoid division by zero
        if (MoveSpeed > 0)
        {
            trailRenderer.time = TrailDistance / MoveSpeed;
        }
    }

    private void RecordTrailPositions()
    {
        // Dynamically adjust collider spacing based on speed (smaller spacing at higher speeds)
        float currentColliderSpacing = BaseColliderSpacing * (MinSpeed / Mathf.Max(MoveSpeed, MinSpeed));
        currentColliderSpacing = Mathf.Clamp(currentColliderSpacing, 0.5f, BaseColliderSpacing); // Adjusted minimum spacing to 0.5f

        // Ajoute la position actuelle si la distance est plus grande que currentColliderSpacing
        if (trailPositions.Count == 0 || Vector3.Distance(trailPositions[trailPositions.Count - 1], transform.position) >= currentColliderSpacing)
        {
            trailPositions.Add(transform.position);
        }

        // Enlève les positions les plus anciennes si la liste dépasse la longueur maximum
        int maxPositions = Mathf.CeilToInt(TrailDistance / currentColliderSpacing) + 1;
        while (trailPositions.Count > maxPositions)
        {
            trailPositions.RemoveAt(0);
        }
    }

    private void UpdateTrail()
    {
        // Update la longueur du trail 
        trailRenderer.time = TrailDistance;

        // change la visibilité du Trail (si demandé)
        trailRenderer.enabled = ShowTrail;

        // Update les colliders selon à partir positions stockées 
        UpdateTrailColliders();
    }

    private void UpdateTrailColliders()
    {
        // Calcule le nombre de colliders nécessaires
        int requiredColliders = trailPositions.Count > 1 ? trailPositions.Count - 1 : 0;

        // Assure qu'il y a assez de colliders
        while (trailColliders.Count < requiredColliders)
        {
            AddTrailCollider();
        }

        // Enlève l'excès de colliders
        while (trailColliders.Count > requiredColliders)
        {
            RemoveTrailCollider();
        }

        // Update les positions et tailles des colliders
        for (int i = 0; i < trailColliders.Count; i++)
        {
            if (i + 1 >= trailPositions.Count) continue;

            Vector3 start = trailPositions[i];
            Vector3 end = trailPositions[i + 1];
            GameObject colliderObj = trailColliders[i];
            BoxCollider col = colliderObj.GetComponent<BoxCollider>();

            // Calcule la direction et la distance entre les points
            Vector3 segment = end - start;
            float distance = segment.magnitude;

            // Positionne le collider au milieu du segment
            colliderObj.transform.position = (start + end) / 2f;

            // Oriente le collider dans la direction du segment
            colliderObj.transform.rotation = Quaternion.LookRotation(segment);

            // Ajuste la taille du collider
            float trailWidth = trailRenderer.startWidth;
            col.size = new Vector3(trailWidth, trailWidth, distance);
        }
    }

    private void AddTrailCollider()
    {
        GameObject colliderObject = new GameObject("TrailCollider");
        colliderObject.transform.position = transform.position;
        colliderObject.transform.SetParent(transform);

        BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(1, 1, 1);

        trailColliders.Add(colliderObject);
    }

    private void RemoveTrailCollider()
    {
        if (trailColliders.Count > 0)
        {
            GameObject colliderToRemove = trailColliders[trailColliders.Count - 1];
            trailColliders.RemoveAt(trailColliders.Count - 1);

            if (colliderToRemove != null)
            {
                Destroy(colliderToRemove);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (trailColliders.Contains(other.gameObject))
        {
            int colliderIndex = trailColliders.IndexOf(other.gameObject);
            Debug.Log($"Collided with TrailCollider at index: {colliderIndex}");
            if (colliderIndex == trailColliders.Count - 1 || colliderIndex == trailColliders.Count || colliderIndex == trailColliders.Count - 2) return;

            OrangeEffect.Play();
            darkOrangeEffect.Play();
            BlackEffect.Play();

            Destroy(this);
        }
    }
}
