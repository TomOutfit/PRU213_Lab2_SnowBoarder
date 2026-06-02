using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Level1 Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource boardingSource;

    [Header("Scene BGM Clips")]
    public AudioClip menuBGM;
    public AudioClip level1BGM;
    public AudioClip level2BGM;
    public AudioClip level3BGM;

    [Header("Level1 Audio Clips")]
    [SerializeField] AudioClip jumpClip;
    [SerializeField] AudioClip crashClip;
    [SerializeField] AudioClip collectClip;
    [SerializeField] AudioClip trickClip;

    [Header("MainMenu Audio Sources (Legacy)")]
    public AudioSource musicSource;

    [Header("MainMenu Audio Clips (Legacy)")]
    public AudioClip menuMusicClip;
    public AudioClip gameplayMusic;
    public AudioClip crashSFX;
    public AudioClip finishSFX;
    public AudioClip collectSFX;
    public AudioClip powerUpSFX;
    public AudioClip menuSelectSFX;

    [Header("Volume")]
    public float masterVolume = 1f;
    public float sfxVolume = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    public void PlayJumpSound()
    {
        AudioClip clip = jumpClip != null ? jumpClip : crashSFX;
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    public void PlayCrashSound()
    {
        AudioClip clip = crashClip != null ? crashClip : crashSFX;
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    public void PlayCollectSound()
    {
        AudioClip clip = collectClip != null ? collectClip : collectSFX;
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    public void PlayTrickSuccessSound()
    {
        AudioClip clip = trickClip != null ? trickClip : powerUpSFX;
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    public void PlayFinishSound()
    {
        if (finishSFX != null && sfxSource != null) sfxSource.PlayOneShot(finishSFX);
    }

    public void PauseBoardingSound()
    {
        if (boardingSource != null && boardingSource.isPlaying)
            boardingSource.Pause();
    }

    public void ResumeBoardingSound()
    {
        if (boardingSource != null && !boardingSource.isPlaying)
            boardingSource.Play();
    }

    public void PlayMenuMusic()
    {
        if (musicSource != null && menuMusicClip != null)
        {
            musicSource.clip = menuMusicClip;
            musicSource.Play();
        }
        else if (bgmSource != null)
        {
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        PlayBGMForScene(scene.name);
    }

    public void PlayBGMForScene(string sceneName)
    {
        if (bgmSource == null) return;
        
        AudioClip clipToPlay = null;
        if (sceneName == "MainMenu") clipToPlay = menuBGM;
        else if (sceneName == "Level1") clipToPlay = level1BGM;
        else if (sceneName == "Level2") clipToPlay = level2BGM;
        else if (sceneName == "Level3") clipToPlay = level3BGM;
        
        if (clipToPlay != null)
        {
            if (bgmSource.clip != clipToPlay || !bgmSource.isPlaying)
            {
                // Đảm bảo bật lặp lại (Loop) cho BGM
                bgmSource.loop = true;

                // Dừng các nguồn phát âm thanh cũ/lỗi thời nếu có để tránh đè nhạc
                if (musicSource != null && musicSource.isPlaying)
                {
                    musicSource.Stop();
                }

                bgmSource.clip = clipToPlay;
                bgmSource.Play();
            }
        }
    }
}
