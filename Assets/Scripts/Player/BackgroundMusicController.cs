using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BackgroundMusicController : MonoBehaviour
{
    private readonly AudioSource[] sources = new AudioSource[2];

    private GameAudioClips settings;
    private int currentSourceIndex;
    private int outgoingSourceIndex = -1;
    private int playlistIndex = -1;
    private float fadeTime;
    private float fadeDuration;
    private float outgoingStartVolume;
    private float incomingTargetVolume;
    private bool menuMode;
    private bool started;
    private bool fading;

    private void Awake()
    {
        sources[0] = CreateSource();
        sources[1] = CreateSource();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (!started || settings == null || AudioListener.pause)
            return;

        if (fading)
        {
            UpdateFade();
            return;
        }

        if (menuMode)
            return;

        AudioSource source = sources[currentSourceIndex];
        int nextIndex = FindPlayableTrack(playlistIndex + 1);

        if (nextIndex < 0)
        {
            StopMusic();
            return;
        }

        BackgroundMusicTrack nextTrack = settings.LevelMusic[nextIndex];
        float remaining = source.clip != null
            ? Mathf.Max(0f, source.clip.length - source.time)
            : 0f;

        if (!source.isPlaying || remaining <= nextTrack.TransitionDuration)
            PlayLevelTrack(nextIndex);
    }

    public void Configure(GameAudioClips newSettings)
    {
        if (newSettings == null)
            return;

        settings = newSettings;
        ApplyScene(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyScene(scene);
    }

    private void ApplyScene(Scene scene)
    {
        if (settings == null)
            return;

        bool shouldUseMenuMusic = string.Equals(
            scene.name,
            settings.MainMenuSceneName,
            System.StringComparison.OrdinalIgnoreCase
        );

        if (started && menuMode == shouldUseMenuMusic)
            return;

        menuMode = shouldUseMenuMusic;

        if (menuMode)
        {
            playlistIndex = -1;
            PlayTrack(settings.MainMenuMusic, true);
            return;
        }

        int firstTrack = FindPlayableTrack(0);

        if (firstTrack < 0)
        {
            Debug.LogWarning("BackgroundMusicController: плейлист уровней пуст", this);
            StopMusic();
            return;
        }

        PlayLevelTrack(firstTrack);
    }

    private void PlayLevelTrack(int index)
    {
        BackgroundMusicTrack[] tracks = settings.LevelMusic;

        if (tracks == null || index < 0 || index >= tracks.Length)
            return;

        playlistIndex = index;
        PlayTrack(tracks[index], false);
    }

    private void PlayTrack(BackgroundMusicTrack track, bool loop)
    {
        if (track == null || track.Clip == null)
        {
            if (menuMode)
                Debug.LogWarning("BackgroundMusicController: музыка главного меню не назначена", this);

            StopMusic();
            return;
        }

        PrepareInterruptedFade();

        AudioSource outgoing = started ? sources[currentSourceIndex] : null;
        int incomingIndex = started ? 1 - currentSourceIndex : currentSourceIndex;
        AudioSource incoming = sources[incomingIndex];
        incoming.Stop();
        incoming.clip = track.Clip;
        incoming.loop = loop;
        incoming.volume = 0f;
        incoming.Play();

        fadeDuration = track.TransitionDuration;
        incomingTargetVolume = track.Volume;
        fadeTime = 0f;
        outgoingSourceIndex = outgoing != null && outgoing.isPlaying
            ? currentSourceIndex
            : -1;
        outgoingStartVolume = outgoingSourceIndex >= 0
            ? sources[outgoingSourceIndex].volume
            : 0f;
        currentSourceIndex = incomingIndex;
        started = true;

        if (fadeDuration <= 0f)
        {
            FinishFade();
            return;
        }

        fading = true;
    }

    private void UpdateFade()
    {
        fadeTime += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(fadeTime / Mathf.Max(0.01f, fadeDuration));
        sources[currentSourceIndex].volume = Mathf.Lerp(
            0f,
            incomingTargetVolume,
            progress
        );

        if (outgoingSourceIndex >= 0)
        {
            sources[outgoingSourceIndex].volume = Mathf.Lerp(
                outgoingStartVolume,
                0f,
                progress
            );
        }

        if (progress >= 1f)
            FinishFade();
    }

    private void FinishFade()
    {
        sources[currentSourceIndex].volume = incomingTargetVolume;

        if (outgoingSourceIndex >= 0)
        {
            sources[outgoingSourceIndex].Stop();
            sources[outgoingSourceIndex].clip = null;
            sources[outgoingSourceIndex].volume = 0f;
        }

        outgoingSourceIndex = -1;
        fading = false;
    }

    private void PrepareInterruptedFade()
    {
        if (!fading)
            return;

        int louderSource = sources[0].volume >= sources[1].volume ? 0 : 1;
        int quieterSource = 1 - louderSource;
        sources[quieterSource].Stop();
        sources[quieterSource].clip = null;
        sources[quieterSource].volume = 0f;
        currentSourceIndex = louderSource;
        outgoingSourceIndex = -1;
        fading = false;
    }

    private int FindPlayableTrack(int startIndex)
    {
        BackgroundMusicTrack[] tracks = settings != null ? settings.LevelMusic : null;

        if (tracks == null || tracks.Length == 0)
            return -1;

        int normalizedStart = ((startIndex % tracks.Length) + tracks.Length) % tracks.Length;

        for (int offset = 0; offset < tracks.Length; offset++)
        {
            int index = (normalizedStart + offset) % tracks.Length;
            BackgroundMusicTrack track = tracks[index];

            if (track != null && track.Clip != null)
                return index;
        }

        return -1;
    }

    private void StopMusic()
    {
        foreach (AudioSource source in sources)
        {
            if (source == null)
                continue;

            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }

        started = false;
        fading = false;
        outgoingSourceIndex = -1;
        playlistIndex = -1;
    }

    private AudioSource CreateSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        return source;
    }
}
