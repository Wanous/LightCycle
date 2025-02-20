using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CameraMovement : NetworkBehaviour
{
    private const float YMin = -50.0f;
    private const float YMax = 50.0f;
    private const float FloorHeight = 0.5f;

    public Transform playerTransform;  // Your player
    private Transform targetEnemy = null; // The closest enemy or other player
    public float distance = 10.0f;
    private float currentX = 0.0f;
    private float currentY = 20.0f; // Default slight upward angle
    public float sensitivity = 50.0f;
    public LayerMask collisionMask; // Layer mask for obstacles

    private bool isFocusedOnEnemy = false;

    void Start()
    {
        if (!isLocalPlayer)
        {
            GetComponent<Camera>().enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        // Toggle between normal mode and enemy-focused mode
        if (Input.GetKeyDown(KeyCode.C))
        {
            isFocusedOnEnemy = !isFocusedOnEnemy;
            if (isFocusedOnEnemy)
            {
                FindClosestEnemy();
            }
        }

        // Unlock cursor when pressing Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Relock cursor when clicking
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (!isLocalPlayer)
            return;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            if (!isFocusedOnEnemy)
            {
                // Free camera movement when NOT focusing on an enemy
                currentX += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
                currentY += Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
                currentY = Mathf.Clamp(currentY, YMin, YMax);
            }
            else if (targetEnemy != null)
            {
                // Rotate to look at the closest enemy
                Vector3 directionToEnemy = targetEnemy.position - playerTransform.position;
                currentX = Mathf.Atan2(directionToEnemy.x, directionToEnemy.z) * Mathf.Rad2Deg;
            }

            // **Camera positioning**
            Vector3 direction = new Vector3(0, 3.0f, -distance); // Slightly elevated
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
            Vector3 desiredPosition = playerTransform.position + rotation * direction;
            desiredPosition.y = Mathf.Max(desiredPosition.y, FloorHeight);

            // Handle collisions
            RaycastHit hit;
            if (Physics.Linecast(playerTransform.position, desiredPosition, out hit, collisionMask))
            {
                transform.position = hit.point;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 5f);
            }

            // Look at the player or enemy
            if (isFocusedOnEnemy && targetEnemy != null)
            {
                transform.LookAt(targetEnemy.position);
            }
            else
            {
                transform.LookAt(playerTransform.position + Vector3.up * 1.5f); // Look slightly above the player
            }
        }
    }

    void FindClosestEnemy()
    {
        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        // Find other networked players
        foreach (NetworkIdentity identity in FindObjectsOfType<NetworkIdentity>())
        {
            if (identity.isLocalPlayer)
                continue; // Skip self

            Transform otherPlayer = identity.transform;
            float distanceToPlayer = Vector3.Distance(playerTransform.position, otherPlayer.position);

            if (distanceToPlayer < closestDistance)
            {
                closestDistance = distanceToPlayer;
                closestEnemy = otherPlayer;
            }
        }

        // Find AI enemies
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            float distanceToEnemy = Vector3.Distance(playerTransform.position, enemy.transform.position);
            if (distanceToEnemy < closestDistance)
            {
                closestDistance = distanceToEnemy;
                closestEnemy = enemy.transform;
            }
        }

        targetEnemy = closestEnemy;
        isFocusedOnEnemy = (targetEnemy != null);
    }
}
