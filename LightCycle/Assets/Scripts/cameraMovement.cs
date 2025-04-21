using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CameraMovement : NetworkBehaviour
{
    private const float YMin = -50.0f;
    private const float YMax = 50.0f;
    private const float FloorHeight = 0.5f;

    [SyncVar] public Transform playerTransform;
    [SyncVar] private Transform targetEnemy = null;
    public float distance = 5.0f;
    [SyncVar] private float currentX = 0.0f;
    [SyncVar] private float currentY = 20.0f;
    public float sensitivity = 50.0f;
    public LayerMask collisionMask;
    public bool invert = false;
    private int inverted = 1;

    private enum CameraState
    {
        FollowEnemy,
        BehindPlayer,
        FollowMouse
    }

    [SyncVar] private CameraState currentCameraState = CameraState.BehindPlayer;
    public float behindDistance = 5.0f;
    public float behindHeight = 3.0f;

    public override void OnStartLocalPlayer()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (invert)
            inverted = -1;
        else inverted = 1;
    }

    void Update()
    {
        if (!isLocalPlayer || playerTransform == null)
            return;

        // Cycle camera modes
        if (Input.GetKeyDown(KeyCode.C))
        {
            CmdCycleCameraState();
        }

        // Escape to free the cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Re‑grab the cursor on click if we're in FollowMouse
        if (Input.GetMouseButtonDown(0) &&
            Cursor.lockState == CursorLockMode.None &&
            currentCameraState == CameraState.FollowMouse)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (!isLocalPlayer || playerTransform == null)
            return;

        bool isMouseModeLocked = (currentCameraState == CameraState.FollowMouse && Cursor.lockState == CursorLockMode.Locked);

        if (currentCameraState != CameraState.FollowMouse || isMouseModeLocked)
        {
            // Mouse look adjustments (only for FollowMouse mode)
            if (currentCameraState == CameraState.FollowMouse)
            {
                currentX += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime * inverted;
                currentY -= Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime * inverted;
                currentY = Mathf.Clamp(currentY, YMin, YMax);
            }

            Vector3 desiredPosition = Vector3.zero;
            Quaternion desiredRotation = Quaternion.identity;

            switch (currentCameraState)
            {
                case CameraState.FollowEnemy:
                    if (targetEnemy != null)
                    {
                        // Calculate the desired position behind the player, looking at the enemy
                        Vector3 directionToEnemy = (targetEnemy.position - playerTransform.position).normalized;
                        desiredPosition = playerTransform.position - directionToEnemy * distance + Vector3.up * 3f; // Adjust height as needed
                        desiredRotation = Quaternion.LookRotation(targetEnemy.position - desiredPosition);
                    }
                    else
                    {
                        CmdSetCameraState(CameraState.BehindPlayer); // Fallback if no enemy
                    }
                    break;

                case CameraState.BehindPlayer:
                    Quaternion behindRotation = Quaternion.Euler(currentY, playerTransform.eulerAngles.y, 0);
                    Vector3 behindOffset = new Vector3(0, behindHeight, -behindDistance);
                    desiredPosition = playerTransform.position + behindRotation * behindOffset;
                    desiredRotation = Quaternion.LookRotation(playerTransform.position + Vector3.up * 1.5f - desiredPosition);
                    break;

                case CameraState.FollowMouse:
                    Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
                    Vector3 offset = new Vector3(0, 3.0f, -distance);
                    desiredPosition = playerTransform.position + rotation * offset;
                    desiredRotation = rotation;
                    break;
            }

            // Ensure the camera stays above the floor
            desiredPosition.y = Mathf.Max(desiredPosition.y, FloorHeight);

            // Collision check
            RaycastHit hit;
            if (Physics.Linecast(playerTransform.position, desiredPosition, out hit, collisionMask))
            {
                transform.position = hit.point;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 5f);
            }

            // Apply rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, Time.deltaTime * 5f);
        }
        else if (isLocalPlayer && currentCameraState == CameraState.FollowMouse)
        {
            // Directly set rotation in FollowMouse when cursor is locked for immediate response
            transform.rotation = Quaternion.Euler(currentY, currentX, 0);
        }
    }

    [Command]
    void CmdCycleCameraState()
    {
        var states = System.Enum.GetValues(typeof(CameraState)).Length;
        currentCameraState = (CameraState)(((int)currentCameraState + 1) % states);

        if (currentCameraState == CameraState.FollowEnemy)
        {
            FindClosestEnemy();
        }
        else if (currentCameraState == CameraState.FollowMouse)
        {
            currentX = transform.eulerAngles.y;
            currentY = transform.eulerAngles.x;
        }
    }

    [Command]
    void CmdSetCameraState(CameraState newState)
    {
        currentCameraState = newState;
        if (currentCameraState == CameraState.FollowEnemy)
        {
            FindClosestEnemy();
        }
    }

    void FindClosestEnemy()
    {
        if (!isServer) return; // Only the server should find the closest enemy

        float closestDistance = Mathf.Infinity; // Distance la plus proche
        Transform closestEnemy = null; // Ennemi le plus proche

        // Trouver d'autres joueurs en réseau
        foreach (NetworkIdentity identity in FindObjectsOfType<NetworkIdentity>())
        {
            if (identity.isLocalPlayer)
                continue; // Ignorer soi-même

            Transform otherPlayer = identity.transform; // Transform de l'autre joueur
            // Ensure we are not targeting the player this camera is following
            if (otherPlayer == playerTransform)
                continue;

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
        // No need to set isFocusedOnEnemy directly here as the state machine handles it
    }

    // Called on the server to set the playerTransform for this camera
    [Server]
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }
}