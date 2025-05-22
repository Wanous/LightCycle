using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EndOfGame : MonoBehaviour
{
    public GameObject panel;

    private string sceneName;

    void Start()
    {
        panel.SetActive(false);
        sceneName = SceneManager.GetActiveScene().name;
    }

    void Update()
    {
        if (FctArthur()) 
        {
            panel.SetActive(true);
        }
    }

    public void Menu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void Retry()
    {
        SceneManager.LoadScene(sceneName);
    }

    public void NextLevel()
    {
        SceneManager.LoadScene("Level2");
    }

    public bool FctArthur()
    {
        // fonction qui dit si oui ou non les enemies existent tjr. => renvoi un boolean true = plus d'enemis
        return false;
    }
}
