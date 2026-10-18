using System.Collections;
using System.Collections.Generic;
using KineticRank.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Persistent background music. Uses its own two AudioSources for crossfades and never
// touches AudioController's SFX source, AudioListener, or gameplay state.
public class MusicPlayer : MonoBehaviour
{
    const string ResourceFolder = "Audio/Music/";
    const string MenuSceneName = "MainMenu";
    const float Volume = 0.45f;
    const float FadeDuration = 0.75f;

    public static MusicPlayer Instance { get; private set; }

    readonly MusicPlaylist menuPlaylist = new MusicPlaylist(MusicPlaylist.MenuTracks);
    readonly MusicPlaylist matchPlaylist = new MusicPlaylist(MusicPlaylist.MatchTracks);
    readonly HashSet<string> missingTracks = new HashSet<string>();

    AudioSource[] sources;
    int activeSource;
    bool inMenu;
    bool hasContext;
    Coroutine fade;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("MusicPlayer").AddComponent<MusicPlayer>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sources = new AudioSource[2];
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
            sources[i].spatialBlend = 0f;
            sources[i].volume = 0f;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;

        bool menu = scene.name == MenuSceneName;
        if (hasContext && menu == inMenu) return; // same context (e.g. rematch reload): keep the current track

        hasContext = true;
        inMenu = menu;
        if (menu) PlayNext(menuPlaylist, true);
        else PlayNext(matchPlaylist, false);
    }

    void Update()
    {
        if (inMenu || fade != null) return;

        AudioSource current = sources[activeSource];
        if (current.clip != null && !current.isPlaying)
            PlayNext(matchPlaylist, false);
    }

    void PlayNext(MusicPlaylist playlist, bool loop)
    {
        if (fade != null)
        {
            StopCoroutine(fade);
            FinishFade();
        }

        AudioClip clip = LoadNext(playlist);
        AudioSource outgoing = sources[activeSource];
        activeSource = 1 - activeSource;
        AudioSource incoming = sources[activeSource];

        incoming.clip = clip;
        incoming.loop = loop;
        incoming.volume = 0f;
        if (clip != null) incoming.Play();

        fade = StartCoroutine(Crossfade(outgoing, incoming));
    }

    // Loads one clip at a time; clips are imported as Streaming.
    AudioClip LoadNext(MusicPlaylist playlist)
    {
        for (int attempt = 0; attempt < playlist.Count; attempt++)
        {
            string name = playlist.Next(n => !missingTracks.Contains(n));
            if (name == null) return null;

            AudioClip clip = Resources.Load<AudioClip>(ResourceFolder + name);
            if (clip != null) return clip;

            missingTracks.Add(name);
            Debug.LogWarning($"MusicPlayer: missing clip Resources/{ResourceFolder}{name}; skipping it.");
        }
        return null;
    }

    IEnumerator Crossfade(AudioSource outgoing, AudioSource incoming)
    {
        for (float elapsed = 0f; elapsed < FadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            MusicFade.Volumes(elapsed, FadeDuration, Volume, out float outgoingVolume, out float incomingVolume);
            outgoing.volume = outgoingVolume;
            incoming.volume = incomingVolume;
            yield return null;
        }
        FinishFade();
    }

    void FinishFade()
    {
        fade = null;

        AudioSource incoming = sources[activeSource];
        AudioSource outgoing = sources[1 - activeSource];
        incoming.volume = Volume;

        AudioClip oldClip = outgoing.clip;
        outgoing.Stop();
        outgoing.volume = 0f;
        outgoing.clip = null;
        if (oldClip != null && oldClip != incoming.clip)
            Resources.UnloadAsset(oldClip);
    }
}
