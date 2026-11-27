using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace Echo.MenuReplay
{
    [DefaultExecutionOrder(30000)]
    public sealed class MenuReplayRecorder : MonoBehaviour
    {
        public enum Rate { FPS15 = 15, FPS30 = 30, FPS60 = 60 }
        public Rate frameRate = Rate.FPS30;
        [Min(1)] public float maximumDuration = 60;
        public string recordingName = "ECHO_MenuReplay";
        public bool createNewVariant;
#if UNITY_EDITOR
        ReplayCapture capture;
        CloneRecorder cycles;
        object cycle;
        double started;
        int lastFrame;
        public bool IsRecording => capture != null;
        public float Duration => IsRecording ? (capture.Frames - 1) / (float)capture.Fps : 0;
        public int ObjectCount => capture?.MovingCount ?? 0;

        public void StartRecording()
        {
            if (!Application.isPlaying || IsRecording) return;
            cycles = FindFirstObjectByType<CloneRecorder>();
            if (!cycles || !FindFirstObjectByType<PlayerMovement>())
            {
                Debug.LogWarning("Open an ECHO gameplay scene to record.", this);
                return;
            }
            cycle = cycles.MenuReplayCycle;
            capture = new ReplayCapture();
            try
            {
                capture.Prepare(gameObject.scene, (int)frameRate);
                capture.Sample();
                lastFrame = Mathf.Max(1, Mathf.RoundToInt(maximumDuration * capture.Fps));
                started = Time.timeAsDouble;
            }
            catch { CancelRecording(); throw; }
        }

        void LateUpdate()
        {
            var keyboard = Keyboard.current;
            if (!IsRecording && keyboard != null && keyboard.f8Key.wasPressedThisFrame) StartRecording();
            if (!IsRecording) return;
            if (!cycles || !ReferenceEquals(cycle, cycles.MenuReplayCycle))
            {
                Debug.Log("ECHO cycle changed. Saving through the previous frame.");
                StopRecording();
                return;
            }
            int target = Mathf.Min(lastFrame, (int)((Time.timeAsDouble - started) * capture.Fps));
            while (capture.Frames <= target) capture.Sample();
            if (target == lastFrame || (keyboard != null && keyboard.f9Key.wasPressedThisFrame)) StopRecording();
        }

        public void StopRecording()
        {
            if (!IsRecording) return;
            capture.Save(recordingName, createNewVariant);
            CancelRecording();
        }

        public void CancelRecording()
        {
            capture?.Dispose();
            capture = null;
        }

        void OnDisable() => CancelRecording();
#else
        public void StartRecording() { }
        public void StopRecording() { }
        public void CancelRecording() { }
#endif
    }
}
