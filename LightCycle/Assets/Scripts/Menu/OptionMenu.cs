using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class OptionMenu : MonoBehaviour
{
    public Transform playerBody;
    private bool isMenuOpen = false;
    

    void Start()
    {
    }

    void Update()
    {
        // Gère l'ouverture/fermeture du menu avec ÉCHAP
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isMenuOpen = !isMenuOpen;
            if (isMenuOpen)
                OpenMenu();
        }
    }

    

    public void OpenMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}

