using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("SFX")]
    [SerializeField] private AudioClip crashSFX;
    [SerializeField] private AudioClip finishSFX;
    [SerializeField] private AudioClip collectSFX;
    [SerializeField] private AudioClip powerUpSFX;
    [SerializeField] private AudioClip menuSelectSFX;

    [Header("Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private float internalMasterVolume = 1f;
    private float internalMusicVolume = 1f;
    private float internalSFXVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();
        SetupAudioSources();
    }

    private void Start()
    {
        ApplyVolumes();
        if (menuMusic != null && musicSource != null)
            PlayMusic(menuMusic);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void SetupAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        ApplyVolumes();
    }

    private void LoadVolumes()
    {
        internalMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        internalMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        internalSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = internalMasterVolume * internalMusicVolume * masterVolume;
        if (sfxSource != null)
            sfxSource.volume = internalMasterVolume * internalSFXVolume * sfxVolume;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            PlayMusic(menuMusic);
        }
        else if (GameManager.Instance?.CurrentState == GameManager.GameState.Playing)
        {
            PlayMusic(gameplayMusic);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PauseMusic()
    {
        if (musicSource != null)
            musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
            musicSource.UnPause();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, internalMasterVolume * internalSFXVolume);
    }

    public void PlayCrashSFX()
    {
        if (crashSFX != null)
            PlaySFX(crashSFX);
        else
            PlaySFX(collectSFX);
    }

    public void PlayFinishSFX()
    {
        if (finishSFX != null)
            PlaySFX(finishSFX);
    }

    public void PlayCollectSFX()
    {
        if (collectSFX != null)
            PlaySFX(collectSFX);
    }

    public void PlayPowerUpSFX()
    {
        if (powerUpSFX != null)
            PlaySFX(powerUpSFX);
    }

    public void PlayMenuSelectSFX()
    {
        if (menuSelectSFX != null)
            PlaySFX(menuSelectSFX);
    }

    public void SetMasterVolume(float volume)
    {
        internalMasterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MasterVolume", internalMasterVolume);
        ApplyVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        internalMusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MusicVolume", internalMusicVolume);
        ApplyVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        internalSFXVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", internalSFXVolume);
        ApplyVolumes();
    }

    public float GetMasterVolume() => internalMasterVolume;
    public float GetMusicVolume() => internalMusicVolume;
    public float GetSFXVolume() => internalSFXVolume;
}
