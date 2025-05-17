using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CameraMovementOffline : MonoBehaviour
{
    private const float YMin = -50.0f; // Limite minimale de l'angle Y
    private const float YMax = 50.0f; // Limite maximale de l'angle Y
    private const float FloorHeight = 0.5f; // Hauteur du sol

    public Transform playerTransform;  // Transform du joueur
    private Transform targetEnemy = null; // Ennemi ou autre joueur le plus proche
    public float distance = 5.0f; // Default distance for Normal mode
    private float currentX = 0.0f;
    private float currentY = 20.0f;
    public float sensitivity; // Removed initial assignment
    public LayerMask collisionMask;
    public bool invert; // Removed initial assignment
    private int inverted = 1;

    private enum CameraMode
    {
        Normal,
        BehindPlayer,
        FocusEnemyBetween
    }
    private CameraMode currentMode = CameraMode.Normal;
    public float behindPlayerDistance = 5.0f;
    public float behindPlayerHeight = 2.0f;
    public float focusDistance = 5.0f;
    public float focusHeightOffset = 2.0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // Read initial invert setting
        invert = Setting.Instance.Invert;
        inverted = invert ? -1 : 1;
    }

    void Update()
    {
        // Dynamically read sensitivity every frame
        if (Setting.Instance != null)
        {
            sensitivity = Setting.Instance.Sensitive;
            // Optionally update invert every frame if it can change during gameplay
            if (invert != Setting.Instance.Invert)
            {
                invert = Setting.Instance.Invert;
                inverted = invert ? -1 : 1;
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            int nextModeIndex = ((int)currentMode + 1) % 3;

            if (nextModeIndex == (int)CameraMode.FocusEnemyBetween)
            {
                FindClosestEnemy(); // Try to find an enemy
                currentMode = (targetEnemy != null) ? (CameraMode)nextModeIndex : (CameraMode)(((int)currentMode + 2) % 3);
                // If enemy found, go to FocusEnemyBetween.
                // If not, skip FocusEnemyBetween and go to the next mode.
            }
            else
            {
                currentMode = (CameraMode)nextModeIndex; // Switch to Normal or BehindPlayer directly
            }

            // Ensure targetEnemy is found if we are already in FocusEnemyBetween mode
            if (currentMode == CameraMode.FocusEnemyBetween && targetEnemy == null)
            {
                FindClosestEnemy();
                if (targetEnemy == null)
                {
                    currentMode = CameraMode.Normal; // Revert to Normal if the target is lost
                }
            }
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
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            if (currentMode == CameraMode.Normal)
            {
                currentX += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime * inverted;
                currentY += Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime * inverted;
                currentY = Mathf.Clamp(currentY, YMin, YMax);
            }
            else if (currentMode == CameraMode.BehindPlayer)
            {
                float targetRotationY = playerTransform.eulerAngles.y;
                Quaternion targetRotation = Quaternion.Euler(0, targetRotationY, 0);
                Vector3 offset = new Vector3(0, behindPlayerHeight, -behindPlayerDistance);
                Vector3 desiredPosition = playerTransform.position + targetRotation * offset;

                transform.position = desiredPosition;
                transform.LookAt(playerTransform.position + Vector3.up * 1.5f);
                return;
            }
            else if (currentMode == CameraMode.FocusEnemyBetween && targetEnemy != null)
            {
                Vector3 playerToEnemy = (targetEnemy.position - playerTransform.position).normalized;
                Vector3 desiredPosition = playerTransform.position - playerToEnemy * focusDistance + Vector3.up * focusHeightOffset;

                RaycastHit hit;
                if (Physics.Linecast(targetEnemy.position, desiredPosition, out hit, collisionMask))
                {
                    transform.position = hit.point;
                }
                else
                {
                    transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 5f);
                }

                transform.LookAt(targetEnemy.position);
                return;
            }

            // Positionnement de la caméra (for Normal mode)
            Vector3 direction = new Vector3(0, 3.0f, -distance);
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
            Vector3 desiredPositionBase = playerTransform.position + rotation * direction;
            Vector3 desiredPositionClamped = new Vector3(desiredPositionBase.x, Mathf.Max(desiredPositionBase.y, FloorHeight), desiredPositionBase.z);

            // Gestion des collisions (for Normal mode)
            RaycastHit hitNormal;
            if (Physics.Linecast(playerTransform.position, desiredPositionClamped, out hitNormal, collisionMask))
            {
                transform.position = hitNormal.point;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPositionClamped, Time.deltaTime * 5f);
            }

            // Orientation de la caméra (for Normal mode)
            if (currentMode == CameraMode.Normal)
            {
                transform.LookAt(playerTransform.position + Vector3.up * 1.5f);
            }
        }
    }

    void FindClosestEnemy()
    {
        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        // Find other players
        foreach (GameObject otherPlayer in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (otherPlayer.transform == playerTransform)
                continue; // Skip the local player

            float distanceToPlayer = Vector3.Distance(playerTransform.position, otherPlayer.transform.position);
            if (distanceToPlayer < closestDistance)
            {
                closestDistance = distanceToPlayer;
                closestEnemy = otherPlayer.transform;
            }
        }

        // Find AI enemies (these should ideally have a different tag, e.g., "Enemy")
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
    }
}