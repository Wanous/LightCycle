using UnityEngine;
using Mirror;

public class cameraLobby : NetworkBehaviour
{
    private const float YMin = -50.0f;
    private const float YMax = 50.0f;
    private const float FloorHeight = 0.5f;

    [Header("Camera Settings")]
    public float distance = 5.0f;
    public float sensitivity = 50.0f;
    public bool invert = false;
    public LayerMask collisionMask;

    [Header("Mode Settings")]
    public float behindPlayerDistance = 5.0f;
    public float behindPlayerHeight = 2.0f;
    public float focusDistance = 5.0f;
    public float focusHeightOffset = 2.0f;

    private float currentX = 0.0f;
    private float currentY = 20.0f;
    private int inverted = 1;
    private Transform targetEnemy;
    private Camera playerCamera;
    private bool controlsEnabled = true;

    private enum CameraMode
    {
        BehindPlayer,
        FocusEnemyBetween
    }
    private CameraMode currentMode = CameraMode.BehindPlayer;

    void Start()
    {
        if (!isLocalPlayer)
        {
            Destroy(GetComponent<Camera>());
            Destroy(this);
            return;
        }

        playerCamera = GetComponent<Camera>();
        inverted = invert ? -1 : 1;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        HandleInput();
        HandleCursorState();
    }

    void LateUpdate()
    {
        if (!isLocalPlayer || !controlsEnabled) return;
        
        UpdateCameraPosition();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            CycleCameraMode();
        }
    }

    private void CycleCameraMode()
    {
        int modeCount = System.Enum.GetValues(typeof(CameraMode)).Length;
        int nextMode = (int)currentMode;

        // Cycle through modes until we find a valid one
        for (int i = 0; i < modeCount; i++)
        {
            nextMode = (nextMode + 1) % modeCount;
            CameraMode potentialMode = (CameraMode)nextMode;

            if (potentialMode == CameraMode.FocusEnemyBetween)
            {
                FindClosestEnemy();
                if (targetEnemy != null)
                {
                    currentMode = potentialMode;
                    return;
                }
            }
            else
            {
                currentMode = potentialMode;
                return;
            }
        }
    }

    private void UpdateCameraPosition()
    {
        switch (currentMode)
        {
            case CameraMode.BehindPlayer:
                UpdateBehindPlayerMode();
                break;
            case CameraMode.FocusEnemyBetween:
                UpdateFocusEnemyMode();
                break;
        }
    }

   
    private void UpdateBehindPlayerMode()
    {
        float targetRotationY = transform.parent.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0, targetRotationY, 0);
        Vector3 offset = new Vector3(0, behindPlayerHeight, -behindPlayerDistance);
        Vector3 desiredPosition = transform.parent.position + targetRotation * offset;

        HandleCameraCollision(desiredPosition);
        transform.LookAt(transform.parent.position + Vector3.up * 1.5f);
    }

    private void UpdateFocusEnemyMode()
    {
        if (targetEnemy == null)
        {
            currentMode = CameraMode.BehindPlayer;
            return;
        }

        Vector3 playerToEnemy = (targetEnemy.position - transform.parent.position).normalized;
        Vector3 desiredPosition = transform.parent.position - playerToEnemy * focusDistance + Vector3.up * focusHeightOffset;

        HandleCameraCollision(desiredPosition);
        transform.LookAt(targetEnemy.position);
    }

    private void HandleCameraCollision(Vector3 desiredPosition)
    {
        RaycastHit hit;
        if (Physics.Linecast(transform.parent.position, desiredPosition, out hit, collisionMask))
        {
            transform.position = hit.point;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 5f);
        }
    }

    private void HandleCursorState()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            controlsEnabled = !controlsEnabled;
            Cursor.lockState = controlsEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !controlsEnabled;
        }

        if (Input.GetMouseButtonDown(0) && !controlsEnabled)
        {
            controlsEnabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void FindClosestEnemy()
    {
        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        // Find players
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player == transform.parent.gameObject) continue;

            float distance = Vector3.Distance(transform.parent.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = player.transform;
            }
        }

        // Find AI enemies
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            float distance = Vector3.Distance(transform.parent.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = enemy.transform;
            }
        }

        targetEnemy = closestTarget;
    }
}