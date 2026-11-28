using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LaserReceiver : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip receivedSound;
    [SerializeField] private UnityEvent onActivated;
    [SerializeField] private UnityEvent onDeactivated;

    private readonly HashSet<LaserEmitter> emitters = new HashSet<LaserEmitter>();

    public bool IsActive => emitters.Count > 0;

    private void Awake()
    {
        audioSource = PlayerAudioController.GetOneShotSource();
        receivedSound = GameAudioClips.Instance != null
            ? GameAudioClips.Instance.LaserReceived
            : null;
    }

    private void Update()
    {
        if (emitters.Count == 0)
            return;

        int removed = emitters.RemoveWhere(emitter => emitter == null);

        if (removed > 0 && emitters.Count == 0)
            onDeactivated?.Invoke();
    }

    public void Receive(LaserEmitter emitter)
    {
        if (emitter == null || !emitters.Add(emitter))
            return;

        if (emitters.Count == 1)
        {
            if (audioSource != null && receivedSound != null)
                audioSource.PlayOneShot(receivedSound);

            onActivated?.Invoke();
        }
    }

    public void StopReceiving(LaserEmitter emitter)
    {
        if (!emitters.Remove(emitter))
            return;

        if (emitters.Count == 0)
            onDeactivated?.Invoke();
    }

    private void OnDisable()
    {
        if (emitters.Count == 0)
            return;

        emitters.Clear();
        onDeactivated?.Invoke();
    }
}
