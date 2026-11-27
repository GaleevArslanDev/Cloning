using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Echo.MenuReplay.MenuReplayData;

namespace Echo.MenuReplay
{
    public sealed class MenuReplayPlayer : MonoBehaviour
    {
        public MenuReplayData recording;
        public bool playOnAwake = true, loop = true;
        [Min(0)] public float playbackSpeed = 1, startTime;
        public bool smoothLoopTransition = true;
        [Min(0)] public float transitionDuration = .75f;
        public bool useUnscaledTime = true;

        Transform[] nodes, moving;
        Binding[] bindings;
        Image fade;
        float time;
        bool playing, wrapping, fadingIn;

        sealed class Binding
        {
            public Visual data;
            public Renderer renderer;
            public LineRenderer line;
            public MaterialPropertyBlock block;
            public int tint;
        }

        void Awake()
        {
            if (!recording || recording.frameCount < 1 || recording.nodes == null) return;
            foreach (var sceneRoot in gameObject.scene.GetRootGameObjects())
                foreach (var r in sceneRoot.GetComponentsInChildren<Renderer>())
                    if (!r.GetComponentInParent<Canvas>()) r.enabled = false;
            var root = new GameObject("ReplayVisuals").transform;
            root.SetParent(transform, false);
            nodes = new Transform[recording.nodes.Length];
            moving = new Transform[recording.movingNodes];
            for (int i = 0; i < nodes.Length; i++)
            {
                var n = recording.nodes[i];
                var t = new GameObject(n.id).transform;
                t.SetParent(root, false);
                t.SetPositionAndRotation(n.initial.position, n.initial.rotation);
                t.localScale = n.initial.scale;
                nodes[i] = t;
                if (n.track >= 0) moving[n.track] = t;
            }
            var animated = new List<Binding>();
            foreach (var v in recording.visuals)
            {
                var obj = nodes[v.node].gameObject;
                Renderer r;
                LineRenderer line = null;
                if (v.line)
                {
                    var child = new GameObject("Laser Path");
                    child.transform.SetParent(obj.transform, false);
                    line = child.AddComponent<LineRenderer>();
                    line.useWorldSpace = v.worldSpace;
                    line.startWidth = v.startWidth;
                    line.endWidth = v.endWidth;
                    r = line;
                }
                else if (v.bones != null && v.bones.Length > 0)
                {
                    var skin = obj.AddComponent<SkinnedMeshRenderer>();
                    skin.sharedMesh = v.mesh;
                    var bones = new Transform[v.bones.Length];
                    for (int i = 0; i < bones.Length; i++) bones[i] = v.bones[i] >= 0 ? nodes[v.bones[i]] : null;
                    skin.bones = bones;
                    if (v.rootBone >= 0) skin.rootBone = nodes[v.rootBone];
                    skin.localBounds = v.bounds;
                    skin.updateWhenOffscreen = true;
                    r = skin;
                }
                else
                {
                    obj.AddComponent<MeshFilter>().sharedMesh = v.mesh;
                    r = obj.AddComponent<MeshRenderer>();
                }
                r.sharedMaterials = v.materials;
                r.enabled = v.visible;
                if (v.visibilityTrack < 0 && !line && v.colorTrack < 0) continue;
                var binding = new Binding { data = v, renderer = r, line = line };
                if (v.colorTrack >= 0)
                {
                    binding.block = new MaterialPropertyBlock();
                    binding.tint = Shader.PropertyToID(r.sharedMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color");
                }
                animated.Add(binding);
            }
            bindings = animated.ToArray();
            CreateFade();
            time = Mathf.Clamp(startTime, 0, recording.Duration);
            playing = playOnAwake;
            Apply();
        }

        void CreateFade()
        {
            var canvas = new GameObject("BackgroundTransition", typeof(Canvas)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MinValue;
            fade = new GameObject("Fade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fade.transform.SetParent(canvas.transform, false);
            fade.rectTransform.anchorMin = Vector2.zero;
            fade.rectTransform.anchorMax = Vector2.one;
            fade.rectTransform.offsetMin = fade.rectTransform.offsetMax = Vector2.zero;
            fade.color = new Color(0, 0, 0, 0);
            fade.raycastTarget = false;
        }

        public void Play() { if (bindings != null) playing = true; }

        void Update()
        {
            if (!playing || recording.Duration <= 0) return;
            float end = recording.Duration;
            float transition = smoothLoopTransition && loop ? Mathf.Min(transitionDuration, end * .5f) : 0;
            if (wrapping)
            {
                time = 0;
                wrapping = false;
                fadingIn = true;
            }
            else
            {
                time += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * playbackSpeed;
                if (time >= end)
                {
                    if (!loop) { time = end; playing = false; }
                    else if (transition > 0) { time = end; wrapping = true; }
                    else time %= end;
                }
            }
            if (time >= transition) fadingIn = false;
            float opacity = transition <= 0 ? 0 : Mathf.Clamp01(fadingIn ? 1 - time / transition : 1 - (end - time) / transition);
            fade.color = new Color(0, 0, 0, opacity);
            Apply();
        }

        void Apply()
        {
            float frame = Mathf.Clamp(time * recording.fps, 0, recording.frameCount - 1);
            int a = (int)frame, b = Mathf.Min(a + 1, recording.frameCount - 1);
            float t = frame - a;
            for (int i = 0; i < moving.Length; i++)
            {
                var x = recording.poses[a * moving.Length + i];
                var y = recording.poses[b * moving.Length + i];
                moving[i].SetPositionAndRotation(Vector3.Lerp(x.position, y.position, t), Quaternion.Slerp(x.rotation, y.rotation, t));
                moving[i].localScale = Vector3.Lerp(x.scale, y.scale, t);
            }
            foreach (var binding in bindings)
            {
                var v = binding.data;
                if (v.visibilityTrack >= 0)
                    binding.renderer.enabled = recording.visibility[a * recording.visibilityCount + v.visibilityTrack] != 0;
                if (v.colorTrack >= 0)
                {
                    binding.block.SetColor(binding.tint, Color.Lerp(recording.colors[a * recording.colorCount + v.colorTrack],
                        recording.colors[b * recording.colorCount + v.colorTrack], t));
                    binding.renderer.SetPropertyBlock(binding.block);
                }
                if (!binding.line) continue;
                var first = recording.beams[a * recording.beamCount + v.lineTrack];
                var next = recording.beams[b * recording.beamCount + v.lineTrack];
                var line = binding.line;
                line.enabled = first.count > 0;
                line.positionCount = first.count;
                line.startColor = first.start;
                line.endColor = first.end;
                for (int i = 0; i < first.count; i++)
                {
                    var p = recording.points[first.offset + i];
                    if (first.count == next.count) p = Vector3.Lerp(p, recording.points[next.offset + i], t);
                    line.SetPosition(i, p);
                }
            }
        }
    }
}
