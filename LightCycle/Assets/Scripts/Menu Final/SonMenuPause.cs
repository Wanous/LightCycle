using UnityEngine;

public class SoundControlOnPanel : MonoBehaviour
{
    [Header("Panel à surveiller")]
    public GameObject panel;

    [Header("Son à garder actif")]
    public AudioSource soundToKeep;

    private bool hasSilencedOthers = false;

    void Update()
    {
        if (panel.activeSelf && !hasSilencedOthers)
        {
            SilenceOtherSounds();
            hasSilencedOthers = true;
        }
        else if (!panel.activeSelf && hasSilencedOthers)
        {
            // Reset si le panel se referme
            hasSilencedOthers = false;
        }
    }

    void SilenceOtherSounds()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource audio in allAudioSources)
        {
            if (audio != soundToKeep)
            {
                audio.Stop(); // ou audio.Pause() si tu veux les reprendre plus tard
            }
        }

        if (!soundToKeep.isPlaying)
        {
            soundToKeep.Play();
        }
    }
}

