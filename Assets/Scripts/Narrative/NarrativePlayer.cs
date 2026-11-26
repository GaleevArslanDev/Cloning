using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NarrativePlayer : MonoBehaviour
{
    public static NarrativePlayer Instance { get; private set; }

    [SerializeField] private bool keepBetweenScenes = true;
    [SerializeField] private GameObject phrasePanel;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text phraseText;
    private AudioSource voiceSource;
    [SerializeField] private UnityEvent onSequenceStarted;
    [SerializeField] private UnityEvent onSequenceFinished;
    [SerializeField] private UnityEvent<int> onLineStarted;
    [SerializeField] private UnityEvent<int> onLineFinished;

    private readonly Queue<NarrativeSequence> queue = new Queue<NarrativeSequence>();
    private Coroutine playbackRoutine;

    public bool IsPlaying => playbackRoutine != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (keepBetweenScenes)
            DontDestroyOnLoad(gameObject);

        voiceSource = PlayerAudioController.CreatePlayerSource();
        SetPanelVisible(false);
    }

    public void Play(NarrativeSequence sequence)
    {
        if (sequence == null)
            return;

        Stop();
        queue.Enqueue(sequence);
        playbackRoutine = StartCoroutine(PlayQueue());
    }

    public void PlayQueued(NarrativeSequence sequence)
    {
        if (sequence == null)
            return;

        queue.Enqueue(sequence);

        if (playbackRoutine == null)
            playbackRoutine = StartCoroutine(PlayQueue());
    }

    public void Stop()
    {
        if (playbackRoutine != null)
            StopCoroutine(playbackRoutine);

        playbackRoutine = null;
        queue.Clear();

        if (voiceSource != null)
            voiceSource.Stop();

        SetPanelVisible(false);
    }

    private IEnumerator PlayQueue()
    {
        while (queue.Count > 0)
        {
            NarrativeSequence sequence = queue.Dequeue();
            yield return PlaySequence(sequence);
        }

        playbackRoutine = null;
        SetPanelVisible(false);
    }

    private IEnumerator PlaySequence(NarrativeSequence sequence)
    {
        NarrativeLine[] lines = sequence.Lines;

        if (lines == null || lines.Length == 0)
            yield break;

        onSequenceStarted?.Invoke();

        for (int i = 0; i < lines.Length; i++)
        {
            NarrativeLine line = lines[i];

            if (line == null)
                continue;

            if (line.DelayBefore > 0f)
                yield return new WaitForSeconds(line.DelayBefore);

            ShowLine(line);
            onLineStarted?.Invoke(i);

            float duration = line.Voice != null
                ? Mathf.Max(line.Duration, line.Voice.length)
                : line.Duration;
            yield return new WaitForSeconds(duration);

            onLineFinished?.Invoke(i);

            if (voiceSource != null)
                voiceSource.Stop();

            if (line.DelayAfter > 0f)
                yield return new WaitForSeconds(line.DelayAfter);
        }

        SetPanelVisible(false);
        onSequenceFinished?.Invoke();
    }

    private void ShowLine(NarrativeLine line)
    {
        if (voiceSource == null)
            voiceSource = PlayerAudioController.CreatePlayerSource();

        if (speakerText != null)
        {
            speakerText.text = line.Speaker;
            speakerText.gameObject.SetActive(!string.IsNullOrWhiteSpace(line.Speaker));
        }

        if (phraseText != null)
            phraseText.text = line.Text;

        SetPanelVisible(true);

        if (voiceSource != null && line.Voice != null)
        {
            voiceSource.clip = line.Voice;
            voiceSource.Play();
        }
    }

    private void SetPanelVisible(bool visible)
    {
        if (phrasePanel != null)
            phrasePanel.SetActive(visible);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
