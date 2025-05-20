using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DialogueManager1 : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public string[] dialogueLines;
    public float typingSpeed = 0.05f;
    public float delayBeforeNextLine = 2f;
    public float triggerDistance = 3f;

    private int index = 0;
    private bool isTyping = false;
    private bool dialogueStarted = false;

    private GameObject player;
    private GameObject marchant;

    void Start()
    {
        // Trouver le joueur (tag "Player") et le marchant (tag "Marchant")
        player = GameObject.FindGameObjectWithTag("Player");
        marchant = GameObject.FindGameObjectWithTag("Marchant");

        dialoguePanel.SetActive(false); // on cache au début
    }

    void Update()
    {
        if (!dialogueStarted && player != null && marchant != null)
        {
            float distance = Vector3.Distance(player.transform.position, marchant.transform.position);
            if (distance <= triggerDistance)
            {
                dialogueStarted = true;
                dialoguePanel.SetActive(true);
                StartCoroutine(TypeLine());
            }
        }
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
            dialoguePanel.SetActive(false); // cache le panel à la fin
        }
    }
}
