using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Setting : MonoBehaviour
{
    public static Setting Instance { get; private set; }

    [Header("Music Settings")]
    public bool Music = true;
    public float Volume = 50f;
    private Slider VolumeSlider;

    [Header("Sensitivity Settings")]
    public float Sensitive = 70f;
    private Slider SensitiveSlider;

    [Header("Bruitage Settings")]
    public float Bruitage = 70f;
    private Slider BruitageSlider;

    [Header("Invert Controls")]
    public bool Invert = false;
    private Button InvertButtonUI; // Reference to the Button UI element

    [Header("Levels")] 
    public int unlocked = 1;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // persist across scenes
            SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe to scene loaded event
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe to prevent memory leaks
    }

    // This method will be called every time a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find and link the UI elements in the newly loaded scene
        FindAndInitializeUI();
    }

    private void FindAndInitializeUI()
    {
        // Find sliders by name in the current scene
        GameObject volumeObj = GameObject.Find("musique");
        GameObject sensitiveObj = GameObject.Find("sensibilite");
        GameObject bruitageObj = GameObject.Find("bruitages");
        GameObject invertButtonObj = GameObject.Find("inverser"); // Find the invert button

        if (volumeObj != null)
        {
            VolumeSlider = volumeObj.GetComponent<Slider>();
            if (VolumeSlider != null)
            {
                VolumeSlider.value = Volume;
                VolumeSlider.onValueChanged.RemoveAllListeners();
                VolumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
                Debug.Log("Volume slider found and initialized.");
            }
            else
            {
                Debug.LogError("Component Slider not found on GameObject 'musique'.");
            }
        }
        else
        {
            Debug.LogWarning("Slider named 'musique' not found.");
            VolumeSlider = null;
        }

        if (sensitiveObj != null)
        {
            SensitiveSlider = sensitiveObj.GetComponent<Slider>();
            if (SensitiveSlider != null)
            {
                SensitiveSlider.value = Sensitive;
                SensitiveSlider.onValueChanged.RemoveAllListeners();
                SensitiveSlider.onValueChanged.AddListener(OnSensitiveSliderChanged);
                Debug.Log("Sensitivity slider found and initialized.");
            }
            else
            {
                Debug.LogError("Component Slider not found on GameObject 'sensibilite'.");
            }
        }
        else
        {
            Debug.LogWarning("Slider named 'sensibilite' not found.");
            SensitiveSlider = null;
        }

        if (bruitageObj != null)
        {
            BruitageSlider = bruitageObj.GetComponent<Slider>();
            if (BruitageSlider != null)
            {
                BruitageSlider.value = Bruitage;
                BruitageSlider.onValueChanged.RemoveAllListeners();
                BruitageSlider.onValueChanged.AddListener(OnBruitageSliderChanged);
                Debug.Log("Bruitage slider found and initialized.");
            }
            else
            {
                Debug.LogError("Component Slider not found on GameObject 'bruitage'.");
            }
        }
        else
        {
            Debug.LogWarning("Slider named 'bruitage' not found.");
            BruitageSlider = null;
        }

        // Handle the invert button
        if (invertButtonObj != null)
        {
            InvertButtonUI = invertButtonObj.GetComponent<Button>();
            if (InvertButtonUI != null)
            {
                InvertButtonUI.onClick.RemoveAllListeners();
                InvertButtonUI.onClick.AddListener(OnInvertButtonClicked); // Listen for button clicks
                Debug.Log("Invert button found and initialized.");
            }
            else
            {
                Debug.LogError("Component Button not found on GameObject 'inverser'.");
            }
        }
        else
        {
            Debug.LogWarning("Button named 'inverser' not found.");
            InvertButtonUI = null;
        }
    }

    public void OnVolumeSliderChanged(float value)
    {
        Volume = value;
        Debug.Log("Volume changed to: " + Volume);
    }

    public void OnSensitiveSliderChanged(float value)
    {
        Sensitive = value;
        Debug.Log("Sensitivity changed to: " + Sensitive);
    }

    public void OnBruitageSliderChanged(float value)
    {
        Bruitage = value;
        Debug.Log("Bruitage changed to: " + Bruitage);
    }

    // New method for the invert button click
    public void OnInvertButtonClicked()
    {
        Invert = !Invert; // Toggle the Invert value
        Debug.Log("Invert toggled via button: " + (Invert ? "ON" : "OFF"));
    }

    public void MusicToggle()
    {
        Music = !Music;
        Debug.Log("Music toggled: " + (Music ? "ON" : "OFF"));
    }

    public void UpdateUnlocked(int value)
    {
        unlocked += value;
    }
}