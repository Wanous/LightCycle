using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // pour utiliser GameObject UI

public class DialogueManager : MonoBehaviour
{
    public GameObject dialoguePanel;               // Le panel à cacher
    public TextMeshProUGUI dialogueText;           // Le texte
    public string[] dialogueLines;   
    public GameObject[] panels;               // Les phrases
    public float typingSpeed = 0.05f;              // Vitesse d'écriture
    public float delayBeforeNextLine = 2f;         // Délai avant ligne suivante

    private int index = 0;
    private bool isTyping = false;


    void Update()
    {
        foreach(var panel in panels)
        {
            while(panel.activeSelf)
            {
                dialoguePanel.SetActive(false);
                Time.timeScale = 0f;
            }
            
                dialoguePanel.SetActive(true);
                Time.timeScale = 1f;
            
        }
    }

    void Start()
    {
        dialoguePanel.SetActive(true);             // Affiche le panel au début
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in dialogueLines[index].ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        // Attendre un moment avant de passer à la ligne suivante
        yield return new WaitForSeconds(delayBeforeNextLine);
        NextLine();
    }

    void NextLine()
    {
        if (index < dialogueLines.Length - 1)
        {
            index++;
            StartCoroutine(TypeLine());
        }
        else
        {
            dialoguePanel.SetActive(false);        // Cache le panel à la fin
        }
    }
}
