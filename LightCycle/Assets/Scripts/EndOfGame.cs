using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EndOfGame : MonoBehaviour
{
    public GameObject panel;
    public Manager manager;

    private string sceneName;

    void Start()
    {
        panel.SetActive(false);
        sceneName = SceneManager.GetActiveScene().name;
        manager = GameObject.FindGameObjectWithTag("Manager").GetComponent<Manager>();
    }

    void Update()
    {
        panel.SetActive(!ManagerIsActive());
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

    public bool ManagerIsActive()
    {
        return manager.IsActive;
    }
}
