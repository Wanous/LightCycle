using UnityEngine;

public class Sounds : MonoBehaviour
{
    public AudioSource Music;

    [Range(0f, 1f)]
    public float DefaultVolume = 0.2f; // Set per scene in Inspector

    private bool customVolumeApplied = false;

    void Start()
    {
        if (Setting.Instance != null)
        {
            // Apply the per-scene default (but allow user override)
            if (Setting.Instance.Volume == 50f) // Assuming 50 is the global default
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
        UpdateVolume();
    }

    void UpdateVolume()
    {
        // Convert 0–100 Setting.Volume to 0–1 Audio volume
        Music.volume = Setting.Instance.Volume / 100f < DefaultVolume
            ? DefaultVolume - (DefaultVolume*100 - Setting.Instance.Volume) / 100f
            : DefaultVolume + Setting.Instance.Volume / 100f;
    }
}