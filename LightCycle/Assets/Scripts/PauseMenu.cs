using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{

    public GameObject pauseMenu;
    public static bool IsPaused;

    public void PauseGame(){
        pauseMenu.SetActive(true);
        Time.timeScale = 0f; //Stop le jeu (les updates/animations)
        IsPaused = true;
    }
    public void ResumeGame(){
        pauseMenu.SetActive(false);
        Time.timeScale = 1f; //Relance le jeu (les updates/animations)
        IsPaused = false;
    }

    public void GoToMenu(){
        Time.timeScale = 1f; //Relance le jeu (les updates/animations)
        SceneManager.LoadScene(0);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)){
            GoToMenu();
            if(IsPaused){
                ResumeGame();
            }
            else{
                PauseGame();
            }
        }
    }
}
