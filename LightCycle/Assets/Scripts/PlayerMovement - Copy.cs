using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Le contrôleur pour le joueur
    public CharacterController player;

    // Paramètres de mouvement
    private float MoveSpeed = 10;
    public float MaxSpeed = 42;
    public float MinSpeed = 10;
    public float acceleration = 5;
    public float SteerSpeed = 180;
    private float gravity = -19.62f;
    private Vector3 velocity;
    private float jumpHeight = 2f; 

    // Paramètres de la traînée
    public GameObject TrailPrefab; // Préfabriqué de la traînée
    private GameObject TrailContainer; // Conteneur des traînées
    private float trailSpawnCooldown = 0f; // Cooldown pour la génération des traînées

    public bool jump = false; // Activer la capacité de sauter
    public Transform isGrounded; // Vérification si le joueur touche le sol
    public LayerMask ground; // Définition du sol
    private bool grounded = false; // Indique si le joueur est au sol

    [SerializeField] ParticleSystem OrangeEffect;
    [SerializeField] ParticleSystem darkOrangeEffect;
    [SerializeField] ParticleSystem BlackEffect;

    void Start()
    {
        // Créer un conteneur pour les traînées
        TrailContainer = new GameObject("TrailContainer_" + gameObject.name);
    }

    void Update()
    {
        playerMovement();
        SpawnTrail();
    }

    private void playerMovement()
    {
        // Vérifier si le joueur est sur le sol
        grounded = Physics.CheckSphere(isGrounded.position, 0.2f, ground);

        // Si le joueur est au sol, réinitialiser la gravité
        if (grounded && velocity.y < 0)
            velocity.y = -2f;

        // Gestion du saut
        if (jump && Input.GetButtonDown("Jump") && grounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Application de la gravité
        velocity.y += gravity * Time.deltaTime;
        player.Move(velocity * Time.deltaTime);

        // Ajuster la vitesse en fonction de l'entrée utilisateur
        MoveSpeed = Mathf.Clamp(MoveSpeed + acceleration * Input.GetAxis("Vertical"), MinSpeed, MaxSpeed);

        // Déplacer le joueur vers l'avant
        Vector3 move = transform.forward * MoveSpeed * Time.deltaTime;
        player.Move(move);

        // Tourner le joueur
        float steerDirection = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up * steerDirection * SteerSpeed * Time.deltaTime);
    }

    private void SpawnTrail()
    {
        // Contrôler la fréquence de génération des segments de traînée en fonction de la vitesse
        // Ajuster la position pour qu'elle soit légèrement derrière le joueur
        Vector3 trailPosition = transform.position - transform.forward * 0.5f;
        GameObject trail = Instantiate(TrailPrefab, trailPosition, transform.rotation);
        trail.transform.SetParent(TrailContainer.transform);
        
        Destroy(trail, 2f); // Détruire après 2 secondes

        // Ajuster dynamiquement la fréquence de génération
        if (MoveSpeed >= MaxSpeed)
        {
            trailSpawnCooldown = 0f; // Pas de cooldown à la vitesse maximale
        }
        else
        {
            trailSpawnCooldown = Mathf.Clamp(0.02f - (MoveSpeed / MaxSpeed) * 0.02f, 0f, 0.02f);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Vérifier si le joueur entre en collision avec un segment de traînée
        if (hit.gameObject.CompareTag("Trail"))
        {
            OrangeEffect.Play();
            darkOrangeEffect.Play();
            BlackEffect.Play();
            Destroy(this); // Tuer le joueur
            // Destroy(gameObject); // Alternative pour détruire l'objet joueur
        }
    }
}
