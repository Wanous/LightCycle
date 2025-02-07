using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CameraMovement : NetworkBehaviour
{
    private const float YMin = -50.0f; // Angle min 
    private const float YMax = 50.0f; // Angle max
    private const float FloorHeight = 0.5f;  // Hauteur minimum de la camera

    public Transform lookAt;
    public Transform Player;
    public float distance = 10.0f; // Distance de la camera du joueur
    private float currentX = 0.0f;
    private float currentY = 0.0f;
    public float sensitivity = 50.0f; // Vitesse de rotation de la camera

    private bool isCameraMoving = false;
    private bool isFocusedOnPlayer = true;

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
        {
            return;
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

        if (Input.GetKeyDown(KeyCode.F))
        {
            isFocusedOnPlayer = !isFocusedOnPlayer;
        }
    }

    void LateUpdate()
    {
        if (!isLocalPlayer)
        {
            return;
        }

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

            Vector3 direction = new Vector3(0, 0, -distance);
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

            Vector3 desiredPosition = lookAt.position + rotation * direction;
            desiredPosition.y = Mathf.Max(desiredPosition.y, FloorHeight);

            transform.position = desiredPosition;
            transform.LookAt(lookAt.position);
        }
    }
}
