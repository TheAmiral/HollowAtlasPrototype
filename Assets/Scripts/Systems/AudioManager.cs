using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    [Header("Music")]
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip bossMusic;

    [Header("Player SFX")]
    [SerializeField] private AudioClip playerDamageClip;
    [SerializeField] private AudioClip playerDeathClip;
    [SerializeField] private AudioClip dashClip;

    [Header("Combat SFX")]
    [SerializeField] private AudioClip enemyHitClip;
    [SerializeField] private AudioClip enemyDeathClip;
    [SerializeField] private AudioClip bossSpawnClip;
    [SerializeField] private AudioClip bossDeathClip;

    [Header("Pickup SFX")]
    [SerializeField] private AudioClip xpPickupClip;
    [SerializeField] private AudioClip goldPickupClip;
    [SerializeField] private AudioClip healthPickupClip;

    [Header("UI SFX")]
    [SerializeField] private AudioClip levelUpClip;
    [SerializeField] private AudioClip cardHoverClip;
    [SerializeField] private AudioClip cardSelectClip;
    [SerializeField] private AudioClip bossRewardClip;

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.55f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
    [Range(0f, 1f)] [SerializeField] private float uiVolume = 0.85f;

    private AudioClip currentMusicClip;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public float UiVolume => uiVolume;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSources();
            ApplyVolumes();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PlayGameplayMusic();
    }

    void EnsureAudioSources()
    {
        if (musicSource == null)
            musicSource = CreateAudioSource("MusicSource", true);

        if (sfxSource == null)
            sfxSource = CreateAudioSource("SfxSource", false);

        if (uiSource == null)
            uiSource = CreateAudioSource("UiSource", false);
    }

    AudioSource CreateAudioSource(string sourceName, bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;

        return source;
    }

    void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume;

        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;

        if (uiSource != null)
            uiSource.volume = masterVolume * uiVolume;
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetUiVolume(float value)
    {
        uiVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }

    public void PlayBossMusic()
    {
        PlayMusic(bossMusic);
    }

    public void StopMusic()
    {
        if (musicSource == null)
            return;

        musicSource.Stop();
        currentMusicClip = null;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null)
            return;

        if (currentMusicClip == clip && musicSource.isPlaying)
            return;

        currentMusicClip = clip;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = masterVolume * musicVolume;
        musicSource.Play();
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * masterVolume * sfxVolume);
    }

    public void PlayUi(AudioClip clip, float volumeScale = 1f)
    {
        if (uiSource == null || clip == null)
            return;

        uiSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * masterVolume * uiVolume);
    }

    public void PlayPlayerDamage()
    {
        PlaySfx(playerDamageClip);
    }

    public void PlayPlayerDeath()
    {
        PlaySfx(playerDeathClip);
    }

    public void PlayDash()
    {
        PlaySfx(dashClip);
    }

    public void PlayEnemyHit()
    {
        PlaySfx(enemyHitClip, 0.75f);
    }

    public void PlayEnemyDeath()
    {
        PlaySfx(enemyDeathClip);
    }

    public void PlayBossSpawn()
    {
        PlaySfx(bossSpawnClip);
        PlayBossMusic();
    }

    public void PlayBossDeath()
    {
        PlaySfx(bossDeathClip);
        PlayGameplayMusic();
    }

    public void PlayXpPickup()
    {
        PlaySfx(xpPickupClip, 0.65f);
    }

    public void PlayGoldPickup()
    {
        PlaySfx(goldPickupClip, 0.75f);
    }

    public void PlayHealthPickup()
    {
        PlaySfx(healthPickupClip);
    }

    public void PlayLevelUp()
    {
        PlayUi(levelUpClip);
    }

    public void PlayCardHover()
    {
        PlayUi(cardHoverClip, 0.55f);
    }

    public void PlayCardSelect()
    {
        PlayUi(cardSelectClip);
    }

    public void PlayBossReward()
    {
        PlayUi(bossRewardClip);
    }
}