using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PanelDisaper : MonoBehaviour
{
    public GameObject panelBasique; 
    public GameObject panelLevel; 
    public GameObject panelMulti; 

    [Header("Boutons correspondant aux maps")]
    public Button[] mapButtons;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        panelBasique.SetActive(true);  
        panelLevel.SetActive(false);  
        panelMulti.SetActive(false); 

        int levelUnlocked = PlayerPrefs.GetInt("LevelUnlocked", 1); // Par défaut, seule la map 1 est débloquée

        for (int i = 0; i < mapButtons.Length; i++)
        {
            if (i < levelUnlocked)
                mapButtons[i].interactable = true;
            else
                mapButtons[i].interactable = false;
        } 
    }

    public void Basique()
    {
        panelBasique.SetActive(true);  
        panelLevel.SetActive(false);  
        panelMulti.SetActive(false);  
    }

    public void Level()
    {
        panelBasique.SetActive(false);  
        panelLevel.SetActive(true);  
        panelMulti.SetActive(false);  
    }

    public void Multi()
    {
        panelBasique.SetActive(false);  
        panelLevel.SetActive(false);  
        panelMulti.SetActive(true);  
    }

    public void UnlockNextLevel()
    {
        int currentLevel = PlayerPrefs.GetInt("LevelUnlocked", 1);
        PlayerPrefs.SetInt("LevelUnlocked", currentLevel + 1);
    }

    

    // Update is called once per frame
    void Update()
    {
        
    }
}