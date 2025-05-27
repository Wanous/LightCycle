using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Setting : MonoBehaviour
{
    public static Setting Instance { get; private set; }
    public bool Music = true;
    public float Volume = 50f;
    private Slider VolumeSlider;
    public float Sensitive = 70f;
    private Slider SensitiveSlider;
    public float Bruitage = 70f;
    private Slider BruitageSlider;
    public bool Invert = false;
    private Button InvertButtonUI;
    private RawImage InvertButtonRawImage;
    [Header("Invert Button Textures")]
    public Color InvertOffTexture;
    public Color InvertOnTexture;
    public int unlocked = 1;
    public Manager manager;

    private void Awake()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        if (manager == null) Debug.LogError("Manager not found",this);
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ChangeDifficulty(Difficulty difficulty)// To Change difficulty
    {
        manager.ChangeDifficulty(difficulty);
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndInitializeUI();
    }

    private void FindAndInitializeUI()
    {
        GameObject volumeObj = GameObject.Find("Slider Musique");
        GameObject sensitiveObj = GameObject.Find("Slider Sensibilité Camera");
        GameObject bruitageObj = GameObject.Find("Slider Bruitages");
        GameObject invertButtonObj = GameObject.Find("Bouton Inversé");
        GameObject imageButtonObj = GameObject.Find("Image Bouton");

        if (volumeObj != null)
        {
            VolumeSlider = volumeObj.GetComponent<Slider>();
            if (VolumeSlider != null)
            {
                VolumeSlider.value = Volume;
                VolumeSlider.onValueChanged.RemoveAllListeners();
                VolumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            }
        }

        if (sensitiveObj != null)
        {
            SensitiveSlider = sensitiveObj.GetComponent<Slider>();
            if (SensitiveSlider != null)
            {
                SensitiveSlider.value = Sensitive;
                SensitiveSlider.onValueChanged.RemoveAllListeners();
                SensitiveSlider.onValueChanged.AddListener(OnSensitiveSliderChanged);
            }
        }

        if (bruitageObj != null)
        {
            BruitageSlider = bruitageObj.GetComponent<Slider>();
            if (BruitageSlider != null)
            {
                BruitageSlider.value = Bruitage;
                BruitageSlider.onValueChanged.RemoveAllListeners();
                BruitageSlider.onValueChanged.AddListener(OnBruitageSliderChanged);
            }
        }

        if (invertButtonObj != null)
        {
            InvertButtonUI = invertButtonObj.GetComponent<Button>();
            if (InvertButtonUI != null)
            {
                InvertButtonUI.onClick.RemoveAllListeners();
                InvertButtonUI.onClick.AddListener(OnInvertButtonClicked);
            }
        }

        if (imageButtonObj != null)
        {
            InvertButtonRawImage = imageButtonObj.GetComponent<RawImage>();
            if (InvertButtonRawImage != null)
            {
                UpdateInvertButtonVisual();
            }
        }
    }

    public void OnVolumeSliderChanged(float value)
    {
        Volume = value;
    }

    public void OnSensitiveSliderChanged(float value)
    {
        Sensitive = value;
    }

    public void OnBruitageSliderChanged(float value)
    {
        Bruitage = value;
    }

    public void OnInvertButtonClicked()
    {
        Invert = !Invert;
        UpdateInvertButtonVisual();
    }

    private void UpdateInvertButtonVisual()
    {
        if (InvertButtonRawImage != null)
        {
            if (Invert)
            {
                InvertButtonRawImage.color = InvertOnTexture;
            }
            else
            {
                InvertButtonRawImage.color = InvertOffTexture;
            }
        }
    }

    public void MusicToggle()
    {
        Music = !Music;
    }

    public void UpdateUnlocked(int value)
    {
        unlocked = value > unlocked ? value : unlocked;
    }
}