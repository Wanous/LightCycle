using UnityEngine;
using UnityEngine.UI;

public class OptionMenu : MonoBehaviour
{
    public float sensitivity = 100f;
    public Slider sensitivitySlider;
    public Transform playerBody;

    private float xRotation = 0f;
    private bool isMenuOpen = false;

    void Start()
    {
        LockCursor();

        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.AddListener(UpdateSensitivity);
            sensitivitySlider.value = sensitivity;
        }
    }

    void Update()
    {
        // Gère l'ouverture/fermeture du menu avec ÉCHAP
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isMenuOpen = !isMenuOpen;
            if (isMenuOpen)
                OpenMenu();
            else
                CloseMenu();
        }

        // Bloquer la caméra si le menu est ouvert
        if (isMenuOpen) return;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }

    public void UpdateSensitivity(float newSensitivity)
    {
        sensitivity = newSensitivity;
    }

    public void OpenMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        LockCursor();
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

