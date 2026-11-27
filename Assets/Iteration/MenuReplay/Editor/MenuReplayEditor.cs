using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echo.MenuReplay.Editor
{
    public sealed class MenuReplayEditor : EditorWindow
    {
        const string Menu = "Assets/Scenes/MainMenu.unity";
        const string DefaultLevel = "Assets/ECHO/MenuReplay/Generated/Level7.unity";

        [MenuItem("Tools/ECHO/Menu Replay")]
        static void Open() => GetWindow<MenuReplayEditor>("Menu Replay");

        static T InScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found) return found;
            }
            return null;
        }

        void OnInspectorUpdate() => Repaint();

        void OnGUI()
        {
            var scene = SceneManager.GetActiveScene();
            var recorder = Object.FindFirstObjectByType<MenuReplayRecorder>();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Setup Recording Scene")) SetupRecordingScene();
                if (GUILayout.Button("Setup Main Menu")) SetupMainMenu();
                if (GUILayout.Button("Open Main Menu") && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(Menu);
            }
            if (!recorder || scene.path == Menu) return;
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(recorder.IsRecording))
            {
                var settings = new SerializedObject(recorder);
                foreach (var field in new[] { "frameRate", "maximumDuration", "recordingName", "createNewVariant" })
                    EditorGUILayout.PropertyField(settings.FindProperty(field));
                settings.ApplyModifiedProperties();
            }
            EditorGUILayout.LabelField(recorder.IsRecording ? $"Recording   {recorder.Duration:F1} s   •   {recorder.ObjectCount} moving transforms" : "Ready");
            using (new EditorGUI.DisabledScope(!Application.isPlaying || recorder.IsRecording))
                if (GUILayout.Button("Start Recording   F8")) recorder.StartRecording();
            using (new EditorGUI.DisabledScope(!recorder.IsRecording))
            {
                if (GUILayout.Button("Stop & Save Recording   F9")) recorder.StopRecording();
                if (GUILayout.Button("Cancel Recording")) recorder.CancelRecording();
            }
        }

        static void SetupRecordingScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!InScene<PlayerMovement>(scene) || scene.path == Menu)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(DefaultLevel);
            }
            var recorder = InScene<MenuReplayRecorder>(scene);
            if (!recorder) recorder = new GameObject("MenuReplayRecorder").AddComponent<MenuReplayRecorder>();
            var player = InScene<PlayerMovement>(scene);
            var view = player.GetComponentInChildren<Camera>(true);
            if (view)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = camera == view;
                    foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
                        listener.enabled = listener.transform == view.transform;
                }
                view.gameObject.SetActive(true);
                if (!view.TryGetComponent<AudioListener>(out _)) view.gameObject.AddComponent<AudioListener>();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = recorder.gameObject;
        }

        static void SetupMainMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = SceneManager.GetActiveScene();
            if (scene.path != Menu) scene = EditorSceneManager.OpenScene(Menu);
            var player = InScene<MenuReplayPlayer>(scene);
            if (!player) player = new GameObject("MenuReplaySystem").AddComponent<MenuReplayPlayer>();
            var last = EditorPrefs.GetString(Application.dataPath + "/LastMenuReplay", "");
            if (!string.IsNullOrEmpty(last))
            {
                var asset = AssetDatabase.LoadAssetAtPath<MenuReplayData>(last);
                if (asset) player.recording = asset;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = player.gameObject;
        }
    }
}
