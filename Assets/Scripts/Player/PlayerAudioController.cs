using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAudioController : MonoBehaviour
{
    private AudioSource oneShotSource;

    public static AudioSource GetOneShotSource()
    {
        PlayerAudioController controller = FindOrCreate();
        return controller != null ? controller.GetOrCreateOneShotSource() : null;
    }

    public static AudioSource CreatePlayerSource()
    {
        PlayerAudioController controller = FindOrCreate();
        return controller != null ? controller.CreateSource() : null;
    }

    private static PlayerAudioController FindOrCreate()
    {
        CloneRecorder recorder = FindFirstObjectByType<CloneRecorder>();

        if (recorder == null)
            return null;

        PlayerAudioController controller = recorder.GetComponent<PlayerAudioController>();

        if (controller == null)
            controller = recorder.gameObject.AddComponent<PlayerAudioController>();

        return controller;
    }

    private AudioSource GetOrCreateOneShotSource()
    {
        if (oneShotSource == null)
            oneShotSource = CreateSource();

        return oneShotSource;
    }

    private AudioSource CreateSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }
}
