using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CameraMovement : NetworkBehaviour
{
    private const float YMin = -50.0f; // Limite minimale de l'angle Y
    private const float YMax = 50.0f; // Limite maximale de l'angle Y
    private const float FloorHeight = 0.5f; // Hauteur du sol

    public Transform playerTransform;  // Transform du joueur
    private Transform targetEnemy = null; // Ennemi ou autre joueur le plus proche
    public float distance = 10.0f; // Distance de la caméra au joueur
    private float currentX = 0.0f; // Angle X actuel
    private float currentY = 20.0f; // Angle Y actuel (élevé pour une meilleure vue)
    public float sensitivity = 50.0f; // Sensibilité de la souris
    public LayerMask collisionMask; // Masque de couche pour les obstacles

    private bool isFocusedOnEnemy = false; // Indique si la caméra est focalisée sur un ennemi

    void Start()
    {
        if (!isLocalPlayer)
        {
            GetComponent<Camera>().enabled = false; // Désactiver la caméra pour les joueurs non locaux
            return;
        }

        Cursor.lockState = CursorLockMode.Locked; // Verrouiller le curseur
        Cursor.visible = false; // Rendre le curseur invisible
    }

    void Update()
    {
        if (!isLocalPlayer)
            return; // Sortir si ce n'est pas un joueur local

        // Basculer entre le mode normal et le mode focalisé sur un ennemi
        if (Input.GetKeyDown(KeyCode.C))
        {
            isFocusedOnEnemy = !isFocusedOnEnemy; // Changer l'état de focalisation
            if (isFocusedOnEnemy)
            {
                FindClosestEnemy(); // Trouver l'ennemi le plus proche
            }
        }

        // Déverrouiller le curseur en appuyant sur Échap
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None; // Déverrouiller le curseur
            Cursor.visible = true; // Rendre le curseur visible
        }

        // Verrouiller le curseur lors du clic
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked; // Verrouiller le curseur
            Cursor.visible = false; // Rendre le curseur invisible
        }
    }

    void LateUpdate()
    {
        if (!isLocalPlayer)
            return; // Sortir si ce n'est pas un joueur local

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            if (!isFocusedOnEnemy)
            {
                // Mouvement libre de la caméra quand on ne se concentre pas sur un ennemi
                currentX += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime; // Contrôle horizontal
                currentY += Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime; // Contrôle vertical
                currentY = Mathf.Clamp(currentY, YMin, YMax); // Limiter l'angle Y
            }
            else if (targetEnemy != null)
            {
                // Rotation pour regarder le plus proche ennemi
                Vector3 directionToEnemy = targetEnemy.position - playerTransform.position;
                currentX = Mathf.Atan2(directionToEnemy.x, directionToEnemy.z) * Mathf.Rad2Deg; // Calculer l'angle
            }

            // Positionnement de la caméra
            Vector3 direction = new Vector3(0, 3.0f, -distance); // Légèrement élevé
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0); // Rotation de la caméra
            Vector3 desiredPosition = playerTransform.position + rotation * direction; // Position désirée
            desiredPosition.y = Mathf.Max(desiredPosition.y, FloorHeight); // Assurer que la caméra ne descend pas sous le sol

            // Gestion des collisions
            RaycastHit hit;
            if (Physics.Linecast(playerTransform.position, desiredPosition, out hit, collisionMask))
            {
                transform.position = hit.point; // Positionner la caméra en fonction des collisions
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 5f); // Mouvement doux
            }

            // Regarder le joueur ou l'ennemi
            if (isFocusedOnEnemy && targetEnemy != null)
            {
                transform.LookAt(targetEnemy.position); // Regarder l'ennemi
            }
            else
            {
                transform.LookAt(playerTransform.position + Vector3.up * 1.5f); // Regarder légèrement au-dessus du joueur
            }
        }
    }

    void FindClosestEnemy()
    {
        float closestDistance = Mathf.Infinity; // Distance la plus proche
        Transform closestEnemy = null; // Ennemi le plus proche

        // Trouver d'autres joueurs en réseau
        foreach (NetworkIdentity identity in FindObjectsOfType<NetworkIdentity>())
        {
            if (identity.isLocalPlayer)
                continue; // Ignorer soi-même

            Transform otherPlayer = identity.transform; // Transform de l'autre joueur
            float distanceToPlayer = Vector3.Distance(playerTransform.position, otherPlayer.position); // Calculer la distance

            if (distanceToPlayer < closestDistance)
            {
                closestDistance = distanceToPlayer; // Mettre à jour la distance la plus proche
                closestEnemy = otherPlayer; // Mettre à jour l'ennemi le plus proche
            }
        }

        // Trouver les ennemis IA
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            float distanceToEnemy = Vector3.Distance(playerTransform.position, enemy.transform.position); // Calculer la distance à l'ennemi
            if (distanceToEnemy < closestDistance)
            {
                closestDistance = distanceToEnemy; // Mettre à jour la distance la plus proche
                closestEnemy = enemy.transform; // Mettre à jour l'ennemi le plus proche
            }
        }

        targetEnemy = closestEnemy; // Assigner l'ennemi le plus proche
        isFocusedOnEnemy = (targetEnemy != null); // Mettre à jour l'état de focalisation
    }
}
