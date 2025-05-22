using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Required for TextMeshPro

public class DialogueMarchant : MonoBehaviour
{
    // Assign these GameObjects and TextMeshProUGUI components in the Unity Inspector
    public GameObject dialoguePanel1;
    public GameObject dialoguePanel2;
    public TextMeshProUGUI dialogueText1;
    public TextMeshProUGUI dialogueText2;

    // Array of dialogue lines to be displayed
    public string[] dialogueLines;

    // Speed at which characters are typed out
    public float typingSpeed = 0.05f;
    // Delay before moving to the next line after one is fully typed
    public float delayBeforeNextLine = 2f;
    // Distance at which the dialogue should trigger
    public float triggerDistance = 3f;

    // Private variables to manage dialogue state
    private int index = 0; // Current line index in dialogueLines
    private bool isTyping = false; // Flag to check if text is currently being typed
    private bool dialogueStarted = false; // Flag to ensure dialogue starts only once

    // Public references for Player and Marchant GameObjects
    // Assign these in the Inspector for reliability in builds!
    public GameObject player;
    public GameObject marchant;

    void Start()
    {
        // It's highly recommended to assign 'player' and 'marchant' directly in the Inspector.
        // If you must use FindGameObjectWithTag, ensure the objects are active and tagged correctly
        // at the time this Start() method is called in the build.
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("DialogueMarchant: Player GameObject not found! Ensure it's tagged 'Player'.");
            }
        }

        if (marchant == null)
        {
            marchant = GameObject.FindGameObjectWithTag("Marchant");
            if (marchant == null)
            {
                Debug.LogError("DialogueMarchant: Marchant GameObject not found! Ensure it's tagged 'Marchant'.");
            }
        }

        // Initially hide both dialogue panels
        if (dialoguePanel1 != null) dialoguePanel1.SetActive(false);
        if (dialoguePanel2 != null) dialoguePanel2.SetActive(false);
    }

    void Update()
    {
        // Check if dialogue hasn't started yet and both player and marchant are found
        if (!dialogueStarted && player != null && marchant != null)
        {
            // Calculate the distance between the player and the marchant
            float distance = Vector3.Distance(player.transform.position, marchant.transform.position);

            // If the player is within the trigger distance
            if (distance <= triggerDistance)
            {
                dialogueStarted = true; // Set flag to true to prevent re-triggering
                if (dialoguePanel1 != null)
                {
                    dialoguePanel1.SetActive(true); // Show the first dialogue panel
                    StartCoroutine(TypeLine()); // Start typing the first line
                }
                else
                {
                    Debug.LogError("DialogueMarchant: dialoguePanel1 is not assigned!");
                }
            }
        }
    }

    // Coroutine to type out each line of dialogue character by character
    IEnumerator TypeLine()
    {
        isTyping = true; // Set typing flag to true

        // Determine which panel and TextMeshProUGUI component to use
        // The last line uses dialoguePanel2 and dialogueText2
        TextMeshProUGUI currentText = null;

        if (index == dialogueLines.Length - 1)
        {
            // If it's the last line, switch to panel 2
            if (dialoguePanel1 != null) dialoguePanel1.SetActive(false);
            if (dialoguePanel2 != null) dialoguePanel2.SetActive(true);
            currentText = dialogueText2;
        }
        else
        {
            // Otherwise, use panel 1
            if (dialoguePanel1 != null) dialoguePanel1.SetActive(true);
            if (dialoguePanel2 != null) dialoguePanel2.SetActive(false); // Ensure panel 2 is off
            currentText = dialogueText1;
        }

        // Clear the current text before typing
        if (currentText != null)
        {
            currentText.text = "";

            // Type out each character with a delay
            foreach (char c in dialogueLines[index].ToCharArray())
            {
                currentText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
        }
        else
        {
            Debug.LogError("DialogueMarchant: Current TextMeshProUGUI component is not assigned!");
        }

        isTyping = false; // Typing finished

        // Wait for a delay before moving to the next line
        yield return new WaitForSeconds(delayBeforeNextLine);
        NextLine(); // Proceed to the next line
    }

    // Function to advance to the next dialogue line or end the dialogue
    void NextLine()
    {
        index++; // Increment line index

        // Check if there are more dialogue lines
        if (index < dialogueLines.Length)
        {
            StartCoroutine(TypeLine()); // Start typing the next line
        }
        else
        {
            // All dialogue lines have been displayed
            if (dialoguePanel1 != null) dialoguePanel1.SetActive(false); // Hide panel 1
            if (dialoguePanel2 != null) dialoguePanel2.SetActive(false); // Hide panel 2

            try
            {
                SceneManager.LoadScene("MainMenu");
            }
            catch (System.Exception e)
            {
                Debug.LogError("DialogueMarchant: Failed to load 'MainMenu' scene. Error: " + e.Message +
                               "\nEnsure 'MainMenu' is added to 'Scenes In Build' in File -> Build Settings.");
            }
        }
    }
}
