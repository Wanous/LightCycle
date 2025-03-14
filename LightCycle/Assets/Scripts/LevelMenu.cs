using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class LevelMenu : MonoBehaviour
{
    public Button[] buttons;
    public int unlockLevel_;
    private void Awake()
    {
        int unlockLevel = PlayerPrefs.GetInt("UnlockedLevel",1);
        foreach (var boutton in buttons){
                boutton.interactable = false;
        }

        if (unlockLevel_ >= buttons.Length){
            for (int i = 0; i < buttons.Length;i++){
                buttons[i].interactable = true;
            }
        }
        else{
            for (int i = 0; i < unlockLevel_;i++){
                buttons[i].interactable = true;
            }
        }
    }

    public void OpenLevel(int levelId){
        string levelName = "Level" + levelId;
        SceneManager.LoadScene(levelName);
    }
}
