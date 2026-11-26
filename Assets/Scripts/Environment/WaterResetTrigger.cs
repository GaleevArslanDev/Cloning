using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterResetTrigger : MonoBehaviour
{
    [SerializeField] private float respawnSoundDelay = 0.25f;

    private AudioSource waterAudioSource;
    private AudioSource respawnAudioSource;
    private AudioClip waterSound;
    private AudioClip respawnSound;

    private bool resetting;

    private void Awake()
    {
        waterAudioSource = PlayerAudioController.GetOneShotSource();
        respawnAudioSource = waterAudioSource;
        GameAudioClips audioClips = GameAudioClips.Instance;

        if (audioClips != null)
        {
            waterSound = audioClips.Water;
            respawnSound = audioClips.Respawn;
        }

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

        if (respawnSoundDelay > 0f)
            yield return new WaitForSeconds(respawnSoundDelay);
        else
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
