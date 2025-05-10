using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelMenu : MonoBehaviour
{
    public Button[] buttons;
    private int currentUnlockedLevel;

    private void Start() // Use Start instead of Awake for potential order of operations
    {
        UpdateLevelButtons();
        currentUnlockedLevel = Setting.Instance.unlocked;
    }

    private void Update()
    {
        if (Setting.Instance.unlocked != currentUnlockedLevel)
        {
            UpdateLevelButtons();
            currentUnlockedLevel = Setting.Instance.unlocked;
        }
    }

    private void UpdateLevelButtons()
    {
        int unlockLevel = Setting.Instance.unlocked;

        foreach (Button button in buttons)
        {
            button.interactable = false;
        }

        for (int i = 0; i < Mathf.Min(unlockLevel, buttons.Length); i++)
        {
            buttons[i].interactable = true;
        }
    }

    public void OpenLevel(int levelId)
    {
        string levelName = "Level" + levelId;
        Debug.Log("Loading scene: " + levelName);
        SceneManager.LoadScene(levelName);
    }

    public void OpenMulti(int levelId)
    {
        string levelName = "Multi" + levelId;
        Debug.Log("Loading scene: " + levelName);
        SceneManager.LoadScene(levelName);
    }

    public void Return()
    {
        Debug.Log("Loading scene: MainMenu");
        SceneManager.LoadScene("MainMenu");
    }
}