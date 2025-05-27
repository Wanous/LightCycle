using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuLevel : MonoBehaviour
{
    public void Level1()
    {
        SceneManager.LoadScene("Level1");
    }

    public void Level2()
    {
        SceneManager.LoadScene("Level2");
    }

    public void Level3()
    {
        SceneManager.LoadScene("Level3");
    }

    public void Level4()
    {
        SceneManager.LoadScene("Leve4");
    }

    public void Multi2()
    {
        SceneManager.LoadScene("Multi2");
    }

    public void Multi3()
    {
        SceneManager.LoadScene("Multi3");
    }

    public void Multi4()
    {
        SceneManager.LoadScene("Multi4");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void Menu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void Options()
    {
        SceneManager.LoadScene("OptionMenu");
    }
}
