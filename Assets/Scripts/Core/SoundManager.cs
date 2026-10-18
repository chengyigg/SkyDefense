using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("音量")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    [Header("SFX 同时播放上限")]
    public int maxConcurrentSfx = 32;

    private Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
    private Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>();
    private List<AudioSource> _sfxPool = new List<AudioSource>();
    private AudioSource _bgmSource;
    private float _lowHealthTimer = 0f;
    private bool _lowHealthPlaying = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllClips();
        CreateSfxPool();
        CreateBgmSource();

        PlayBgm();
    }

    void LoadAllClips()
    {
        AudioClip[] all = Resources.LoadAll<AudioClip>("Audio");
        foreach (var c in all)
        {
            if (!_clips.ContainsKey(c.name))
                _clips[c.name] = c;
        }
        Debug.Log($"[SoundManager] 加载了 {_clips.Count} 个音频");
    }

    void CreateSfxPool()
    {
        for (int i = 0; i < maxConcurrentSfx; i++)
        {
            GameObject g = new GameObject($"SfxSource_{i}");
            g.transform.SetParent(transform);
            var src = g.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.volume = sfxVolume;
            _sfxPool.Add(src);
        }
    }

    void CreateBgmSource()
    {
        GameObject g = new GameObject("BgmSource");
        g.transform.SetParent(transform);
        _bgmSource = g.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.volume = bgmVolume;
        _bgmSource.playOnAwake = false;
    }

    public void Play(string clipName, float volumeScale = 1f)
    {
        if (!_clips.TryGetValue(clipName, out var clip)) return;

        if (_lastPlayTime.TryGetValue(clipName, out float lastTime))
        {
            if (Time.time - lastTime < 0.05f) return;
        }
        _lastPlayTime[clipName] = Time.time;

        AudioSource free = null;
        foreach (var s in _sfxPool)
            if (!s.isPlaying) { free = s; break; }

        if (free == null) free = _sfxPool[0];

        free.volume = sfxVolume * volumeScale;
        free.PlayOneShot(clip);
    }

    public void PlayBgm(string name = "bgm")
    {
        if (_clips.TryGetValue(name, out var clip))
        {
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }
    }

    public void StopBgm() { _bgmSource.Stop(); }

    public void SetLowHealthActive(bool active)
    {
        _lowHealthPlaying = active;
    }

    void Update()
    {
        if (!_lowHealthPlaying) return;

        _lowHealthTimer -= Time.deltaTime;
        if (_lowHealthTimer <= 0f)
        {
            Play("低血量警告");
            _lowHealthTimer = 1.5f;
        }
    }
}