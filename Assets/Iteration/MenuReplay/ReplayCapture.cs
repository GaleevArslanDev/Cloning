#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using static Echo.MenuReplay.MenuReplayData;
using Pose = Echo.MenuReplay.MenuReplayData.Pose;

namespace Echo.MenuReplay
{
    // Editor-only capture. Meshes and bones share a flat world-space coordinate system.
    internal sealed class ReplayCapture
    {
        readonly List<Node> nodes = new();
        readonly List<Visual> visuals = new();
        readonly Dictionary<Transform, int> indices = new();
        readonly List<Transform> moving = new();
        readonly HashSet<Transform> roots = new();
        readonly List<Renderer> visible = new();
        readonly List<Material> tinted = new();
        readonly List<BeamPool> pools = new();
        readonly List<Pose> poses = new();
        readonly List<byte> visibility = new();
        readonly List<Color> colors = new();
        readonly List<Beam> beams = new();
        readonly List<Vector3> points = new();
        readonly Dictionary<Animator, AnimatorCullingMode> animators = new();
        int beamCount;
        public int Frames { get; private set; }
        public int Fps { get; private set; }
        public int MovingCount => moving.Count;

        sealed class BeamPool
        {
            public LaserEmitter emitter;
            public LineRenderer[] slots;
            public int children = -1;

            public void Refresh()
            {
                if (!emitter || emitter.transform.childCount == children) return;
                var t = emitter.transform;
                children = t.childCount;
                slots[0] = emitter.GetComponent<LineRenderer>();
                int slot = 1;
                for (int i = 0; i < children && slot < slots.Length; i++)
                    if (t.GetChild(i).TryGetComponent(out LineRenderer line)) slots[slot++] = line;
            }
        }

        static string PathOf(Transform t)
        {
            string segment = t.name + "[" + t.GetSiblingIndex() + "]";
            return t.parent ? PathOf(t.parent) + "/" + segment : segment;
        }

        int Register(Transform t, bool dynamic)
        {
            if (!t) return -1;
            if (!indices.TryGetValue(t, out int index))
            {
                index = nodes.Count;
                nodes.Add(new Node { id = PathOf(t), initial = new Pose(t) });
                indices.Add(t, index);
            }
            if (dynamic && nodes[index].track < 0)
            {
                nodes[index].track = moving.Count;
                moving.Add(t);
            }
            return index;
        }

        public void Prepare(Scene scene, int fps)
        {
            Fps = fps;
            var sceneRoots = new List<GameObject>(scene.GetRootGameObjects());
            // ECHO keeps GameAudioClips on the player and moves that player to DontDestroyOnLoad.
            foreach (var player in Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            {
                var root = player.transform.root.gameObject;
                if (!sceneRoots.Contains(root)) sceneRoots.Add(root);
            }
            foreach (var root in sceneRoots)
            foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!script) continue;
                if (script is PlayerMovement || script is ClonePlayback || script is CarryableItem ||
                    script is PressurePlate || script is ButtonInteractable || script is TwoPointsCopy ||
                    script is HoldDoorAction || script is LaserReceiver) roots.Add(script.transform);
                if (script is FloorCubeLiftAction) AddReferences(script, "cubes");
                if (script is ObjectVisibilityAction || script is ButtonInteractable) AddReferences(script, "objectsToHide");
                if (script is HoldDoorAction) AddReferences(script, "doorAnimator");
                if (script is PressurePlate)
                {
                    AddReferences(script, "plateVisual");
                    AddReferences(script, "stateRenderer");
                }
                if (script is LaserEmitter emitter) AddEmitter(emitter);
            }
            foreach (var root in sceneRoots)
            {
                foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                {
                    roots.Add(animator.transform);
                    animators[animator] = animator.cullingMode;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    AddRenderer(renderer);
            }
        }

        void AddReferences(MonoBehaviour owner, string name)
        {
            using var serialized = new SerializedObject(owner);
            var p = serialized.FindProperty(name);
            if (p == null) return;
            int count = p.isArray ? p.arraySize : 1;
            for (int i = 0; i < count; i++)
            {
                Object value = (p.isArray ? p.GetArrayElementAtIndex(i) : p).objectReferenceValue;
                if (value is Component component) roots.Add(component.transform);
                if (value is GameObject obj) roots.Add(obj.transform);
            }
        }

        void AddEmitter(LaserEmitter emitter)
        {
            var line = emitter.GetComponent<LineRenderer>();
            if (!line) return;
            using var so = new SerializedObject(emitter);
            int count = Mathf.Max(1, so.FindProperty("maximumPaths").intValue);
            var pool = new BeamPool { emitter = emitter, slots = new LineRenderer[count] };
            pools.Add(pool);
            for (int i = 0; i < count; i++)
                visuals.Add(new Visual { node = Register(line.transform, true), line = true,
                    lineTrack = beamCount++, materials = line.sharedMaterials, worldSpace = true,
                    startWidth = line.startWidth, endWidth = line.endWidth });
        }

        void AddRenderer(Renderer r)
        {
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer) || r.GetComponentInParent<Canvas>(true)) return;
            bool dynamic = false;
            for (var t = r.transform; t; t = t.parent)
                if (roots.Contains(t)) { dynamic = true; break; }
            var skin = r as SkinnedMeshRenderer;
            var filter = r.GetComponent<MeshFilter>();
            Mesh mesh = skin ? skin.sharedMesh : filter ? filter.sharedMesh : null;
            if (!mesh || (!dynamic && !IsVisible(r))) return;
            // RoomCubeField replaces this renderer with its Breathing Visual child.
            if (!r.enabled && r.transform.Find("Breathing Visual")) return;
            var v = new Visual { node = Register(r.transform, dynamic), mesh = mesh,
                materials = r.sharedMaterials, visible = IsVisible(r) };
            if (skin)
            {
                var bones = skin.bones;
                v.bones = new int[bones.Length];
                for (int i = 0; i < bones.Length; i++) v.bones[i] = Register(bones[i], true);
                v.rootBone = Register(skin.rootBone, true);
                v.bounds = skin.localBounds;
            }
            if (dynamic)
            {
                v.visibilityTrack = visible.Count;
                visible.Add(r);
                if (r.sharedMaterial && r.GetComponentInParent<PressurePlate>(true))
                {
                    v.colorTrack = tinted.Count;
                    tinted.Add(r.sharedMaterial);
                }
            }
            visuals.Add(v);
        }

        static bool IsVisible(Renderer r) => r && r.enabled && r.gameObject.activeInHierarchy && !r.forceRenderingOff;

        public void Sample()
        {
            foreach (var t in moving) poses.Add(t ? new Pose(t) : default);
            foreach (var r in visible) visibility.Add((byte)(IsVisible(r) ? 1 : 0));
            foreach (var m in tinted) colors.Add(m ? m.color : Color.white);
            foreach (var pool in pools)
            {
                pool.Refresh();
                foreach (var line in pool.slots)
                {
                    var beam = new Beam { offset = points.Count };
                    if (IsVisible(line))
                    {
                        beam.count = line.positionCount;
                        beam.start = line.startColor;
                        beam.end = line.endColor;
                        for (int i = 0; i < beam.count; i++) points.Add(line.GetPosition(i));
                    }
                    beams.Add(beam);
                }
            }
            Frames++;
        }

        public void Save(string requestedName, bool variant)
        {
            const string folder = "Assets/ECHO/MenuReplay/Recordings";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/ECHO/MenuReplay", "Recordings");
            string name = string.Join("_", (requestedName ?? "").Split(Path.GetInvalidFileNameChars())).Trim().TrimEnd('.');
            if (string.IsNullOrEmpty(name)) name = "ECHO_MenuReplay";
            string path = folder + "/" + name + ".asset";
            if (variant) path = AssetDatabase.GenerateUniqueAssetPath(path);
            var data = AssetDatabase.LoadAssetAtPath<MenuReplayData>(path);
            if (!data && AssetDatabase.LoadMainAssetAtPath(path)) throw new IOException("Another asset occupies " + path);
            if (!data)
            {
                data = ScriptableObject.CreateInstance<MenuReplayData>();
                AssetDatabase.CreateAsset(data, path);
            }
            var previous = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            var copies = new Dictionary<Object, Object>();
            foreach (var v in visuals)
            {
                v.mesh = Persist(v.mesh, data, copies);
                for (int i = 0; i < v.materials.Length; i++) v.materials[i] = Persist(v.materials[i], data, copies);
            }
            data.recordingName = name;
            data.frameCount = Frames;
            data.fps = Fps;
            data.movingNodes = moving.Count;
            data.visibilityCount = visible.Count;
            data.colorCount = tinted.Count;
            data.beamCount = beamCount;
            data.nodes = nodes.ToArray();
            data.visuals = visuals.ToArray();
            data.poses = poses.ToArray();
            data.colors = colors.ToArray();
            data.visibility = visibility.ToArray();
            data.beams = beams.ToArray();
            data.points = points.ToArray();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
            foreach (var old in previous) Object.DestroyImmediate(old, true);
            AssetDatabase.SaveAssets();
            EditorPrefs.SetString(Application.dataPath + "/LastMenuReplay", path);
            Debug.Log("Saved ECHO replay: " + path, data);
        }

        static T Persist<T>(T source, MenuReplayData owner, Dictionary<Object, Object> copies) where T : Object
        {
            if (!source || EditorUtility.IsPersistent(source)) return source;
            if (copies.TryGetValue(source, out var existing)) return (T)existing;
            var copy = Object.Instantiate(source);
            copy.name = source.name;
            copy.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(copy, owner);
            copies[source] = copy;
            return copy;
        }

        public void Dispose()
        {
            foreach (var entry in animators)
                if (entry.Key) entry.Key.cullingMode = entry.Value;
        }
    }
}
#endif
