using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterResetTrigger : MonoBehaviour
{
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
        recorder.FinishRecording();
        yield return null;
        resetting = false;
    }
}
