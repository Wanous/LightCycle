using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DialogueMarchant : MonoBehaviour
{
    public GameObject dialoguePanel1;
    public GameObject dialoguePanel2;
    public TextMeshProUGUI dialogueText1;
    public TextMeshProUGUI dialogueText2;
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
        player = GameObject.FindGameObjectWithTag("Player");
        marchant = GameObject.FindGameObjectWithTag("Marchant");

        dialoguePanel1.SetActive(false);
        dialoguePanel2.SetActive(false);
    }

    void Update()
    {
        if (!dialogueStarted && player != null && marchant != null)
        {
            float distance = Vector3.Distance(player.transform.position, marchant.transform.position);
            if (distance <= triggerDistance)
            {
                dialogueStarted = true;
                dialoguePanel1.SetActive(true);
                StartCoroutine(TypeLine());
            }
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;

        // Choisir le bon panel et texte
        if (index == dialogueLines.Length - 1)
        {
            dialoguePanel1.SetActive(false);
            dialoguePanel2.SetActive(true);
            dialogueText2.text = "";
        }
        else
        {
            dialogueText1.text = "";
        }

        TextMeshProUGUI currentText = index == dialogueLines.Length - 1 ? dialogueText2 : dialogueText1;

        foreach (char c in dialogueLines[index].ToCharArray())
        {
            currentText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        yield return new WaitForSeconds(delayBeforeNextLine);
        NextLine();
    }

    void NextLine()
    {
        index++;

        if (index < dialogueLines.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            dialoguePanel1.SetActive(false);
            dialoguePanel2.SetActive(false);
            SceneManager.LoadScene("MainMenu");
        }
    }
}