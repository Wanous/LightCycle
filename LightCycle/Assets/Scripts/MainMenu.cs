using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene(2);
    }


    public void PlayOnlineGame()
    {
        SceneManager.LoadScene("Multi3");
    }


    public void QuitGame()
    {
        Application.Quit();
    }
}
