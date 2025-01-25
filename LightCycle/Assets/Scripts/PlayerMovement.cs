using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5.0f; // Vitesse de déplacement

    private TrailRenderer trailRenderer;

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
        // Déplacement avec les flèches directionnelles
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        // Mouvement dans le plan XZ
        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        transform.Translate(movement * speed * Time.deltaTime, Space.World);
    }
}
