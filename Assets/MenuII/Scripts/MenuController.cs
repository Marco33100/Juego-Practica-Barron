using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Adaptacion del MenuManager del profesor para los cuatro paneles de la actividad II.</summary>
public class MenuController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject scoresPanel;
    public GameObject creditsPanel;

    [Header("Botones: Jugar, Ajustes, Puntuaciones, Creditos, Salir")]
    public Button[] menuButtons;
    public Button[] backButtons;

    [Header("Ajustes")]
    public TMP_InputField playerNameField;
    public TMP_Dropdown qualityDropdown;
    public Slider brightnessSlider;
    public Toggle fullscreenToggle;
    public Slider volumeSlider;
    public TMP_Text brightnessValue;
    public TMP_Text volumeValue;

    [Header("Escena de juego")]
    public string gameSceneName = "Nivel1";

    [Header("Audio de UI opcional, como en el ejemplo del profesor")]
    public AudioSource uiAudio;
    public AudioClip clickClip;

    public const string NameKey = "MenuII.PlayerName";
    public const string QualityKey = "MenuII.Quality";
    public const string BrightnessKey = "MenuII.Brightness";
    public const string FullscreenKey = "MenuII.Fullscreen";
    public const string VolumeKey = "MasterVolume";

    private void Awake()
    {
        Time.timeScale = 1;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        menuButtons[0].onClick.AddListener(() => { Click(); Play(); });
        menuButtons[1].onClick.AddListener(() => { Click(); Show(settingsPanel); });
        menuButtons[2].onClick.AddListener(() => { Click(); Show(scoresPanel); });
        menuButtons[3].onClick.AddListener(() => { Click(); Show(creditsPanel); });
        menuButtons[4].onClick.AddListener(() => { Click(); Quit(); });
        foreach (var button in backButtons)
            button.onClick.AddListener(() => { Click(); Show(mainPanel); SaveSettings(); });

        playerNameField.characterLimit = 16;
        playerNameField.SetTextWithoutNotify(PlayerPrefs.GetString(NameKey, ""));
        qualityDropdown.SetValueWithoutNotify(Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, 2), 0, 3));
        brightnessSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(BrightnessKey, 0.8f));
        fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FullscreenKey, 1) == 1);
        volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(VolumeKey, 1));

        playerNameField.onEndEdit.AddListener(SetPlayerName);
        qualityDropdown.onValueChanged.AddListener(SetQuality);
        brightnessSlider.onValueChanged.AddListener(SetBrightness);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        volumeSlider.onValueChanged.AddListener(SetVolume);
        SetQuality(qualityDropdown.value);
        SetBrightness(brightnessSlider.value);
        SetFullscreen(fullscreenToggle.isOn);
        SetVolume(volumeSlider.value);
        Show(mainPanel);
    }

    private void Start() => Select(menuButtons[0].gameObject);

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Show(mainPanel);
    }

    public void Show(GameObject target)
    {
        mainPanel.SetActive(target == mainPanel);
        settingsPanel.SetActive(target == settingsPanel);
        scoresPanel.SetActive(target == scoresPanel);
        creditsPanel.SetActive(target == creditsPanel);
        Select(target == mainPanel ? menuButtons[0].gameObject :
            target == settingsPanel ? playerNameField.gameObject :
            target == scoresPanel ? backButtons[1].gameObject : backButtons[2].gameObject);
    }

    private void Play()
    {
        SaveSettings();
        SceneManager.LoadScene(gameSceneName);
    }

    private void SetPlayerName(string value)
    {
        string name = (value ?? "").Trim();
        if (name.Length > 16) name = name.Substring(0, 16);
        playerNameField.SetTextWithoutNotify(name);
        PlayerPrefs.SetString(NameKey, name);
    }

    private void SetQuality(int value)
    {
        QualitySettings.SetQualityLevel(value, true);
        PlayerPrefs.SetInt(QualityKey, value);
    }

    private void SetBrightness(float value)
    {
        BrilloGlobal.Apply(value);
        brightnessValue.text = Mathf.RoundToInt(value * 100) + " %";
        PlayerPrefs.SetFloat(BrightnessKey, value);
    }

    private void SetFullscreen(bool enabled)
    {
#if !UNITY_EDITOR
        Screen.fullScreen = enabled;
#endif
        PlayerPrefs.SetInt(FullscreenKey, enabled ? 1 : 0);
    }

    private void SetVolume(float value)
    {
        AudioListener.volume = value;
        volumeValue.text = Mathf.RoundToInt(value * 100) + " %";
        PlayerPrefs.SetFloat(VolumeKey, value);
    }

    private void SaveSettings()
    {
        SetPlayerName(playerNameField.text);
        PlayerPrefs.Save();
    }

    private void Click()
    {
        if (uiAudio != null && clickClip != null) uiAudio.PlayOneShot(clickClip);
    }

    private void Quit()
    {
        SaveSettings();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
    }
}
