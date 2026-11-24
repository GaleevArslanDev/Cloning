using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterResetTrigger : MonoBehaviour
{
    [SerializeField] private AudioSource waterAudioSource;
    [SerializeField] private AudioSource respawnAudioSource;
    [SerializeField] private AudioClip waterSound;
    [SerializeField] private AudioClip respawnSound;

    private bool resetting;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (resetting)
            return;

        CloneRecorder recorder = other.GetComponentInParent<CloneRecorder>();

        if (recorder == null)
            return;

        StartCoroutine(ResetPlayer(recorder));
    }

    private IEnumerator ResetPlayer(CloneRecorder recorder)
    {
        resetting = true;
        PlaySound(waterAudioSource, waterSound);
        recorder.FinishRecording();
        yield return null;
        PlaySound(respawnAudioSource, respawnSound);
        resetting = false;
    }

    private void PlaySound(AudioSource source, AudioClip clip)
    {
        if (source != null && clip != null)
            source.PlayOneShot(clip);
    }
}
