using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class RoomCubeField : MonoBehaviour
{
    [SerializeField] private Transform cubesRoot;
    [SerializeField] private Transform flatRoomCenter;
    [SerializeField] private Vector3 flatRoomSize = new Vector3(20f, 8f, 20f);
    [SerializeField] private bool hasCeiling;
    [SerializeField] private bool detectRoomAutomatically = true;
    [SerializeField] private float detectionStep = 0.05f;
    [SerializeField] private bool showRoomPreview = true;
    [SerializeField] private Color previewColor = new Color(0.1f, 0.85f, 1f, 0.2f);
    [SerializeField] private float breathingAmplitude = 0.025f;
    [SerializeField] private float floorBreathingAmplitude = 0.003f;
    [SerializeField] private float breathingSpeed = 0.8f;
    [SerializeField] private float speedRandomness = 0.35f;
    [SerializeField] private float waveRadius = 1.25f;
    [SerializeField] private float waveHeight = 0.18f;
    [SerializeField] private int randomSeed = 731;

    private readonly List<CubeState> cubes = new List<CubeState>();
    private readonly List<RoomWave> waves = new List<RoomWave>();
    private readonly Dictionary<Object, FloorLiftGroup> floorLifts = new Dictionary<Object, FloorLiftGroup>();
    private float flatness;
    private float breathingStrength = 1f;
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
        public float AppliedOffset;
        public Rigidbody Body;
        public bool IsFloor;
    }

    private class FloorLiftGroup
    {
        public readonly HashSet<CubeState> Cubes = new HashSet<CubeState>();
        public float Height;
    }

    private struct RoomWave
    {
        public Vector3 Position;
        public float Strength;
    }

    public float Flatness => flatness;
    public Transform FlatRoomCenter => flatRoomCenter;
    public Vector3 FlatRoomSize => flatRoomSize;
    public bool HasCeiling => hasCeiling;

    private void Awake()
    {
        if (cubesRoot == null)
            cubesRoot = transform;

        if (detectRoomAutomatically)
            DetectRoom();

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
            float lift = GetLift(cube);
            float offset = wave + lift;

            if (moveGeometry || Mathf.Abs(offset - cube.AppliedOffset) > 0.0001f)
            {
                Vector3 position = basePosition - cube.Normal * offset;

                if (cube.Body != null)
                    cube.Body.MovePosition(position);
                else
                    cube.Root.position = position;

                cube.AppliedOffset = offset;
            }

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
            float breath = Mathf.Sin(time * cube.Speed + cube.Phase) *
                           amplitude * breathingStrength;
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

            cube.AppliedOffset = 0f;

            if (cube.Body != null)
                cube.Body.position = cube.Root.position;
        }
    }

    public void SetBreathingStrength(float value)
    {
        breathingStrength = Mathf.Clamp01(value);
    }

    public void AddFloorWave(Vector3 position, float strength)
    {
        waves.Add(new RoomWave
        {
            Position = position,
            Strength = Mathf.Clamp01(strength)
        });
    }

    public void RegisterFloorLift(Object owner, Transform[] selectedCubes)
    {
        if (owner == null || selectedCubes == null)
            return;

        FloorLiftGroup group = new FloorLiftGroup();

        foreach (Transform selectedCube in selectedCubes)
        {
            if (selectedCube == null)
                continue;

            CubeState cube = FindCube(selectedCube);

            if (cube == null || !cube.IsFloor)
            {
                continue;
            }

            EnsureFloorBody(cube);
            group.Cubes.Add(cube);
        }

        floorLifts[owner] = group;
        geometryDirty = true;
    }

    private CubeState FindCube(Transform selectedCube)
    {
        foreach (CubeState cube in cubes)
        {
            if (cube.Root == selectedCube)
                return cube;
        }

        return null;
    }

    public void SetFloorLift(Object owner, float height)
    {
        if (owner == null || !floorLifts.TryGetValue(owner, out FloorLiftGroup group))
            return;

        height = Mathf.Max(0f, height);

        if (Mathf.Abs(group.Height - height) <= 0.0001f)
            return;

        group.Height = height;
        geometryDirty = true;
    }

    public void RemoveFloorLift(Object owner)
    {
        if (owner == null || !floorLifts.Remove(owner))
            return;

        geometryDirty = true;
    }

    private void EnsureFloorBody(CubeState cube)
    {
        if (cube.Body != null)
            return;

        cube.Body = cube.Root.GetComponent<Rigidbody>();

        if (cube.Body == null)
            cube.Body = cube.Root.gameObject.AddComponent<Rigidbody>();

        cube.Body.isKinematic = true;
        cube.Body.useGravity = false;
        cube.Body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private float GetLift(CubeState cube)
    {
        if (!cube.IsFloor || floorLifts.Count == 0)
            return 0f;

        float height = 0f;

        foreach (FloorLiftGroup group in floorLifts.Values)
        {
            if (group.Height <= height || !group.Cubes.Contains(cube))
                continue;

            height = group.Height;
        }

        return height;
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
            GetFlatPose(
                cubeRoot,
                sourceFilter.sharedMesh.bounds,
                out Vector3 flatPosition,
                out Vector3 normal,
                out bool isFloor
            );

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

    private void DetectRoom()
    {
        if (!TryCalculateRoom(out Vector3 center, out Quaternion rotation, out Vector3 size))
            return;

        GameObject centerObject = new GameObject("Detected Flat Room Center");
        centerObject.transform.SetParent(transform, false);
        centerObject.transform.SetPositionAndRotation(center, rotation);
        flatRoomCenter = centerObject.transform;
        flatRoomSize = size;
    }

    [ContextMenu("Сохранить найденную комнату")]
    private void SaveDetectedRoom()
    {
        if (!TryCalculateRoom(out Vector3 center, out Quaternion rotation, out Vector3 size))
            return;

        Transform centerPoint = transform.Find("Flat Room Center");

        if (centerPoint == null)
        {
            GameObject centerObject = new GameObject("Flat Room Center");
            centerPoint = centerObject.transform;
            centerPoint.SetParent(transform);

#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(centerObject, "Create flat room center");
#endif
        }

#if UNITY_EDITOR
        Undo.RecordObject(centerPoint, "Save flat room center");
        Undo.RecordObject(this, "Save detected room");
#endif

        centerPoint.SetPositionAndRotation(center, rotation);
        centerPoint.localScale = Vector3.one;
        flatRoomCenter = centerPoint;
        flatRoomSize = size;
        detectRoomAutomatically = false;

#if UNITY_EDITOR
        EditorUtility.SetDirty(centerPoint);
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private bool TryCalculateRoom(
        out Vector3 center,
        out Quaternion rotation,
        out Vector3 size
    )
    {
        center = transform.position;
        rotation = transform.rotation;
        size = Vector3.zero;

        if (cubesRoot == null)
            return false;

        MeshRenderer[] renderers = cubesRoot.GetComponentsInChildren<MeshRenderer>(true);
        List<DetectedBlock> blocks = new List<DetectedBlock>();
        List<float> xCenters = new List<float>();
        List<float> zCenters = new List<float>();

        foreach (MeshRenderer renderer in renderers)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();

            if (filter == null || filter.sharedMesh == null)
                continue;

            GetBoundsInTransform(
                renderer.transform,
                filter.sharedMesh.bounds,
                cubesRoot,
                out Vector3 minimum,
                out Vector3 maximum
            );

            Vector3 blockSize = maximum - minimum;
            int axis = blockSize.x >= blockSize.y && blockSize.x >= blockSize.z
                ? 0
                : blockSize.y >= blockSize.z ? 1 : 2;

            DetectedBlock block = new DetectedBlock
            {
                Minimum = minimum,
                Maximum = maximum,
                Center = (minimum + maximum) * 0.5f,
                Axis = axis
            };

            blocks.Add(block);

            if (axis == 0)
                xCenters.Add(block.Center.x);
            else if (axis == 2)
                zCenters.Add(block.Center.z);
        }

        if (xCenters.Count < 2 || zCenters.Count < 2)
            return false;

        float middleX = GetMedian(xCenters);
        float middleZ = GetMedian(zCenters);
        List<float> leftFaces = new List<float>();
        List<float> rightFaces = new List<float>();
        List<float> backFaces = new List<float>();
        List<float> frontFaces = new List<float>();
        List<float> floorFaces = new List<float>();
        float top = float.MinValue;

        foreach (DetectedBlock block in blocks)
        {
            if (block.Axis == 0)
            {
                if (block.Center.x < middleX)
                    leftFaces.Add(block.Maximum.x);
                else
                    rightFaces.Add(block.Minimum.x);

                top = Mathf.Max(top, block.Maximum.y);
            }
            else if (block.Axis == 1)
            {
                floorFaces.Add(block.Maximum.y);
            }
            else
            {
                if (block.Center.z < middleZ)
                    backFaces.Add(block.Maximum.z);
                else
                    frontFaces.Add(block.Minimum.z);

                top = Mathf.Max(top, block.Maximum.y);
            }
        }

        if (leftFaces.Count == 0 ||
            rightFaces.Count == 0 ||
            backFaces.Count == 0 ||
            frontFaces.Count == 0 ||
            floorFaces.Count == 0 ||
            top == float.MinValue)
        {
            return false;
        }

        float left = GetMostCommon(leftFaces);
        float right = GetMostCommon(rightFaces);
        float back = GetMostCommon(backFaces);
        float front = GetMostCommon(frontFaces);
        float floor = GetMostCommon(floorFaces);
        Vector3 localCenter = new Vector3(
            (left + right) * 0.5f,
            (floor + top) * 0.5f,
            (back + front) * 0.5f
        );

        Vector3 scale = cubesRoot.lossyScale;
        center = cubesRoot.TransformPoint(localCenter);
        rotation = cubesRoot.rotation;
        size = new Vector3(
            Mathf.Abs(right - left) * Mathf.Abs(scale.x),
            Mathf.Abs(top - floor) * Mathf.Abs(scale.y),
            Mathf.Abs(front - back) * Mathf.Abs(scale.z)
        );
        return true;
    }

    private struct DetectedBlock
    {
        public Vector3 Minimum;
        public Vector3 Maximum;
        public Vector3 Center;
        public int Axis;
    }

    private float GetMedian(List<float> values)
    {
        values.Sort();
        int middle = values.Count / 2;

        if (values.Count % 2 == 0)
            return (values[middle - 1] + values[middle]) * 0.5f;

        return values[middle];
    }

    private float GetMostCommon(List<float> values)
    {
        float step = Mathf.Max(0.001f, detectionStep);
        Dictionary<int, int> counts = new Dictionary<int, int>();
        Dictionary<int, float> sums = new Dictionary<int, float>();
        int bestKey = 0;
        int bestCount = 0;

        foreach (float value in values)
        {
            int key = Mathf.RoundToInt(value / step);

            if (!counts.ContainsKey(key))
            {
                counts[key] = 0;
                sums[key] = 0f;
            }

            counts[key]++;
            sums[key] += value;

            if (counts[key] > bestCount)
            {
                bestKey = key;
                bestCount = counts[key];
            }
        }

        return sums[bestKey] / counts[bestKey];
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
        Transform cubeRoot,
        Bounds meshBounds,
        out Vector3 flatPosition,
        out Vector3 normal,
        out bool isFloor
    )
    {
        GetRoomBounds(cubeRoot, meshBounds, out Vector3 minimum, out Vector3 maximum);
        Vector3 localPosition = (minimum + maximum) * 0.5f;
        Vector3 halfSize = flatRoomSize * 0.5f;
        Vector3 normalized = new Vector3(
            Mathf.Abs(localPosition.x) / Mathf.Max(0.01f, halfSize.x),
            Mathf.Abs(localPosition.y) / Mathf.Max(0.01f, halfSize.y),
            Mathf.Abs(localPosition.z) / Mathf.Max(0.01f, halfSize.z)
        );
        float verticalDistance = localPosition.y < 0f || hasCeiling
            ? normalized.y
            : -1f;

        Vector3 localNormal;
        Vector3 surfacePosition = localPosition;

        if (normalized.x >= verticalDistance && normalized.x >= normalized.z)
        {
            float sign = Mathf.Sign(localPosition.x);
            surfacePosition.x = sign > 0f ? minimum.x : maximum.x;
            localNormal = Vector3.right * sign;
        }
        else if (verticalDistance >= normalized.z)
        {
            float sign = Mathf.Sign(localPosition.y);
            surfacePosition.y = sign > 0f ? minimum.y : maximum.y;
            localNormal = Vector3.up * sign;
        }
        else
        {
            float sign = Mathf.Sign(localPosition.z);
            surfacePosition.z = sign > 0f ? minimum.z : maximum.z;
            localNormal = Vector3.forward * sign;
        }

        Vector3 targetSurface = surfacePosition;

        if (Mathf.Abs(localNormal.x) > 0.5f)
            targetSurface.x = localNormal.x * halfSize.x;
        else if (Mathf.Abs(localNormal.y) > 0.5f)
            targetSurface.y = localNormal.y * halfSize.y;
        else
            targetSurface.z = localNormal.z * halfSize.z;

        Vector3 worldSurface = flatRoomCenter.TransformPoint(surfacePosition);
        Vector3 targetWorldSurface = flatRoomCenter.TransformPoint(targetSurface);
        flatPosition = cubeRoot.position + targetWorldSurface - worldSurface;
        normal = flatRoomCenter.TransformDirection(localNormal).normalized;
        isFloor = localNormal.y < -0.5f;
    }

    private void GetRoomBounds(
        Transform cubeRoot,
        Bounds meshBounds,
        out Vector3 minimum,
        out Vector3 maximum
    )
    {
        GetBoundsInTransform(
            cubeRoot,
            meshBounds,
            flatRoomCenter,
            out minimum,
            out maximum
        );
    }

    private void GetBoundsInTransform(
        Transform cubeRoot,
        Bounds meshBounds,
        Transform target,
        out Vector3 minimum,
        out Vector3 maximum
    )
    {
        Vector3 center = meshBounds.center;
        Vector3 extents = meshBounds.extents;
        minimum = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        maximum = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 meshPoint = center + Vector3.Scale(
                        extents,
                        new Vector3(x, y, z)
                    );
                    Vector3 worldPoint = cubeRoot.TransformPoint(meshPoint);
                    Vector3 roomPoint = target.InverseTransformPoint(worldPoint);
                    minimum = Vector3.Min(minimum, roomPoint);
                    maximum = Vector3.Max(maximum, roomPoint);
                }
            }
        }
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

    private void OnDrawGizmosSelected()
    {
        if (!showRoomPreview)
            return;

        Vector3 center;
        Quaternion rotation;
        Vector3 size;
        bool detected = detectRoomAutomatically
            ? TryCalculateRoom(out center, out rotation, out size)
            : TryGetManualRoom(out center, out rotation, out size);

        if (!detected)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            return;
        }

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
        Vector3 half = size * 0.5f;
        float thickness = Mathf.Max(0.01f, Mathf.Min(size.x, size.z) * 0.002f);

        Gizmos.color = previewColor;
        Gizmos.DrawCube(
            new Vector3(0f, -half.y, 0f),
            new Vector3(size.x, thickness, size.z)
        );
        Gizmos.DrawCube(
            new Vector3(-half.x, 0f, 0f),
            new Vector3(thickness, size.y, size.z)
        );
        Gizmos.DrawCube(
            new Vector3(half.x, 0f, 0f),
            new Vector3(thickness, size.y, size.z)
        );
        Gizmos.DrawCube(
            new Vector3(0f, 0f, -half.z),
            new Vector3(size.x, size.y, thickness)
        );
        Gizmos.DrawCube(
            new Vector3(0f, 0f, half.z),
            new Vector3(size.x, size.y, thickness)
        );

        Gizmos.color = new Color(previewColor.r, previewColor.g, previewColor.b, 1f);
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.DrawSphere(Vector3.zero, thickness * 4f);
        Gizmos.matrix = oldMatrix;
    }

    private bool TryGetManualRoom(
        out Vector3 center,
        out Quaternion rotation,
        out Vector3 size
    )
    {
        if (flatRoomCenter == null)
        {
            center = transform.position;
            rotation = transform.rotation;
            size = Vector3.zero;
            return false;
        }

        center = flatRoomCenter.position;
        rotation = flatRoomCenter.rotation;
        size = flatRoomSize;
        return true;
    }
}
