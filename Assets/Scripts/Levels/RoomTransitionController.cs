using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTransitionController : MonoBehaviour
{
    private static readonly Dictionary<Material, Color> originalMaterialColors =
        new Dictionary<Material, Color>();

    [SerializeField] private RoomCubeField roomCubes;
    [SerializeField] private Transform levelObjectsRoot;
    [SerializeField, HideInInspector] private Material roomMaterial;
    [SerializeField] private Material[] roomMaterials;
    [SerializeField] private Color transitionColor = Color.white;
    [SerializeField] private float materialColorDuration = 4f;
    [SerializeField] private float objectMoveDuration = 4f;
    [SerializeField] private float roomMoveDuration = 3f;
    [SerializeField] private float breathingSettleDuration = 1.5f;
    [SerializeField] private float objectDelay = 0.12f;
    [SerializeField] private float hiddenDepth = 2f;
    [SerializeField] private bool buildWhenSceneStarts;
    [SerializeField] private AudioSource rebuildAudioSource;
    [SerializeField] private AudioClip rebuildSound;

    private readonly List<TransitionObject> objects = new List<TransitionObject>();
    private readonly List<TransitionMaterial> transitionMaterials = new List<TransitionMaterial>();

    private Transform FlatRoomCenter => roomCubes != null
        ? roomCubes.FlatRoomCenter
        : transform;
    private Vector3 FlatRoomSize => roomCubes != null
        ? roomCubes.FlatRoomSize
        : new Vector3(20f, 8f, 20f);

    private class TransitionObject
    {
        public GameObject GameObject;
        public Transform Transform;
        public Vector3 FinalPosition;
        public Quaternion FinalRotation;
        public Vector3 StartPosition;
        public Quaternion StartRotation;
        public Vector3 HiddenPosition;
        public Vector3 RollAxis;
        public float RollAngle;
        public bool Rolls;
        public Rigidbody[] Bodies;
        public bool[] KinematicStates;
        public bool[] GravityStates;
        public Collider[] Colliders;
        public bool[] ColliderStates;
        public MonoBehaviour[] Behaviours;
        public bool[] BehaviourStates;
    }

    private class TransitionMaterial
    {
        public Material Material;
        public string ColorProperty;
        public Color OriginalColor;
        public Color StartColor;
        public Color TargetColor;
    }

    public bool BuildWhenSceneStarts => buildWhenSceneStarts;

    private void Awake()
    {
        if (roomCubes == null)
            roomCubes = FindFirstObjectByType<RoomCubeField>();

        CaptureMaterialColors();
        CaptureObjects();
    }

    public IEnumerator HideLevel()
    {
        Coroutine colorAnimation = StartMaterialAnimation(true);
        StartRebuildSound();
        PrepareObjects(false);
        yield return AnimateObjects(0f, 1f);
        yield return AnimateRoom(0f, 1f);
        yield return AnimateBreathing(1f, 0f);

        if (colorAnimation != null)
            yield return colorAnimation;

        if (roomCubes != null)
            roomCubes.SetFrozen(true);

        StopRebuildSound();
    }

    public IEnumerator BuildLevel()
    {
        StartRebuildSound();
        SetTransitionMaterialColors();
        Coroutine colorAnimation = StartMaterialAnimation(false);

        if (roomCubes != null)
        {
            roomCubes.SetFlatness(1f);
            roomCubes.SetBreathingStrength(0f);
            roomCubes.SetFrozen(false);
        }

        PrepareObjects(true);
        SetObjectProgress(1f);
        yield return AnimateRoom(1f, 0f);
        yield return AnimateBreathing(0f, 1f);
        yield return AnimateObjects(1f, 0f);

        if (colorAnimation != null)
            yield return colorAnimation;

        RestoreObjects();
        StopRebuildSound();
    }

    private void StartRebuildSound()
    {
        if (rebuildAudioSource == null || rebuildSound == null)
            return;

        rebuildAudioSource.clip = rebuildSound;
        rebuildAudioSource.loop = true;
        rebuildAudioSource.Play();
    }

    private void StopRebuildSound()
    {
        if (rebuildAudioSource != null)
            rebuildAudioSource.Stop();
    }

    private void OnDisable()
    {
        StopRebuildSound();
    }

    private void CaptureMaterialColors()
    {
        transitionMaterials.Clear();
        AddTransitionMaterial(roomMaterial);

        if (roomMaterials == null)
            return;

        foreach (Material material in roomMaterials)
            AddTransitionMaterial(material);
    }

    private void AddTransitionMaterial(Material material)
    {
        if (material == null)
            return;

        foreach (TransitionMaterial item in transitionMaterials)
        {
            if (item.Material == material)
                return;
        }

        string colorProperty;

        if (material.HasProperty("_BaseColor"))
            colorProperty = "_BaseColor";
        else if (material.HasProperty("_Color"))
            colorProperty = "_Color";
        else
            return;

        if (!originalMaterialColors.TryGetValue(material, out Color originalColor))
        {
            originalColor = material.GetColor(colorProperty);
            originalMaterialColors.Add(material, originalColor);
        }

        transitionMaterials.Add(new TransitionMaterial
        {
            Material = material,
            ColorProperty = colorProperty,
            OriginalColor = originalColor
        });
    }

    private Coroutine StartMaterialAnimation(bool toTransitionColor)
    {
        if (transitionMaterials.Count == 0)
            return null;

        foreach (TransitionMaterial item in transitionMaterials)
        {
            item.StartColor = item.Material.GetColor(item.ColorProperty);
            item.TargetColor = toTransitionColor
                ? transitionColor
                : item.OriginalColor;
        }

        return StartCoroutine(AnimateMaterialColors());
    }

    private IEnumerator AnimateMaterialColors()
    {
        float duration = Mathf.Max(0.01f, materialColorDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Smooth(Mathf.Clamp01(elapsed / duration));

            foreach (TransitionMaterial item in transitionMaterials)
            {
                item.Material.SetColor(
                    item.ColorProperty,
                    Color.Lerp(item.StartColor, item.TargetColor, progress)
                );
            }

            yield return null;
        }

        foreach (TransitionMaterial item in transitionMaterials)
            item.Material.SetColor(item.ColorProperty, item.TargetColor);
    }

    private void SetTransitionMaterialColors()
    {
        foreach (TransitionMaterial item in transitionMaterials)
            item.Material.SetColor(item.ColorProperty, transitionColor);
    }

    private void CaptureObjects()
    {
        if (levelObjectsRoot == null)
            return;

        foreach (Transform child in levelObjectsRoot)
        {
            objects.Add(new TransitionObject
            {
                GameObject = child.gameObject,
                Transform = child,
                FinalPosition = child.position,
                FinalRotation = child.rotation
            });
        }
    }

    private void PrepareObjects(bool useFinalPose)
    {
        foreach (TransitionObject item in objects)
        {
            if (item.Transform == null)
                continue;

            if (!item.GameObject.activeSelf)
                item.GameObject.SetActive(true);

            item.StartPosition = useFinalPose ? item.FinalPosition : item.Transform.position;
            item.StartRotation = useFinalPose ? item.FinalRotation : item.Transform.rotation;
            GetHiddenPose(item);
            DisableObject(item);
        }
    }

    private void GetHiddenPose(TransitionObject item)
    {
        Vector3 localPosition = FlatRoomCenter.InverseTransformPoint(item.StartPosition);
        Vector3 halfSize = FlatRoomSize * 0.5f;
        float floorDistance = Mathf.Abs(localPosition.y + halfSize.y);
        float ceilingDistance = roomCubes != null && roomCubes.HasCeiling
            ? Mathf.Abs(halfSize.y - localPosition.y)
            : float.MaxValue;
        float leftDistance = Mathf.Abs(localPosition.x + halfSize.x);
        float rightDistance = Mathf.Abs(halfSize.x - localPosition.x);
        float backDistance = Mathf.Abs(localPosition.z + halfSize.z);
        float frontDistance = Mathf.Abs(halfSize.z - localPosition.z);
        float nearest = Mathf.Min(
            floorDistance,
            ceilingDistance,
            leftDistance,
            rightDistance,
            backDistance,
            frontDistance
        );

        item.Rolls = nearest == floorDistance;
        Vector3 localDirection;

        if (item.Rolls)
        {
            float xDistance = Mathf.Min(leftDistance, rightDistance);
            float zDistance = Mathf.Min(backDistance, frontDistance);

            if (xDistance <= zDistance)
                localDirection = localPosition.x < 0f ? Vector3.left : Vector3.right;
            else
                localDirection = localPosition.z < 0f ? Vector3.back : Vector3.forward;
        }
        else if (nearest == ceilingDistance)
            localDirection = Vector3.up;
        else if (nearest == leftDistance)
            localDirection = Vector3.left;
        else if (nearest == rightDistance)
            localDirection = Vector3.right;
        else if (nearest == backDistance)
            localDirection = Vector3.back;
        else
            localDirection = Vector3.forward;

        Vector3 direction = FlatRoomCenter.TransformDirection(localDirection).normalized;
        Vector3 hiddenLocal = localPosition;

        if (Mathf.Abs(localDirection.x) > 0.5f)
            hiddenLocal.x = localDirection.x * (halfSize.x + hiddenDepth);
        else if (Mathf.Abs(localDirection.y) > 0.5f)
            hiddenLocal.y = localDirection.y * (halfSize.y + hiddenDepth);
        else
            hiddenLocal.z = localDirection.z * (halfSize.z + hiddenDepth);

        item.HiddenPosition = FlatRoomCenter.TransformPoint(hiddenLocal);
        item.RollAxis = Vector3.Cross(direction, FlatRoomCenter.up).normalized;

        Bounds bounds = GetBounds(item.GameObject);
        float radius = Mathf.Max(0.1f, bounds.extents.y);
        float distance = Vector3.Distance(item.StartPosition, item.HiddenPosition);
        item.RollAngle = distance / radius * Mathf.Rad2Deg;
    }

    private IEnumerator AnimateObjects(float from, float to)
    {
        float duration = Mathf.Max(0.01f, objectMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float value = Mathf.Lerp(from, to, Smooth(progress));
            SetObjectProgress(value);
            yield return null;
        }

        SetObjectProgress(to);
    }

    private void SetObjectProgress(float progress)
    {
        float totalDelay = Mathf.Max(0f, objects.Count - 1) * objectDelay;
        float fullDuration = Mathf.Max(0.01f, objectMoveDuration + totalDelay);

        for (int i = 0; i < objects.Count; i++)
        {
            TransitionObject item = objects[i];

            if (item.Transform == null)
                continue;

            float delay = i * objectDelay / fullDuration;
            float available = Mathf.Max(0.01f, 1f - totalDelay / fullDuration);
            float localProgress = Mathf.Clamp01((progress - delay) / available);
            localProgress = Smooth(localProgress);

            item.Transform.position = Vector3.Lerp(
                item.StartPosition,
                item.HiddenPosition,
                localProgress
            );

            if (item.Rolls)
            {
                item.Transform.rotation = Quaternion.AngleAxis(
                    item.RollAngle * localProgress,
                    item.RollAxis
                ) * item.StartRotation;

                if (roomCubes != null && localProgress > 0f && localProgress < 1f)
                {
                    float wave = Mathf.Sin(localProgress * Mathf.PI);
                    roomCubes.AddFloorWave(item.Transform.position, wave);
                }
            }
            else
            {
                item.Transform.rotation = item.StartRotation;
            }
        }
    }

    private IEnumerator AnimateRoom(float from, float to)
    {
        if (roomCubes == null)
            yield break;

        float duration = Mathf.Max(0.01f, roomMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Smooth(Mathf.Clamp01(elapsed / duration));
            roomCubes.SetFlatness(Mathf.Lerp(from, to, progress));
            yield return null;
        }

        roomCubes.SetFlatness(to);
    }

    private IEnumerator AnimateBreathing(float from, float to)
    {
        if (roomCubes == null)
            yield break;

        float duration = Mathf.Max(0.01f, breathingSettleDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Smooth(Mathf.Clamp01(elapsed / duration));
            roomCubes.SetBreathingStrength(Mathf.Lerp(from, to, progress));
            yield return null;
        }

        roomCubes.SetBreathingStrength(to);
    }

    private void DisableObject(TransitionObject item)
    {
        item.Colliders = item.GameObject.GetComponentsInChildren<Collider>(true);
        item.ColliderStates = new bool[item.Colliders.Length];

        for (int i = 0; i < item.Colliders.Length; i++)
        {
            Collider itemCollider = item.Colliders[i];
            item.ColliderStates[i] = itemCollider.enabled;
            itemCollider.enabled = false;
        }

        item.Bodies = item.GameObject.GetComponentsInChildren<Rigidbody>(true);
        item.KinematicStates = new bool[item.Bodies.Length];
        item.GravityStates = new bool[item.Bodies.Length];

        for (int i = 0; i < item.Bodies.Length; i++)
        {
            Rigidbody body = item.Bodies[i];
            item.KinematicStates[i] = body.isKinematic;
            item.GravityStates[i] = body.useGravity;
            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        item.Behaviours = item.GameObject.GetComponentsInChildren<MonoBehaviour>(true);
        item.BehaviourStates = new bool[item.Behaviours.Length];

        for (int i = 0; i < item.Behaviours.Length; i++)
        {
            MonoBehaviour behaviour = item.Behaviours[i];
            item.BehaviourStates[i] = behaviour.enabled;
            behaviour.enabled = false;
        }
    }

    private void RestoreObjects()
    {
        foreach (TransitionObject item in objects)
        {
            if (item.Transform == null)
                continue;

            item.Transform.SetPositionAndRotation(item.FinalPosition, item.FinalRotation);

            for (int i = 0; i < item.Bodies.Length; i++)
            {
                if (item.Bodies[i] == null)
                    continue;

                item.Bodies[i].useGravity = item.GravityStates[i];
                item.Bodies[i].isKinematic = item.KinematicStates[i];
            }

            for (int i = 0; i < item.Colliders.Length; i++)
            {
                if (item.Colliders[i] != null)
                    item.Colliders[i].enabled = item.ColliderStates[i];
            }

            for (int i = 0; i < item.Behaviours.Length; i++)
            {
                if (item.Behaviours[i] != null)
                    item.Behaviours[i].enabled = item.BehaviourStates[i];
            }
        }

        Physics.SyncTransforms();
    }

    private Bounds GetBounds(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);

        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;

            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);

            return bounds;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        return new Bounds(target.transform.position, Vector3.one);
    }

    private float Smooth(float value)
    {
        return value * value * (3f - 2f * value);
    }
}
