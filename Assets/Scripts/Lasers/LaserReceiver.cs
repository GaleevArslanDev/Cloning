using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LaserReceiver : MonoBehaviour
{
    [SerializeField] private UnityEvent onActivated;
    [SerializeField] private UnityEvent onDeactivated;

    private readonly HashSet<LaserEmitter> emitters = new HashSet<LaserEmitter>();

    public bool IsActive => emitters.Count > 0;

    public void Receive(LaserEmitter emitter)
    {
        if (emitter == null || !emitters.Add(emitter))
            return;

        if (emitters.Count == 1)
            onActivated?.Invoke();
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
