using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenu : MonoBehaviour
{

    public GameObject dialoguePanel1;
    public GameObject pauseMenu;
    public static bool IsPaused;

    void Start()
    {
        if (dialoguePanel1 != null) dialoguePanel1.SetActive(false);
    }
    public void PauseGame()
    {
        if (dialoguePanel1 != null) dialoguePanel1.SetActive(true);
        if (pauseMenu != null) pauseMenu.SetActive(true);
        Time.timeScale = 0f; //Stop le jeu (les updates/animations)
        IsPaused = true;
    }
    public void ResumeGame(){
        if (pauseMenu != null) pauseMenu.SetActive(false);
        Time.timeScale = 1f; //Relance le jeu (les updates/animations)
        IsPaused = false;
        if (dialoguePanel1 != null) dialoguePanel1.SetActive(false);
    }

    public void GoToMenu(){
        Time.timeScale = 1f; //Relance le jeu (les updates/animations)
        if (pauseMenu != null) pauseMenu.SetActive(false);
        SceneManager.LoadScene("MainMenu");
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            GoToMenu();
        }
        if(Input.GetKeyDown("p"))
        {
            PauseGame();
        }
    }
}
