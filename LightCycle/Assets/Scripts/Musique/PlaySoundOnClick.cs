using UnityEngine;

public class PlaySoundOnClick : MonoBehaviour
{
    public Button button;           // Le bouton à cliquer
    public AudioSource audioSource; // L'audio source qui joue le son

    void Start()
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

    void PlaySound()
    {
        audioSource.Play();
    }
}
