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
        panel.SetActive(false); // tu avais écrit dialoguePanel1, corrigé
        sceneName = SceneManager.GetActiveScene().name;
    }

    void Update()
    {
        if (FctArthur()) // appel correct de la fonction
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
        // Ici tu peux mettre ta vraie logique plus tard
        return false;
    }
}
