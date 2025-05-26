using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class MiniOptionMenu : MonoBehaviour
{
    public GameObject panel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        panel.SetActive(false); 
    }

    public void Panel()
    {
        panel.SetActive(true);
    }

    public void BackOption()
    {
        panel.SetActive(false); 
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
