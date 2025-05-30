using UnityEngine;

public class SoundsEffect : MonoBehaviour
{
    public AudioSource Music;

    [Range(0f, 1f)]
    public float DefaultVolume = 0.5f; // Set per scene in Inspector

    private bool customVolumeApplied = false;

    void Start()
    {
        if (Setting.Instance != null)
        {
            // Apply the per-scene default (but allow user override)
            if (Setting.Instance.Bruitage == 70f) // Assuming 50 is the global default
            {
                Music.volume = DefaultVolume;
                customVolumeApplied = true;
            }
            else
            {
                UpdateVolume();
            }

            Music.mute = !Setting.Instance.Music;
        }
    }

    void Update()
    {
        if (Setting.Instance != null && !customVolumeApplied)
        {
            UpdateVolume();
            Music.mute = !Setting.Instance.Music;
        }
    }

    void UpdateVolume()
    {
        // Convert 0–100 Setting.Volume to 0–1 Audio volume
        Music.volume = Setting.Instance.Bruitage / 100f < DefaultVolume
            ? DefaultVolume - (DefaultVolume*100 - Setting.Instance.Bruitage) / 100f
            : DefaultVolume + Setting.Instance.Bruitage / 100f;
    }
}