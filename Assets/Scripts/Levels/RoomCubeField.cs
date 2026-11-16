using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RoomCubeField : MonoBehaviour
{
    [SerializeField] private Transform cubesRoot;
    [SerializeField] private Transform flatRoomCenter;
    [SerializeField] private Vector3 flatRoomSize = new Vector3(20f, 8f, 20f);
    [SerializeField] private float breathingAmplitude = 0.025f;
    [SerializeField] private float floorBreathingAmplitude = 0.003f;
    [SerializeField] private float breathingSpeed = 0.8f;
    [SerializeField] private float speedRandomness = 0.35f;
    [SerializeField] private float waveRadius = 1.25f;
    [SerializeField] private float waveHeight = 0.18f;
    [SerializeField] private int randomSeed = 731;

    private readonly List<CubeState> cubes = new List<CubeState>();
    private readonly List<RoomWave> waves = new List<RoomWave>();
    private float flatness;
    private bool frozen;
    private bool geometryDirty;

    private class CubeState
    {
        public Transform Root;
        public Transform Visual;
        public Vector3 FinalPosition;
        public Vector3 FlatPosition;
        public Vector3 Normal;
        public float Phase;
        public float Speed;
        public bool IsFloor;
    }

    private struct RoomWave
    {
        public Vector3 Position;
        public float Strength;
    }

    public float Flatness => flatness;
    public Transform FlatRoomCenter => flatRoomCenter;
    public Vector3 FlatRoomSize => flatRoomSize;

    private void Awake()
    {
        if (cubesRoot == null)
            cubesRoot = transform;
        if (flatRoomCenter == null)
            flatRoomCenter = transform;

        CollectCubes();
    }

    private void LateUpdate()
    {
        float time = Time.time;
        bool moveGeometry = geometryDirty || waves.Count > 0;

        foreach (CubeState cube in cubes)
        {
            Vector3 basePosition = Vector3.Lerp(
                cube.FinalPosition,
                cube.FlatPosition,
                Smooth(flatness)
            );

            float wave = GetWave(cube, basePosition);

            if (moveGeometry)
                cube.Root.position = basePosition - cube.Normal * wave;

            if (cube.Visual == null)
                continue;

            if (frozen)
            {
                cube.Visual.localPosition = Vector3.zero;
                continue;
            }

            float amplitude = cube.IsFloor
                ? floorBreathingAmplitude
                : breathingAmplitude;
            float breath = Mathf.Sin(time * cube.Speed + cube.Phase) * amplitude;
            cube.Visual.position = cube.Root.position + cube.Normal * breath;
        }

        waves.Clear();
        geometryDirty = false;
    }

    public void SetFlatness(float value)
    {
        flatness = Mathf.Clamp01(value);
        geometryDirty = true;
    }

    public void SetFrozen(bool value)
    {
        frozen = value;

        if (!frozen)
            return;

        foreach (CubeState cube in cubes)
        {
            cube.Root.position = Vector3.Lerp(
                cube.FinalPosition,
                cube.FlatPosition,
                Smooth(flatness)
            );

            if (cube.Visual != null)
                cube.Visual.localPosition = Vector3.zero;
        }
    }

    public void AddFloorWave(Vector3 position, float strength)
    {
        waves.Add(new RoomWave
        {
            Position = position,
            Strength = Mathf.Clamp01(strength)
        });
    }

    private void CollectCubes()
    {
        MeshRenderer[] renderers = cubesRoot.GetComponentsInChildren<MeshRenderer>(true);
        HashSet<Transform> added = new HashSet<Transform>();

        foreach (MeshRenderer sourceRenderer in renderers)
        {
            Transform cubeRoot = sourceRenderer.transform;

            if (!added.Add(cubeRoot))
                continue;

            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();

            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                continue;

            Vector3 finalPosition = cubeRoot.position;
            GetFlatPose(finalPosition, out Vector3 flatPosition, out Vector3 normal, out bool isFloor);

            cubes.Add(new CubeState
            {
                Root = cubeRoot,
                Visual = CreateVisual(sourceRenderer, sourceFilter),
                FinalPosition = finalPosition,
                FlatPosition = flatPosition,
                Normal = normal,
                Phase = GetValue(finalPosition, randomSeed) * Mathf.PI * 2f,
                Speed = breathingSpeed * Mathf.Lerp(
                    1f - speedRandomness,
                    1f + speedRandomness,
                    GetValue(finalPosition, randomSeed + 41)
                ),
                IsFloor = isFloor
            });
        }
    }

    private Transform CreateVisual(MeshRenderer sourceRenderer, MeshFilter sourceFilter)
    {
        GameObject visualObject = new GameObject("Breathing Visual");
        visualObject.layer = sourceRenderer.gameObject.layer;
        visualObject.transform.SetParent(sourceRenderer.transform, false);

        MeshFilter visualFilter = visualObject.AddComponent<MeshFilter>();
        visualFilter.sharedMesh = sourceFilter.sharedMesh;

        MeshRenderer visualRenderer = visualObject.AddComponent<MeshRenderer>();
        visualRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
        visualRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
        visualRenderer.receiveShadows = sourceRenderer.receiveShadows;
        visualRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
        visualRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
        visualRenderer.lightmapIndex = sourceRenderer.lightmapIndex;
        visualRenderer.lightmapScaleOffset = sourceRenderer.lightmapScaleOffset;
        visualRenderer.renderingLayerMask = sourceRenderer.renderingLayerMask;

        sourceRenderer.enabled = false;
        return visualObject.transform;
    }

    private void GetFlatPose(
        Vector3 worldPosition,
        out Vector3 flatPosition,
        out Vector3 normal,
        out bool isFloor
    )
    {
        Vector3 localPosition = flatRoomCenter.InverseTransformPoint(worldPosition);
        Vector3 halfSize = flatRoomSize * 0.5f;
        Vector3 normalized = new Vector3(
            Mathf.Abs(localPosition.x) / Mathf.Max(0.01f, halfSize.x),
            Mathf.Abs(localPosition.y) / Mathf.Max(0.01f, halfSize.y),
            Mathf.Abs(localPosition.z) / Mathf.Max(0.01f, halfSize.z)
        );

        Vector3 localNormal;

        if (normalized.x >= normalized.y && normalized.x >= normalized.z)
        {
            float sign = Mathf.Sign(localPosition.x);
            localPosition.x = sign * halfSize.x;
            localNormal = Vector3.right * sign;
        }
        else if (normalized.y >= normalized.z)
        {
            float sign = Mathf.Sign(localPosition.y);
            localPosition.y = sign * halfSize.y;
            localNormal = Vector3.up * sign;
        }
        else
        {
            float sign = Mathf.Sign(localPosition.z);
            localPosition.z = sign * halfSize.z;
            localNormal = Vector3.forward * sign;
        }

        flatPosition = flatRoomCenter.TransformPoint(localPosition);
        normal = flatRoomCenter.TransformDirection(localNormal).normalized;
        isFloor = localNormal.y < -0.5f;
    }

    private float GetWave(CubeState cube, Vector3 position)
    {
        if (!cube.IsFloor || waves.Count == 0)
            return 0f;

        float strongest = 0f;

        foreach (RoomWave wave in waves)
        {
            Vector3 difference = Vector3.ProjectOnPlane(position - wave.Position, cube.Normal);
            float distance = difference.magnitude;

            if (distance >= waveRadius)
                continue;

            float amount = 1f - distance / Mathf.Max(0.01f, waveRadius);
            strongest = Mathf.Max(strongest, Mathf.Sin(amount * Mathf.PI) * wave.Strength);
        }

        return strongest * waveHeight;
    }

    private float GetValue(Vector3 position, int seed)
    {
        float value = Mathf.Sin(
            position.x * 12.9898f +
            position.y * 78.233f +
            position.z * 37.719f +
            seed
        ) * 43758.5453f;

        return value - Mathf.Floor(value);
    }

    private float Smooth(float value)
    {
        return value * value * (3f - 2f * value);
    }
}
