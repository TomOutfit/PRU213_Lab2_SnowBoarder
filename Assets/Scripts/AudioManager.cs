using UnityEngine;

/// <summary>
/// Audio manager for handling all game sounds and music.
/// Manages background music, sound effects, and dynamic audio based on gameplay.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;
    
    [Header("Music")]
    [SerializeField] private AudioClip[] gameplayMusic;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameOverMusic;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip[] jumpSounds;
    [SerializeField] private AudioClip[] landingSounds;
    [SerializeField] private AudioClip[] crashSounds;
    [SerializeField] private AudioClip[] collectSounds;
    [SerializeField] private AudioClip[] trickSounds;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip powerUpSound;
    
    [Header("Settings")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float musicVolume = 0.8f;
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private bool muteOnPause = true;
    
    private AudioClip currentMusic;
    private bool isPlaying;
    
    public float MasterVolume
    {
        get => masterVolume;
        set { masterVolume = Mathf.Clamp01(value); UpdateVolumes(); }
    }
    
    public float MusicVolume
    {
        get => musicVolume;
        set { musicVolume = Mathf.Clamp01(value); UpdateVolumes(); }
    }
    
    public float SFXVolume
    {
        get => sfxVolume;
        set { sfxVolume = Mathf.Clamp01(value); UpdateVolumes(); }
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        LoadAudioSettings();
        InitializeAudioSources();
    }
    
    private void LoadAudioSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }
    
    private void InitializeAudioSources()
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
        
        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
        }
        
        UpdateVolumes();
    }
    
    private void UpdateVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = masterVolume * musicVolume;
        }
        
        if (sfxSource != null)
        {
            sfxSource.volume = masterVolume * sfxVolume;
        }
        
        if (ambientSource != null)
        {
            ambientSource.volume = masterVolume * musicVolume * 0.5f;
        }
    }
    
    #region Music
    
    public void PlayGameplayMusic()
    {
        if (gameplayMusic != null && gameplayMusic.Length > 0)
        {
            AudioClip clip = gameplayMusic[Random.Range(0, gameplayMusic.Length)];
            PlayMusic(clip);
        }
    }
    
    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }
    
    public void PlayGameOverMusic()
    {
        PlayMusic(gameOverMusic);
    }
    
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        
        if (musicSource.clip != clip || !musicSource.isPlaying)
        {
            musicSource.clip = clip;
            musicSource.Play();
            currentMusic = clip;
        }
    }
    
    public void StopMusic()
    {
        musicSource.Stop();
        currentMusic = null;
    }
    
    public void PauseMusic()
    {
        musicSource.Pause();
    }
    
    public void ResumeMusic()
    {
        musicSource.UnPause();
    }
    
    public void FadeMusic(float duration, float targetVolume = 0f)
    {
        StartCoroutine(FadeMusicCoroutine(duration, targetVolume));
    }
    
    private System.Collections.IEnumerator FadeMusicCoroutine(float duration, float targetVolume)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }
        
        musicSource.volume = targetVolume;
        
        if (targetVolume <= 0f)
        {
            musicSource.Stop();
        }
    }
    
    #endregion
    
    #region Sound Effects
    
    public void PlayJumpSound()
    {
        if (jumpSounds != null && jumpSounds.Length > 0)
        {
            PlaySFX(jumpSounds[Random.Range(0, jumpSounds.Length)]);
        }
    }
    
    public void PlayLandingSound()
    {
        if (landingSounds != null && landingSounds.Length > 0)
        {
            PlaySFX(landingSounds[Random.Range(0, landingSounds.Length)]);
        }
    }
    
    public void PlayCrashSound()
    {
        if (crashSounds != null && crashSounds.Length > 0)
        {
            PlaySFX(crashSounds[Random.Range(0, crashSounds.Length)]);
        }
    }
    
    public void PlayCollectSound()
    {
        if (collectSounds != null && collectSounds.Length > 0)
        {
            PlaySFX(collectSounds[Random.Range(0, collectSounds.Length)]);
        }
    }
    
    public void PlayTrickSound()
    {
        if (trickSounds != null && trickSounds.Length > 0)
        {
            PlaySFX(trickSounds[Random.Range(0, trickSounds.Length)]);
        }
    }
    
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSound);
    }
    
    public void PlayPowerUp()
    {
        PlaySFX(powerUpSound);
    }
    
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }
    
    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, masterVolume * sfxVolume * volumeScale);
        }
    }
    
    #endregion
    
    #region Ambient
    
    public void PlayAmbient(AudioClip clip)
    {
        if (ambientSource != null && clip != null)
        {
            ambientSource.clip = clip;
            ambientSource.Play();
        }
    }
    
    public void StopAmbient()
    {
        if (ambientSource != null)
        {
            ambientSource.Stop();
        }
    }
    
    #endregion
    
    #region Game State Integration
    
    public void OnGameStarted()
    {
        PlayGameplayMusic();
    }
    
    public void OnGamePaused()
    {
        if (muteOnPause)
        {
            PauseMusic();
        }
    }
    
    public void OnGameResumed()
    {
        if (muteOnPause)
        {
            ResumeMusic();
        }
    }
    
    public void OnGameOver()
    {
        FadeMusic(1f, 0.5f);
        Invoke(nameof(PlayGameOverMusic), 1f);
    }
    
    public void OnMainMenu()
    {
        FadeMusic(0.5f, 0f);
        Invoke(nameof(PlayMenuMusic), 0.5f);
    }
    
    #endregion
    
    public void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
