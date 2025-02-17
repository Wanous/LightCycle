using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CameraMovement : NetworkBehaviour
{
    private const float YMin = -50.0f;
    private const float YMax = 50.0f;
    private const float FloorHeight = 0.5f;

    public Transform lookAt;
    public Transform Player;
    public float distance = 10.0f;
    private float currentX = 0.0f;
    private float currentY = 0.0f;
    public float sensitivity = 50.0f;
    public LayerMask collisionMask;

    private bool isCameraMoving = false;
    private bool isFocusedOnPlayer = true;

    private Transform targetEnemy; // Store the current enemy focus

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

        // Toggle between focusing on the player and the nearest enemy
        if (Input.GetKeyDown(KeyCode.C))
        {
            isFocusedOnPlayer = !isFocusedOnPlayer;
            
        }
        if (!isFocusedOnPlayer)
        {
            focusOnEnemy();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

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
            if (!isCameraMoving && (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0))
            {
                isCameraMoving = true;
            }

            if (isCameraMoving)
            {
                currentX += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
                currentY += Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
                currentY = Mathf.Clamp(currentY, YMin, YMax);
            }

            // Determine what the camera should focus on
            Transform focusTarget = isFocusedOnPlayer ? Player : targetEnemy;

            // If no valid enemy target is found, reset focus to the player
            if (focusTarget == null)
            {
                isFocusedOnPlayer = true;
                focusTarget = Player;
            }

            Vector3 direction = new Vector3(0, 0, -distance);
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
            Vector3 desiredPosition = focusTarget.position + rotation * direction;
            desiredPosition.y = Mathf.Max(desiredPosition.y, FloorHeight);

            // Collision Handling - Check if the camera is blocked
            RaycastHit hit;
            if (Physics.Linecast(focusTarget.position, desiredPosition, out hit, collisionMask))
            {
                transform.position = hit.point;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 5f);
            }

            transform.LookAt(focusTarget.position);
        }
    }

    void focusOnEnemy()
    {
        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        // Find the closest enemy AI
        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(Player.position, enemy.transform.position);
            if (distanceToEnemy < closestDistance)
            {
                closestDistance = distanceToEnemy;
                closestEnemy = enemy.transform;
            }
        }

        // Find the closest other player (but NOT yourself)
        foreach (GameObject otherPlayer in players)
        {
            if (otherPlayer == gameObject) // Skip yourself
                continue;

            float distanceToPlayer = Vector3.Distance(Player.position, otherPlayer.transform.position);
            if (distanceToPlayer < closestDistance)
            {
                closestDistance = distanceToPlayer;
                closestEnemy = otherPlayer.transform;
            }
        }

        // Set the closest target or revert to the player if no target found
        targetEnemy = closestEnemy;
        if (targetEnemy == null)
        {
            isFocusedOnPlayer = true;
        }
    }
}
