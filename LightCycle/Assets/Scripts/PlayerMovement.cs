using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Le controlleur pour le jour
    public CharacterController player;

    public float speed = 5.0f; // Vitesse de déplacement
    private float gravity = -19.62f; // Gravite pour tomber
    private Vector3 velocity; // Vitesse de chute
    private float jumpHeight = 2f; // Puissance du saut


    private TrailRenderer trailRenderer;
    public Transform isGrounded; // si le joueur touche le sol
    public LayerMask ground;
    private bool grounded = false ;

    void Start()
    {
        // Récupère le Trail Renderer
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            Debug.LogError("Trail Renderer is missing! Please attach one.");
        }
    }

    void Update()
    {
        playerMovement();
    }

    private void playerMovement()
    {
        // Verifier si le joueur et sur le sol
        grounded = Physics.CheckSphere(isGrounded.position, 1f, ground);

        // Si le joueur et sur le sol arreter le velocity d'augmenter
        if (grounded && velocity.y < 0)
            velocity.y = -2f;

        if (Input.GetButtonDown("Jump") && grounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Application du gravite
        velocity.y += gravity * Time.deltaTime;
        player.Move(velocity * Time.deltaTime);

        // Déplacement avec les flèches directionnelles
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        // Mouvement dans le plan XZ
        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical).normalized;
        player.Move(movement * speed * Time.deltaTime);
    }
}
