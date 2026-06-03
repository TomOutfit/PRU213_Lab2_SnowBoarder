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

    [Header("Level Audio Clips")]
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
            return;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
        }

        LoadBGMClipsIfNull();
    }

    void Start()
    {
        PlayBGMForScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void LoadBGMClipsIfNull()
    {
        if (menuBGM == null) menuBGM = Resources.Load<AudioClip>("BGM/muzaproduction-snowboard-show-short-121373");
        if (level1BGM == null) level1BGM = Resources.Load<AudioClip>("BGM/muzaproduction-snowboard-show-121374");
        if (level2BGM == null) level2BGM = Resources.Load<AudioClip>("BGM/phantasticbeats-jazzy-snow-452072");
        if (level3BGM == null) level3BGM = Resources.Load<AudioClip>("BGM/villatic_music-christmas-snow-265212");

        if (crashSFX == null) crashSFX = Resources.Load<AudioClip>("SFx/Crash SFX");
        if (finishSFX == null) finishSFX = Resources.Load<AudioClip>("SFx/Finish SFX");
        if (menuSelectSFX == null) menuSelectSFX = Resources.Load<AudioClip>("SFx/click1");

        if (crashClip == null) crashClip = Resources.Load<AudioClip>("SFx/Crash SFX");
        if (jumpClip == null) jumpClip = Resources.Load<AudioClip>("SFx/switch1");
        if (collectClip == null) collectClip = Resources.Load<AudioClip>("SFx/click4");
        if (trickClip == null) trickClip = Resources.Load<AudioClip>("SFx/Finish SFX");
    }

    public void PlayMenuSelectSound()
    {
        if (menuSelectSFX != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(menuSelectSFX);
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

    public void PlaySlowDownSound()
    {
        AudioClip clip = jumpClip != null ? jumpClip : crashSFX;
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip, 0.6f);
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
        if (sceneName == "Menu") clipToPlay = menuBGM;
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

            // Dừng các AudioSource khác trong scene để tránh đè âm thanh (như AudioSource mặc định trong Menu)
            AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude);
            foreach (var src in allSources)
            {
                if (src != bgmSource && src != sfxSource && src != boardingSource)
                {
                    if (src.isPlaying)
                    {
                        src.Stop();
                    }
                }
            }
        }
        else
        {
            // Dừng nhạc nếu scene không có nhạc nền được chỉ định
            bgmSource.Stop();
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }
    }
}
