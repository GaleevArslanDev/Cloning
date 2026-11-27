using System;
using UnityEngine;

namespace Echo.MenuReplay
{
    [PreferBinarySerialization]
    public sealed class MenuReplayData : ScriptableObject
    {
        [Serializable]
        public struct Pose
        {
            public Vector3 position, scale;
            public Quaternion rotation;

            public Pose(Transform source)
            {
                position = source.position;
                rotation = source.rotation;
                scale = source.lossyScale;
            }
        }

        [Serializable]
        public sealed class Node
        {
            public string id;
            public Pose initial;
            public int track = -1;
        }

        [Serializable]
        public sealed class Visual
        {
            public int node;
            public Mesh mesh;
            public Material[] materials;
            public int[] bones;
            public int rootBone = -1;
            public Bounds bounds;
            public bool visible, line, worldSpace;
            public float startWidth, endWidth;
            public int visibilityTrack = -1, colorTrack = -1, lineTrack = -1;
        }

        [Serializable]
        public struct Beam
        {
            public int offset, count;
            public Color start, end;
        }

        public string recordingName;
        public int fps, frameCount, movingNodes, visibilityCount, colorCount, beamCount;
        public float Duration => Mathf.Max(0, frameCount - 1) / (float)Mathf.Max(1, fps);
        [HideInInspector] public Node[] nodes;
        [HideInInspector] public Visual[] visuals;
        [HideInInspector] public Pose[] poses;
        [HideInInspector] public byte[] visibility;
        [HideInInspector] public Color[] colors;
        [HideInInspector] public Beam[] beams;
        [HideInInspector] public Vector3[] points;
    }
}
