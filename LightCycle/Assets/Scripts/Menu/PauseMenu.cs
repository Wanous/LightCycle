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
        dialoguePanel1.SetActive(false);
    }
    public void PauseGame()
    {
        dialoguePanel1.SetActive(true);
        pauseMenu.SetActive(true);
        Time.timeScale = 0f; //Stop le jeu (les updates/animations)
        IsPaused = true;
    }
    public void ResumeGame(){
        pauseMenu.SetActive(false);
        Time.timeScale = 1f; //Relance le jeu (les updates/animations)
        IsPaused = false;
        dialoguePanel1.SetActive(false);
    }

    public void GoToMenu(){
        Time.timeScale = 1f; //Relance le jeu (les updates/animations)
        pauseMenu.SetActive(false);
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
