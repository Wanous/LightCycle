using UnityEngine;
using UnityEngine.UI;

public class PlaySoundOnClick : MonoBehaviour
{
    [Header("Boutons correspondant aux maps")]
    public Button[] Boutons;         // Le bouton à cliquer
    public AudioSource audioSource; // L'audio source qui joue le son

    void Start()
    {
        foreach(var button in Boutons)
        {
            if (button != null && audioSource != null)
            {
                button.onClick.AddListener(PlaySound);
            }
            else
            {
                Debug.LogWarning("Button ou AudioSource non assigné dans l'inspecteur !");
            }
        }
        
    }

    void PlaySound()
    {
        audioSource.Play();
    }
}
