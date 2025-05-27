using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndOfGame : MonoBehaviour
{
    public GameObject panel;
    public Button menuButton;
    public Button retryButton;
    public Button nextLevelButton;
    public Manager manager;

    private string sceneName;
    private bool isGameOver = false;

    void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;
        manager = GameObject.FindGameObjectWithTag("Manager").GetComponent<Manager>();

        // Hide UI panel initially
        panel.SetActive(false);

        // Hook up button actions
        menuButton.onClick.AddListener(Menu);
        retryButton.onClick.AddListener(Retry);
        nextLevelButton.onClick.AddListener(NextLevel);

        ResumeGame();
    }

    void Update()
    {
        if (!manager.IsActive && !isGameOver)
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        isGameOver = true;

        Time.timeScale = 0f;
        panel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        nextLevelButton.gameObject.SetActive(true);

        if (Setting.Instance != null)
        {
            Setting.Instance.ClampCam = false;
            if (sceneName == "Level1") Setting.Instance.UpdateUnlocked(2);
            if (sceneName == "Level2") Setting.Instance.UpdateUnlocked(3);
            if (sceneName == "Level3") Setting.Instance.UpdateUnlocked(4);
        }
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Menu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void Retry()
    {
        ResumeGame();
        SceneManager.LoadScene(sceneName);
    }

    public void NextLevel()
    {
        ResumeGame();

        if (sceneName == "Level1") SceneManager.LoadScene("Level2");
        if (sceneName == "Level2") SceneManager.LoadScene("Level3");
        if (sceneName == "Level3") SceneManager.LoadScene("Level4"); 
    }
}
